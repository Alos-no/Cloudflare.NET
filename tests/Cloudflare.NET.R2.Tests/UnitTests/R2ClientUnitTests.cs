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

    // ClearBucketAsync aborts multipart uploads left open in the bucket unless the caller opts out, so it
    // discovers them on every call. Answer that discovery with "no open uploads" by default; the tests that
    // exercise the aborting behaviour override this setup with their own.
    _mockS3Client
      .Setup(c => c.ListMultipartUploadsAsync(It.IsAny<ListMultipartUploadsRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListMultipartUploadsResponse { MultipartUploads = [], IsTruncated = false });
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
  ///   Verifies that if a user requests a part size larger than R2's maximum (5 GiB), the client clamps it down to
  ///   the maximum allowed size.
  /// </summary>
  [Fact]
  public async Task UploadMultipartAsync_WithTooLargePartSize_ClampsToMax()
  {
    // Arrange
    // The file size MUST be larger than the max part size to test clamping.
    // We'll simulate a file that would create one max-sized part and one smaller part.
    var fileSize = R2Client.R2MaxPartSize + 10 * 1024 * 1024; // 5 GiB + 10 MiB

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
  public async Task DeleteObjectsAsync_WhenErrorOccursAcrossMultipleBatches_AggregatesAllFailedKeys()
  {
    // Arrange
    var keys = Enumerable.Range(1, 1500).Select(i => $"key-{i}").ToList();

    var firstResponse  = new DeleteObjectsResponse { DeleteErrors = [new DeleteError { Key = "key-500" }] };
    var secondResponse = new DeleteObjectsResponse { DeleteErrors = [new DeleteError { Key = "key-1200" }] };

    _mockS3Client.SetupSequence(c => c.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(firstResponse)
                 .ReturnsAsync(secondResponse);

    // Act
    var action = () => _sut.DeleteObjectsAsync("bucket", keys, true);

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareR2BatchException<string>>();
    ex.Which.FailedItems.Should().HaveCount(2);
    ex.Which.FailedItems.Should().Contain("key-500");
    ex.Which.FailedItems.Should().Contain("key-1200");
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(0); // Deletes are free
    _mockS3Client.Verify(c => c.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
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
    // 2 object lists + 1 list of open multipart uploads, all Class A; the 2 deletes are free.
    result.ClassAOperations.Should().Be(3);
  }

  [Fact]
  public async Task ClearBucketAsync_WhenListReturnsNullObjectsButIsTruncated_ContinuesAndSucceeds()
  {
    // Arrange
    var page2Keys = Enumerable.Range(1, 5).Select(i => new S3Object { Key = $"key-page2-{i}" }).ToList();

    _mockS3Client.SetupSequence(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                 // First page is empty but indicates more data is available.
                 .ReturnsAsync(new ListObjectsV2Response { S3Objects = null, IsTruncated      = true, NextContinuationToken = "token" })
                 .ReturnsAsync(new ListObjectsV2Response { S3Objects = page2Keys, IsTruncated = false });

    _mockS3Client.Setup(c => c.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new DeleteObjectsResponse { DeleteErrors = [] });

    // Act
    var result = await _sut.ClearBucketAsync("bucket");

    // Assert
    _mockS3Client.Verify(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    _mockS3Client.Verify(c => c.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()),
                         Times.Once);       // Only one batch had keys
    // 2 object lists + 1 list of open multipart uploads, all Class A.
    result.ClassAOperations.Should().Be(3);
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
  public async Task ListObjectsPageAsync_ReturnsOnePageAndItsContinuationToken()
  {
    // Arrange: a truncated page, which is what a caller resuming the walk later receives.
    var objects = Enumerable.Range(1, 3).Select(i => new S3Object { Key = $"key-{i}" }).ToList();

    _mockS3Client
      .Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListObjectsV2Response
      {
        S3Objects             = objects,
        IsTruncated           = true,
        NextContinuationToken = "next-token"
      });

    // Act
    var result = await _sut.ListObjectsPageAsync("bucket", "prefix/", 100, null);

    // Assert: exactly one call, and the page carries the token the caller needs to continue.
    _mockS3Client.Verify(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()),
                         Times.Once);
    result.Data.Objects.Should().HaveCount(3);
    result.Data.IsTruncated.Should().BeTrue();
    result.Data.NextContinuationToken.Should().Be("next-token");
    result.Metrics.ClassAOperations.Should().Be(1);
  }


  [Fact]
  public async Task ListObjectsPageAsync_PassesPrefixAndContinuationTokenToS3()
  {
    // Arrange: capture the request so the test can prove the caller's token reached R2 unchanged.
    ListObjectsV2Request? capturedRequest = null;

    _mockS3Client
      .Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
      .Callback<ListObjectsV2Request, CancellationToken>((r, _) => capturedRequest = r)
      .ReturnsAsync(new ListObjectsV2Response { S3Objects = [], IsTruncated = false });

    // Act
    await _sut.ListObjectsPageAsync("bucket", "prefix/", 250, "resume-here");

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.BucketName.Should().Be("bucket");
    capturedRequest.Prefix.Should().Be("prefix/");
    capturedRequest.ContinuationToken.Should().Be("resume-here");
    capturedRequest.MaxKeys.Should().Be(250);
  }


  [Theory]
  [InlineData(0, 1000)]     // A caller asking for nothing gets the full page rather than an empty one.
  [InlineData(-5, 1000)]    // Negative values are treated the same way.
  [InlineData(5000, 1000)]  // Anything above the S3 ceiling is clamped down to it.
  [InlineData(250, 250)]    // A value inside the range is passed through untouched.
  public async Task ListObjectsPageAsync_ClampsMaxKeysToTheS3PageCeiling(int requestedMaxKeys, int expectedMaxKeys)
  {
    // Arrange
    ListObjectsV2Request? capturedRequest = null;

    _mockS3Client
      .Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
      .Callback<ListObjectsV2Request, CancellationToken>((r, _) => capturedRequest = r)
      .ReturnsAsync(new ListObjectsV2Response { S3Objects = [], IsTruncated = false });

    // Act
    await _sut.ListObjectsPageAsync("bucket", null, requestedMaxKeys, null);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.MaxKeys.Should().Be(expectedMaxKeys);
  }


  [Fact]
  public async Task ListObjectsPageAsync_WhenResponseCarriesNoObjectList_ReturnsEmptyPage()
  {
    // Arrange: R2 omits the object list entirely when the prefix matches nothing.
    _mockS3Client
      .Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListObjectsV2Response { S3Objects = null, IsTruncated = false });

    // Act
    var result = await _sut.ListObjectsPageAsync("bucket", "empty-prefix/", 1000, null);

    // Assert: the caller gets an empty list, never a null reference.
    result.Data.Objects.Should().BeEmpty();
    result.Data.IsTruncated.Should().BeFalse();
    result.Data.NextContinuationToken.Should().BeNull();
  }


  [Fact]
  public async Task ListObjectsPageAsync_OnS3Error_ThrowsCloudflareR2ListException()
  {
    // Arrange
    var s3Exception = new AmazonS3Exception("List failed");

    _mockS3Client
      .Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(s3Exception);

    // Act
    var action = async () => await _sut.ListObjectsPageAsync("bucket", "prefix/", 1000, null);

    // Assert: the failed call is still billed, so its cost is reported to the caller.
    var ex = await action.Should().ThrowAsync<CloudflareR2ListException<S3Object>>();
    ex.Which.InnerException.Should().Be(s3Exception);
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(1);
  }


  [Fact]
  public async Task ListObjectsAsync_WhenProviderReturnsTruncatedWithNoToken_ThrowsToPreventInfiniteLoop()
  {
    // Arrange: R2 claims there is more data while returning no continuation token. Repeating the call would
    // send the identical request and return this very page again, appending its objects on every pass.
    var objects = new List<S3Object> { new() { Key = "key-1" } };

    _mockS3Client
      .SetupSequence(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListObjectsV2Response
      {
        S3Objects             = objects,
        IsTruncated           = true,
        NextContinuationToken = null
      })
      .ThrowsAsync(new AmazonS3Exception("This should not be called."));

    // Act
    var action = async () => await _sut.ListObjectsAsync("bucket", "prefix/");

    // Assert: the objects already fetched are handed back, and only one call was made.
    var ex = await action.Should().ThrowAsync<CloudflareR2ListException<S3Object>>();
    ex.Which.InnerException.Should().BeOfType<InvalidOperationException>();
    ex.Which.Message.Should().Contain("inconsistent pagination response");
    ex.Which.PartialData.Should().HaveCount(1);
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(1);
  }


  [Fact]
  public async Task ListObjectsAsync_WhenAPageFails_ReportsObjectsFromEarlierPages()
  {
    // Arrange: the first page succeeds, the second fails. The caller must still receive the first page's
    // objects, which is the partial listing this method has always promised.
    var page1 = new List<S3Object> { new() { Key = "key-1" }, new() { Key = "key-2" } };
    var s3Exception = new AmazonS3Exception("List failed");

    _mockS3Client
      .SetupSequence(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListObjectsV2Response
      {
        S3Objects             = page1,
        IsTruncated           = true,
        NextContinuationToken = "token"
      })
      .ThrowsAsync(s3Exception);

    // Act
    var action = async () => await _sut.ListObjectsAsync("bucket", "prefix/");

    // Assert: both list attempts are billed, and the AWS SDK failure is preserved as the cause.
    var ex = await action.Should().ThrowAsync<CloudflareR2ListException<S3Object>>();
    ex.Which.PartialData.Should().HaveCount(2);
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(2);
    ex.Which.InnerException.Should().Be(s3Exception);
  }


  [Fact]
  public async Task ClearBucketAsync_AbortsMultipartUploadsLeftOpenInTheBucket()
  {
    // Arrange: an empty bucket that still holds two uploads that were started and never completed. Cloudflare
    // refuses to delete a bucket while those exist, so clearing it must abort them.
    _mockS3Client
      .Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListObjectsV2Response { S3Objects = [], IsTruncated = false });

    _mockS3Client
      .Setup(c => c.ListMultipartUploadsAsync(It.IsAny<ListMultipartUploadsRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListMultipartUploadsResponse
      {
        MultipartUploads = [new MultipartUpload { Key = "a", UploadId = "upload-a" },
                            new MultipartUpload { Key = "b", UploadId = "upload-b" }],
        IsTruncated = false
      });

    var abortedUploadIds = new List<string>();
    _mockS3Client
      .Setup(c => c.AbortMultipartUploadAsync(It.IsAny<AbortMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .Callback<AbortMultipartUploadRequest, CancellationToken>((r, _) => abortedUploadIds.Add(r.UploadId))
      .ReturnsAsync(new AbortMultipartUploadResponse());

    // Act
    var result = await _sut.ClearBucketAsync("bucket");

    // Assert: every discovered upload is aborted, and aborting is free.
    abortedUploadIds.Should().BeEquivalentTo(["upload-a", "upload-b"]);
    // 1 object list + 1 list of open multipart uploads; the 2 aborts cost nothing.
    result.ClassAOperations.Should().Be(2);
  }


  [Fact]
  public async Task ClearBucketAsync_WhenAbortingIsDeclined_LeavesOpenMultipartUploadsAlone()
  {
    // Arrange
    _mockS3Client
      .Setup(c => c.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListObjectsV2Response { S3Objects = [], IsTruncated = false });

    // Act: the caller opts out of the multipart upload cleanup.
    var result = await _sut.ClearBucketAsync("bucket", true, false);

    // Assert: R2 is never asked about open uploads, so no extra Class A operation is billed.
    _mockS3Client.Verify(
      c => c.ListMultipartUploadsAsync(It.IsAny<ListMultipartUploadsRequest>(), It.IsAny<CancellationToken>()),
      Times.Never);
    _mockS3Client.Verify(
      c => c.AbortMultipartUploadAsync(It.IsAny<AbortMultipartUploadRequest>(), It.IsAny<CancellationToken>()),
      Times.Never);
    result.ClassAOperations.Should().Be(1);
  }


  [Fact]
  public async Task ListMultipartUploadsAsync_HandlesPagination()
  {
    // Arrange: a truncated first page that carries both continuation markers, then a final page.
    var page1Uploads = new List<MultipartUpload> { new() { Key = "a", UploadId = "upload-a" } };
    var page2Uploads = new List<MultipartUpload> { new() { Key = "b", UploadId = "upload-b" } };

    _mockS3Client
      .SetupSequence(c => c.ListMultipartUploadsAsync(It.IsAny<ListMultipartUploadsRequest>(),
                                                      It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListMultipartUploadsResponse
      {
        MultipartUploads   = page1Uploads,
        IsTruncated        = true,
        NextKeyMarker      = "a",
        NextUploadIdMarker = "upload-a"
      })
      .ReturnsAsync(new ListMultipartUploadsResponse { MultipartUploads = page2Uploads, IsTruncated = false });

    // Act
    var result = await _sut.ListMultipartUploadsAsync("bucket", "prefix/");

    // Assert: both pages are accumulated, and each listing is one Class A operation.
    result.Data.Should().HaveCount(2);
    result.Metrics.ClassAOperations.Should().Be(2);
    _mockS3Client.Verify(
      c => c.ListMultipartUploadsAsync(It.IsAny<ListMultipartUploadsRequest>(), It.IsAny<CancellationToken>()),
      Times.Exactly(2));
  }


  [Fact]
  public async Task ListMultipartUploadsAsync_WhenProviderReturnsTruncatedWithNoMarkers_ThrowsToPreventInfiniteLoop()
  {
    // Arrange: the provider claims there is more data while returning neither continuation marker, so a second
    // call would re-issue the identical request and return this very page again, for ever.
    var page1Uploads = new List<MultipartUpload> { new() { Key = "a", UploadId = "upload-a" } };

    _mockS3Client
      .SetupSequence(c => c.ListMultipartUploadsAsync(It.IsAny<ListMultipartUploadsRequest>(),
                                                      It.IsAny<CancellationToken>()))
      .ReturnsAsync(new ListMultipartUploadsResponse
      {
        MultipartUploads   = page1Uploads,
        IsTruncated        = true,
        NextKeyMarker      = null,
        NextUploadIdMarker = null
      })
      .ThrowsAsync(new AmazonS3Exception("This should not be called."));

    // Act
    var action = () => _sut.ListMultipartUploadsAsync("bucket", "prefix/");

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareR2ListException<MultipartUpload>>();
    ex.Which.InnerException.Should().BeOfType<InvalidOperationException>();
    ex.Which.Message.Should().Contain("inconsistent pagination response");
    ex.Which.PartialData.Should().HaveCount(1);
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(1); // Only one attempt should be made.
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
  public async Task ListPartsAsync_WhenProviderReturnsTruncatedWithNullMarker_ThrowsToPreventInfiniteLoop()
  {
    // Arrange
    var page1Parts = Enumerable.Range(1, 10).Select(i => new PartDetail { PartNumber = i, ETag = $"etag-{i}" }).ToList();

    _mockS3Client.SetupSequence(c => c.ListPartsAsync(It.IsAny<ListPartsRequest>(), It.IsAny<CancellationToken>()))
                 // First page succeeds but the provider gives an inconsistent response.
                 .ReturnsAsync(new ListPartsResponse { Parts = page1Parts, IsTruncated = true, NextPartNumberMarker = null })
                 .ThrowsAsync(new AmazonS3Exception("This should not be called.")); // The SUT should not make a second call.

    // Act
    var action = () => _sut.ListPartsAsync("bucket", "key", "upload-id");

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareR2ListException<ListedPart>>();
    ex.Which.InnerException.Should().BeOfType<InvalidOperationException>();
    ex.Which.Message.Should().Contain("inconsistent pagination response");
    ex.Which.PartialData.Should().HaveCount(10);
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(1); // Only one attempt should be made.
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
  public void CreatePresignedGetUrl_WithResponseHeaderOverrides_SignsOverridesIntoRequest()
  {
    // Arrange
    var request = new PresignedGetRequest(
      "manifests/manifest-1",
      TimeSpan.FromMinutes(5),
      ResponseContentType: "application/octet-stream",
      ResponseContentDisposition: "attachment; filename=\"manifest-1\"");

    // We mock the R2Client itself to override the virtual URL generation method and capture the SDK request it builds.
    var mockLoggerFactory = new Mock<ILoggerFactory>();
    mockLoggerFactory
      .Setup(f => f.CreateLogger(It.IsAny<string>()))
      .Returns(new Mock<ILogger<R2Client>>().Object);
    var mockS3Client = new Mock<IAmazonS3>();
    var mockR2Client = new Mock<R2Client>(mockLoggerFactory.Object, mockS3Client.Object) { CallBase = true };

    GetPreSignedUrlRequest? capturedRequest = null;
    mockR2Client
      .Protected()
      .Setup<string>("GeneratePresignedUrl", ItExpr.IsAny<GetPreSignedUrlRequest>())
      .Callback<GetPreSignedUrlRequest>(r => capturedRequest = r)
      .Returns("https://example.com/presigned-get");

    var sut = mockR2Client.Object;

    // Act
    var url = sut.CreatePresignedGetUrl("bucket", request);

    // Assert
    url.Should().Be("https://example.com/presigned-get");
    capturedRequest.Should().NotBeNull();
    capturedRequest!.BucketName.Should().Be("bucket");
    capturedRequest.Key.Should().Be("manifests/manifest-1");
    capturedRequest.Verb.Should().Be(HttpVerb.GET);
    capturedRequest.Expires.Should().BeAfter(DateTime.UtcNow);
    // The overrides are part of the signed query string, so they must land on the SDK request before signing.
    capturedRequest.ResponseHeaderOverrides.ContentType.Should().Be("application/octet-stream");
    capturedRequest.ResponseHeaderOverrides.ContentDisposition.Should().Be("attachment; filename=\"manifest-1\"");
  }

  [Fact]
  public void CreatePresignedGetUrl_WithoutResponseHeaderOverrides_LeavesOverridesUnset()
  {
    // Arrange
    var request = new PresignedGetRequest("key", TimeSpan.FromMinutes(5));

    // We mock the R2Client itself to override the virtual URL generation method and capture the SDK request it builds.
    var mockLoggerFactory = new Mock<ILoggerFactory>();
    mockLoggerFactory
      .Setup(f => f.CreateLogger(It.IsAny<string>()))
      .Returns(new Mock<ILogger<R2Client>>().Object);
    var mockS3Client = new Mock<IAmazonS3>();
    var mockR2Client = new Mock<R2Client>(mockLoggerFactory.Object, mockS3Client.Object) { CallBase = true };

    GetPreSignedUrlRequest? capturedRequest = null;
    mockR2Client
      .Protected()
      .Setup<string>("GeneratePresignedUrl", ItExpr.IsAny<GetPreSignedUrlRequest>())
      .Callback<GetPreSignedUrlRequest>(r => capturedRequest = r)
      .Returns("https://example.com/presigned-get");

    var sut = mockR2Client.Object;

    // Act
    sut.CreatePresignedGetUrl("bucket", request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.ResponseHeaderOverrides.ContentType.Should().BeNull();
    capturedRequest.ResponseHeaderOverrides.ContentDisposition.Should().BeNull();
  }

  [Fact]
  public void CreatePresignedGetUrl_OnS3Error_ThrowsCloudflareR2OperationException()
  {
    // Arrange
    var s3Exception = new AmazonS3Exception("Presigning failed");
    var request     = new PresignedGetRequest("key", TimeSpan.FromMinutes(5));

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
    var action = () => sut.CreatePresignedGetUrl("bucket", request);

    // Assert
    var ex = action.Should().Throw<CloudflareR2OperationException>().Which;
    ex.InnerException.Should().Be(s3Exception);
    ex.Message.Should().Be("Failed to generate presigned GET URL.");
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
  public void CreatePresignedUploadPartUrl_PutsUploadIdAndPartNumberOnTheirDedicatedProperties()
  {
    // Arrange
    // The upload identifier and part number must travel on the request's own UploadId and PartNumber
    // properties, which produce the "uploadId" and "partNumber" query parameters that S3 keys the
    // UploadPart operation on. Routing them through the Parameters collection instead produces
    // "x-uploadId" and "x-partNumber", and R2 answers such a request by writing a plain object over the
    // key rather than recording a part, while still returning 200.
    var request = new PresignedUploadPartRequest("key", "upload-id", 7, TimeSpan.FromMinutes(5), 1024,
                                                 "application/octet-stream");

    var mockLoggerFactory = new Mock<ILoggerFactory>();
    mockLoggerFactory
      .Setup(f => f.CreateLogger(It.IsAny<string>()))
      .Returns(new Mock<ILogger<R2Client>>().Object);
    var mockS3Client = new Mock<IAmazonS3>();
    var mockR2Client = new Mock<R2Client>(mockLoggerFactory.Object, mockS3Client.Object) { CallBase = true };

    GetPreSignedUrlRequest? captured = null;

    mockR2Client
      .Protected()
      .Setup<string>("GeneratePresignedUrl", ItExpr.IsAny<GetPreSignedUrlRequest>())
      .Callback<GetPreSignedUrlRequest>(r => captured = r)
      .Returns("https://example.invalid/signed");

    // Act
    mockR2Client.Object.CreatePresignedUploadPartUrl("bucket", request);

    // Assert
    captured.Should().NotBeNull();
    captured!.UploadId.Should().Be("upload-id");
    captured.PartNumber.Should().Be(7);
    captured.Parameters.Count.Should().Be(0, "routing these through custom parameters would prefix them with \"x-\"");
  }

  [Fact]
  public void CreatePresignedUploadPartsUrls_PutsUploadIdAndEachPartNumberOnTheirDedicatedProperties()
  {
    // Arrange
    // Two parts of the same size, because this method requires every part to match the first part's size.
    const long partSize = 5L * 1024 * 1024;

    var request = new PresignedUploadPartsRequest("key", "upload-id", TimeSpan.FromMinutes(5),
                                                  new Dictionary<int, long>
                                                  {
                                                    [1] = partSize,
                                                    [2] = partSize
                                                  });

    var mockLoggerFactory = new Mock<ILoggerFactory>();
    mockLoggerFactory
      .Setup(f => f.CreateLogger(It.IsAny<string>()))
      .Returns(new Mock<ILogger<R2Client>>().Object);
    var mockS3Client = new Mock<IAmazonS3>();
    var mockR2Client = new Mock<R2Client>(mockLoggerFactory.Object, mockS3Client.Object) { CallBase = true };

    // The values are read at call time rather than the captured object being kept, so the assertion holds
    // whether the implementation builds one request object per part or reuses one across the loop.
    var seenUploadIds   = new List<string?>();
    var seenPartNumbers = new List<int?>();
    var seenParameters  = new List<int>();

    mockR2Client
      .Protected()
      .Setup<string>("GeneratePresignedUrl", ItExpr.IsAny<GetPreSignedUrlRequest>())
      .Callback<GetPreSignedUrlRequest>(r =>
      {
        seenUploadIds.Add(r.UploadId);
        seenPartNumbers.Add(r.PartNumber);
        seenParameters.Add(r.Parameters.Count);
      })
      .Returns("https://example.invalid/signed");

    // Act
    var urls = mockR2Client.Object.CreatePresignedUploadPartsUrls("bucket", request);

    // Assert
    urls.Should().HaveCount(2);
    seenUploadIds.Should().AllBe("upload-id");
    seenPartNumbers.Should().BeEquivalentTo(new int?[] { 1, 2 });
    seenParameters.Should().AllBeEquivalentTo(0, "routing these through custom parameters would prefix them with \"x-\"");
  }

  [Fact]
  public void CreatePresignedUploadPartsUrls_OnS3Error_ThrowsCloudflareR2OperationException()
  {
    // Arrange
    var s3Exception = new AmazonS3Exception("Presigning failed");
    var request = new PresignedUploadPartsRequest(
      "key", "upload-id",
      TimeSpan.FromMinutes(5),
      // The part size must be valid to pass pre-flight checks and reach the mocked method.
      new Dictionary<int, long> { { 1, R2Client.R2MinPartSize } }
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

  [Theory]
  [InlineData(new[] { R2Client.R2MinPartSize - 1 })]                                                 // Below min
  [InlineData(new[] { R2Client.R2MaxPartSize + 1 })]                                                 // Above max
  [InlineData(new[] { R2Client.R2MinPartSize, R2Client.R2MinPartSize + 1, R2Client.R2MinPartSize })] // Non-uniform
  [InlineData(new[] { R2Client.R2MinPartSize, R2Client.R2MinPartSize, R2Client.R2MinPartSize + 1 })] // Last part larger
  public void CreatePresignedUploadPartsUrls_WithInvalidPartSizesForR2_ThrowsArgumentException(long[] partSizes)
  {
    // Arrange
    var partDict = partSizes.Select((size, index) => new { Key = index + 1, Value = size })
                            .ToDictionary(p => p.Key, p => p.Value);

    var request = new PresignedUploadPartsRequest("key", "upload-id", TimeSpan.FromMinutes(5), partDict);

    // Act
    var action = () => _sut.CreatePresignedUploadPartsUrls("bucket", request);

    // Assert
    action.Should().Throw<ArgumentException>();
  }

  [Theory]
  // All five algorithms are verified by R2 on a single-part PUT, so every one must be signable. The digest
  // header must land on the SDK request before signing so its name enters X-Amz-SignedHeaders and the
  // client is forced to send the digest.
  [InlineData("crc32", 4, "x-amz-checksum-crc32")]
  [InlineData("crc32c", 4, "x-amz-checksum-crc32c")]
  [InlineData("sha1", 20, "x-amz-checksum-sha1")]
  [InlineData("sha256", 32, "x-amz-checksum-sha256")]
  [InlineData("md5", 16, "Content-MD5")]
  public void CreatePresignedPutUrl_WithChecksum_SignsTheAlgorithmDigestHeader(
    string algorithmName,
    int    digestByteLength,
    string expectedHeaderName)
  {
    // Arrange
    R2ChecksumAlgorithm.TryParse(algorithmName, out var algorithm).Should().BeTrue();
    var digest  = Convert.ToBase64String(new byte[digestByteLength]);
    var request = new PresignedPutRequest("key", TimeSpan.FromMinutes(5), 1024, "text/plain",
                                          Checksum: new UploadChecksum(algorithm, digest));

    // We mock the R2Client itself to override the virtual URL generation method and capture the SDK request it builds.
    var mockLoggerFactory = new Mock<ILoggerFactory>();
    mockLoggerFactory
      .Setup(f => f.CreateLogger(It.IsAny<string>()))
      .Returns(new Mock<ILogger<R2Client>>().Object);
    var mockS3Client = new Mock<IAmazonS3>();
    var mockR2Client = new Mock<R2Client>(mockLoggerFactory.Object, mockS3Client.Object) { CallBase = true };

    GetPreSignedUrlRequest? captured = null;

    mockR2Client
      .Protected()
      .Setup<string>("GeneratePresignedUrl", ItExpr.IsAny<GetPreSignedUrlRequest>())
      .Callback<GetPreSignedUrlRequest>(r => captured = r)
      .Returns("https://example.invalid/signed");

    // Act
    mockR2Client.Object.CreatePresignedPutUrl("bucket", request);

    // Assert
    captured.Should().NotBeNull();
    captured!.Headers[expectedHeaderName].Should().Be(digest);
  }

  [Fact]
  public void CreatePresignedPutUrl_WithChecksumCollidingWithHeadersToSign_TypedChecksumWins()
  {
    // Arrange
    // The caller signs a stale digest through the free-form HeadersToSign dictionary AND supplies a
    // validated typed checksum for the same header. The typed value must win, because it is the one the
    // library validated for base64 shape and digest length.
    var validatedDigest = Convert.ToBase64String(new byte[32]);
    var request = new PresignedPutRequest("key", TimeSpan.FromMinutes(5), 1024, "text/plain",
                                          HeadersToSign: new Dictionary<string, string>
                                          {
                                            ["x-amz-checksum-sha256"] = "stale-value-from-free-form-headers"
                                          },
                                          Checksum: new UploadChecksum(R2ChecksumAlgorithm.Sha256, validatedDigest));

    var mockLoggerFactory = new Mock<ILoggerFactory>();
    mockLoggerFactory
      .Setup(f => f.CreateLogger(It.IsAny<string>()))
      .Returns(new Mock<ILogger<R2Client>>().Object);
    var mockS3Client = new Mock<IAmazonS3>();
    var mockR2Client = new Mock<R2Client>(mockLoggerFactory.Object, mockS3Client.Object) { CallBase = true };

    GetPreSignedUrlRequest? captured = null;

    mockR2Client
      .Protected()
      .Setup<string>("GeneratePresignedUrl", ItExpr.IsAny<GetPreSignedUrlRequest>())
      .Callback<GetPreSignedUrlRequest>(r => captured = r)
      .Returns("https://example.invalid/signed");

    // Act
    mockR2Client.Object.CreatePresignedPutUrl("bucket", request);

    // Assert
    captured.Should().NotBeNull();
    captured!.Headers["x-amz-checksum-sha256"].Should().Be(validatedDigest);
  }

  [Theory]
  // R2 verifies crc32, crc32c and md5 on multipart part uploads, so these three must be signable on a
  // single part URL.
  [InlineData("crc32", 4, "x-amz-checksum-crc32")]
  [InlineData("crc32c", 4, "x-amz-checksum-crc32c")]
  [InlineData("md5", 16, "Content-MD5")]
  public void CreatePresignedUploadPartUrl_WithPartSupportedChecksum_SignsTheAlgorithmDigestHeader(
    string algorithmName,
    int    digestByteLength,
    string expectedHeaderName)
  {
    // Arrange
    R2ChecksumAlgorithm.TryParse(algorithmName, out var algorithm).Should().BeTrue();
    var digest = Convert.ToBase64String(new byte[digestByteLength]);
    var request = new PresignedUploadPartRequest("key", "upload-id", 1, TimeSpan.FromMinutes(5), 1024,
                                                 "application/octet-stream",
                                                 Checksum: new UploadChecksum(algorithm, digest));

    var mockLoggerFactory = new Mock<ILoggerFactory>();
    mockLoggerFactory
      .Setup(f => f.CreateLogger(It.IsAny<string>()))
      .Returns(new Mock<ILogger<R2Client>>().Object);
    var mockS3Client = new Mock<IAmazonS3>();
    var mockR2Client = new Mock<R2Client>(mockLoggerFactory.Object, mockS3Client.Object) { CallBase = true };

    GetPreSignedUrlRequest? captured = null;

    mockR2Client
      .Protected()
      .Setup<string>("GeneratePresignedUrl", ItExpr.IsAny<GetPreSignedUrlRequest>())
      .Callback<GetPreSignedUrlRequest>(r => captured = r)
      .Returns("https://example.invalid/signed");

    // Act
    mockR2Client.Object.CreatePresignedUploadPartUrl("bucket", request);

    // Assert
    captured.Should().NotBeNull();
    captured!.Headers[expectedHeaderName].Should().Be(digest);
  }

  [Theory]
  // R2 answers 501 NotImplemented to a part upload carrying a SHA-1 or SHA-256 checksum header, whatever
  // the digest's value (verified against live R2, 2026-08-24). Signing such a URL would only manufacture
  // a guaranteed failure for the client holding it, so URL generation refuses up front.
  [InlineData("sha1", 20)]
  [InlineData("sha256", 32)]
  public void CreatePresignedUploadPartUrl_WithShaChecksum_ThrowsArgumentException(string algorithmName, int digestByteLength)
  {
    // Arrange
    R2ChecksumAlgorithm.TryParse(algorithmName, out var algorithm).Should().BeTrue();
    var request = new PresignedUploadPartRequest("key", "upload-id", 1, TimeSpan.FromMinutes(5), 1024,
                                                 "application/octet-stream",
                                                 Checksum: new UploadChecksum(
                                                   algorithm, Convert.ToBase64String(new byte[digestByteLength])));

    // Act
    var action = () => _sut.CreatePresignedUploadPartUrl("bucket", request);

    // Assert
    action.Should().Throw<ArgumentException>().WithMessage("*501 NotImplemented*");
  }

  [Fact]
  public void CreatePresignedUploadPartsUrls_WithPerPartChecksums_SignsEachPartsOwnDigestOnly()
  {
    // Arrange
    // Three parts; part 1 carries a crc32 digest, part 3 an md5 digest, part 2 nothing. Part 2's URL must
    // sign neither digest header: a leaked header would enter X-Amz-SignedHeaders and force the client to
    // send another part's digest, which R2 would then reject.
    const long partSize = 5L * 1024 * 1024;

    var crc32Digest = Convert.ToBase64String(new byte[] { 0x01, 0x02, 0x03, 0x04 });
    var md5Digest   = Convert.ToBase64String(new byte[16]);

    var request = new PresignedUploadPartsRequest("key", "upload-id", TimeSpan.FromMinutes(5),
                                                  new Dictionary<int, long>
                                                  {
                                                    [1] = partSize,
                                                    [2] = partSize,
                                                    [3] = partSize
                                                  },
                                                  ChecksumsByPartNumber: new Dictionary<int, UploadChecksum>
                                                  {
                                                    [1] = new(R2ChecksumAlgorithm.Crc32, crc32Digest),
                                                    [3] = new(R2ChecksumAlgorithm.Md5, md5Digest)
                                                  });

    var mockLoggerFactory = new Mock<ILoggerFactory>();
    mockLoggerFactory
      .Setup(f => f.CreateLogger(It.IsAny<string>()))
      .Returns(new Mock<ILogger<R2Client>>().Object);
    var mockS3Client = new Mock<IAmazonS3>();
    var mockR2Client = new Mock<R2Client>(mockLoggerFactory.Object, mockS3Client.Object) { CallBase = true };

    // Snapshot each part's digest headers at call time, keyed by the part number the request carried.
    var digestHeadersByPart = new Dictionary<int, Dictionary<string, string>>();

    mockR2Client
      .Protected()
      .Setup<string>("GeneratePresignedUrl", ItExpr.IsAny<GetPreSignedUrlRequest>())
      .Callback<GetPreSignedUrlRequest>(r =>
      {
        var digestHeaders = new Dictionary<string, string>();

        foreach (var headerName in new[] { "x-amz-checksum-crc32", "Content-MD5" })
          if (r.Headers.Keys.Contains(headerName))
            digestHeaders[headerName] = r.Headers[headerName];

        digestHeadersByPart[r.PartNumber!.Value] = digestHeaders;
      })
      .Returns("https://example.invalid/signed");

    // Act
    var urls = mockR2Client.Object.CreatePresignedUploadPartsUrls("bucket", request);

    // Assert
    urls.Should().HaveCount(3);
    digestHeadersByPart[1].Should().Equal(new Dictionary<string, string> { ["x-amz-checksum-crc32"] = crc32Digest });
    digestHeadersByPart[2].Should().BeEmpty("a part without a checksum must not inherit another part's digest header");
    digestHeadersByPart[3].Should().Equal(new Dictionary<string, string> { ["Content-MD5"] = md5Digest });
  }

  [Fact]
  public void CreatePresignedUploadPartsUrls_WithChecksumForUnknownPartNumber_ThrowsArgumentException()
  {
    // Arrange
    // The checksum dictionary names part 2, but the request only defines part 1. A silently ignored digest
    // would leave the caller believing part 2's bytes were bound when nothing was signed, so the whole
    // call must fail before any URL is generated.
    var request = new PresignedUploadPartsRequest("key", "upload-id", TimeSpan.FromMinutes(5),
                                                  new Dictionary<int, long> { [1] = R2Client.R2MinPartSize },
                                                  ChecksumsByPartNumber: new Dictionary<int, UploadChecksum>
                                                  {
                                                    [2] = new(R2ChecksumAlgorithm.Crc32,
                                                              Convert.ToBase64String(new byte[4]))
                                                  });

    // Act
    var action = () => _sut.CreatePresignedUploadPartsUrls("bucket", request);

    // Assert
    action.Should().Throw<ArgumentException>().WithMessage("*no such part*");
  }

  [Fact]
  public void CreatePresignedUploadPartsUrls_WithShaChecksumForAPart_ThrowsArgumentException()
  {
    // Arrange
    // The same refusal as the single part URL method: R2 answers 501 NotImplemented to a part upload
    // carrying a SHA-256 checksum header, so the batch method refuses before signing anything.
    var request = new PresignedUploadPartsRequest("key", "upload-id", TimeSpan.FromMinutes(5),
                                                  new Dictionary<int, long> { [1] = R2Client.R2MinPartSize },
                                                  ChecksumsByPartNumber: new Dictionary<int, UploadChecksum>
                                                  {
                                                    [1] = new(R2ChecksumAlgorithm.Sha256,
                                                              Convert.ToBase64String(new byte[32]))
                                                  });

    // Act
    var action = () => _sut.CreatePresignedUploadPartsUrls("bucket", request);

    // Assert
    action.Should().Throw<ArgumentException>().WithMessage("*501 NotImplemented*");
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
    // The default part size is 50MB. (5TiB / 50MB) > 10000, so this should fail the part count check.
    mockStream.Setup(s => s.Length).Returns(R2Client.R2MaxMultipartFileSize);

    // Act
    var action = () => _sut.UploadMultipartAsync("bucket", "key", mockStream.Object, null);

    // Assert
    await action.Should().ThrowAsync<ArgumentException>()
                .WithMessage(
                  "The calculated number of parts (*) exceeds the R2 maximum of 10000 parts. Consider increasing the part size.*");
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


  [Fact]
  public async Task InitiateMultipartUploadAsync_WithContentType_SendsItOnTheInitiateRequest()
  {
    // Arrange: capture the request, because the content type S3 records for the assembled object comes
    // from this call and from nowhere else. The parts cannot carry it.
    InitiateMultipartUploadRequest? capturedRequest = null;

    _mockS3Client
      .Setup(c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .Callback<InitiateMultipartUploadRequest, CancellationToken>((r, _) => capturedRequest = r)
      .ReturnsAsync(new InitiateMultipartUploadResponse { UploadId = "upload-id" });

    // Act
    var result = await _sut.InitiateMultipartUploadAsync("bucket", "report.pdf", "application/pdf");

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.BucketName.Should().Be("bucket");
    capturedRequest.Key.Should().Be("report.pdf");
    capturedRequest.ContentType.Should().Be("application/pdf");
    result.Data.Should().Be("upload-id");
    result.Metrics.ClassAOperations.Should().Be(1);
  }


  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task InitiateMultipartUploadAsync_WithNoUsableContentType_LeavesThePropertyUnset(string? contentType)
  {
    // Arrange: an absent or blank content type must leave the property alone so that R2 applies its own
    // default, rather than the client sending an empty Content-Type header.
    InitiateMultipartUploadRequest? capturedRequest = null;

    _mockS3Client
      .Setup(c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .Callback<InitiateMultipartUploadRequest, CancellationToken>((r, _) => capturedRequest = r)
      .ReturnsAsync(new InitiateMultipartUploadResponse { UploadId = "upload-id" });

    // Act
    await _sut.InitiateMultipartUploadAsync("bucket", "key.bin", contentType);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.ContentType.Should().BeNull();
  }


  [Fact]
  public async Task InitiateMultipartUploadAsync_WithoutAContentTypeArgument_LeavesThePropertyUnset()
  {
    // Arrange: this covers the overload that predates the content type, proving it still sends exactly
    // what it always sent.
    InitiateMultipartUploadRequest? capturedRequest = null;

    _mockS3Client
      .Setup(c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .Callback<InitiateMultipartUploadRequest, CancellationToken>((r, _) => capturedRequest = r)
      .ReturnsAsync(new InitiateMultipartUploadResponse { UploadId = "upload-id" });

    // Act
    var result = await _sut.InitiateMultipartUploadAsync("bucket", "key.bin", CancellationToken.None);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.BucketName.Should().Be("bucket");
    capturedRequest.Key.Should().Be("key.bin");
    capturedRequest.ContentType.Should().BeNull();
    result.Data.Should().Be("upload-id");
  }


  [Fact]
  public async Task InitiateMultipartUploadAsync_WithContentType_OnS3Error_ThrowsCloudflareR2OperationException()
  {
    // Arrange
    _mockS3Client
      .Setup(c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new AmazonS3Exception("Access Denied"));

    // Act
    var action = async () => await _sut.InitiateMultipartUploadAsync("bucket", "key.bin", "application/pdf");

    // Assert: the failed call is still billed, so its cost is reported to the caller.
    var ex = await action.Should().ThrowAsync<CloudflareR2OperationException>();
    ex.Which.PartialMetrics.ClassAOperations.Should().Be(1);
    ex.Which.InnerException.Should().BeOfType<AmazonS3Exception>();
  }

  [Fact]
  public async Task UploadSinglePartAsync_WithContentType_SetsItOnThePutObjectRequest()
  {
    // Arrange
    using var stream = new MemoryStream(new byte[1024]);

    PutObjectRequest? captured = null;
    SetupPutObjectCapture(r => captured = r);

    // Act
    await _sut.UploadSinglePartAsync("bucket", "thumb.webp", stream, "image/webp");

    // Assert
    captured.Should().NotBeNull();
    captured!.ContentType.Should().Be("image/webp");
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task UploadSinglePartAsync_WithNoUsableContentType_LeavesThePutObjectRequestTypeUnset(string? contentType)
  {
    // Arrange: an absent or blank content type must leave the property alone so that R2 applies its own
    // default, rather than the client sending an empty Content-Type header.
    using var stream = new MemoryStream(new byte[1024]);

    PutObjectRequest? captured = null;
    SetupPutObjectCapture(r => captured = r);

    // Act
    await _sut.UploadSinglePartAsync("bucket", "key.bin", stream, contentType);

    // Assert
    captured.Should().NotBeNull();
    captured!.ContentType.Should().BeNull();
  }

  [Fact]
  public async Task UploadSinglePartAsync_WithoutContentTypeArgument_SendsWhatItAlwaysSent()
  {
    // Arrange: this covers the overload that predates the content type and checksum, proving it still
    // sends exactly what it always sent: no content type and no digest property.
    using var stream = new MemoryStream(new byte[1024]);

    PutObjectRequest? captured = null;
    SetupPutObjectCapture(r => captured = r);

    // Act
    await _sut.UploadSinglePartAsync("bucket", "key.bin", stream);

    // Assert
    captured.Should().NotBeNull();
    captured!.ContentType.Should().BeNull();
    captured.ChecksumCRC32.Should().BeNull();
    captured.ChecksumCRC32C.Should().BeNull();
    captured.ChecksumSHA1.Should().BeNull();
    captured.ChecksumSHA256.Should().BeNull();
    captured.MD5Digest.Should().BeNull();
    captured.Headers.CacheControl.Should().BeNull();
  }

  [Fact]
  public async Task UploadSinglePartAsync_WithCacheControl_SetsItOnThePutObjectRequest()
  {
    // Arrange
    using var stream = new MemoryStream(new byte[1024]);

    PutObjectRequest? captured = null;
    SetupPutObjectCapture(r => captured = r);

    // Act
    await _sut.UploadSinglePartAsync("bucket", "config.json", stream, "application/json",
                                     cacheControl: "public, max-age=3600");

    // Assert
    captured.Should().NotBeNull();
    captured!.Headers.CacheControl.Should().Be("public, max-age=3600");
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public async Task UploadSinglePartAsync_WithNoUsableCacheControl_LeavesTheHeaderUnset(string? cacheControl)
  {
    // Arrange: an absent or blank Cache-Control must leave the header alone, so the request goes out
    // exactly as it did before the parameter existed.
    using var stream = new MemoryStream(new byte[1024]);

    PutObjectRequest? captured = null;
    SetupPutObjectCapture(r => captured = r);

    // Act
    await _sut.UploadSinglePartAsync("bucket", "key.bin", stream, null, cacheControl: cacheControl);

    // Assert
    captured.Should().NotBeNull();
    captured!.Headers.CacheControl.Should().BeNull();
  }

  [Fact]
  public async Task UploadMultipartAsync_WithCacheControl_SetsItOnTheInitiateRequest()
  {
    // Arrange: like the content type, the assembled object's Cache-Control comes from the initiate call
    // and from nowhere else, so the multipart overload must place its value there.
    using var stream = new MemoryStream(new byte[60 * 1024 * 1024]);

    InitiateMultipartUploadRequest? capturedInitiate = null;
    SetupSuccessfulMultipart(r => capturedInitiate = r);

    // Act
    await _sut.UploadMultipartAsync("bucket", "large.bin", stream, null, null, "public, max-age=3600");

    // Assert
    capturedInitiate.Should().NotBeNull();
    capturedInitiate!.Headers.CacheControl.Should().Be("public, max-age=3600");
  }

  [Fact]
  public async Task InitiateMultipartUploadAsync_WithCacheControl_SendsItOnTheInitiateRequest()
  {
    // Arrange
    InitiateMultipartUploadRequest? capturedRequest = null;

    _mockS3Client
      .Setup(c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .Callback<InitiateMultipartUploadRequest, CancellationToken>((r, _) => capturedRequest = r)
      .ReturnsAsync(new InitiateMultipartUploadResponse { UploadId = "upload-id" });

    // Act
    await _sut.InitiateMultipartUploadAsync("bucket", "config.json", "application/json", "public, max-age=3600");

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.ContentType.Should().Be("application/json");
    capturedRequest.Headers.CacheControl.Should().Be("public, max-age=3600");
  }

  [Theory]
  [InlineData(null)]
  [InlineData("   ")]
  public async Task InitiateMultipartUploadAsync_WithNoUsableCacheControl_LeavesTheHeaderUnset(string? cacheControl)
  {
    // Arrange
    InitiateMultipartUploadRequest? capturedRequest = null;

    _mockS3Client
      .Setup(c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .Callback<InitiateMultipartUploadRequest, CancellationToken>((r, _) => capturedRequest = r)
      .ReturnsAsync(new InitiateMultipartUploadResponse { UploadId = "upload-id" });

    // Act
    await _sut.InitiateMultipartUploadAsync("bucket", "key.bin", null, cacheControl);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Headers.CacheControl.Should().BeNull();
  }

  [Fact]
  public async Task UploadAsync_WithSmallStream_ForwardsCacheControlToTheSinglePartPut()
  {
    // Arrange: a stream under the 50 MiB threshold takes the single PUT branch, so the value must land
    // on the PutObjectRequest.
    using var stream = new MemoryStream(new byte[1024]);

    PutObjectRequest? captured = null;
    SetupPutObjectCapture(r => captured = r);

    // Act
    await _sut.UploadAsync("bucket", "config.json", stream, null, "application/json",
                           cacheControl: "public, max-age=3600");

    // Assert
    captured.Should().NotBeNull();
    captured!.Headers.CacheControl.Should().Be("public, max-age=3600");
  }

  [Fact]
  public async Task UploadAsync_WithLargeStream_ForwardsCacheControlToTheInitiateRequest()
  {
    // Arrange: a stream over the 50 MiB threshold takes the multipart branch, so the value must land on
    // the initiate request.
    using var stream = new MemoryStream(new byte[60 * 1024 * 1024]);

    InitiateMultipartUploadRequest? capturedInitiate = null;
    SetupSuccessfulMultipart(r => capturedInitiate = r);

    // Act
    await _sut.UploadAsync("bucket", "large.bin", stream, null, null, cacheControl: "public, max-age=3600");

    // Assert
    capturedInitiate.Should().NotBeNull();
    capturedInitiate!.Headers.CacheControl.Should().Be("public, max-age=3600");
  }

  [Fact]
  public void CreatePresignedPutUrl_WithCacheControl_SignsTheHeader()
  {
    // Arrange: the header must land on the SDK request before signing, so its name enters
    // X-Amz-SignedHeaders and the client is forced to send exactly this value.
    var request = new PresignedPutRequest("config.json", TimeSpan.FromMinutes(5), 1024, "application/json",
                                          CacheControl: "public, max-age=3600");

    var mockLoggerFactory = new Mock<ILoggerFactory>();
    mockLoggerFactory
      .Setup(f => f.CreateLogger(It.IsAny<string>()))
      .Returns(new Mock<ILogger<R2Client>>().Object);
    var mockS3Client = new Mock<IAmazonS3>();
    var mockR2Client = new Mock<R2Client>(mockLoggerFactory.Object, mockS3Client.Object) { CallBase = true };

    GetPreSignedUrlRequest? captured = null;

    mockR2Client
      .Protected()
      .Setup<string>("GeneratePresignedUrl", ItExpr.IsAny<GetPreSignedUrlRequest>())
      .Callback<GetPreSignedUrlRequest>(r => captured = r)
      .Returns("https://example.invalid/signed");

    // Act
    mockR2Client.Object.CreatePresignedPutUrl("bucket", request);

    // Assert
    captured.Should().NotBeNull();
    captured!.Headers.CacheControl.Should().Be("public, max-age=3600");
  }

  [Fact]
  public void CreatePresignedPutUrl_WithCacheControlCollidingWithHeadersToSign_TypedValueWins()
  {
    // Arrange: the caller signs a stale value through the free-form HeadersToSign dictionary AND supplies
    // the typed parameter for the same header. The typed value must win, matching the checksum rule.
    var request = new PresignedPutRequest("config.json", TimeSpan.FromMinutes(5), 1024, "application/json",
                                          HeadersToSign: new Dictionary<string, string>
                                          {
                                            ["Cache-Control"] = "no-store"
                                          },
                                          CacheControl: "public, max-age=3600");

    var mockLoggerFactory = new Mock<ILoggerFactory>();
    mockLoggerFactory
      .Setup(f => f.CreateLogger(It.IsAny<string>()))
      .Returns(new Mock<ILogger<R2Client>>().Object);
    var mockS3Client = new Mock<IAmazonS3>();
    var mockR2Client = new Mock<R2Client>(mockLoggerFactory.Object, mockS3Client.Object) { CallBase = true };

    GetPreSignedUrlRequest? captured = null;

    mockR2Client
      .Protected()
      .Setup<string>("GeneratePresignedUrl", ItExpr.IsAny<GetPreSignedUrlRequest>())
      .Callback<GetPreSignedUrlRequest>(r => captured = r)
      .Returns("https://example.invalid/signed");

    // Act
    mockR2Client.Object.CreatePresignedPutUrl("bucket", request);

    // Assert
    captured.Should().NotBeNull();
    captured!.Headers.CacheControl.Should().Be("public, max-age=3600");
  }

  [Fact]
  public void CreatePresignedPutUrl_WithoutCacheControl_LeavesTheHeaderUnsigned()
  {
    // Arrange: a request without the parameter must not sign the header, so clients that send no
    // Cache-Control keep working.
    var request = new PresignedPutRequest("key.bin", TimeSpan.FromMinutes(5), 1024, "application/octet-stream");

    var mockLoggerFactory = new Mock<ILoggerFactory>();
    mockLoggerFactory
      .Setup(f => f.CreateLogger(It.IsAny<string>()))
      .Returns(new Mock<ILogger<R2Client>>().Object);
    var mockS3Client = new Mock<IAmazonS3>();
    var mockR2Client = new Mock<R2Client>(mockLoggerFactory.Object, mockS3Client.Object) { CallBase = true };

    GetPreSignedUrlRequest? captured = null;

    mockR2Client
      .Protected()
      .Setup<string>("GeneratePresignedUrl", ItExpr.IsAny<GetPreSignedUrlRequest>())
      .Callback<GetPreSignedUrlRequest>(r => captured = r)
      .Returns("https://example.invalid/signed");

    // Act
    mockR2Client.Object.CreatePresignedPutUrl("bucket", request);

    // Assert
    captured.Should().NotBeNull();
    captured!.Headers.CacheControl.Should().BeNull();
  }

  [Theory]
  // Every admitted algorithm maps to its own PutObjectRequest property; the SDK sends each property in
  // that algorithm's header. Exactly one property must carry the digest and the other four stay unset.
  [InlineData("crc32", 4)]
  [InlineData("crc32c", 4)]
  [InlineData("sha1", 20)]
  [InlineData("sha256", 32)]
  [InlineData("md5", 16)]
  public async Task UploadSinglePartAsync_WithChecksum_SetsTheMatchingPutObjectRequestProperty(
    string algorithmName,
    int    digestByteLength)
  {
    // Arrange
    R2ChecksumAlgorithm.TryParse(algorithmName, out var algorithm).Should().BeTrue();
    var       digest = Convert.ToBase64String(new byte[digestByteLength]);
    using var stream = new MemoryStream(new byte[1024]);

    PutObjectRequest? captured = null;
    SetupPutObjectCapture(r => captured = r);

    // Act
    await _sut.UploadSinglePartAsync("bucket", "key.bin", stream, "application/octet-stream",
                                     new UploadChecksum(algorithm, digest));

    // Assert
    captured.Should().NotBeNull();

    var digestProperties = new Dictionary<string, string?>
    {
      ["crc32"]  = captured!.ChecksumCRC32,
      ["crc32c"] = captured.ChecksumCRC32C,
      ["sha1"]   = captured.ChecksumSHA1,
      ["sha256"] = captured.ChecksumSHA256,
      ["md5"]    = captured.MD5Digest
    };

    digestProperties[algorithmName].Should().Be(digest);
    digestProperties.Where(p => p.Key != algorithmName).Should().OnlyContain(p => p.Value == null);
  }

  [Fact]
  public async Task UploadMultipartAsync_WithContentType_SetsItOnTheInitiateRequest()
  {
    // Arrange: the content type S3 records for the assembled object comes from the initiate call and from
    // nowhere else, so the multipart overload must place its value there.
    using var stream = new MemoryStream(new byte[60 * 1024 * 1024]);

    InitiateMultipartUploadRequest? capturedInitiate = null;
    SetupSuccessfulMultipart(r => capturedInitiate = r);

    // Act
    await _sut.UploadMultipartAsync("bucket", "large.webp", stream, null, "image/webp");

    // Assert
    capturedInitiate.Should().NotBeNull();
    capturedInitiate!.ContentType.Should().Be("image/webp");
  }

  [Theory]
  [InlineData(null)]
  [InlineData("   ")]
  public async Task UploadMultipartAsync_WithNoUsableContentType_LeavesTheInitiateRequestTypeUnset(string? contentType)
  {
    // Arrange
    using var stream = new MemoryStream(new byte[60 * 1024 * 1024]);

    InitiateMultipartUploadRequest? capturedInitiate = null;
    SetupSuccessfulMultipart(r => capturedInitiate = r);

    // Act
    await _sut.UploadMultipartAsync("bucket", "key.bin", stream, null, contentType);

    // Assert
    capturedInitiate.Should().NotBeNull();
    capturedInitiate!.ContentType.Should().BeNull();
  }

  [Fact]
  public async Task UploadMultipartAsync_WithoutContentTypeArgument_LeavesTheInitiateRequestTypeUnset()
  {
    // Arrange: this covers the overload that predates the content type, proving it still sends exactly
    // what it always sent.
    using var stream = new MemoryStream(new byte[60 * 1024 * 1024]);

    InitiateMultipartUploadRequest? capturedInitiate = null;
    SetupSuccessfulMultipart(r => capturedInitiate = r);

    // Act
    await _sut.UploadMultipartAsync("bucket", "key.bin", stream, null);

    // Assert
    capturedInitiate.Should().NotBeNull();
    capturedInitiate!.ContentType.Should().BeNull();
  }

  [Fact]
  public async Task UploadAsync_WithSmallStream_ForwardsContentTypeAndChecksumToTheSinglePartPut()
  {
    // Arrange: a stream under the 50 MiB threshold takes the single PUT branch, so both values must land
    // on the PutObjectRequest.
    var       payload = new byte[1024];
    using var stream  = new MemoryStream(payload);
    var       digest  = Convert.ToBase64String(new byte[32]);

    PutObjectRequest? captured = null;
    SetupPutObjectCapture(r => captured = r);

    // Act
    await _sut.UploadAsync("bucket", "thumb.webp", stream, null, "image/webp",
                           new UploadChecksum(R2ChecksumAlgorithm.Sha256, digest));

    // Assert
    captured.Should().NotBeNull();
    captured!.ContentType.Should().Be("image/webp");
    captured.ChecksumSHA256.Should().Be(digest);
  }

  [Fact]
  public async Task UploadAsync_WithLargeStream_ForwardsContentTypeToTheInitiateRequest()
  {
    // Arrange: a stream over the 50 MiB threshold takes the multipart branch, so the type must land on
    // the initiate request.
    using var stream = new MemoryStream(new byte[60 * 1024 * 1024]);

    InitiateMultipartUploadRequest? capturedInitiate = null;
    SetupSuccessfulMultipart(r => capturedInitiate = r);

    // Act
    await _sut.UploadAsync("bucket", "large.webp", stream, null, "image/webp");

    // Assert
    capturedInitiate.Should().NotBeNull();
    capturedInitiate!.ContentType.Should().Be("image/webp");
  }

  [Fact]
  public async Task UploadAsync_WithLargeStreamAndChecksum_ThrowsBeforeInitiatingAnything()
  {
    // Arrange: the digest covers the whole object, but a multipart upload is verified per part and the
    // client does not compute per-part digests, so accepting the digest would silently skip the
    // verification the caller asked for. The call must fail before any request is sent.
    using var stream = new MemoryStream(new byte[60 * 1024 * 1024]);

    var checksum = new UploadChecksum(R2ChecksumAlgorithm.Sha256, Convert.ToBase64String(new byte[32]));

    // Act
    var action = () => _sut.UploadAsync("bucket", "key.bin", stream, null, "image/webp", checksum);

    // Assert
    await action.Should().ThrowAsync<ArgumentException>().WithMessage("*single-part upload*");
    _mockS3Client.Verify(
      c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task UploadAsync_WithNonSeekableStreamAndChecksum_ThrowsBeforeInitiatingAnything()
  {
    // Arrange: a non-seekable stream always takes the multipart branch, so a checksum must be refused the
    // same way as for an oversized stream.
    var mockStream = new Mock<Stream>();
    mockStream.Setup(s => s.CanSeek).Returns(false);

    var checksum = new UploadChecksum(R2ChecksumAlgorithm.Crc32, Convert.ToBase64String(new byte[4]));

    // Act
    var action = () => _sut.UploadAsync("bucket", "key.bin", mockStream.Object, null, null, checksum);

    // Assert
    await action.Should().ThrowAsync<ArgumentException>().WithMessage("*single-part upload*");
    _mockS3Client.Verify(
      c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task UploadAsync_WithSmallFile_ForwardsContentTypeAndChecksumToTheSinglePartPut()
  {
    // Arrange
    var filePath = CreateTempFileOfSize(1024);
    var digest   = Convert.ToBase64String(new byte[32]);

    PutObjectRequest? captured = null;
    SetupPutObjectCapture(r => captured = r);

    try
    {
      // Act
      await _sut.UploadAsync("bucket", "thumb.webp", filePath, null, "image/webp",
                             new UploadChecksum(R2ChecksumAlgorithm.Sha256, digest));
    }
    finally
    {
      File.Delete(filePath);
    }

    // Assert
    captured.Should().NotBeNull();
    captured!.ContentType.Should().Be("image/webp");
    captured.ChecksumSHA256.Should().Be(digest);
  }

  [Fact]
  public async Task UploadAsync_WithLargeFile_ForwardsContentTypeToTheInitiateRequest()
  {
    // Arrange: a file over the 50 MiB threshold takes the multipart branch, so the type must land on the
    // initiate request. The file is created by extending its length, so nothing is actually written.
    var filePath = CreateTempFileOfSize(60L * 1024 * 1024);

    InitiateMultipartUploadRequest? capturedInitiate = null;
    SetupSuccessfulMultipart(r => capturedInitiate = r);

    try
    {
      // Act
      await _sut.UploadAsync("bucket", "large.webp", filePath, null, "image/webp");
    }
    finally
    {
      File.Delete(filePath);
    }

    // Assert
    capturedInitiate.Should().NotBeNull();
    capturedInitiate!.ContentType.Should().Be("image/webp");
  }

  [Fact]
  public async Task UploadAsync_WithLargeFileAndChecksum_ThrowsBeforeInitiatingAnything()
  {
    // Arrange
    var filePath = CreateTempFileOfSize(60L * 1024 * 1024);
    var checksum = new UploadChecksum(R2ChecksumAlgorithm.Sha256, Convert.ToBase64String(new byte[32]));

    try
    {
      // Act
      var action = () => _sut.UploadAsync("bucket", "key.bin", filePath, null, "image/webp", checksum);

      // Assert
      await action.Should().ThrowAsync<ArgumentException>().WithMessage("*single-part upload*");
      _mockS3Client.Verify(
        c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()),
        Times.Never);
    }
    finally
    {
      File.Delete(filePath);
    }
  }

  #endregion


  #region Methods - Non-Public

  /// <summary>Captures every <see cref="PutObjectRequest" /> the client sends, answering each with success.</summary>
  /// <param name="onPut">Receives each captured request.</param>
  private void SetupPutObjectCapture(Action<PutObjectRequest> onPut)
  {
    _mockS3Client
      .Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
      .Callback<PutObjectRequest, CancellationToken>((r, _) => onPut(r))
      .ReturnsAsync(new PutObjectResponse());
  }

  /// <summary>
  ///   Mocks a fully successful multipart flow (initiate, every part, complete) and captures the initiate request,
  ///   where the assembled object's content type must travel.
  /// </summary>
  /// <param name="onInitiate">Receives the captured initiate request.</param>
  private void SetupSuccessfulMultipart(Action<InitiateMultipartUploadRequest> onInitiate)
  {
    _mockS3Client
      .Setup(c => c.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .Callback<InitiateMultipartUploadRequest, CancellationToken>((r, _) => onInitiate(r))
      .ReturnsAsync(new InitiateMultipartUploadResponse { UploadId = "upload-id" });

    _mockS3Client
      .Setup(c => c.UploadPartAsync(It.IsAny<UploadPartRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((UploadPartRequest req, CancellationToken _) =>
                      new UploadPartResponse { PartNumber = req.PartNumber, ETag = "etag" });

    _mockS3Client
      .Setup(c => c.CompleteMultipartUploadAsync(It.IsAny<CompleteMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(new CompleteMultipartUploadResponse());
  }

  /// <summary>
  ///   Creates a temporary file of the requested size by extending its length, so a large file costs no
  ///   write time. The caller deletes the file.
  /// </summary>
  /// <param name="sizeInBytes">The size the file reports.</param>
  /// <returns>The path of the created file.</returns>
  private static string CreateTempFileOfSize(long sizeInBytes)
  {
    var path = Path.GetTempFileName();

    using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Write);
    fileStream.SetLength(sizeInBytes);

    return path;
  }

  #endregion
}
