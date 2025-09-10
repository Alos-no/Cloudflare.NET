namespace Cloudflare.NET.R2.Tests.UnitTests;

using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Models;
using Moq;
using Moq.Protected;
using NET.Tests.Shared.Fixtures;
using Xunit.Abstractions;

[Trait("Category", TestConstants.TestCategories.Unit)]
public class R2ClientUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly Mock<IAmazonS3> _mockS3Client;
  private readonly R2Client        _sut;

  #endregion

  #region Constructors

  public R2ClientUnitTests(ITestOutputHelper output)
  {
    _mockS3Client = new Mock<IAmazonS3>();
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    var loggerFactory  = new LoggerFactory([loggerProvider]);
    _sut = new R2Client(loggerFactory, _mockS3Client.Object);
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
  public async Task UploadSinglePartAsync_OnS3Error_ThrowsWithCorrectMetrics()
  {
    // Arrange
    var mockStream = new Mock<MemoryStream> { CallBase = true };
    mockStream.Object.Write(new byte[1024], 0, 1024);
    // Simulate that the SDK read 512 bytes before failing.
    mockStream.Object.Position = 512;

    var s3Exception = new AmazonS3Exception("Access Denied", ErrorType.Sender, "AccessDenied", "reqid", HttpStatusCode.Forbidden);

    _mockS3Client
      .Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(s3Exception);

    // Act
    var action = () => _sut.UploadSinglePartAsync("bucket", "key", mockStream.Object);

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareR2OperationException>();
    ex.Which.InnerException.Should().Be(s3Exception);
    // The operation attempt should always be counted.
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(1);
    // The partial ingress should be captured from the stream's position.
    ex.Which.PartialMetrics.IngressBytes.Should().Be(512);
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
    using var stream      = new MemoryStream(new byte[60 * 1024 * 1024]);
    var       s3Exception = new AmazonS3Exception("Initiate Failed");

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
    var mockStream = new Mock<MemoryStream> { CallBase = true };
    mockStream.Object.Write(new byte[60 * 1024 * 1024], 0, 60 * 1024 * 1024);
    // Simulate that the SDK read 1MB into the failing part before erroring.
    // The default part size is 50MB, so the position will be 50MB (part 1) + 1MB (failed part 2).
    mockStream.SetupGet(s => s.Position).Returns(50 * 1024 * 1024 + 1 * 1024 * 1024);
    mockStream.Object.Position = 0; // Reset for actual test.

    var uploadId    = "test-upload-id";
    var s3Exception = new AmazonS3Exception("Part Upload Failed");

    _mockS3Client
      .Setup(c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new InitiateMultipartUploadResponse { UploadId = uploadId });

    // First part succeeds.
    _mockS3Client
      .Setup(c => c.UploadPartAsync(It.Is<UploadPartRequest>(r => r.PartNumber == 1), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new UploadPartResponse { PartNumber = 1, ETag = "etag-1" });

    // Second part fails.
    _mockS3Client
      .Setup(c => c.UploadPartAsync(It.Is<UploadPartRequest>(r => r.PartNumber == 2), It.IsAny<CancellationToken>()))
      .ThrowsAsync(s3Exception);

    _mockS3Client
      .Setup(c => c.AbortMultipartUploadAsync(It.IsAny<AbortMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new AbortMultipartUploadResponse());

    // Act
    var action = () => _sut.UploadMultipartAsync("bucket", "key", mockStream.Object, null);

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareR2OperationException>();
    ex.Which.InnerException.Should().Be(s3Exception);
    // 1 (init) + 1 (successful part) + 1 (failed part) + 0 (free abort)
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(3);
    // 50MB (successful part 1) + 1MB (partial ingress from failed part 2)
    ex.Which.PartialMetrics.IngressBytes.Should().Be(50 * 1024 * 1024 + 1 * 1024 * 1024);
    _mockS3Client.Verify(c => c.AbortMultipartUploadAsync(
                           It.Is<AbortMultipartUploadRequest>(r => r.UploadId == uploadId), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task UploadMultipartAsync_WithTooSmallPartSize_ClampsToMin()
  {
    // Arrange
    var       fileSize         = 60 * 1024 * 1024;
    using var stream           = new MemoryStream(new byte[fileSize]);
    var       uploadId         = "test-upload-id";
    var       capturedRequests = new List<UploadPartRequest>();

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
    capturedRequests.First().PartSize.Should().Be(R2Client.R2MinPartSize);
  }

  /// <summary>
  ///   Verifies that if a user requests a part size larger than R2's maximum (5 GiB), the
  ///   client clamps it down to the maximum allowed size.
  /// </summary>
  [Fact]
  public async Task UploadMultipartAsync_WithTooLargePartSize_ClampsToMax()
  {
    // Arrange
    // The file size MUST be larger than the max part size to test clamping.
    // We'll simulate a file that would create one max-sized part and one smaller part.
    var fileSize = R2Client.R2MaxPartSize + (10 * 1024 * 1024); // 5 GiB + 10 MiB

    // Mock a seekable stream with a specific length, without allocating memory for it.
    var mockStream = new Mock<Stream>();
    mockStream.Setup(s => s.CanSeek).Returns(true);
    mockStream.Setup(s => s.Length).Returns(fileSize);
    // The S3 client will try to read from the stream, so we need to allow reads, returning 0 bytes read.
    mockStream.Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Returns(0);

    var uploadId         = "test-upload-id";
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

    // Act: Request a part size of 6 GiB, which is above the 5 GiB maximum.
    await _sut.UploadMultipartAsync("bucket", "key", mockStream.Object, 6L * 1024 * 1024 * 1024);

    // Assert
    capturedRequests.Should().NotBeEmpty();
    // The first part's size should be clamped to the maximum allowed size.
    capturedRequests.First().PartSize.Should().Be(R2Client.R2MaxPartSize);
    // There should be a second part for the remainder.
    capturedRequests.Should().HaveCount(2);
    capturedRequests[1].PartSize.Should().Be(10 * 1024 * 1024);
  }

  [Fact]
  public async Task DownloadFileAsync_OnS3Error_ThrowsWithCorrectMetrics()
  {
    // Arrange
    var mockStream = new Mock<MemoryStream> { CallBase = true };
    // Simulate that we wrote 256 bytes to the output stream before it failed.
    mockStream.SetupGet(s => s.Position).Returns(256);
    mockStream.Object.Position = 0; // Reset for test.

    var s3Exception =
      new AmazonS3Exception("Network Error", ErrorType.Receiver, "NetworkError", "reqid", HttpStatusCode.ServiceUnavailable);
    _mockS3Client
      .Setup(c => c.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(s3Exception);

    // Act
    var action = async () => await _sut.DownloadFileAsync("bucket", "key", mockStream.Object);

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareR2OperationException>();
    ex.Which.InnerException.Should().BeOfType<AmazonS3Exception>();
    // The operation attempt should always be counted.
    ex.Which.PartialMetrics.ClassBOperations.Should().Be(1);
    // The partial egress should be captured from the stream's position.
    ex.Which.PartialMetrics.EgressBytes.Should().Be(256);
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
    // Delete is a free operation, so the failed attempt should not count as a Class A op.
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(0);
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
    // DeleteObjects is considered a free operation.
    result.ClassAOperations.Should().Be(0);
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
    // DeleteObjects is now considered a free operation.
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(0);
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
    // DeleteObjects is considered a free operation.
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(0);
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
    // 2 lists (Class A) + 2 free deletes
    result.ClassAOperations.Should().Be(2);
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
    // The cost of the single failed list attempt should be counted.
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(1);
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
      .ThrowsAsync(new CloudflareR2BatchException<string>("delete failed", ["key-5"], new R2Result(0), new Exception()));


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
      "delete failed", keys.Select(k => k.Key).ToList(), new R2Result(0), new Exception());
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
  public async Task ListPartsAsync_OnFailure_ThrowsWithPartialDataAndCorrectMetrics()
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
    // 1 for the successful call, 1 for the failed attempt.
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(2);
  }

  [Fact]
  public void CreatePresignedPutUrl_OnS3Error_ThrowsCloudflareR2OperationException()
  {
    // Arrange
    var s3Exception = new AmazonS3Exception("Presigning failed");
    var request     = new PresignedPutRequest("key", TimeSpan.FromMinutes(5), 1024, "text/plain");

    // We mock the R2Client itself to override the virtual URL generation method.
    var mockLoggerFactory = new Mock<ILoggerFactory>();
    mockLoggerFactory
      .Setup(f => f.CreateLogger(It.IsAny<string>()))
      .Returns(new Mock<ILogger<R2Client>>().Object);
    var mockS3Client = new Mock<IAmazonS3>();
    var mockR2Client = new Mock<R2Client>(mockLoggerFactory.Object, mockS3Client.Object) { CallBase = true };

    mockR2Client
      .Protected()
      .Setup<string>("GeneratePresignedUrl", ItExpr.IsAny<GetPreSignedUrlRequest>())
      .Throws(s3Exception);

    var sut = mockR2Client.Object;

    // Act
    var action = () => sut.CreatePresignedPutUrl("bucket", request);

    // Assert
    var ex = action.Should().Throw<CloudflareR2OperationException>().Which;
    ex.InnerException.Should().Be(s3Exception);
    ex.Message.Should().Be("Failed to generate presigned PUT URL.");
  }

  [Fact]
  public void CreatePresignedUploadPartUrl_OnS3Error_ThrowsCloudflareR2OperationException()
  {
    // Arrange
    var s3Exception = new AmazonS3Exception("Presigning failed");
    var request     = new PresignedUploadPartRequest("key", "upload-id", 1, TimeSpan.FromMinutes(5), 1024, "application/octet-stream");

    // We mock the R2Client itself to override the virtual URL generation method.
    var mockLoggerFactory = new Mock<ILoggerFactory>();
    mockLoggerFactory
      .Setup(f => f.CreateLogger(It.IsAny<string>()))
      .Returns(new Mock<ILogger<R2Client>>().Object);
    var mockS3Client = new Mock<IAmazonS3>();
    var mockR2Client = new Mock<R2Client>(mockLoggerFactory.Object, mockS3Client.Object) { CallBase = true };

    mockR2Client
      .Protected()
      .Setup<string>("GeneratePresignedUrl", ItExpr.IsAny<GetPreSignedUrlRequest>())
      .Throws(s3Exception);

    var sut = mockR2Client.Object;

    // Act
    var action = () => sut.CreatePresignedUploadPartUrl("bucket", request);

    // Assert
    var ex = action.Should().Throw<CloudflareR2OperationException>().Which;
    ex.InnerException.Should().Be(s3Exception);
    ex.Message.Should().Be("Failed to generate presigned part URL.");
  }

  [Fact]
  public void CreatePresignedUploadPartsUrls_OnS3Error_ThrowsCloudflareR2OperationException()
  {
    // Arrange
    var s3Exception = new AmazonS3Exception("Presigning failed");
    var request = new PresignedUploadPartsRequest(
      "key", "upload-id",
      TimeSpan.FromMinutes(5),
      new Dictionary<int, long> { { 1, 1024 } }
    );

    // We mock the R2Client itself to override the virtual URL generation method.
    var mockLoggerFactory = new Mock<ILoggerFactory>();
    mockLoggerFactory
      .Setup(f => f.CreateLogger(It.IsAny<string>()))
      .Returns(new Mock<ILogger<R2Client>>().Object);
    // The mock S3 client is still needed for the R2Client constructor, but its methods won't be called by the SUT.
    var mockS3Client = new Mock<IAmazonS3>();
    var mockR2Client = new Mock<R2Client>(mockLoggerFactory.Object, mockS3Client.Object) { CallBase = true };

    mockR2Client
      .Protected()
      .Setup<string>("GeneratePresignedUrl", ItExpr.IsAny<GetPreSignedUrlRequest>())
      .Throws(s3Exception);

    var sut = mockR2Client.Object;

    // Act
    var action = () => sut.CreatePresignedUploadPartsUrls("bucket", request);

    // Assert
    var ex = action.Should().Throw<CloudflareR2OperationException>().Which;
    ex.InnerException.Should().Be(s3Exception);
    ex.Message.Should().Be($"Failed to generate one or more presigned part URLs for upload {request.UploadId}.");
  }

  [Fact]
  public async Task UploadSinglePartAsync_WithStreamTooLarge_ThrowsArgumentException()
  {
    // Arrange
    var mockStream = new Mock<Stream>();
    mockStream.Setup(s => s.CanSeek).Returns(true);
    mockStream.Setup(s => s.Length).Returns(R2Client.R2MaxSinglePartUploadSize + 1);

    // Act
    var action = () => _sut.UploadSinglePartAsync("bucket", "key", mockStream.Object);

    // Assert
    await action.Should().ThrowAsync<ArgumentException>()
          .WithMessage("Stream length (* bytes) exceeds the maximum size for a single-part upload (5 GiB).*");
  }

  [Fact]
  public async Task UploadMultipartAsync_WithStreamTooLarge_ThrowsArgumentException()
  {
    // Arrange
    var mockStream = new Mock<Stream>();
    mockStream.Setup(s => s.CanSeek).Returns(true);
    mockStream.Setup(s => s.Length).Returns(R2Client.R2MaxMultipartFileSize + 1);

    // Act
    var action = () => _sut.UploadMultipartAsync("bucket", "key", mockStream.Object, null);

    // Assert
    await action.Should().ThrowAsync<ArgumentException>()
          .WithMessage("Stream length (* bytes) exceeds the maximum R2 object size of 5 TiB.*");
  }

  [Fact]
  public async Task UploadAsync_WithStreamTooLarge_ThrowsArgumentException()
  {
    // Arrange
    var mockStream = new Mock<Stream>();
    mockStream.Setup(s => s.CanSeek).Returns(true);
    mockStream.Setup(s => s.Length).Returns(R2Client.R2MaxMultipartFileSize + 1);

    // Act
    var action = () => _sut.UploadAsync("bucket", "key", mockStream.Object);

    // Assert
    await action.Should().ThrowAsync<ArgumentException>()
          .WithMessage("Stream length (* bytes) exceeds the maximum R2 object size of 5 TiB.*");
  }

  [Fact]
  public async Task UploadAsync_WithNonSeekableStream_BypassesSizeValidation()
  {
    // Arrange
    var mockStream = new Mock<Stream>();
    mockStream.Setup(s => s.CanSeek).Returns(false);
    // Because the stream is non-seekable, the client must attempt a multipart upload.
    // This will fail with a NotSupportedException, which is the expected behavior here.
    // The key is that it should NOT fail with an ArgumentException from a size check.

    // Act
    var action = () => _sut.UploadAsync("bucket", "key", mockStream.Object);

    // Assert
    // The action should throw the exception from the multipart check, not the size validation.
    await action.Should().ThrowAsync<NotSupportedException>();
  }

  #endregion
}
