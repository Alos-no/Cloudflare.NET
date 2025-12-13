namespace Cloudflare.NET.Tests.IntegrationTests;

using Dns;
using Dns.Models;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;

/// <summary>
///   Temporary cleanup test to delete stale test DNS records that accumulated
///   from failed test runs. This test should be run manually when the DNS record
///   quota is exceeded (error 81045).
/// </summary>
/// <remarks>
///   This test targets records with the following patterns:
///   <list type="bullet">
///     <item><c>_cfnet-dns-*</c> - DnsApiIntegrationTests records</item>
///     <item><c>_cfnet-zonesdns-*</c> - ZonesApiDnsIntegrationTests records</item>
///     <item><c>_cfnet-scan-*</c> - DnsScanIntegrationTests records</item>
///   </list>
///   Run this test with: <c>dotnet test --filter "FullyQualifiedName~CleanupStaleTestRecordsTests"</c>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class CleanupStaleTestRecordsTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The DNS API client.</summary>
  private readonly IDnsApi _dnsApi;

  /// <summary>The ID of the test zone from configuration.</summary>
  private readonly string _zoneId;

  /// <summary>The base domain for the test zone.</summary>
  private readonly string _baseDomain;

  /// <summary>The test output helper for logging.</summary>
  private readonly ITestOutputHelper _output;

  /// <summary>
  ///   Prefixes used by integration tests. Records with these prefixes in their name
  ///   will be deleted during cleanup.
  /// </summary>
  private static readonly string[] TestRecordPrefixes =
  [
    "_cfnet-dns-",
    "_cfnet-zonesdns-",
    "_cfnet-scan-",
    "_cfnet-"
  ];

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="CleanupStaleTestRecordsTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public CleanupStaleTestRecordsTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _dnsApi     = fixture.DnsApi;
    _zoneId     = TestConfiguration.CloudflareSettings.ZoneId;
    _baseDomain = TestConfiguration.CloudflareSettings.BaseDomain;
    _output     = output;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region Cleanup Tests

  /// <summary>
  ///   Deletes all DNS records that match the test record patterns.
  ///   This is a destructive operation and should only be run manually.
  /// </summary>
  [IntegrationTest(Skip = "This test is destructive and should only be run manually")]
  public async Task CleanupAllStaleTestRecords()
  {
    _output.WriteLine("Starting cleanup of stale test records...");
    _output.WriteLine($"Zone ID: {_zoneId}");
    _output.WriteLine($"Base Domain: {_baseDomain}");
    _output.WriteLine($"Test record prefixes: {string.Join(", ", TestRecordPrefixes)}");
    _output.WriteLine("");

    // List all DNS records in the zone.
    var allRecords = new List<DnsRecord>();
    await foreach (var record in _dnsApi.ListAllDnsRecordsAsync(_zoneId))
    {
      allRecords.Add(record);
    }

    _output.WriteLine($"Total records in zone: {allRecords.Count}");

    // Find records that match the test patterns.
    var testRecords = allRecords
      .Where(r => TestRecordPrefixes.Any(prefix => r.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
      .ToList();

    _output.WriteLine($"Test records to delete: {testRecords.Count}");
    _output.WriteLine("");

    if (testRecords.Count == 0)
    {
      _output.WriteLine("No stale test records found. Zone is clean!");
      return;
    }

    // Log all records to be deleted.
    _output.WriteLine("Records to delete:");
    foreach (var record in testRecords)
    {
      _output.WriteLine($"  - {record.Name} ({record.Type}) -> {record.Content}");
    }
    _output.WriteLine("");

    // Delete each test record.
    var deletedCount = 0;
    var failedCount  = 0;

    foreach (var record in testRecords)
    {
      try
      {
        await _dnsApi.DeleteDnsRecordAsync(_zoneId, record.Id);
        deletedCount++;
        _output.WriteLine($"Deleted: {record.Name}");
      }
      catch (Exception ex)
      {
        failedCount++;
        _output.WriteLine($"Failed to delete {record.Name}: {ex.Message}");
      }
    }

    _output.WriteLine("");
    _output.WriteLine($"Cleanup complete. Deleted: {deletedCount}, Failed: {failedCount}");

    // Assert that we cleaned up at least some records.
    deletedCount.Should().BeGreaterThan(0, "at least some records should have been deleted");
  }

  /// <summary>
  ///   Lists all DNS records matching test patterns without deleting them.
  ///   Use this to preview what would be deleted.
  /// </summary>
  [IntegrationTest(Skip = "This test is destructive and should only be run manually")]
  public async Task ListStaleTestRecords()
  {
    _output.WriteLine("Listing stale test records (preview mode)...");
    _output.WriteLine($"Zone ID: {_zoneId}");
    _output.WriteLine($"Base Domain: {_baseDomain}");
    _output.WriteLine("");

    // List all DNS records in the zone.
    var allRecords = new List<DnsRecord>();
    await foreach (var record in _dnsApi.ListAllDnsRecordsAsync(_zoneId))
    {
      allRecords.Add(record);
    }

    _output.WriteLine($"Total records in zone: {allRecords.Count}");

    // Find records that match the test patterns.
    var testRecords = allRecords
      .Where(r => TestRecordPrefixes.Any(prefix => r.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
      .OrderBy(r => r.Name)
      .ToList();

    _output.WriteLine($"Test records found: {testRecords.Count}");
    _output.WriteLine("");

    if (testRecords.Count == 0)
    {
      _output.WriteLine("No stale test records found. Zone is clean!");
      return;
    }

    // Group by prefix for better readability.
    foreach (var prefix in TestRecordPrefixes)
    {
      var recordsWithPrefix = testRecords
        .Where(r => r.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        .ToList();

      if (recordsWithPrefix.Count > 0)
      {
        _output.WriteLine($"Records with prefix '{prefix}':");
        foreach (var record in recordsWithPrefix)
        {
          _output.WriteLine($"  - {record.Name} ({record.Type}) -> {record.Content}");
        }
        _output.WriteLine("");
      }
    }
  }

  #endregion
}
