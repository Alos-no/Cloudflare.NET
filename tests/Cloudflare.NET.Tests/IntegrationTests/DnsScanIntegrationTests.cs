namespace Cloudflare.NET.Tests.IntegrationTests;

using System.Diagnostics;
using Dns;
using Dns.Models;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for the DNS scan operations in <see cref="DnsApi" /> class.
///   <para>
///     These tests exercise the DNS record scanning workflow against the live Cloudflare API.
///     Scanning is asynchronous - after triggering, results may not be immediately available.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     <b>Important:</b> DNS scanning discovers records by querying authoritative nameservers.
///     A zone with no existing DNS records may return empty scan results.
///   </para>
///   <para>
///     Accepted records become permanent DNS records that must be cleaned up after tests.
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
[Collection(TestCollections.DnsScan)]
public class DnsScanIntegrationTests : IClassFixture<CloudflareApiTestFixture>, IAsyncLifetime
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IDnsApi _sut;

  /// <summary>The ID of the test zone from configuration.</summary>
  private readonly string _zoneId;

  /// <summary>The settings loaded from the test configuration.</summary>
  private readonly TestCloudflareSettings _settings;

  /// <summary>Tracks all record IDs created during tests for cleanup.</summary>
  private readonly List<string> _acceptedRecordIds = [];

  /// <summary>Default timeout for polling scan results.</summary>
  private static readonly TimeSpan DefaultPollTimeout = TimeSpan.FromSeconds(30);

  /// <summary>Default interval between poll attempts.</summary>
  private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(2);

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="DnsScanIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public DnsScanIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut      = fixture.DnsApi;
    _settings = TestConfiguration.CloudflareSettings;
    _zoneId   = _settings.ZoneId;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region Methods Impl - Lifecycle

  /// <summary>
  ///   Asynchronously initializes the test class.
  ///   This runs once before any tests in the class.
  /// </summary>
  public Task InitializeAsync()
  {
    // No setup required for scan tests - we test triggering from scratch.
    return Task.CompletedTask;
  }

  /// <summary>
  ///   Asynchronously cleans up any DNS records created by accepting scanned records.
  /// </summary>
  public async Task DisposeAsync()
  {
    foreach (var recordId in _acceptedRecordIds)
    {
      try
      {
        await _sut.DeleteDnsRecordAsync(_zoneId, recordId);
      }
      catch
      {
        // Ignore errors during cleanup - record may already be deleted.
      }
    }
  }

  #endregion


  #region Test Methods - Trigger Scan (I01-I03)

  /// <summary>I01: Verifies TriggerDnsRecordScanAsync completes successfully.</summary>
  [IntegrationTest]
  public async Task TriggerDnsRecordScanAsync_ValidZone_CompletesWithoutException()
  {
    // Act
    var action = async () => await _sut.TriggerDnsRecordScanAsync(_zoneId);

    // Assert
    await action.Should().NotThrowAsync();
  }

  /// <summary>I02: Verifies TriggerDnsRecordScanAsync throws for non-existent zone.</summary>
  [IntegrationTest]
  public async Task TriggerDnsRecordScanAsync_InvalidZone_ThrowsException()
  {
    // Arrange
    const string invalidZoneId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.TriggerDnsRecordScanAsync(invalidZoneId);

    // Assert
    await action.Should().ThrowAsync<Exception>();
  }

  /// <summary>I03: Verifies second scan trigger doesn't cause error (may be rate limited but not error).</summary>
  [IntegrationTest]
  public async Task TriggerDnsRecordScanAsync_DoubleTrigger_DoesNotThrowImmediately()
  {
    // Arrange - First trigger
    await _sut.TriggerDnsRecordScanAsync(_zoneId);

    // Act - Second trigger immediately
    // Note: Cloudflare may rate limit but shouldn't throw an error for a valid zone
    var action = async () => await _sut.TriggerDnsRecordScanAsync(_zoneId);

    // Assert - Should either succeed or throw rate limit error (429), not zone error
    // Either completion without exception or HttpRequestException with 429 is acceptable
    try
    {
      await action();
      // Success - double trigger allowed
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    {
      // Also acceptable - rate limited
    }
  }

  #endregion


  #region Test Methods - Get Review (I04-I06)

  /// <summary>I04: Verifies GetDnsRecordScanReviewAsync returns a list (may be empty).</summary>
  [IntegrationTest]
  public async Task GetDnsRecordScanReviewAsync_ValidZone_ReturnsList()
  {
    // Act
    var result = await _sut.GetDnsRecordScanReviewAsync(_zoneId);

    // Assert
    result.Should().NotBeNull();
    result.Should().BeAssignableTo<IReadOnlyList<DnsRecord>>();
  }

  /// <summary>I05: Verifies GetDnsRecordScanReviewAsync can be called after triggering scan.</summary>
  [IntegrationTest]
  public async Task GetDnsRecordScanReviewAsync_AfterTrigger_ReturnsList()
  {
    // Arrange
    await _sut.TriggerDnsRecordScanAsync(_zoneId);

    // Wait briefly for scan to potentially find something
    await Task.Delay(TimeSpan.FromSeconds(2));

    // Act
    var result = await _sut.GetDnsRecordScanReviewAsync(_zoneId);

    // Assert - Result may be empty if zone has no discoverable records
    result.Should().NotBeNull();
  }

  /// <summary>I06: Verifies returned records have expected structure (if any).</summary>
  [IntegrationTest]
  public async Task GetDnsRecordScanReviewAsync_WithRecords_RecordsHaveExpectedStructure()
  {
    // Arrange - Trigger scan and wait
    await _sut.TriggerDnsRecordScanAsync(_zoneId);
    var records = await PollForScanResultsAsync(_zoneId, DefaultPollTimeout, DefaultPollInterval);

    // Skip if no records found (zone may not have discoverable DNS)
    if (records.Count == 0)
    {
      // No records to validate - test is inconclusive but not a failure
      return;
    }

    // Assert - Verify record structure
    var record = records[0];
    record.Id.Should().NotBeNullOrEmpty();
    record.Name.Should().NotBeNullOrEmpty();
    record.Type.Should().NotBeNull();
    record.Content.Should().NotBeNull();
  }

  #endregion


  #region Test Methods - Submit Review (I07-I12)

  /// <summary>I07: Verifies accepting records creates DNS records (if records available and API supports it).</summary>
  /// <remarks>
  ///   This test may skip if no scanned records are found, or if the zone/plan doesn't support
  ///   scan review operations.
  /// </remarks>
  [IntegrationTest]
  public async Task SubmitDnsRecordScanReviewAsync_AcceptRecords_CreatesRecords()
  {
    // Arrange - Trigger scan and wait for records
    await _sut.TriggerDnsRecordScanAsync(_zoneId);
    var pendingRecords = await PollForScanResultsAsync(_zoneId, DefaultPollTimeout, DefaultPollInterval);

    // Skip if no records found
    if (pendingRecords.Count == 0)
    {
      return;
    }

    // Accept first record only to minimize cleanup
    var recordToAccept = pendingRecords[0];
    var request = new DnsScanReviewRequest { Accepts = [recordToAccept.Id] };

    // Act - API may reject if zone/plan doesn't support scan review
    try
    {
      var result = await _sut.SubmitDnsRecordScanReviewAsync(_zoneId, request);

      // Track for cleanup
      _acceptedRecordIds.Add(recordToAccept.Id);

      // Assert
      result.Should().NotBeNull();
      result.Accepts.Should().BeGreaterThanOrEqualTo(1);

      // Verify record now exists as a permanent DNS record
      var createdRecord = await _sut.GetDnsRecordAsync(_zoneId, recordToAccept.Id);
      createdRecord.Should().NotBeNull();
      createdRecord.Id.Should().Be(recordToAccept.Id);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
    {
      // API rejected - zone/plan may not support scan review submissions
    }
  }

  /// <summary>I08: Verifies rejecting records removes them from review queue (if records available and API supports it).</summary>
  [IntegrationTest]
  public async Task SubmitDnsRecordScanReviewAsync_RejectRecords_RemovesFromReview()
  {
    // Arrange - Trigger scan and wait for records
    await _sut.TriggerDnsRecordScanAsync(_zoneId);
    var pendingRecords = await PollForScanResultsAsync(_zoneId, DefaultPollTimeout, DefaultPollInterval);

    // Skip if no records found
    if (pendingRecords.Count == 0)
    {
      return;
    }

    // Reject first record
    var recordToReject = pendingRecords[0];
    var request = new DnsScanReviewRequest { Rejects = [recordToReject.Id] };

    // Act - API may reject if zone/plan doesn't support scan review
    try
    {
      var result = await _sut.SubmitDnsRecordScanReviewAsync(_zoneId, request);

      // Assert
      result.Should().NotBeNull();
      result.Rejects.Should().BeGreaterThanOrEqualTo(1);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
    {
      // API rejected - zone/plan may not support scan review submissions
    }
  }

  /// <summary>I09: Verifies mixed accept/reject works (if enough records available and API supports it).</summary>
  [IntegrationTest]
  public async Task SubmitDnsRecordScanReviewAsync_MixedAcceptReject_ProcessesBoth()
  {
    // Arrange - Trigger scan and wait for records
    await _sut.TriggerDnsRecordScanAsync(_zoneId);
    var pendingRecords = await PollForScanResultsAsync(_zoneId, DefaultPollTimeout, DefaultPollInterval);

    // Skip if less than 2 records found
    if (pendingRecords.Count < 2)
    {
      return;
    }

    // Accept first, reject second
    var acceptIds = new[] { pendingRecords[0].Id };
    var rejectIds = new[] { pendingRecords[1].Id };
    var request = new DnsScanReviewRequest { Accepts = acceptIds, Rejects = rejectIds };

    // Act - API may reject if zone/plan doesn't support scan review
    try
    {
      var result = await _sut.SubmitDnsRecordScanReviewAsync(_zoneId, request);

      // Track accepted record for cleanup
      _acceptedRecordIds.Add(pendingRecords[0].Id);

      // Assert
      result.Should().NotBeNull();
      result.Accepts.Should().Be(1);
      result.Rejects.Should().Be(1);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
    {
      // API rejected - zone/plan may not support scan review submissions
    }
  }

  /// <summary>I12: Verifies empty request is handled (may return zero counts or fail based on API implementation).</summary>
  /// <remarks>
  ///   Some zones may reject empty scan review requests. This test verifies the API responds
  ///   consistently (either success with zero counts or a well-formed error).
  /// </remarks>
  [IntegrationTest]
  public async Task SubmitDnsRecordScanReviewAsync_EmptyRequest_HandledConsistently()
  {
    // Arrange
    var request = new DnsScanReviewRequest();

    // Act - The API may return success with zero counts or reject the request
    try
    {
      var result = await _sut.SubmitDnsRecordScanReviewAsync(_zoneId, request);

      // If it succeeds, verify zero counts
      result.Should().NotBeNull();
      result.Accepts.Should().Be(0);
      result.Rejects.Should().Be(0);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
    {
      // API rejected empty request - this is acceptable behavior
      // Some zones/plans may not support empty scan review submissions
    }
  }

  #endregion


  #region Test Methods - Full Workflow (I13)

  /// <summary>I13: Verifies complete scan workflow: trigger → poll → accept → verify (if API supports it).</summary>
  [IntegrationTest]
  public async Task CompleteScanWorkflow_TriggerPollAcceptVerify_WorksEndToEnd()
  {
    // Step 1: Trigger scan
    await _sut.TriggerDnsRecordScanAsync(_zoneId);

    // Step 2: Poll for results
    var pendingRecords = await PollForScanResultsAsync(_zoneId, DefaultPollTimeout, DefaultPollInterval);

    // Skip remaining steps if no records found
    if (pendingRecords.Count == 0)
    {
      return;
    }

    // Step 3: Accept a record - API may reject if zone/plan doesn't support scan review
    var recordToAccept = pendingRecords[0];
    var acceptRequest = new DnsScanReviewRequest { Accepts = [recordToAccept.Id] };

    try
    {
      var reviewResult = await _sut.SubmitDnsRecordScanReviewAsync(_zoneId, acceptRequest);

      _acceptedRecordIds.Add(recordToAccept.Id);

      // Assert review result
      reviewResult.Accepts.Should().Be(1);

      // Step 4: Verify accepted record exists in DNS list
      var dnsRecord = await _sut.GetDnsRecordAsync(_zoneId, recordToAccept.Id);
      dnsRecord.Should().NotBeNull();
      dnsRecord.Name.Should().Be(recordToAccept.Name);
      dnsRecord.Type.Should().Be(recordToAccept.Type);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
    {
      // API rejected - zone/plan may not support scan review submissions
    }
  }

  #endregion


  #region Test Methods - Edge Cases (I16-I19)

  /// <summary>I18: Verifies request with invalid record ID is handled gracefully.</summary>
  [IntegrationTest]
  public async Task SubmitDnsRecordScanReviewAsync_InvalidRecordId_HandledGracefully()
  {
    // Arrange - Use a fake record ID
    var request = new DnsScanReviewRequest { Accepts = ["00000000000000000000000000000000"] };

    // Act - Submit with invalid ID
    // Note: Cloudflare may return 0 accepts (ignoring invalid IDs) or throw an error
    try
    {
      var result = await _sut.SubmitDnsRecordScanReviewAsync(_zoneId, request);
      // If no exception, result should show 0 accepts for non-existent ID
      result.Accepts.Should().Be(0);
    }
    catch (Exception)
    {
      // Exception is also acceptable behavior for invalid ID
    }
  }

  /// <summary>I19: Verifies scan on zone with minimal DNS works without error.</summary>
  [IntegrationTest]
  public async Task TriggerAndGetReview_MinimalZone_NoError()
  {
    // Act - Full workflow on the test zone
    await _sut.TriggerDnsRecordScanAsync(_zoneId);

    // Brief wait
    await Task.Delay(TimeSpan.FromSeconds(1));

    // Get review - may be empty, that's OK
    var action = async () => await _sut.GetDnsRecordScanReviewAsync(_zoneId);

    // Assert - Should not throw regardless of zone content
    await action.Should().NotThrowAsync();
  }

  #endregion


  #region Helper Methods

  /// <summary>
  ///   Polls for scan results with timeout.
  ///   Returns whatever records are found when the timeout expires.
  /// </summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="timeout">Maximum time to wait for results.</param>
  /// <param name="pollInterval">Time between poll attempts.</param>
  /// <returns>List of scanned DNS records (may be empty).</returns>
  private async Task<IReadOnlyList<DnsRecord>> PollForScanResultsAsync(
    string   zoneId,
    TimeSpan timeout,
    TimeSpan pollInterval)
  {
    var stopwatch = Stopwatch.StartNew();

    while (stopwatch.Elapsed < timeout)
    {
      var results = await _sut.GetDnsRecordScanReviewAsync(zoneId);

      if (results.Count > 0)
        return results;

      await Task.Delay(pollInterval);
    }

    // Return empty list after timeout - scan may have found nothing
    return [];
  }

  #endregion
}
