namespace Cloudflare.NET.R2.Tests.IntegrationTests;

using Accounts.Buckets;
using Accounts.Models;
using Fixtures;
using FluentAssertions;
using Helpers;
using Microsoft.Extensions.DependencyInjection;
using NET.Tests.Shared.Fixtures;
using NET.Tests.Shared.Helpers;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for the R2 S3 client US jurisdiction support. These tests verify that the
///   <see cref="IR2ClientFactory" /> correctly creates clients for the US jurisdiction and that
///   S3 operations work correctly against the US-specific endpoint.
/// </summary>
/// <remarks>
///   <para>
///     The US jurisdiction is generally available and uses the S3 endpoint:
///     <c>https://{account_id}.us.r2.cloudflarestorage.com</c>
///   </para>
///   <para>
///     These tests mirror a subset of <see cref="R2ClientJurisdictionIntegrationTests" /> (the EU suite);
///     the mechanism under test is identical, only the jurisdiction value and endpoint differ.
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class R2ClientUsJurisdictionIntegrationTests : IClassFixture<R2ClientTestFixture>, IAsyncLifetime
{
  #region Properties & Fields - Non-Public

  private readonly IR2ClientFactory  _factory;
  private readonly IR2BucketsApi     _bucketsApi;
  private readonly ITestOutputHelper _output;

  /// <summary>The name of the US jurisdiction bucket created for this test run.</summary>
  private readonly string _usBucketName = $"cfnet-r2-us-test-{Guid.NewGuid():N}";

  #endregion


  #region Constructors

  public R2ClientUsJurisdictionIntegrationTests(R2ClientTestFixture fixture, ITestOutputHelper output)
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
    // Create a US jurisdiction bucket for the test run.
    await _bucketsApi.CreateAsync(_usBucketName, jurisdiction: R2Jurisdiction.UnitedStates);
    _output.WriteLine($"Created US jurisdiction bucket: {_usBucketName}");
  }


  public async Task DisposeAsync()
  {
    // Best-effort cleanup of the US bucket.
    try
    {
      var usClient = _factory.GetClient(R2Jurisdiction.UnitedStates);
      await usClient.ClearBucketAsync(_usBucketName, true);
    }
    catch (Exception ex)
    {
      _output.WriteLine($"Warning: Failed to clear US bucket: {ex.Message}");
    }

    try
    {
      await _bucketsApi.DeleteAsync(_usBucketName, R2Jurisdiction.UnitedStates);
      _output.WriteLine($"Deleted US jurisdiction bucket: {_usBucketName}");
    }
    catch (Exception ex)
    {
      _output.WriteLine($"Warning: Failed to delete US bucket: {ex.Message}");
    }
  }

  #endregion


  #region GetClient(R2Jurisdiction) Integration Tests

  /// <summary>
  ///   Verifies that getting a client for US jurisdiction returns a functional client
  ///   that can perform S3 operations against US buckets.
  /// </summary>
  [IntegrationTest]
  public async Task GetClient_WithUsJurisdiction_CanUploadToUsBucket()
  {
    // Arrange
    var       key      = $"us-upload-test-{Guid.NewGuid()}.txt";
    using var tempFile = new TempFile(1024); // 1KB file
    var       usClient = _factory.GetClient(R2Jurisdiction.UnitedStates);

    // Act
    var uploadResult = await usClient.UploadAsync(_usBucketName, key, tempFile.FilePath);

    // Assert
    uploadResult.ClassAOperations.Should().Be(1);
    uploadResult.IngressBytes.Should().Be(tempFile.FileSize);
  }


  /// <summary>
  ///   Verifies that uploading and downloading through the US jurisdiction client
  ///   correctly round-trips data.
  /// </summary>
  [IntegrationTest]
  public async Task GetClient_WithUsJurisdiction_CanRoundTripData()
  {
    // Arrange
    var       key          = $"us-roundtrip-test-{Guid.NewGuid()}.bin";
    using var uploadFile   = new TempFile(2048); // 2KB file
    using var downloadFile = new TempFile(0);
    var       usClient     = _factory.GetClient(R2Jurisdiction.UnitedStates);

    // Act
    await usClient.UploadAsync(_usBucketName, key, uploadFile.FilePath);
    var downloadResult = await usClient.DownloadFileAsync(_usBucketName, key, downloadFile.FilePath);

    // Assert
    downloadResult.ClassBOperations.Should().Be(1);
    downloadResult.EgressBytes.Should().Be(uploadFile.FileSize);

    var originalBytes   = await File.ReadAllBytesAsync(uploadFile.FilePath);
    var downloadedBytes = await File.ReadAllBytesAsync(downloadFile.FilePath);
    downloadedBytes.Should().BeEquivalentTo(originalBytes);
  }

  #endregion


  #region Cross-Jurisdiction Error Tests

  /// <summary>
  ///   Verifies that using the default jurisdiction client to access a US bucket fails.
  ///   This confirms that jurisdiction-specific endpoints are actually being used.
  /// </summary>
  [IntegrationTest]
  public async Task GetClient_WithDefaultJurisdiction_CannotAccessUsBucket()
  {
    // Arrange
    var defaultClient = _factory.GetClient(R2Jurisdiction.Default);

    // Act - Attempt to list objects in a US bucket using the default (non-US) endpoint
    var action = async () => await defaultClient.ListObjectsAsync(_usBucketName, null);

    // Assert - Should fail because the bucket is in US jurisdiction
    // The exact error may vary (404 NotFound or AccessDenied), but it should fail
    await action.Should().ThrowAsync<Exception>(
      "accessing a US bucket via the default endpoint should fail");
  }

  #endregion


  #region Presigned URL Tests

  /// <summary>
  ///   Verifies that presigned URLs generated by the US client work for US bucket uploads.
  /// </summary>
  [IntegrationTest]
  public async Task GetClient_WithUsJurisdiction_PresignedPutUrlWorks()
  {
    // Arrange
    var       key         = $"us-presigned-put-{Guid.NewGuid()}.txt";
    var       contentType = "text/plain";
    using var tempFile    = new TempFile(512);
    var       usClient    = _factory.GetClient(R2Jurisdiction.UnitedStates);
    var       request     = new Models.PresignedPutRequest(key, TimeSpan.FromMinutes(5), tempFile.FileSize, contentType);

    // Act
    var presignedUrl = usClient.CreatePresignedPutUrl(_usBucketName, request);

    // Assert - URL should point to US endpoint
    presignedUrl.Should().Contain(".us.r2.cloudflarestorage.com", "presigned URL should use US endpoint");

    // Verify the presigned URL works
    using var       httpClient  = new HttpClient();
    await using var fileStream  = File.OpenRead(tempFile.FilePath);
    using var       fileContent = new StreamContent(fileStream);
    fileContent.Headers.ContentType   = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
    fileContent.Headers.ContentLength = tempFile.FileSize;
    var httpResponse = await httpClient.PutAsync(presignedUrl, fileContent);

    httpResponse.EnsureSuccessStatusCode();

    // Verify the file was uploaded
    var listResult = await usClient.ListObjectsAsync(_usBucketName, key);
    listResult.Data.Should().ContainSingle().Which.Size.Should().Be(tempFile.FileSize);
  }

  #endregion
}
