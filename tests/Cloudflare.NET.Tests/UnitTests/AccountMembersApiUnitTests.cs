namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using Cloudflare.NET.Core.Exceptions;
using Cloudflare.NET.Roles.Models;
using Members;
using Members.Models;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Cloudflare.NET.Security.Firewall.Models;
using Shared.Fixtures;
using Xunit.Abstractions;


/// <summary>Contains unit tests for the <see cref="MembersApi"/> class.</summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class AccountMembersApiUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly ILoggerFactory _loggerFactory;

  #endregion


  #region Constructors

  public AccountMembersApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion


  #region Request Construction Tests (U01-U08)

  /// <summary>U01: Verifies that ListAccountMembersAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task ListAccountMembersAsync_NoFilters_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var successResponse = CreatePagePaginatedResponse(Array.Empty<AccountMember>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAccountMembersAsync(accountId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/members");
    capturedRequest.RequestUri.Query.Should().BeEmpty();
  }

  /// <summary>U02: Verifies that ListAccountMembersAsync includes pagination parameters.</summary>
  [Fact]
  public async Task ListAccountMembersAsync_WithPagination_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var filters = new ListAccountMembersFilters(Page: 2, PerPage: 10);
    var successResponse = CreatePagePaginatedResponse(Array.Empty<AccountMember>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAccountMembersAsync(accountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("page=2");
    capturedRequest.RequestUri.Query.Should().Contain("per_page=10");
  }

  /// <summary>U03: Verifies that ListAccountMembersAsync includes status filter.</summary>
  [Fact]
  public async Task ListAccountMembersAsync_WithStatusFilter_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var filters = new ListAccountMembersFilters(Status: MemberStatus.Accepted);
    var successResponse = CreatePagePaginatedResponse(Array.Empty<AccountMember>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAccountMembersAsync(accountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("status=accepted");
  }

  /// <summary>U04: Verifies that ListAccountMembersAsync includes direction and order filters.</summary>
  [Fact]
  public async Task ListAccountMembersAsync_WithDirectionAndOrder_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var filters = new ListAccountMembersFilters(Direction: ListOrderDirection.Ascending, Order: MemberOrderField.UserEmail);
    var successResponse = CreatePagePaginatedResponse(Array.Empty<AccountMember>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAccountMembersAsync(accountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("direction=asc");
    capturedRequest.RequestUri.Query.Should().Contain("order=user.email");
  }

  /// <summary>U05: Verifies that GetAccountMemberAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task GetAccountMemberAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var memberId = "member-123";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestMember(memberId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountMemberAsync(accountId, memberId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/members/{memberId}");
  }

  /// <summary>U06: Verifies that CreateAccountMemberAsync sends a POST request with correct body.</summary>
  [Fact]
  public async Task CreateAccountMemberAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var request = new CreateAccountMemberRequest(
      Email: "test@example.com",
      Roles: new[] { "role-1", "role-2" }
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestMember("member-new"));
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, async (req, _) =>
    {
      capturedRequest = req;
      if (req.Content is not null)
        capturedBody = await req.Content.ReadAsStringAsync();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateAccountMemberAsync(accountId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/members");
    capturedBody.Should().NotBeNullOrEmpty();
    capturedBody.Should().Contain("test@example.com");
    capturedBody.Should().Contain("role-1");
    capturedBody.Should().Contain("role-2");
  }

  /// <summary>U07: Verifies that UpdateAccountMemberAsync sends a PUT request with correct body.</summary>
  [Fact]
  public async Task UpdateAccountMemberAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var memberId = "member-123";
    var request = new UpdateAccountMemberRequest(
      Roles: new[] { "role-updated" }
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestMember(memberId));
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, async (req, _) =>
    {
      capturedRequest = req;
      if (req.Content is not null)
        capturedBody = await req.Content.ReadAsStringAsync();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateAccountMemberAsync(accountId, memberId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/members/{memberId}");
    capturedBody.Should().NotBeNullOrEmpty();
    capturedBody.Should().Contain("role-updated");
  }

  /// <summary>U08: Verifies that DeleteAccountMemberAsync sends a DELETE request to the correct endpoint.</summary>
  [Fact]
  public async Task DeleteAccountMemberAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var memberId = "member-123";
    var successResponse = HttpFixtures.CreateSuccessResponse(new { id = memberId });
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    await sut.DeleteAccountMemberAsync(accountId, memberId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/members/{memberId}");
  }

  #endregion


  #region Response Deserialization Tests (U09-U18)

  /// <summary>U09: Verifies that AccountMember model deserializes all properties correctly.</summary>
  [Fact]
  public async Task GetAccountMemberAsync_DeserializesFullModel()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""member-123"",
        ""status"": ""accepted"",
        ""user"": {
          ""id"": ""user-456"",
          ""email"": ""user@example.com"",
          ""first_name"": ""John"",
          ""last_name"": ""Doe"",
          ""two_factor_authentication_enabled"": true
        },
        ""roles"": [
          {
            ""id"": ""role-admin"",
            ""name"": ""Administrator"",
            ""description"": ""Administrative access"",
            ""permissions"": {
              ""zones"": { ""read"": true, ""write"": true }
            }
          }
        ]
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountMemberAsync("acc123", "member-123");

    // Assert
    result.Id.Should().Be("member-123");
    result.Status.Should().Be(MemberStatus.Accepted);
    result.User.Should().NotBeNull();
    result.User.Id.Should().Be("user-456");
    result.User.Email.Should().Be("user@example.com");
    result.User.FirstName.Should().Be("John");
    result.User.LastName.Should().Be("Doe");
    result.User.TwoFactorAuthenticationEnabled.Should().BeTrue();
    result.Roles.Should().HaveCount(1);
    result.Roles[0].Id.Should().Be("role-admin");
    result.Roles[0].Name.Should().Be("Administrator");
  }

  /// <summary>U10: Verifies that MemberStatus extensible enum deserializes correctly.</summary>
  [Theory]
  [InlineData("accepted", true)]
  [InlineData("pending", true)]
  [InlineData("rejected", true)]
  [InlineData("custom_status", true)]
  public async Task GetAccountMemberAsync_DeserializesMemberStatus(string statusValue, bool shouldSucceed)
  {
    // Arrange
    var jsonResponse = $@"{{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {{
        ""id"": ""member-1"",
        ""status"": ""{statusValue}"",
        ""user"": {{
          ""id"": ""user-1"",
          ""email"": ""test@example.com""
        }},
        ""roles"": []
      }}
    }}";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountMemberAsync("acc", "member-1");

    // Assert
    if (shouldSucceed)
    {
      result.Status.Value.Should().Be(statusValue);
    }
  }

  /// <summary>U11: Verifies that MemberUser with minimal fields deserializes correctly.</summary>
  [Fact]
  public async Task GetAccountMemberAsync_DeserializesMinimalUser()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""member-1"",
        ""status"": ""pending"",
        ""user"": {
          ""id"": ""user-1"",
          ""email"": ""pending@example.com""
        },
        ""roles"": []
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountMemberAsync("acc", "member-1");

    // Assert
    result.User.Id.Should().Be("user-1");
    result.User.Email.Should().Be("pending@example.com");
    result.User.FirstName.Should().BeNull();
    result.User.LastName.Should().BeNull();
    result.User.TwoFactorAuthenticationEnabled.Should().BeFalse();
  }

  /// <summary>U12: Verifies that AccountMember with policies deserializes correctly.</summary>
  [Fact]
  public async Task GetAccountMemberAsync_DeserializesWithPolicies()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""member-1"",
        ""status"": ""accepted"",
        ""user"": {
          ""id"": ""user-1"",
          ""email"": ""policy@example.com""
        },
        ""roles"": [],
        ""policies"": [
          {
            ""id"": ""policy-1"",
            ""access"": ""allow"",
            ""permission_groups"": [
              { ""id"": ""perm-group-1"" }
            ],
            ""resource_groups"": [
              { ""id"": ""resource-group-1"" }
            ]
          }
        ]
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountMemberAsync("acc", "member-1");

    // Assert
    result.Policies.Should().NotBeNull();
    result.Policies.Should().HaveCount(1);
    result.Policies![0].Id.Should().Be("policy-1");
    result.Policies[0].Access.Should().Be("allow");
    result.Policies[0].PermissionGroups.Should().HaveCount(1);
    result.Policies[0].PermissionGroups[0].Id.Should().Be("perm-group-1");
    result.Policies[0].ResourceGroups.Should().HaveCount(1);
    result.Policies[0].ResourceGroups[0].Id.Should().Be("resource-group-1");
  }

  /// <summary>U13: Verifies that AccountMember with multiple roles deserializes correctly.</summary>
  [Fact]
  public async Task GetAccountMemberAsync_DeserializesMultipleRoles()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""member-1"",
        ""status"": ""accepted"",
        ""user"": {
          ""id"": ""user-1"",
          ""email"": ""multi@example.com""
        },
        ""roles"": [
          {
            ""id"": ""role-1"",
            ""name"": ""DNS Admin"",
            ""description"": ""DNS management"",
            ""permissions"": { ""dns"": { ""read"": true, ""write"": true } }
          },
          {
            ""id"": ""role-2"",
            ""name"": ""Analytics Viewer"",
            ""description"": ""View analytics"",
            ""permissions"": { ""analytics"": { ""read"": true, ""write"": false } }
          }
        ]
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAccountMemberAsync("acc", "member-1");

    // Assert
    result.Roles.Should().HaveCount(2);
    result.Roles[0].Id.Should().Be("role-1");
    result.Roles[0].Name.Should().Be("DNS Admin");
    result.Roles[1].Id.Should().Be("role-2");
    result.Roles[1].Name.Should().Be("Analytics Viewer");
  }

  /// <summary>U14: Verifies that DeleteAccountMemberResult deserializes correctly.</summary>
  [Fact]
  public async Task DeleteAccountMemberAsync_DeserializesResult()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""member-deleted""
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.DeleteAccountMemberAsync("acc", "member-deleted");

    // Assert
    result.Id.Should().Be("member-deleted");
  }

  /// <summary>U15: Verifies that CreateAccountMemberAsync response deserializes correctly.</summary>
  [Fact]
  public async Task CreateAccountMemberAsync_DeserializesCreatedMember()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""member-new"",
        ""status"": ""pending"",
        ""user"": {
          ""id"": ""user-new"",
          ""email"": ""new@example.com""
        },
        ""roles"": [
          {
            ""id"": ""role-1"",
            ""name"": ""Viewer"",
            ""description"": ""Read-only access"",
            ""permissions"": {}
          }
        ]
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    var request = new CreateAccountMemberRequest(
      Email: "new@example.com",
      Roles: new[] { "role-1" }
    );

    // Act
    var result = await sut.CreateAccountMemberAsync("acc", request);

    // Assert
    result.Id.Should().Be("member-new");
    result.Status.Should().Be(MemberStatus.Pending);
    result.User.Email.Should().Be("new@example.com");
    result.Roles.Should().HaveCount(1);
  }

  /// <summary>U16: Verifies that UpdateAccountMemberAsync response deserializes correctly.</summary>
  [Fact]
  public async Task UpdateAccountMemberAsync_DeserializesUpdatedMember()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""member-updated"",
        ""status"": ""accepted"",
        ""user"": {
          ""id"": ""user-1"",
          ""email"": ""updated@example.com""
        },
        ""roles"": [
          {
            ""id"": ""role-new"",
            ""name"": ""New Role"",
            ""description"": ""Updated role"",
            ""permissions"": {}
          }
        ]
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    var request = new UpdateAccountMemberRequest(Roles: new[] { "role-new" });

    // Act
    var result = await sut.UpdateAccountMemberAsync("acc", "member-updated", request);

    // Assert
    result.Id.Should().Be("member-updated");
    result.Roles.Should().HaveCount(1);
    result.Roles[0].Id.Should().Be("role-new");
  }

  /// <summary>U17: Verifies that CreateAccountMemberRequest serializes policies correctly.</summary>
  [Fact]
  public async Task CreateAccountMemberAsync_SerializesPolicies()
  {
    // Arrange
    var request = new CreateAccountMemberRequest(
      Email: "policy@example.com",
      Roles: new[] { "role-1" },
      Policies: new[]
      {
        new CreateMemberPolicyRequest(
          Access: "allow",
          PermissionGroups: new[] { new MemberPermissionGroupReference("perm-1") },
          ResourceGroups: new[] { new MemberResourceGroupReference("res-1") }
        )
      }
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestMember("member-new"));
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, async (req, _) =>
    {
      if (req.Content is not null)
        capturedBody = await req.Content.ReadAsStringAsync();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateAccountMemberAsync("acc", request);

    // Assert
    capturedBody.Should().NotBeNullOrEmpty();
    capturedBody.Should().Contain("policies");
    capturedBody.Should().Contain("allow");
    capturedBody.Should().Contain("perm-1");
    capturedBody.Should().Contain("res-1");
  }

  /// <summary>U18: Verifies that optional status in CreateAccountMemberRequest serializes correctly.</summary>
  [Fact]
  public async Task CreateAccountMemberAsync_SerializesOptionalStatus()
  {
    // Arrange
    var request = new CreateAccountMemberRequest(
      Email: "status@example.com",
      Roles: new[] { "role-1" },
      Status: MemberStatus.Accepted
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestMember("member-new"));
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, async (req, _) =>
    {
      if (req.Content is not null)
        capturedBody = await req.Content.ReadAsStringAsync();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateAccountMemberAsync("acc", request);

    // Assert
    capturedBody.Should().NotBeNullOrEmpty();
    capturedBody.Should().Contain("status");
    capturedBody.Should().Contain("accepted");
  }

  #endregion


  #region Pagination Tests (U19-U20)

  /// <summary>U19: Verifies ListAllAccountMembersAsync makes single request for single page.</summary>
  [Fact]
  public async Task ListAllAccountMembersAsync_SinglePage_MakesSingleRequest()
  {
    // Arrange
    var member = CreateTestMember("member-1");
    var response = CreatePagePaginatedResponse(new[] { member }, page: 1, perPage: 20, totalPages: 1, totalCount: 1);
    var requestCount = 0;
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((_, _) => requestCount++)
      .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(response) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    var results = new List<AccountMember>();
    await foreach (var m in sut.ListAllAccountMembersAsync("acc-123"))
      results.Add(m);

    // Assert
    requestCount.Should().Be(1);
    results.Should().HaveCount(1);
  }

  /// <summary>U20: Verifies ListAllAccountMembersAsync makes multiple requests for multiple pages.</summary>
  [Fact]
  public async Task ListAllAccountMembersAsync_MultiplePages_MakesMultipleRequests()
  {
    // Arrange
    var member1 = CreateTestMember("member-1");
    var member2 = CreateTestMember("member-2");

    var responsePage1 = CreatePagePaginatedResponse(new[] { member1 }, page: 1, perPage: 1, totalPages: 2, totalCount: 2);
    var responsePage2 = CreatePagePaginatedResponse(new[] { member2 }, page: 2, perPage: 1, totalPages: 2, totalCount: 2);

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
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    var results = new List<AccountMember>();
    await foreach (var m in sut.ListAllAccountMembersAsync("acc-123", new ListAccountMembersFilters(PerPage: 1)))
      results.Add(m);

    // Assert
    capturedRequests.Should().HaveCount(2);
    results.Should().HaveCount(2);
    results.Select(m => m.Id).Should().ContainInOrder("member-1", "member-2");
  }

  #endregion


  #region Error Handling Tests (U21-U28)

  /// <summary>U21: Verifies GetAccountMemberAsync throws on 404 Not Found.</summary>
  [Fact]
  public async Task GetAccountMemberAsync_WhenNotFound_ThrowsHttpRequestException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10000, ""message"": ""Member not found"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.NotFound);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetAccountMemberAsync("acc", "nonexistent"));
    exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  /// <summary>U22: Verifies API error envelope throws CloudflareApiException.</summary>
  [Fact]
  public async Task ListAccountMembersAsync_WhenApiError_ThrowsCloudflareApiException()
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
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(() => sut.ListAccountMembersAsync("acc"));
    exception.Errors.Should().HaveCount(1);
    exception.Errors[0].Code.Should().Be(10000);
  }

  /// <summary>U23: Verifies multiple errors in response are captured in CloudflareApiException.</summary>
  [Fact]
  public async Task ListAccountMembersAsync_WhenMultipleErrors_CapturesAllErrors()
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
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(() => sut.ListAccountMembersAsync("acc"));
    exception.Errors.Should().HaveCount(2);
  }

  /// <summary>U24: Verifies unauthorized (401) throws HttpRequestException.</summary>
  [Fact]
  public async Task ListAccountMembersAsync_WhenUnauthorized_ThrowsHttpRequestException()
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
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.ListAccountMembersAsync("acc"));
    exception.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>U25: Verifies forbidden (403) throws HttpRequestException.</summary>
  [Fact]
  public async Task CreateAccountMemberAsync_WhenForbidden_ThrowsHttpRequestException()
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
    var sut = new MembersApi(httpClient, _loggerFactory);

    var request = new CreateAccountMemberRequest(Email: "test@example.com", Roles: new[] { "role-1" });

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.CreateAccountMemberAsync("acc", request));
    exception.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }

  /// <summary>U26: Verifies rate limited (429) throws HttpRequestException.</summary>
  [Fact]
  public async Task ListAccountMembersAsync_WhenRateLimited_ThrowsHttpRequestException()
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
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.ListAccountMembersAsync("acc"));
    exception.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
  }

  /// <summary>U27: Verifies server error (500/502/503) throws HttpRequestException.</summary>
  [Theory]
  [InlineData(HttpStatusCode.InternalServerError)]
  [InlineData(HttpStatusCode.BadGateway)]
  [InlineData(HttpStatusCode.ServiceUnavailable)]
  public async Task ListAccountMembersAsync_WhenServerError_ThrowsHttpRequestException(HttpStatusCode statusCode)
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10000, ""message"": ""Server error"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, statusCode);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sut.ListAccountMembersAsync("acc"));
    exception.StatusCode.Should().Be(statusCode);
  }

  /// <summary>U28: Verifies CreateAccountMemberAsync error for duplicate email.</summary>
  [Fact]
  public async Task CreateAccountMemberAsync_WhenDuplicateEmail_ThrowsCloudflareApiException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 1005, ""message"": ""Member already exists"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    var request = new CreateAccountMemberRequest(Email: "existing@example.com", Roles: new[] { "role-1" });

    // Act & Assert
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(() => sut.CreateAccountMemberAsync("acc", request));
    exception.Errors.Should().HaveCount(1);
    exception.Errors[0].Code.Should().Be(1005);
  }

  #endregion


  #region URL Encoding Tests (U29-U30)

  /// <summary>U29: Verifies that GetAccountMemberAsync properly URL-encodes the account ID.</summary>
  [Fact]
  public async Task GetAccountMemberAsync_WithSpecialChars_UrlEncodesAccountId()
  {
    // Arrange
    var accountId = "abc+def/ghi";
    var memberId = "member-123";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestMember(memberId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountMemberAsync(accountId, memberId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.AbsolutePath.Should().Contain("abc%2Bdef%2Fghi");
  }

  /// <summary>U30: Verifies that GetAccountMemberAsync properly URL-encodes the member ID.</summary>
  [Fact]
  public async Task GetAccountMemberAsync_WithSpecialChars_UrlEncodesMemberId()
  {
    // Arrange
    var accountId = "acc-123";
    var memberId = "member+with/special";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestMember(memberId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    await sut.GetAccountMemberAsync(accountId, memberId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.AbsolutePath.Should().Contain("member%2Bwith%2Fspecial");
  }

  #endregion


  #region Parameter Validation Tests (U31-U42)

  /// <summary>U31: Verifies that ListAccountMembersAsync throws on null accountId.</summary>
  [Fact]
  public async Task ListAccountMembersAsync_NullAccountId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.ListAccountMembersAsync(null!));
  }

  /// <summary>U32: Verifies that ListAccountMembersAsync throws on whitespace accountId.</summary>
  [Fact]
  public async Task ListAccountMembersAsync_WhitespaceAccountId_ThrowsArgumentException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() => sut.ListAccountMembersAsync("   "));
  }

  /// <summary>U33: Verifies that GetAccountMemberAsync throws on null accountId.</summary>
  [Fact]
  public async Task GetAccountMemberAsync_NullAccountId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetAccountMemberAsync(null!, "member-123"));
  }

  /// <summary>U34: Verifies that GetAccountMemberAsync throws on null memberId.</summary>
  [Fact]
  public async Task GetAccountMemberAsync_NullMemberId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetAccountMemberAsync("acc-123", null!));
  }

  /// <summary>U35: Verifies that CreateAccountMemberAsync throws on null accountId.</summary>
  [Fact]
  public async Task CreateAccountMemberAsync_NullAccountId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    var request = new CreateAccountMemberRequest(Email: "test@example.com", Roles: new[] { "role-1" });

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CreateAccountMemberAsync(null!, request));
  }

  /// <summary>U36: Verifies that CreateAccountMemberAsync throws on null request.</summary>
  [Fact]
  public async Task CreateAccountMemberAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CreateAccountMemberAsync("acc-123", null!));
  }

  /// <summary>U37: Verifies that UpdateAccountMemberAsync throws on null accountId.</summary>
  [Fact]
  public async Task UpdateAccountMemberAsync_NullAccountId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    var request = new UpdateAccountMemberRequest(Roles: new[] { "role-1" });

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.UpdateAccountMemberAsync(null!, "member-123", request));
  }

  /// <summary>U38: Verifies that UpdateAccountMemberAsync throws on null memberId.</summary>
  [Fact]
  public async Task UpdateAccountMemberAsync_NullMemberId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    var request = new UpdateAccountMemberRequest(Roles: new[] { "role-1" });

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.UpdateAccountMemberAsync("acc-123", null!, request));
  }

  /// <summary>U39: Verifies that UpdateAccountMemberAsync throws on null request.</summary>
  [Fact]
  public async Task UpdateAccountMemberAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.UpdateAccountMemberAsync("acc-123", "member-123", null!));
  }

  /// <summary>U40: Verifies that DeleteAccountMemberAsync throws on null accountId.</summary>
  [Fact]
  public async Task DeleteAccountMemberAsync_NullAccountId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.DeleteAccountMemberAsync(null!, "member-123"));
  }

  /// <summary>U41: Verifies that DeleteAccountMemberAsync throws on null memberId.</summary>
  [Fact]
  public async Task DeleteAccountMemberAsync_NullMemberId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.DeleteAccountMemberAsync("acc-123", null!));
  }

  /// <summary>U42: Verifies that DeleteAccountMemberAsync throws on whitespace memberId.</summary>
  [Fact]
  public async Task DeleteAccountMemberAsync_WhitespaceMemberId_ThrowsArgumentException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() => sut.DeleteAccountMemberAsync("acc-123", "   "));
  }

  #endregion


  #region Filter Tests (U43-U46)

  /// <summary>U43: Verifies all MemberOrderField values are serialized correctly.</summary>
  [Theory]
  [InlineData(MemberOrderField.UserId, "user.id")]
  [InlineData(MemberOrderField.UserEmail, "user.email")]
  [InlineData(MemberOrderField.UserFirstName, "user.first_name")]
  [InlineData(MemberOrderField.UserLastName, "user.last_name")]
  [InlineData(MemberOrderField.Status, "status")]
  public async Task ListAccountMembersAsync_WithOrderField_SerializesCorrectly(MemberOrderField orderField, string expected)
  {
    // Arrange
    var accountId = "test-account-id";
    var filters = new ListAccountMembersFilters(Order: orderField);
    var successResponse = CreatePagePaginatedResponse(Array.Empty<AccountMember>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAccountMembersAsync(accountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain($"order={Uri.EscapeDataString(expected)}");
  }

  /// <summary>U44: Verifies MemberStatus filter values are serialized correctly.</summary>
  [Theory]
  [InlineData("accepted")]
  [InlineData("pending")]
  [InlineData("rejected")]
  public async Task ListAccountMembersAsync_WithStatusFilter_SerializesCorrectly(string statusValue)
  {
    // Arrange
    var accountId = "test-account-id";
    var status = new MemberStatus(statusValue);
    var filters = new ListAccountMembersFilters(Status: status);
    var successResponse = CreatePagePaginatedResponse(Array.Empty<AccountMember>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAccountMembersAsync(accountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain($"status={statusValue}");
  }

  /// <summary>U45: Verifies ListOrderDirection filter values are serialized correctly.</summary>
  [Theory]
  [InlineData(ListOrderDirection.Ascending, "asc")]
  [InlineData(ListOrderDirection.Descending, "desc")]
  public async Task ListAccountMembersAsync_WithDirection_SerializesCorrectly(ListOrderDirection direction, string expected)
  {
    // Arrange
    var accountId = "test-account-id";
    var filters = new ListAccountMembersFilters(Direction: direction);
    var successResponse = CreatePagePaginatedResponse(Array.Empty<AccountMember>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAccountMembersAsync(accountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain($"direction={expected}");
  }

  /// <summary>U46: Verifies combined filters are all included in request.</summary>
  [Fact]
  public async Task ListAccountMembersAsync_WithAllFilters_IncludesAllParameters()
  {
    // Arrange
    var accountId = "test-account-id";
    var filters = new ListAccountMembersFilters(
      Status: MemberStatus.Accepted,
      Page: 3,
      PerPage: 25,
      Direction: ListOrderDirection.Descending,
      Order: MemberOrderField.UserEmail
    );
    var successResponse = CreatePagePaginatedResponse(Array.Empty<AccountMember>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new MembersApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAccountMembersAsync(accountId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    var query = capturedRequest!.RequestUri!.Query;
    query.Should().Contain("status=accepted");
    query.Should().Contain("page=3");
    query.Should().Contain("per_page=25");
    query.Should().Contain("direction=desc");
    query.Should().Contain("order=user.email");
  }

  #endregion


  #region Helper Methods

  /// <summary>Creates a test AccountMember instance with default or custom values.</summary>
  private static AccountMember CreateTestMember(string? id = null, string? email = null)
  {
    return new AccountMember(
      Id: id ?? "test-member-123",
      Status: MemberStatus.Accepted,
      User: new MemberUser(
        Id: "user-456",
        Email: email ?? "test@example.com",
        FirstName: "Test",
        LastName: "User"
      ),
      Roles: new[]
      {
        new AccountRole(
          "role-1",
          "Test Role",
          "Test role description",
          new RolePermissions(Dns: new PermissionGrant(true, true))
        )
      }
    );
  }

  /// <summary>Creates a page-paginated JSON response for account members.</summary>
  private static string CreatePagePaginatedResponse(
    IEnumerable<AccountMember> items,
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
