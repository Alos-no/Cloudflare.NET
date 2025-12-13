namespace Cloudflare.NET.Tests.IntegrationTests;

using System.Net;
using Core.Models;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;
using Zones.CustomHostnames;
using Zones.CustomHostnames.Models;

/// <summary>
///   Contains integration tests for the <see cref="CustomHostnamesApi" /> class. These tests interact with the live
///   Cloudflare API and require credentials and a zone with Cloudflare for SaaS (Custom Hostnames) enabled.
/// </summary>
/// <remarks>
///   <para>
///     <strong>Prerequisites:</strong> These tests require an Enterprise zone with Cloudflare for SaaS enabled. The
///     zone must have a configured fallback origin before custom hostnames can be created.
///   </para>
///   <para>
///     <strong>Test Isolation:</strong> Each test creates unique hostnames using GUIDs to avoid collisions between
///     concurrent test runs. Tests are grouped in a collection to run sequentially, preventing race conditions during
///     pagination when other tests' cleanup deletes hostnames mid-iteration.
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
[Collection(TestCollections.CustomHostnames)]
public class CustomHostnamesApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>, IAsyncLifetime
{
  #region Constants & Statics

  /// <summary>The prefix used for all test custom hostnames to identify them for cleanup.</summary>
  private const string TestHostnamePrefix = "cfnet-ch-test-";

  #endregion

  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly ICustomHostnamesApi _sut;

  /// <summary>The ID of the test zone from configuration.</summary>
  private readonly string _zoneId;

  /// <summary>The settings loaded from the test configuration.</summary>
  private readonly TestCloudflareSettings _settings;

  /// <summary>The unique hostname created for this test run.</summary>
  private readonly string _testHostname;

  /// <summary>The ID of the custom hostname created for the test run (used for basic lifecycle tests).</summary>
  private string? _customHostnameId;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="CustomHostnamesApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public CustomHostnamesApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    // Resolve the SUT and settings from the fixture and configuration.
    _sut      = fixture.ZonesApi.CustomHostnames;
    _settings = TestConfiguration.CloudflareSettings;
    _zoneId   = _settings.ZoneId;

    // Create a unique hostname for this test run. We use a customer's vanity domain pattern.
    // The hostname should NOT be a subdomain of the zone itself, as that would conflict.
    // Use a short GUID segment to keep hostname under 64 chars (avoids Cloudflare Branding requirement).
    var shortGuid = Guid.NewGuid().ToString("N")[..8];
    _testHostname = $"{TestHostnamePrefix}{shortGuid}.customer-test.net";

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion

  #region Methods Impl

  /// <summary>Asynchronously creates a custom hostname required for the tests. This runs once before any tests in the class.</summary>
  public async Task InitializeAsync()
  {
    // Create a custom hostname with basic TXT-based DCV validation.
    // This is the most common validation method for SaaS integrations.
    var sslConfig = new SslConfiguration(
      DcvMethod.Txt,
      CertificateType.Dv
    );

    var request = new CreateCustomHostnameRequest(
      _testHostname,
      sslConfig
    );

    var result = await _sut.CreateAsync(_zoneId, request);
    _customHostnameId = result.Id;
  }

  /// <summary>Asynchronously deletes the custom hostname after all tests in the class have run, ensuring a clean state.</summary>
  public async Task DisposeAsync()
  {
    if (_customHostnameId is not null)
    {
      try
      {
        await _sut.DeleteAsync(_zoneId, _customHostnameId);
      }
      catch (HttpRequestException)
      {
        // Ignore cleanup errors - the resource may already be deleted or in a transitional state.
      }
    }
  }

  #endregion

  #region Methods

  #region Tests - Fallback Origin

