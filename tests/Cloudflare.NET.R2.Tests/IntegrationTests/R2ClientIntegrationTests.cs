namespace Cloudflare.NET.R2.Tests.IntegrationTests;

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Amazon.S3;
using Amazon.S3.Model;
using Accounts;
using Exceptions;
using Fixtures;
using FluentAssertions;
using Helpers;
using Models;
using NET.Tests.Shared.Fixtures;
using NET.Tests.Shared.Helpers;

[Trait("Category", TestConstants.TestCategories.Integration)]
public class R2ClientIntegrationTests : IClassFixture<R2ClientTestFixture>, IAsyncLifetime
{
  #region Properties & Fields - Non-Public

  private readonly IR2Client    _sut;
  private readonly IAccountsApi _accountsApi;
  private readonly IAmazonS3    _s3Client;
  private readonly string       _bucketName = $"cfnet-r2-test-bucket-{Guid.NewGuid():N}";

  #endregion

  #region Constructors

  public R2ClientIntegrationTests(R2ClientTestFixture fixture)
  {
    _sut         = fixture.R2Client;
    _accountsApi = fixture.AccountsApi;
    _s3Client    = fixture.S3Client;
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
    deleteResult.ClassAOperations.Should().Be(1);

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
    using var downloadFile   = new TempFile(0);
    // ReSharper disable once AccessToDisposedClosure
    var       downloadAction = () => _sut.DownloadFileAsync(_bucketName, key, downloadFile.FilePath);

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
    deleteResult.ClassAOperations.Should().Be(1);
    var listResult = await _sut.ListObjectsAsync(_bucketName, "delete-batch-");
    listResult.Data.Should().BeEmpty();
  }

  [IntegrationTest]
  public async Task DeleteObjectsAsync_WithNonExistentKeys_SucceedsAndReportsNoErrors()
  {
    // Arrange
    var realKey1 = $"real-key-1-{Guid.NewGuid()}.txt";
    var realKey2 = $"real-key-2-{Guid.NewGuid()}.txt";
    var fakeKey1 = "this-key-does-not-exist.txt";
    var fakeKey2 = "neither-does-this-one.txt";
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
    clearResult.ClassAOperations.Should().BeGreaterThanOrEqualTo(2); // At least one list and one delete for each page
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
    result.ClassAOperations.Should().Be(1); // Should only perform one List operation
    var listResult = await _sut.ListObjectsAsync(_bucketName, null);
    listResult.Data.Should().BeEmpty();
  }

  [IntegrationTest]
  public async Task CompleteMultipartUploadAsync_WithInvalidPart_ThrowsException()
  {
    // Arrange
    var       key      = $"bad-multipart-{Guid.NewGuid()}.bin";
    using var tempFile = new TempFile(6 * 1024 * 1024); // 6MB
    var uploadIdResult = await _sut.InitiateMultipartUploadAsync(_bucketName, key);
    var uploadId       = uploadIdResult.Data;
    
    try
    {
      // Upload a valid part 1 using the raw S3 client to get a valid ETag
      await using var fileStream  = File.OpenRead(tempFile.FilePath);
      
      var       partRequest = new UploadPartRequest
      {
        BucketName                     = _bucketName,
        Key                            = key,
        UploadId                       = uploadId,
        PartNumber                     = 1,
        PartSize                       = 5 * 1024 * 1024,
        InputStream                    = fileStream,
        DisablePayloadSigning          = true,
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
        .Which.Message.Should().Contain("InvalidPart"); // R2/S3 specific error message
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
    using var httpClient = new HttpClient();
    await using var fileStream = File.OpenRead(tempFile.FilePath);
    using var fileContent = new StreamContent(fileStream);
    fileContent.Headers.ContentType     = new MediaTypeHeaderValue(contentType);
    fileContent.Headers.ContentLength = tempFile.FileSize;
    var httpResponse = await httpClient.PutAsync(presignedUrl, fileContent);

    // Assert
    // 3. Verify the upload was successful
    httpResponse.EnsureSuccessStatusCode();
    var listResult = await _sut.ListObjectsAsync(_bucketName, key);
    listResult.Data.Should().ContainSingle().Which.Size.Should().Be(tempFile.FileSize);
  }

  [IntegrationTest]
  public async Task CanGenerateAndUsePresignedPostUrl()
  {
    // Arrange
    var       key         = $"presigned-post-{Guid.NewGuid()}.txt";
    var       contentType = "text/plain";
    using var tempFile    = new TempFile(1024);
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
    using var formData   = new MultipartFormDataContent();

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

  #endregion
}
