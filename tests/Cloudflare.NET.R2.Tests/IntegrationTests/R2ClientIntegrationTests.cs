// Suppress obsolete warnings for tests that use deprecated methods for bucket management.
// The R2 client tests use the deprecated IAccountsApi methods for bucket lifecycle management.
#pragma warning disable CS0618

namespace Cloudflare.NET.R2.Tests.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using Accounts;
using Amazon.S3;
using Amazon.S3.Model;
using Exceptions;
using Fixtures;
using FluentAssertions;
using Helpers;
using Microsoft.Extensions.DependencyInjection;
using Models;
using NET.Tests.Shared.Fixtures;
using NET.Tests.Shared.Helpers;
using Xunit.Abstractions;

[Trait("Category", TestConstants.TestCategories.Integration)]
public class R2ClientIntegrationTests : IClassFixture<R2ClientTestFixture>, IAsyncLifetime
{
  #region Properties & Fields - Non-Public

  private readonly IR2Client         _sut;
  private readonly IAccountsApi      _accountsApi;
  private readonly IAmazonS3         _s3Client;
  private readonly ITestOutputHelper _output;
  private readonly string            _bucketName = $"cfnet-r2-test-bucket-{Guid.NewGuid():N}";

  #endregion

  #region Constructors

  public R2ClientIntegrationTests(R2ClientTestFixture fixture, ITestOutputHelper output)
  {
    _sut         = fixture.R2Client;
    _accountsApi = fixture.AccountsApi;
    _s3Client    = fixture.S3Client;
    _output      = output;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion

  #region Methods Impl

  public Task InitializeAsync()
  {
    // The R2 bucket is created via the Cloudflare REST API, which is outside the scope of this client.
    // For a fully isolated test, we would use the Cloudflare.NET core client to create the bucket here.
    // For now, we assume the bucket can be created manually or that the test runner has permissions.
    // Let's use the core client to create it.
    return _accountsApi.CreateR2BucketAsync(_bucketName);
  }

  public async Task DisposeAsync()
  {
    // Best-effort cleanup.
    try
    {
      await _sut.ClearBucketAsync(_bucketName, true);
    }
    catch (Exception)
    {
      // ignore failures during cleanup
    }

    try
    {
      await _accountsApi.DeleteR2BucketAsync(_bucketName);
    }
    catch (Exception)
    {
      // ignore failures during cleanup
    }
  }

  #endregion

  #region Methods

  [IntegrationTest]
  public async Task CanPerformFullObjectLifecycle()
  {
    // ARRANGE
    // 1. Create temp files for upload
    using var smallFile    = new TempFile(10 * 1024);       // 10 KB
    using var largeFile    = new TempFile(6 * 1024 * 1024); // 6 MB (for multipart)
    var       smallFileKey = $"lifecycle/small-file-{Guid.NewGuid()}.bin";
    var       largeFileKey = $"lifecycle/large-file-{Guid.NewGuid()}.bin";

    // ACT & ASSERT
    // 2. Upload small file (single part)
    var smallUploadResult = await _sut.UploadAsync(_bucketName, smallFileKey, smallFile.FilePath);
    smallUploadResult.ClassAOperations.Should().Be(1);
    smallUploadResult.IngressBytes.Should().Be(smallFile.FileSize);

    // 3. Upload large file (multipart)
    var largeUploadResult = await _sut.UploadMultipartAsync(_bucketName, largeFileKey, largeFile.FilePath, 5 * 1024 * 1024); // 5MB parts
    largeUploadResult.ClassAOperations.Should().Be(1 + 2 + 1); // Init + 2 Parts + Complete
    largeUploadResult.IngressBytes.Should().Be(largeFile.FileSize);

    // 4. List objects
    var listResult = await _sut.ListObjectsAsync(_bucketName, "lifecycle/");
    listResult.Metrics.ClassAOperations.Should().BeGreaterThanOrEqualTo(1);
    listResult.Data.Should().HaveCount(2);
    // ReSharper disable once AccessToDisposedClosure
    listResult.Data.Should().Contain(o => o.Key == smallFileKey && o.Size == smallFile.FileSize);
    // ReSharper disable once AccessToDisposedClosure
    listResult.Data.Should().Contain(o => o.Key == largeFileKey && o.Size == largeFile.FileSize);

    // 5. Download file
    using var downloadFile   = new TempFile(0);
    var       downloadResult = await _sut.DownloadFileAsync(_bucketName, smallFileKey, downloadFile.FilePath);
    downloadResult.ClassBOperations.Should().Be(1);
    downloadResult.EgressBytes.Should().Be(smallFile.FileSize);
    var downloadedBytes = await File.ReadAllBytesAsync(downloadFile.FilePath);
    var originalBytes   = await File.ReadAllBytesAsync(smallFile.FilePath);
    downloadedBytes.Should().BeEquivalentTo(originalBytes);

    // 6. Delete single object
    var deleteResult = await _sut.DeleteObjectAsync(_bucketName, smallFileKey);
    // DeleteObject is a free operation.
    deleteResult.ClassAOperations.Should().Be(0);

    // 7. Verify deletion
    var listAfterDeleteResult = await _sut.ListObjectsAsync(_bucketName, "lifecycle/");
    listAfterDeleteResult.Data.Should().HaveCount(1);
    listAfterDeleteResult.Data.Should().NotContain(o => o.Key == smallFileKey);
  }

  [IntegrationTest]
  public async Task ListObjectsAsync_WithPrefixAndPagination_ReturnsCorrectSubset()
  {
    // Arrange
    var prefixA = "list-prefix-test/a/";
    var prefixB = "list-prefix-test/b/";
    var keysA   = new List<string>();
    var keysB   = new List<string>();

    // Create 12 objects for prefix A and 8 for prefix B to test prefix filtering.
    // The underlying ListObjectsV2Async handles pagination transparently, so this test
    // implicitly covers it if the number of objects exceeds the page size (default 1000).
    using var tempFile = new TempFile(10);
    for (var i = 0; i < 12; i++)
    {
      var key = $"{prefixA}file-{i}.txt";
      await _sut.UploadAsync(_bucketName, key, tempFile.FilePath);
      keysA.Add(key);
    }

    for (var i = 0; i < 8; i++)
    {
      var key = $"{prefixB}file-{i}.txt";
      await _sut.UploadAsync(_bucketName, key, tempFile.FilePath);
      keysB.Add(key);
    }

    // Act
    var resultA   = await _sut.ListObjectsAsync(_bucketName, prefixA);
    var resultB   = await _sut.ListObjectsAsync(_bucketName, prefixB);
    var resultAll = await _sut.ListObjectsAsync(_bucketName, "list-prefix-test/");

    // Assert
    resultA.Data.Should().HaveCount(12);
    resultA.Data.Select(o => o.Key).Should().BeEquivalentTo(keysA);
    resultB.Data.Should().HaveCount(8);
    resultB.Data.Select(o => o.Key).Should().BeEquivalentTo(keysB);
    resultAll.Data.Should().HaveCount(20);
  }

  [IntegrationTest]
  public async Task UploadAsync_WithZeroByteFile_Succeeds()
  {
    // Arrange
    var       key          = $"zero-byte-file-{Guid.NewGuid()}.txt";
    using var zeroByteFile = new TempFile(0);

    // Act
    var uploadResult = await _sut.UploadAsync(_bucketName, key, zeroByteFile.FilePath);

    // Assert
    uploadResult.ClassAOperations.Should().Be(1);
    uploadResult.IngressBytes.Should().Be(0);

    // Verify by listing
    var listResult = await _sut.ListObjectsAsync(_bucketName, key);
    listResult.Data.Should().ContainSingle().Which.Size.Should().Be(0);

    // Verify by downloading
    using var downloadFile = new TempFile(10); // create with non-zero size
    await _sut.DownloadFileAsync(_bucketName, key, downloadFile.FilePath);
    var fi = new FileInfo(downloadFile.FilePath);
    fi.Length.Should().Be(0);
  }

  [IntegrationTest]
  public async Task UploadAsync_WithSpecialCharactersInKey_Succeeds()
  {
    // Arrange
    // Key with spaces, slashes for directory structure, and other symbols
    var       key      = $"special chars/folder name with spaces/file_name-@!*().txt";
    using var tempFile = new TempFile(100);

    // Act
    var uploadResult = await _sut.UploadAsync(_bucketName, key, tempFile.FilePath);

    // Assert
    uploadResult.ClassAOperations.Should().Be(1);

    // Verify by downloading
    using var downloadFile = new TempFile(0);
    // ReSharper disable once AccessToDisposedClosure
    var downloadAction = () => _sut.DownloadFileAsync(_bucketName, key, downloadFile.FilePath);

    await downloadAction.Should().NotThrowAsync();
    var fi = new FileInfo(downloadFile.FilePath);
    fi.Length.Should().Be(tempFile.FileSize);
  }

  [IntegrationTest]
  public async Task CanDeleteMultipleObjects()
  {
    // Arrange
    var keys = new List<string>();
    for (var i = 0; i < 5; i++)
    {
      var       key  = $"delete-batch-{i}-{Guid.NewGuid()}.txt";
      using var file = new TempFile(10);
      await _sut.UploadAsync(_bucketName, key, file.FilePath);
      keys.Add(key);
    }

    // Act
    var deleteResult = await _sut.DeleteObjectsAsync(_bucketName, keys);

    // Assert
    // DeleteObjects is a free operation.
    deleteResult.ClassAOperations.Should().Be(0);
    var listResult = await _sut.ListObjectsAsync(_bucketName, "delete-batch-");
    listResult.Data.Should().BeEmpty();
  }

  [IntegrationTest]
  public async Task DeleteObjectsAsync_WithNonExistentKeys_SucceedsAndReportsNoErrors()
  {
    // Arrange
    var       realKey1 = $"real-key-1-{Guid.NewGuid()}.txt";
    var       realKey2 = $"real-key-2-{Guid.NewGuid()}.txt";
    var       fakeKey1 = "this-key-does-not-exist.txt";
    var       fakeKey2 = "neither-does-this-one.txt";
    using var tempFile = new TempFile(10);
    await _sut.UploadAsync(_bucketName, realKey1, tempFile.FilePath);
    await _sut.UploadAsync(_bucketName, realKey2, tempFile.FilePath);
    var keysToDelete = new List<string> { realKey1, fakeKey1, realKey2, fakeKey2 };

    // Act
    var deleteAction = () => _sut.DeleteObjectsAsync(_bucketName, keysToDelete);

    // Assert
    // The S3 API for DeleteObjects is idempotent and does not error on non-existent keys.
    await deleteAction.Should().NotThrowAsync();

    // Verify only the real keys were deleted
    var listResult = await _sut.ListObjectsAsync(_bucketName, "real-key-");
    listResult.Data.Should().BeEmpty();
  }

  [IntegrationTest]
  public async Task CanClearBucket()
  {
    // Arrange
    for (var i = 0; i < 15; i++) // Create more than one page of deletions if batched by 10
    {
      var       key  = $"clear-bucket-{i}-{Guid.NewGuid()}.txt";
      using var file = new TempFile(10);
      await _sut.UploadAsync(_bucketName, key, file.FilePath);
    }

    // Act
    var clearResult = await _sut.ClearBucketAsync(_bucketName);

    // Assert
    // Two Class A operations: one page of object listing, plus the ListMultipartUploads call that
    // finds uploads left open. The deletes and the aborts themselves are free.
    clearResult.ClassAOperations.Should().Be(2);
    var listResult = await _sut.ListObjectsAsync(_bucketName, null);
    listResult.Data.Should().BeEmpty();
  }

  [IntegrationTest]
  public async Task UploadAsync_OverwritingExistingKey_Succeeds()
  {
    // Arrange
    var       key    = $"overwrite-{Guid.NewGuid()}.txt";
    using var fileV1 = new TempFile(100);
    using var fileV2 = new TempFile(200);

    // Act
    await _sut.UploadAsync(_bucketName, key, fileV1.FilePath);
    await _sut.UploadAsync(_bucketName, key, fileV2.FilePath);

    // Assert
    var listResult = await _sut.ListObjectsAsync(_bucketName, key);
    listResult.Data.Should().ContainSingle();
    listResult.Data[0].Key.Should().Be(key);
    listResult.Data[0].Size.Should().Be(fileV2.FileSize);

    using var downloadFile = new TempFile(0);

    await _sut.DownloadFileAsync(_bucketName, key, downloadFile.FilePath);
    var downloadedBytes = await File.ReadAllBytesAsync(downloadFile.FilePath);
    var originalBytesV2 = await File.ReadAllBytesAsync(fileV2.FilePath);
    downloadedBytes.Should().BeEquivalentTo(originalBytesV2);
  }

  [IntegrationTest]
  public async Task DeleteObjectAsync_WhenObjectDoesNotExist_SucceedsWithoutError()
  {
    // Arrange
    var key = $"non-existent-key-{Guid.NewGuid()}.txt";

    // Act
    var action = async () => await _sut.DeleteObjectAsync(_bucketName, key);

    // Assert
    await action.Should().NotThrowAsync();
  }

  [IntegrationTest]
  public async Task ClearBucketAsync_OnEmptyBucket_Succeeds()
  {
    // Arrange
    // Bucket is created empty in InitializeAsync

    // Act
    var result = await _sut.ClearBucketAsync(_bucketName);

    // Assert
    // Two Class A operations even on an empty bucket: the object listing still costs one, and the
    // ListMultipartUploads call that looks for uploads left open costs another.
    result.ClassAOperations.Should().Be(2);
    var listResult = await _sut.ListObjectsAsync(_bucketName, null);
    listResult.Data.Should().BeEmpty();
  }

  [IntegrationTest]
  public async Task CompleteMultipartUploadAsync_WithInvalidPart_ThrowsException()
  {
    // Arrange
    var       key            = $"bad-multipart-{Guid.NewGuid()}.bin";
    using var tempFile       = new TempFile(6 * 1024 * 1024); // 6MB
    var       uploadIdResult = await _sut.InitiateMultipartUploadAsync(_bucketName, key);
    var       uploadId       = uploadIdResult.Data;

    try
    {
      // Upload a valid part 1 using the raw S3 client to get a valid ETag
      await using var fileStream = File.OpenRead(tempFile.FilePath);

      var partRequest = new UploadPartRequest
      {
        BucketName                       = _bucketName,
        Key                              = key,
        UploadId                         = uploadId,
        PartNumber                       = 1,
        PartSize                         = 5 * 1024 * 1024,
        InputStream                      = fileStream,
        DisablePayloadSigning            = true,
        DisableDefaultChecksumValidation = true
      };

      await _s3Client.UploadPartAsync(partRequest);

      // Create an invalid PartETag list for the complete call
      var invalidParts = new List<PartETag> { new(1, "\"invalid-etag-intentionally-wrong\"") };

      // Act
      var action = () => _sut.CompleteMultipartUploadAsync(_bucketName, key, uploadId, invalidParts);

      // Assert
      var ex = await action.Should().ThrowAsync<CloudflareR2OperationException>();
      ex.Which.InnerException.Should().BeOfType<AmazonS3Exception>()
        .Which.ErrorCode.Should().Contain("InvalidPart"); // R2/S3 specific error message
    }
    finally
    {
      // Cleanup: Abort the multipart upload regardless of outcome
      await _sut.AbortMultipartUploadAsync(_bucketName, key, uploadId);
    }
  }

  [IntegrationTest]
  public async Task CanGenerateAndUsePresignedPutUrl()
  {
    // Arrange
    var       key         = $"presigned-put-{Guid.NewGuid()}.txt";
    var       contentType = "text/plain";
    using var tempFile    = new TempFile(512);
    var       request     = new PresignedPutRequest(key, TimeSpan.FromMinutes(5), tempFile.FileSize, contentType);

    // Act
    // 1. Generate the presigned URL
    var presignedUrl = _sut.CreatePresignedPutUrl(_bucketName, request);
    presignedUrl.Should().NotBeNullOrEmpty();

    // 2. Use the URL with a standard HttpClient
    using var       httpClient  = new HttpClient();
    await using var fileStream  = File.OpenRead(tempFile.FilePath);
    using var       fileContent = new StreamContent(fileStream);
    fileContent.Headers.ContentType   = new MediaTypeHeaderValue(contentType);
    fileContent.Headers.ContentLength = tempFile.FileSize;
    var httpResponse = await httpClient.PutAsync(presignedUrl, fileContent);

    // Assert
    // 3. Verify the upload was successful
    httpResponse.EnsureSuccessStatusCode();
    var listResult = await _sut.ListObjectsAsync(_bucketName, key);
    listResult.Data.Should().ContainSingle().Which.Size.Should().Be(tempFile.FileSize);
  }

  [IntegrationTest]
  public async Task PresignedPutUrl_WithMismatchedContentType_ShouldFail()
  {
    // Arrange
    var       key        = $"presigned-fail-content-type-{Guid.NewGuid()}.txt";
    var       signedType = "text/plain";
    var       actualType = "application/octet-stream";
    using var tempFile   = new TempFile(128);
    var       request    = new PresignedPutRequest(key, TimeSpan.FromMinutes(5), tempFile.FileSize, signedType);

    // Act
    var             presignedUrl = _sut.CreatePresignedPutUrl(_bucketName, request);
    using var       httpClient   = new HttpClient();
    await using var fileStream   = File.OpenRead(tempFile.FilePath);
    using var       fileContent  = new StreamContent(fileStream);
    // Set the wrong Content-Type header
    fileContent.Headers.ContentType   = new MediaTypeHeaderValue(actualType);
    fileContent.Headers.ContentLength = tempFile.FileSize;
    var httpResponse = await httpClient.PutAsync(presignedUrl, fileContent);

    // Assert
    httpResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden, "the signature should not match if Content-Type is different");
  }

