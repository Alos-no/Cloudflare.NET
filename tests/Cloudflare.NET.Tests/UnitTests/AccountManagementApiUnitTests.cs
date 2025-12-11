namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using Accounts;
using Accounts.Models;
using Cloudflare.NET.Core.Exceptions;
using Cloudflare.NET.Security.Firewall.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.Protected;
using Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>Contains unit tests for the Account Management operations in the <see cref="AccountsApi" /> class.</summary>
/// <remarks>
///   This test class covers the Account CRUD operations (List, Get, Create, Update, Delete) as opposed to
///   the R2 bucket operations which are tested in <see cref="AccountsApiUnitTests" />.
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class AccountManagementApiUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly ILoggerFactory _loggerFactory;
  private readonly JsonSerializerOptions _serializerOptions =
    new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

  #endregion

  #region Constructors

  public AccountManagementApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion


  #region Account Management - Request Construction Tests (U01-U09)

  /// <summary>U01: Verifies that ListAccountsAsync sends a GET request to /accounts with no query params when no filters.</summary>
  [Fact]
  public async Task ListAccountsAsync_NoFilters_SendsCorrectRequest()
  {
    // Arrange
    var successResponse = CreatePagePaginatedResponse(new[] { CreateTestAccount() });
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.ListAccountsAsync();

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be("/client/v4/accounts");
    capturedRequest.RequestUri.Query.Should().BeEmpty();
  }

  /// <summary>U02: Verifies that ListAccountsAsync includes name filter in query string.</summary>
  [Fact]
  public async Task ListAccountsAsync_WithNameFilter_SendsCorrectRequest()
  {
    // Arrange
    var filters = new ListAccountsFilters(Name: "test");
    var successResponse = CreatePagePaginatedResponse(Array.Empty<Account>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.ListAccountsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("name=test");
  }

  /// <summary>U03: Verifies that ListAccountsAsync includes pagination parameters in query string.</summary>
  [Fact]
  public async Task ListAccountsAsync_WithPagination_SendsCorrectRequest()
  {
    // Arrange
    var filters = new ListAccountsFilters(Page: 2, PerPage: 10);
    var successResponse = CreatePagePaginatedResponse(Array.Empty<Account>(), page: 2, perPage: 10);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.ListAccountsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("page=2");
    capturedRequest.RequestUri.Query.Should().Contain("per_page=10");
  }

  /// <summary>U04: Verifies that ListAccountsAsync includes direction parameter in query string.</summary>
  [Fact]
  public async Task ListAccountsAsync_WithDirection_SendsCorrectRequest()
  {
    // Arrange
    var filters = new ListAccountsFilters(Direction: ListOrderDirection.Descending);
    var successResponse = CreatePagePaginatedResponse(Array.Empty<Account>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.ListAccountsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("direction=desc");
  }

  /// <summary>U05: Verifies that GetAccountAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task GetAccountAsync_SendsCorrectRequest()
  {
    // Arrange
    var targetAccountId = "abc123def456";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestAccount(targetAccountId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.GetAccountAsync(targetAccountId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{targetAccountId}");
  }

  /// <summary>U06: Verifies that CreateAccountAsync sends a POST request with correct body.</summary>
  [Fact]
  public async Task CreateAccountAsync_SendsCorrectRequest()
  {
    // Arrange
    var request = new CreateAccountRequest("My New Account");
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestAccount(name: "My New Account"));
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.CreateAccountAsync(request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be("/client/v4/accounts");
    capturedBody.Should().NotBeNull();
    capturedBody.Should().Contain("\"name\"");
    capturedBody.Should().Contain("My New Account");
  }

  /// <summary>U07: Verifies that UpdateAccountAsync sends a PUT request with name in body.</summary>
  [Fact]
  public async Task UpdateAccountAsync_WithName_SendsCorrectRequest()
  {
    // Arrange
    var targetAccountId = "abc123def456";
    var request = new UpdateAccountRequest("New Name");
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestAccount(targetAccountId, "New Name"));
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.UpdateAccountAsync(targetAccountId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{targetAccountId}");
    capturedBody.Should().NotBeNull();
    capturedBody.Should().Contain("\"name\"");
    capturedBody.Should().Contain("New Name");
  }

  /// <summary>U08: Verifies that UpdateAccountAsync sends settings in request body.</summary>
  [Fact]
  public async Task UpdateAccountAsync_WithSettings_SendsCorrectRequest()
  {
    // Arrange
    var targetAccountId = "abc123def456";
    var settings = new AccountSettings(EnforceTwofactor: true);
    var request = new UpdateAccountRequest("Test Account", settings);
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestAccount(targetAccountId, settings: settings));
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.UpdateAccountAsync(targetAccountId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedBody.Should().NotBeNull();
    capturedBody.Should().Contain("\"settings\"");
    capturedBody.Should().Contain("\"enforce_twofactor\"");
    capturedBody.Should().Contain("true");
  }

  /// <summary>U09: Verifies that DeleteAccountAsync sends a DELETE request to correct endpoint.</summary>
  [Fact]
  public async Task DeleteAccountAsync_SendsCorrectRequest()
  {
    // Arrange
    var targetAccountId = "abc123def456";
    var successResponse = HttpFixtures.CreateSuccessResponse(new DeleteAccountResult(targetAccountId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.DeleteAccountAsync(targetAccountId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{targetAccountId}");
  }

  #endregion


  #region Account Management - URL Encoding Tests (U10-U11)

  /// <summary>U10: Verifies that GetAccountAsync properly URL-encodes the account ID.</summary>
  [Fact]
  public async Task GetAccountAsync_WithSpecialChars_UrlEncodesAccountId()
  {
    // Arrange
    var targetAccountId = "abc+def/ghi";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestAccount(targetAccountId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.GetAccountAsync(targetAccountId);

    // Assert
    capturedRequest.Should().NotBeNull();
    // Uri.EscapeDataString encodes + as %2B and / as %2F
    capturedRequest!.RequestUri!.AbsolutePath.Should().Contain("abc%2Bdef%2Fghi");
  }

  /// <summary>U11: Verifies that ListAccountsAsync properly URL-encodes the name filter.</summary>
  [Fact]
  public async Task ListAccountsAsync_WithSpecialCharsInName_UrlEncodesName()
  {
    // Arrange
    var filters = new ListAccountsFilters(Name: "test account & company");
    var successResponse = CreatePagePaginatedResponse(Array.Empty<Account>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.ListAccountsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    // Uri.EscapeDataString encodes spaces as %20 and & as %26
    capturedRequest!.RequestUri!.Query.Should().Contain("name=test%20account%20%26%20company");
  }

  #endregion


  #region Account Management - Response Deserialization Tests (U12-U20)

  /// <summary>U12: Verifies Account model deserializes all properties correctly.</summary>
  [Fact]
  public async Task GetAccountAsync_DeserializesFullModel()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""abc123"",
        ""name"": ""Test Account"",
        ""type"": ""standard"",
        ""created_on"": ""2024-01-15T10:30:00Z"",
        ""managed_by"": {
          ""parent_org_id"": ""org123"",
          ""parent_org_name"": ""Parent Org""
        },
        ""settings"": {
          ""abuse_contact_email"": ""abuse@test.com"",
          ""enforce_twofactor"": true
        }
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.GetAccountAsync("abc123");

    // Assert
    result.Id.Should().Be("abc123");
    result.Name.Should().Be("Test Account");
    result.Type.Should().Be(AccountType.Standard);
    result.CreatedOn.Should().Be(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
    result.ManagedBy.Should().NotBeNull();
    result.ManagedBy!.ParentOrgId.Should().Be("org123");
    result.ManagedBy.ParentOrgName.Should().Be("Parent Org");
    result.Settings.Should().NotBeNull();
    result.Settings!.AbuseContactEmail.Should().Be("abuse@test.com");
    result.Settings.EnforceTwofactor.Should().BeTrue();
  }

  /// <summary>U13: Verifies Account model handles null optional fields.</summary>
  [Fact]
  public async Task GetAccountAsync_DeserializesOptionalNullFields()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""abc123"",
        ""name"": ""Test Account"",
        ""type"": ""standard"",
        ""created_on"": ""2024-01-15T10:30:00Z""
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.GetAccountAsync("abc123");

    // Assert
    result.ManagedBy.Should().BeNull();
    result.Settings.Should().BeNull();
  }

  /// <summary>U14: Verifies Account model deserializes managed_by nested record.</summary>
  [Fact]
  public async Task GetAccountAsync_DeserializesManagedBy()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""abc123"",
        ""name"": ""Managed Account"",
        ""type"": ""enterprise"",
        ""created_on"": ""2024-01-15T10:30:00Z"",
        ""managed_by"": {
          ""parent_org_id"": ""parent-org-456"",
          ""parent_org_name"": ""Enterprise Parent""
        }
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.GetAccountAsync("abc123");

    // Assert
    result.ManagedBy.Should().NotBeNull();
    result.ManagedBy!.ParentOrgId.Should().Be("parent-org-456");
    result.ManagedBy.ParentOrgName.Should().Be("Enterprise Parent");
  }

  /// <summary>U15: Verifies Account model deserializes settings nested record.</summary>
  [Fact]
  public async Task GetAccountAsync_DeserializesSettings()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""abc123"",
        ""name"": ""Configured Account"",
        ""type"": ""standard"",
        ""created_on"": ""2024-01-15T10:30:00Z"",
        ""settings"": {
          ""abuse_contact_email"": ""security@example.com"",
          ""enforce_twofactor"": false
        }
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.GetAccountAsync("abc123");

    // Assert
    result.Settings.Should().NotBeNull();
    result.Settings!.AbuseContactEmail.Should().Be("security@example.com");
    result.Settings.EnforceTwofactor.Should().BeFalse();
  }

  /// <summary>U16: Verifies AccountType.Standard deserializes correctly.</summary>
  [Fact]
  public async Task GetAccountAsync_DeserializesStandardAccountType()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""abc123"",
        ""name"": ""Standard Account"",
        ""type"": ""standard"",
        ""created_on"": ""2024-01-15T10:30:00Z""
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.GetAccountAsync("abc123");

    // Assert
    result.Type.Should().Be(AccountType.Standard);
    result.Type.Value.Should().Be("standard");
  }

  /// <summary>U17: Verifies AccountType.Enterprise deserializes correctly.</summary>
  [Fact]
  public async Task GetAccountAsync_DeserializesEnterpriseAccountType()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""abc123"",
        ""name"": ""Enterprise Account"",
        ""type"": ""enterprise"",
        ""created_on"": ""2024-01-15T10:30:00Z""
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.GetAccountAsync("abc123");

    // Assert
    result.Type.Should().Be(AccountType.Enterprise);
    result.Type.Value.Should().Be("enterprise");
  }

  /// <summary>U18: Verifies AccountType extensible enum preserves unknown values.</summary>
  [Fact]
  public async Task GetAccountAsync_DeserializesUnknownAccountType()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""abc123"",
        ""name"": ""Future Account"",
        ""type"": ""future_premium_type"",
        ""created_on"": ""2024-01-15T10:30:00Z""
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.GetAccountAsync("abc123");

    // Assert
    result.Type.Value.Should().Be("future_premium_type");
    // Extensible enum should not be equal to known types
    result.Type.Should().NotBe(AccountType.Standard);
    result.Type.Should().NotBe(AccountType.Enterprise);
  }

  /// <summary>U19: Verifies DeleteAccountResult deserializes correctly.</summary>
  [Fact]
  public async Task DeleteAccountAsync_DeserializesResult()
  {
    // Arrange
    var targetAccountId = "deleted-account-123";
    var jsonResponse = $@"{{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {{
        ""id"": ""{targetAccountId}""
      }}
    }}";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.DeleteAccountAsync(targetAccountId);

    // Assert
    result.Id.Should().Be(targetAccountId);
  }

  /// <summary>U20: Verifies created_on DateTime is parsed correctly.</summary>
  [Fact]
  public async Task GetAccountAsync_ParsesDateTime()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""abc123"",
        ""name"": ""Test Account"",
        ""type"": ""standard"",
        ""created_on"": ""2023-06-15T14:45:30.123Z""
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.GetAccountAsync("abc123");

    // Assert
    result.CreatedOn.Year.Should().Be(2023);
    result.CreatedOn.Month.Should().Be(6);
    result.CreatedOn.Day.Should().Be(15);
    result.CreatedOn.Hour.Should().Be(14);
    result.CreatedOn.Minute.Should().Be(45);
    result.CreatedOn.Second.Should().Be(30);
  }

  #endregion


  #region Account Management - Pagination Tests (U21-U23)

  /// <summary>U21: Verifies ListAllAccountsAsync makes single request for single page.</summary>
  [Fact]
  public async Task ListAllAccountsAsync_SinglePage_MakesSingleRequest()
  {
    // Arrange
    var account = CreateTestAccount();
    var response = CreatePagePaginatedResponse(new[] { account }, page: 1, perPage: 20, totalPages: 1, totalCount: 1);
    var requestCount = 0;
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((_, _) => requestCount++)
      .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(response) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var results = new List<Account>();
    await foreach (var acc in sut.ListAllAccountsAsync())
      results.Add(acc);

    // Assert
    requestCount.Should().Be(1);
    results.Should().HaveCount(1);
    results[0].Id.Should().Be(account.Id);
  }

  /// <summary>U22: Verifies ListAllAccountsAsync makes multiple requests for multiple pages.</summary>
  [Fact]
  public async Task ListAllAccountsAsync_MultiplePages_MakesMultipleRequests()
  {
    // Arrange
    var account1 = CreateTestAccount("acc1", "Account 1");
    var account2 = CreateTestAccount("acc2", "Account 2");
    var account3 = CreateTestAccount("acc3", "Account 3");

    var responsePage1 = CreatePagePaginatedResponse(new[] { account1 }, page: 1, perPage: 1, totalPages: 3, totalCount: 3);
    var responsePage2 = CreatePagePaginatedResponse(new[] { account2 }, page: 2, perPage: 1, totalPages: 3, totalCount: 3);
    var responsePage3 = CreatePagePaginatedResponse(new[] { account3 }, page: 3, perPage: 1, totalPages: 3, totalCount: 3);

    var capturedRequests = new List<HttpRequestMessage>();
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequests.Add(req))
      .Returns((HttpRequestMessage req, CancellationToken _) =>
      {
        var query = req.RequestUri?.Query ?? "";
        if (query.Contains("page=3"))
          return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage3) });
        if (query.Contains("page=2"))
          return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage2) });
        return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage1) });
      });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var results = new List<Account>();
    await foreach (var acc in sut.ListAllAccountsAsync(new ListAccountsFilters(PerPage: 1)))
      results.Add(acc);

    // Assert
    capturedRequests.Should().HaveCount(3);
    results.Should().HaveCount(3);
    results.Select(a => a.Name).Should().ContainInOrder("Account 1", "Account 2", "Account 3");
  }

  /// <summary>U23: Verifies ListAllAccountsAsync handles empty result.</summary>
  [Fact]
  public async Task ListAllAccountsAsync_EmptyResult_YieldsNothing()
  {
    // Arrange
    var response = CreatePagePaginatedResponse(Array.Empty<Account>(), page: 1, perPage: 20, totalPages: 0, totalCount: 0);
    var requestCount = 0;
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((_, _) => requestCount++)
      .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(response) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var results = new List<Account>();
    await foreach (var acc in sut.ListAllAccountsAsync())
      results.Add(acc);

    // Assert
    requestCount.Should().Be(1);
    results.Should().BeEmpty();
  }

  #endregion


  #region Account Management - Error Handling Tests (U24-U31)

  /// <summary>U24: Verifies GetAccountAsync throws on 404 Not Found.</summary>
  [Fact]
  public async Task GetAccountAsync_WhenNotFound_ThrowsHttpRequestException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 7003, ""message"": ""Could not route to /accounts/nonexistent, perhaps your object identifier is invalid?"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.NotFound);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetAccountAsync("nonexistent"));
    exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  /// <summary>U25: Verifies API error envelope throws CloudflareApiException.</summary>
  [Fact]
  public async Task GetAccountAsync_WhenApiError_ThrowsCloudflareApiException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 6003, ""message"": ""Invalid request headers"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(() => sut.GetAccountAsync("abc123"));
    exception.Errors.Should().HaveCount(1);
    exception.Errors[0].Code.Should().Be(6003);
    exception.Errors[0].Message.Should().Be("Invalid request headers");
  }

  /// <summary>U26: Verifies multiple API errors are captured in exception.</summary>
  [Fact]
  public async Task GetAccountAsync_WhenMultipleApiErrors_CapturesAllErrors()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [
        { ""code"": 6003, ""message"": ""Invalid request headers"" },
        { ""code"": 6007, ""message"": ""Account identifier is invalid"" }
      ],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(() => sut.GetAccountAsync("abc123"));
    exception.Errors.Should().HaveCount(2);
    exception.Errors.Select(e => e.Code).Should().Contain(new[] { 6003, 6007 });
  }

  /// <summary>U27: Verifies CreateAccountAsync throws on permission error.</summary>
  [Fact]
  public async Task CreateAccountAsync_WhenUnauthorized_ThrowsCloudflareApiException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10000, ""message"": ""Authentication error"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.Forbidden);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<HttpRequestException>(() => sut.CreateAccountAsync(new CreateAccountRequest("Test")));
  }

  /// <summary>U28: Verifies DeleteAccountAsync throws on permission error.</summary>
  [Fact]
  public async Task DeleteAccountAsync_WhenUnauthorized_ThrowsCloudflareApiException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10000, ""message"": ""Authentication error"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.Forbidden);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<HttpRequestException>(() => sut.DeleteAccountAsync("abc123"));
  }

  /// <summary>U29: Verifies unauthorized (401) throws HttpRequestException.</summary>
  [Fact]
  public async Task GetAccountAsync_WhenUnauthorized401_ThrowsHttpRequestException()
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
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetAccountAsync("abc123"));
    exception.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>U30: Verifies rate limited (429) throws HttpRequestException.</summary>
  [Fact]
  public async Task ListAccountsAsync_WhenRateLimited_ThrowsHttpRequestException()
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
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.ListAccountsAsync());
    exception.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
  }

  /// <summary>U31: Verifies server error (500) throws HttpRequestException.</summary>
  [Fact]
  public async Task UpdateAccountAsync_WhenServerError_ThrowsHttpRequestException()
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
    var options    = Options.Create(new CloudflareApiOptions { AccountId = "test-account-id" });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(
      () => sut.UpdateAccountAsync("abc123", new UpdateAccountRequest("Test")));
    exception.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
  }

  #endregion


  #region Account Management - Helper Methods

  /// <summary>Creates a test Account instance with default or custom values.</summary>
  private static Account CreateTestAccount(
    string? id = null,
    string? name = null,
    AccountType? type = null,
    AccountManagedBy? managedBy = null,
    AccountSettings? settings = null)
  {
    return new Account(
      id ?? "test-account-12345",
      name ?? "Test Account",
      type ?? AccountType.Standard,
      DateTime.UtcNow,
      managedBy,
      settings
    );
  }

  /// <summary>Creates a page-paginated JSON response for accounts.</summary>
  private static string CreatePagePaginatedResponse(
    IEnumerable<Account> accounts,
    int page = 1,
    int perPage = 20,
    int? totalPages = null,
    int? totalCount = null)
  {
    var accountList = accounts.ToList();
    var tp = totalPages ?? (accountList.Count > 0 ? 1 : 0);
    var tc = totalCount ?? accountList.Count;

    return JsonSerializer.Serialize(
      new
      {
        success = true,
        errors = Array.Empty<object>(),
        messages = Array.Empty<object>(),
        result = accountList,
        result_info = new
        {
          page,
          per_page = perPage,
          count = accountList.Count,
          total_pages = tp,
          total_count = tc
        }
      },
      new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
  }

  #endregion
}
