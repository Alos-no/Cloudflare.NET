namespace Cloudflare.NET.R2.Tests.IntegrationTests;

using Accounts.Buckets;
using Accounts.Models;
using Amazon.S3;
using Fixtures;
using FluentAssertions;
using Helpers;
using Microsoft.Extensions.DependencyInjection;
using NET.Tests.Shared.Fixtures;
using NET.Tests.Shared.Helpers;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for the R2 S3 client jurisdiction support. These tests verify that the
///   <see cref="IR2ClientFactory" /> correctly creates clients for different jurisdictions and that
///   S3 operations work correctly against jurisdiction-specific endpoints.
/// </summary>
/// <remarks>
///   <para>
///     <strong>IMPORTANT:</strong> These tests require a Cloudflare account with EU jurisdiction enabled.
///   </para>
///   <para>
///     Jurisdictions use different S3 endpoints:
///     <list type="bullet">
///       <item><description>Default: https://{account_id}.r2.cloudflarestorage.com</description></item>
///       <item><description>EU: https://{account_id}.eu.r2.cloudflarestorage.com</description></item>
///       <item><description>FedRAMP: https://{account_id}.fedramp.r2.cloudflarestorage.com</description></item>
///     </list>
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class R2ClientJurisdictionIntegrationTests : IClassFixture<R2ClientTestFixture>, IAsyncLifetime
{
  #region Properties & Fields - Non-Public

  private readonly IR2ClientFactory  _factory;
  private readonly IR2BucketsApi     _bucketsApi;
  private readonly ITestOutputHelper _output;

  /// <summary>The name of the EU jurisdiction bucket created for this test run.</summary>
  private readonly string _euBucketName = $"cfnet-r2-eu-test-{Guid.NewGuid():N}";

  #endregion


  #region Constructors

  public R2ClientJurisdictionIntegrationTests(R2ClientTestFixture fixture, ITestOutputHelper output)
  {
    _factory    = fixture.ServiceProvider.GetRequiredService<IR2ClientFactory>();
    _bucketsApi = fixture.ServiceProvider.GetRequiredService<ICloudflareApiClient>().Accounts.Buckets;
    _output     = output;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region IAsyncLifetime

  public async Task InitializeAsync()
  {
    // Create an EU jurisdiction bucket for the test run.
    await _bucketsApi.CreateAsync(_euBucketName, jurisdiction: R2Jurisdiction.EuropeanUnion);
    _output.WriteLine($"Created EU jurisdiction bucket: {_euBucketName}");
  }


  public async Task DisposeAsync()
  {
    // Best-effort cleanup of the EU bucket.
    try
    {
      var euClient = _factory.GetClient(R2Jurisdiction.EuropeanUnion);
      await euClient.ClearBucketAsync(_euBucketName, true);
    }
    catch (Exception ex)
    {
      _output.WriteLine($"Warning: Failed to clear EU bucket: {ex.Message}");
    }

    try
    {
      await _bucketsApi.DeleteAsync(_euBucketName, R2Jurisdiction.EuropeanUnion);
      _output.WriteLine($"Deleted EU jurisdiction bucket: {_euBucketName}");
    }
    catch (Exception ex)
    {
      _output.WriteLine($"Warning: Failed to delete EU bucket: {ex.Message}");
    }
  }

  #endregion


  #region GetClient(R2Jurisdiction) Integration Tests

  /// <summary>
  ///   Verifies that getting a client for EU jurisdiction returns a functional client
  ///   that can perform S3 operations against EU buckets.
  /// </summary>
  [IntegrationTest]
  public async Task GetClient_WithEuJurisdiction_CanUploadToEuBucket()
  {
    // Arrange
    var       key      = $"eu-upload-test-{Guid.NewGuid()}.txt";
    using var tempFile = new TempFile(1024); // 1KB file
    var       euClient = _factory.GetClient(R2Jurisdiction.EuropeanUnion);

    // Act
    var uploadResult = await euClient.UploadAsync(_euBucketName, key, tempFile.FilePath);

    // Assert
    uploadResult.ClassAOperations.Should().Be(1);
    uploadResult.IngressBytes.Should().Be(tempFile.FileSize);
  }


  /// <summary>
  ///   Verifies that uploading and downloading through the EU jurisdiction client
  ///   correctly round-trips data.
  /// </summary>
  [IntegrationTest]
  public async Task GetClient_WithEuJurisdiction_CanRoundTripData()
  {
    // Arrange
    var       key          = $"eu-roundtrip-test-{Guid.NewGuid()}.bin";
    using var uploadFile   = new TempFile(2048); // 2KB file
    using var downloadFile = new TempFile(0);
    var       euClient     = _factory.GetClient(R2Jurisdiction.EuropeanUnion);

    // Act
    await euClient.UploadAsync(_euBucketName, key, uploadFile.FilePath);
    var downloadResult = await euClient.DownloadFileAsync(_euBucketName, key, downloadFile.FilePath);

    // Assert
    downloadResult.ClassBOperations.Should().Be(1);
    downloadResult.EgressBytes.Should().Be(uploadFile.FileSize);

    var originalBytes   = await File.ReadAllBytesAsync(uploadFile.FilePath);
    var downloadedBytes = await File.ReadAllBytesAsync(downloadFile.FilePath);
    downloadedBytes.Should().BeEquivalentTo(originalBytes);
  }


  /// <summary>
  ///   Verifies that listing objects through the EU jurisdiction client works correctly.
  /// </summary>
  [IntegrationTest]
  public async Task GetClient_WithEuJurisdiction_CanListObjects()
  {
    // Arrange
    var       prefix   = "eu-list-test/";
    using var tempFile = new TempFile(512);
    var       euClient = _factory.GetClient(R2Jurisdiction.EuropeanUnion);
    var       keys     = new List<string>();

    for (var i = 0; i < 3; i++)
    {
      var key = $"{prefix}file-{i}.txt";
      await euClient.UploadAsync(_euBucketName, key, tempFile.FilePath);
      keys.Add(key);
    }

    // Act
    var listResult = await euClient.ListObjectsAsync(_euBucketName, prefix);

    // Assert
    listResult.Data.Should().HaveCount(3);
    listResult.Data.Select(o => o.Key).Should().BeEquivalentTo(keys);
  }


  /// <summary>
  ///   Verifies that deleting objects through the EU jurisdiction client works correctly.
  /// </summary>
  [IntegrationTest]
  public async Task GetClient_WithEuJurisdiction_CanDeleteObjects()
  {
    // Arrange
    var       key      = $"eu-delete-test-{Guid.NewGuid()}.txt";
    using var tempFile = new TempFile(256);
    var       euClient = _factory.GetClient(R2Jurisdiction.EuropeanUnion);

    await euClient.UploadAsync(_euBucketName, key, tempFile.FilePath);

    // Act
    var deleteResult = await euClient.DeleteObjectAsync(_euBucketName, key);

    // Assert
    deleteResult.ClassAOperations.Should().Be(0); // DeleteObject is free

    var listResult = await euClient.ListObjectsAsync(_euBucketName, key);
    listResult.Data.Should().BeEmpty();
  }


  /// <summary>
  ///   Verifies that multipart uploads work through the EU jurisdiction client.
  /// </summary>
  [IntegrationTest]
  public async Task GetClient_WithEuJurisdiction_CanPerformMultipartUpload()
  {
    // Arrange
    var       key       = $"eu-multipart-test-{Guid.NewGuid()}.bin";
    using var largeFile = new TempFile(6 * 1024 * 1024); // 6MB file (requires multipart)
    var       euClient  = _factory.GetClient(R2Jurisdiction.EuropeanUnion);

    // Act
    var uploadResult = await euClient.UploadMultipartAsync(_euBucketName, key, largeFile.FilePath, 5 * 1024 * 1024);

    // Assert
    uploadResult.ClassAOperations.Should().BeGreaterThan(1); // Init + Parts + Complete
    uploadResult.IngressBytes.Should().Be(largeFile.FileSize);

    var listResult = await euClient.ListObjectsAsync(_euBucketName, key);
    listResult.Data.Should().ContainSingle().Which.Size.Should().Be(largeFile.FileSize);
  }

  #endregion


  #region Cross-Jurisdiction Error Tests

  /// <summary>
  ///   Verifies that using the default jurisdiction client to access an EU bucket fails.
  ///   This confirms that jurisdiction-specific endpoints are actually being used.
  /// </summary>
  [IntegrationTest]
  public async Task GetClient_WithDefaultJurisdiction_CannotAccessEuBucket()
  {
    // Arrange
    var defaultClient = _factory.GetClient(R2Jurisdiction.Default);

    // Act - Attempt to list objects in an EU bucket using the default (non-EU) endpoint
    var action = async () => await defaultClient.ListObjectsAsync(_euBucketName, null);

    // Assert - Should fail because the bucket is in EU jurisdiction
    // The exact error may vary (404 NotFound or AccessDenied), but it should fail
    await action.Should().ThrowAsync<Exception>(
      "accessing an EU bucket via the default endpoint should fail");
  }

  #endregion


  #region Client Caching Tests

  /// <summary>
  ///   Verifies that the factory caches EU jurisdiction clients correctly.
  /// </summary>
  [IntegrationTest]
  public void GetClient_CalledMultipleTimes_ReturnsSameInstance()
  {
    // Act
    var client1 = _factory.GetClient(R2Jurisdiction.EuropeanUnion);
    var client2 = _factory.GetClient(R2Jurisdiction.EuropeanUnion);

    // Assert
    client1.Should().BeSameAs(client2, "factory should cache and return the same client instance");
  }


  /// <summary>
  ///   Verifies that different jurisdictions return different client instances.
  /// </summary>
  [IntegrationTest]
  public void GetClient_DifferentJurisdictions_ReturnsDifferentInstances()
  {
    // Act
    var defaultClient = _factory.GetClient(R2Jurisdiction.Default);
    var euClient      = _factory.GetClient(R2Jurisdiction.EuropeanUnion);

    // Assert
    defaultClient.Should().NotBeSameAs(euClient, "different jurisdictions should have different clients");
  }

  #endregion


  #region Presigned URL Tests

  /// <summary>
  ///   Verifies that presigned URLs generated by the EU client work for EU bucket uploads.
  /// </summary>
  [IntegrationTest]
  public async Task GetClient_WithEuJurisdiction_PresignedPutUrlWorks()
  {
    // Arrange
    var       key         = $"eu-presigned-put-{Guid.NewGuid()}.txt";
    var       contentType = "text/plain";
    using var tempFile    = new TempFile(512);
    var       euClient    = _factory.GetClient(R2Jurisdiction.EuropeanUnion);
    var       request     = new Models.PresignedPutRequest(key, TimeSpan.FromMinutes(5), tempFile.FileSize, contentType);

    // Act
    var presignedUrl = euClient.CreatePresignedPutUrl(_euBucketName, request);

    // Assert - URL should point to EU endpoint
    presignedUrl.Should().Contain(".eu.r2.cloudflarestorage.com", "presigned URL should use EU endpoint");

    // Verify the presigned URL works
    using var       httpClient  = new HttpClient();
    await using var fileStream  = File.OpenRead(tempFile.FilePath);
    using var       fileContent = new StreamContent(fileStream);
    fileContent.Headers.ContentType   = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
    fileContent.Headers.ContentLength = tempFile.FileSize;
    var httpResponse = await httpClient.PutAsync(presignedUrl, fileContent);

    httpResponse.EnsureSuccessStatusCode();

    // Verify the file was uploaded
    var listResult = await euClient.ListObjectsAsync(_euBucketName, key);
    listResult.Data.Should().ContainSingle().Which.Size.Should().Be(tempFile.FileSize);
  }

  #endregion
}
