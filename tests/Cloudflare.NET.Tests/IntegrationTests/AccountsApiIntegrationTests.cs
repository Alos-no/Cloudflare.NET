﻿namespace Cloudflare.NET.Tests.IntegrationTests;

using System.Net;
using Accounts;
using Fixtures;
using Shared.Fixtures;
using Shared.Helpers;

/// <summary>
///   Contains integration tests for the <see cref="AccountsApi" /> class. These tests
///   interact with the live Cloudflare API and require credentials.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class AccountsApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>, IAsyncLifetime
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IAccountsApi _sut;

  /// <summary>A unique name for the R2 bucket used in this test run, to avoid collisions.</summary>
  private readonly string _bucketName = $"cfnet-test-bucket-{Guid.NewGuid():N}";

  /// <summary>The settings loaded from the test configuration.</summary>
  private readonly TestCloudflareSettings _settings;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="AccountsApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  public AccountsApiIntegrationTests(CloudflareApiTestFixture fixture)
  {
    // The SUT is resolved via the fixture's pre-configured DI container.
    _sut      = fixture.AccountsApi;
    _settings = TestConfiguration.CloudflareSettings;
  }

  #endregion

  #region Methods Impl

  /// <summary>
  ///   Asynchronously creates the R2 bucket required for the tests. This runs once before
  ///   any tests in this class.
  /// </summary>
  public async Task InitializeAsync()
  {
    // Create a new R2 bucket for the test run.
    await _sut.CreateR2BucketAsync(_bucketName);
  }

  /// <summary>Asynchronously deletes the R2 bucket after all tests in this class have run.</summary>
  public async Task DisposeAsync()
  {
    // Clean up the R2 bucket.
    // A retry loop is helpful here as some resources (like custom domains)
    // may take time to fully detach, causing a temporary conflict when deleting the bucket.
    var       attempts    = 0;
    const int maxAttempts = 3;

    while (true)
      try
      {
        await _sut.DeleteR2BucketAsync(_bucketName);
        return; // Success
      }
      catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
      {
        attempts++;
        if (attempts >= maxAttempts)
          // Log or handle the final failure if necessary
          throw; // Rethrow after final attempt

        // Wait before retrying
        await Task.Delay(TimeSpan.FromSeconds(5));
      }
  }

  #endregion

  #region Methods

  /// <summary>Verifies that the r2.dev URL for a bucket can be disabled.</summary>
  [IntegrationTest]
  public async Task CanDisableDevUrl()
  {
    // Arrange & Act
    var action = async () => await _sut.DisableDevUrlAsync(_bucketName);

    // Assert
    await action.Should().NotThrowAsync();
  }

  /// <summary>Tests the full lifecycle of a custom domain: attach, get status, and detach.</summary>
  [IntegrationTest]
  public async Task CanManageCustomDomainLifecycle()
  {
    // Arrange
    // A unique hostname for this test run. The BaseDomain is now inferred by the fixture.
    var hostname = $"cfnet-test-{Guid.NewGuid():N}.{_settings.BaseDomain}";
    var zoneId   = _settings.ZoneId;

    try
    {
      // Act & Assert

      // 1. Attach Custom Domain
      var attachResult = await _sut.AttachCustomDomainAsync(_bucketName, hostname, zoneId);
      attachResult.Should().NotBeNull();
      attachResult.Domain.Should().Be(hostname);

      // 2. Get Custom Domain Status
      var statusResult = await _sut.GetCustomDomainStatusAsync(_bucketName, hostname);
      statusResult.Should().NotBeNull();
      statusResult.Status.Should()
                  .BeOneOf("pending", "active"); // Status depends on timing
    }
    finally
    {
      // 3. Detach Custom Domain (Cleanup)
      var detachAction = async () => await _sut.DetachCustomDomainAsync(_bucketName, hostname);
      await detachAction.Should().NotThrowAsync("the custom domain should be cleaned up successfully");
    }
  }

  /// <summary>Verifies that an R2 bucket can be created and then deleted successfully.</summary>
  [IntegrationTest]
  public async Task CanCreateAndDeleteR2Bucket()
  {
    // Arrange
    var bucketName = $"cfnet-standalone-bucket-{Guid.NewGuid():N}";

    try
    {
      // Act
      var createResult = await _sut.CreateR2BucketAsync(bucketName);

      // Assert
      createResult.Should().NotBeNull();
      createResult.Name.Should().Be(bucketName);
    }
    finally
    {
      // Cleanup
      var deleteAction = async () => await _sut.DeleteR2BucketAsync(bucketName);
      await deleteAction.Should().NotThrowAsync("the bucket should be cleaned up successfully");
    }
  }

  #endregion
}
