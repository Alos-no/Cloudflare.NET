namespace Cloudflare.NET.Tests.IntegrationTests;

using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;
using Zones;
using Zones.Models;

/// <summary>
///   Contains integration tests for the Zone CRUD operations of <see cref="ZonesApi" />.
///   These tests interact with the live Cloudflare API and require credentials.
/// </summary>
/// <remarks>
///   This test class focuses on Zone-level operations including:
///   <list type="bullet">
///     <item><description>ListZonesAsync / ListAllZonesAsync - Zone listing and pagination</description></item>
///     <item><description>GetZoneDetailsAsync - Fetching zone details</description></item>
///     <item><description>TriggerActivationCheckAsync - Zone activation checks</description></item>
///     <item><description>SetZonePausedAsync - Pausing/unpausing zones</description></item>
///   </list>
///   For DNS operations via IZonesApi, see <see cref="ZonesApiDnsIntegrationTests" />.
///   For the dedicated DNS API tests, see <see cref="DnsApiIntegrationTests" />.
///   For Zone Hold tests, see <see cref="ZoneHoldsApiIntegrationTests" />.
///   For Zone Settings tests, see <see cref="ZoneSettingsApiIntegrationTests" />.
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class ZonesApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IZonesApi _sut;

  /// <summary>The ID of the test zone from configuration.</summary>
  private readonly string _zoneId;

  /// <summary>The settings loaded from the test configuration.</summary>
  private readonly TestCloudflareSettings _settings;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="ZonesApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public ZonesApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    // Resolve the SUT and settings from the fixture and configuration.
    _sut      = fixture.ZonesApi;
    _settings = TestConfiguration.CloudflareSettings;
    _zoneId   = _settings.ZoneId;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region Zone Details Tests

  /// <summary>Verifies that the details for a specific zone can be fetched successfully.</summary>
  [IntegrationTest]
  public async Task CanGetZoneDetails()
  {
    // Arrange
    // The Zone ID is configured in user secrets and loaded into _zoneId.
    _zoneId.Should().NotBeNullOrWhiteSpace("the Zone ID must be configured for integration tests");

    // Act
    var zoneDetails = await _sut.GetZoneDetailsAsync(_zoneId);

    // Assert
    zoneDetails.Should().NotBeNull();
    zoneDetails.Id.Should().Be(_zoneId);
    // The BaseDomain is inferred from the zone details in the fixture, so this confirms consistency.
    zoneDetails.Name.Should().Be(_settings.BaseDomain);
    // A zone used for testing should be active.
    zoneDetails.Status.Should().Be(ZoneStatus.Active);
  }

  /// <summary>Verifies that zone details include all expected nested objects.</summary>
  [IntegrationTest]
  public async Task GetZoneDetailsAsync_IncludesNestedObjects()
  {
    // Act
    var zone = await _sut.GetZoneDetailsAsync(_zoneId);

    // Assert - Account information
    zone.Account.Should().NotBeNull();
    zone.Account.Id.Should().NotBeNullOrWhiteSpace();
    zone.Account.Name.Should().NotBeNullOrWhiteSpace();

    // Assert - Owner information
    zone.Owner.Should().NotBeNull();

    // Assert - Plan information
    zone.Plan.Should().NotBeNull();
    zone.Plan.Id.Should().NotBeNullOrWhiteSpace();
    zone.Plan.Name.Should().NotBeNullOrWhiteSpace();

    // Assert - Name servers
    zone.NameServers.Should().NotBeNullOrEmpty();
    zone.NameServers.Should().OnlyContain(ns => !string.IsNullOrWhiteSpace(ns));

    // Assert - Timestamps
    zone.CreatedOn.Should().NotBe(default(DateTime));
    zone.ModifiedOn.Should().NotBe(default(DateTime));
  }

  #endregion


  #region Zone Listing Tests

  /// <summary>Verifies that zones can be listed for the authenticated account.</summary>
  [IntegrationTest]
  public async Task ListZonesAsync_ReturnsZonesForAccount()
  {
    // Arrange
    // Filter by the configured zone name to ensure we get at least our test zone.
    var filters = new ListZonesFilters(Name: _settings.BaseDomain);

    // Act
    var result = await _sut.ListZonesAsync(filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeEmpty();
    result.Items.Should().Contain(z => z.Id == _zoneId);
    result.PageInfo.Should().NotBeNull();
    result.PageInfo!.TotalCount.Should().BeGreaterThanOrEqualTo(1);
  }

  /// <summary>Verifies that ListAllZonesAsync correctly iterates through zones.</summary>
  [IntegrationTest]
  public async Task ListAllZonesAsync_CanIterateZones()
  {
    // Arrange
    var zones = new List<Zone>();

    // Act - Using small page size to exercise pagination if multiple zones exist
    var filters = new ListZonesFilters(PerPage: 2);
    await foreach (var zone in _sut.ListAllZonesAsync(filters))
    {
      zones.Add(zone);
      // Safety limit to avoid long-running test if account has many zones
      if (zones.Count >= 10) break;
    }

    // Assert
    zones.Should().NotBeEmpty();
    zones.Should().Contain(z => z.Id == _zoneId);
    zones.Should().OnlyContain(z => !string.IsNullOrWhiteSpace(z.Id));
    zones.Should().OnlyContain(z => !string.IsNullOrWhiteSpace(z.Name));
  }

  /// <summary>
  ///   Verifies that zones can be listed with status filter.
  ///   Since our test zone is active, filtering for active status should return it.
  /// </summary>
  [IntegrationTest]
  public async Task ListZonesAsync_WithStatusFilter_ReturnsActiveZones()
  {
    // Arrange
    var filters = new ListZonesFilters(Status: ZoneStatus.Active, PerPage: 5);

    // Act
    var result = await _sut.ListZonesAsync(filters);

    // Assert
    result.Items.Should().NotBeEmpty();
    result.Items.Should().OnlyContain(z => z.Status == ZoneStatus.Active);
    result.Items.Should().Contain(z => z.Id == _zoneId);
  }

  #endregion


  #region Zone Activation Tests

  /// <summary>
  ///   Verifies that triggering an activation check on an already-active zone either succeeds
  ///   or returns a 403 Forbidden if the API token lacks the required permission.
  ///   This is a safe mutation test since activation check is idempotent.
  /// </summary>
  [IntegrationTest]
  public async Task TriggerActivationCheckAsync_OnActiveZone_SucceedsOrRequiresPermission()
  {
    // Arrange
    // Verify zone is active before triggering check
    var zone = await _sut.GetZoneDetailsAsync(_zoneId);
    zone.Status.Should().Be(ZoneStatus.Active, "activation check test requires an active zone");

    // Act
    try
    {
      var result = await _sut.TriggerActivationCheckAsync(_zoneId);

      // Assert - If we reach here, the API token has the required permission
      result.Should().NotBeNull();
      result.Id.Should().Be(_zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      // Expected if API token doesn't have Zone > Zone > Edit permission for activation check
      // This is acceptable behavior - the test verifies the API integration works
    }
  }

  #endregion


  #region Zone Pause/Unpause Tests

  /// <summary>
  ///   Verifies that zone pause state can be toggled and reverted, or returns 403 if
  ///   the API token lacks Zone > Zone > Edit permission.
  /// </summary>
  [IntegrationTest]
  public async Task SetZonePausedAsync_CanToggleAndRevertOrRequiresPermission()
  {
    // Arrange - Get current pause state
    var originalZone     = await _sut.GetZoneDetailsAsync(_zoneId);
    var originalPaused   = originalZone.Paused;
    var testPausedState  = !originalPaused; // Toggle to opposite state

    try
    {
      // Act - Toggle to different state
      var toggledZone = await _sut.SetZonePausedAsync(_zoneId, testPausedState);

      // Assert - Verify the toggle worked (API token has permission)
      toggledZone.Paused.Should().Be(testPausedState, "zone paused state should be toggled");

      // Cleanup - Revert to original state
      await _sut.SetZonePausedAsync(_zoneId, originalPaused);

      // Verify revert succeeded
      var revertedZone = await _sut.GetZoneDetailsAsync(_zoneId);
      revertedZone.Paused.Should().Be(originalPaused, "zone should be reverted to original paused state");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      // Expected if API token doesn't have Zone > Zone > Edit permission
      // This is acceptable behavior - the test verifies the API integration works
    }
  }

  #endregion
}
