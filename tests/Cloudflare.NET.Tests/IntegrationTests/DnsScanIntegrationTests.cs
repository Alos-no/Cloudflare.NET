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

  /// <summary>I03: Verifies second scan trigger completes without throwing.</summary>
  [IntegrationTest]
  public async Task TriggerDnsRecordScanAsync_DoubleTrigger_DoesNotThrowImmediately()
  {
    // Arrange - First trigger
    await _sut.TriggerDnsRecordScanAsync(_zoneId);

    // Act - Second trigger immediately
    var action = async () => await _sut.TriggerDnsRecordScanAsync(_zoneId);

    // Assert - Should complete without throwing
    await action.Should().NotThrowAsync();
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

    records.Should().NotBeEmpty("DNS scan should discover records for a zone not yet migrated to Cloudflare nameservers");

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
  ///   DNS scan is designed for zones before nameserver migration to Cloudflare.
  ///   Once nameservers point to Cloudflare, the scan has nothing external to discover.
  /// </remarks>
  [IntegrationTest(Skip = "Requires disposable domain - Consider WireMock - DNS scan only works for zones not yet migrated to Cloudflare nameservers")]
  public async Task SubmitDnsRecordScanReviewAsync_AcceptRecords_CreatesRecords()
  {
    // Arrange - Trigger scan and wait for records
    await _sut.TriggerDnsRecordScanAsync(_zoneId);
    var pendingRecords = await PollForScanResultsAsync(_zoneId, DefaultPollTimeout, DefaultPollInterval);

    pendingRecords.Should().NotBeEmpty("DNS scan should discover records for a zone not yet migrated to Cloudflare nameservers");

    // Accept first record only to minimize cleanup - convert to DnsScanAcceptItem
    var recordToAccept = pendingRecords[0];
    var acceptItem = DnsScanAcceptItem.FromDnsRecord(recordToAccept);
    var request = new DnsScanReviewRequest { Accepts = [acceptItem] };

    // Act
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

  /// <summary>I08: Verifies rejecting records removes them from review queue (if records available and API supports it).</summary>
  /// <remarks>
  ///   DNS scan is designed for zones before nameserver migration to Cloudflare.
  ///   Once nameservers point to Cloudflare, the scan has nothing external to discover.
  /// </remarks>
  [IntegrationTest(Skip = "Requires disposable domain - Consider WireMock - DNS scan only works for zones not yet migrated to Cloudflare nameservers")]
  public async Task SubmitDnsRecordScanReviewAsync_RejectRecords_RemovesFromReview()
  {
    // Arrange - Trigger scan and wait for records
    await _sut.TriggerDnsRecordScanAsync(_zoneId);
    var pendingRecords = await PollForScanResultsAsync(_zoneId, DefaultPollTimeout, DefaultPollInterval);

    pendingRecords.Should().NotBeEmpty("DNS scan should discover records for a zone not yet migrated to Cloudflare nameservers");

    // Reject first record
    var recordToReject = pendingRecords[0];
    var request = new DnsScanReviewRequest { Rejects = [recordToReject.Id] };

    // Act
    var result = await _sut.SubmitDnsRecordScanReviewAsync(_zoneId, request);

    // Assert
    result.Should().NotBeNull();
    result.Rejects.Should().BeGreaterThanOrEqualTo(1);
  }

  /// <summary>I09: Verifies mixed accept/reject works (if enough records available and API supports it).</summary>
  /// <remarks>
  ///   DNS scan is designed for zones before nameserver migration to Cloudflare.
  ///   Once nameservers point to Cloudflare, the scan has nothing external to discover.
  /// </remarks>
  [IntegrationTest(Skip = "Requires disposable domain - Consider WireMock - DNS scan only works for zones not yet migrated to Cloudflare nameservers")]
  public async Task SubmitDnsRecordScanReviewAsync_MixedAcceptReject_ProcessesBoth()
  {
    // Arrange - Trigger scan and wait for records
    await _sut.TriggerDnsRecordScanAsync(_zoneId);
    var pendingRecords = await PollForScanResultsAsync(_zoneId, DefaultPollTimeout, DefaultPollInterval);

    pendingRecords.Should().HaveCountGreaterThanOrEqualTo(2, "DNS scan should discover at least 2 records for this test");

    // Accept first record, reject second
    var recordToAccept = pendingRecords[0];
    var acceptItem = DnsScanAcceptItem.FromDnsRecord(recordToAccept);
    var rejectIds = new[] { pendingRecords[1].Id };
    var request = new DnsScanReviewRequest { Accepts = [acceptItem], Rejects = rejectIds };

    // Act
    var result = await _sut.SubmitDnsRecordScanReviewAsync(_zoneId, request);

    // Track accepted record for cleanup
    _acceptedRecordIds.Add(pendingRecords[0].Id);

    // Assert
    result.Should().NotBeNull();
    result.Accepts.Should().Be(1);
    result.Rejects.Should().Be(1);
  }

  /// <summary>I12: Verifies empty request throws BadRequest (API requires at least one accept or reject).</summary>
  /// <remarks>
  ///   The Cloudflare API requires at least one record in the accepts or rejects array.
  ///   Submitting an empty request returns error 9207 "Request body is invalid".
  /// </remarks>
  [IntegrationTest]
  public async Task SubmitDnsRecordScanReviewAsync_EmptyRequest_ThrowsBadRequest()
  {
    // Arrange
    var request = new DnsScanReviewRequest();

    // Act
    var action = async () => await _sut.SubmitDnsRecordScanReviewAsync(_zoneId, request);

    // Assert - API rejects empty requests with error 9207
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.BadRequest);
  }

  #endregion


  #region Test Methods - Full Workflow (I13)

  /// <summary>I13: Verifies complete scan workflow: trigger → poll → accept → verify (if API supports it).</summary>
  /// <remarks>
  ///   DNS scan is designed for zones before nameserver migration to Cloudflare.
  ///   Once nameservers point to Cloudflare, the scan has nothing external to discover.
  /// </remarks>
  [IntegrationTest(Skip = "Requires disposable domain - Consider WireMock - DNS scan only works for zones not yet migrated to Cloudflare nameservers")]
  public async Task CompleteScanWorkflow_TriggerPollAcceptVerify_WorksEndToEnd()
  {
    // Step 1: Trigger scan
    await _sut.TriggerDnsRecordScanAsync(_zoneId);

    // Step 2: Poll for results
    var pendingRecords = await PollForScanResultsAsync(_zoneId, DefaultPollTimeout, DefaultPollInterval);

    pendingRecords.Should().NotBeEmpty("DNS scan should discover records for a zone not yet migrated to Cloudflare nameservers");

    // Step 3: Accept a record
    var recordToAccept = pendingRecords[0];
    var acceptItem = DnsScanAcceptItem.FromDnsRecord(recordToAccept);
    var acceptRequest = new DnsScanReviewRequest { Accepts = [acceptItem] };

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

  #endregion


  #region Test Methods - Edge Cases (I16-I19)

  /// <summary>I18: Verifies request with a record not in the scan queue throws NotFound.</summary>
  /// <remarks>
  ///   The Cloudflare API returns error 81044 "Record ID does not exist" with HTTP 404 NotFound
  ///   when attempting to accept a record ID that doesn't exist in the scan review queue.
  /// </remarks>
  [IntegrationTest]
  public async Task SubmitDnsRecordScanReviewAsync_RecordNotInQueue_ThrowsNotFound()
  {
    // Arrange - Use a fake record that doesn't match anything in the scan review queue
    var fakeAcceptItem = new DnsScanAcceptItem(
      Id: "fake-nonexistent-id",
      Type: DnsRecordType.A,
      Name: "fake-nonexistent.example.com",
      Content: "192.0.2.1",
      Ttl: 1,
      Proxied: false
    );
    var request = new DnsScanReviewRequest { Accepts = [fakeAcceptItem] };

    // Act
    var action = async () => await _sut.SubmitDnsRecordScanReviewAsync(_zoneId, request);

    // Assert - Cloudflare returns 404 NotFound with error 81044 for non-existent record IDs
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
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

  /// <summary>I20: Verifies that triggering a scan with a malformed zone ID returns HTTP 404 NotFound.</summary>
  /// <remarks>
  ///   Invalid zone ID formats (not 32-character hex strings) return 404 NotFound
  ///   with error code 7003 "Could not route to..." because Cloudflare's routing layer
  ///   cannot match the path to a valid zone endpoint.
  /// </remarks>
  [IntegrationTest]
  public async Task TriggerDnsRecordScanAsync_MalformedZoneId_ThrowsNotFound()
  {
    // Arrange
    var malformedZoneId = "invalid-zone-id-format!!!";

    // Act
    var action = async () => await _sut.TriggerDnsRecordScanAsync(malformedZoneId);

    // Assert
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
  }

  /// <summary>I21: Verifies that getting scan review with a malformed zone ID returns HTTP 404 NotFound.</summary>
  /// <remarks>
  ///   Invalid zone ID formats (not 32-character hex strings) return 404 NotFound
  ///   with error code 7003 "Could not route to..." because Cloudflare's routing layer
  ///   cannot match the path to a valid zone endpoint.
  /// </remarks>
  [IntegrationTest]
  public async Task GetDnsRecordScanReviewAsync_MalformedZoneId_ThrowsNotFound()
  {
    // Arrange
    var malformedZoneId = "invalid-zone-id-format!!!";

    // Act
    var action = async () => await _sut.GetDnsRecordScanReviewAsync(malformedZoneId);

    // Assert
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
  }

  /// <summary>I22: Verifies that submitting scan review with a malformed zone ID returns HTTP 404 NotFound.</summary>
  /// <remarks>
  ///   Invalid zone ID formats (not 32-character hex strings) return 404 NotFound
  ///   with error code 7003 "Could not route to..." because Cloudflare's routing layer
  ///   cannot match the path to a valid zone endpoint.
  /// </remarks>
  [IntegrationTest]
  public async Task SubmitDnsRecordScanReviewAsync_MalformedZoneId_ThrowsNotFound()
  {
    // Arrange
    var malformedZoneId = "invalid-zone-id-format!!!";
    var request = new DnsScanReviewRequest { Accepts = [], Rejects = [] };

    // Act
    var action = async () => await _sut.SubmitDnsRecordScanReviewAsync(malformedZoneId, request);

    // Assert
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
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
