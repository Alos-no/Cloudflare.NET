namespace Cloudflare.NET.R2.Tests.UnitTests;

using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Models;
using Moq;
using NET.Tests.Shared.Fixtures;

[Trait("Category", TestConstants.TestCategories.Unit)]
public class R2ClientUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly Mock<IAmazonS3> _mockS3Client;
  private readonly R2Client        _sut;

  // Constants from R2Client to use in tests.
  private const long R2MinPartSize = 5L * 1024 * 1024;
  private const long R2MaxPartSize = 5L * 1024 * 1024 * 1024;

  #endregion

  #region Constructors

  public R2ClientUnitTests()
  {
    _mockS3Client = new Mock<IAmazonS3>();
    var mockLogger = new Mock<ILogger<R2Client>>();
    _sut = new R2Client(mockLogger.Object, _mockS3Client.Object);
  }

  #endregion

  #region Methods

  [Fact]
  public async Task UploadAsync_WithSmallFile_UsesSinglePartUpload()
  {
    // Arrange
    using var stream = new MemoryStream(new byte[1024]); // 1 KB
    _mockS3Client
      .Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new PutObjectResponse());

    // Act
    var result = await _sut.UploadAsync("bucket", "key", stream);

    // Assert
    _mockS3Client.Verify(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    _mockS3Client.Verify(
      c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    result.ClassAOperations.Should().Be(1);
    result.IngressBytes.Should().Be(1024);
  }

  [Fact]
  public async Task UploadSinglePartAsync_OnS3Error_ThrowsCloudflareR2OperationException()
  {
    // Arrange
    using var stream = new MemoryStream(new byte[1024]);
    var s3Exception = new AmazonS3Exception("Access Denied", ErrorType.Sender, "AccessDenied", "reqid", HttpStatusCode.Forbidden);

    _mockS3Client
      .Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(s3Exception);

    // Act
    var action = () => _sut.UploadSinglePartAsync("bucket", "key", stream);

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareR2OperationException>();
    ex.Which.InnerException.Should().Be(s3Exception);
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(0); // The operation failed, but the attempt is not counted here.
    ex.Which.PartialMetrics.IngressBytes.Should().Be(0);
  }

  [Fact]
  public async Task UploadAsync_WithLargeFile_UsesMultipartUpload()
  {
    // Arrange
    var       sixtyMb  = 60 * 1024 * 1024;
    using var stream   = new MemoryStream(new byte[sixtyMb]);
    var       uploadId = "test-upload-id";

    _mockS3Client
      .Setup(c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new InitiateMultipartUploadResponse { UploadId = uploadId });
    _mockS3Client
      .Setup(c => c.UploadPartAsync(It.IsAny<UploadPartRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((UploadPartRequest req, CancellationToken _) => new UploadPartResponse { PartNumber = req.PartNumber, ETag = "etag" });
    _mockS3Client
      .Setup(c => c.CompleteMultipartUploadAsync(It.IsAny<CompleteMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new CompleteMultipartUploadResponse());

    // Act
    var result = await _sut.UploadAsync("bucket", "key", stream);

    // Assert
    _mockS3Client.Verify(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    _mockS3Client.Verify(
      c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    // 60MB file with 50MB chunks = 2 parts
    _mockS3Client.Verify(c => c.UploadPartAsync(It.IsAny<UploadPartRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    _mockS3Client.Verify(
      c => c.CompleteMultipartUploadAsync(It.IsAny<CompleteMultipartUploadRequest>(), It.IsAny<CancellationToken>()), Times.Once);

    result.ClassAOperations.Should().Be(1 + 2 + 1); // Init + 2 parts + Complete
    result.IngressBytes.Should().Be(sixtyMb);
  }

  [Fact]
  public async Task UploadMultipartAsync_WithNonSeekableStream_ThrowsNotSupportedException()
  {
    // Arrange
    var mockStream = new Mock<Stream>();
    mockStream.Setup(s => s.CanSeek).Returns(false);

    // Act
    var action = async () => await _sut.UploadMultipartAsync("bucket", "key", mockStream.Object, null);

    // Assert
    await action.Should().ThrowAsync<NotSupportedException>();
  }

  [Fact]
  public async Task UploadAsync_WithNonExistentFile_ThrowsFileNotFoundException()
  {
    // Arrange
    var nonExistentPath = "non-existent-file.tmp";

    // Act
    var action = async () => await _sut.UploadAsync("bucket", "key", nonExistentPath);

    // Assert
    await action.Should().ThrowAsync<FileNotFoundException>();
  }

  [Fact]
  public async Task UploadMultipartAsync_WhenInitiateFails_ThrowsCloudflareR2OperationException()
  {
    // Arrange
    using var stream = new MemoryStream(new byte[60 * 1024 * 1024]);
    var s3Exception = new AmazonS3Exception("Initiate Failed");

    _mockS3Client
      .Setup(c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(s3Exception);

    // Act
    var action = () => _sut.UploadMultipartAsync("bucket", "key", stream, null);

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareR2OperationException>();
    ex.Which.InnerException.Should().Be(s3Exception);
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(1); // The failed initiation attempt.
  }

  [Fact]
  public async Task UploadMultipartAsync_WhenPartUploadFails_AbortsAndThrows()
  {
    // Arrange
    using var stream   = new MemoryStream(new byte[60 * 1024 * 1024]);
    var       uploadId = "test-upload-id";
    var s3Exception = new AmazonS3Exception("Part Upload Failed");

    _mockS3Client
      .Setup(c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new InitiateMultipartUploadResponse { UploadId = uploadId });

    _mockS3Client
      .Setup(c => c.UploadPartAsync(It.IsAny<UploadPartRequest>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(s3Exception);

    _mockS3Client
      .Setup(c => c.AbortMultipartUploadAsync(It.IsAny<AbortMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new AbortMultipartUploadResponse());

    // Act
    var action = () => _sut.UploadMultipartAsync("bucket", "key", stream, null);

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareR2OperationException>();
    ex.Which.InnerException.Should().Be(s3Exception);
    // 1 (init) + 1 (failed part) + 1 (abort)
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(3);
    _mockS3Client.Verify(c => c.AbortMultipartUploadAsync(
      It.Is<AbortMultipartUploadRequest>(r => r.UploadId == uploadId), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task UploadMultipartAsync_WithTooSmallPartSize_ClampsToMin()
  {
    // Arrange
    var       fileSize = 60 * 1024 * 1024;
    using var stream   = new MemoryStream(new byte[fileSize]);
    var       uploadId = "test-upload-id";
    var capturedRequests = new List<UploadPartRequest>();

    _mockS3Client
      .Setup(c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new InitiateMultipartUploadResponse { UploadId = uploadId });
    _mockS3Client
      .Setup(c => c.UploadPartAsync(It.IsAny<UploadPartRequest>(), It.IsAny<CancellationToken>()))
      .Callback<UploadPartRequest, CancellationToken>((req, _) => capturedRequests.Add(req))
      .ReturnsAsync((UploadPartRequest req, CancellationToken _) => new UploadPartResponse { PartNumber = req.PartNumber, ETag = "etag" });
    _mockS3Client
      .Setup(c => c.CompleteMultipartUploadAsync(It.IsAny<CompleteMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new CompleteMultipartUploadResponse());

    // Act: Request a part size of 1MB, which is below the 5MB minimum.
    await _sut.UploadMultipartAsync("bucket", "key", stream, 1 * 1024 * 1024);

    // Assert
    capturedRequests.Should().NotBeEmpty();
    capturedRequests.First().PartSize.Should().Be(R2MinPartSize);
  }

  [Fact]
  public async Task DownloadFileAsync_WhenObjectDoesNotExist_ThrowsCloudflareR2OperationException()
  {
    // Arrange
    _mockS3Client
      .Setup(c => c.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new AmazonS3Exception("Not Found", ErrorType.Sender, "NoSuchKey", "reqid", HttpStatusCode.NotFound));

    // Act
    var action = async () => await _sut.DownloadFileAsync("bucket", "non-existent-key", "some-path");

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareR2OperationException>();
    ex.Which.InnerException.Should().BeOfType<AmazonS3Exception>();
    ex.Which.PartialMetrics.ClassBOperations.Should().Be(1);
  }

  [Fact]
  public async Task DeleteObjectAsync_OnS3Error_ThrowsCloudflareR2OperationException()
  {
    // Arrange
    var s3Exception = new AmazonS3Exception("Delete failed");
    _mockS3Client
      .Setup(c => c.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(s3Exception);

    // Act
    var action = () => _sut.DeleteObjectAsync("bucket", "key");

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareR2OperationException>();
    ex.Which.InnerException.Should().Be(s3Exception);
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(1);
  }

  [Fact]
  public async Task DeleteObjectsAsync_WithEmptyKeyList_DoesNothing()
  {
    // Arrange
    var emptyList = Enumerable.Empty<string>();

    // Act
    var result = await _sut.DeleteObjectsAsync("bucket", emptyList);

    // Assert
    result.ClassAOperations.Should().Be(0);
    _mockS3Client.Verify(c => c.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()), Times.Never);
  }


  [Fact]
  public async Task DeleteObjectsAsync_WithOver1000Keys_BatchesRequests()
  {
    // Arrange
    var keys             = Enumerable.Range(1, 1500).Select(i => $"key-{i}").ToList();
    var capturedRequests = new List<DeleteObjectsRequest>();

    _mockS3Client
      .Setup(c => c.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
      .Callback<DeleteObjectsRequest, CancellationToken>((req, _) => capturedRequests.Add(req))
      // The response must have a non-null DeleteErrors list to avoid a NullReferenceException.
      .ReturnsAsync(new DeleteObjectsResponse { DeleteErrors = [] });

    // Act
    var result = await _sut.DeleteObjectsAsync("bucket", keys);

    // Assert
    _mockS3Client.Verify(c => c.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    capturedRequests.Count.Should().Be(2);
    capturedRequests[0].Objects.Count.Should().Be(1000);
    capturedRequests[1].Objects.Count.Should().Be(500);
    result.ClassAOperations.Should().Be(2);
  }

  [Fact]
  public async Task DeleteObjectsAsync_WhenApiReturnsErrorsAndContinueOnErrorIsTrue_ThrowsAtEnd()
  {
    // Arrange
    var keys = new[] { "key-1", "key-2", "key-fail" };
    var errorResponse = new DeleteObjectsResponse
    {
      DeleteErrors = [new DeleteError { Key = "key-fail", Message = "Some error" }]
    };
    _mockS3Client
      .Setup(c => c.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(errorResponse);

    // Act
    var action = async () => await _sut.DeleteObjectsAsync("bucket", keys, true);

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareR2BatchException<string>>();
    ex.Which.FailedItems.Should().ContainSingle().Which.Should().Be("key-fail");
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(1);
  }

  [Fact]
  public async Task DeleteObjectsAsync_WhenApiReturnsErrorsAndContinueOnErrorIsFalse_ThrowsImmediately()
  {
    // Arrange
    var keys = new[] { "key-1", "key-2", "key-fail" };
    var errorResponse = new DeleteObjectsResponse
    {
      DeleteErrors = [new DeleteError { Key = "key-fail" }]
    };
    _mockS3Client
      .Setup(c => c.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(errorResponse);

    // Act
    var action = async () => await _sut.DeleteObjectsAsync("bucket", keys, false);

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareR2BatchException<string>>();
    ex.Which.FailedItems.Should().ContainSingle().Which.Should().Be("key-fail");
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(1);
  }

  [Fact]
  public async Task ClearBucketAsync_HandlesPagination()
  {
    // Arrange
    var page1Keys = Enumerable.Range(1, 10).Select(i => new S3Object { Key = $"key-page1-{i}" }).ToList();
    var page2Keys = Enumerable.Range(1, 5).Select(i => new S3Object { Key  = $"key-page2-{i}" }).ToList();

    _mockS3Client.SetupSequence(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new ListObjectsV2Response { S3Objects = page1Keys, IsTruncated = true, NextContinuationToken = "token" })
                 .ReturnsAsync(new ListObjectsV2Response { S3Objects = page2Keys, IsTruncated = false });

    _mockS3Client.Setup(c => c.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
                 // The response must have a non-null DeleteErrors list to avoid a NullReferenceException.
                 .ReturnsAsync(new DeleteObjectsResponse { DeleteErrors = [] });

    // Act
    var result = await _sut.ClearBucketAsync("bucket");

    // Assert
    _mockS3Client.Verify(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    _mockS3Client.Verify(c => c.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    result.ClassAOperations.Should().Be(4); // 2 lists + 2 deletes
  }

  [Fact]
  public async Task ClearBucketAsync_WhenListFails_ThrowsCloudflareR2ListException()
  {
    // Arrange
    _mockS3Client
      .Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new AmazonS3Exception("List failed"));

    // Act
    var action = async () => await _sut.ClearBucketAsync("bucket");

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareR2ListException<string>>();
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(0);
    ex.Which.PartialData.Should().BeEmpty();
  }

  [Fact]
  public async Task ClearBucketAsync_WhenDeleteFailsAndContinueOnErrorIsFalse_ThrowsImmediately()
  {
    // Arrange
    var keys = Enumerable.Range(1, 10).Select(i => new S3Object { Key = $"key-{i}" }).ToList();
    _mockS3Client
      .Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListObjectsV2Response { S3Objects = keys, IsTruncated = false });

    _mockS3Client
      .Setup(c => c.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new CloudflareR2BatchException<string>("delete failed", ["key-5"], new R2Result(1), new Exception()));


    // Act
    var action = async () => await _sut.ClearBucketAsync("bucket", false);

    // Assert
    await action.Should().ThrowAsync<CloudflareR2BatchException<string>>();
    _mockS3Client.Verify(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()), Times.Once);
    _mockS3Client.Verify(c => c.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task ClearBucketAsync_WhenFullBatchDeleteFails_AbortsToPreventInfiniteLoop()
  {
    // Arrange
    var keys = Enumerable.Range(1, 10).Select(i => new S3Object { Key = $"key-{i}" }).ToList();
    _mockS3Client
      .Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListObjectsV2Response { S3Objects = keys, IsTruncated = true, NextContinuationToken = "token" });

    // This exception indicates all 10 keys in the batch failed.
    var batchException = new CloudflareR2BatchException<string>(
      "delete failed", keys.Select(k => k.Key).ToList(), new R2Result(1), new Exception());
    _mockS3Client
      .Setup(c => c.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(batchException);

    // Act
    var action = async () => await _sut.ClearBucketAsync("bucket", true);

    // Assert
    // It should throw at the end, but critically, it should not make a second List call.
    var ex = await action.Should().ThrowAsync<CloudflareR2BatchException<string>>();
    ex.Which.FailedItems.Should().HaveCount(10);

    _mockS3Client.Verify(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()), Times.Once);
    _mockS3Client.Verify(c => c.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
  }


  [Fact]
  public async Task ListPartsAsync_HandlesPagination()
  {
    // Arrange
    var page1Parts = Enumerable.Range(1, 10).Select(i => new PartDetail { PartNumber = i }).ToList();
    var page2Parts = Enumerable.Range(11, 5).Select(i => new PartDetail { PartNumber = i }).ToList();

    _mockS3Client.SetupSequence(c => c.ListPartsAsync(It.IsAny<ListPartsRequest>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new ListPartsResponse { Parts = page1Parts, IsTruncated = true, NextPartNumberMarker = 10 })
                 .ReturnsAsync(new ListPartsResponse { Parts = page2Parts, IsTruncated = false });

    // Act
    var result = await _sut.ListPartsAsync("bucket", "key", "upload-id");

    // Assert
    result.Data.Should().HaveCount(15);
    result.Metrics.ClassAOperations.Should().Be(2);
    _mockS3Client.Verify(c => c.ListPartsAsync(It.IsAny<ListPartsRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
  }


  [Fact]
  public async Task ListPartsAsync_OnFailure_ThrowsWithPartialData()
  {
    // Arrange
    var uploadId   = "test-upload-id";
    var page1Parts = Enumerable.Range(1, 10).Select(i => new PartDetail { PartNumber = i, ETag = $"etag-{i}" }).ToList();

    _mockS3Client.SetupSequence(c => c.ListPartsAsync(It.IsAny<ListPartsRequest>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new ListPartsResponse { Parts = page1Parts, IsTruncated = true, NextPartNumberMarker = 10 })
                 .ThrowsAsync(new AmazonS3Exception("List failed"));

    // Act
    var action = () => _sut.ListPartsAsync("bucket", "key", uploadId);

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareR2ListException<ListedPart>>();
    ex.Which.PartialData.Should().HaveCount(10);
    ex.Which.PartialData.First().PartNumber.Should().Be(1);
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(1);
  }

  [Fact]
  public void CreatePresignedUploadPartsUrls_OnS3Error_ThrowsCloudflareR2OperationException()
  {
    // Arrange
    var s3Exception = new AmazonS3Exception("Presigning failed");
    var request = new PresignedUploadPartsRequest("bucket", "key", TimeSpan.FromMinutes(5),
                                                  new Dictionary<int, long> { { 1, 1024 } }, "application/octet-stream");

    // This setup uses a concrete client to test the exception wrapping logic.
    var mockS3Config  = new AmazonS3Config { ServiceURL = "https://dummy.r2.dev" };
    var mockS3Client  = new Mock<AmazonS3Client>("accessKey", "secretKey", mockS3Config);
    var mockLogger    = new Mock<ILogger<R2Client>>();
    var sutWithRealS3 = new R2Client(mockLogger.Object, mockS3Client.Object);

    mockS3Client
      .Setup(c => c.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
      .Throws(s3Exception);

    // Act
    var action = () => sutWithRealS3.CreatePresignedUploadPartsUrls("bucket", request);

    // Assert
    action.Should().Throw<CloudflareR2OperationException>().WithInnerException<AmazonS3Exception>();
  }

  #endregion
}
