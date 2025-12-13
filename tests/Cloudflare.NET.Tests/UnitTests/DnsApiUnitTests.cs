namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using Cloudflare.NET.Dns;
using Cloudflare.NET.Zones.Models;
using Dns.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Shared.Fixtures;
using Shared.Mocks;
using DnsRecord = Cloudflare.NET.Dns.Models.DnsRecord;
using ListDnsRecordsFilters = Cloudflare.NET.Dns.Models.ListDnsRecordsFilters;
using CreateDnsRecordRequest = Cloudflare.NET.Dns.Models.CreateDnsRecordRequest;
using UpdateDnsRecordRequest = Cloudflare.NET.Dns.Models.UpdateDnsRecordRequest;
using PatchDnsRecordRequest = Cloudflare.NET.Dns.Models.PatchDnsRecordRequest;
using BatchDnsRecordsRequest = Cloudflare.NET.Dns.Models.BatchDnsRecordsRequest;
using BatchDeleteOperation = Cloudflare.NET.Dns.Models.BatchDeleteOperation;
using BatchPatchOperation = Cloudflare.NET.Dns.Models.BatchPatchOperation;

/// <summary>
///   Unit tests for the <see cref="DnsApi"/> class.
///   Tests cover request construction, URL encoding, response deserialization, and parameter validation.
/// </summary>
[Trait("Category", "Unit")]
public class DnsApiUnitTests
{
  #region Properties & Fields - Non-Public

  /// <summary>Well-known test zone ID.</summary>
  private const string TestZoneId = TestDataFactory.TestZoneId;

  /// <summary>Well-known test DNS record ID.</summary>
  private const string TestDnsRecordId = TestDataFactory.TestDnsRecordId;

  /// <summary>Test domain for constructing record names.</summary>
  private const string TestDomain = TestDataFactory.TestDomain;

  #endregion


  #region Test Methods - GetDnsRecordAsync (Request Construction)

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U01_GetDnsRecordAsync_ConstructsCorrectEndpoint()
  {
    // Arrange
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreateFullDnsRecordResponse(),
      HttpStatusCode.OK,
      (req, _) => capturedRequest = req
    );
    var api = CreateDnsApi(mockHandler.Object);

    // Act
    await api.GetDnsRecordAsync(TestZoneId, TestDnsRecordId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/zones/{TestZoneId}/dns_records/{TestDnsRecordId}");
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U02_GetDnsRecordAsync_EncodesSpecialCharactersInZoneId()
  {
    // Arrange
    const string specialZoneId = "zone/with+special&chars";
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreateFullDnsRecordResponse(),
      HttpStatusCode.OK,
      (req, _) => capturedRequest = req
    );
    var api = CreateDnsApi(mockHandler.Object);

    // Act
    await api.GetDnsRecordAsync(specialZoneId, TestDnsRecordId);

    // Assert
    capturedRequest!.RequestUri!.ToString().Should().Contain("zone%2Fwith%2Bspecial%26chars");
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U03_GetDnsRecordAsync_EncodesSpecialCharactersInRecordId()
  {
    // Arrange
    const string specialRecordId = "record/with+special&chars";
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreateFullDnsRecordResponse(),
      HttpStatusCode.OK,
      (req, _) => capturedRequest = req
    );
    var api = CreateDnsApi(mockHandler.Object);

    // Act
    await api.GetDnsRecordAsync(TestZoneId, specialRecordId);

    // Assert
    capturedRequest!.RequestUri!.ToString().Should().Contain("record%2Fwith%2Bspecial%26chars");
  }

  #endregion


  #region Test Methods - GetDnsRecordAsync (Response Deserialization)

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U04_GetDnsRecordAsync_DeserializesAllProperties()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(CreateFullDnsRecordResponse(), HttpStatusCode.OK);
    var api         = CreateDnsApi(mockHandler.Object);

    // Act
    var record = await api.GetDnsRecordAsync(TestZoneId, TestDnsRecordId);