  [IntegrationTest]
  public async Task PresignedPutUrl_WithMismatchedContentLength_ShouldFail()
  {
    // Arrange
    var key          = $"presigned-fail-content-length-{Guid.NewGuid()}.txt";
    var contentType  = "text/plain";
    var signedLength = 128;
    var actualLength = signedLength - 1; // Mismatch
    var request      = new PresignedPutRequest(key, TimeSpan.FromMinutes(5), signedLength, contentType);

    // Act
    var       presignedUrl = _sut.CreatePresignedPutUrl(_bucketName, request);
    using var httpClient   = new HttpClient();

    // Create a byte array with the 'actual' length. The HttpClient will send this array,
    // and its Content-Length will be `actualLength`. R2 will reject this because the
    // presigned URL was signed for `signedLength`. This avoids the client-side
    // HttpRequestException that occurs when a StreamContent's length exceeds the
    // explicitly set Content-Length header.
    var uploadBytes = new byte[actualLength];
    Random.Shared.NextBytes(uploadBytes);

    using var fileContent = new ByteArrayContent(uploadBytes);
    fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
    // Note: ByteArrayContent automatically sets the Content-Length header to its correct size.
    var httpResponse = await httpClient.PutAsync(presignedUrl, fileContent);

    // Assert
    httpResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden, "the signature should not match if Content-Length is different");
  }

  [IntegrationTest]
  public async Task PresignedUploadPart_WithMismatchedContentLength_ShouldFail()
  {
    // Arrange
    var       key            = $"presigned-part-fail-{Guid.NewGuid()}.bin";
    var       uploadIdResult = await _sut.InitiateMultipartUploadAsync(_bucketName, key);
    var       uploadId       = uploadIdResult.Data;
    var       partNumber     = 1;
    var       signedLength   = 1024;
    var       actualLength   = 512; // Mismatch
    using var tempFile       = new TempFile(actualLength);

    var request = new PresignedUploadPartRequest(key, uploadId, partNumber, TimeSpan.FromMinutes(5), signedLength,
                                                 "application/octet-stream");

    try
    {
      // Act
      var             presignedUrl = _sut.CreatePresignedUploadPartUrl(_bucketName, request);
      using var       httpClient   = new HttpClient();
      await using var fileStream   = File.OpenRead(tempFile.FilePath);
      using var       fileContent  = new StreamContent(fileStream);
      fileContent.Headers.ContentLength = actualLength; // Use the actual, mismatched length
      var httpResponse = await httpClient.PutAsync(presignedUrl, fileContent);

      // Assert
      httpResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden, "the signature should not match if Content-Length is different");
    }
    finally
    {
      await _sut.AbortMultipartUploadAsync(_bucketName, key, uploadId);
    }
  }

  [IntegrationTest]
  public async Task CompleteMultipartUploadAsync_WithDuplicatePart_ThrowsException()
  {
    // Arrange
    var       key            = $"duplicate-part-list-{Guid.NewGuid()}.bin";
    var       partSize       = (int)R2Client.R2MinPartSize;
    using var tempFile       = new TempFile(partSize);
    var       uploadIdResult = await _sut.InitiateMultipartUploadAsync(_bucketName, key);
    var       uploadId       = uploadIdResult.Data;
    var       uploadedParts  = new Dictionary<int, string>();

    try
    {
      // Upload parts 1 and 2
      var partsToUpload = new[] { 1, 2 };
      foreach (var partNumber in partsToUpload)
      {
        await using var fileStream = File.OpenRead(tempFile.FilePath);
        var partRequest = new UploadPartRequest
        {
          BucketName                       = _bucketName,
          Key                              = key,
          UploadId                         = uploadId,
          PartNumber                       = partNumber,
          InputStream                      = fileStream,
          PartSize                         = tempFile.FileSize,
          DisablePayloadSigning            = true,
          DisableDefaultChecksumValidation = true
        };
        var partResponse = await _s3Client.UploadPartAsync(partRequest);
        uploadedParts[partNumber] = partResponse.ETag;
      }

      // Construct a list with a duplicate part for the Complete call
      var completionList = new List<PartETag> { new(1, uploadedParts[1]), new(2, uploadedParts[2]), new(1, uploadedParts[1]) };

      // Act
      var action = () => _sut.CompleteMultipartUploadAsync(_bucketName, key, uploadId, completionList);

      // Assert
      var ex = await action.Should().ThrowAsync<CloudflareR2OperationException>();
      ex.Which.InnerException.Should().BeOfType<AmazonS3Exception>()
        .Which.ErrorCode.Should().Be("InvalidPart"); // R2 historically returned a generic "InternalError" for duplicate parts; since July 2026 it returns the S3-standard "InvalidPart"
    }
    finally
    {
      // The Complete call is expected to fail, so the upload must be aborted.
      await _sut.AbortMultipartUploadAsync(_bucketName, key, uploadId);
    }
  }

  [IntegrationTest]
  public async Task CompleteMultipartUploadAsync_WithOutOfOrderParts_Succeeds()
  {
    // Arrange
    var       key             = $"out-of-order-parts-{Guid.NewGuid()}.bin";
    var       partSize        = (int)R2Client.R2MinPartSize;
    using var tempFile        = new TempFile(partSize); // Same content for both parts is fine
    var       uploadIdResult  = await _sut.InitiateMultipartUploadAsync(_bucketName, key);
    var       uploadId        = uploadIdResult.Data;
    var       uploadedParts   = new Dictionary<int, string>();
    string?   finalObjectEtag = null;

    try
    {
      // Upload parts 1 and 2
      var partsToUpload = new[] { 1, 2 };
      foreach (var partNumber in partsToUpload)
      {
        await using var fileStream = File.OpenRead(tempFile.FilePath);
        var partRequest = new UploadPartRequest
        {
          BucketName                       = _bucketName,
          Key                              = key,
          UploadId                         = uploadId,
          PartNumber                       = partNumber,
          InputStream                      = fileStream,
          PartSize                         = tempFile.FileSize,
          DisablePayloadSigning            = true,
          DisableDefaultChecksumValidation = true
        };
        var partResponse = await _s3Client.UploadPartAsync(partRequest);
        uploadedParts[partNumber] = partResponse.ETag;
      }

      // Construct the list for the Complete call in a non-sequential order
      var completionList = new List<PartETag> { new(2, uploadedParts[2]), new(1, uploadedParts[1]) };

      // Act
      var action = () => _sut.CompleteMultipartUploadAsync(_bucketName, key, uploadId, completionList);

      // Assert: R2 allows out-of-order parts, so this should succeed.
      var result = await action.Should().NotThrowAsync();
      result.Subject.ClassAOperations.Should().Be(1);

      // Verify the object was created successfully
      var listResult    = await _sut.ListObjectsAsync(_bucketName, key);
      var createdObject = listResult.Data.Should().ContainSingle().Subject;
      createdObject.Size.Should().Be(tempFile.FileSize * partsToUpload.Length);
      finalObjectEtag = createdObject.ETag;
    }
    finally
    {
      if (finalObjectEtag is null)
        // If completion failed or an assertion prevented `finalObjectEtag` from being set, abort.
        await _sut.AbortMultipartUploadAsync(_bucketName, key, uploadId, CancellationToken.None);
    }
  }

