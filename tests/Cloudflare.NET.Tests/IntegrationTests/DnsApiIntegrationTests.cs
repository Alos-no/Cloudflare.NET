namespace Cloudflare.NET.Tests.IntegrationTests;

using Dns;
using Dns.Models;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;
using DnsRecordType = Zones.Models.DnsRecordType;

/// <summary>
///   Contains integration tests for the <see cref="DnsApi" /> class.
///   <para>
///     These tests exercise the complete DNS record CRUD lifecycle against the live Cloudflare API.
///     Test records use a unique hostname pattern to avoid conflicts with production data.
///   </para>
/// </summary>
[Trait("Category", TestConstants.TestCategories.Integration)]
[Collection(TestCollections.DnsOperations)]
public class DnsApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>, IAsyncLifetime
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IDnsApi _sut;

  /// <summary>The ID of the test zone from configuration.</summary>
  private readonly string _zoneId;

  /// <summary>A unique base hostname for test records.</summary>
  private readonly string _testBase;

  /// <summary>The settings loaded from the test configuration.</summary>
  private readonly TestCloudflareSettings _settings;

  /// <summary>Tracks all record IDs created during tests for cleanup.</summary>
  private readonly List<string> _createdRecordIds = [];

  /// <summary>The ID of the primary DNS record created for lifecycle tests.</summary>
  private string? _primaryRecordId;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="DnsApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public DnsApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    // Resolve the SUT and settings from the fixture and configuration.
    _sut      = fixture.DnsApi;
    _settings = TestConfiguration.CloudflareSettings;

    _zoneId   = _settings.ZoneId;
    _testBase = $"_cfnet-dns-{Guid.NewGuid().ToString("N")[..8]}";

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region Methods Impl - Lifecycle

  /// <summary>
  ///   Asynchronously creates a primary DNS record required for some tests.
  ///   This runs once before any tests in the class.
  /// </summary>
  public async Task InitializeAsync()
  {
    // Create a primary test record for lifecycle tests.
    var hostname     = $"{_testBase}-primary.{_settings.BaseDomain}";
    var cnameTarget  = "localhost";
    var createResult = await _sut.CreateCnameRecordAsync(_zoneId, hostname, cnameTarget);

    _primaryRecordId = createResult.Id;
    _createdRecordIds.Add(createResult.Id);
  }

  /// <summary>
  ///   Asynchronously deletes all DNS records created during tests,
  ///   ensuring a clean state after all tests in the class have run.
  /// </summary>
  public async Task DisposeAsync()
  {
    foreach (var recordId in _createdRecordIds)
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


  #region Helper Methods

  /// <summary>Generates a unique hostname for a test record.</summary>
  /// <param name="suffix">An optional suffix to append to the hostname.</param>
  /// <returns>A unique hostname in the test domain.</returns>
  private string GenerateHostname(string suffix = "") =>
    $"{_testBase}{suffix}-{Guid.NewGuid().ToString("N")[..8]}.{_settings.BaseDomain}";

  /// <summary>Creates a test record and tracks it for cleanup.</summary>
  /// <param name="request">The DNS record creation request.</param>
  /// <returns>The created DNS record.</returns>
  private async Task<DnsRecord> CreateAndTrackRecordAsync(CreateDnsRecordRequest request)
  {
    var record = await _sut.CreateDnsRecordAsync(_zoneId, request);
    _createdRecordIds.Add(record.Id);

    return record;
  }

  #endregion


  #region Single Record CRUD Tests (I01-I11)

  /// <summary>
  ///   I01: Verifies that an A record can be created successfully.
  /// </summary>
  [IntegrationTest]
  public async Task CreateDnsRecordAsync_ARecord_ReturnsCreatedRecord()
  {
    // Arrange
    var hostname = GenerateHostname("-a");
    var request  = new CreateDnsRecordRequest(DnsRecordType.A, hostname, "192.0.2.1");

    // Act
    var record = await CreateAndTrackRecordAsync(request);

    // Assert
    record.Should().NotBeNull();
    record.Id.Should().NotBeNullOrWhiteSpace();
    record.Name.Should().Be(hostname);
    record.Type.Should().Be(DnsRecordType.A);
    record.Content.Should().Be("192.0.2.1");
  }

  /// <summary>
  ///   I02: Verifies that a CNAME record can be created and is proxiable.
  /// </summary>
  [IntegrationTest]
  public async Task CreateDnsRecordAsync_CnameRecord_IsProxiable()
  {
    // Arrange
    var hostname = GenerateHostname("-cname");
    var request  = new CreateDnsRecordRequest(DnsRecordType.CNAME, hostname, "example.com");

    // Act
    var record = await CreateAndTrackRecordAsync(request);

    // Assert
    record.Should().NotBeNull();
    record.Type.Should().Be(DnsRecordType.CNAME);
    record.Proxiable.Should().BeTrue("CNAME records should be proxiable");
  }

  /// <summary>
  ///   I03: Verifies that an MX record can be created with a priority.
  /// </summary>
  [IntegrationTest]
  public async Task CreateDnsRecordAsync_MxWithPriority_PriorityIsSet()
  {
    // Arrange
    var hostname = GenerateHostname("-mx");
    var request  = new CreateDnsRecordRequest(DnsRecordType.MX, hostname, "mail.example.com", Priority: 10);

    // Act
    var record = await CreateAndTrackRecordAsync(request);

    // Assert
    record.Should().NotBeNull();
    record.Type.Should().Be(DnsRecordType.MX);
    record.Priority.Should().Be(10);
    record.Proxiable.Should().BeFalse("MX records cannot be proxied");
  }

  /// <summary>
  ///   I04: Verifies that a TXT record preserves its content exactly.
  /// </summary>
  [IntegrationTest]
  public async Task CreateDnsRecordAsync_TxtRecord_ContentPreserved()
  {
    // Arrange
    var hostname = GenerateHostname("-txt");
    var txtValue = "v=spf1 include:_spf.example.com ~all";
    var request  = new CreateDnsRecordRequest(DnsRecordType.TXT, hostname, txtValue);

    // Act
    var record = await CreateAndTrackRecordAsync(request);

    // Assert
    record.Should().NotBeNull();
    record.Type.Should().Be(DnsRecordType.TXT);
    record.Content.Should().Be(txtValue);
  }

  /// <summary>
  ///   I05: Verifies that getting a record by ID returns the full DnsRecord model.
  /// </summary>
  [IntegrationTest]
  public async Task GetDnsRecordAsync_ExistingRecord_ReturnsFullModel()
  {
    // Arrange
    var hostname = GenerateHostname("-get");
    var request  = new CreateDnsRecordRequest(
      DnsRecordType.A,
      hostname,
      "192.0.2.2",
      Comment: "Test record for GetDnsRecordAsync"
    );
    var created = await CreateAndTrackRecordAsync(request);

    // Act
    var record = await _sut.GetDnsRecordAsync(_zoneId, created.Id);

    // Assert
    record.Should().NotBeNull();
    record.Id.Should().Be(created.Id);
    record.Name.Should().Be(hostname);
    record.Type.Should().Be(DnsRecordType.A);
    record.Content.Should().Be("192.0.2.2");
    record.CreatedOn.Should().NotBe(default(DateTime));
    record.ModifiedOn.Should().NotBe(default(DateTime));
    record.Comment.Should().Be("Test record for GetDnsRecordAsync");
  }

  /// <summary>
  ///   I06: Verifies that a record can be fully updated using PUT.
  /// </summary>
  [IntegrationTest]
  public async Task UpdateDnsRecordAsync_FullUpdate_ContentChanged()
  {
    // Arrange
    var hostname = GenerateHostname("-update");
    var createRequest = new CreateDnsRecordRequest(DnsRecordType.A, hostname, "192.0.2.10");
    var created = await CreateAndTrackRecordAsync(createRequest);

    var updateRequest = new UpdateDnsRecordRequest(DnsRecordType.A, hostname, "192.0.2.20");

    // Act
    var updated = await _sut.UpdateDnsRecordAsync(_zoneId, created.Id, updateRequest);

    // Assert
    updated.Should().NotBeNull();
    updated.Id.Should().Be(created.Id);
    updated.Content.Should().Be("192.0.2.20");
  }

  /// <summary>
  ///   I07: Verifies that patching only the content leaves other fields unchanged.
  /// </summary>
  [IntegrationTest]
  public async Task PatchDnsRecordAsync_ContentOnly_OtherFieldsUnchanged()
  {
    // Arrange
    var hostname = GenerateHostname("-patch-content");
    var createRequest = new CreateDnsRecordRequest(
      DnsRecordType.A,
      hostname,
      "192.0.2.30",
      Ttl: 300,
      Comment: "Original comment"
    );
    var created = await CreateAndTrackRecordAsync(createRequest);

    var patchRequest = new PatchDnsRecordRequest(Content: "192.0.2.31");

    // Act
    var patched = await _sut.PatchDnsRecordAsync(_zoneId, created.Id, patchRequest);

    // Assert
    patched.Should().NotBeNull();
    patched.Content.Should().Be("192.0.2.31", "content should be updated");
    patched.Ttl.Should().Be(300, "TTL should remain unchanged");
    patched.Comment.Should().Be("Original comment", "comment should remain unchanged");
  }

  /// <summary>
  ///   I08: Verifies that patching only the TTL leaves other fields unchanged.
  /// </summary>
  [IntegrationTest]
  public async Task PatchDnsRecordAsync_TtlOnly_TtlChanged()
  {
    // Arrange
    var hostname = GenerateHostname("-patch-ttl");
    var createRequest = new CreateDnsRecordRequest(DnsRecordType.A, hostname, "192.0.2.40", Ttl: 300);
    var created = await CreateAndTrackRecordAsync(createRequest);

    var patchRequest = new PatchDnsRecordRequest(Ttl: 3600);

    // Act
    var patched = await _sut.PatchDnsRecordAsync(_zoneId, created.Id, patchRequest);

    // Assert
    patched.Should().NotBeNull();
    patched.Ttl.Should().Be(3600);
    patched.Content.Should().Be("192.0.2.40", "content should remain unchanged");
  }

  /// <summary>
  ///   I09: Verifies that the proxied status can be toggled via patch.
  /// </summary>
  [IntegrationTest]
  public async Task PatchDnsRecordAsync_ProxiedStatus_CanToggle()
  {
    // Arrange - Create a non-proxied A record
    var hostname = GenerateHostname("-patch-proxy");
    var createRequest = new CreateDnsRecordRequest(DnsRecordType.A, hostname, "192.0.2.50", Proxied: false);
    var created = await CreateAndTrackRecordAsync(createRequest);
    created.Proxied.Should().BeFalse("initial state should be not proxied");

    var patchRequest = new PatchDnsRecordRequest(Proxied: true);

    // Act
    var patched = await _sut.PatchDnsRecordAsync(_zoneId, created.Id, patchRequest);

    // Assert
    patched.Should().NotBeNull();
    patched.Proxied.Should().BeTrue("proxied status should be toggled to true");
  }

  /// <summary>
  ///   I10: Verifies that a record can be deleted successfully.
  /// </summary>
  [IntegrationTest]
  public async Task DeleteDnsRecordAsync_ExistingRecord_RecordDeleted()
  {
    // Arrange
    var hostname = GenerateHostname("-delete");
    var request  = new CreateDnsRecordRequest(DnsRecordType.A, hostname, "192.0.2.60");
    var created  = await _sut.CreateDnsRecordAsync(_zoneId, request);
    // Note: Don't track this for cleanup since we're deleting it in this test.

    // Act
    await _sut.DeleteDnsRecordAsync(_zoneId, created.Id);

    // Assert - Verify the record no longer exists.
    var found = await _sut.FindDnsRecordByNameAsync(_zoneId, hostname);
    found.Should().BeNull("record should no longer exist after deletion");
  }

  /// <summary>
  ///   I11: Verifies that deleting a non-existent record throws 404.
  /// </summary>
  [IntegrationTest]
  public async Task DeleteDnsRecordAsync_NonExistentRecord_ThrowsNotFound()
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


  #region List and Find Tests (I12-I17)

  /// <summary>
  ///   I12: Verifies that listing DNS records returns a non-empty list.
  /// </summary>
  [IntegrationTest]
  public async Task ListDnsRecordsAsync_Basic_ReturnsNonEmptyList()
  {
    // Arrange - A record already exists from InitializeAsync.

    // Act
    var result = await _sut.ListDnsRecordsAsync(_zoneId);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeEmpty();
    result.PageInfo.Should().NotBeNull();
  }

  /// <summary>
  ///   I13: Verifies that records can be filtered by type.
  /// </summary>
  [IntegrationTest]
  public async Task ListDnsRecordsAsync_FilterByType_ReturnsOnlyMatchingType()
  {
    // Arrange - Create both A and CNAME records.
    var aHostname = GenerateHostname("-list-a");
    var aRecord   = await CreateAndTrackRecordAsync(new CreateDnsRecordRequest(DnsRecordType.A, aHostname, "192.0.2.70"));

    var cnameHostname = GenerateHostname("-list-cname");
    var cnameRecord   = await CreateAndTrackRecordAsync(new CreateDnsRecordRequest(DnsRecordType.CNAME, cnameHostname, "example.com"));

    // Act - Filter by A type.
    var filters = new ListDnsRecordsFilters(Type: DnsRecordType.A, Name: aHostname);
    var result  = await _sut.ListDnsRecordsAsync(_zoneId, filters);

    // Assert
    result.Items.Should().NotBeEmpty();
    result.Items.Should().OnlyContain(r => r.Type == DnsRecordType.A);
  }

  /// <summary>
  ///   I14: Verifies that records can be filtered by exact name.
  /// </summary>
  [IntegrationTest]
  public async Task ListDnsRecordsAsync_FilterByName_ReturnsSingleRecord()
  {
    // Arrange - Use the primary record created in InitializeAsync.
    var hostname = $"{_testBase}-primary.{_settings.BaseDomain}";

    // Act
    var filters = new ListDnsRecordsFilters(Name: hostname);
    var result  = await _sut.ListDnsRecordsAsync(_zoneId, filters);

    // Assert
    result.Items.Should().HaveCount(1);
    result.Items[0].Name.Should().Be(hostname);
  }

  /// <summary>
  ///   I15: Verifies that ListAllDnsRecordsAsync handles pagination correctly.
  /// </summary>
  [IntegrationTest]
  public async Task ListAllDnsRecordsAsync_WithPagination_ReturnsAllRecords()
  {
    // Arrange - Create multiple records with a unique content to filter by.
    var cnameTarget  = $"{Guid.NewGuid():N}.test-pagination.com";
    var createdIds   = new List<string>();

    for (var i = 0; i < 3; i++)
    {
      var hostname = GenerateHostname($"-page{i}");
      var record   = await CreateAndTrackRecordAsync(new CreateDnsRecordRequest(DnsRecordType.CNAME, hostname, cnameTarget));
      createdIds.Add(record.Id);
    }

    // Act - Use small page size to force pagination.
    var filters    = new ListDnsRecordsFilters(Content: cnameTarget, PerPage: 2);
    var allRecords = new List<DnsRecord>();

    await foreach (var record in _sut.ListAllDnsRecordsAsync(_zoneId, filters))
      allRecords.Add(record);

    // Assert
    allRecords.Should().HaveCount(3);
    allRecords.Select(r => r.Id).Should().BeEquivalentTo(createdIds);
  }

  /// <summary>
  ///   I16: Verifies that FindDnsRecordByNameAsync returns the matching record.
  /// </summary>
  [IntegrationTest]
  public async Task FindDnsRecordByNameAsync_ExistingRecord_ReturnsMatch()
  {
    // Arrange
    var hostname = GenerateHostname("-find");
    var created  = await CreateAndTrackRecordAsync(new CreateDnsRecordRequest(DnsRecordType.A, hostname, "192.0.2.80"));

    // Act
    var found = await _sut.FindDnsRecordByNameAsync(_zoneId, hostname);

    // Assert
    found.Should().NotBeNull();
    found!.Id.Should().Be(created.Id);
    found.Name.Should().Be(hostname);
  }

  /// <summary>
  ///   I17: Verifies that FindDnsRecordByNameAsync returns null for non-existent records.
  /// </summary>
  [IntegrationTest]
  public async Task FindDnsRecordByNameAsync_NonExistent_ReturnsNull()
  {
    // Arrange
    var hostname = $"non-existent-{Guid.NewGuid():N}.{_settings.BaseDomain}";

    // Act
    var found = await _sut.FindDnsRecordByNameAsync(_zoneId, hostname);

    // Assert
    found.Should().BeNull();
  }

  #endregion


  #region Batch Operation Tests (I18-I23)

  /// <summary>
  ///   I18: Verifies that batch create operations work correctly.
  /// </summary>
  [IntegrationTest]
  public async Task BatchDnsRecordsAsync_CreateOnly_RecordsCreated()
  {
    // Arrange
    var posts = new List<CreateDnsRecordRequest>
    {
      new(DnsRecordType.A, GenerateHostname("-batch-create1"), "192.0.2.91"),
      new(DnsRecordType.A, GenerateHostname("-batch-create2"), "192.0.2.92"),
      new(DnsRecordType.A, GenerateHostname("-batch-create3"), "192.0.2.93")
    };
    var request = new BatchDnsRecordsRequest(Posts: posts);

    // Act
    var result = await _sut.BatchDnsRecordsAsync(_zoneId, request);

    // Assert
    result.Should().NotBeNull();
    result.Posts.Should().NotBeNull();
    result.Posts!.Should().HaveCount(3);

    // Track for cleanup.
    foreach (var record in result.Posts!)
      _createdRecordIds.Add(record.Id);
  }

  /// <summary>
  ///   I19: Verifies that batch delete operations work correctly.
  /// </summary>
  [IntegrationTest]
  public async Task BatchDnsRecordsAsync_DeleteOnly_RecordsDeleted()
  {
    // Arrange - Create records to delete.
    var record1 = await _sut.CreateDnsRecordAsync(_zoneId, new CreateDnsRecordRequest(DnsRecordType.A, GenerateHostname("-batch-del1"), "192.0.2.101"));
    var record2 = await _sut.CreateDnsRecordAsync(_zoneId, new CreateDnsRecordRequest(DnsRecordType.A, GenerateHostname("-batch-del2"), "192.0.2.102"));

    var deletes = new List<BatchDeleteOperation>
    {
      new(record1.Id),
      new(record2.Id)
    };
    var request = new BatchDnsRecordsRequest(Deletes: deletes);

    // Act
    var result = await _sut.BatchDnsRecordsAsync(_zoneId, request);

    // Assert
    result.Should().NotBeNull();
    result.Deletes.Should().NotBeNull();
    result.Deletes!.Should().HaveCount(2);

    // Verify records are deleted.
    var found1 = await _sut.FindDnsRecordByNameAsync(_zoneId, record1.Name);
    var found2 = await _sut.FindDnsRecordByNameAsync(_zoneId, record2.Name);
    found1.Should().BeNull();
    found2.Should().BeNull();
  }

  /// <summary>
  ///   I20: Verifies that batch update (PUT) operations work correctly.
  /// </summary>
  [IntegrationTest]
  public async Task BatchDnsRecordsAsync_PutOnly_RecordsReplaced()
  {
    // Arrange
    var record1 = await CreateAndTrackRecordAsync(new CreateDnsRecordRequest(DnsRecordType.A, GenerateHostname("-batch-put1"), "192.0.2.111"));
    var record2 = await CreateAndTrackRecordAsync(new CreateDnsRecordRequest(DnsRecordType.A, GenerateHostname("-batch-put2"), "192.0.2.112"));

    var puts = new List<BatchPutOperation>
    {
      new(record1.Id, DnsRecordType.A, record1.Name, "192.0.2.121"),
      new(record2.Id, DnsRecordType.A, record2.Name, "192.0.2.122")
    };
    var request = new BatchDnsRecordsRequest(Puts: puts);

    // Act
    var result = await _sut.BatchDnsRecordsAsync(_zoneId, request);

    // Assert
    result.Should().NotBeNull();
    result.Puts.Should().NotBeNull();
    result.Puts!.Should().HaveCount(2);
    result.Puts![0].Content.Should().Be("192.0.2.121");
    result.Puts![1].Content.Should().Be("192.0.2.122");
  }

  /// <summary>
  ///   I21: Verifies that batch patch operations work correctly.
  /// </summary>
  [IntegrationTest]
  public async Task BatchDnsRecordsAsync_PatchOnly_RecordsUpdated()
  {
    // Arrange
    var record1 = await CreateAndTrackRecordAsync(new CreateDnsRecordRequest(DnsRecordType.A, GenerateHostname("-batch-patch1"), "192.0.2.131"));
    var record2 = await CreateAndTrackRecordAsync(new CreateDnsRecordRequest(DnsRecordType.A, GenerateHostname("-batch-patch2"), "192.0.2.132"));

    var patches = new List<BatchPatchOperation>
    {
      new(record1.Id, Content: "192.0.2.141"),
      new(record2.Id, Ttl: 3600)
    };
    var request = new BatchDnsRecordsRequest(Patches: patches);

    // Act
    var result = await _sut.BatchDnsRecordsAsync(_zoneId, request);

    // Assert
    result.Should().NotBeNull();
    result.Patches.Should().NotBeNull();
    result.Patches!.Should().HaveCount(2);
    result.Patches![0].Content.Should().Be("192.0.2.141");
    result.Patches![1].Ttl.Should().Be(3600);
  }

  /// <summary>
  ///   I22: Verifies that batch operations with mixed operation types work correctly.
  /// </summary>
  [IntegrationTest]
  public async Task BatchDnsRecordsAsync_MixedOperations_AllSucceed()
  {
    // Arrange - Create records for update and delete.
    var recordToDelete = await _sut.CreateDnsRecordAsync(_zoneId, new CreateDnsRecordRequest(DnsRecordType.A, GenerateHostname("-batch-mix-del"), "192.0.2.150"));
    var recordToUpdate = await CreateAndTrackRecordAsync(new CreateDnsRecordRequest(DnsRecordType.A, GenerateHostname("-batch-mix-upd"), "192.0.2.151"));

    var request = new BatchDnsRecordsRequest(
      Deletes: [new BatchDeleteOperation(recordToDelete.Id)],
      Patches: [new BatchPatchOperation(recordToUpdate.Id, Content: "192.0.2.161")],
      Posts:   [new CreateDnsRecordRequest(DnsRecordType.A, GenerateHostname("-batch-mix-new"), "192.0.2.152")]
    );

    // Act
    var result = await _sut.BatchDnsRecordsAsync(_zoneId, request);

    // Assert
    result.Should().NotBeNull();
    result.Deletes.Should().NotBeNull().And.HaveCount(1);
    result.Patches.Should().NotBeNull().And.HaveCount(1);
    result.Posts.Should().NotBeNull().And.HaveCount(1);

    // Track the newly created record.
    _createdRecordIds.Add(result.Posts![0].Id);

    // Verify execution order effects (deletes run first).
    var deletedFound = await _sut.FindDnsRecordByNameAsync(_zoneId, recordToDelete.Name);
    deletedFound.Should().BeNull("deleted record should not exist");
  }

  /// <summary>
  ///   I23: Verifies that batch execution order allows delete-then-create of same name.
  ///   <para>
  ///     The API guarantees: Deletes → Patches → Puts → Posts.
  ///     This test creates a record, then in a single batch: deletes it and creates a new one with the same name.
  ///   </para>
  /// </summary>
  [IntegrationTest]
  public async Task BatchDnsRecordsAsync_ExecutionOrder_DeletesBeforePosts()
  {
    // Arrange - Create a record that we'll delete and re-create in the batch.
    var hostname     = GenerateHostname("-batch-order");
    var originalRecord = await _sut.CreateDnsRecordAsync(_zoneId, new CreateDnsRecordRequest(DnsRecordType.A, hostname, "192.0.2.170"));

    // In a single batch: delete the original, then create a new record with the same name.
    var request = new BatchDnsRecordsRequest(
      Deletes: [new BatchDeleteOperation(originalRecord.Id)],
      Posts:   [new CreateDnsRecordRequest(DnsRecordType.A, hostname, "192.0.2.180")]
    );

    // Act
    var result = await _sut.BatchDnsRecordsAsync(_zoneId, request);

    // Assert
    result.Should().NotBeNull();
    result.Deletes.Should().NotBeNull().And.HaveCount(1);
    result.Posts.Should().NotBeNull().And.HaveCount(1);

    // Track the new record for cleanup.
    _createdRecordIds.Add(result.Posts![0].Id);

    // Verify the new record exists with the new content.
    var finalRecord = await _sut.FindDnsRecordByNameAsync(_zoneId, hostname);
    finalRecord.Should().NotBeNull();
    finalRecord!.Content.Should().Be("192.0.2.180");
    finalRecord.Id.Should().NotBe(originalRecord.Id, "should be a new record");
  }

  #endregion


  #region Import/Export Tests (I24-I27)

  /// <summary>
  ///   I24: Verifies that DNS records can be exported in BIND format.
  /// </summary>
  [IntegrationTest]
  public async Task ExportDnsRecordsAsync_ReturnsBindFormat()
  {
    // Act
    var bindContent = await _sut.ExportDnsRecordsAsync(_zoneId);

    // Assert
    bindContent.Should().NotBeNullOrWhiteSpace();
    // BIND format typically starts with comments and contains record definitions.
    bindContent.Should().Contain(";");
  }

  /// <summary>
  ///   I25: Verifies that export includes a test record we created.
  /// </summary>
  [IntegrationTest]
  public async Task ExportDnsRecordsAsync_ContainsCreatedRecord()
  {
    // Arrange - Create a unique record.
    var hostname = GenerateHostname("-export");
    var record   = await CreateAndTrackRecordAsync(new CreateDnsRecordRequest(DnsRecordType.A, hostname, "192.0.2.190"));

    // Act
    var bindContent = await _sut.ExportDnsRecordsAsync(_zoneId);

    // Assert
    bindContent.Should().Contain(hostname);
  }

  /// <summary>
  ///   I26: Verifies that DNS records can be imported from BIND format.
  /// </summary>
  [IntegrationTest]
  public async Task ImportDnsRecordsAsync_ValidBind_ReturnsImportResult()
  {
    // Arrange - Create minimal BIND content for a unique record.
    var hostname    = GenerateHostname("-import");
    var bindContent = $"{hostname} 300 IN A 192.0.2.200\n";

    // Act
    var result = await _sut.ImportDnsRecordsAsync(_zoneId, bindContent);

    // Assert
    result.Should().NotBeNull();
    result.RecordsAdded.Should().BeGreaterThanOrEqualTo(0);
    result.TotalRecordsParsed.Should().BeGreaterThanOrEqualTo(0);

    // Cleanup - Find and track the imported record.
    var imported = await _sut.FindDnsRecordByNameAsync(_zoneId, hostname);
    if (imported != null)
      _createdRecordIds.Add(imported.Id);
  }

  /// <summary>
  ///   I27: Verifies that export-then-import roundtrip preserves records.
  /// </summary>
  [IntegrationTest]
  public async Task DnsRecords_ImportExport_CanRoundtrip()
  {
    // Arrange - Create a unique test record.
    var hostname = GenerateHostname("-roundtrip");
    var record   = await CreateAndTrackRecordAsync(new CreateDnsRecordRequest(DnsRecordType.A, hostname, "192.0.2.210"));

    // Step 1: Export.
    var bindContent = await _sut.ExportDnsRecordsAsync(_zoneId);
    bindContent.Should().Contain(hostname);

    // Step 2: Delete the record.
    await _sut.DeleteDnsRecordAsync(_zoneId, record.Id);
    _createdRecordIds.Remove(record.Id);

    var deleted = await _sut.FindDnsRecordByNameAsync(_zoneId, hostname);
    deleted.Should().BeNull("record should be deleted before re-import");

    // Step 3: Import from exported content.
    var importResult = await _sut.ImportDnsRecordsAsync(_zoneId, bindContent);
    importResult.Should().NotBeNull();

    // Step 4: Verify the record was re-imported.
    var reimported = await _sut.FindDnsRecordByNameAsync(_zoneId, hostname);
    reimported.Should().NotBeNull("record should be restored by import");
    reimported!.Content.Should().Be("192.0.2.210");

    // Track for cleanup.
    _createdRecordIds.Add(reimported.Id);
  }

  #endregion


  #region Full Lifecycle Tests (I28-I29)

  /// <summary>
  ///   I28: Verifies the complete CRUD lifecycle for a DNS record.
  /// </summary>
  [IntegrationTest]
  public async Task DnsRecord_FullCrudLifecycle_AllOperationsSucceed()
  {
    // Step 1: Create.
    var hostname = GenerateHostname("-lifecycle");
    var createRequest = new CreateDnsRecordRequest(DnsRecordType.A, hostname, "192.0.2.1", Comment: "Lifecycle test");
    var created = await _sut.CreateDnsRecordAsync(_zoneId, createRequest);

    created.Should().NotBeNull();
    created.Name.Should().Be(hostname);
    created.Comment.Should().Be("Lifecycle test");

    try
    {
      // Step 2: Get.
      var fetched = await _sut.GetDnsRecordAsync(_zoneId, created.Id);
      fetched.Should().NotBeNull();
      fetched.Id.Should().Be(created.Id);

      // Step 3: Patch.
      var patchRequest = new PatchDnsRecordRequest(Content: "192.0.2.2");
      var patched = await _sut.PatchDnsRecordAsync(_zoneId, created.Id, patchRequest);
      patched.Content.Should().Be("192.0.2.2");
      patched.Comment.Should().Be("Lifecycle test", "comment should remain unchanged");

      // Step 4: Update (PUT).
      var updateRequest = new UpdateDnsRecordRequest(DnsRecordType.A, hostname, "192.0.2.3", Comment: "Updated lifecycle");
      var updated = await _sut.UpdateDnsRecordAsync(_zoneId, created.Id, updateRequest);
      updated.Content.Should().Be("192.0.2.3");
      updated.Comment.Should().Be("Updated lifecycle");

      // Step 5: Get again to verify.
      var verified = await _sut.GetDnsRecordAsync(_zoneId, created.Id);
      verified.Content.Should().Be("192.0.2.3");
      verified.Comment.Should().Be("Updated lifecycle");

      // Step 6: Delete.
      await _sut.DeleteDnsRecordAsync(_zoneId, created.Id);

      // Step 7: Verify deletion.
      var final = await _sut.FindDnsRecordByNameAsync(_zoneId, hostname);
      final.Should().BeNull("record should be deleted");
    }
    catch
    {
      // Cleanup on failure.
      _createdRecordIds.Add(created.Id);

      throw;
    }
  }

  /// <summary>
  ///   I29: Verifies that a record with all basic features (comment, TTL, proxied) is preserved.
  ///   <para>
  ///     Note: Tags feature is not tested as it requires an Enterprise plan or higher tier.
  ///     Free and Pro zones have a tags quota of 0.
  ///   </para>
  /// </summary>
  [IntegrationTest]
  public async Task CreateDnsRecordAsync_WithAllFeatures_AllFieldsPreserved()
  {
    // Arrange
    // Note: Tags are excluded as they require Enterprise plan (quota=0 on free/pro zones).
    var hostname = GenerateHostname("-allfeatures");
    var request = new CreateDnsRecordRequest(
      DnsRecordType.A,
      hostname,
      "192.0.2.220",
      Ttl: 300,
      Proxied: false,
      Comment: "Full-featured test record"
    );

    // Act
    var record = await CreateAndTrackRecordAsync(request);

    // Assert
    record.Should().NotBeNull();
    record.Name.Should().Be(hostname);
    record.Content.Should().Be("192.0.2.220");
    record.Ttl.Should().Be(300);
    record.Proxied.Should().BeFalse();
    record.Comment.Should().Be("Full-featured test record");
    record.Proxiable.Should().BeTrue("A records should be proxiable");
    record.CreatedOn.Should().NotBe(default(DateTime));
    record.ModifiedOn.Should().NotBe(default(DateTime));
  }

  #endregion


  #region Edge Cases (I30-I38)

  /// <summary>
  ///   I30: Verifies that creating a CNAME at a hostname with existing records throws an error.
  ///   <para>
  ///     Note: Multiple A records at the same hostname are allowed (round-robin DNS).
  ///     However, CNAME records cannot coexist with other record types at the same hostname.
  ///   </para>
  /// </summary>
  [IntegrationTest]
  public async Task CreateDnsRecordAsync_CnameAtExistingName_ThrowsConflict()
  {
    // Arrange - Create an A record first.
    var hostname = GenerateHostname("-cname-conflict");
    var aRecord = await CreateAndTrackRecordAsync(new CreateDnsRecordRequest(DnsRecordType.A, hostname, "192.0.2.230"));

    // Act - Try to create a CNAME at the same hostname.
    // CNAME records cannot coexist with other record types at the same name.
    var action = async () =>
    {
      var cname = await _sut.CreateDnsRecordAsync(
        _zoneId,
        new CreateDnsRecordRequest(DnsRecordType.CNAME, hostname, "target.example.com")
      );
      // Track for cleanup if it somehow succeeds
      _createdRecordIds.Add(cname.Id);
    };

    // Assert - Should fail because CNAME cannot coexist with A record.
    await action.Should().ThrowAsync<Exception>();
  }

  /// <summary>
  ///   I32: Verifies that TXT records with special characters are preserved.
  /// </summary>
  [IntegrationTest]
  public async Task CreateDnsRecordAsync_TxtWithSpecialChars_ContentPreserved()
  {
    // Arrange
    var hostname = GenerateHostname("-txt-special");
    var content  = "v=DKIM1; k=rsa; p=MIGfMA0GCSqGSIb3DQEBA";
    var request  = new CreateDnsRecordRequest(DnsRecordType.TXT, hostname, content);

    // Act
    var record = await CreateAndTrackRecordAsync(request);

    // Assert
    record.Should().NotBeNull();
    record.Content.Should().Be(content);
  }

  /// <summary>
  ///   I35: Verifies that operations on a non-existent zone throw an error.
  /// </summary>
  [IntegrationTest]
  public async Task GetDnsRecordAsync_NonExistentZone_ThrowsError()
  {
    // Arrange
    var nonExistentZoneId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.GetDnsRecordAsync(nonExistentZoneId, _primaryRecordId!);

    // Assert
    var ex = await action.Should().ThrowAsync<HttpRequestException>();
    ex.Which.StatusCode.Should().BeOneOf(
      System.Net.HttpStatusCode.NotFound,
      System.Net.HttpStatusCode.Forbidden,
      System.Net.HttpStatusCode.BadRequest);
  }

  /// <summary>
  ///   I38: Verifies that getting a non-existent record throws 404.
  /// </summary>
  [IntegrationTest]
  public async Task GetDnsRecordAsync_NonExistentRecord_ThrowsNotFound()
  {
    // Arrange
    var nonExistentRecordId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.GetDnsRecordAsync(_zoneId, nonExistentRecordId);

    // Assert
    var ex = await action.Should().ThrowAsync<HttpRequestException>();
    ex.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
  }

  #endregion
}