    // Assert
    record.Id.Should().Be(TestDnsRecordId);
    record.Name.Should().Be($"test.{TestDomain}");
    record.Type.Should().Be(DnsRecordType.A);
    record.Content.Should().Be("192.0.2.1");
    record.Proxied.Should().BeFalse();
    record.Proxiable.Should().BeTrue();
    record.Ttl.Should().Be(3600);
    record.CreatedOn.Should().NotBe(default);
    record.ModifiedOn.Should().NotBe(default);
    record.Comment.Should().Be("Test comment");
    record.Tags.Should().Contain("env:test");
    record.Priority.Should().BeNull();
    record.Meta.Should().NotBeNull();
    record.Meta!.AutoAdded.Should().BeFalse();
    record.Meta.Source.Should().Be("primary");
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U05_GetDnsRecordAsync_DeserializesMxRecordWithPriority()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(CreateDnsRecordResponseWithPriority(), HttpStatusCode.OK);
    var api         = CreateDnsApi(mockHandler.Object);

    // Act
    var record = await api.GetDnsRecordAsync(TestZoneId, TestDnsRecordId);

    // Assert
    record.Type.Should().Be(DnsRecordType.MX);
    record.Content.Should().Be("mail.example.com");
    record.Priority.Should().Be(10);
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U06_GetDnsRecordAsync_DeserializesRecordWithSettings()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(CreateDnsRecordResponseWithSettings(), HttpStatusCode.OK);
    var api         = CreateDnsApi(mockHandler.Object);

    // Act
    var record = await api.GetDnsRecordAsync(TestZoneId, TestDnsRecordId);

