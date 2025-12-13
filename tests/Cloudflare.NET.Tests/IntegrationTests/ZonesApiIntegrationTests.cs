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
  ///   Verifies that triggering an activation check on an already-active zone succeeds.
  ///   This is a safe mutation test since activation check is idempotent.
  /// </summary>
  [IntegrationTest]
  public async Task TriggerActivationCheckAsync_OnActiveZone_Succeeds()
  {
    // Arrange
    // Verify zone is active before triggering check.
    var zone = await _sut.GetZoneDetailsAsync(_zoneId);
    zone.Status.Should().Be(ZoneStatus.Active, "activation check test requires an active zone");

    // Act
    var result = await _sut.TriggerActivationCheckAsync(_zoneId);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(_zoneId);
  }

  #endregion


  #region Zone Pause/Unpause Tests

  /// <summary>
  ///   Verifies that zone pause state can be toggled and reverted.
  /// </summary>
  [IntegrationTest]
  public async Task SetZonePausedAsync_CanToggleAndRevert()
  {
    // Arrange - Get current pause state.
    var originalZone    = await _sut.GetZoneDetailsAsync(_zoneId);
    var originalPaused  = originalZone.Paused;
    var testPausedState = !originalPaused; // Toggle to opposite state

    // Act - Toggle to different state.
    var toggledZone = await _sut.SetZonePausedAsync(_zoneId, testPausedState);

    // Assert - Verify the toggle worked.
    toggledZone.Paused.Should().Be(testPausedState, "zone paused state should be toggled");

    // Cleanup - Revert to original state.
    await _sut.SetZonePausedAsync(_zoneId, originalPaused);

    // Verify revert succeeded.
    var revertedZone = await _sut.GetZoneDetailsAsync(_zoneId);
    revertedZone.Paused.Should().Be(originalPaused, "zone should be reverted to original paused state");
  }

  #endregion


  #region Zone Create/Edit/Delete Tests (Skipped - Dangerous Operations)

  /// <summary>
  ///   Verifies that a zone can be created successfully.
  ///   This test creates a new zone and then deletes it as cleanup.
  /// </summary>
  /// <remarks>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/zones/methods/create/</item>
  ///     <item>Full setup: https://developers.cloudflare.com/dns/zone-setups/full-setup/setup/</item>
  ///     <item>Zone requires nameserver delegation at domain registrar</item>
  ///     <item>Domain must be valid TLD from Public Suffix List (PSL)</item>
  ///     <item>Zone remains "pending" until nameservers are delegated</item>
  ///   </list>
  ///   <para>
  ///     <b>Prerequisites:</b> To run this test, you need:
  ///     <list type="bullet">
  ///       <item><description>A domain you own that is not already in Cloudflare</description></item>
  ///       <item><description>An API token with Zone:Edit permission</description></item>
  ///       <item><description>Ability to delegate nameservers at registrar</description></item>
  ///     </list>
  ///   </para>
  /// </remarks>
  [IntegrationTest(Skip = "Requires disposable test domain + nameserver delegation at registrar - Consider Dev account or WireMock")]
  public async Task CreateZoneAsync_ReturnsCreatedZone()
  {
    // Arrange
    // Note: Replace with a test domain you own
    var request = new CreateZoneRequest(
      Name: "test-domain-for-sdk.example",
      Type: ZoneType.Full,
      Account: new ZoneAccountReference(_settings.AccountId));

    // Act
    var result = await _sut.CreateZoneAsync(request);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().NotBeNullOrEmpty();
    result.Name.Should().Be(request.Name);
    result.Status.Should().BeOneOf(ZoneStatus.Pending, ZoneStatus.Active);

    // Cleanup - Delete the zone
    // await _sut.DeleteZoneAsync(result.Id);
  }

  /// <summary>Verifies that a zone can be edited successfully.</summary>
  /// <remarks>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/zones/methods/edit/</item>
  ///     <item>Can modify: paused, plan, type, vanity_name_servers</item>
  ///     <item>Plan changes have billing implications</item>
  ///   </list>
  ///   <para>
  ///     <b>Coverage Note:</b> SetZonePausedAsync_CanToggleAndRevert tests the EditZoneAsync method
  ///     with the paused field, providing integration test coverage for the edit endpoint.
  ///   </para>
  /// </remarks>
  [IntegrationTest(Skip = "Requires disposable test domain - Consider Dev account or WireMock")]
  public async Task EditZoneAsync_ReturnsUpdatedZone()
  {
    // Arrange
    var zone = await _sut.GetZoneDetailsAsync(_zoneId);
    var request = new EditZoneRequest(
      Paused: !zone.Paused,
      VanityNameServers: zone.VanityNameServers);

    // Act
    var result = await _sut.EditZoneAsync(_zoneId, request);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(_zoneId);
    result.Paused.Should().Be(!zone.Paused);

    // Cleanup - Revert the change
    var revertRequest = new EditZoneRequest(Paused: zone.Paused);
    await _sut.EditZoneAsync(_zoneId, revertRequest);
  }

  /// <summary>Verifies that a zone can be deleted successfully.</summary>
  /// <remarks>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/zones/methods/delete/</item>
  ///     <item>Zone deletion is IRREVERSIBLE</item>
  ///     <item>All DNS records, settings, configs permanently lost</item>
  ///     <item>Requires disposable test zone with domain ownership</item>
  ///   </list>
  /// </remarks>
  [IntegrationTest(Skip = "Requires disposable test domain - Zone deletion is DESTRUCTIVE/IRREVERSIBLE - Consider Dev account or WireMock")]
  public async Task DeleteZoneAsync_ReturnsDeleteResult()
  {
    // Arrange
    // First create a zone to delete (requires domain ownership)
    // var createRequest = new CreateZoneRequest(Name: "delete-test.example", Type: ZoneType.Full, Account: new ZoneAccountReference(_settings.AccountId));
    // var zone = await _sut.CreateZoneAsync(createRequest);
    var zoneIdToDelete = "zone-id-from-created-zone";

    // Act
    await _sut.DeleteZoneAsync(zoneIdToDelete);

    // Assert - Verify deletion by trying to get the zone (should throw 404)
    var action = async () => await _sut.GetZoneDetailsAsync(zoneIdToDelete);
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
  }

  #endregion
}