#if false // There is currently a NRE in the AWS SDK when using CreatePresignedPostUrl with R2.
  [IntegrationTest]
  public async Task CanGenerateAndUsePresignedPostUrl()
  {
    // Arrange
    var       key = $"presigned-post-{Guid.NewGuid()}.txt";
    var       contentType = "text/plain";
    using var tempFile = new TempFile(1024);
    var request = new PresignedPostRequest(
      key,
      TimeSpan.FromMinutes(10),
      ContentType: contentType,
      ContentLengthRange: (1, 2048)
    );

    // Act
    // 1. Generate the presigned POST data
    var postResponse = await _sut.CreatePresignedPostUrlAsync(_bucketName, request);
    postResponse.Url.Should().NotBeNullOrEmpty();
    postResponse.Fields.Should().NotBeEmpty();

    // 2. Use the data to perform an upload
    using var httpClient = new HttpClient();
    using var formData = new MultipartFormDataContent();

    // Add all the required fields from the response
    foreach (var field in postResponse.Fields)
      formData.Add(new StringContent(field.Value), $"\"{field.Key}\"");

    // Add the file content itself. This MUST be the last part of the form.
    await using var fileStream = File.OpenRead(tempFile.FilePath);
    formData.Add(new StreamContent(fileStream), "\"file\"", $"\"{Path.GetFileName(tempFile.FilePath)}\"");
    var httpResponse = await httpClient.PostAsync(postResponse.Url, formData);

    // Assert
    // 3. Verify the upload was successful
    httpResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    var listResult = await _sut.ListObjectsAsync(_bucketName, key);
    listResult.Data.Should().ContainSingle().Which.Size.Should().Be(tempFile.FileSize);
  }
