namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using Cloudflare.NET.Core.Exceptions;
using Cloudflare.NET.Members.Models;
using Cloudflare.NET.Roles.Models;
using Cloudflare.NET.User;
using Cloudflare.NET.User.Models;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>Contains unit tests for the <see cref="UserApi" /> class implementing F14 - User Management.</summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class UserApiUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly ILoggerFactory _loggerFactory;
  private readonly JsonSerializerOptions _serializerOptions =
    new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

  #endregion


  #region Constructors

  public UserApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion


  #region Request Construction Tests (U01-U05)

  /// <summary>U01: Verifies that GetUserAsync sends a GET request to /user.</summary>
  [Fact]
  public async Task GetUserAsync_SendsCorrectGetRequest()
  {
    // Arrange
    var expectedUser = CreateTestUser();
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedUser);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetUserAsync();

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(expectedUser.Id);
    result.Email.Should().Be(expectedUser.Email);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be("https://api.cloudflare.com/client/v4/user");
  }

  /// <summary>U02: Verifies that EditUserAsync with all fields sends a PATCH request with all fields in JSON body.</summary>
  [Fact]
  public async Task EditUserAsync_WithAllFields_SendsCorrectPatchRequest()
  {
    // Arrange
    var request = new EditUserRequest(
      FirstName: "John",
      LastName: "Doe",
      Country: "US",
      Telephone: "+1-555-555-5555",
      Zipcode: "12345"
    );
    var expectedUser = CreateTestUser(
      firstName: "John",
      lastName: "Doe",
      country: "US",
      telephone: "+1-555-555-5555",
      zipcode: "12345"
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedUser);
    HttpRequestMessage? capturedRequest = null;
    string? capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.EditUserAsync(request);

    // Assert
    result.Should().NotBeNull();
    result.FirstName.Should().Be("John");
    result.LastName.Should().Be("Doe");
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Patch);
    capturedRequest.RequestUri!.ToString().Should().Be("https://api.cloudflare.com/client/v4/user");

    // Verify JSON body contains all fields
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("first_name").GetString().Should().Be("John");
    doc.RootElement.GetProperty("last_name").GetString().Should().Be("Doe");
    doc.RootElement.GetProperty("country").GetString().Should().Be("US");
    doc.RootElement.GetProperty("telephone").GetString().Should().Be("+1-555-555-5555");
    doc.RootElement.GetProperty("zipcode").GetString().Should().Be("12345");
  }

  /// <summary>U03: Verifies that EditUserAsync with only FirstName sends a PATCH request with only first_name in JSON body.</summary>
  [Fact]
  public async Task EditUserAsync_WithFirstNameOnly_SendsOnlyFirstNameInBody()
  {
    // Arrange
    var request = new EditUserRequest(FirstName: "Jane");
    var expectedUser = CreateTestUser(firstName: "Jane");
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedUser);
    HttpRequestMessage? capturedRequest = null;
    string? capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.EditUserAsync(request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Patch);
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("first_name").GetString().Should().Be("Jane");

    // Verify other fields are not present (null omission)
    doc.RootElement.TryGetProperty("last_name", out _).Should().BeFalse();
    doc.RootElement.TryGetProperty("country", out _).Should().BeFalse();
    doc.RootElement.TryGetProperty("telephone", out _).Should().BeFalse();
    doc.RootElement.TryGetProperty("zipcode", out _).Should().BeFalse();
  }

  /// <summary>U04: Verifies that EditUserAsync with only Country sends a PATCH request with only country in JSON body.</summary>
  [Fact]
  public async Task EditUserAsync_WithCountryOnly_SendsOnlyCountryInBody()
  {
    // Arrange
    var request = new EditUserRequest(Country: "GB");
    var expectedUser = CreateTestUser(country: "GB");
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedUser);
    HttpRequestMessage? capturedRequest = null;
    string? capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.EditUserAsync(request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Patch);
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("country").GetString().Should().Be("GB");

    // Verify other fields are not present (null omission)
    doc.RootElement.TryGetProperty("first_name", out _).Should().BeFalse();
    doc.RootElement.TryGetProperty("last_name", out _).Should().BeFalse();
    doc.RootElement.TryGetProperty("telephone", out _).Should().BeFalse();
    doc.RootElement.TryGetProperty("zipcode", out _).Should().BeFalse();
  }

  /// <summary>U05: Verifies that EditUserRequest with only FirstName does not include null fields in JSON.</summary>
  [Fact]
  public async Task EditUserAsync_NullFieldsOmitted_SerializesCorrectly()
  {
    // Arrange
    var request = new EditUserRequest(FirstName: "Test");
    var expectedUser = CreateTestUser(firstName: "Test");
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedUser);
    string? capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    await sut.EditUserAsync(request);

    // Assert
    capturedJsonBody.Should().NotBeNull();
    // Only first_name should be in the JSON, no other fields
    capturedJsonBody.Should().Contain("first_name");
    capturedJsonBody.Should().NotContain("last_name");
    capturedJsonBody.Should().NotContain("country");
    capturedJsonBody.Should().NotContain("telephone");
    capturedJsonBody.Should().NotContain("zipcode");
  }

  #endregion


  #region Response Deserialization Tests (U06-U13)

  /// <summary>U06: Verifies that User model deserializes all properties correctly from a complete JSON response.</summary>
  [Fact]
  public async Task GetUserAsync_FullModel_DeserializesAllProperties()
  {
    // Arrange
    var userJson = """
      {
        "id": "7c5dae5552338874e5053f2534d2767a",
        "email": "user@example.com",
        "first_name": "John",
        "last_name": "Doe",
        "telephone": "+1-555-555-5555",
        "country": "US",
        "zipcode": "12345",
        "suspended": false,
        "two_factor_authentication_enabled": true,
        "two_factor_authentication_locked": false,
        "has_pro_zones": true,
        "has_business_zones": false,
        "has_enterprise_zones": true,
        "betas": ["speed_brain", "rulesets"],
        "organizations": [
          {
            "id": "org-123",
            "name": "Test Org",
            "status": "active",
            "permissions": ["read", "write"],
            "roles": ["admin"]
          }
        ],
        "created_on": "2020-01-01T00:00:00Z",
        "modified_on": "2024-06-15T12:30:00Z"
      }
      """;
    var successResponse = CreateSuccessResponseRaw(userJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetUserAsync();

    // Assert
    result.Id.Should().Be("7c5dae5552338874e5053f2534d2767a");
    result.Email.Should().Be("user@example.com");
    result.FirstName.Should().Be("John");
    result.LastName.Should().Be("Doe");
    result.Telephone.Should().Be("+1-555-555-5555");
    result.Country.Should().Be("US");
    result.Zipcode.Should().Be("12345");
    result.Suspended.Should().BeFalse();
    result.TwoFactorAuthenticationEnabled.Should().BeTrue();
    result.TwoFactorAuthenticationLocked.Should().BeFalse();
    result.HasProZones.Should().BeTrue();
    result.HasBusinessZones.Should().BeFalse();
    result.HasEnterpriseZones.Should().BeTrue();
    result.Betas.Should().NotBeNull();
    result.Betas!.Should().HaveCount(2);
    result.Betas.Should().Contain("speed_brain");
    result.Organizations.Should().NotBeNull();
    result.Organizations!.Should().HaveCount(1);
    result.Organizations[0].Id.Should().Be("org-123");
    result.CreatedOn.Should().Be(DateTime.Parse("2020-01-01T00:00:00Z").ToUniversalTime());
    result.ModifiedOn.Should().Be(DateTime.Parse("2024-06-15T12:30:00Z").ToUniversalTime());
  }

  /// <summary>U07: Verifies that User model with missing optional fields has null for those properties.</summary>
  [Fact]
  public async Task GetUserAsync_OptionalFieldsNull_DeserializesWithNulls()
  {
    // Arrange - minimal user with only required fields
    var userJson = """
      {
        "id": "user-id-123",
        "email": "test@example.com",
        "suspended": false,
        "two_factor_authentication_enabled": false
      }
      """;
    var successResponse = CreateSuccessResponseRaw(userJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetUserAsync();

    // Assert
    result.Id.Should().Be("user-id-123");
    result.Email.Should().Be("test@example.com");
    result.FirstName.Should().BeNull();
    result.LastName.Should().BeNull();
    result.Telephone.Should().BeNull();
    result.Country.Should().BeNull();
    result.Zipcode.Should().BeNull();
    result.Betas.Should().BeNull();
    result.Organizations.Should().BeNull();
    result.CreatedOn.Should().BeNull();
    result.ModifiedOn.Should().BeNull();
  }

  /// <summary>U08: Verifies that User model with suspended=true deserializes correctly.</summary>
  [Fact]
  public async Task GetUserAsync_SuspendedTrue_DeserializesCorrectly()
  {
    // Arrange
    var userJson = """
      {
        "id": "suspended-user",
        "email": "suspended@example.com",
        "suspended": true,
        "two_factor_authentication_enabled": false
      }
      """;
    var successResponse = CreateSuccessResponseRaw(userJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetUserAsync();

    // Assert
    result.Suspended.Should().BeTrue();
  }

  /// <summary>U09: Verifies that User model with two_factor_authentication_enabled=true deserializes correctly.</summary>
  [Fact]
  public async Task GetUserAsync_TwoFactorEnabled_DeserializesCorrectly()
  {
    // Arrange
    var userJson = """
      {
        "id": "2fa-user",
        "email": "secure@example.com",
        "suspended": false,
        "two_factor_authentication_enabled": true,
        "two_factor_authentication_locked": true
      }
      """;
    var successResponse = CreateSuccessResponseRaw(userJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetUserAsync();

    // Assert
    result.TwoFactorAuthenticationEnabled.Should().BeTrue();
    result.TwoFactorAuthenticationLocked.Should().BeTrue();
  }

  /// <summary>U10: Verifies that User model with has_pro_zones=true deserializes correctly.</summary>
  [Fact]
  public async Task GetUserAsync_HasProZones_DeserializesCorrectly()
  {
    // Arrange
    var userJson = """
      {
        "id": "pro-user",
        "email": "pro@example.com",
        "suspended": false,
        "two_factor_authentication_enabled": false,
        "has_pro_zones": true,
        "has_business_zones": false,
        "has_enterprise_zones": false
      }
      """;
    var successResponse = CreateSuccessResponseRaw(userJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetUserAsync();

    // Assert
    result.HasProZones.Should().BeTrue();
    result.HasBusinessZones.Should().BeFalse();
    result.HasEnterpriseZones.Should().BeFalse();
  }

  /// <summary>U11: Verifies that User model with organizations array deserializes correctly.</summary>
  [Fact]
  public async Task GetUserAsync_WithOrganizations_DeserializesCorrectly()
  {
    // Arrange
    var userJson = """
      {
        "id": "org-user",
        "email": "org@example.com",
        "suspended": false,
        "two_factor_authentication_enabled": false,
        "organizations": [
          {
            "id": "org-1",
            "name": "Org One",
            "status": "active",
            "permissions": ["zone:read", "zone:write"],
            "roles": ["administrator", "editor"]
          },
          {
            "id": "org-2",
            "name": "Org Two",
            "status": "pending"
          }
        ]
      }
      """;
    var successResponse = CreateSuccessResponseRaw(userJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetUserAsync();

    // Assert
    result.Organizations.Should().NotBeNull();
    result.Organizations!.Should().HaveCount(2);

    var org1 = result.Organizations[0];
    org1.Id.Should().Be("org-1");
    org1.Name.Should().Be("Org One");
    org1.Status.Should().Be("active");
    org1.Permissions.Should().NotBeNull();
    org1.Permissions!.Should().HaveCount(2);
    org1.Roles.Should().NotBeNull();
    org1.Roles!.Should().HaveCount(2);

    var org2 = result.Organizations[1];
    org2.Id.Should().Be("org-2");
    org2.Name.Should().Be("Org Two");
    org2.Status.Should().Be("pending");
    org2.Permissions.Should().BeNull();
    org2.Roles.Should().BeNull();
  }

  /// <summary>U12: Verifies that User model with betas array deserializes correctly.</summary>
  [Fact]
  public async Task GetUserAsync_WithBetas_DeserializesCorrectly()
  {
    // Arrange
    var userJson = """
      {
        "id": "beta-user",
        "email": "beta@example.com",
        "suspended": false,
        "two_factor_authentication_enabled": false,
        "betas": ["speed_brain", "rulesets", "new_feature"]
      }
      """;
    var successResponse = CreateSuccessResponseRaw(userJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetUserAsync();

    // Assert
    result.Betas.Should().NotBeNull();
    result.Betas!.Should().HaveCount(3);
    result.Betas.Should().Contain("speed_brain");
    result.Betas.Should().Contain("rulesets");
    result.Betas.Should().Contain("new_feature");
  }

  /// <summary>U13: Verifies that UserOrganization model deserializes all properties correctly.</summary>
  [Fact]
  public async Task GetUserAsync_UserOrganization_DeserializesAllProperties()
  {
    // Arrange
    var userJson = """
      {
        "id": "user-with-org",
        "email": "test@example.com",
        "suspended": false,
        "two_factor_authentication_enabled": false,
        "organizations": [
          {
            "id": "org-full",
            "name": "Full Organization",
            "status": "member",
            "permissions": ["analytics:read", "dns:edit", "zone:read"],
            "roles": ["dns_admin", "viewer"]
          }
        ]
      }
      """;
    var successResponse = CreateSuccessResponseRaw(userJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetUserAsync();

    // Assert
    var org = result.Organizations![0];
    org.Id.Should().Be("org-full");
    org.Name.Should().Be("Full Organization");
    org.Status.Should().Be("member");
    org.Permissions.Should().ContainInOrder("analytics:read", "dns:edit", "zone:read");
    org.Roles.Should().ContainInOrder("dns_admin", "viewer");
  }

  #endregion


  #region Error Handling Tests (U14-U16)

  /// <summary>U14: Verifies that GetUserAsync throws CloudflareApiException when API returns success=false.</summary>
  [Fact]
  public async Task GetUserAsync_ApiError_ThrowsCloudflareApiException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(1001, "Invalid API token");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetUserAsync();

    // Assert
    var exception = await act.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().HaveCount(1);
    exception.Which.Errors[0].Code.Should().Be(1001);
    exception.Which.Errors[0].Message.Should().Be("Invalid API token");
  }

  /// <summary>U15: Verifies that EditUserAsync throws CloudflareApiException when country code is invalid.</summary>
  [Fact]
  public async Task EditUserAsync_InvalidCountryCode_ThrowsCloudflareApiException()
  {
    // Arrange
    var request = new EditUserRequest(Country: "XX");
    var errorResponse = HttpFixtures.CreateErrorResponse(1002, "Invalid country code");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.EditUserAsync(request);

    // Assert
    var exception = await act.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().HaveCount(1);
    exception.Which.Errors[0].Code.Should().Be(1002);
  }

  /// <summary>U16: Verifies that CloudflareApiException contains all errors when API returns multiple errors.</summary>
  [Fact]
  public async Task GetUserAsync_MultipleErrors_ThrowsCloudflareApiExceptionWithAllErrors()
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
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetUserAsync();

    // Assert
    var exception = await act.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().HaveCount(3);
    exception.Which.Errors[0].Code.Should().Be(1001);
    exception.Which.Errors[1].Code.Should().Be(1002);
    exception.Which.Errors[2].Code.Should().Be(1003);
  }

  #endregion


  #region F16 User Invitations - Request Construction Tests (U01-U04)

  /// <summary>U01: Verifies that ListInvitationsAsync sends a GET request to /user/invites.</summary>
  [Fact]
  public async Task ListInvitationsAsync_SendsCorrectGetRequest()
  {
    // Arrange
    var invitationsJson = """
      [
        {
          "id": "invite-001",
          "invited_member_email": "user@example.com",
          "status": "pending",
          "invited_on": "2024-01-15T10:00:00Z",
          "expires_on": "2024-01-22T10:00:00Z",
          "organization_name": "Test Account"
        }
      ]
      """;
    var successResponse = CreateSuccessResponseRaw(invitationsJson);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListInvitationsAsync();

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(1);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be("https://api.cloudflare.com/client/v4/user/invites");
  }

  /// <summary>U02: Verifies that GetInvitationAsync sends a GET request to /user/invites/{id}.</summary>
  [Fact]
  public async Task GetInvitationAsync_SendsCorrectGetRequest()
  {
    // Arrange
    var invitationId = "invite-12345";
    var invitationJson = """
      {
        "id": "invite-12345",
        "invited_member_email": "user@example.com",
        "status": "pending",
        "invited_on": "2024-01-15T10:00:00Z",
        "expires_on": "2024-01-22T10:00:00Z"
      }
      """;
    var successResponse = CreateSuccessResponseRaw(invitationJson);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetInvitationAsync(invitationId);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be("invite-12345");
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be("https://api.cloudflare.com/client/v4/user/invites/invite-12345");
  }

  /// <summary>U03: Verifies that RespondToInvitationAsync with Accept sends PATCH with {"status":"accepted"}.</summary>
  [Fact]
  public async Task RespondToInvitationAsync_AcceptInvitation_SendsCorrectPatchRequest()
  {
    // Arrange
    var invitationId = "invite-accept-001";
    var request = new RespondToInvitationRequest(MemberStatus.Accepted);
    var invitationJson = """
      {
        "id": "invite-accept-001",
        "invited_member_email": "user@example.com",
        "status": "accepted",
        "invited_on": "2024-01-15T10:00:00Z",
        "expires_on": "2024-01-22T10:00:00Z"
      }
      """;
    var successResponse = CreateSuccessResponseRaw(invitationJson);
    HttpRequestMessage? capturedRequest = null;
    string? capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.RespondToInvitationAsync(invitationId, request);

    // Assert
    result.Should().NotBeNull();
    result.Status.Should().Be(MemberStatus.Accepted);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Patch);
    capturedRequest.RequestUri!.ToString().Should().Be("https://api.cloudflare.com/client/v4/user/invites/invite-accept-001");

    // Verify JSON body contains status:accepted
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("status").GetString().Should().Be("accepted");
  }

  /// <summary>U04: Verifies that RespondToInvitationAsync with Reject sends PATCH with {"status":"rejected"}.</summary>
  [Fact]
  public async Task RespondToInvitationAsync_RejectInvitation_SendsCorrectPatchRequest()
  {
    // Arrange
    var invitationId = "invite-reject-001";
    var request = new RespondToInvitationRequest(MemberStatus.Rejected);
    var invitationJson = """
      {
        "id": "invite-reject-001",
        "invited_member_email": "user@example.com",
        "status": "rejected",
        "invited_on": "2024-01-15T10:00:00Z",
        "expires_on": "2024-01-22T10:00:00Z"
      }
      """;
    var successResponse = CreateSuccessResponseRaw(invitationJson);
    HttpRequestMessage? capturedRequest = null;
    string? capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.RespondToInvitationAsync(invitationId, request);

    // Assert
    result.Should().NotBeNull();
    result.Status.Should().Be(MemberStatus.Rejected);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Patch);
    capturedRequest.RequestUri!.ToString().Should().Be("https://api.cloudflare.com/client/v4/user/invites/invite-reject-001");

    // Verify JSON body contains status:rejected
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("status").GetString().Should().Be("rejected");
  }

  #endregion


  #region F16 User Invitations - Response Deserialization Tests (U05-U13)

  /// <summary>U05: Verifies that UserInvitation model deserializes all properties correctly from a complete JSON response.</summary>
  /// <remarks>
  ///   The Cloudflare API returns roles as an array of role name strings (not AccountRole objects).
  ///   Example: "roles": ["Administrator", "DNS Manager"]
  /// </remarks>
  [Fact]
  public async Task GetInvitationAsync_FullModel_DeserializesAllProperties()
  {
    // Arrange
    var invitationJson = """
      {
        "id": "invite-full-001",
        "invited_member_email": "invited@example.com",
        "status": "pending",
        "invited_on": "2024-01-15T10:30:00Z",
        "expires_on": "2024-01-22T10:30:00Z",
        "organization_name": "Acme Corporation",
        "roles": ["Administrator", "DNS Manager"]
      }
      """;
    var successResponse = CreateSuccessResponseRaw(invitationJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetInvitationAsync("invite-full-001");

    // Assert
    result.Id.Should().Be("invite-full-001");
    result.InvitedMemberEmail.Should().Be("invited@example.com");
    result.Status.Should().Be(MemberStatus.Pending);
    result.InvitedOn.Should().Be(DateTime.Parse("2024-01-15T10:30:00Z").ToUniversalTime());
    result.ExpiresOn.Should().Be(DateTime.Parse("2024-01-22T10:30:00Z").ToUniversalTime());
    result.OrganizationName.Should().Be("Acme Corporation");
    result.Roles.Should().NotBeNull();
    result.Roles!.Should().HaveCount(2);
    result.Roles[0].Should().Be("Administrator");
    result.Roles[1].Should().Be("DNS Manager");
  }

  /// <summary>U06: Verifies that UserInvitation model with only required fields has null for optional properties.</summary>
  [Fact]
  public async Task GetInvitationAsync_MinimalModel_DeserializesWithNulls()
  {
    // Arrange
    var invitationJson = """
      {
        "id": "invite-minimal-001",
        "invited_member_email": "minimal@example.com",
        "status": "pending",
        "invited_on": "2024-01-15T10:00:00Z",
        "expires_on": "2024-01-22T10:00:00Z"
      }
      """;
    var successResponse = CreateSuccessResponseRaw(invitationJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetInvitationAsync("invite-minimal-001");

    // Assert
    result.Id.Should().Be("invite-minimal-001");
    result.InvitedMemberEmail.Should().Be("minimal@example.com");
    result.Status.Should().Be(MemberStatus.Pending);
    result.OrganizationName.Should().BeNull();
    result.Roles.Should().BeNull();
  }

  /// <summary>U07: Verifies that UserInvitation with status "pending" deserializes to MemberStatus.Pending.</summary>
  [Fact]
  public async Task GetInvitationAsync_PendingStatus_DeserializesCorrectly()
  {
    // Arrange
    var invitationJson = """
      {
        "id": "invite-pending",
        "invited_member_email": "pending@example.com",
        "status": "pending",
        "invited_on": "2024-01-15T10:00:00Z",
        "expires_on": "2024-01-22T10:00:00Z"
      }
      """;
    var successResponse = CreateSuccessResponseRaw(invitationJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetInvitationAsync("invite-pending");

    // Assert
    result.Status.Should().Be(MemberStatus.Pending);
    ((string)result.Status).Should().Be("pending");
  }

  /// <summary>U08: Verifies that UserInvitation with status "accepted" deserializes to MemberStatus.Accepted.</summary>
  [Fact]
  public async Task GetInvitationAsync_AcceptedStatus_DeserializesCorrectly()
  {
    // Arrange
    var invitationJson = """
      {
        "id": "invite-accepted",
        "invited_member_email": "accepted@example.com",
        "status": "accepted",
        "invited_on": "2024-01-15T10:00:00Z",
        "expires_on": "2024-01-22T10:00:00Z"
      }
      """;
    var successResponse = CreateSuccessResponseRaw(invitationJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetInvitationAsync("invite-accepted");

    // Assert
    result.Status.Should().Be(MemberStatus.Accepted);
    ((string)result.Status).Should().Be("accepted");
  }

  /// <summary>U09: Verifies that UserInvitation with status "rejected" deserializes to MemberStatus.Rejected.</summary>
  [Fact]
  public async Task GetInvitationAsync_RejectedStatus_DeserializesCorrectly()
  {
    // Arrange
    var invitationJson = """
      {
        "id": "invite-rejected",
        "invited_member_email": "rejected@example.com",
        "status": "rejected",
        "invited_on": "2024-01-15T10:00:00Z",
        "expires_on": "2024-01-22T10:00:00Z"
      }
      """;
    var successResponse = CreateSuccessResponseRaw(invitationJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetInvitationAsync("invite-rejected");

    // Assert
    result.Status.Should().Be(MemberStatus.Rejected);
    ((string)result.Status).Should().Be("rejected");
  }

  /// <summary>U10: Verifies that UserInvitation with roles array deserializes role name strings correctly.</summary>
  /// <remarks>
  ///   The Cloudflare API returns roles as an array of role name strings (e.g., "Super Administrator - All Privileges").
  ///   This differs from the Account Roles API which returns full AccountRole objects.
  /// </remarks>
  [Fact]
  public async Task GetInvitationAsync_WithRoles_DeserializesRolesCorrectly()
  {
    // Arrange
    var invitationJson = """
      {
        "id": "invite-with-roles",
        "invited_member_email": "withroles@example.com",
        "status": "pending",
        "invited_on": "2024-01-15T10:00:00Z",
        "expires_on": "2024-01-22T10:00:00Z",
        "roles": ["Super Administrator - All Privileges", "DNS Admin"]
      }
      """;
    var successResponse = CreateSuccessResponseRaw(invitationJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetInvitationAsync("invite-with-roles");

    // Assert
    result.Roles.Should().NotBeNull();
    result.Roles!.Should().HaveCount(2);
    result.Roles[0].Should().Be("Super Administrator - All Privileges");
    result.Roles[1].Should().Be("DNS Admin");
  }

  /// <summary>U11: Verifies that UserInvitation with organization_name deserializes correctly.</summary>
  [Fact]
  public async Task GetInvitationAsync_WithOrganizationName_DeserializesCorrectly()
  {
    // Arrange
    var invitationJson = """
      {
        "id": "invite-with-org",
        "invited_member_email": "withorg@example.com",
        "status": "pending",
        "invited_on": "2024-01-15T10:00:00Z",
        "expires_on": "2024-01-22T10:00:00Z",
        "organization_name": "My Test Organization Ltd."
      }
      """;
    var successResponse = CreateSuccessResponseRaw(invitationJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetInvitationAsync("invite-with-org");

    // Assert
    result.OrganizationName.Should().Be("My Test Organization Ltd.");
  }

  /// <summary>U12: Verifies that DateTime parsing for InvitedOn and ExpiresOn works correctly with ISO format.</summary>
  [Fact]
  public async Task GetInvitationAsync_DateTimeParsing_ParsesIsoFormatCorrectly()
  {
    // Arrange
    var invitationJson = """
      {
        "id": "invite-datetime",
        "invited_member_email": "datetime@example.com",
        "status": "pending",
        "invited_on": "2024-06-15T14:30:45Z",
        "expires_on": "2024-06-22T23:59:59Z"
      }
      """;
    var successResponse = CreateSuccessResponseRaw(invitationJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetInvitationAsync("invite-datetime");

    // Assert
    result.InvitedOn.Should().Be(new DateTime(2024, 6, 15, 14, 30, 45, DateTimeKind.Utc));
    result.ExpiresOn.Should().Be(new DateTime(2024, 6, 22, 23, 59, 59, DateTimeKind.Utc));
  }

  /// <summary>U13: Verifies that ListInvitationsAsync returns empty list when no invitations exist.</summary>
  [Fact]
  public async Task ListInvitationsAsync_EmptyList_ReturnsEmptyCollection()
  {
    // Arrange
    var invitationsJson = "[]";
    var successResponse = CreateSuccessResponseRaw(invitationsJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListInvitationsAsync();

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
  }

  #endregion


  #region F16 User Invitations - Error Handling Tests (U14-U18)

  /// <summary>U14: Verifies that GetInvitationAsync throws CloudflareApiException when API returns success=false.</summary>
  [Fact]
  public async Task GetInvitationAsync_ApiError_ThrowsCloudflareApiException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(1001, "Invalid API token");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetInvitationAsync("invite-001");

    // Assert
    var exception = await act.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().HaveCount(1);
    exception.Which.Errors[0].Code.Should().Be(1001);
    exception.Which.Errors[0].Message.Should().Be("Invalid API token");
  }

  /// <summary>U15: Verifies that RespondToInvitationAsync throws CloudflareApiException when invitation already responded.</summary>
  [Fact]
  public async Task RespondToInvitationAsync_AlreadyResponded_ThrowsCloudflareApiException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(1004, "Invitation has already been responded to");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.RespondToInvitationAsync("invite-001", new RespondToInvitationRequest(MemberStatus.Accepted));

    // Assert
    var exception = await act.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().HaveCount(1);
    exception.Which.Errors[0].Code.Should().Be(1004);
  }

  /// <summary>U16: Verifies that CloudflareApiException contains all errors when API returns multiple errors.</summary>
  [Fact]
  public async Task ListInvitationsAsync_MultipleErrors_ThrowsCloudflareApiExceptionWithAllErrors()
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
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.ListInvitationsAsync();

    // Assert
    var exception = await act.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().HaveCount(2);
    exception.Which.Errors[0].Code.Should().Be(1001);
    exception.Which.Errors[1].Code.Should().Be(1002);
  }

  /// <summary>U17: Verifies that GetInvitationAsync properly URL-encodes special characters in invitation ID.</summary>
  [Fact]
  public async Task GetInvitationAsync_SpecialCharsInId_UrlEncodesCorrectly()
  {
    // Arrange
    var invitationIdWithSpecialChars = "invite/test+id&foo=bar";
    var invitationJson = """
      {
        "id": "invite/test+id&foo=bar",
        "invited_member_email": "urltest@example.com",
        "status": "pending",
        "invited_on": "2024-01-15T10:00:00Z",
        "expires_on": "2024-01-22T10:00:00Z"
      }
      """;
    var successResponse = CreateSuccessResponseRaw(invitationJson);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetInvitationAsync(invitationIdWithSpecialChars);

    // Assert
    capturedRequest.Should().NotBeNull();
    var requestUri = capturedRequest!.RequestUri!.ToString();

    // Verify URL encoding: / -> %2F, + -> %2B, & -> %26, = -> %3D
    requestUri.Should().Contain("invite%2Ftest%2Bid%26foo%3Dbar");
    requestUri.Should().NotContain("/test+"); // Should be encoded
    requestUri.Should().NotContain("&foo");   // Should be encoded
  }

  /// <summary>U18: Verifies that RespondToInvitationAsync properly URL-encodes special characters in invitation ID.</summary>
  [Fact]
  public async Task RespondToInvitationAsync_SpecialCharsInId_UrlEncodesCorrectly()
  {
    // Arrange
    var invitationIdWithSpecialChars = "invite#test%id";
    var invitationJson = """
      {
        "id": "invite#test%id",
        "invited_member_email": "urltest@example.com",
        "status": "accepted",
        "invited_on": "2024-01-15T10:00:00Z",
        "expires_on": "2024-01-22T10:00:00Z"
      }
      """;
    var successResponse = CreateSuccessResponseRaw(invitationJson);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.RespondToInvitationAsync(invitationIdWithSpecialChars, new RespondToInvitationRequest(MemberStatus.Accepted));

    // Assert
    capturedRequest.Should().NotBeNull();
    var requestUri = capturedRequest!.RequestUri!.ToString();

    // Verify URL encoding: # -> %23, % -> %25
    requestUri.Should().Contain("invite%23test%25id");
    requestUri.Should().NotContain("#test"); // Should be encoded
  }

  #endregion


  #region F16 User Invitations - Parameter Validation Tests

  /// <summary>Verifies that GetInvitationAsync throws ArgumentException when invitationId is null.</summary>
  [Fact]
  public async Task GetInvitationAsync_NullId_ThrowsArgumentException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetInvitationAsync(null!);

    // Assert
    await act.Should().ThrowAsync<ArgumentException>();
  }

  /// <summary>Verifies that GetInvitationAsync throws ArgumentException when invitationId is empty.</summary>
  [Fact]
  public async Task GetInvitationAsync_EmptyId_ThrowsArgumentException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetInvitationAsync("");

    // Assert
    await act.Should().ThrowAsync<ArgumentException>();
  }

  /// <summary>Verifies that GetInvitationAsync throws ArgumentException when invitationId is whitespace.</summary>
  [Fact]
  public async Task GetInvitationAsync_WhitespaceId_ThrowsArgumentException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetInvitationAsync("   ");

    // Assert
    await act.Should().ThrowAsync<ArgumentException>();
  }

  /// <summary>Verifies that RespondToInvitationAsync throws ArgumentException when invitationId is null.</summary>
  [Fact]
  public async Task RespondToInvitationAsync_NullId_ThrowsArgumentException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.RespondToInvitationAsync(null!, new RespondToInvitationRequest(MemberStatus.Accepted));

    // Assert
    await act.Should().ThrowAsync<ArgumentException>();
  }

  /// <summary>Verifies that RespondToInvitationAsync throws ArgumentNullException when request is null.</summary>
  [Fact]
  public async Task RespondToInvitationAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.RespondToInvitationAsync("invite-001", null!);

    // Assert
    await act.Should().ThrowAsync<ArgumentNullException>();
  }

  #endregion


  #region F11 User Memberships - Request Construction Tests (U01-U09)

  /// <summary>U01: Verifies that ListMembershipsAsync sends a GET request to /memberships without filters.</summary>
  [Fact]
  public async Task ListMembershipsAsync_NoFilters_SendsCorrectGetRequest()
  {
    // Arrange
    var membershipsJson = "[]";
    var successResponse = CreatePaginatedResponseRaw(membershipsJson);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListMembershipsAsync();

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be("https://api.cloudflare.com/client/v4/memberships");
  }

  /// <summary>U02: Verifies that ListMembershipsAsync with status filter includes status in query string.</summary>
  [Fact]
  public async Task ListMembershipsAsync_WithStatusFilter_IncludesStatusInQueryString()
  {
    // Arrange
    var membershipsJson = "[]";
    var successResponse = CreatePaginatedResponseRaw(membershipsJson);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);
    var filters = new ListMembershipsFilters(Status: MemberStatus.Pending);

    // Act
    await sut.ListMembershipsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("status=pending");
  }

  /// <summary>U03: Verifies that ListMembershipsAsync with account name filter includes account.name in query string.</summary>
  [Fact]
  public async Task ListMembershipsAsync_WithAccountNameFilter_IncludesAccountNameInQueryString()
  {
    // Arrange
    var membershipsJson = "[]";
    var successResponse = CreatePaginatedResponseRaw(membershipsJson);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);
    var filters = new ListMembershipsFilters(AccountName: "test");

    // Act
    await sut.ListMembershipsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("account.name=test");
  }

  /// <summary>U04: Verifies that ListMembershipsAsync with order filter includes order in query string.</summary>
  [Fact]
  public async Task ListMembershipsAsync_WithOrderFilter_IncludesOrderInQueryString()
  {
    // Arrange
    var membershipsJson = "[]";
    var successResponse = CreatePaginatedResponseRaw(membershipsJson);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);
    var filters = new ListMembershipsFilters(Order: MembershipOrderField.AccountName);

    // Act
    await sut.ListMembershipsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("order=account.name");
  }

  /// <summary>U05: Verifies that ListMembershipsAsync with pagination includes page and per_page in query string.</summary>
  [Fact]
  public async Task ListMembershipsAsync_WithPagination_IncludesPaginationInQueryString()
  {
    // Arrange
    var membershipsJson = "[]";
    var successResponse = CreatePaginatedResponseRaw(membershipsJson);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);
    var filters = new ListMembershipsFilters(Page: 2, PerPage: 10);

    // Act
    await sut.ListMembershipsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.Query.Should().Contain("page=2");
    capturedRequest.RequestUri!.Query.Should().Contain("per_page=10");
  }

  /// <summary>U06: Verifies that GetMembershipAsync sends a GET request to /memberships/{id}.</summary>
  [Fact]
  public async Task GetMembershipAsync_SendsCorrectGetRequest()
  {
    // Arrange
    var membershipId = "membership-12345";
    var membershipJson = CreateTestMembershipJson();
    var successResponse = CreateSuccessResponseRaw(membershipJson);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    await sut.GetMembershipAsync(membershipId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be("https://api.cloudflare.com/client/v4/memberships/membership-12345");
  }

  /// <summary>U07: Verifies that UpdateMembershipAsync with accept status sends PUT with correct body.</summary>
  [Fact]
  public async Task UpdateMembershipAsync_AcceptStatus_SendsCorrectPutRequest()
  {
    // Arrange
    var membershipId = "membership-accept-001";
    var request = new UpdateMembershipRequest(MemberStatus.Accepted);
    var membershipJson = CreateTestMembershipJson("accepted");
    var successResponse = CreateSuccessResponseRaw(membershipJson);
    HttpRequestMessage? capturedRequest = null;
    string? capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateMembershipAsync(membershipId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should().Be("https://api.cloudflare.com/client/v4/memberships/membership-accept-001");
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("status").GetString().Should().Be("accepted");
  }

  /// <summary>U08: Verifies that UpdateMembershipAsync with reject status sends PUT with correct body.</summary>
  [Fact]
  public async Task UpdateMembershipAsync_RejectStatus_SendsCorrectPutRequest()
  {
    // Arrange
    var membershipId = "membership-reject-001";
    var request = new UpdateMembershipRequest(MemberStatus.Rejected);
    var membershipJson = CreateTestMembershipJson("rejected");
    var successResponse = CreateSuccessResponseRaw(membershipJson);
    HttpRequestMessage? capturedRequest = null;
    string? capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateMembershipAsync(membershipId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("status").GetString().Should().Be("rejected");
  }

  /// <summary>U09: Verifies that DeleteMembershipAsync sends a DELETE request to /memberships/{id}.</summary>
  [Fact]
  public async Task DeleteMembershipAsync_SendsCorrectDeleteRequest()
  {
    // Arrange
    var membershipId = "membership-delete-001";
    var successResponse = CreateSuccessResponseRaw("null");
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    await sut.DeleteMembershipAsync(membershipId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should().Be("https://api.cloudflare.com/client/v4/memberships/membership-delete-001");
  }

  #endregion


  #region F11 User Memberships - Response Deserialization Tests (U10-U17)

  /// <summary>U10: Verifies that Membership model deserializes all properties correctly from a complete JSON response.</summary>
  [Fact]
  public async Task GetMembershipAsync_FullModel_DeserializesAllProperties()
  {
    // Arrange
    var membershipJson = """
      {
        "id": "membership-full-001",
        "status": "accepted",
        "api_access_enabled": true,
        "account": {
          "id": "account-001",
          "name": "Test Account",
          "type": "standard",
          "settings": {
            "enforce_twofactor": true
          }
        },
        "permissions": {
          "analytics": { "read": true, "write": false },
          "dns": { "read": true, "write": true }
        },
        "roles": ["role-001", "role-002"],
        "policies": [
          {
            "id": "policy-001",
            "access": "allow",
            "permission_groups": [{ "id": "pg-001" }],
            "resource_groups": [{ "id": "rg-001" }]
          }
        ]
      }
      """;
    var successResponse = CreateSuccessResponseRaw(membershipJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetMembershipAsync("membership-full-001");

    // Assert
    result.Id.Should().Be("membership-full-001");
    result.Status.Should().Be(MemberStatus.Accepted);
    result.ApiAccessEnabled.Should().BeTrue();
    result.Account.Should().NotBeNull();
    result.Account.Id.Should().Be("account-001");
    result.Account.Name.Should().Be("Test Account");
    result.Permissions.Should().NotBeNull();
    result.Permissions!.Analytics.Should().NotBeNull();
    result.Permissions.Analytics!.Read.Should().BeTrue();
    result.Permissions.Analytics.Write.Should().BeFalse();
    result.Permissions.Dns.Should().NotBeNull();
    result.Roles.Should().NotBeNull();
    result.Roles!.Should().HaveCount(2);
    result.Policies.Should().NotBeNull();
    result.Policies!.Should().HaveCount(1);
    result.Policies[0].Id.Should().Be("policy-001");
  }

  /// <summary>U11: Verifies that Membership model with only required fields has null for optional properties.</summary>
  [Fact]
  public async Task GetMembershipAsync_MinimalModel_DeserializesWithNulls()
  {
    // Arrange
    var membershipJson = """
      {
        "id": "membership-minimal-001",
        "status": "pending",
        "account": {
          "id": "account-001",
          "name": "Minimal Account"
        }
      }
      """;
    var successResponse = CreateSuccessResponseRaw(membershipJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetMembershipAsync("membership-minimal-001");

    // Assert
    result.Id.Should().Be("membership-minimal-001");
    result.Status.Should().Be(MemberStatus.Pending);
    result.Account.Should().NotBeNull();
    result.ApiAccessEnabled.Should().BeNull();
    result.Permissions.Should().BeNull();
    result.Roles.Should().BeNull();
    result.Policies.Should().BeNull();
  }

  /// <summary>U12: Verifies that Membership model with nested account deserializes correctly.</summary>
  [Fact]
  public async Task GetMembershipAsync_WithAccount_DeserializesAccountCorrectly()
  {
    // Arrange
    var membershipJson = """
      {
        "id": "membership-account-001",
        "status": "accepted",
        "account": {
          "id": "account-full-001",
          "name": "Full Account Details",
          "type": "enterprise",
          "settings": {
            "enforce_twofactor": false,
            "use_account_custom_ns_by_default": true
          }
        }
      }
      """;
    var successResponse = CreateSuccessResponseRaw(membershipJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetMembershipAsync("membership-account-001");

    // Assert
    result.Account.Should().NotBeNull();
    result.Account.Id.Should().Be("account-full-001");
    result.Account.Name.Should().Be("Full Account Details");
  }

  /// <summary>U13: Verifies that Membership with status "accepted" deserializes to MemberStatus.Accepted.</summary>
  [Fact]
  public async Task GetMembershipAsync_AcceptedStatus_DeserializesCorrectly()
  {
    // Arrange
    var membershipJson = CreateTestMembershipJson("accepted");
    var successResponse = CreateSuccessResponseRaw(membershipJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetMembershipAsync("test");

    // Assert
    result.Status.Should().Be(MemberStatus.Accepted);
    ((string)result.Status).Should().Be("accepted");
  }

  /// <summary>U14: Verifies that Membership with status "pending" deserializes to MemberStatus.Pending.</summary>
  [Fact]
  public async Task GetMembershipAsync_PendingStatus_DeserializesCorrectly()
  {
    // Arrange
    var membershipJson = CreateTestMembershipJson("pending");
    var successResponse = CreateSuccessResponseRaw(membershipJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetMembershipAsync("test");

    // Assert
    result.Status.Should().Be(MemberStatus.Pending);
    ((string)result.Status).Should().Be("pending");
  }

  /// <summary>U15: Verifies that Membership with status "rejected" deserializes to MemberStatus.Rejected.</summary>
  [Fact]
  public async Task GetMembershipAsync_RejectedStatus_DeserializesCorrectly()
  {
    // Arrange
    var membershipJson = CreateTestMembershipJson("rejected");
    var successResponse = CreateSuccessResponseRaw(membershipJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetMembershipAsync("test");

    // Assert
    result.Status.Should().Be(MemberStatus.Rejected);
    ((string)result.Status).Should().Be("rejected");
  }

  /// <summary>U16: Verifies that MembershipPermissions with all permission categories deserializes correctly.</summary>
  [Fact]
  public async Task GetMembershipAsync_WithPermissions_DeserializesPermissionsCorrectly()
  {
    // Arrange
    var membershipJson = """
      {
        "id": "membership-perm-001",
        "status": "accepted",
        "account": { "id": "account-001", "name": "Test Account" },
        "permissions": {
          "analytics": { "read": true, "write": true },
          "billing": { "read": true, "write": false },
          "cache_purge": { "read": false, "write": true },
          "dns": { "read": true, "write": true },
          "lb": { "read": true, "write": false },
          "logs": { "read": true, "write": false },
          "organization": { "read": true, "write": true },
          "ssl": { "read": true, "write": true },
          "waf": { "read": true, "write": false },
          "zones": { "read": true, "write": true }
        }
      }
      """;
    var successResponse = CreateSuccessResponseRaw(membershipJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetMembershipAsync("membership-perm-001");

    // Assert
    result.Permissions.Should().NotBeNull();
    var perms = result.Permissions!;
    perms.Analytics!.Read.Should().BeTrue();
    perms.Analytics.Write.Should().BeTrue();
    perms.Billing!.Read.Should().BeTrue();
    perms.Billing.Write.Should().BeFalse();
    perms.CachePurge!.Read.Should().BeFalse();
    perms.CachePurge.Write.Should().BeTrue();
    perms.Dns!.Read.Should().BeTrue();
    perms.Dns.Write.Should().BeTrue();
    perms.LoadBalancer!.Read.Should().BeTrue();
    perms.Logs!.Read.Should().BeTrue();
    perms.Organization!.Read.Should().BeTrue();
    perms.Organization.Write.Should().BeTrue();
    perms.Ssl!.Read.Should().BeTrue();
    perms.Waf!.Read.Should().BeTrue();
    perms.Zones!.Read.Should().BeTrue();
    perms.Zones.Write.Should().BeTrue();
  }

  /// <summary>U17: Verifies that MembershipPolicy with permission and resource groups deserializes correctly.</summary>
  [Fact]
  public async Task GetMembershipAsync_WithPolicies_DeserializesPoliciesCorrectly()
  {
    // Arrange
    var membershipJson = """
      {
        "id": "membership-policy-001",
        "status": "accepted",
        "account": { "id": "account-001", "name": "Test Account" },
        "policies": [
          {
            "id": "policy-001",
            "access": "allow",
            "permission_groups": [
              { "id": "pg-001" },
              { "id": "pg-002" }
            ],
            "resource_groups": [
              { "id": "rg-001" },
              { "id": "rg-002" }
            ]
          },
          {
            "id": "policy-002",
            "access": "deny"
          }
        ]
      }
      """;
    var successResponse = CreateSuccessResponseRaw(membershipJson);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetMembershipAsync("membership-policy-001");

    // Assert
    result.Policies.Should().NotBeNull();
    result.Policies!.Should().HaveCount(2);
    var policy1 = result.Policies[0];
    policy1.Id.Should().Be("policy-001");
    policy1.Access.Should().Be("allow");
    policy1.PermissionGroups.Should().HaveCount(2);
    policy1.PermissionGroups![0].Id.Should().Be("pg-001");
    policy1.ResourceGroups.Should().HaveCount(2);
    policy1.ResourceGroups![0].Id.Should().Be("rg-001");
    var policy2 = result.Policies[1];
    policy2.Id.Should().Be("policy-002");
    policy2.Access.Should().Be("deny");
  }

  #endregion


  #region F11 User Memberships - Pagination Tests (U18-U19)

  /// <summary>U18: Verifies that ListAllMembershipsAsync with single page returns all items.</summary>
  [Fact]
  public async Task ListAllMembershipsAsync_SinglePage_ReturnsAllItems()
  {
    // Arrange
    var membershipJson = $"[{CreateTestMembershipJson()}]";
    var successResponse = CreatePaginatedResponseRaw(membershipJson, page: 1, totalPages: 1, count: 1, totalCount: 1);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var results = new List<Membership>();
    await foreach (var item in sut.ListAllMembershipsAsync())
    {
      results.Add(item);
    }

    // Assert
    results.Should().HaveCount(1);
    results[0].Id.Should().Be("membership-001");
  }

  /// <summary>U19: Verifies that ListAllMembershipsAsync with multiple pages iterates all pages.</summary>
  [Fact]
  public async Task ListAllMembershipsAsync_MultiplePages_IteratesAllPages()
  {
    // Arrange
    var callCount = 0;
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .ReturnsAsync(() =>
      {
        callCount++;
        var membershipJson = $"{{\"id\": \"membership-{callCount:D3}\", \"status\": \"accepted\", \"account\": {{\"id\": \"account-001\", \"name\": \"Test\"}}}}";
        var response = CreatePaginatedResponseRaw(
          $"[{membershipJson}]",
          page: callCount,
          totalPages: 3,
          count: 1,
          totalCount: 3);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
          Content = new StringContent(response, System.Text.Encoding.UTF8, "application/json")
        };
      });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var results = new List<Membership>();
    await foreach (var item in sut.ListAllMembershipsAsync())
    {
      results.Add(item);
    }

    // Assert
    results.Should().HaveCount(3);
    callCount.Should().Be(3);
  }

  #endregion


  #region F11 User Memberships - Error Handling Tests (U20-U23)

  /// <summary>U20: Verifies that GetMembershipAsync throws CloudflareApiException when API returns success=false.</summary>
  [Fact]
  public async Task GetMembershipAsync_ApiError_ThrowsCloudflareApiException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(1001, "Invalid API token");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetMembershipAsync("membership-001");

    // Assert
    var exception = await act.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().HaveCount(1);
    exception.Which.Errors[0].Code.Should().Be(1001);
  }

  /// <summary>U21: Verifies that UpdateMembershipAsync throws CloudflareApiException for invalid status update.</summary>
  [Fact]
  public async Task UpdateMembershipAsync_InvalidStatus_ThrowsCloudflareApiException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(1004, "Invalid status transition");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.UpdateMembershipAsync("membership-001", new UpdateMembershipRequest(MemberStatus.Accepted));

    // Assert
    var exception = await act.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().HaveCount(1);
    exception.Which.Errors[0].Code.Should().Be(1004);
  }

  /// <summary>U22: Verifies that CloudflareApiException contains all errors when API returns multiple errors.</summary>
  [Fact]
  public async Task ListMembershipsAsync_MultipleErrors_ThrowsCloudflareApiExceptionWithAllErrors()
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
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.ListMembershipsAsync();

    // Assert
    var exception = await act.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().HaveCount(2);
    exception.Which.Errors[0].Code.Should().Be(1001);
    exception.Which.Errors[1].Code.Should().Be(1002);
  }

  /// <summary>U23: Verifies that GetMembershipAsync properly URL-encodes special characters in membership ID.</summary>
  [Fact]
  public async Task GetMembershipAsync_SpecialCharsInId_UrlEncodesCorrectly()
  {
    // Arrange
    var membershipIdWithSpecialChars = "membership/test+id&foo=bar";
    var membershipJson = CreateTestMembershipJson();
    var successResponse = CreateSuccessResponseRaw(membershipJson);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    await sut.GetMembershipAsync(membershipIdWithSpecialChars);

    // Assert
    capturedRequest.Should().NotBeNull();
    var requestUri = capturedRequest!.RequestUri!.ToString();

    // Verify URL encoding: / -> %2F, + -> %2B, & -> %26, = -> %3D
    requestUri.Should().Contain("membership%2Ftest%2Bid%26foo%3Dbar");
    requestUri.Should().NotContain("/test+");
    requestUri.Should().NotContain("&foo");
  }

  #endregion


  #region F11 User Memberships - Parameter Validation Tests

  /// <summary>Verifies that GetMembershipAsync throws ArgumentException when membershipId is null.</summary>
  [Fact]
  public async Task GetMembershipAsync_MembershipId_NullId_ThrowsArgumentException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetMembershipAsync(null!);

    // Assert
    await act.Should().ThrowAsync<ArgumentException>();
  }

  /// <summary>Verifies that GetMembershipAsync throws ArgumentException when membershipId is empty.</summary>
  [Fact]
  public async Task GetMembershipAsync_MembershipId_EmptyId_ThrowsArgumentException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetMembershipAsync("");

    // Assert
    await act.Should().ThrowAsync<ArgumentException>();
  }

  /// <summary>Verifies that GetMembershipAsync throws ArgumentException when membershipId is whitespace.</summary>
  [Fact]
  public async Task GetMembershipAsync_MembershipId_WhitespaceId_ThrowsArgumentException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.GetMembershipAsync("   ");

    // Assert
    await act.Should().ThrowAsync<ArgumentException>();
  }

  /// <summary>Verifies that UpdateMembershipAsync throws ArgumentException when membershipId is null.</summary>
  [Fact]
  public async Task UpdateMembershipAsync_NullId_ThrowsArgumentException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.UpdateMembershipAsync(null!, new UpdateMembershipRequest(MemberStatus.Accepted));

    // Assert
    await act.Should().ThrowAsync<ArgumentException>();
  }

  /// <summary>Verifies that UpdateMembershipAsync throws ArgumentNullException when request is null.</summary>
  [Fact]
  public async Task UpdateMembershipAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.UpdateMembershipAsync("membership-001", null!);

    // Assert
    await act.Should().ThrowAsync<ArgumentNullException>();
  }

  /// <summary>Verifies that DeleteMembershipAsync throws ArgumentException when membershipId is null.</summary>
  [Fact]
  public async Task DeleteMembershipAsync_NullId_ThrowsArgumentException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.DeleteMembershipAsync(null!);

    // Assert
    await act.Should().ThrowAsync<ArgumentException>();
  }

  /// <summary>Verifies that DeleteMembershipAsync throws ArgumentException when membershipId is empty.</summary>
  [Fact]
  public async Task DeleteMembershipAsync_EmptyId_ThrowsArgumentException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new UserApi(httpClient, _loggerFactory);

    // Act
    var act = () => sut.DeleteMembershipAsync("");

    // Assert
    await act.Should().ThrowAsync<ArgumentException>();
  }

  #endregion


  #region Helper Methods

  /// <summary>Creates a test User instance with default or custom values.</summary>
  private static User CreateTestUser(
    string? id = null,
    string? email = null,
    string? firstName = null,
    string? lastName = null,
    string? telephone = null,
    string? country = null,
    string? zipcode = null,
    bool suspended = false,
    bool twoFactorEnabled = false,
    bool twoFactorLocked = false,
    bool hasProZones = false,
    bool hasBusinessZones = false,
    bool hasEnterpriseZones = false,
    IReadOnlyList<string>? betas = null,
    IReadOnlyList<UserOrganization>? organizations = null,
    DateTime? createdOn = null,
    DateTime? modifiedOn = null)
  {
    return new User(
      id ?? "test-user-12345678901234567890123456",
      email ?? "test@example.com",
      firstName,
      lastName,
      telephone,
      country,
      zipcode,
      suspended,
      twoFactorEnabled,
      twoFactorLocked,
      hasProZones,
      hasBusinessZones,
      hasEnterpriseZones,
      betas,
      organizations,
      createdOn ?? DateTime.UtcNow.AddYears(-1),
      modifiedOn ?? DateTime.UtcNow
    );
  }

  /// <summary>Creates a raw success response JSON string with a given result JSON.</summary>
  private static string CreateSuccessResponseRaw(string resultJson)
  {
    return $$"""
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": {{resultJson}}
      }
      """;
  }

  /// <summary>Creates a paginated response JSON string for page-based pagination.</summary>
  private static string CreatePaginatedResponseRaw(
    string resultJson,
    int page = 1,
    int totalPages = 1,
    int count = 0,
    int totalCount = 0)
  {
    return $$"""
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": {{resultJson}},
        "result_info": {
          "page": {{page}},
          "per_page": 20,
          "count": {{count}},
          "total_count": {{totalCount}},
          "total_pages": {{totalPages}}
        }
      }
      """;
  }

  /// <summary>Creates a test membership JSON string with the specified status.</summary>
  private static string CreateTestMembershipJson(string status = "accepted")
  {
    return $$"""
      {
        "id": "membership-001",
        "status": "{{status}}",
        "account": {
          "id": "account-001",
          "name": "Test Account"
        }
      }
      """;
  }

  #endregion
}
