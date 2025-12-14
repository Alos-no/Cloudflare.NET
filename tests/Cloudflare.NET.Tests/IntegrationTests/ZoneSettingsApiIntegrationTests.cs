namespace Cloudflare.NET.Tests.IntegrationTests;

using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;
using Zones;
using Zones.CustomHostnames.Models;
using Zones.Models;

/// <summary>
///   Contains integration tests for the Zone Settings operations of <see cref="ZonesApi" />. These tests interact with the
///   live Cloudflare API and require credentials.
/// </summary>
/// <remarks>
///   This test class focuses on Zone Settings operations including:
///   <list type="bullet">
///     <item><description>GetZoneSettingAsync - Get a specific zone setting</description></item>
///     <item><description>SetZoneSettingAsync - Update a zone setting value</description></item>
///   </list>
///   For Zone CRUD integration tests, see <see cref="ZonesApiIntegrationTests" />.
///   For Zone Hold integration tests, see <see cref="ZoneHoldsApiIntegrationTests" />.
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class ZoneSettingsApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IZonesApi _sut;

  /// <summary>The ID of the test zone from configuration.</summary>
  private readonly string _zoneId;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="ZoneSettingsApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public ZoneSettingsApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    // Resolve the SUT and settings from the fixture and configuration.
    _sut = fixture.ZonesApi;
    var settings = TestConfiguration.CloudflareSettings;
    _zoneId = settings.ZoneId;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion

  #region Zone Settings Integration Tests

  /// <summary>
  ///   I01: Verifies that a string-based zone setting can be retrieved.
  ///   Uses SSL mode as the test setting since it's available on all plans.
  /// </summary>
  [IntegrationTest]
  public async Task GetZoneSettingAsync_SslSetting_ReturnsStringValue()
  {
    // Act
    var setting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.Ssl);

    // Assert
    setting.Should().NotBeNull();
    setting.Id.Value.Should().Be("ssl");
    setting.Value.ValueKind.Should().Be(System.Text.Json.JsonValueKind.String);
    var sslMode = setting.Value.GetString();
    sslMode.Should().BeOneOf(EnumTestHelpers.GetAllValues<SslMode>());
  }

  /// <summary>
  ///   I02: Verifies that an integer-based zone setting can be retrieved.
  ///   Uses browser cache TTL as the test setting.
  /// </summary>
  [IntegrationTest]
  public async Task GetZoneSettingAsync_BrowserCacheTtl_ReturnsIntegerValue()
  {
    // Act
    var setting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.BrowserCacheTtl);

    // Assert
    setting.Should().NotBeNull();
    setting.Id.Value.Should().Be("browser_cache_ttl");
    setting.Value.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Number);
    var ttlValue = setting.Value.GetInt32();
    ttlValue.Should().BeGreaterThanOrEqualTo(0, "TTL should be 0 or positive");
  }

  /// <summary>
  ///   I03: Verifies that an on/off toggle setting can be retrieved.
  ///   Uses always_use_https as the test setting.
  /// </summary>
  [IntegrationTest]
  public async Task GetZoneSettingAsync_AlwaysUseHttps_ReturnsOnOffValue()
  {
    // Act
    var setting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.AlwaysUseHttps);

    // Assert
    setting.Should().NotBeNull();
    setting.Id.Value.Should().Be("always_use_https");
    var toggleValue = setting.Value.GetString();
    toggleValue.Should().BeOneOf(EnumTestHelpers.GetAllValues<SslToggle>());
  }

  /// <summary>
  ///   I04: Verifies that the editable property is correctly populated.
  /// </summary>
  [IntegrationTest]
  public async Task GetZoneSettingAsync_ChecksEditableProperty()
  {
    // Act
    var setting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.Ssl);

    // Assert
    // SSL setting should be editable on most plans
    setting.Editable.Should().BeTrue("SSL setting should be editable");
  }

  /// <summary>
  ///   I05: Verifies that using a constant from ZoneSettingId works correctly.
  /// </summary>
  [IntegrationTest]
  public async Task GetZoneSettingAsync_UsingConstant_WorksCorrectly()
  {
    // Act
    var setting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.MinTlsVersion);

    // Assert
    setting.Should().NotBeNull();
    setting.Id.Value.Should().Be("min_tls_version");
    var tlsVersion = setting.Value.GetString();
    tlsVersion.Should().BeOneOf(EnumTestHelpers.GetAllValues<MinTlsVersion>());
  }

  /// <summary>
  ///   I06: Verifies that a string-based setting can be updated and reverted.
  ///   Uses min_tls_version as the test setting.
  /// </summary>
  [IntegrationTest]
  public async Task SetZoneSettingAsync_MinTlsVersion_CanUpdateAndRevert()
  {
    // Arrange - Get current value
    var originalSetting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.MinTlsVersion);
    var originalValue = originalSetting.Value.GetString();

    // Choose a different valid value to set
    var newValue = originalValue == "1.2" ? "1.0" : "1.2";

    // Act - Update to new value
    var updatedSetting = await _sut.SetZoneSettingAsync(_zoneId, ZoneSettingId.MinTlsVersion, newValue);

    // Assert
    updatedSetting.Should().NotBeNull();
    updatedSetting.Value.GetString().Should().Be(newValue);

    // Cleanup - Revert to original
    await _sut.SetZoneSettingAsync(_zoneId, ZoneSettingId.MinTlsVersion, originalValue!);

    // Verify revert
    var revertedSetting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.MinTlsVersion);
    revertedSetting.Value.GetString().Should().Be(originalValue);
  }

  /// <summary>
  ///   I07: Verifies that an integer-based setting can be updated and reverted.
  ///   Uses browser_cache_ttl as the test setting.
  /// </summary>
  [IntegrationTest]
  public async Task SetZoneSettingAsync_BrowserCacheTtl_CanUpdateAndRevert()
  {
    // Arrange - Get current value
    var originalSetting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.BrowserCacheTtl);
    var originalValue = originalSetting.Value.GetInt32();

    // Choose a different valid value (common TTL values)
    var newValue = originalValue == 14400 ? 7200 : 14400;

    // Act - Update to new value
    var updatedSetting = await _sut.SetZoneSettingAsync(_zoneId, ZoneSettingId.BrowserCacheTtl, newValue);

    // Assert
    updatedSetting.Should().NotBeNull();
    updatedSetting.Value.GetInt32().Should().Be(newValue);

    // Cleanup - Revert to original
    await _sut.SetZoneSettingAsync(_zoneId, ZoneSettingId.BrowserCacheTtl, originalValue);

    // Verify revert
    var revertedSetting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.BrowserCacheTtl);
    revertedSetting.Value.GetInt32().Should().Be(originalValue);
  }

  /// <summary>
  ///   I08: Verifies that an on/off toggle setting can be updated and reverted.
  ///   Uses brotli as the test setting.
  /// </summary>
  [IntegrationTest]
  public async Task SetZoneSettingAsync_Brotli_CanToggleAndRevert()
  {
    // Arrange - Get current value
    var originalSetting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.Brotli);
    var originalValue = originalSetting.Value.GetString();

    // Toggle to opposite value
    var newValue = originalValue == "on" ? "off" : "on";

    // Act - Toggle
    var toggledSetting = await _sut.SetZoneSettingAsync(_zoneId, ZoneSettingId.Brotli, newValue);

    // Assert
    toggledSetting.Should().NotBeNull();
    toggledSetting.Value.GetString().Should().Be(newValue);

    // Cleanup - Revert
    await _sut.SetZoneSettingAsync(_zoneId, ZoneSettingId.Brotli, originalValue!);

    // Verify revert
    var revertedSetting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.Brotli);
    revertedSetting.Value.GetString().Should().Be(originalValue);
  }

  /// <summary>
  ///   I09: Verifies that setting a value returns the updated setting with correct metadata.
  /// </summary>
  [IntegrationTest]
  public async Task SetZoneSettingAsync_ReturnsUpdatedMetadata()
  {
    // Arrange
    var originalSetting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.AlwaysOnline);
    var originalValue = originalSetting.Value.GetString();
    var newValue = originalValue == "on" ? "off" : "on";

    // Act
    var updatedSetting = await _sut.SetZoneSettingAsync(_zoneId, ZoneSettingId.AlwaysOnline, newValue);

    // Assert
    updatedSetting.Id.Value.Should().Be("always_online");
    updatedSetting.Editable.Should().BeTrue();
    updatedSetting.ModifiedOn.Should().NotBeNull("modified_on should be updated");
    updatedSetting.ModifiedOn!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));

    // Cleanup
    await _sut.SetZoneSettingAsync(_zoneId, ZoneSettingId.AlwaysOnline, originalValue!);
  }

  /// <summary>
  ///   I10: Verifies that development mode can be enabled.
  ///   Development mode is a special toggle that auto-disables after 3 hours.
  /// </summary>
  [IntegrationTest]
  public async Task SetZoneSettingAsync_DevelopmentMode_CanEnable()
  {
    // Arrange
    var originalSetting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.DevelopmentMode);

    // Act - Enable development mode
    var updatedSetting = await _sut.SetZoneSettingAsync(_zoneId, ZoneSettingId.DevelopmentMode, "on");

    // Assert
    updatedSetting.Value.GetString().Should().Be("on");

    // Cleanup - Disable development mode
    await _sut.SetZoneSettingAsync(_zoneId, ZoneSettingId.DevelopmentMode, "off");
  }

  /// <summary>
  ///   I11: Verifies that development mode can be disabled.
  /// </summary>
  [IntegrationTest]
  public async Task SetZoneSettingAsync_DevelopmentMode_CanDisable()
  {
    // Arrange
    var originalSetting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.DevelopmentMode);

    // Act - Ensure development mode is off
    var updatedSetting = await _sut.SetZoneSettingAsync(_zoneId, ZoneSettingId.DevelopmentMode, "off");

    // Assert
    updatedSetting.Value.GetString().Should().Be("off");
  }

  /// <summary>
  ///   I12: Verifies reading development mode includes all expected fields.
  /// </summary>
  [IntegrationTest]
  public async Task GetZoneSettingAsync_DevelopmentMode_IncludesAllFields()
  {
    // Act
    var setting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.DevelopmentMode);

    // Assert
    setting.Should().NotBeNull();
    setting.Id.Value.Should().Be("development_mode");
    setting.Value.GetString().Should().BeOneOf(EnumTestHelpers.GetAllValues<SslToggle>());
    setting.Editable.Should().BeTrue("development mode should be editable");
  }

  /// <summary>
  ///   I13: Verifies that SSL mode can be read correctly.
  /// </summary>
  [IntegrationTest]
  public async Task GetZoneSettingAsync_SslMode_ReturnsValidMode()
  {
    // Act
    var setting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.Ssl);

    // Assert
    setting.Should().NotBeNull();
    var sslMode = setting.Value.GetString();
    sslMode.Should().BeOneOf(EnumTestHelpers.GetAllValues<SslMode>(),
      "SSL mode should be one of the valid Cloudflare SSL modes");
  }

  /// <summary>
  ///   I14: Verifies that TLS 1.3 setting can be read.
  /// </summary>
  [IntegrationTest]
  public async Task GetZoneSettingAsync_Tls13_ReturnsValidValue()
  {
    // Act
    var setting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.Tls13);

    // Assert
    setting.Should().NotBeNull();
    setting.Id.Value.Should().Be("tls_1_3");
    var tls13Value = setting.Value.GetString();
    tls13Value.Should().BeOneOf(EnumTestHelpers.GetAllValues<Tls13Setting>());
  }

  /// <summary>
  ///   I15: Verifies that security level can be read.
  /// </summary>
  [IntegrationTest]
  public async Task GetZoneSettingAsync_SecurityLevel_ReturnsValidLevel()
  {
    // Act
    var setting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.SecurityLevel);

    // Assert
    setting.Should().NotBeNull();
    setting.Id.Value.Should().Be("security_level");
    var level = setting.Value.GetString();
    level.Should().BeOneOf(EnumTestHelpers.GetAllValues<ZoneSecurityLevel>());
  }

  /// <summary>
  ///   I16: Verifies that requesting an unknown setting ID returns HTTP 400 BadRequest.
  /// </summary>
  /// <remarks>
  ///   The Cloudflare API validates setting IDs against a known list and returns error code 1003
  ///   "Undefined zone setting: {setting_id}" with HTTP 400 BadRequest for invalid setting names.
  /// </remarks>
  [IntegrationTest]
  public async Task GetZoneSettingAsync_UnknownSettingId_ThrowsBadRequest()
  {
    // Arrange
    var unknownSettingId = "this_setting_does_not_exist";

    // Act
    var action = async () => await _sut.GetZoneSettingAsync(_zoneId, unknownSettingId);

    // Assert - API returns 400 BadRequest for undefined settings (error code 1003)
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.BadRequest);
  }

  /// <summary>
  ///   I17: Verifies that setting an invalid value throws an error.
  /// </summary>
  [IntegrationTest]
  public async Task SetZoneSettingAsync_InvalidValue_ThrowsError()
  {
    // Arrange - SSL mode only accepts specific values
    var invalidSslMode = "not_a_valid_mode";

    // Act
    var action = async () => await _sut.SetZoneSettingAsync(_zoneId, ZoneSettingId.Ssl, invalidSslMode);

    // Assert - API should reject the invalid value
    await action.Should().ThrowAsync<Exception>();
  }

  /// <summary>
  ///   I18: Verifies that multiple settings can be read in sequence.
  /// </summary>
  [IntegrationTest]
  public async Task GetZoneSettingAsync_MultipleSettings_AllReturn()
  {
    // Act
    var sslSetting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.Ssl);
    var tlsSetting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.MinTlsVersion);
    var cacheSetting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.BrowserCacheTtl);

    // Assert
    sslSetting.Should().NotBeNull();
    sslSetting.Id.Value.Should().Be("ssl");

    tlsSetting.Should().NotBeNull();
    tlsSetting.Id.Value.Should().Be("min_tls_version");

    cacheSetting.Should().NotBeNull();
    cacheSetting.Id.Value.Should().Be("browser_cache_ttl");
  }

  /// <summary>
  ///   I19: Verifies that getting a setting for a non-existent zone returns HTTP 403 Forbidden.
  /// </summary>
  /// <remarks>
  ///   Using a valid format but non-existent zone ID (32-character hex) returns 403 Forbidden.
  ///   This is a deliberate security measure to prevent zone enumeration attacks - the API
  ///   does not reveal whether a zone exists or not.
  /// </remarks>
  [IntegrationTest]
  public async Task GetZoneSettingAsync_NonExistentZone_ThrowsForbidden()
  {
    // Arrange - Valid format but non-existent zone ID
    var nonExistentZoneId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.GetZoneSettingAsync(nonExistentZoneId, ZoneSettingId.Ssl);

    // Assert - Returns 403 to prevent zone enumeration attacks
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.Forbidden);
  }

  /// <summary>
  ///   I20: Verifies that websockets setting can be read (common setting).
  /// </summary>
  [IntegrationTest]
  public async Task GetZoneSettingAsync_Websockets_ReturnsOnOffValue()
  {
    // Act
    var setting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.Websockets);

    // Assert
    setting.Should().NotBeNull();
    setting.Id.Value.Should().Be("websockets");
    setting.Value.GetString().Should().BeOneOf(EnumTestHelpers.GetAllValues<SslToggle>());
  }

  /// <summary>
  ///   I21: Verifies that IPv6 setting can be read.
  /// </summary>
  [IntegrationTest]
  public async Task GetZoneSettingAsync_Ipv6_ReturnsOnOffValue()
  {
    // Act
    var setting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.Ipv6);

    // Assert
    setting.Should().NotBeNull();
    setting.Id.Value.Should().Be("ipv6");
    setting.Value.GetString().Should().BeOneOf(EnumTestHelpers.GetAllValues<SslToggle>());
  }

  /// <summary>
  ///   I22: Verifies that 0-RTT setting can be read.
  /// </summary>
  [IntegrationTest]
  public async Task GetZoneSettingAsync_ZeroRtt_ReturnsOnOffValue()
  {
    // Act
    var setting = await _sut.GetZoneSettingAsync(_zoneId, ZoneSettingId.ZeroRtt);

    // Assert
    setting.Should().NotBeNull();
    setting.Id.Value.Should().Be("0rtt");
    setting.Value.GetString().Should().BeOneOf(EnumTestHelpers.GetAllValues<SslToggle>());
  }

  /// <summary>
  ///   I23: Verifies that getting a setting with a malformed zone ID returns HTTP 404 NotFound.
  /// </summary>
  /// <remarks>
  ///   Malformed zone IDs (containing invalid characters) fail at the routing layer before
  ///   reaching the zone settings handler, resulting in error code 7003 "Could not route to..."
  /// </remarks>
  [IntegrationTest]
  public async Task GetZoneSettingAsync_MalformedZoneId_ThrowsNotFound()
  {
    // Arrange
    var malformedZoneId = "invalid-zone-id-format!!!";

    // Act
    var action = async () => await _sut.GetZoneSettingAsync(malformedZoneId, ZoneSettingId.Ssl);

    // Assert - Malformed ID fails at routing layer with 404 (error code 7003)
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
  }

  /// <summary>
  ///   I24: Verifies that setting a value with a malformed zone ID returns HTTP 404 NotFound.
  /// </summary>
  /// <remarks>
  ///   Malformed zone IDs (containing invalid characters) fail at the routing layer before
  ///   reaching the zone settings handler, resulting in error code 7003 "Could not route to..."
  /// </remarks>
  [IntegrationTest]
  public async Task SetZoneSettingAsync_MalformedZoneId_ThrowsNotFound()
  {
    // Arrange
    var malformedZoneId = "invalid-zone-id-format!!!";

    // Act
    var action = async () => await _sut.SetZoneSettingAsync(malformedZoneId, ZoneSettingId.Ssl, "full");

    // Assert - Malformed ID fails at routing layer with 404 (error code 7003)
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
  }

  #endregion
}
