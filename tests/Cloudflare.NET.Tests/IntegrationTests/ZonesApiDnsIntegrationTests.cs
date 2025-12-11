namespace Cloudflare.NET.Tests.IntegrationTests;

using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;
using Zones;
using Zones.Models;

/// <summary>
///   Contains integration tests for the DNS operations on <see cref="ZonesApi" />.
///   These tests exercise the legacy DNS methods exposed through <see cref="IZonesApi" />.
/// </summary>
/// <remarks>
///   For tests of the dedicated DNS API (<see cref="Dns.IDnsApi" />), see <see cref="DnsApiIntegrationTests" />.
///   For Zone CRUD tests, see <see cref="ZonesApiIntegrationTests" />.
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class ZonesApiDnsIntegrationTests : IClassFixture<CloudflareApiTestFixture>, IAsyncLifetime
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

  /// <summary>Initializes a new instance of the <see cref="ZonesApiDnsIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public ZonesApiDnsIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    // Resolve the SUT and settings from the fixture and configuration.
    _sut      = fixture.ZonesApi;
    _settings = TestConfiguration.CloudflareSettings;

    _zoneId   = _settings.ZoneId;
    _hostname = $"_cfnet-zonesdns-{Guid.NewGuid():N}.{_settings.BaseDomain}";

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region Methods Impl - Lifecycle

  /// <summary>
  ///   Asynchronously creates the DNS record required for the tests.
  ///   This runs once before any tests in the class.
  /// </summary>
  public async Task InitializeAsync()
  {
    var cnameTarget  = "localhost";
    var createResult = await _sut.CreateCnameRecordAsync(_zoneId, _hostname, cnameTarget);

    _recordId = createResult.Id;
  }

  /// <summary>
  ///   Asynchronously deletes the DNS record after all tests in the class have run,
  ///   ensuring a clean state.
  /// </summary>
  public async Task DisposeAsync()
  {
    if (_recordId is not null)
      await _sut.DeleteDnsRecordAsync(_zoneId, _recordId);
  }

  #endregion


  #region DNS Record Listing Tests

  /// <summary>Tests the lifecycle of listing DNS records via IZonesApi.</summary>
  [IntegrationTest]
  public async Task DnsRecordListing_Lifecycle()
  {
    // Arrange
    // Record is created in InitializeAsync.
    var filters = new ListDnsRecordsFilters { Name = _hostname };

    // Act
    var records = new List<DnsRecord>();
    await foreach (var record in _sut.ListAllDnsRecordsAsync(_zoneId, filters))
      records.Add(record);

    // Assert
    records.Should().HaveCount(1);
    records[0].Name.Should().Be(_hostname);
    records[0].Type.Should().Be(DnsRecordType.CNAME);
  }

  /// <summary>
  ///   Verifies that the ListAllDnsRecordsAsync method correctly handles multiple pages of results from the live API.
  ///   It creates enough records to span across pages and asserts that the total count is correct.
  /// </summary>
  [IntegrationTest]
  public async Task ListAllDnsRecordsAsync_HandlesMultiplePages()
  {
    // Arrange: Create enough records to guarantee pagination
    var recordsToCreate  = 3;
    var createdRecordIds = new List<string>();
    var baseHostname     = $"_cfnet-zonesdns-pagination-{Guid.NewGuid():N}";
    // Use a unique CNAME target for this test run to allow for efficient filtering.
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
      var filters    = new ListDnsRecordsFilters { Content = cnameTarget, PerPage = 2 };
      var allRecords = new List<DnsRecord>();

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

  #endregion


  #region DNS Record CRUD Tests

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
    findResult.Type.Should().Be(DnsRecordType.CNAME);
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

  #endregion


  #region DNS Import/Export Tests

  /// <summary>Tests that DNS records can be exported, deleted, and then re-imported via IZonesApi.</summary>
  [IntegrationTest]
  public async Task DnsRecord_ImportExport_CanRoundtrip()
  {
    // Arrange
    var    tempRecordName = $"_cfnet-zonesdns-roundtrip-{Guid.NewGuid():N}.{_settings.BaseDomain}";
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
    }
    finally
    {
      // Cleanup
      var finalRecord = await _sut.FindDnsRecordByNameAsync(_zoneId, tempRecordName);
      if (finalRecord is not null)
        await _sut.DeleteDnsRecordAsync(_zoneId, finalRecord.Id);
    }
  }

  #endregion


  #region Cache Purge Tests

  /// <summary>Verifies that the cache for a zone can be purged completely via IZonesApi.</summary>
  [IntegrationTest]
  public async Task PurgeCacheAsync_CanPurgeEverything()
  {
    // Arrange
    var request = new PurgeCacheRequest(true);

    // Act
    var result = await _sut.PurgeCacheAsync(_zoneId, request);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(_zoneId);
  }

  #endregion
}
