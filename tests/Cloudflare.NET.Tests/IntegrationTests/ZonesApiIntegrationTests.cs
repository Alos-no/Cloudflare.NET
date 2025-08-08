namespace Cloudflare.NET.Tests.IntegrationTests;

using Fixtures;
using Shared.Fixtures;
using Shared.Helpers;
using Zones;

/// <summary>Contains integration tests for the <see cref="ZonesApi" /> class.</summary>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class ZonesApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>, IAsyncLifetime
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IZonesApi _sut;

  /// <summary>The ID of the test zone from configuration.</summary>
  private readonly string _zoneId;

  /// <summary>A unique hostname for the test CNAME record.</summary>
  private readonly string _hostname;

  /// <summary>The settings loaded from the test configuration.</summary>
  private readonly TestCloudflareSettings _settings;

  /// <summary>The ID of the DNS record created for the test run.</summary>
  private string? _recordId;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="ZonesApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  public ZonesApiIntegrationTests(CloudflareApiTestFixture fixture)
  {
    // Resolve the SUT and settings from the fixture and configuration.
    _sut      = fixture.ZonesApi;
    _settings = TestConfiguration.CloudflareSettings;

    _zoneId   = _settings.ZoneId;
    _hostname = $"_cfnet-test-{Guid.NewGuid():N}.{_settings.BaseDomain}";
  }

  #endregion

  #region Methods Impl

  /// <summary>
  ///   Asynchronously creates the DNS record required for the tests. This runs once before
  ///   any tests in the class.
  /// </summary>
  public async Task InitializeAsync()
  {
    var cnameTarget  = "localhost";
    var createResult = await _sut.CreateCnameRecordAsync(_zoneId, _hostname, cnameTarget);

    _recordId = createResult.Id;
  }

  /// <summary>
  ///   Asynchronously deletes the DNS record after all tests in the class have run, ensuring
  ///   a clean state.
  /// </summary>
  public async Task DisposeAsync()
  {
    if (_recordId is not null)
      await _sut.DeleteDnsRecordAsync(_zoneId, _recordId);
  }

  #endregion

  #region Methods

  /// <summary>Tests that a DNS record can be found by its name after being created.</summary>
  [IntegrationTest]
  public async Task CanFindDnsRecordByName()
  {
    // Arrange
    // The record is created in InitializeAsync. This test just needs to verify it can be found.
    _recordId.Should().NotBeNullOrWhiteSpace("the DNS record should have been created in InitializeAsync");

    // Act
    var findResult = await _sut.FindDnsRecordByNameAsync(_zoneId, _hostname);

    // Assert
    findResult.Should().NotBeNull();
    findResult.Id.Should().Be(_recordId);
    findResult.Name.Should().Be(_hostname);
    findResult.Type.Should().Be("CNAME");
  }

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
    zoneDetails.Status.Should().Be("active");
  }

  #endregion
}
