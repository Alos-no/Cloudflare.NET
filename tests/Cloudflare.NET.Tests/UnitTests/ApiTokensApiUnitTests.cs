namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using ApiTokens;
using ApiTokens.Models;
using Cloudflare.NET.Core.Exceptions;
using Cloudflare.NET.Security.Firewall.Models;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>Contains unit tests for the <see cref="ApiTokensApi" /> class.</summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class ApiTokensApiUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly ILoggerFactory _loggerFactory;

  #endregion


  #region Constructors

  public ApiTokensApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion


  #region Request Construction Tests (U01-U12)

  /// <summary>U01: Verifies that ListAccountTokensAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task ListAccountTokensAsync_NoFilters_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var successResponse = CreatePagePaginatedResponse(Array.Empty<ApiToken>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAccountTokensAsync(accountId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/tokens");
    capturedRequest.RequestUri.Query.Should().BeEmpty();
  }

  /// <summary>U02: Verifies that ListAccountTokensAsync includes pagination parameters.</summary>
  [Fact]
  public async Task ListAccountTokensAsync_WithPagination_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var filters = new ListApiTokensFilters(Page: 2, PerPage: 10);
    var successResponse = CreatePagePaginatedResponse(Array.Empty<ApiToken>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAccountTokensAsync(accountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("page=2");
    capturedRequest.RequestUri.Query.Should().Contain("per_page=10");
  }

  /// <summary>U03: Verifies that ListAccountTokensAsync includes direction parameter.</summary>
  [Fact]
  public async Task ListAccountTokensAsync_WithDirection_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var filters = new ListApiTokensFilters(Direction: ListOrderDirection.Descending);
    var successResponse = CreatePagePaginatedResponse(Array.Empty<ApiToken>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAccountTokensAsync(accountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("direction=desc");
  }

  /// <summary>U04: Verifies that GetAccountTokenAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task GetAccountTokenAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var tokenId = "token-123";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestToken(tokenId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountTokenAsync(accountId, tokenId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/tokens/{tokenId}");
  }

  /// <summary>U05: Verifies that CreateAccountTokenAsync sends a POST request with correct body.</summary>
  [Fact]
  public async Task CreateAccountTokenAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var request = new CreateApiTokenRequest(
      "Test Token",
      new[]
      {
        new CreateTokenPolicyRequest(
          "allow",
          new[] { new TokenPermissionGroupReference("perm-group-123") },
          new Dictionary<string, string> { ["com.cloudflare.api.account.*"] = "*" }
        )
      },
      ExpiresOn: DateTime.UtcNow.AddDays(30)
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestTokenResult());
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateAccountTokenAsync(accountId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/tokens");
    capturedBody.Should().NotBeNull();
    capturedBody.Should().Contain("\"name\"");
    capturedBody.Should().Contain("Test Token");
    capturedBody.Should().Contain("\"policies\"");
    capturedBody.Should().Contain("\"allow\"");
  }

  /// <summary>U06: Verifies that CreateAccountTokenAsync omits null optional fields.</summary>
  [Fact]
  public async Task CreateAccountTokenAsync_OmitsNullFields()
  {
    // Arrange
    var accountId = "test-account-id";
    var request = new CreateApiTokenRequest(
      "Minimal Token",
      new[]
      {
        new CreateTokenPolicyRequest(
          "allow",
          new[] { new TokenPermissionGroupReference("perm-group-123") },
          new Dictionary<string, string> { ["com.cloudflare.api.account.*"] = "*" }
        )
      }
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestTokenResult());
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateAccountTokenAsync(accountId, request);

    // Assert
    capturedBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedBody!);
    doc.RootElement.TryGetProperty("expires_on", out _).Should().BeFalse("expires_on should be omitted when null");
    doc.RootElement.TryGetProperty("not_before", out _).Should().BeFalse("not_before should be omitted when null");
    doc.RootElement.TryGetProperty("condition", out _).Should().BeFalse("condition should be omitted when null");
  }

  /// <summary>U07: Verifies that UpdateAccountTokenAsync sends a PUT request with correct body.</summary>
  [Fact]
  public async Task UpdateAccountTokenAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var tokenId = "token-123";
    var request = new UpdateApiTokenRequest(
      "Updated Token",
      new[]
      {
        new CreateTokenPolicyRequest(
          "allow",
          new[] { new TokenPermissionGroupReference("perm-group-456") },
          new Dictionary<string, string> { ["com.cloudflare.api.account.*"] = "*" }
        )
      },
      Status: TokenStatus.Disabled
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestToken(tokenId));
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateAccountTokenAsync(accountId, tokenId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/tokens/{tokenId}");
    capturedBody.Should().NotBeNull();
    capturedBody.Should().Contain("Updated Token");
    capturedBody.Should().Contain("\"status\"");
    capturedBody.Should().Contain("\"disabled\"");
  }

  /// <summary>U08: Verifies that DeleteAccountTokenAsync sends a DELETE request to the correct endpoint.</summary>
  [Fact]
  public async Task DeleteAccountTokenAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var tokenId = "token-to-delete";
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.DeleteAccountTokenAsync(accountId, tokenId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/tokens/{tokenId}");
  }

  /// <summary>U09: Verifies that VerifyAccountTokenAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task VerifyAccountTokenAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var successResponse = HttpFixtures.CreateSuccessResponse(new VerifyTokenResult("token-123", TokenStatus.Active));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.VerifyAccountTokenAsync(accountId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/tokens/verify");
  }

  /// <summary>U10: Verifies that RollAccountTokenAsync sends a PUT request to the correct endpoint.</summary>
  [Fact]
  public async Task RollAccountTokenAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var tokenId = "token-to-roll";
    var newTokenValue = "new-secret-token-value-xyz";
    var successResponse = HttpFixtures.CreateSuccessResponse(newTokenValue);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.RollAccountTokenAsync(accountId, tokenId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/tokens/{tokenId}/value");
    result.Should().Be(newTokenValue);
  }

  /// <summary>U11: Verifies that GetAccountPermissionGroupsAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task GetAccountPermissionGroupsAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var successResponse = CreatePagePaginatedResponse(new[]
    {
      new PermissionGroup("pg-1", "Zone Read", new[] { "zone:read" })
    });
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountPermissionGroupsAsync(accountId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/tokens/permission_groups");
  }

  /// <summary>U12: Verifies that GetAccountPermissionGroupsAsync includes filter parameters.</summary>
  [Fact]
  public async Task GetAccountPermissionGroupsAsync_WithFilters_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var filters = new ListPermissionGroupsFilters(Name: "Zone", Scope: "zone:read", Page: 2, PerPage: 10);
    var successResponse = CreatePagePaginatedResponse(Array.Empty<PermissionGroup>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountPermissionGroupsAsync(accountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("name=Zone");
    capturedRequest.RequestUri.Query.Should().Contain("scope=zone%3Aread");
    capturedRequest.RequestUri.Query.Should().Contain("page=2");
    capturedRequest.RequestUri.Query.Should().Contain("per_page=10");
  }

  #endregion


  #region Response Deserialization Tests (U13-U22)

  /// <summary>U13: Verifies that ApiToken model deserializes all properties correctly.</summary>
  [Fact]
  public async Task GetAccountTokenAsync_DeserializesFullModel()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""token-abc123"",
        ""name"": ""Test Token"",
        ""status"": ""active"",
        ""issued_on"": ""2024-01-15T10:30:00Z"",
        ""modified_on"": ""2024-01-20T14:00:00Z"",
        ""last_used_on"": ""2024-01-25T09:15:00Z"",
        ""expires_on"": ""2025-01-15T10:30:00Z"",
        ""not_before"": ""2024-01-10T00:00:00Z"",
        ""policies"": [
          {
            ""id"": ""policy-123"",
            ""effect"": ""allow"",
            ""permission_groups"": [{ ""id"": ""pg-001"" }],
            ""resources"": { ""com.cloudflare.api.account.*"": ""*"" }
          }
        ],
        ""condition"": {
          ""request.ip"": {
            ""in"": [""192.168.1.0/24""],
            ""not_in"": [""192.168.1.100/32""]
          }
        }
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountTokenAsync("acc123", "token-abc123");

    // Assert
    result.Id.Should().Be("token-abc123");
    result.Name.Should().Be("Test Token");
    result.Status.Should().Be(TokenStatus.Active);
    result.IssuedOn.Should().Be(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
    result.ModifiedOn.Should().Be(new DateTime(2024, 1, 20, 14, 0, 0, DateTimeKind.Utc));
    result.LastUsedOn.Should().Be(new DateTime(2024, 1, 25, 9, 15, 0, DateTimeKind.Utc));
    result.ExpiresOn.Should().Be(new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc));
    result.NotBefore.Should().Be(new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc));
    result.Policies.Should().HaveCount(1);
    result.Policies[0].Id.Should().Be("policy-123");
    result.Policies[0].Effect.Should().Be("allow");
    result.Condition.Should().NotBeNull();
    result.Condition!.RequestIp.Should().NotBeNull();
    result.Condition.RequestIp!.In.Should().Contain("192.168.1.0/24");
    result.Condition.RequestIp.NotIn.Should().Contain("192.168.1.100/32");
  }

  /// <summary>U14: Verifies that ApiToken model handles null optional fields.</summary>
  [Fact]
  public async Task GetAccountTokenAsync_DeserializesOptionalNullFields()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""token-123"",
        ""name"": ""Minimal Token"",
        ""status"": ""active"",
        ""issued_on"": ""2024-01-15T10:30:00Z"",
        ""modified_on"": ""2024-01-15T10:30:00Z"",
        ""policies"": []
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountTokenAsync("acc123", "token-123");

    // Assert
    result.ExpiresOn.Should().BeNull();
    result.NotBefore.Should().BeNull();
    result.LastUsedOn.Should().BeNull();
    result.Condition.Should().BeNull();
  }

  /// <summary>U15: Verifies that TokenStatus.Active deserializes correctly.</summary>
  [Fact]
  public async Task GetAccountTokenAsync_DeserializesActiveStatus()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true, ""errors"": [], ""messages"": [],
      ""result"": {
        ""id"": ""t1"", ""name"": ""Token"", ""status"": ""active"",
        ""issued_on"": ""2024-01-01T00:00:00Z"", ""modified_on"": ""2024-01-01T00:00:00Z"", ""policies"": []
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountTokenAsync("acc", "t1");

    // Assert
    result.Status.Should().Be(TokenStatus.Active);
    result.Status.Value.Should().Be("active");
  }

  /// <summary>U16: Verifies that TokenStatus.Disabled deserializes correctly.</summary>
  [Fact]
  public async Task GetAccountTokenAsync_DeserializesDisabledStatus()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true, ""errors"": [], ""messages"": [],
      ""result"": {
        ""id"": ""t1"", ""name"": ""Token"", ""status"": ""disabled"",
        ""issued_on"": ""2024-01-01T00:00:00Z"", ""modified_on"": ""2024-01-01T00:00:00Z"", ""policies"": []
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountTokenAsync("acc", "t1");

    // Assert
    result.Status.Should().Be(TokenStatus.Disabled);
    result.Status.Value.Should().Be("disabled");
  }

  /// <summary>U17: Verifies that TokenStatus.Expired deserializes correctly.</summary>
  [Fact]
  public async Task GetAccountTokenAsync_DeserializesExpiredStatus()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true, ""errors"": [], ""messages"": [],
      ""result"": {
        ""id"": ""t1"", ""name"": ""Token"", ""status"": ""expired"",
        ""issued_on"": ""2024-01-01T00:00:00Z"", ""modified_on"": ""2024-01-01T00:00:00Z"", ""policies"": []
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountTokenAsync("acc", "t1");

    // Assert
    result.Status.Should().Be(TokenStatus.Expired);
    result.Status.Value.Should().Be("expired");
  }

  /// <summary>U18: Verifies that TokenStatus extensible enum preserves unknown values.</summary>
  [Fact]
  public async Task GetAccountTokenAsync_DeserializesUnknownStatus()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true, ""errors"": [], ""messages"": [],
      ""result"": {
        ""id"": ""t1"", ""name"": ""Token"", ""status"": ""pending_approval"",
        ""issued_on"": ""2024-01-01T00:00:00Z"", ""modified_on"": ""2024-01-01T00:00:00Z"", ""policies"": []
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountTokenAsync("acc", "t1");

    // Assert
    result.Status.Value.Should().Be("pending_approval");
    result.Status.Should().NotBe(TokenStatus.Active);
    result.Status.Should().NotBe(TokenStatus.Disabled);
    result.Status.Should().NotBe(TokenStatus.Expired);
  }

  /// <summary>U19: Verifies that CreateApiTokenResult deserializes correctly with token value.</summary>
  [Fact]
  public async Task CreateAccountTokenAsync_DeserializesResultWithValue()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""new-token-123"",
        ""name"": ""New Token"",
        ""value"": ""secret-token-value-xyz"",
        ""status"": ""active"",
        ""issued_on"": ""2024-01-15T10:30:00Z"",
        ""modified_on"": ""2024-01-15T10:30:00Z"",
        ""policies"": [
          {
            ""id"": ""pol-1"",
            ""effect"": ""allow"",
            ""permission_groups"": [{ ""id"": ""pg-1"" }],
            ""resources"": { ""com.cloudflare.api.account.*"": ""*"" }
          }
        ]
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var request = new CreateApiTokenRequest("New Token", new[] {
      new CreateTokenPolicyRequest("allow", new[] { new TokenPermissionGroupReference("pg-1") },
        new Dictionary<string, string> { ["com.cloudflare.api.account.*"] = "*" })
    });
    var result = await sut.CreateAccountTokenAsync("acc", request);

    // Assert
    result.Id.Should().Be("new-token-123");
    result.Name.Should().Be("New Token");
    result.Value.Should().Be("secret-token-value-xyz");
    result.Status.Should().Be(TokenStatus.Active);
  }

  /// <summary>U20: Verifies that VerifyTokenResult deserializes correctly.</summary>
  [Fact]
  public async Task VerifyAccountTokenAsync_DeserializesResult()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""verified-token-123"",
        ""status"": ""active"",
        ""expires_on"": ""2025-06-15T00:00:00Z"",
        ""not_before"": ""2024-01-01T00:00:00Z""
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.VerifyAccountTokenAsync("acc");

    // Assert
    result.Id.Should().Be("verified-token-123");
    result.Status.Should().Be(TokenStatus.Active);
    result.ExpiresOn.Should().Be(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc));
    result.NotBefore.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
  }

  /// <summary>U21: Verifies that PermissionGroup model deserializes correctly.</summary>
  [Fact]
  public async Task GetAccountPermissionGroupsAsync_DeserializesResult()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": [
        {
          ""id"": ""pg-001"",
          ""name"": ""Zone Read"",
          ""scopes"": [""zone:read"", ""dns_records:read""]
        },
        {
          ""id"": ""pg-002"",
          ""name"": ""Zone Write"",
          ""scopes"": [""zone:write"", ""dns_records:write""]
        }
      ],
      ""result_info"": {
        ""page"": 1, ""per_page"": 20, ""count"": 2, ""total_pages"": 1, ""total_count"": 2
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountPermissionGroupsAsync("acc");

    // Assert
    result.Items.Should().HaveCount(2);
    result.Items[0].Id.Should().Be("pg-001");
    result.Items[0].Name.Should().Be("Zone Read");
    result.Items[0].Scopes.Should().Contain(new[] { "zone:read", "dns_records:read" });
    result.Items[1].Id.Should().Be("pg-002");
    result.Items[1].Name.Should().Be("Zone Write");
  }

  /// <summary>U22: Verifies that TokenPolicy with permission group meta deserializes correctly.</summary>
  [Fact]
  public async Task GetAccountTokenAsync_DeserializesPolicyWithMeta()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true, ""errors"": [], ""messages"": [],
      ""result"": {
        ""id"": ""t1"", ""name"": ""Token"", ""status"": ""active"",
        ""issued_on"": ""2024-01-01T00:00:00Z"", ""modified_on"": ""2024-01-01T00:00:00Z"",
        ""policies"": [
          {
            ""id"": ""pol-1"",
            ""effect"": ""allow"",
            ""permission_groups"": [
              { ""id"": ""pg-1"", ""meta"": { ""key"": ""value"", ""number"": 42 } }
            ],
            ""resources"": { ""com.cloudflare.api.account.*"": ""*"" }
          }
        ]
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountTokenAsync("acc", "t1");

    // Assert
    var pg = result.Policies[0].PermissionGroups[0];
    pg.Id.Should().Be("pg-1");
    pg.Meta.Should().NotBeNull();
    pg.Meta!["key"].GetString().Should().Be("value");
    pg.Meta["number"].GetInt32().Should().Be(42);
  }

  #endregion


  #region URL Encoding Tests (U23-U24)

  /// <summary>U23: Verifies that GetAccountTokenAsync properly URL-encodes the account ID.</summary>
  [Fact]
  public async Task GetAccountTokenAsync_WithSpecialChars_UrlEncodesAccountId()
  {
    // Arrange
    var accountId = "abc+def/ghi";
    var tokenId = "token-123";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestToken(tokenId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountTokenAsync(accountId, tokenId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.AbsolutePath.Should().Contain("abc%2Bdef%2Fghi");
  }

  /// <summary>U24: Verifies that GetAccountTokenAsync properly URL-encodes the token ID.</summary>
  [Fact]
  public async Task GetAccountTokenAsync_WithSpecialChars_UrlEncodesTokenId()
  {
    // Arrange
    var accountId = "acc-123";
    var tokenId = "token+with/special";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestToken(tokenId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountTokenAsync(accountId, tokenId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.AbsolutePath.Should().Contain("token%2Bwith%2Fspecial");
  }

  #endregion


  #region Parameter Validation Tests (U25-U28)

  /// <summary>U25: Verifies that ListAccountTokensAsync throws on null accountId.</summary>
  [Fact]
  public async Task ListAccountTokensAsync_NullAccountId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.ListAccountTokensAsync(null!));
  }

  /// <summary>U26: Verifies that GetAccountTokenAsync throws on null accountId.</summary>
  [Fact]
  public async Task GetAccountTokenAsync_NullAccountId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetAccountTokenAsync(null!, "token-123"));
  }

  /// <summary>U27: Verifies that GetAccountTokenAsync throws on null tokenId.</summary>
  [Fact]
  public async Task GetAccountTokenAsync_NullTokenId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetAccountTokenAsync("acc-123", null!));
  }

  /// <summary>U28: Verifies that CreateAccountTokenAsync throws on null request.</summary>
  [Fact]
  public async Task CreateAccountTokenAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CreateAccountTokenAsync("acc-123", null!));
  }

  #endregion


  #region Pagination Tests (U29-U31)

  /// <summary>U29: Verifies ListAllAccountTokensAsync makes single request for single page.</summary>
  [Fact]
  public async Task ListAllAccountTokensAsync_SinglePage_MakesSingleRequest()
  {
    // Arrange
    var token = CreateTestToken("token-1");
    var response = CreatePagePaginatedResponse(new[] { token }, page: 1, perPage: 20, totalPages: 1, totalCount: 1);
    var requestCount = 0;
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((_, _) => requestCount++)
      .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(response) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var results = new List<ApiToken>();
    await foreach (var t in sut.ListAllAccountTokensAsync("acc-123"))
      results.Add(t);

    // Assert
    requestCount.Should().Be(1);
    results.Should().HaveCount(1);
  }

  /// <summary>U30: Verifies ListAllAccountTokensAsync makes multiple requests for multiple pages.</summary>
  [Fact]
  public async Task ListAllAccountTokensAsync_MultiplePages_MakesMultipleRequests()
  {
    // Arrange
    var token1 = CreateTestToken("token-1");
    var token2 = CreateTestToken("token-2");

    var responsePage1 = CreatePagePaginatedResponse(new[] { token1 }, page: 1, perPage: 1, totalPages: 2, totalCount: 2);
    var responsePage2 = CreatePagePaginatedResponse(new[] { token2 }, page: 2, perPage: 1, totalPages: 2, totalCount: 2);

    var capturedRequests = new List<HttpRequestMessage>();
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequests.Add(req))
      .Returns((HttpRequestMessage req, CancellationToken _) =>
      {
        var query = req.RequestUri?.Query ?? "";
        if (query.Contains("page=2"))
          return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage2) });
        return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage1) });
      });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var results = new List<ApiToken>();
    await foreach (var t in sut.ListAllAccountTokensAsync("acc-123", new ListApiTokensFilters(PerPage: 1)))
      results.Add(t);

    // Assert
    capturedRequests.Should().HaveCount(2);
    results.Should().HaveCount(2);
    results.Select(t => t.Id).Should().ContainInOrder("token-1", "token-2");
  }

  /// <summary>U31: Verifies ListAllAccountTokensAsync handles empty result.</summary>
  [Fact]
  public async Task ListAllAccountTokensAsync_EmptyResult_YieldsNothing()
  {
    // Arrange
    var response = CreatePagePaginatedResponse(Array.Empty<ApiToken>(), page: 1, perPage: 20, totalPages: 0, totalCount: 0);
    var requestCount = 0;
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((_, _) => requestCount++)
      .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(response) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var results = new List<ApiToken>();
    await foreach (var t in sut.ListAllAccountTokensAsync("acc-123"))
      results.Add(t);

    // Assert
    requestCount.Should().Be(1);
    results.Should().BeEmpty();
  }

  #endregion


  #region Error Handling Tests (U32-U37)

  /// <summary>U32: Verifies GetAccountTokenAsync throws on 404 Not Found.</summary>
  [Fact]
  public async Task GetAccountTokenAsync_WhenNotFound_ThrowsHttpRequestException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 9109, ""message"": ""Token not found"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.NotFound);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetAccountTokenAsync("acc", "nonexistent"));
    exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  /// <summary>U33: Verifies API error envelope throws CloudflareApiException.</summary>
  [Fact]
  public async Task CreateAccountTokenAsync_WhenApiError_ThrowsCloudflareApiException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 9102, ""message"": ""Invalid token policy"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    var request = new CreateApiTokenRequest("Test", new[] {
      new CreateTokenPolicyRequest("invalid", new[] { new TokenPermissionGroupReference("pg-1") },
        new Dictionary<string, string> { ["*"] = "*" })
    });
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(() => sut.CreateAccountTokenAsync("acc", request));
    exception.Errors.Should().HaveCount(1);
    exception.Errors[0].Code.Should().Be(9102);
  }

  /// <summary>U34: Verifies unauthorized (401) throws HttpRequestException.</summary>
  [Fact]
  public async Task ListAccountTokensAsync_WhenUnauthorized_ThrowsHttpRequestException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10000, ""message"": ""Authentication error"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.Unauthorized);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.ListAccountTokensAsync("acc"));
    exception.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>U35: Verifies forbidden (403) throws HttpRequestException.</summary>
  [Fact]
  public async Task DeleteAccountTokenAsync_WhenForbidden_ThrowsHttpRequestException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10000, ""message"": ""Access denied"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.Forbidden);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.DeleteAccountTokenAsync("acc", "token-123"));
    exception.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }

  /// <summary>U36: Verifies rate limited (429) throws HttpRequestException.</summary>
  [Fact]
  public async Task RollAccountTokenAsync_WhenRateLimited_ThrowsHttpRequestException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10000, ""message"": ""Rate limited"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.TooManyRequests);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.RollAccountTokenAsync("acc", "token-123"));
    exception.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
  }

  /// <summary>U37: Verifies server error (500) throws HttpRequestException.</summary>
  [Fact]
  public async Task VerifyAccountTokenAsync_WhenServerError_ThrowsHttpRequestException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10000, ""message"": ""Internal server error"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.InternalServerError);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.VerifyAccountTokenAsync("acc"));
    exception.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
  }

  #endregion


  #region Helper Methods

  /// <summary>Creates a test ApiToken instance with default or custom values.</summary>
  private static ApiToken CreateTestToken(string? id = null, string? name = null)
  {
    return new ApiToken(
      id ?? "test-token-123",
      name ?? "Test Token",
      TokenStatus.Active,
      DateTime.UtcNow,
      DateTime.UtcNow,
      Array.Empty<TokenPolicy>()
    );
  }

  /// <summary>Creates a test CreateApiTokenResult instance.</summary>
  private static CreateApiTokenResult CreateTestTokenResult()
  {
    return new CreateApiTokenResult(
      "new-token-123",
      "New Token",
      "secret-value-xyz",
      TokenStatus.Active,
      DateTime.UtcNow,
      DateTime.UtcNow,
      Array.Empty<TokenPolicy>()
    );
  }

  /// <summary>Creates a page-paginated JSON response for API tokens or permission groups.</summary>
  private static string CreatePagePaginatedResponse<T>(
    IEnumerable<T> items,
    int page = 1,
    int perPage = 20,
    int? totalPages = null,
    int? totalCount = null)
  {
    var itemList = items.ToList();
    var tp = totalPages ?? (itemList.Count > 0 ? 1 : 0);
    var tc = totalCount ?? itemList.Count;

    return JsonSerializer.Serialize(
      new
      {
        success = true,
        errors = Array.Empty<object>(),
        messages = Array.Empty<object>(),
        result = itemList,
        result_info = new
        {
          page,
          per_page = perPage,
          count = itemList.Count,
          total_pages = tp,
          total_count = tc
        }
      },
      new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
  }

  #endregion


  // ==========================================================================================================
  // F17: User API Tokens - Unit Tests
  // ==========================================================================================================

  #region F17: User Token Request Construction Tests (F17-U01 to F17-U13)

  /// <summary>F17-U01: Verifies that ListUserTokensAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task ListUserTokensAsync_NoFilters_SendsCorrectRequest()
  {
    // Arrange
    var successResponse = CreatePagePaginatedResponse(Array.Empty<ApiToken>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.ListUserTokensAsync();

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be("/client/v4/user/tokens");
    capturedRequest.RequestUri.Query.Should().BeEmpty();
  }

  /// <summary>F17-U02: Verifies that ListUserTokensAsync includes pagination parameters.</summary>
  [Fact]
  public async Task ListUserTokensAsync_WithPagination_SendsCorrectRequest()
  {
    // Arrange
    var filters = new ListApiTokensFilters(Page: 2, PerPage: 10);
    var successResponse = CreatePagePaginatedResponse(Array.Empty<ApiToken>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.ListUserTokensAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("page=2");
    capturedRequest.RequestUri.Query.Should().Contain("per_page=10");
  }

  /// <summary>F17-U03: Verifies that ListUserTokensAsync includes direction parameter.</summary>
  [Fact]
  public async Task ListUserTokensAsync_WithDirection_SendsCorrectRequest()
  {
    // Arrange
    var filters = new ListApiTokensFilters(Direction: ListOrderDirection.Descending);
    var successResponse = CreatePagePaginatedResponse(Array.Empty<ApiToken>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.ListUserTokensAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("direction=desc");
  }

  /// <summary>F17-U04: Verifies ListAllUserTokensAsync makes single request for single page.</summary>
  [Fact]
  public async Task ListAllUserTokensAsync_SinglePage_MakesSingleRequest()
  {
    // Arrange
    var token = CreateTestToken("token-1");
    var response = CreatePagePaginatedResponse(new[] { token }, page: 1, perPage: 20, totalPages: 1, totalCount: 1);
    var requestCount = 0;
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((_, _) => requestCount++)
      .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(response) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var results = new List<ApiToken>();
    await foreach (var t in sut.ListAllUserTokensAsync())
      results.Add(t);

    // Assert
    requestCount.Should().Be(1);
    results.Should().HaveCount(1);
  }

  /// <summary>F17-U05: Verifies ListAllUserTokensAsync makes multiple requests for multiple pages.</summary>
  [Fact]
  public async Task ListAllUserTokensAsync_MultiplePages_MakesMultipleRequests()
  {
    // Arrange
    var token1 = CreateTestToken("token-1");
    var token2 = CreateTestToken("token-2");

    var responsePage1 = CreatePagePaginatedResponse(new[] { token1 }, page: 1, perPage: 1, totalPages: 2, totalCount: 2);
    var responsePage2 = CreatePagePaginatedResponse(new[] { token2 }, page: 2, perPage: 1, totalPages: 2, totalCount: 2);

    var capturedRequests = new List<HttpRequestMessage>();
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequests.Add(req))
      .Returns((HttpRequestMessage req, CancellationToken _) =>
      {
        var query = req.RequestUri?.Query ?? "";
        if (query.Contains("page=2"))
          return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage2) });
        return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage1) });
      });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var results = new List<ApiToken>();
    await foreach (var t in sut.ListAllUserTokensAsync(new ListApiTokensFilters(PerPage: 1)))
      results.Add(t);

    // Assert
    capturedRequests.Should().HaveCount(2);
    results.Should().HaveCount(2);
    results.Select(t => t.Id).Should().ContainInOrder("token-1", "token-2");
  }

  /// <summary>F17-U06: Verifies that GetUserTokenAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task GetUserTokenAsync_SendsCorrectRequest()
  {
    // Arrange
    var tokenId = "token-123";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestToken(tokenId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.GetUserTokenAsync(tokenId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/user/tokens/{tokenId}");
  }

  /// <summary>F17-U07: Verifies that CreateUserTokenAsync sends a POST request with correct body.</summary>
  [Fact]
  public async Task CreateUserTokenAsync_SendsCorrectRequest()
  {
    // Arrange
    var request = new CreateApiTokenRequest(
      "Test User Token",
      new[]
      {
        new CreateTokenPolicyRequest(
          "allow",
          new[] { new TokenPermissionGroupReference("perm-group-123") },
          new Dictionary<string, string> { ["com.cloudflare.api.user"] = "*" }
        )
      },
      ExpiresOn: DateTime.UtcNow.AddDays(30)
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestTokenResult());
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateUserTokenAsync(request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be("/client/v4/user/tokens");
    capturedBody.Should().NotBeNull();
    capturedBody.Should().Contain("\"name\"");
    capturedBody.Should().Contain("Test User Token");
    capturedBody.Should().Contain("\"policies\"");
    capturedBody.Should().Contain("\"allow\"");
  }

  /// <summary>F17-U08: Verifies that UpdateUserTokenAsync sends a PUT request with correct body.</summary>
  [Fact]
  public async Task UpdateUserTokenAsync_SendsCorrectRequest()
  {
    // Arrange
    var tokenId = "token-123";
    var request = new UpdateApiTokenRequest(
      "Updated User Token",
      new[]
      {
        new CreateTokenPolicyRequest(
          "allow",
          new[] { new TokenPermissionGroupReference("perm-group-456") },
          new Dictionary<string, string> { ["com.cloudflare.api.user"] = "*" }
        )
      },
      Status: TokenStatus.Disabled
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestToken(tokenId));
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateUserTokenAsync(tokenId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/user/tokens/{tokenId}");
    capturedBody.Should().NotBeNull();
    capturedBody.Should().Contain("Updated User Token");
    capturedBody.Should().Contain("\"status\"");
    capturedBody.Should().Contain("\"disabled\"");
  }

  /// <summary>F17-U09: Verifies that DeleteUserTokenAsync sends a DELETE request to the correct endpoint.</summary>
  [Fact]
  public async Task DeleteUserTokenAsync_SendsCorrectRequest()
  {
    // Arrange
    var tokenId = "token-to-delete";
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.DeleteUserTokenAsync(tokenId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/user/tokens/{tokenId}");
  }

  /// <summary>F17-U10: Verifies that VerifyUserTokenAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task VerifyUserTokenAsync_SendsCorrectRequest()
  {
    // Arrange
    var successResponse = HttpFixtures.CreateSuccessResponse(new VerifyTokenResult("token-123", TokenStatus.Active));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.VerifyUserTokenAsync();

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be("/client/v4/user/tokens/verify");
  }

  /// <summary>F17-U11: Verifies that RollUserTokenAsync sends a PUT request to the correct endpoint.</summary>
  [Fact]
  public async Task RollUserTokenAsync_SendsCorrectRequest()
  {
    // Arrange
    var tokenId = "token-to-roll";
    var newTokenValue = "new-secret-token-value-xyz";
    var successResponse = HttpFixtures.CreateSuccessResponse(newTokenValue);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.RollUserTokenAsync(tokenId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/user/tokens/{tokenId}/value");
    result.Should().Be(newTokenValue);
  }

  /// <summary>F17-U12: Verifies that GetUserPermissionGroupsAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task GetUserPermissionGroupsAsync_SendsCorrectRequest()
  {
    // Arrange
    var successResponse = CreatePagePaginatedResponse(new[]
    {
      new PermissionGroup("pg-1", "Zone Read", new[] { "zone:read" })
    });
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.GetUserPermissionGroupsAsync();

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be("/client/v4/user/tokens/permission_groups");
  }

  /// <summary>F17-U13: Verifies that GetUserPermissionGroupsAsync includes filter parameters.</summary>
  [Fact]
  public async Task GetUserPermissionGroupsAsync_WithFilters_SendsCorrectRequest()
  {
    // Arrange
    var filters = new ListPermissionGroupsFilters(Name: "Zone", Scope: "zone:read", Page: 2, PerPage: 10);
    var successResponse = CreatePagePaginatedResponse(Array.Empty<PermissionGroup>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.GetUserPermissionGroupsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("name=Zone");
    capturedRequest.RequestUri.Query.Should().Contain("scope=zone%3Aread");
    capturedRequest.RequestUri.Query.Should().Contain("page=2");
    capturedRequest.RequestUri.Query.Should().Contain("per_page=10");
  }

  #endregion


  #region F17: User Token URL Encoding Tests (F17-U40 to F17-U41)

  /// <summary>F17-U40: Verifies that GetUserTokenAsync properly URL-encodes the token ID.</summary>
  [Fact]
  public async Task GetUserTokenAsync_WithSpecialChars_UrlEncodesTokenId()
  {
    // Arrange
    var tokenId = "token+with/special";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestToken(tokenId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    await sut.GetUserTokenAsync(tokenId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.AbsolutePath.Should().Contain("token%2Bwith%2Fspecial");
  }

  /// <summary>F17-U41: Verifies GetAllUserPermissionGroupsAsync iterates all pages.</summary>
  [Fact]
  public async Task GetAllUserPermissionGroupsAsync_MultiplePages_MakesMultipleRequests()
  {
    // Arrange
    var group1 = new PermissionGroup("pg-1", "Zone Read", new[] { "zone:read" });
    var group2 = new PermissionGroup("pg-2", "DNS Read", new[] { "dns_records:read" });

    var responsePage1 = CreatePagePaginatedResponse(new[] { group1 }, page: 1, perPage: 1, totalPages: 2, totalCount: 2);
    var responsePage2 = CreatePagePaginatedResponse(new[] { group2 }, page: 2, perPage: 1, totalPages: 2, totalCount: 2);

    var capturedRequests = new List<HttpRequestMessage>();
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequests.Add(req))
      .Returns((HttpRequestMessage req, CancellationToken _) =>
      {
        var query = req.RequestUri?.Query ?? "";
        if (query.Contains("page=2"))
          return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage2) });
        return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage1) });
      });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act
    var results = new List<PermissionGroup>();
    await foreach (var g in sut.GetAllUserPermissionGroupsAsync(new ListPermissionGroupsFilters(PerPage: 1)))
      results.Add(g);

    // Assert
    capturedRequests.Should().HaveCount(2);
    results.Should().HaveCount(2);
    results.Select(g => g.Id).Should().ContainInOrder("pg-1", "pg-2");
  }

  #endregion


  #region F17: User Token Parameter Validation Tests (F17-U42 to F17-U47)

  /// <summary>F17-U42: Verifies that GetUserTokenAsync throws on null tokenId.</summary>
  [Fact]
  public async Task GetUserTokenAsync_NullTokenId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetUserTokenAsync(null!));
  }

  /// <summary>F17-U43: Verifies that GetUserTokenAsync throws on whitespace tokenId.</summary>
  [Fact]
  public async Task GetUserTokenAsync_WhitespaceTokenId_ThrowsArgumentException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() => sut.GetUserTokenAsync("   "));
  }

  /// <summary>F17-U44: Verifies that CreateUserTokenAsync throws on null request.</summary>
  [Fact]
  public async Task CreateUserTokenAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CreateUserTokenAsync(null!));
  }

  /// <summary>F17-U45: Verifies that UpdateUserTokenAsync throws on null tokenId.</summary>
  [Fact]
  public async Task UpdateUserTokenAsync_NullTokenId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);
    var request = new UpdateApiTokenRequest("Name", Array.Empty<CreateTokenPolicyRequest>());

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.UpdateUserTokenAsync(null!, request));
  }

  /// <summary>F17-U46: Verifies that UpdateUserTokenAsync throws on null request.</summary>
  [Fact]
  public async Task UpdateUserTokenAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.UpdateUserTokenAsync("token-123", null!));
  }

  /// <summary>F17-U47: Verifies that DeleteUserTokenAsync throws on null tokenId.</summary>
  [Fact]
  public async Task DeleteUserTokenAsync_NullTokenId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.DeleteUserTokenAsync(null!));
  }

  /// <summary>F17-U48: Verifies that RollUserTokenAsync throws on null tokenId.</summary>
  [Fact]
  public async Task RollUserTokenAsync_NullTokenId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.RollUserTokenAsync(null!));
  }

  #endregion


  #region F17: User Token Error Handling Tests (F17-U30 to F17-U39)

  /// <summary>F17-U30: Verifies GetUserTokenAsync throws on 404 Not Found.</summary>
  [Fact]
  public async Task GetUserTokenAsync_WhenNotFound_ThrowsHttpRequestException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 9109, ""message"": ""Token not found"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.NotFound);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetUserTokenAsync("nonexistent"));
    exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  /// <summary>F17-U31: Verifies unauthorized (401) throws HttpRequestException.</summary>
  [Fact]
  public async Task ListUserTokensAsync_WhenUnauthorized_ThrowsHttpRequestException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10000, ""message"": ""Authentication error"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.Unauthorized);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.ListUserTokensAsync());
    exception.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>F17-U32: Verifies API error envelope throws CloudflareApiException.</summary>
  [Fact]
  public async Task CreateUserTokenAsync_WhenApiError_ThrowsCloudflareApiException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 9102, ""message"": ""Invalid token policy"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    var request = new CreateApiTokenRequest("Test", new[] {
      new CreateTokenPolicyRequest("invalid", new[] { new TokenPermissionGroupReference("pg-1") },
        new Dictionary<string, string> { ["*"] = "*" })
    });
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(() => sut.CreateUserTokenAsync(request));
    exception.Errors.Should().HaveCount(1);
    exception.Errors[0].Code.Should().Be(9102);
  }

  /// <summary>F17-U34: Verifies multiple errors in response are captured.</summary>
  [Fact]
  public async Task CreateUserTokenAsync_WhenMultipleApiErrors_CapturesAllErrors()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [
        { ""code"": 9102, ""message"": ""Invalid token policy"" },
        { ""code"": 9103, ""message"": ""Invalid permission group"" }
      ],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    var request = new CreateApiTokenRequest("Test", new[] {
      new CreateTokenPolicyRequest("invalid", new[] { new TokenPermissionGroupReference("pg-1") },
        new Dictionary<string, string> { ["*"] = "*" })
    });
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(() => sut.CreateUserTokenAsync(request));
    exception.Errors.Should().HaveCount(2);
    exception.Errors.Select(e => e.Code).Should().Contain(new[] { 9102, 9103 });
  }

  /// <summary>F17-U35: Verifies forbidden (403) throws HttpRequestException.</summary>
  [Fact]
  public async Task DeleteUserTokenAsync_WhenForbidden_ThrowsHttpRequestException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10000, ""message"": ""Access denied"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.Forbidden);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.DeleteUserTokenAsync("token-123"));
    exception.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }

  /// <summary>F17-U36: Verifies rate limited (429) throws HttpRequestException.</summary>
  [Fact]
  public async Task RollUserTokenAsync_WhenRateLimited_ThrowsHttpRequestException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10000, ""message"": ""Rate limited"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.TooManyRequests);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.RollUserTokenAsync("token-123"));
    exception.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
  }

  /// <summary>F17-U37: Verifies server error (500) throws HttpRequestException.</summary>
  [Fact]
  public async Task VerifyUserTokenAsync_WhenServerError500_ThrowsHttpRequestException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10000, ""message"": ""Internal server error"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.InternalServerError);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.VerifyUserTokenAsync());
    exception.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
  }

  /// <summary>F17-U38: Verifies server error (502) throws HttpRequestException.</summary>
  [Fact]
  public async Task GetUserPermissionGroupsAsync_WhenServerError502_ThrowsHttpRequestException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10000, ""message"": ""Bad Gateway"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.BadGateway);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetUserPermissionGroupsAsync());
    exception.StatusCode.Should().Be(HttpStatusCode.BadGateway);
  }

  /// <summary>F17-U39: Verifies server error (503) throws HttpRequestException.</summary>
  [Fact]
  public async Task UpdateUserTokenAsync_WhenServerError503_ThrowsHttpRequestException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10000, ""message"": ""Service Unavailable"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.ServiceUnavailable);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new ApiTokensApi(httpClient, _loggerFactory);

    // Act & Assert
    var request = new UpdateApiTokenRequest("Name", Array.Empty<CreateTokenPolicyRequest>());
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.UpdateUserTokenAsync("token-123", request));
    exception.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
  }

  #endregion
}