    // Assert
    record.Settings.Should().NotBeNull();
    record.Settings!.Ipv4Only.Should().BeTrue();
    record.Settings.Ipv6Only.Should().BeFalse();
  }

  #endregion


  #region Test Methods - ListDnsRecordsAsync (Request Construction)

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U07_ListDnsRecordsAsync_ConstructsCorrectEndpointWithoutFilters()
  {
    // Arrange
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreatePaginatedDnsRecordsResponse(1),
      HttpStatusCode.OK,
      (req, _) => capturedRequest = req
    );
    var api = CreateDnsApi(mockHandler.Object);

    // Act
    await api.ListDnsRecordsAsync(TestZoneId);

    // Assert
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/zones/{TestZoneId}/dns_records");
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U08_ListDnsRecordsAsync_AppliesTypeFilter()
  {
    // Arrange
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreatePaginatedDnsRecordsResponse(1),
      HttpStatusCode.OK,
      (req, _) => capturedRequest = req
    );
    var api     = CreateDnsApi(mockHandler.Object);
    var filters = new ListDnsRecordsFilters(Type: DnsRecordType.A);

    // Act
    await api.ListDnsRecordsAsync(TestZoneId, filters);

    // Assert
    capturedRequest!.RequestUri!.ToString().Should().Contain("type=A");
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U09_ListDnsRecordsAsync_AppliesNameFilter()
  {
    // Arrange
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreatePaginatedDnsRecordsResponse(1),
      HttpStatusCode.OK,
      (req, _) => capturedRequest = req
    );
    var api     = CreateDnsApi(mockHandler.Object);
    var filters = new ListDnsRecordsFilters(Name: "www.example.com");

    // Act
    await api.ListDnsRecordsAsync(TestZoneId, filters);

    // Assert
    capturedRequest!.RequestUri!.ToString().Should().Contain("name=www.example.com");
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U10_ListDnsRecordsAsync_AppliesProxiedFilter()
  {
    // Arrange
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreatePaginatedDnsRecordsResponse(1),
      HttpStatusCode.OK,
      (req, _) => capturedRequest = req
    );
    var api     = CreateDnsApi(mockHandler.Object);
    var filters = new ListDnsRecordsFilters(Proxied: true);

    // Act
    await api.ListDnsRecordsAsync(TestZoneId, filters);

    // Assert
    capturedRequest!.RequestUri!.ToString().Should().Contain("proxied=true");
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U11_ListDnsRecordsAsync_AppliesPaginationFilters()
  {
    // Arrange
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreatePaginatedDnsRecordsResponse(1),
      HttpStatusCode.OK,
      (req, _) => capturedRequest = req
    );
    var api     = CreateDnsApi(mockHandler.Object);
    var filters = new ListDnsRecordsFilters(Page: 2, PerPage: 50);

    // Act
    await api.ListDnsRecordsAsync(TestZoneId, filters);

    // Assert
    capturedRequest!.RequestUri!.ToString().Should().Contain("page=2");
    capturedRequest.RequestUri.ToString().Should().Contain("per_page=50");
  }

  #endregion


  #region Test Methods - CreateDnsRecordAsync (Request Construction)

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U12_CreateDnsRecordAsync_ConstructsCorrectEndpoint()
  {
    // Arrange
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreateFullDnsRecordResponse(),
      HttpStatusCode.OK,
      (req, _) => capturedRequest = req
    );
    var api     = CreateDnsApi(mockHandler.Object);
    var request = new CreateDnsRecordRequest(DnsRecordType.A, "test.example.com", "192.0.2.1");

    // Act
    await api.CreateDnsRecordAsync(TestZoneId, request);

    // Assert
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/zones/{TestZoneId}/dns_records");
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U13_CreateDnsRecordAsync_SerializesRequestBody()
  {
    // Arrange
    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreateFullDnsRecordResponse(),
      HttpStatusCode.OK,
      (req, _) =>
      {
        capturedRequest  = req;
        capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
      }
    );
    var api     = CreateDnsApi(mockHandler.Object);
    var request = new CreateDnsRecordRequest(
      DnsRecordType.A,
      "test.example.com",
      "192.0.2.1",
      Ttl: 3600,
      Proxied: true,
      Comment: "Test record"
    );

    // Act
    await api.CreateDnsRecordAsync(TestZoneId, request);

    // Assert
    capturedJsonBody.Should().Contain("\"type\":\"A\"");
    capturedJsonBody.Should().Contain("\"name\":\"test.example.com\"");
    capturedJsonBody.Should().Contain("\"content\":\"192.0.2.1\"");
    capturedJsonBody.Should().Contain("\"ttl\":3600");
    capturedJsonBody.Should().Contain("\"proxied\":true");
    capturedJsonBody.Should().Contain("\"comment\":\"Test record\"");
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U14_CreateDnsRecordAsync_SerializesMxRecordWithPriority()
  {
    // Arrange
    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreateDnsRecordResponseWithPriority(),
      HttpStatusCode.OK,
      (req, _) =>
      {
        capturedRequest  = req;
        capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
      }
    );
    var api     = CreateDnsApi(mockHandler.Object);
    var request = new CreateDnsRecordRequest(
      DnsRecordType.MX,
      "example.com",
      "mail.example.com",
      Priority: 10
    );

    // Act
    await api.CreateDnsRecordAsync(TestZoneId, request);

    // Assert
    capturedJsonBody.Should().Contain("\"type\":\"MX\"");
    capturedJsonBody.Should().Contain("\"priority\":10");
  }

  #endregion


  #region Test Methods - UpdateDnsRecordAsync (Request Construction)

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U15_UpdateDnsRecordAsync_UsesPutMethod()
  {
    // Arrange
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreateFullDnsRecordResponse(),
      HttpStatusCode.OK,
      (req, _) => capturedRequest = req
    );
    var api     = CreateDnsApi(mockHandler.Object);
    var request = new UpdateDnsRecordRequest(DnsRecordType.A, "test.example.com", "192.0.2.2");

    // Act
    await api.UpdateDnsRecordAsync(TestZoneId, TestDnsRecordId, request);

    // Assert
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should().Contain($"/dns_records/{TestDnsRecordId}");
  }

  #endregion


  #region Test Methods - PatchDnsRecordAsync (Request Construction)

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U16_PatchDnsRecordAsync_UsesPatchMethod()
  {
    // Arrange
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreateFullDnsRecordResponse(),
      HttpStatusCode.OK,
      (req, _) => capturedRequest = req
    );
    var api   = CreateDnsApi(mockHandler.Object);
    var patch = new PatchDnsRecordRequest(Content: "192.0.2.100");

    // Act
    await api.PatchDnsRecordAsync(TestZoneId, TestDnsRecordId, patch);

    // Assert
    capturedRequest!.Method.Should().Be(HttpMethod.Patch);
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U17_PatchDnsRecordAsync_OmitsNullFields()
  {
    // Arrange
    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreateFullDnsRecordResponse(),
      HttpStatusCode.OK,
      (req, _) =>
      {
        capturedRequest  = req;
        capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
      }
    );
    var api   = CreateDnsApi(mockHandler.Object);
    var patch = new PatchDnsRecordRequest(Content: "192.0.2.100");

    // Act
    await api.PatchDnsRecordAsync(TestZoneId, TestDnsRecordId, patch);

    // Assert
    // Should only contain the content field, not null fields
    capturedJsonBody.Should().Contain("\"content\":\"192.0.2.100\"");
    capturedJsonBody.Should().NotContain("\"type\":");
    capturedJsonBody.Should().NotContain("\"name\":");
  }

  #endregion


  #region Test Methods - DeleteDnsRecordAsync

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U18_DeleteDnsRecordAsync_UsesDeleteMethod()
  {
    // Arrange
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      """{"success":true,"errors":[],"messages":[],"result":{"id":"372e67954025e0ba6aaa6d586b9e0b59"}}""",
      HttpStatusCode.OK,
      (req, _) => capturedRequest = req
    );
    var api = CreateDnsApi(mockHandler.Object);

    // Act
    await api.DeleteDnsRecordAsync(TestZoneId, TestDnsRecordId);

    // Assert
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should().Contain($"/dns_records/{TestDnsRecordId}");
  }

  #endregion


  #region Test Methods - BatchDnsRecordsAsync

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U19_BatchDnsRecordsAsync_ConstructsCorrectEndpoint()
  {
    // Arrange
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreateBatchDnsRecordsResponse(),
      HttpStatusCode.OK,
      (req, _) => capturedRequest = req
    );
    var api   = CreateDnsApi(mockHandler.Object);
    var batch = new BatchDnsRecordsRequest(
      Posts: [new CreateDnsRecordRequest(DnsRecordType.A, "new.example.com", "192.0.2.1")]
    );

    // Act
    await api.BatchDnsRecordsAsync(TestZoneId, batch);

    // Assert
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Contain("/dns_records/batch");
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U20_BatchDnsRecordsAsync_SerializesAllOperationTypes()
  {
    // Arrange
    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreateBatchDnsRecordsResponse(),
      HttpStatusCode.OK,
      (req, _) =>
      {
        capturedRequest  = req;
        capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
      }
    );
    var api   = CreateDnsApi(mockHandler.Object);
    var batch = new BatchDnsRecordsRequest(
      Deletes: [new BatchDeleteOperation("delete-id")],
      Patches: [new BatchPatchOperation("patch-id", Content: "192.0.2.100")],
      Posts: [new CreateDnsRecordRequest(DnsRecordType.A, "new.example.com", "192.0.2.1")]
    );

    // Act
    await api.BatchDnsRecordsAsync(TestZoneId, batch);

    // Assert
    capturedJsonBody.Should().Contain("\"deletes\"");
    capturedJsonBody.Should().Contain("\"patches\"");
    capturedJsonBody.Should().Contain("\"posts\"");
    capturedJsonBody.Should().Contain("delete-id");
    capturedJsonBody.Should().Contain("patch-id");
    capturedJsonBody.Should().Contain("new.example.com");
  }

  #endregion


  #region Test Methods - ExportDnsRecordsAsync

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U21_ExportDnsRecordsAsync_ConstructsCorrectEndpoint()
  {
    // Arrange
    const string bindContent = """
      ; Zone file for example.com
      $ORIGIN example.com.
      $TTL 3600
      @    IN    A     192.0.2.1
      www  IN    A     192.0.2.2
      """;
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      bindContent,
      HttpStatusCode.OK,
      (req, _) => capturedRequest = req
    );
    var api = CreateDnsApi(mockHandler.Object);

    // Act
    var result = await api.ExportDnsRecordsAsync(TestZoneId);

    // Assert
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Contain("/dns_records/export");
    result.Should().Contain("Zone file for example.com");
  }

  #endregion


  #region Test Methods - Parameter Validation

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  [Trait("Category", "Unit")]
  public async Task U22_GetDnsRecordAsync_ThrowsOnInvalidZoneId(string? invalidZoneId)
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(CreateFullDnsRecordResponse(), HttpStatusCode.OK);
    var api         = CreateDnsApi(mockHandler.Object);

    // Act
    var act = () => api.GetDnsRecordAsync(invalidZoneId!, TestDnsRecordId);

    // Assert
    await act.Should().ThrowAsync<ArgumentException>();
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  [Trait("Category", "Unit")]
  public async Task U23_GetDnsRecordAsync_ThrowsOnInvalidRecordId(string? invalidRecordId)
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(CreateFullDnsRecordResponse(), HttpStatusCode.OK);
    var api         = CreateDnsApi(mockHandler.Object);

    // Act
    var act = () => api.GetDnsRecordAsync(TestZoneId, invalidRecordId!);

    // Assert
    await act.Should().ThrowAsync<ArgumentException>();
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U24_CreateDnsRecordAsync_ThrowsOnNullRequest()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(CreateFullDnsRecordResponse(), HttpStatusCode.OK);
    var api         = CreateDnsApi(mockHandler.Object);

    // Act
    var act = () => api.CreateDnsRecordAsync(TestZoneId, null!);

    // Assert
    await act.Should().ThrowAsync<ArgumentNullException>();
  }

  #endregion


  #region Test Methods - FindDnsRecordByNameAsync

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U25_FindDnsRecordByNameAsync_ReturnsFirstMatchingRecord()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(CreatePaginatedDnsRecordsResponse(1), HttpStatusCode.OK);
    var api         = CreateDnsApi(mockHandler.Object);

    // Act
    var record = await api.FindDnsRecordByNameAsync(TestZoneId, $"test.{TestDomain}");

    // Assert
    record.Should().NotBeNull();
    record!.Name.Should().Contain("example.com");
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U26_FindDnsRecordByNameAsync_ReturnsNullWhenNotFound()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(CreatePaginatedDnsRecordsResponse(0), HttpStatusCode.OK);
    var api         = CreateDnsApi(mockHandler.Object);

    // Act
    var record = await api.FindDnsRecordByNameAsync(TestZoneId, "nonexistent.example.com");

    // Assert
    record.Should().BeNull();
  }

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U27_FindDnsRecordByNameAsync_IncludesTypeFilter()
  {
    // Arrange
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreatePaginatedDnsRecordsResponse(1),
      HttpStatusCode.OK,
      (req, _) => capturedRequest = req
    );
    var api = CreateDnsApi(mockHandler.Object);

    // Act
    await api.FindDnsRecordByNameAsync(TestZoneId, "test.example.com", DnsRecordType.AAAA);

    // Assert
    capturedRequest!.RequestUri!.ToString().Should().Contain("type=AAAA");
    capturedRequest.RequestUri.ToString().Should().Contain("name=test.example.com");
  }

  #endregion


  #region Test Methods - CreateCnameRecordAsync

  [Fact]
  [Trait("Category", "Unit")]
  public async Task U28_CreateCnameRecordAsync_CreatesCnameType()
  {
    // Arrange
    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      CreateCnameDnsRecordResponse(),
      HttpStatusCode.OK,
      (req, _) =>
      {
        capturedRequest  = req;
        capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
      }
    );
    var api = CreateDnsApi(mockHandler.Object);

    // Act
    await api.CreateCnameRecordAsync(TestZoneId, "cdn.example.com", "cdn.provider.com", proxied: true);

    // Assert
    capturedJsonBody.Should().Contain("\"type\":\"CNAME\"");
    capturedJsonBody.Should().Contain("\"name\":\"cdn.example.com\"");
    capturedJsonBody.Should().Contain("\"content\":\"cdn.provider.com\"");
    capturedJsonBody.Should().Contain("\"proxied\":true");
  }

  #endregion


  #region Helper Methods - Test Infrastructure

  /// <summary>Creates a DnsApi instance with a mock HTTP handler.</summary>
  private static DnsApi CreateDnsApi(HttpMessageHandler handler)
  {
    var httpClient = new HttpClient(handler)
    {
      BaseAddress = new Uri("https://api.cloudflare.com/client/v4/")
    };
    var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug));

    return new DnsApi(httpClient, loggerFactory);
  }

  #endregion


  #region Helper Methods - Response Builders

  /// <summary>Creates a full DNS record response with all properties.</summary>
  private static string CreateFullDnsRecordResponse()
  {
    var record = new
    {
      id          = TestDnsRecordId,
      name        = $"test.{TestDomain}",
      type        = "A",
      content     = "192.0.2.1",
      proxied     = false,
      proxiable   = true,
      ttl         = 3600,
      created_on  = TestDataFactory.TestCreatedOnString,
      modified_on = TestDataFactory.TestModifiedOnString,
      comment     = "Test comment",
      tags        = new[] { "env:test" },
      meta = new
      {
        auto_added = false,
        source     = "primary"
      }
    };

    return JsonSerializer.Serialize(new { success = true, errors = Array.Empty<object>(), messages = Array.Empty<object>(), result = record });
  }

  /// <summary>Creates a DNS record response with priority (MX record).</summary>
  private static string CreateDnsRecordResponseWithPriority()
  {
    var record = new
    {
      id          = TestDnsRecordId,
      name        = TestDomain,
      type        = "MX",
      content     = "mail.example.com",
      proxied     = false,
      proxiable   = false,
      ttl         = 3600,
      priority    = 10,
      created_on  = TestDataFactory.TestCreatedOnString,
      modified_on = TestDataFactory.TestModifiedOnString
    };

    return JsonSerializer.Serialize(new { success = true, errors = Array.Empty<object>(), messages = Array.Empty<object>(), result = record });
  }

  /// <summary>Creates a DNS record response with settings.</summary>
  private static string CreateDnsRecordResponseWithSettings()
  {
    var record = new
    {
      id          = TestDnsRecordId,
      name        = $"test.{TestDomain}",
      type        = "A",
      content     = "192.0.2.1",
      proxied     = false,
      proxiable   = true,
      ttl         = 3600,
      created_on  = TestDataFactory.TestCreatedOnString,
      modified_on = TestDataFactory.TestModifiedOnString,
      settings = new
      {
        ipv4_only = true,
        ipv6_only = false
      }
    };

    return JsonSerializer.Serialize(new { success = true, errors = Array.Empty<object>(), messages = Array.Empty<object>(), result = record });
  }

  /// <summary>Creates a CNAME DNS record response.</summary>
  private static string CreateCnameDnsRecordResponse()
  {
    var record = new
    {
      id          = TestDnsRecordId,
      name        = $"cdn.{TestDomain}",
      type        = "CNAME",
      content     = "cdn.provider.com",
      proxied     = true,
      proxiable   = true,
      ttl         = 1,
      created_on  = TestDataFactory.TestCreatedOnString,
      modified_on = TestDataFactory.TestModifiedOnString
    };

    return JsonSerializer.Serialize(new { success = true, errors = Array.Empty<object>(), messages = Array.Empty<object>(), result = record });
  }

  /// <summary>Creates a paginated DNS records response.</summary>
  private static string CreatePaginatedDnsRecordsResponse(int count)
  {
    var records = Enumerable.Range(0, count).Select(i => new
    {
      id          = TestDataFactory.GenerateId(),
      name        = $"record{i + 1}.{TestDomain}",
      type        = "A",
      content     = $"192.0.2.{i + 1}",
      proxied     = false,
      proxiable   = true,
      ttl         = 3600,
      created_on  = TestDataFactory.TestCreatedOnString,
      modified_on = TestDataFactory.TestModifiedOnString
    }).ToArray();

    var response = new
    {
      success     = true,
      errors      = Array.Empty<object>(),
      messages    = Array.Empty<object>(),
      result      = records,
      result_info = new
      {
        page        = 1,
        per_page    = 100,
        count       = count,
        total_count = count,
        total_pages = 1
      }
    };

    return JsonSerializer.Serialize(response);
  }

  /// <summary>Creates a batch DNS records response.</summary>
  private static string CreateBatchDnsRecordsResponse()
  {
    var result = new
    {
      posts = new[]
      {
        new
        {
          id          = TestDataFactory.GenerateId(),
          name        = $"new.{TestDomain}",
          type        = "A",
          content     = "192.0.2.1",
          proxied     = false,
          proxiable   = true,
          ttl         = 1,
          created_on  = TestDataFactory.TestCreatedOnString,
          modified_on = TestDataFactory.TestModifiedOnString
        }
      }
    };

    return JsonSerializer.Serialize(new { success = true, errors = Array.Empty<object>(), messages = Array.Empty<object>(), result });
  }

  #endregion
}