  /// <summary>Tests the full fallback origin lifecycle: Create (or get existing), Update, Verify, and Restore/Delete.</summary>
  /// <remarks>
  ///   <para>
  ///     This test handles both scenarios: zones with an existing fallback origin and zones without one. If no fallback
  ///     origin exists, it creates one, tests it, and then deletes it. If one exists, it updates it with a test value and
  ///     restores the original.
  ///   </para>
  ///   <para>
  ///     The fallback origin is optional in Cloudflare for SaaS - it's only needed when custom hostnames don't specify
  ///     their own <c>custom_origin_server</c>.
  ///   </para>
  /// </remarks>
  [IntegrationTest]
  public async Task FallbackOrigin_FullLifecycle()
  {
    // Arrange - Check if a fallback origin already exists.
    string? originalOrigin       = null;
    var     fallbackOriginExists = false;

    try
    {
      var existing = await _sut.GetFallbackOriginAsync(_zoneId);
      originalOrigin       = existing.Origin;
      fallbackOriginExists = true;
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
      // No fallback origin configured - this is fine, we'll create one.
      fallbackOriginExists = false;
    }

    // Use a test origin based on the zone's base domain.
    var testOrigin = $"fallback-test.{_settings.BaseDomain}";

    try
    {
      // Act - Create or Update the fallback origin.
      var updateRequest = new UpdateFallbackOriginRequest(testOrigin);
      var updateResult  = await _sut.UpdateFallbackOriginAsync(_zoneId, updateRequest);

      // Assert - Update was successful.
      updateResult.Should().NotBeNull();
      updateResult.Origin.Should().Be(testOrigin);

      // Act - Verify by getting the fallback origin.
      var getResult = await _sut.GetFallbackOriginAsync(_zoneId);

      // Assert - Get returns the expected value.
      getResult.Should().NotBeNull();
      getResult.Origin.Should().Be(testOrigin);
    }
    finally
    {
      // Cleanup - Restore original or delete if we created it.
      try
      {
        if (fallbackOriginExists && originalOrigin is not null)
        {
          // Restore the original fallback origin.
          var restoreRequest = new UpdateFallbackOriginRequest(originalOrigin);
          await _sut.UpdateFallbackOriginAsync(_zoneId, restoreRequest);
        }
        else
        {
          // We created the fallback origin, so delete it.
          await _sut.DeleteFallbackOriginAsync(_zoneId);
        }
      }
      catch
      {
        // Ignore cleanup errors - manual cleanup may be needed.
      }
    }
  }

  #endregion

  #endregion


  #region Tests - CRUD Lifecycle

  /// <summary>Verifies that a custom hostname can be retrieved after creation.</summary>
  [IntegrationTest]
  public async Task GetAsync_ReturnsCreatedHostname()
  {
    // Arrange
    _customHostnameId.Should().NotBeNullOrWhiteSpace(
      "the custom hostname should have been created in InitializeAsync. " +
      "Ensure the zone has Cloudflare for SaaS enabled.");

    // Act
    var result = await _sut.GetAsync(_zoneId, _customHostnameId!);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(_customHostnameId);
    result.Hostname.Should().Be(_testHostname);
    result.Ssl.Should().NotBeNull();
    result.Ssl.Method.Should().Be(DcvMethod.Txt);
    result.Ssl.Type.Should().Be(CertificateType.Dv);
  }

