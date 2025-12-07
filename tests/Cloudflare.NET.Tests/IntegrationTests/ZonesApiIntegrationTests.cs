namespace Cloudflare.NET.Tests.IntegrationTests;

using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;
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
  /// <param name="output">The xUnit test output helper.</param>
  public ZonesApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    // Resolve the SUT and settings from the fixture and configuration.
    _sut      = fixture.ZonesApi;
    _settings = TestConfiguration.CloudflareSettings;

    _zoneId   = _settings.ZoneId;
    _hostname = $"_cfnet-test-{Guid.NewGuid():N}.{_settings.BaseDomain}";

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion

  #region Methods Impl

  /// <summary>Asynchronously creates the DNS record required for the tests. This runs once before any tests in the class.</summary>
  public async Task InitializeAsync()
  {
    var cnameTarget  = "localhost";
    var createResult = await _sut.CreateCnameRecordAsync(_zoneId, _hostname, cnameTarget);

    _recordId = createResult.Id;
  }

  /// <summary>Asynchronously deletes the DNS record after all tests in the class have run, ensuring a clean state.</summary>
  public async Task DisposeAsync()
  {
    if (_recordId is not null)
      await _sut.DeleteDnsRecordAsync(_zoneId, _recordId);
  }

  #endregion

  #region Methods

  /// <summary>Tests the lifecycle of listing DNS records.</summary>
  [IntegrationTest]
  public async Task DnsRecordListing_Lifecycle()
  {
    // Arrange
    // Record is created in InitializeAsync.
    var filters = new Zones.Models.ListDnsRecordsFilters { Name = _hostname };

    // Act
    var records = new List<Zones.Models.DnsRecord>();
    await foreach (var record in _sut.ListAllDnsRecordsAsync(_zoneId, filters))
      records.Add(record);

    // Assert
    records.Should().HaveCount(1);
    records[0].Name.Should().Be(_hostname);
    records[0].Type.Should().Be("CNAME");
  }

  /// <summary>Tests that DNS records can be exported, deleted, and then re-imported.</summary>
  [IntegrationTest]
  public async Task DnsRecord_ImportExport_CanRoundtrip()
  {
    // Arrange
    var    tempRecordName = $"_cfnet-roundtrip-{Guid.NewGuid():N}.{_settings.BaseDomain}";
    var    tempRecord     = await _sut.CreateCnameRecordAsync(_zoneId, tempRecordName, "localhost");
    string bindContent;

    try
    {
      // 1. Export
      bindContent = await _sut.ExportDnsRecordsAsync(_zoneId);
      bindContent.Should().Contain(tempRecordName);

      // 2. Delete
      await _sut.DeleteDnsRecordAsync(_zoneId, tempRecord.Id);
      var deletedRecord = await _sut.FindDnsRecordByNameAsync(_zoneId, tempRecordName);
      deletedRecord.Should().BeNull();

      // Act
      // 3. Import
      using var stream       = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(bindContent));
      var       importResult = await _sut.ImportDnsRecordsAsync(_zoneId, stream, true, false);

      // Assert
      importResult.Should().NotBeNull();
      importResult.RecordsAdded.Should().BeGreaterThan(0);

      var reimportedRecord = await _sut.FindDnsRecordByNameAsync(_zoneId, tempRecordName);
      reimportedRecord.Should().NotBeNull();
      // The 'finally' block handles the cleanup of the re-imported record.
      // Do not assign its ID to the instance field '_recordId' to avoid a double-delete in DisposeAsync.
    }
    finally
    {
      // Cleanup
      var finalRecord = await _sut.FindDnsRecordByNameAsync(_zoneId, tempRecordName);
      if (finalRecord is not null)
        await _sut.DeleteDnsRecordAsync(_zoneId, finalRecord.Id);
    }
  }

  /// <summary>
  ///   Verifies that the ListAllDnsRecordsAsync method correctly handles multiple pages of results from the live API.
  ///   It creates enough records to span across pages and asserts that the total count is correct. The `IAsyncEnumerable`
  ///   pattern is used here to abstract away the underlying pagination mechanism, providing a simpler development
  ///   experience. [1, 5, 7]
  /// </summary>
  [IntegrationTest]
  public async Task ListAllDnsRecordsAsync_HandlesMultiplePages()
  {
    // Arrange: Create enough records to guarantee pagination
    var recordsToCreate  = 3;
    var createdRecordIds = new List<string>();
    var baseHostname     = $"_cfnet-pagination-test-{Guid.NewGuid():N}";
    // Use a unique CNAME target for this test run to allow for efficient filtering. The 'name'
    // parameter is an exact match, so filtering by a unique 'content' is the correct way to
    // isolate records for this test. [1, 2]
    var cnameTarget = $"{Guid.NewGuid():N}.test-target.com";

    try
    {
      for (var i = 0; i < recordsToCreate; i++)
      {
        var hostname = $"{baseHostname}-{i}.{_settings.BaseDomain}";
        var record   = await _sut.CreateCnameRecordAsync(_zoneId, hostname, cnameTarget);
        createdRecordIds.Add(record.Id);
      }

      // Act: List records with a small per-page limit to force pagination.
      // We filter by the unique content to ensure we only get records from this test run.
      var filters    = new Zones.Models.ListDnsRecordsFilters { Content = cnameTarget, PerPage = 2 };
      var allRecords = new List<Zones.Models.DnsRecord>();

      // Using a small PerPage value forces the pagination logic to be exercised.
      await foreach (var record in _sut.ListAllDnsRecordsAsync(_zoneId, filters))
        allRecords.Add(record);

      // Assert
      allRecords.Should().HaveCount(recordsToCreate);
      allRecords.Select(r => r.Id).Should().BeEquivalentTo(createdRecordIds);
    }
    finally
    {
      // Cleanup
      foreach (var recordId in createdRecordIds)
        await _sut.DeleteDnsRecordAsync(_zoneId, recordId);
    }
  }

  /// <summary>Verifies that the cache for a zone can be purged completely.</summary>
  [IntegrationTest]
  public async Task PurgeCacheAsync_CanPurgeEverything()
  {
    // Arrange
    var request = new Zones.Models.PurgeCacheRequest(true);

    // Act
    var result = await _sut.PurgeCacheAsync(_zoneId, request);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(_zoneId);
  }

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

  /// <summary>Verifies that attempting to delete a non-existent resource correctly throws a 404 Not Found exception.</summary>
  [IntegrationTest]
  public async Task DeleteDnsRecordAsync_WhenRecordDoesNotExist_ThrowsNotFound()
  {
    // Arrange
    var nonExistentId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.DeleteDnsRecordAsync(_zoneId, nonExistentId);

    // Assert
    var ex = await action.Should().ThrowAsync<HttpRequestException>();
    ex.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
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
