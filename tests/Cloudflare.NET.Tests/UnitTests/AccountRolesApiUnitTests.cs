namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using Cloudflare.NET.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Roles;
using Roles.Models;
using Shared.Fixtures;
using Xunit.Abstractions;


/// <summary>Contains unit tests for the <see cref="RolesApi"/> class.</summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class AccountRolesApiUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly ILoggerFactory _loggerFactory;

  #endregion


  #region Constructors

  public AccountRolesApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion


  #region Request Construction Tests (U01-U03)

  /// <summary>U01: Verifies that ListAccountRolesAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task ListAccountRolesAsync_NoFilters_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var successResponse = CreatePagePaginatedResponse(Array.Empty<AccountRole>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAccountRolesAsync(accountId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/roles");
    capturedRequest.RequestUri.Query.Should().BeEmpty();
  }

  /// <summary>U02: Verifies that ListAccountRolesAsync includes pagination parameters.</summary>
  [Fact]
  public async Task ListAccountRolesAsync_WithPagination_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var filters = new ListAccountRolesFilters(Page: 2, PerPage: 10);
    var successResponse = CreatePagePaginatedResponse(Array.Empty<AccountRole>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAccountRolesAsync(accountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("page=2");
    capturedRequest.RequestUri.Query.Should().Contain("per_page=10");
  }

  /// <summary>U03: Verifies that GetAccountRoleAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task GetAccountRoleAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var roleId = "role-123";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestRole(roleId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountRoleAsync(accountId, roleId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/roles/{roleId}");
  }

  #endregion


  #region Response Deserialization Tests (U04-U09)

  /// <summary>U04: Verifies that AccountRole model deserializes all properties correctly.</summary>
  [Fact]
  public async Task GetAccountRoleAsync_DeserializesFullModel()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""role-admin"",
        ""name"": ""Administrator"",
        ""description"": ""Administrative access to the entire Account"",
        ""permissions"": {
          ""analytics"": { ""read"": true, ""write"": true },
          ""billing"": { ""read"": true, ""write"": true },
          ""cache_purge"": { ""read"": true, ""write"": true },
          ""dns"": { ""read"": true, ""write"": true },
          ""dns_records"": { ""read"": true, ""write"": true },
          ""lb"": { ""read"": true, ""write"": true },
          ""logs"": { ""read"": true, ""write"": true },
          ""organization"": { ""read"": true, ""write"": true },
          ""ssl"": { ""read"": true, ""write"": true },
          ""waf"": { ""read"": true, ""write"": true },
          ""zone_settings"": { ""read"": true, ""write"": true },
          ""zones"": { ""read"": true, ""write"": true }
        }
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountRoleAsync("acc123", "role-admin");

    // Assert
    result.Id.Should().Be("role-admin");
    result.Name.Should().Be("Administrator");
    result.Description.Should().Be("Administrative access to the entire Account");
    result.Permissions.Should().NotBeNull();
    result.Permissions.Analytics.Should().NotBeNull();
    result.Permissions.Analytics!.Read.Should().BeTrue();
    result.Permissions.Analytics.Write.Should().BeTrue();
    result.Permissions.Billing!.Read.Should().BeTrue();
    result.Permissions.Dns!.Read.Should().BeTrue();
    result.Permissions.Dns.Write.Should().BeTrue();
    result.Permissions.DnsRecords!.Read.Should().BeTrue();
    result.Permissions.Zones!.Read.Should().BeTrue();
    result.Permissions.Zones.Write.Should().BeTrue();
  }

  /// <summary>U05: Verifies that RolePermissions with all fields deserializes correctly.</summary>
  [Fact]
  public async Task GetAccountRoleAsync_DeserializesAllPermissionFields()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""role-full"",
        ""name"": ""Full Access"",
        ""description"": ""All permissions"",
        ""permissions"": {
          ""analytics"": { ""read"": true, ""write"": true },
          ""billing"": { ""read"": true, ""write"": true },
          ""cache_purge"": { ""read"": true, ""write"": true },
          ""dns"": { ""read"": true, ""write"": true },
          ""dns_records"": { ""read"": true, ""write"": true },
          ""lb"": { ""read"": true, ""write"": true },
          ""logs"": { ""read"": true, ""write"": true },
          ""organization"": { ""read"": true, ""write"": true },
          ""ssl"": { ""read"": true, ""write"": true },
          ""waf"": { ""read"": true, ""write"": true },
          ""zone_settings"": { ""read"": true, ""write"": true },
          ""zones"": { ""read"": true, ""write"": true }
        }
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountRoleAsync("acc", "role-full");

    // Assert
    result.Permissions.Analytics.Should().NotBeNull();
    result.Permissions.Billing.Should().NotBeNull();
    result.Permissions.CachePurge.Should().NotBeNull();
    result.Permissions.Dns.Should().NotBeNull();
    result.Permissions.DnsRecords.Should().NotBeNull();
    result.Permissions.LoadBalancer.Should().NotBeNull();
    result.Permissions.Logs.Should().NotBeNull();
    result.Permissions.Organization.Should().NotBeNull();
    result.Permissions.Ssl.Should().NotBeNull();
    result.Permissions.Waf.Should().NotBeNull();
    result.Permissions.ZoneSettings.Should().NotBeNull();
    result.Permissions.Zones.Should().NotBeNull();
  }

  /// <summary>U06: Verifies that RolePermissions with partial permissions deserializes correctly.</summary>
  [Fact]
  public async Task GetAccountRoleAsync_DeserializesPartialPermissions()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""role-dns"",
        ""name"": ""DNS Administrator"",
        ""description"": ""DNS management only"",
        ""permissions"": {
          ""dns"": { ""read"": true, ""write"": true },
          ""dns_records"": { ""read"": true, ""write"": true }
        }
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountRoleAsync("acc", "role-dns");

    // Assert
    result.Permissions.Dns.Should().NotBeNull();
    result.Permissions.DnsRecords.Should().NotBeNull();
    result.Permissions.Analytics.Should().BeNull();
    result.Permissions.Billing.Should().BeNull();
    result.Permissions.CachePurge.Should().BeNull();
    result.Permissions.LoadBalancer.Should().BeNull();
    result.Permissions.Logs.Should().BeNull();
    result.Permissions.Organization.Should().BeNull();
    result.Permissions.Ssl.Should().BeNull();
    result.Permissions.Waf.Should().BeNull();
    result.Permissions.ZoneSettings.Should().BeNull();
    result.Permissions.Zones.Should().BeNull();
  }

  /// <summary>U07: Verifies that PermissionGrant with read-only access deserializes correctly.</summary>
  [Fact]
  public async Task GetAccountRoleAsync_DeserializesReadOnlyPermission()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""role-viewer"",
        ""name"": ""Viewer"",
        ""description"": ""Read-only access"",
        ""permissions"": {
          ""analytics"": { ""read"": true, ""write"": false },
          ""zones"": { ""read"": true, ""write"": false }
        }
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountRoleAsync("acc", "role-viewer");

    // Assert
    result.Permissions.Analytics!.Read.Should().BeTrue();
    result.Permissions.Analytics.Write.Should().BeFalse();
    result.Permissions.Zones!.Read.Should().BeTrue();
    result.Permissions.Zones.Write.Should().BeFalse();
  }

  /// <summary>U08: Verifies that PermissionGrant with full access deserializes correctly.</summary>
  [Fact]
  public async Task GetAccountRoleAsync_DeserializesFullAccessPermission()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""role-admin"",
        ""name"": ""Admin"",
        ""description"": ""Full access"",
        ""permissions"": {
          ""dns"": { ""read"": true, ""write"": true }
        }
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountRoleAsync("acc", "role-admin");

    // Assert
    result.Permissions.Dns!.Read.Should().BeTrue();
    result.Permissions.Dns.Write.Should().BeTrue();
  }

  /// <summary>U09: Verifies that PermissionGrant with no access deserializes correctly.</summary>
  [Fact]
  public async Task GetAccountRoleAsync_DeserializesNoAccessPermission()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""role-none"",
        ""name"": ""No Access"",
        ""description"": ""No permissions"",
        ""permissions"": {
          ""dns"": { ""read"": false, ""write"": false }
        }
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountRoleAsync("acc", "role-none");

    // Assert
    result.Permissions.Dns!.Read.Should().BeFalse();
    result.Permissions.Dns.Write.Should().BeFalse();
  }

  #endregion


  #region Pagination Tests (U10-U11)

  /// <summary>U10: Verifies ListAllAccountRolesAsync makes single request for single page.</summary>
  [Fact]
  public async Task ListAllAccountRolesAsync_SinglePage_MakesSingleRequest()
  {
    // Arrange
    var role = CreateTestRole("role-1");
    var response = CreatePagePaginatedResponse(new[] { role }, page: 1, perPage: 20, totalPages: 1, totalCount: 1);
    var requestCount = 0;
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((_, _) => requestCount++)
      .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(response) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act
    var results = new List<AccountRole>();
    await foreach (var r in sut.ListAllAccountRolesAsync("acc-123"))
      results.Add(r);

    // Assert
    requestCount.Should().Be(1);
    results.Should().HaveCount(1);
  }

  /// <summary>U11: Verifies ListAllAccountRolesAsync makes multiple requests for multiple pages.</summary>
  [Fact]
  public async Task ListAllAccountRolesAsync_MultiplePages_MakesMultipleRequests()
  {
    // Arrange
    var role1 = CreateTestRole("role-1");
    var role2 = CreateTestRole("role-2");

    var responsePage1 = CreatePagePaginatedResponse(new[] { role1 }, page: 1, perPage: 1, totalPages: 2, totalCount: 2);
    var responsePage2 = CreatePagePaginatedResponse(new[] { role2 }, page: 2, perPage: 1, totalPages: 2, totalCount: 2);

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
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act
    var results = new List<AccountRole>();
    await foreach (var r in sut.ListAllAccountRolesAsync("acc-123", new ListAccountRolesFilters(PerPage: 1)))
      results.Add(r);

    // Assert
    capturedRequests.Should().HaveCount(2);
    results.Should().HaveCount(2);
    results.Select(r => r.Id).Should().ContainInOrder("role-1", "role-2");
  }

  #endregion


  #region Error Handling Tests (U12-U14)

  /// <summary>U12: Verifies API error envelope throws CloudflareApiException.</summary>
  [Fact]
  public async Task ListAccountRolesAsync_WhenApiError_ThrowsCloudflareApiException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10000, ""message"": ""Invalid account"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(() => sut.ListAccountRolesAsync("acc"));
    exception.Errors.Should().HaveCount(1);
    exception.Errors[0].Code.Should().Be(10000);
  }

  /// <summary>U13: Verifies multiple errors in response are captured in CloudflareApiException.</summary>
  [Fact]
  public async Task ListAccountRolesAsync_WhenMultipleErrors_CapturesAllErrors()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [
        { ""code"": 10001, ""message"": ""First error"" },
        { ""code"": 10002, ""message"": ""Second error"" }
      ],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(() => sut.ListAccountRolesAsync("acc"));
    exception.Errors.Should().HaveCount(2);
  }

  #endregion


  #region URL Encoding Tests (U15-U16)

  /// <summary>U15: Verifies that GetAccountRoleAsync properly URL-encodes the account ID.</summary>
  [Fact]
  public async Task GetAccountRoleAsync_WithSpecialChars_UrlEncodesAccountId()
  {
    // Arrange
    var accountId = "abc+def/ghi";
    var roleId = "role-123";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestRole(roleId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountRoleAsync(accountId, roleId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.AbsolutePath.Should().Contain("abc%2Bdef%2Fghi");
  }

  /// <summary>U16: Verifies that GetAccountRoleAsync properly URL-encodes the role ID.</summary>
  [Fact]
  public async Task GetAccountRoleAsync_WithSpecialChars_UrlEncodesRoleId()
  {
    // Arrange
    var accountId = "acc-123";
    var roleId = "role+with/special";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestRole(roleId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountRoleAsync(accountId, roleId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.AbsolutePath.Should().Contain("role%2Bwith%2Fspecial");
  }

  #endregion


  #region Parameter Validation Tests (U21-U24)

  /// <summary>U21: Verifies that ListAccountRolesAsync throws on null accountId.</summary>
  [Fact]
  public async Task ListAccountRolesAsync_NullAccountId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.ListAccountRolesAsync(null!));
  }

  /// <summary>U22: Verifies that ListAccountRolesAsync throws on whitespace accountId.</summary>
  [Fact]
  public async Task ListAccountRolesAsync_WhitespaceAccountId_ThrowsArgumentException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() => sut.ListAccountRolesAsync("   "));
  }

  /// <summary>U23: Verifies that GetAccountRoleAsync throws on null accountId.</summary>
  [Fact]
  public async Task GetAccountRoleAsync_NullAccountId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetAccountRoleAsync(null!, "role-123"));
  }

  /// <summary>U24: Verifies that GetAccountRoleAsync throws on null roleId.</summary>
  [Fact]
  public async Task GetAccountRoleAsync_NullRoleId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new RolesApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetAccountRoleAsync("acc-123", null!));
  }

  #endregion


  #region Helper Methods

  /// <summary>Creates a test AccountRole instance with default or custom values.</summary>
  private static AccountRole CreateTestRole(string? id = null, string? name = null)
  {
    return new AccountRole(
      id ?? "test-role-123",
      name ?? "Test Role",
      "Test role description",
      new RolePermissions(
        Analytics: new PermissionGrant(true, false),
        Dns: new PermissionGrant(true, true)
      )
    );
  }

  /// <summary>Creates a page-paginated JSON response for account roles.</summary>
  private static string CreatePagePaginatedResponse(
    IEnumerable<AccountRole> items,
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
}