#endif

  /// <summary>
  ///   Verifies that a caller can walk a prefix one page at a time by feeding the returned continuation token
  ///   back into the next call, and that the pages together cover every object exactly once.
  /// </summary>
  [IntegrationTest]
  public async Task ListObjectsPageAsync_WalksAPrefixOnePageAtATime()
  {
    // Arrange - five objects under one prefix, read back two at a time.
    var       prefix   = $"paged-listing-{Guid.NewGuid():N}/";
    using var tempFile = new TempFile(64);

    var expectedKeys = new List<string>();

    for (var i = 0; i < 5; i++)
    {
      var key = $"{prefix}object-{i}.bin";
      await _sut.UploadAsync(_bucketName, key, tempFile.FilePath);
      expectedKeys.Add(key);
    }

    // Act - drive the walk from the caller side, exactly as a resumable job would.
    var       collectedKeys     = new List<string>();
    string?   continuationToken = null;
    var       pageCount         = 0;

    do
    {
      var page = await _sut.ListObjectsPageAsync(_bucketName, prefix, 2, continuationToken);
      pageCount++;

      collectedKeys.AddRange(page.Data.Objects.Select(o => o.Key));

      // Each page is a single billable list call.
      page.Metrics.ClassAOperations.Should().Be(1);

      continuationToken = page.Data.IsTruncated ? page.Data.NextContinuationToken : null;

      // A truncated page must carry the token that continues the walk.
      if (page.Data.IsTruncated)
        continuationToken.Should().NotBeNullOrEmpty();
    } while (continuationToken is not null);

    // Assert - three pages of at most two keys cover all five objects, with no key seen twice.
    pageCount.Should().Be(3);
    collectedKeys.Should().BeEquivalentTo(expectedKeys);
    collectedKeys.Should().OnlyHaveUniqueItems();
  }


  /// <summary>
  ///   Verifies that a multipart upload which was started and never completed is invisible to object listing
  ///   but is reported by <see cref="IR2Client.ListMultipartUploadsAsync" />, and that aborting it removes it.
  /// </summary>
  [IntegrationTest]
  public async Task ListMultipartUploadsAsync_FindsAnUploadThatObjectListingCannotSee()
  {
    // Arrange - start an upload and send one part, then leave it open.
    var key            = $"open-upload-{Guid.NewGuid():N}.bin";
    var initiateResult = await _sut.InitiateMultipartUploadAsync(_bucketName, key);
    var uploadId       = initiateResult.Data;

    // Act - the object does not exist yet, because the upload was never completed.
    var objectListing = await _sut.ListObjectsAsync(_bucketName, key);
    objectListing.Data.Should().BeEmpty("an upload that has not completed produces no object");

    // Assert - the open upload is discoverable, which is the only way to find it.
    var openUploads = await _sut.ListMultipartUploadsAsync(_bucketName, key);
    var discovered  = openUploads.Data.Should().ContainSingle().Which;
    discovered.Key.Should().Be(key);
    openUploads.Metrics.ClassAOperations.Should().BeGreaterThan(0, "listing uploads is a Class A operation");

    // R2 does not return the same upload identifier here that it returned when the upload was started, so a
    // cleanup must abort using the identifier the listing gave it. Both are recorded for diagnostics.
    _output.WriteLine($"UploadId from InitiateMultipartUploadAsync: {uploadId}");
    _output.WriteLine($"UploadId from ListMultipartUploadsAsync:    {discovered.UploadId}");

    // Act - abort using the identifier the listing supplied, which is what the cleanup path does.
    await _sut.AbortMultipartUploadAsync(_bucketName, discovered.Key, discovered.UploadId);

    // Assert - the upload is gone, so the identifier from the listing is the one that works.
    var afterAbort = await _sut.ListMultipartUploadsAsync(_bucketName, key);
    afterAbort.Data.Should().BeEmpty("aborting with the identifier from the listing must remove the upload");
  }


  /// <summary>
  ///   Verifies that clearing a bucket aborts multipart uploads left open in it, which is what allows the
  ///   bucket to be deleted afterwards, and that opting out leaves those uploads in place.
  /// </summary>
  [IntegrationTest]
  public async Task ClearBucketAsync_AbortsOpenMultipartUploadsUnlessTheCallerOptsOut()
  {
    // Arrange - an upload left open, with no completed object anywhere in the bucket.
    var key = $"clear-open-upload-{Guid.NewGuid():N}.bin";
    await _sut.InitiateMultipartUploadAsync(_bucketName, key);

    // Act - clear the bucket while declining the multipart upload cleanup.
    await _sut.ClearBucketAsync(_bucketName, true, false);

    // Assert - the upload survives, because the caller asked for objects only.
    var afterOptOut = await _sut.ListMultipartUploadsAsync(_bucketName, key);
    afterOptOut.Data.Should().ContainSingle()
               .Which.Key.Should().Be(key, "declining the cleanup must leave the upload open");

    // Act - clear the bucket again, this time with the default cleanup.
    await _sut.ClearBucketAsync(_bucketName);

    // Assert - the upload is gone, so Cloudflare will now accept a delete of the bucket.
    var afterCleanup = await _sut.ListMultipartUploadsAsync(_bucketName, key);
    afterCleanup.Data.Should().BeEmpty("clearing the bucket aborts uploads left open in it");
  }


  /// <summary>
  ///   Verifies that a presigned GET URL downloads the object, and that the response header overrides signed
  ///   into the URL are the headers R2 actually returns.
  /// </summary>
  [IntegrationTest]
  public async Task CanGenerateAndUsePresignedGetUrl_WithResponseHeaderOverrides()
  {
    // Arrange - an object stored as one media type, to be served as another.
    var       key      = $"presigned-get-{Guid.NewGuid():N}.bin";
    using var tempFile = new TempFile(256);
    await _sut.UploadAsync(_bucketName, key, tempFile.FilePath);

    var request = new PresignedGetRequest(
      key,
      TimeSpan.FromMinutes(5),
      ResponseContentType: "application/json",
      ResponseContentDisposition: "attachment; filename=\"renamed-by-the-url.json\"");

    // Act
    var presignedUrl = _sut.CreatePresignedGetUrl(_bucketName, request);
    presignedUrl.Should().NotBeNullOrEmpty();

    using var httpClient   = new HttpClient();
    var       httpResponse = await httpClient.GetAsync(presignedUrl);

    // Assert - the download succeeds and returns the object's bytes.
    httpResponse.EnsureSuccessStatusCode();
    var downloadedBytes = await httpResponse.Content.ReadAsByteArrayAsync();
    var originalBytes   = await File.ReadAllBytesAsync(tempFile.FilePath);
    downloadedBytes.Should().BeEquivalentTo(originalBytes);

    // Assert - R2 honored the overrides that were signed into the URL, rather than the stored metadata.
    httpResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    httpResponse.Content.Headers.ContentDisposition.Should().NotBeNull();
    httpResponse.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
    // The .NET header parser strips the quotes that the signed value carries around the filename.
    httpResponse.Content.Headers.ContentDisposition.FileName.Should().Be("renamed-by-the-url.json");
  }


  /// <summary>
  ///   Verifies that a presigned GET URL created without any response header overrides still downloads the
  ///   object, leaving R2 to serve the stored metadata.
  /// </summary>
  [IntegrationTest]
  public async Task CanGenerateAndUsePresignedGetUrl_WithoutOverrides()
  {
    // Arrange
    var       key      = $"presigned-get-plain-{Guid.NewGuid():N}.bin";
    using var tempFile = new TempFile(128);
    await _sut.UploadAsync(_bucketName, key, tempFile.FilePath);

    // Act
    var presignedUrl = _sut.CreatePresignedGetUrl(_bucketName, new PresignedGetRequest(key, TimeSpan.FromMinutes(5)));

    using var httpClient   = new HttpClient();
    var       httpResponse = await httpClient.GetAsync(presignedUrl);

    // Assert
    httpResponse.EnsureSuccessStatusCode();
    var downloadedBytes = await httpResponse.Content.ReadAsByteArrayAsync();
    downloadedBytes.Should().HaveCount((int)tempFile.FileSize);
  }


  /// <summary>
  ///   Verifies that a presigned GET URL stops working once its validity window has passed.
  /// </summary>
  [IntegrationTest]
  public async Task PresignedGetUrl_AfterExpiry_IsRejected()
  {
    // Arrange - a well-formed URL with a very short validity window, so that waiting makes it expire. Asking
    // for a negative window instead would produce a malformed request, which R2 rejects for a different
    // reason (400) and would not prove that expiry itself is enforced.
    var       key      = $"presigned-get-expired-{Guid.NewGuid():N}.bin";
    using var tempFile = new TempFile(64);
    await _sut.UploadAsync(_bucketName, key, tempFile.FilePath);

    // The window is several seconds wide, not one: the suite runs many tests in parallel, and a narrower
    // window can close between signing and the first fetch below, failing the fetch that is supposed to
    // prove the URL was well-formed while still valid.
    var presignedUrl = _sut.CreatePresignedGetUrl(
      _bucketName,
      new PresignedGetRequest(key, TimeSpan.FromSeconds(5)));

    using var httpClient = new HttpClient();

    // The URL works while its window is open.
    var beforeExpiry = await httpClient.GetAsync(presignedUrl);
    beforeExpiry.EnsureSuccessStatusCode();

    // Act - wait for the window to close (with margin for clock skew), then use the same URL again.
    await Task.Delay(TimeSpan.FromSeconds(8));

    var afterExpiry = await httpClient.GetAsync(presignedUrl);

    // Assert - R2 refuses the expired URL instead of serving the object.
    afterExpiry.IsSuccessStatusCode.Should().BeFalse("the URL's validity window has passed");
    afterExpiry.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }

  [IntegrationTest]
  public async Task MultipartUpload_TakesItsContentTypeFromTheInitiateCall()
  {
    // Arrange - a multipart upload carrying a content type that R2 would never infer on its own.
    // A single-part multipart upload is legal at any size, because the 5 MiB minimum applies to every
    // part except the last one, and the only part here is also the last.
    var       key         = $"multipart-content-type-{Guid.NewGuid():N}.bin";
    var       contentType = "application/pdf";
    using var tempFile    = new TempFile(64 * 1024);

    var initiate = await _sut.InitiateMultipartUploadAsync(_bucketName, key, contentType);
    var uploadId = initiate.Data;

    try
    {
      // Act
      await using var fileStream = File.OpenRead(tempFile.FilePath);

      var partResponse = await _s3Client.UploadPartAsync(new UploadPartRequest
      {
        BucketName                       = _bucketName,
        Key                              = key,
        UploadId                         = uploadId,
        PartNumber                       = 1,
        PartSize                         = tempFile.FileSize,
        InputStream                      = fileStream,
        DisablePayloadSigning            = true,
        DisableDefaultChecksumValidation = true
      });

      await _sut.CompleteMultipartUploadAsync(_bucketName, key, uploadId, [new PartETag(1, partResponse.ETag)]);

      // Assert - the assembled object carries the value supplied when the upload started. The key ends in
      // ".bin", so R2 could not have guessed "application/pdf" from the extension.
      var metadata = await _s3Client.GetObjectMetadataAsync(_bucketName, key);
      _output.WriteLine($"Content-Type reported by R2: {metadata.Headers.ContentType}");
      metadata.Headers.ContentType.Should().Be(contentType,
                                               "S3 records the assembled object's Content-Type from the initiate request, never from the parts");
    }
    catch
    {
      await _sut.AbortMultipartUploadAsync(_bucketName, key, uploadId);
      throw;
    }
  }

  [IntegrationTest]
  public async Task MultipartUpload_WithoutAContentType_LeavesR2ToChooseOne()
  {
    // Arrange - the same upload with no content type supplied, which is what every caller of the older
    // overload gets. This is the comparison that proves the content type argument is what changes the
    // stored value, rather than something else about the upload.
    var       key      = $"multipart-no-content-type-{Guid.NewGuid():N}.bin";
    using var tempFile = new TempFile(64 * 1024);

    var initiate = await _sut.InitiateMultipartUploadAsync(_bucketName, key);
    var uploadId = initiate.Data;

    try
    {
      // Act
      await using var fileStream = File.OpenRead(tempFile.FilePath);

      var partResponse = await _s3Client.UploadPartAsync(new UploadPartRequest
      {
        BucketName                       = _bucketName,
        Key                              = key,
        UploadId                         = uploadId,
        PartNumber                       = 1,
        PartSize                         = tempFile.FileSize,
        InputStream                      = fileStream,
        DisablePayloadSigning            = true,
        DisableDefaultChecksumValidation = true
      });

      await _sut.CompleteMultipartUploadAsync(_bucketName, key, uploadId, [new PartETag(1, partResponse.ETag)]);

      // Assert - R2 applies a default of its own choosing. The exact string is R2's to pick, so the test
      // only proves it is not the value the other test supplies.
      var metadata = await _s3Client.GetObjectMetadataAsync(_bucketName, key);
      _output.WriteLine($"Content-Type chosen by R2 when none was supplied: {metadata.Headers.ContentType}");
      metadata.Headers.ContentType.Should().NotBe("application/pdf");
    }
    catch
    {
      await _sut.AbortMultipartUploadAsync(_bucketName, key, uploadId);
      throw;
    }
  }

  [IntegrationTest]
  public async Task UploadSinglePartAsync_WithContentType_StoresTheTypeOnTheObject()
  {
    // Arrange - the key ends in ".bin", so R2 could not have guessed "image/webp" from the extension:
    // the stored type can only come from the argument.
    var       key         = $"single-part-content-type-{Guid.NewGuid():N}.bin";
    var       contentType = "image/webp";
    using var stream      = new MemoryStream(new byte[64 * 1024]);

    // Act
    await _sut.UploadSinglePartAsync(_bucketName, key, stream, contentType);

    // Assert
    var metadata = await _s3Client.GetObjectMetadataAsync(_bucketName, key);
    _output.WriteLine($"Content-Type reported by R2: {metadata.Headers.ContentType}");
    metadata.Headers.ContentType.Should().Be(contentType);
  }

  [IntegrationTest]
  public async Task UploadMultipartAsync_WithContentType_StoresTheTypeOnTheAssembledObject()
  {
    // Arrange - the high-level multipart method must place the type on its own initiate call. A 64 KiB
    // file is a legal multipart upload, because the 5 MiB minimum applies to every part except the last
    // and the only part here is also the last.
    var       key         = $"upload-multipart-content-type-{Guid.NewGuid():N}.bin";
    var       contentType = "image/webp";
    using var tempFile    = new TempFile(64 * 1024);

    // Act
    await _sut.UploadMultipartAsync(_bucketName, key, tempFile.FilePath, null, contentType);

    // Assert
    var metadata = await _s3Client.GetObjectMetadataAsync(_bucketName, key);
    _output.WriteLine($"Content-Type reported by R2: {metadata.Headers.ContentType}");
    metadata.Headers.ContentType.Should().Be(contentType,
                                             "the high-level multipart upload records the type on the initiate request");
  }

  #endregion
}