  /// <summary>Tests the full CRUD lifecycle of a custom hostname: Create, Get, Update (refresh validation), and Delete.</summary>
  [IntegrationTest]
  public async Task CustomHostname_FullLifecycle()
  {
    // Arrange - Create a unique hostname for this specific test.
    // Use a short GUID segment to keep hostname under 64 chars (avoids Cloudflare Branding requirement).
    var shortGuid = Guid.NewGuid().ToString("N")[..8];
    var hostname  = $"{TestHostnamePrefix}lc-{shortGuid}.customer-test.net";
    var sslConfig = new SslConfiguration(
      DcvMethod.Txt,
      CertificateType.Dv,
      new SslSettings(SslToggle.On)
    );
    var     createRequest = new CreateCustomHostnameRequest(hostname, sslConfig);
    string? createdId     = null;

    try
    {
      // Act - Create
      var createResult = await _sut.CreateAsync(_zoneId, createRequest);

      // Assert - Create
      createResult.Should().NotBeNull();
      createResult.Hostname.Should().Be(hostname);
      createResult.Ssl.Should().NotBeNull();
      createResult.Ssl.Method.Should().Be(DcvMethod.Txt);
      createdId = createResult.Id;

      // Act - Get
      var getResult = await _sut.GetAsync(_zoneId, createdId);

      // Assert - Get
      getResult.Should().NotBeNull();
      getResult.Id.Should().Be(createdId);
      getResult.Hostname.Should().Be(hostname);

      // Act - Update (this triggers a validation refresh)
      var updateRequest = new UpdateCustomHostnameRequest(
        new SslConfiguration(
          DcvMethod.Txt,
          CertificateType.Dv,
          new SslSettings(MinTlsVersion: MinTlsVersion.Tls12)
        )
      );
      var updateResult = await _sut.UpdateAsync(_zoneId, createdId, updateRequest);

      // Assert - Update
      updateResult.Should().NotBeNull();
      updateResult.Id.Should().Be(createdId);

      // Act - Delete
      await _sut.DeleteAsync(_zoneId, createdId);
      createdId = null; // Mark as deleted to skip cleanup

      // Assert - Verify deletion by attempting to get (should throw 404)
      var getDeletedAction = async () => await _sut.GetAsync(_zoneId, updateResult.Id);
      var ex               = await getDeletedAction.Should().ThrowAsync<HttpRequestException>();
      ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    finally
    {
      // Cleanup if the test failed before deletion.
      if (createdId is not null)
      {
        try
        {
          await _sut.DeleteAsync(_zoneId, createdId);
        }
        catch
        {
          // Ignore cleanup errors.
        }
      }
    }
  }

  /// <summary>Verifies that a custom hostname can be created with a custom origin server configuration.</summary>
  [IntegrationTest]
  public async Task CreateAsync_WithCustomOriginServer_SetsOriginCorrectly()
  {
    // Arrange
    // Use a short GUID segment to keep hostname under 64 chars (avoids Cloudflare Branding requirement).
    var shortGuid = Guid.NewGuid().ToString("N")[..8];
    var hostname  = $"{TestHostnamePrefix}{shortGuid}.customer-test.net";
    // Use the zone's base domain for the custom origin server (not example.com which is prohibited).
    var customOriginServer = $"origin.{_settings.BaseDomain}";
    var sslConfig          = new SslConfiguration(DcvMethod.Txt, CertificateType.Dv);
    var request = new CreateCustomHostnameRequest(
      hostname,
      sslConfig,
      CustomOriginServer: customOriginServer
    );
    string? createdId = null;

    try
    {
      // Act
      var result = await _sut.CreateAsync(_zoneId, request);
      createdId = result.Id;

      // Assert
      result.Should().NotBeNull();
      result.Hostname.Should().Be(hostname);
      result.CustomOriginServer.Should().Be(customOriginServer);
    }
    finally
    {
      // Cleanup
      if (createdId is not null)
      {
        try
        {
          await _sut.DeleteAsync(_zoneId, createdId);
        }
        catch
        {
          // Ignore cleanup errors.
        }
      }
    }
  }

  /// <summary>
  ///   Verifies that a custom hostname can be created with minimal SslConfiguration (null Settings, null BundleMethod, etc).
  ///   This tests that nullable properties in SslConfiguration are handled correctly by the API.
  /// </summary>
  [IntegrationTest]
  public async Task CreateAsync_MinimalSslConfiguration_Succeeds()
  {
    // Arrange - Create a unique hostname for this test
    var shortGuid = Guid.NewGuid().ToString("N")[..8];
    var hostname = $"{TestHostnamePrefix}min-{shortGuid}.customer-test.net";

    // Minimal SslConfiguration - only Method and Type, all optional properties null
    var sslConfig = new SslConfiguration(
      DcvMethod.Txt,
      CertificateType.Dv
      // Settings: null (default)
      // BundleMethod: null (default)
      // Wildcard: null (default)
      // CertificateAuthority: null (default)
    );
    var request = new CreateCustomHostnameRequest(hostname, sslConfig);
    string? createdId = null;

    try
    {
      // Act - This should succeed with minimal SslConfiguration
      var result = await _sut.CreateAsync(_zoneId, request);
      createdId = result.Id;

      // Assert
      result.Should().NotBeNull();
      result.Hostname.Should().Be(hostname);
      result.Ssl.Should().NotBeNull();
      result.Ssl.Method.Should().Be(DcvMethod.Txt);
      result.Ssl.Type.Should().Be(CertificateType.Dv);
    }
    finally
    {
      // Cleanup
      if (createdId is not null)
      {
        try
        {
          await _sut.DeleteAsync(_zoneId, createdId);
        }
        catch
        {
          // Ignore cleanup errors.
        }
      }
    }
  }

  #endregion


  #region Tests - Listing

  /// <summary>Verifies that the ListAsync method returns the created custom hostname with proper pagination information.</summary>
  [IntegrationTest]
  public async Task ListAsync_ReturnsCreatedHostname()
  {
    // Arrange
    _customHostnameId.Should().NotBeNullOrWhiteSpace(
      "the custom hostname should have been created in InitializeAsync. " +
      "Ensure the zone has Cloudflare for SaaS enabled.");

    var filters = new ListCustomHostnamesFilters { PerPage = 50 };

    // Act
    var result = await _sut.ListAsync(_zoneId, filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeEmpty();
    result.PageInfo.Should().NotBeNull();
    result.PageInfo!.Page.Should().BeGreaterThanOrEqualTo(1);
    result.PageInfo.PerPage.Should().Be(50);

    // Verify our test hostname is in the results.
    result.Items.Should().Contain(h => h.Id == _customHostnameId);
  }

  /// <summary>Verifies that the ListAsync method supports filtering by hostname.</summary>
  [IntegrationTest]
  public async Task ListAsync_WithHostnameFilter_ReturnsMatchingHostnames()
  {
    // Arrange
    _customHostnameId.Should().NotBeNullOrWhiteSpace(
      "the custom hostname should have been created in InitializeAsync. " +
      "Ensure the zone has Cloudflare for SaaS enabled.");

    // Filter by the exact hostname.
    var filters = new ListCustomHostnamesFilters { Hostname = _testHostname };

    // Act - Retry to handle eventual consistency. The Cloudflare listing API
    // may not immediately reflect newly created hostnames.
    const int maxRetries = 10;
    PagePaginatedResult<CustomHostname>? result = null;

    for (var attempt = 0; attempt < maxRetries; attempt++)
    {
      result = await _sut.ListAsync(_zoneId, filters);

      if (result.Items.Count >= 1)
        break;

      await Task.Delay(TimeSpan.FromSeconds(1));
    }

    // Assert
    result.Should().NotBeNull();
    result!.Items.Should().HaveCount(1, $"expected 1 hostname after {maxRetries} retries for eventual consistency");
    result.Items[0].Id.Should().Be(_customHostnameId);
    result.Items[0].Hostname.Should().Be(_testHostname);
  }

  /// <summary>
  ///   Verifies that ListAllAsync correctly handles pagination by creating multiple hostnames and iterating through
  ///   them with a small per-page limit.
  /// </summary>
  [IntegrationTest]
  public async Task ListAllAsync_HandlesMultiplePages()
  {
    // Arrange: Create enough hostnames to guarantee pagination.
    // Use a short GUID segment to keep hostname under 64 chars (avoids Cloudflare Branding requirement).
    const int        hostnamesToCreate = 3;
    var              createdIds        = new List<string>();
    var              shortGuid         = Guid.NewGuid().ToString("N")[..8];
    SslConfiguration sslConfig         = new(DcvMethod.Txt, CertificateType.Dv);

    try
    {
      for (var i = 0; i < hostnamesToCreate; i++)
      {
        var hostname = $"{TestHostnamePrefix}pg{i}-{shortGuid}.customer-test.net";
        var request  = new CreateCustomHostnameRequest(hostname, sslConfig);
        var result   = await _sut.CreateAsync(_zoneId, request);
        createdIds.Add(result.Id);
      }

      // Act: List all with a small per-page limit to force pagination.
      // We don't use the hostname filter because Cloudflare validates it as a hostname
      // and our test prefix ends with '-' which is invalid. Instead, we filter client-side
      // for hostnames from this specific test run (matching the unique shortGuid).
      // Note: The SDK's ListAllAsync method handles deduplication internally, so we can
      // safely use a simple list here.
      var filters      = new ListCustomHostnamesFilters { PerPage = 2 };
      var allHostnames = new List<CustomHostname>();

      // Retry listing until we find all expected hostnames or timeout.
      // Cloudflare's listing API may not immediately reflect newly created hostnames.
      const int maxRetries = 10;

      for (var attempt = 0; attempt < maxRetries; attempt++)
      {
        allHostnames.Clear();

        await foreach (var hostname in _sut.ListAllAsync(_zoneId, filters))
        {
          // Only include hostnames from this specific test run (matching the shortGuid).
          if (hostname.Hostname.Contains(shortGuid))
          {
            allHostnames.Add(hostname);
          }
        }

        if (allHostnames.Count >= hostnamesToCreate)
          break;

        await Task.Delay(TimeSpan.FromSeconds(1));
      }

      // Assert: All created hostnames should be present.
      allHostnames.Should().HaveCount(hostnamesToCreate,
                                      $"expected all {hostnamesToCreate} hostnames to be found after {maxRetries} retries");
      allHostnames.Select(h => h.Id).Should().BeEquivalentTo(createdIds);
    }
    finally
    {
      // Cleanup: Delete all created hostnames.
      foreach (var id in createdIds)
      {
        try
        {
          await _sut.DeleteAsync(_zoneId, id);
        }
        catch
        {
          // Ignore cleanup errors.
        }
      }
    }
  }

  /// <summary>Verifies that ListAsync supports filtering by SSL status.</summary>
  [IntegrationTest]
  public async Task ListAsync_WithSslFilter_ReturnsMatchingHostnames()
  {
    // Arrange
    _customHostnameId.Should().NotBeNullOrWhiteSpace(
      "the custom hostname should have been created in InitializeAsync. " +
      "Ensure the zone has Cloudflare for SaaS enabled.");

    // Filter by SSL status - newly created hostnames are typically pending validation.
    var filters = new ListCustomHostnamesFilters { Ssl = SslStatus.PendingValidation };

    // Act
    var result = await _sut.ListAsync(_zoneId, filters);

    // Assert
    result.Should().NotBeNull();
    // The test hostname should be in the pending validation state.
    result.Items.Should().Contain(h => h.Id == _customHostnameId);
  }

  #endregion


  #region Tests - Error Handling

  /// <summary>
  ///   Verifies that attempting to get a non-existent custom hostname throws a Bad Request exception. Note: Unlike
  ///   other Cloudflare APIs, the Custom Hostnames API returns 400 (not 404) with error code 1435 "The custom hostname ID is
  ///   invalid" for non-existent IDs.
  /// </summary>
  [IntegrationTest]
  public async Task GetAsync_WhenHostnameDoesNotExist_ThrowsBadRequest()
  {
    // Arrange - Use a valid UUID format that doesn't exist.
    var nonExistentId = "00000000-0000-0000-0000-000000000000";

    // Act
    var action = async () => await _sut.GetAsync(_zoneId, nonExistentId);

    // Assert - Custom Hostnames API returns 400 Bad Request for invalid/non-existent IDs.
    var ex = await action.Should().ThrowAsync<HttpRequestException>();
    ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  /// <summary>
  ///   Verifies that attempting to delete a non-existent custom hostname throws a Bad Request exception. Note: Unlike
  ///   other Cloudflare APIs, the Custom Hostnames API returns 400 (not 404) with error code 1435 "The custom hostname ID is
  ///   invalid" for non-existent IDs.
  /// </summary>
  [IntegrationTest]
  public async Task DeleteAsync_WhenHostnameDoesNotExist_ThrowsBadRequest()
  {
    // Arrange - Use a valid UUID format that doesn't exist.
    var nonExistentId = "00000000-0000-0000-0000-000000000000";

    // Act
    var action = async () => await _sut.DeleteAsync(_zoneId, nonExistentId);

    // Assert - Custom Hostnames API returns 400 Bad Request for invalid/non-existent IDs.
    var ex = await action.Should().ThrowAsync<HttpRequestException>();
    ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  /// <summary>Verifies that creating a custom hostname with an invalid hostname format throws a bad request exception.</summary>
  [IntegrationTest]
  public async Task CreateAsync_WithInvalidHostname_ThrowsBadRequest()
  {
    // Arrange - Invalid hostname (no dots, which is not a valid FQDN).
    var invalidHostname = "invalid-hostname-without-domain";
    var sslConfig       = new SslConfiguration(DcvMethod.Txt, CertificateType.Dv);
    var request         = new CreateCustomHostnameRequest(invalidHostname, sslConfig);

    // Act
    var action = async () => await _sut.CreateAsync(_zoneId, request);

    // Assert
    await action.Should().ThrowAsync<HttpRequestException>();
  }

  #endregion
}
