namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using Cloudflare.NET.AuditLogs;
using Cloudflare.NET.AuditLogs.Models;
using Cloudflare.NET.Core.Exceptions;
using Cloudflare.NET.Security.Firewall.Models;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>Contains unit tests for the <see cref="AuditLogsApi" /> class implementing F07 - Account Audit Logs and F15 - User Audit Logs.</summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class AuditLogsApiUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly ILoggerFactory _loggerFactory;
  private const string TestAccountId = "test-account-id-12345";

  #endregion


  #region Constructors

  public AuditLogsApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion


  #region Request Construction Tests (U01-U12)

  /// <summary>U01: Verifies that GetAccountAuditLogsAsync with no filters sends a GET request to /accounts/{accountId}/logs/audit.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_NoFilters_SendsCorrectGetRequest()
  {
    // Arrange
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/logs/audit");
  }

  /// <summary>U02: Verifies that GetAccountAuditLogsAsync with cursor includes cursor in query string.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_WithCursor_IncludesCursorInQueryString()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(Cursor: "abc123");
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountAuditLogsAsync(TestAccountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("cursor=abc123");
  }

  /// <summary>U03: Verifies that GetAccountAuditLogsAsync with limit includes limit in query string.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_WithLimit_IncludesLimitInQueryString()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(Limit: 50);
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountAuditLogsAsync(TestAccountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("limit=50");
  }

  /// <summary>U04: Verifies that GetAccountAuditLogsAsync with Since includes since in ISO 8601 format.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_WithSince_IncludesSinceInIso8601Format()
  {
    // Arrange
    var since = new DateTime(2024, 6, 15, 12, 30, 0, DateTimeKind.Utc);
    var filters = new ListAuditLogsFilters(Since: since);
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountAuditLogsAsync(TestAccountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    // Should contain URL-encoded ISO 8601 format (: becomes %3A)
    capturedRequest!.RequestUri!.Query.Should().Contain("since=");
    capturedRequest.RequestUri.Query.Should().Contain("2024-06-15");
  }

  /// <summary>U05: Verifies that GetAccountAuditLogsAsync with Before includes before in query string.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_WithBefore_IncludesBeforeInQueryString()
  {
    // Arrange
    var before = new DateTime(2024, 6, 20, 18, 0, 0, DateTimeKind.Utc);
    var filters = new ListAuditLogsFilters(Before: before);
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountAuditLogsAsync(TestAccountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("before=");
    capturedRequest.RequestUri.Query.Should().Contain("2024-06-20");
  }

  /// <summary>U06: Verifies that GetAccountAuditLogsAsync with ActorEmails includes actor_email filter with URL encoding.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_WithActorEmails_IncludesActorEmailFilterUrlEncoded()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(ActorEmails: new[] { "a@b.com" });
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountAuditLogsAsync(TestAccountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    // @ becomes %40 in URL encoding
    capturedRequest!.RequestUri!.Query.Should().Contain("actor_email=a%40b.com");
  }

  /// <summary>U07: Verifies that GetAccountAuditLogsAsync with ActorEmailsNot includes actor_email.not filter.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_WithActorEmailsNot_IncludesActorEmailNotFilter()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(ActorEmailsNot: new[] { "exclude@example.com" });
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountAuditLogsAsync(TestAccountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("actor_email.not=");
  }

  /// <summary>U08: Verifies that GetAccountAuditLogsAsync with ActionTypes includes action_type filter.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_WithActionTypes_IncludesActionTypeFilter()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(ActionTypes: new[] { "create" });
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountAuditLogsAsync(TestAccountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("action_type=create");
  }

  /// <summary>U09: Verifies that GetAccountAuditLogsAsync with ActionTypesNot includes action_type.not filter.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_WithActionTypesNot_IncludesActionTypeNotFilter()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(ActionTypesNot: new[] { "delete" });
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountAuditLogsAsync(TestAccountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("action_type.not=delete");
  }

  /// <summary>U10: Verifies that GetAccountAuditLogsAsync with ZoneIds includes zone_id filter.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_WithZoneIds_IncludesZoneIdFilter()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(ZoneIds: new[] { "zone-123" });
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountAuditLogsAsync(TestAccountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("zone_id=zone-123");
  }

  /// <summary>U11: Verifies that GetAccountAuditLogsAsync with multiple filters includes all filters in query string.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_WithMultipleFilters_IncludesAllFiltersInQueryString()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(
      Limit: 25,
      ActionTypes: new[] { "create", "update" },
      ActionResults: new[] { "success" },
      ZoneIds: new[] { "zone-abc" }
    );
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountAuditLogsAsync(TestAccountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    var query = capturedRequest!.RequestUri!.Query;
    query.Should().Contain("limit=25");
    query.Should().Contain("action_type=create");
    query.Should().Contain("action_type=update");
    query.Should().Contain("action_result=success");
    query.Should().Contain("zone_id=zone-abc");
  }

  /// <summary>U12: Verifies that GetAccountAuditLogsAsync with RawStatusCodes includes raw_status_code filters.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_WithRawStatusCodes_IncludesRawStatusCodeFilters()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(RawStatusCodes: new[] { 200, 201 });
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountAuditLogsAsync(TestAccountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    var query = capturedRequest!.RequestUri!.Query;
    query.Should().Contain("raw_status_code=200");
    query.Should().Contain("raw_status_code=201");
  }

  #endregion


  #region Response Deserialization Tests (U13-U21)

  /// <summary>U13: Verifies that AuditLog model deserializes all properties from a complete JSON response.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_FullModel_DeserializesAllProperties()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "audit-log-123",
        "account": { "id": "acc-123", "name": "Test Account" },
        "action": { "description": "Created zone", "result": "success", "time": "2024-06-15T12:30:00Z", "type": "create" },
        "actor": { "id": "user-456", "context": "api", "email": "admin@example.com", "ip_address": "192.168.1.1", "token_id": "tok-789", "token_name": "API Token", "type": "user" },
        "raw": { "cf_ray_id": "ray-abc", "method": "POST", "status_code": 200, "uri": "/client/v4/zones", "user_agent": "Cloudflare-SDK" },
        "resource": { "id": "res-123", "product": "zones", "scope": "account", "type": "zone" },
        "zone": { "id": "zone-xyz", "name": "example.com" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    result.Items.Should().HaveCount(1);
    var log = result.Items[0];
    log.Id.Should().Be("audit-log-123");
    log.Account.Id.Should().Be("acc-123");
    log.Account.Name.Should().Be("Test Account");
    log.Action.Type.Should().Be("create");
    log.Action.Result.Should().Be("success");
    log.Action.Description.Should().Be("Created zone");
    log.Actor.Id.Should().Be("user-456");
    log.Actor.Email.Should().Be("admin@example.com");
    log.Raw.Should().NotBeNull();
    log.Raw!.Method.Should().Be("POST");
    log.Resource.Should().NotBeNull();
    log.Resource!.Product.Should().Be("zones");
    log.Zone.Should().NotBeNull();
    log.Zone!.Name.Should().Be("example.com");
  }

  /// <summary>U14: Verifies that AuditLog model with missing optional fields has null for those properties.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_OptionalFieldsNull_DeserializesWithNulls()
  {
    // Arrange - minimal audit log with only required fields
    var auditLogJson = """
      {
        "id": "audit-log-minimal",
        "account": { "id": "acc-min" },
        "action": { "result": "success", "time": "2024-06-15T12:30:00Z", "type": "delete" },
        "actor": { "id": "system" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    result.Items.Should().HaveCount(1);
    var log = result.Items[0];
    log.Id.Should().Be("audit-log-minimal");
    log.Account.Name.Should().BeNull();
    log.Action.Description.Should().BeNull();
    log.Actor.Email.Should().BeNull();
    log.Actor.IpAddress.Should().BeNull();
    log.Raw.Should().BeNull();
    log.Resource.Should().BeNull();
    log.Zone.Should().BeNull();
  }

  /// <summary>U15: Verifies that AuditLogAccount model deserializes Id and Name correctly.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_AuditLogAccount_DeserializesIdAndName()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "log-1",
        "account": { "id": "acc-full", "name": "My Account" },
        "action": { "result": "success", "time": "2024-01-01T00:00:00Z", "type": "update" },
        "actor": { "id": "user-1" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    var account = result.Items[0].Account;
    account.Id.Should().Be("acc-full");
    account.Name.Should().Be("My Account");
  }

  /// <summary>U16: Verifies that AuditLogAction model deserializes Type, Result, and Time correctly.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_AuditLogAction_DeserializesTypeResultTime()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "log-2",
        "account": { "id": "acc-1" },
        "action": { "description": "Deleted record", "result": "failure", "time": "2024-03-15T10:45:30Z", "type": "delete" },
        "actor": { "id": "user-2" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    var action = result.Items[0].Action;
    action.Type.Should().Be("delete");
    action.Result.Should().Be("failure");
    action.Description.Should().Be("Deleted record");
    action.Time.Should().Be(DateTime.Parse("2024-03-15T10:45:30Z").ToUniversalTime());
  }

  /// <summary>U17: Verifies that AuditLogActor model deserializes all actor details correctly.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_AuditLogActor_DeserializesAllDetails()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "log-3",
        "account": { "id": "acc-1" },
        "action": { "result": "success", "time": "2024-01-01T00:00:00Z", "type": "create" },
        "actor": {
          "id": "actor-id-123",
          "context": "dashboard",
          "email": "user@example.org",
          "ip_address": "10.0.0.1",
          "token_id": "token-123",
          "token_name": "Dashboard Token",
          "type": "user"
        }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    var actor = result.Items[0].Actor;
    actor.Id.Should().Be("actor-id-123");
    actor.Context.Should().Be("dashboard");
    actor.Email.Should().Be("user@example.org");
    actor.IpAddress.Should().Be("10.0.0.1");
    actor.TokenId.Should().Be("token-123");
    actor.TokenName.Should().Be("Dashboard Token");
    actor.Type.Should().Be("user");
  }

  /// <summary>U18: Verifies that AuditLogRaw model deserializes Method, StatusCode, and Uri correctly.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_AuditLogRaw_DeserializesHttpDetails()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "log-4",
        "account": { "id": "acc-1" },
        "action": { "result": "success", "time": "2024-01-01T00:00:00Z", "type": "update" },
        "actor": { "id": "user-1" },
        "raw": {
          "cf_ray_id": "ray-xyz-123",
          "method": "PUT",
          "status_code": 201,
          "uri": "/client/v4/zones/abc/dns_records",
          "user_agent": "curl/7.88.0"
        }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    var raw = result.Items[0].Raw;
    raw.Should().NotBeNull();
    raw!.CfRayId.Should().Be("ray-xyz-123");
    raw.Method.Should().Be("PUT");
    raw.StatusCode.Should().Be(201);
    raw.Uri.Should().Be("/client/v4/zones/abc/dns_records");
    raw.UserAgent.Should().Be("curl/7.88.0");
  }

  /// <summary>U19: Verifies that AuditLogResource model deserializes Product, Type, and Scope correctly.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_AuditLogResource_DeserializesResourceDetails()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "log-5",
        "account": { "id": "acc-1" },
        "action": { "result": "success", "time": "2024-01-01T00:00:00Z", "type": "create" },
        "actor": { "id": "user-1" },
        "resource": {
          "id": "resource-id-456",
          "product": "dns",
          "scope": "zone",
          "type": "dns_record"
        }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    var resource = result.Items[0].Resource;
    resource.Should().NotBeNull();
    resource!.Id.Should().Be("resource-id-456");
    resource.Product.Should().Be("dns");
    resource.Scope.Should().Be("zone");
    resource.Type.Should().Be("dns_record");
  }

  /// <summary>U20: Verifies that AuditLogResource with request/response JsonElement payloads deserializes correctly.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_AuditLogResourceWithPayloads_DeserializesJsonElements()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "log-6",
        "account": { "id": "acc-1" },
        "action": { "result": "success", "time": "2024-01-01T00:00:00Z", "type": "update" },
        "actor": { "id": "user-1" },
        "resource": {
          "id": "res-with-payloads",
          "request": { "name": "test.example.com", "type": "A" },
          "response": { "id": "new-record-id", "success": true }
        }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    var resource = result.Items[0].Resource;
    resource.Should().NotBeNull();
    resource!.Request.Should().NotBeNull();
    resource.Request!.Value.GetProperty("name").GetString().Should().Be("test.example.com");
    resource.Response.Should().NotBeNull();
    resource.Response!.Value.GetProperty("success").GetBoolean().Should().BeTrue();
  }

  /// <summary>U21: Verifies that AuditLogZone model deserializes Id and Name correctly.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_AuditLogZone_DeserializesIdAndName()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "log-7",
        "account": { "id": "acc-1" },
        "action": { "result": "success", "time": "2024-01-01T00:00:00Z", "type": "create" },
        "actor": { "id": "user-1" },
        "zone": { "id": "zone-id-789", "name": "mysite.com" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    var zone = result.Items[0].Zone;
    zone.Should().NotBeNull();
    zone!.Id.Should().Be("zone-id-789");
    zone.Name.Should().Be("mysite.com");
  }

  #endregion


  #region Pagination Tests (U22-U24)

  /// <summary>U22: Verifies that GetAllAccountAuditLogsAsync with single page yields all items.</summary>
  [Fact]
  public async Task GetAllAccountAuditLogsAsync_SinglePage_YieldsAllItems()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "single-log",
        "account": { "id": "acc-1" },
        "action": { "result": "success", "time": "2024-01-01T00:00:00Z", "type": "create" },
        "actor": { "id": "user-1" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson, cursor: null);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var logs = new List<AuditLog>();
    await foreach (var log in sut.GetAllAccountAuditLogsAsync(TestAccountId))
    {
      logs.Add(log);
    }

    // Assert
    logs.Should().HaveCount(1);
    logs[0].Id.Should().Be("single-log");
  }

  /// <summary>U23: Verifies that GetAllAccountAuditLogsAsync with multiple pages makes multiple requests.</summary>
  [Fact]
  public async Task GetAllAccountAuditLogsAsync_MultiplePages_MakesMultipleRequests()
  {
    // Arrange
    var requestCount = 0;
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
      {
        requestCount++;
        var cursor = requestCount < 3 ? $"cursor-{requestCount}" : null;
        var logJson = $$"""
          {
            "id": "log-page-{{requestCount}}",
            "account": { "id": "acc-1" },
            "action": { "result": "success", "time": "2024-01-01T00:00:00Z", "type": "create" },
            "actor": { "id": "user-1" }
          }
          """;
        var responseJson = CreateAuditLogCursorResponse(logJson, cursor);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };
      });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var logs = new List<AuditLog>();
    await foreach (var log in sut.GetAllAccountAuditLogsAsync(TestAccountId))
    {
      logs.Add(log);
    }

    // Assert
    logs.Should().HaveCount(3);
    requestCount.Should().Be(3);
    logs[0].Id.Should().Be("log-page-1");
    logs[1].Id.Should().Be("log-page-2");
    logs[2].Id.Should().Be("log-page-3");
  }

  /// <summary>U24: Verifies that GetAllAccountAuditLogsAsync with empty result yields zero items.</summary>
  [Fact]
  public async Task GetAllAccountAuditLogsAsync_EmptyResult_YieldsZeroItems()
  {
    // Arrange
    var successResponse = CreateEmptyAuditLogResponse();
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var logs = new List<AuditLog>();
    await foreach (var log in sut.GetAllAccountAuditLogsAsync(TestAccountId))
    {
      logs.Add(log);
    }

    // Assert
    logs.Should().BeEmpty();
  }

  #endregion


  #region Error Handling Tests (U25-U31)

  /// <summary>U25: Verifies that GetAccountAuditLogsAsync throws HttpRequestException with status 404 when account not found.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_AccountNotFound404_ThrowsHttpRequestException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(7003, "Account not found");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.NotFound);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetAccountAuditLogsAsync("nonexistent-account");

    // Assert
    var exception = await act.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  /// <summary>U26: Verifies that GetAccountAuditLogsAsync throws CloudflareApiException when API returns success=false.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_ApiError_ThrowsCloudflareApiException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(1001, "Invalid API token");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    var exception = await act.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().HaveCount(1);
    exception.Which.Errors[0].Code.Should().Be(1001);
    exception.Which.Errors[0].Message.Should().Be("Invalid API token");
  }

  /// <summary>U27: Verifies that CloudflareApiException contains all errors when API returns multiple errors.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_MultipleErrors_ThrowsCloudflareApiExceptionWithAllErrors()
  {
    // Arrange
    var errorResponse = """
      {
        "success": false,
        "errors": [
          { "code": 1001, "message": "First error" },
          { "code": 1002, "message": "Second error" },
          { "code": 1003, "message": "Third error" }
        ],
        "messages": [],
        "result": null
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    var exception = await act.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().HaveCount(3);
    exception.Which.Errors[0].Code.Should().Be(1001);
    exception.Which.Errors[1].Code.Should().Be(1002);
    exception.Which.Errors[2].Code.Should().Be(1003);
  }

  /// <summary>U28: Verifies that GetAccountAuditLogsAsync throws HttpRequestException with status 401 when unauthorized.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_Unauthorized401_ThrowsHttpRequestException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(10000, "Authentication error");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.Unauthorized);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    var exception = await act.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>U29: Verifies that GetAccountAuditLogsAsync throws HttpRequestException with status 403 when forbidden.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_Forbidden403_ThrowsHttpRequestException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(10001, "Forbidden");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.Forbidden);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    var exception = await act.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }

  /// <summary>U30: Verifies that GetAccountAuditLogsAsync throws HttpRequestException with status 429 when rate limited.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_RateLimited429_ThrowsHttpRequestException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(10002, "Rate limited");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.TooManyRequests);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    var exception = await act.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
  }

  /// <summary>U31: Verifies that GetAccountAuditLogsAsync throws HttpRequestException with 5xx status on server errors.</summary>
  [Theory]
  [InlineData(HttpStatusCode.InternalServerError)]
  [InlineData(HttpStatusCode.BadGateway)]
  [InlineData(HttpStatusCode.ServiceUnavailable)]
  public async Task GetAccountAuditLogsAsync_ServerError5xx_ThrowsHttpRequestException(HttpStatusCode statusCode)
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(10003, "Server error");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, statusCode);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    var exception = await act.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().Be(statusCode);
  }

  #endregion


  #region URL Encoding and DateTime Tests (U32-U33)

  /// <summary>U32: Verifies that AccountId with special characters is properly URL-encoded.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_AccountIdWithSpecialChars_UrlEncodesAccountId()
  {
    // Arrange
    var accountId = "acc/with spaces+special";
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountAuditLogsAsync(accountId);

    // Assert
    capturedRequest.Should().NotBeNull();
    // Use AbsoluteUri to get the URI with proper encoding preserved
    var uri = capturedRequest!.RequestUri!.AbsoluteUri;
    // Should be URL encoded (/ becomes %2F, space becomes %20, + becomes %2B)
    uri.Should().NotContain("acc/with spaces+special");
    uri.Should().Contain("accounts/");
    uri.Should().Contain("%2F"); // encoded /
    uri.Should().Contain("%20"); // encoded space
    uri.Should().Contain("%2B"); // encoded +
  }

  /// <summary>U33: Verifies that DateTime in action.time is parsed correctly using ISO 8601 format.</summary>
  [Fact]
  public async Task GetAccountAuditLogsAsync_ActionTime_ParsesIso8601DateTime()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "log-datetime",
        "account": { "id": "acc-1" },
        "action": { "result": "success", "time": "2024-06-15T14:30:45.123Z", "type": "create" },
        "actor": { "id": "user-1" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountAuditLogsAsync(TestAccountId);

    // Assert
    var action = result.Items[0].Action;
    action.Time.Year.Should().Be(2024);
    action.Time.Month.Should().Be(6);
    action.Time.Day.Should().Be(15);
    action.Time.Hour.Should().Be(14);
    action.Time.Minute.Should().Be(30);
    action.Time.Second.Should().Be(45);
  }

  #endregion


  #region F15 User Audit Logs - Request Construction Tests (U01-U09)

  /// <summary>U01: Verifies that ListUserAuditLogsAsync with no filters sends a GET request to /user/audit_logs.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_NoFilters_SendsCorrectGetRequest()
  {
    // Arrange
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.ListUserAuditLogsAsync();

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be("https://api.cloudflare.com/client/v4/user/audit_logs");
  }

  /// <summary>U02: Verifies that ListUserAuditLogsAsync with Since and Before includes date range in query string.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_WithDateRange_IncludesDateRangeInQueryString()
  {
    // Arrange
    var since = new DateTime(2024, 6, 10, 0, 0, 0, DateTimeKind.Utc);
    var before = new DateTime(2024, 6, 20, 23, 59, 59, DateTimeKind.Utc);
    var filters = new ListAuditLogsFilters(Since: since, Before: before);
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.ListUserAuditLogsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    var query = capturedRequest!.RequestUri!.Query;
    query.Should().Contain("since=");
    query.Should().Contain("2024-06-10");
    query.Should().Contain("before=");
    query.Should().Contain("2024-06-20");
  }

  /// <summary>U03: Verifies that ListUserAuditLogsAsync with ActorEmail includes actor_email in query string.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_WithActorEmail_IncludesActorEmailInQueryString()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(ActorEmails: new[] { "user@example.com" });
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.ListUserAuditLogsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("actor_email=user%40example.com");
  }

  /// <summary>U04: Verifies that ListUserAuditLogsAsync with ActorIp includes actor_ip_address in query string.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_WithActorIp_IncludesActorIpInQueryString()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(ActorIpAddresses: new[] { "192.168.1.1" });
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.ListUserAuditLogsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("actor_ip_address=192.168.1.1");
  }

  /// <summary>U05: Verifies that ListUserAuditLogsAsync with Direction includes direction in query string.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_WithDirection_IncludesDirectionInQueryString()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(Direction: ListOrderDirection.Descending);
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.ListUserAuditLogsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("direction=desc");
  }

  /// <summary>U06: Verifies that ListUserAuditLogsAsync with PerPage includes limit in query string.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_WithPerPage_IncludesLimitInQueryString()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(Limit: 50);
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.ListUserAuditLogsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("limit=50");
  }

  /// <summary>U07: Verifies that ListUserAuditLogsAsync with Cursor includes cursor in query string.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_WithCursor_IncludesCursorInQueryString()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(Cursor: "next-page-cursor-xyz");
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.ListUserAuditLogsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("cursor=next-page-cursor-xyz");
  }

  /// <summary>U08: Verifies that ListAllUserAuditLogsAsync iterates through cursor-based pagination.</summary>
  [Fact]
  public async Task ListAllUserAuditLogsAsync_AutoPagination_IteratesThroughCursorPages()
  {
    // Arrange
    var requestCount = 0;
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
      {
        requestCount++;
        var cursor = requestCount < 2 ? $"cursor-{requestCount}" : null;
        var logJson = $$"""
          {
            "id": "user-log-page-{{requestCount}}",
            "account": { "id": "acc-1" },
            "action": { "result": "success", "time": "2024-01-01T00:00:00Z", "type": "create" },
            "actor": { "id": "user-1" }
          }
          """;
        var responseJson = CreateAuditLogCursorResponse(logJson, cursor);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };
      });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var logs = new List<AuditLog>();
    await foreach (var log in sut.ListAllUserAuditLogsAsync())
    {
      logs.Add(log);
    }

    // Assert
    logs.Should().HaveCount(2);
    requestCount.Should().Be(2);
    logs[0].Id.Should().Be("user-log-page-1");
    logs[1].Id.Should().Be("user-log-page-2");
  }

  /// <summary>U09: Verifies that ListUserAuditLogsAsync with combined filters includes all in query string.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_CombinedFilters_IncludesAllFiltersInQueryString()
  {
    // Arrange
    var since = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    var filters = new ListAuditLogsFilters(
      Since: since,
      Limit: 25,
      ActionTypes: new[] { "create", "update" },
      Direction: ListOrderDirection.Ascending
    );
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.ListUserAuditLogsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    var query = capturedRequest!.RequestUri!.Query;
    query.Should().Contain("since=");
    query.Should().Contain("limit=25");
    query.Should().Contain("action_type=create");
    query.Should().Contain("action_type=update");
    query.Should().Contain("direction=asc");
  }

  #endregion


  #region F15 User Audit Logs - Response Deserialization Tests (U10-U18)

  /// <summary>U10: Verifies that ListUserAuditLogsAsync deserializes full AuditLog model correctly.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_FullModel_DeserializesAllProperties()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "user-audit-log-123",
        "account": { "id": "acc-123", "name": "Test Account" },
        "action": { "description": "Created zone", "result": "success", "time": "2024-06-15T12:30:00Z", "type": "create" },
        "actor": { "id": "user-456", "context": "api", "email": "admin@example.com", "ip_address": "192.168.1.1", "type": "user" },
        "resource": { "id": "res-123", "product": "zones", "type": "zone" },
        "zone": { "id": "zone-xyz", "name": "example.com" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserAuditLogsAsync();

    // Assert
    result.Items.Should().HaveCount(1);
    var log = result.Items[0];
    log.Id.Should().Be("user-audit-log-123");
    log.Account.Id.Should().Be("acc-123");
    log.Action.Type.Should().Be("create");
    log.Actor.Email.Should().Be("admin@example.com");
    log.Resource.Should().NotBeNull();
    log.Zone.Should().NotBeNull();
  }

  /// <summary>U11: Verifies that ListUserAuditLogsAsync with minimal response has null optional properties.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_MinimalModel_HasNullOptionalProperties()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "minimal-log",
        "account": { "id": "acc-min" },
        "action": { "result": "success", "time": "2024-06-15T12:30:00Z", "type": "delete" },
        "actor": { "id": "system" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserAuditLogsAsync();

    // Assert
    var log = result.Items[0];
    log.Account.Name.Should().BeNull();
    log.Action.Description.Should().BeNull();
    log.Actor.Email.Should().BeNull();
    log.Raw.Should().BeNull();
    log.Resource.Should().BeNull();
    log.Zone.Should().BeNull();
  }

  /// <summary>U12: Verifies that AuditLogAction nested object deserializes with type and result.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_AuditLogAction_DeserializesTypeAndResult()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "log-action",
        "account": { "id": "acc-1" },
        "action": { "description": "Updated DNS record", "result": "failure", "time": "2024-06-15T10:00:00Z", "type": "update" },
        "actor": { "id": "user-1" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserAuditLogsAsync();

    // Assert
    var action = result.Items[0].Action;
    action.Type.Should().Be("update");
    action.Result.Should().Be("failure");
    action.Description.Should().Be("Updated DNS record");
  }

  /// <summary>U13: Verifies that AuditLogActor nested object deserializes with id, email, and IP.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_AuditLogActor_DeserializesIdEmailIp()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "log-actor",
        "account": { "id": "acc-1" },
        "action": { "result": "success", "time": "2024-06-15T10:00:00Z", "type": "create" },
        "actor": { "id": "actor-xyz", "email": "test@example.org", "ip_address": "10.0.0.1" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserAuditLogsAsync();

    // Assert
    var actor = result.Items[0].Actor;
    actor.Id.Should().Be("actor-xyz");
    actor.Email.Should().Be("test@example.org");
    actor.IpAddress.Should().Be("10.0.0.1");
  }

  /// <summary>U14: Verifies that AuditLogResource nested object deserializes type and ID.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_AuditLogResource_DeserializesTypeAndId()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "log-resource",
        "account": { "id": "acc-1" },
        "action": { "result": "success", "time": "2024-06-15T10:00:00Z", "type": "create" },
        "actor": { "id": "user-1" },
        "resource": { "id": "resource-id", "type": "dns_record", "product": "dns", "scope": "zone" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserAuditLogsAsync();

    // Assert
    var resource = result.Items[0].Resource;
    resource.Should().NotBeNull();
    resource!.Id.Should().Be("resource-id");
    resource.Type.Should().Be("dns_record");
  }

  /// <summary>U15: Verifies that AuditLogAccount nested object deserializes correctly.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_AuditLogAccount_DeserializesCorrectly()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "log-account",
        "account": { "id": "acc-full", "name": "My Account Name" },
        "action": { "result": "success", "time": "2024-06-15T10:00:00Z", "type": "update" },
        "actor": { "id": "user-1" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserAuditLogsAsync();

    // Assert
    var account = result.Items[0].Account;
    account.Id.Should().Be("acc-full");
    account.Name.Should().Be("My Account Name");
  }

  /// <summary>U16: Verifies that AuditLogZone nested object deserializes correctly.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_AuditLogZone_DeserializesCorrectly()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "log-zone",
        "account": { "id": "acc-1" },
        "action": { "result": "success", "time": "2024-06-15T10:00:00Z", "type": "create" },
        "actor": { "id": "user-1" },
        "zone": { "id": "zone-id-abc", "name": "mysite.io" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserAuditLogsAsync();

    // Assert
    var zone = result.Items[0].Zone;
    zone.Should().NotBeNull();
    zone!.Id.Should().Be("zone-id-abc");
    zone.Name.Should().Be("mysite.io");
  }

  /// <summary>U17: Verifies that ListUserAuditLogsAsync with empty result returns empty list.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_EmptyResult_ReturnsEmptyList()
  {
    // Arrange
    var successResponse = CreateEmptyAuditLogResponse();
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserAuditLogsAsync();

    // Assert
    result.Items.Should().BeEmpty();
  }

  /// <summary>U18: Verifies that cursor pagination info deserializes correctly.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_CursorPaginationInfo_DeserializesCorrectly()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "log-1",
        "account": { "id": "acc-1" },
        "action": { "result": "success", "time": "2024-06-15T10:00:00Z", "type": "create" },
        "actor": { "id": "user-1" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson, cursor: "next-cursor-123");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserAuditLogsAsync();

    // Assert
    result.CursorInfo.Should().NotBeNull();
    result.CursorInfo!.Cursor.Should().Be("next-cursor-123");
  }

  #endregion


  #region F15 User Audit Logs - Pagination Tests (U19-U22)

  /// <summary>U19: Verifies that cursor is present when more pages are available.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_MorePagesAvailable_CursorPresent()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "log-1",
        "account": { "id": "acc-1" },
        "action": { "result": "success", "time": "2024-06-15T10:00:00Z", "type": "create" },
        "actor": { "id": "user-1" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson, cursor: "has-more-cursor");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserAuditLogsAsync();

    // Assert
    result.CursorInfo!.Cursor.Should().NotBeNull();
    result.CursorInfo.Cursor.Should().Be("has-more-cursor");
  }

  /// <summary>U20: Verifies that cursor is null on last page.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_LastPage_CursorNull()
  {
    // Arrange
    var auditLogJson = """
      {
        "id": "log-last",
        "account": { "id": "acc-1" },
        "action": { "result": "success", "time": "2024-06-15T10:00:00Z", "type": "create" },
        "actor": { "id": "user-1" }
      }
      """;
    var successResponse = CreateAuditLogCursorResponse(auditLogJson, cursor: null);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserAuditLogsAsync();

    // Assert
    result.CursorInfo!.Cursor.Should().BeNull();
  }

  /// <summary>U21: Verifies that ListAllUserAuditLogsAsync combines all pages into single enumerable.</summary>
  [Fact]
  public async Task ListAllUserAuditLogsAsync_ThreePages_CombinesAllItems()
  {
    // Arrange
    var requestCount = 0;
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
      {
        requestCount++;
        var cursor = requestCount < 3 ? $"cursor-{requestCount}" : null;
        var logJson = $$"""
          {
            "id": "combined-log-{{requestCount}}",
            "account": { "id": "acc-1" },
            "action": { "result": "success", "time": "2024-01-01T00:00:00Z", "type": "create" },
            "actor": { "id": "user-1" }
          }
          """;
        var responseJson = CreateAuditLogCursorResponse(logJson, cursor);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };
      });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var logs = new List<AuditLog>();
    await foreach (var log in sut.ListAllUserAuditLogsAsync())
    {
      logs.Add(log);
    }

    // Assert
    logs.Should().HaveCount(3);
    logs[0].Id.Should().Be("combined-log-1");
    logs[1].Id.Should().Be("combined-log-2");
    logs[2].Id.Should().Be("combined-log-3");
  }

  /// <summary>U22: Verifies that ListAllUserAuditLogsAsync with filters applies filters to all pages.</summary>
  [Fact]
  public async Task ListAllUserAuditLogsAsync_WithFilters_AppliesFiltersToAllPages()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(ActionTypes: new[] { "create" }, Limit: 10);
    var capturedRequests = new List<HttpRequestMessage>();
    var requestCount = 0;
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
      {
        capturedRequests.Add(req);
        requestCount++;
        var cursor = requestCount < 2 ? "cursor-next" : null;
        var logJson = $$"""
          {
            "id": "filtered-log-{{requestCount}}",
            "account": { "id": "acc-1" },
            "action": { "result": "success", "time": "2024-01-01T00:00:00Z", "type": "create" },
            "actor": { "id": "user-1" }
          }
          """;
        var responseJson = CreateAuditLogCursorResponse(logJson, cursor);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };
      });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var logs = new List<AuditLog>();
    await foreach (var log in sut.ListAllUserAuditLogsAsync(filters))
    {
      logs.Add(log);
    }

    // Assert
    capturedRequests.Should().HaveCount(2);
    foreach (var req in capturedRequests)
    {
      req.RequestUri!.Query.Should().Contain("action_type=create");
      req.RequestUri.Query.Should().Contain("limit=10");
    }
  }

  #endregion


  #region F15 User Audit Logs - Error Handling Tests (U23-U31)

  /// <summary>U23: Verifies that ListUserAuditLogsAsync throws HttpRequestException with 401 when unauthorized.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_Unauthorized401_ThrowsHttpRequestException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(10000, "Authentication error");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.Unauthorized);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.ListUserAuditLogsAsync();

    // Assert
    var exception = await act.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>U24: Verifies that ListUserAuditLogsAsync throws CloudflareApiException when API returns success=false.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_ApiError_ThrowsCloudflareApiException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(1001, "Invalid request");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.ListUserAuditLogsAsync();

    // Assert
    var exception = await act.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().HaveCount(1);
    exception.Which.Errors[0].Code.Should().Be(1001);
  }

  /// <summary>U25: Verifies that invalid date format filter is handled (tested indirectly via query building).</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_ValidDateFormat_UsesIso8601()
  {
    // Arrange
    var since = new DateTime(2024, 12, 1, 14, 30, 45, DateTimeKind.Utc);
    var filters = new ListAuditLogsFilters(Since: since);
    var successResponse = CreateEmptyAuditLogResponse();
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    await sut.ListUserAuditLogsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    // Should use ISO 8601 format with URL encoding
    capturedRequest!.RequestUri!.Query.Should().Contain("since=");
    capturedRequest.RequestUri.Query.Should().Contain("2024-12-01");
  }

  /// <summary>U26: Verifies that CloudflareApiException contains all errors from response.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_MultipleErrors_ThrowsCloudflareApiExceptionWithAll()
  {
    // Arrange
    var errorResponse = """
      {
        "success": false,
        "errors": [
          { "code": 1001, "message": "First error" },
          { "code": 1002, "message": "Second error" }
        ],
        "messages": [],
        "result": null
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.ListUserAuditLogsAsync();

    // Assert
    var exception = await act.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().HaveCount(2);
  }

  /// <summary>U27: Verifies that ListUserAuditLogsAsync throws HttpRequestException with 403 when forbidden.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_Forbidden403_ThrowsHttpRequestException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(10001, "Forbidden");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.Forbidden);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.ListUserAuditLogsAsync();

    // Assert
    var exception = await act.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }

  /// <summary>U28: Verifies that ListUserAuditLogsAsync throws HttpRequestException with 429 when rate limited.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_RateLimited429_ThrowsHttpRequestException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(10002, "Rate limited");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.TooManyRequests);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.ListUserAuditLogsAsync();

    // Assert
    var exception = await act.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
  }

  /// <summary>U29: Verifies that ListUserAuditLogsAsync throws HttpRequestException on HTTP 500.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_ServerError500_ThrowsHttpRequestException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(10003, "Server error");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.InternalServerError);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.ListUserAuditLogsAsync();

    // Assert
    var exception = await act.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
  }

  /// <summary>U30: Verifies that ListUserAuditLogsAsync throws HttpRequestException on HTTP 502.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_BadGateway502_ThrowsHttpRequestException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(10004, "Bad gateway");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.BadGateway);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.ListUserAuditLogsAsync();

    // Assert
    var exception = await act.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().Be(HttpStatusCode.BadGateway);
  }

  /// <summary>U31: Verifies that ListUserAuditLogsAsync throws HttpRequestException on HTTP 503.</summary>
  [Fact]
  public async Task ListUserAuditLogsAsync_ServiceUnavailable503_ThrowsHttpRequestException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(10005, "Service unavailable");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.ServiceUnavailable);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new AuditLogsApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.ListUserAuditLogsAsync();

    // Assert
    var exception = await act.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
  }

  #endregion


  #region Helper Methods

  /// <summary>Creates an empty audit log cursor paginated response JSON string.</summary>
  private static string CreateEmptyAuditLogResponse()
  {
    return """
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": [],
        "cursor_result_info": {
          "cursor": null,
          "count": 0,
          "per_page": 100
        }
      }
      """;
  }

  /// <summary>Creates a cursor paginated response JSON string with a given audit log result JSON.</summary>
  private static string CreateAuditLogCursorResponse(string auditLogJson, string? cursor = null)
  {
    var cursorValue = cursor == null ? "null" : $"\"{cursor}\"";
    return $$"""
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": [{{auditLogJson}}],
        "cursor_result_info": {
          "cursor": {{cursorValue}},
          "count": 1,
          "per_page": 100
        }
      }
      """;
  }

  #endregion
}
