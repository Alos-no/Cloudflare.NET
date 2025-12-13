namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using Cloudflare.NET.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Shared.Fixtures;
using Subscriptions;
using Subscriptions.Models;
using Xunit.Abstractions;


/// <summary>
///   Contains unit tests for User Subscriptions operations in <see cref="SubscriptionsApi"/>.
/// </summary>
/// <remarks>
///   <para>
///     These tests verify the user-scoped subscription endpoints (/user/subscriptions).
///     User subscriptions cannot be created via API - only List, Update, and Delete operations.
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class UserSubscriptionsApiUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly ILoggerFactory _loggerFactory;

  #endregion


  #region Constructors

  public UserSubscriptionsApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion


  #region Request Construction Tests (U01-U07)

  /// <summary>U01: Verifies that ListUserSubscriptionsAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task ListUserSubscriptionsAsync_SendsCorrectRequest()
  {
    // Arrange
    var successResponse = CreateListResponse(Array.Empty<Subscription>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.ListUserSubscriptionsAsync();

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be("/client/v4/user/subscriptions");
    capturedRequest.RequestUri.Query.Should().BeEmpty();
  }

  /// <summary>U02: Verifies that UpdateUserSubscriptionAsync sends a PUT request to the correct endpoint.</summary>
  [Fact]
  public async Task UpdateUserSubscriptionAsync_SendsCorrectRequest()
  {
    // Arrange
    var subscriptionId = "sub-123";
    var request = new UpdateUserSubscriptionRequest(
      Frequency: SubscriptionFrequency.Monthly);
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription(subscriptionId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateUserSubscriptionAsync(subscriptionId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/user/subscriptions/{subscriptionId}");
  }

  /// <summary>U03: Verifies that UpdateUserSubscriptionAsync includes frequency in request body.</summary>
  [Fact]
  public async Task UpdateUserSubscriptionAsync_WithFrequency_IncludesFrequencyInBody()
  {
    // Arrange
    var request = new UpdateUserSubscriptionRequest(
      Frequency: SubscriptionFrequency.Yearly);
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription("sub"));
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, async (req, _) =>
    {
      capturedBody = await req.Content!.ReadAsStringAsync();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateUserSubscriptionAsync("sub", request);

    // Assert
    capturedBody.Should().Contain("\"frequency\":\"yearly\"");
  }

  /// <summary>U04: Verifies that UpdateUserSubscriptionAsync includes rate_plan in request body.</summary>
  [Fact]
  public async Task UpdateUserSubscriptionAsync_WithRatePlan_IncludesRatePlanInBody()
  {
    // Arrange
    var request = new UpdateUserSubscriptionRequest(
      RatePlan: new RatePlanReference("new_plan_id"));
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription("sub"));
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, async (req, _) =>
    {
      capturedBody = await req.Content!.ReadAsStringAsync();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateUserSubscriptionAsync("sub", request);

    // Assert
    capturedBody.Should().Contain("\"rate_plan\"");
    capturedBody.Should().Contain("\"id\":\"new_plan_id\"");
  }

  /// <summary>U05: Verifies that UpdateUserSubscriptionAsync includes component_values in request body.</summary>
  [Fact]
  public async Task UpdateUserSubscriptionAsync_WithComponents_IncludesComponentsInBody()
  {
    // Arrange
    var request = new UpdateUserSubscriptionRequest(
      ComponentValues: new[]
      {
        new SubscriptionComponentValue("page_rules", 100)
      });
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription("sub"));
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, async (req, _) =>
    {
      capturedBody = await req.Content!.ReadAsStringAsync();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateUserSubscriptionAsync("sub", request);

    // Assert
    capturedBody.Should().Contain("\"component_values\"");
    capturedBody.Should().Contain("\"name\":\"page_rules\"");
    capturedBody.Should().Contain("\"value\":100");
  }

  /// <summary>U06: Verifies that DeleteUserSubscriptionAsync sends a DELETE request to the correct endpoint.</summary>
  [Fact]
  public async Task DeleteUserSubscriptionAsync_SendsCorrectRequest()
  {
    // Arrange
    var subscriptionId = "sub-456";
    var successResponse = HttpFixtures.CreateSuccessResponse(new DeleteUserSubscriptionResult(subscriptionId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.DeleteUserSubscriptionAsync(subscriptionId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/user/subscriptions/{subscriptionId}");
  }

  /// <summary>U07: Verifies that UpdateUserSubscriptionRequest omits null fields.</summary>
  [Fact]
  public async Task UpdateUserSubscriptionAsync_OnlyFrequency_OmitsNullFields()
  {
    // Arrange
    var request = new UpdateUserSubscriptionRequest(
      Frequency: SubscriptionFrequency.Monthly);
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription("sub"));
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, async (req, _) =>
    {
      capturedBody = await req.Content!.ReadAsStringAsync();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateUserSubscriptionAsync("sub", request);

    // Assert
    capturedBody.Should().Contain("\"frequency\"");
    capturedBody.Should().NotContain("\"rate_plan\"");
    capturedBody.Should().NotContain("\"component_values\"");
  }

  #endregion


  #region Response Deserialization Tests (U08-U15)

  /// <summary>U08: Verifies that Subscription full model deserializes correctly.</summary>
  [Fact]
  public async Task ListUserSubscriptionsAsync_FullModel_DeserializesCorrectly()
  {
    // Arrange
    var responseJson = """
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": [{
          "id": "sub-123",
          "state": "Paid",
          "price": 99.99,
          "currency": "USD",
          "frequency": "monthly",
          "rate_plan": {
            "id": "plan-1",
            "public_name": "Pro Plan",
            "currency": "USD",
            "scope": "user",
            "externally_managed": false
          },
          "current_period_start": "2024-01-01T00:00:00Z",
          "current_period_end": "2024-02-01T00:00:00Z",
          "component_values": [
            { "name": "page_rules", "value": 50, "default": 10, "price": 5.00 }
          ]
        }]
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().HaveCount(1);
    var sub = result[0];
    sub.Id.Should().Be("sub-123");
    sub.State.Should().Be(SubscriptionState.Paid);
    sub.Price.Should().Be(99.99m);
    sub.Currency.Should().Be("USD");
    sub.Frequency.Should().Be(SubscriptionFrequency.Monthly);
    sub.RatePlan.Should().NotBeNull();
    sub.RatePlan!.Id.Should().Be("plan-1");
    sub.RatePlan.PublicName.Should().Be("Pro Plan");
    sub.RatePlan.Scope.Should().Be("user");
    sub.RatePlan.ExternallyManaged.Should().BeFalse();
    sub.CurrentPeriodStart.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    sub.CurrentPeriodEnd.Should().Be(new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc));
    sub.ComponentValues.Should().HaveCount(1);
    sub.ComponentValues![0].Name.Should().Be("page_rules");
    sub.ComponentValues[0].Value.Should().Be(50);
    sub.ComponentValues[0].Default.Should().Be(10);
    sub.ComponentValues[0].Price.Should().Be(5.00m);
  }

  /// <summary>U09: Verifies that Subscription minimal model deserializes correctly.</summary>
  [Fact]
  public async Task ListUserSubscriptionsAsync_MinimalModel_DeserializesCorrectly()
  {
    // Arrange
    var responseJson = """
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": [{
          "id": "sub-minimal",
          "state": "Trial",
          "price": 0.00,
          "currency": "USD",
          "frequency": "weekly"
        }]
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().HaveCount(1);
    var sub = result[0];
    sub.Id.Should().Be("sub-minimal");
    sub.State.Should().Be(SubscriptionState.Trial);
    sub.RatePlan.Should().BeNull();
    sub.CurrentPeriodStart.Should().BeNull();
    sub.CurrentPeriodEnd.Should().BeNull();
    sub.ComponentValues.Should().BeNull();
  }

  /// <summary>U10: Verifies that all SubscriptionState values deserialize correctly.</summary>
  [Theory]
  [InlineData("Trial", "Trial")]
  [InlineData("Provisioned", "Provisioned")]
  [InlineData("Paid", "Paid")]
  [InlineData("AwaitingPayment", "AwaitingPayment")]
  [InlineData("Cancelled", "Cancelled")]
  [InlineData("Failed", "Failed")]
  [InlineData("Expired", "Expired")]
  [InlineData("NewFutureState", "NewFutureState")]
  public async Task ListUserSubscriptionsAsync_StateValues_DeserializeCorrectly(string stateValue, string expectedValue)
  {
    // Arrange
    var responseJson = $$"""
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": [{
          "id": "sub-1",
          "state": "{{stateValue}}",
          "price": 0.00,
          "currency": "USD",
          "frequency": "monthly"
        }]
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().HaveCount(1);
    result[0].State.Value.Should().Be(expectedValue);
  }

  /// <summary>U11: Verifies that all SubscriptionFrequency values deserialize correctly.</summary>
  [Theory]
  [InlineData("weekly", "weekly")]
  [InlineData("monthly", "monthly")]
  [InlineData("quarterly", "quarterly")]
  [InlineData("yearly", "yearly")]
  [InlineData("biannual", "biannual")]
  public async Task ListUserSubscriptionsAsync_FrequencyValues_DeserializeCorrectly(string frequencyValue, string expectedValue)
  {
    // Arrange
    var responseJson = $$"""
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": [{
          "id": "sub-1",
          "state": "Paid",
          "price": 0.00,
          "currency": "USD",
          "frequency": "{{frequencyValue}}"
        }]
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().HaveCount(1);
    result[0].Frequency.Value.Should().Be(expectedValue);
  }

  /// <summary>U12: Verifies that RatePlan nested object deserializes correctly.</summary>
  [Fact]
  public async Task UpdateUserSubscriptionAsync_RatePlanNested_DeserializesCorrectly()
  {
    // Arrange
    var responseJson = """
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": {
          "id": "sub-1",
          "state": "Paid",
          "price": 199.00,
          "currency": "EUR",
          "frequency": "yearly",
          "rate_plan": {
            "id": "enterprise_plan",
            "public_name": "Enterprise",
            "currency": "EUR",
            "scope": "user",
            "externally_managed": true
          }
        }
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);
    var request = new UpdateUserSubscriptionRequest();

    // Act
    var result = await sut.UpdateUserSubscriptionAsync("sub-1", request);

    // Assert
    result.RatePlan.Should().NotBeNull();
    result.RatePlan!.Id.Should().Be("enterprise_plan");
    result.RatePlan.PublicName.Should().Be("Enterprise");
    result.RatePlan.Currency.Should().Be("EUR");
    result.RatePlan.Scope.Should().Be("user");
    result.RatePlan.ExternallyManaged.Should().BeTrue();
  }

  /// <summary>U13: Verifies that SubscriptionComponent nested array deserializes correctly.</summary>
  [Fact]
  public async Task ListUserSubscriptionsAsync_ComponentsNested_DeserializeCorrectly()
  {
    // Arrange
    var responseJson = """
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": [{
          "id": "sub-1",
          "state": "Paid",
          "price": 99.00,
          "currency": "USD",
          "frequency": "monthly",
          "component_values": [
            { "name": "page_rules", "value": 100, "default": 20, "price": 10.00 },
            { "name": "workers", "value": 5, "default": 1, "price": 2.50 }
          ]
        }]
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().HaveCount(1);
    result[0].ComponentValues.Should().HaveCount(2);
    var components = result[0].ComponentValues!;
    components[0].Name.Should().Be("page_rules");
    components[0].Value.Should().Be(100);
    components[1].Name.Should().Be("workers");
    components[1].Value.Should().Be(5);
  }

  /// <summary>U14: Verifies that empty subscription list deserializes correctly.</summary>
  [Fact]
  public async Task ListUserSubscriptionsAsync_EmptyList_ReturnsEmptyList()
  {
    // Arrange
    var responseJson = """
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": []
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
  }

  /// <summary>U15: Verifies that DeleteUserSubscriptionResult deserializes correctly.</summary>
  [Fact]
  public async Task DeleteUserSubscriptionAsync_Result_DeserializesCorrectly()
  {
    // Arrange
    var responseJson = """
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": {
          "subscription_id": "deleted-sub-123"
        }
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.DeleteUserSubscriptionAsync("deleted-sub-123");

    // Assert
    result.Should().NotBeNull();
    result.SubscriptionId.Should().Be("deleted-sub-123");
  }

  #endregion


  #region Error Handling Tests (U16-U19)

  /// <summary>U16: Verifies that API error (success=false) throws CloudflareApiException.</summary>
  [Fact]
  public async Task UpdateUserSubscriptionAsync_ApiError_ThrowsCloudflareApiException()
  {
    // Arrange - 2xx status with success=false throws CloudflareApiException.
    var responseJson = HttpFixtures.CreateErrorResponse(1001, "Invalid subscription");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);
    var request = new UpdateUserSubscriptionRequest();

    // Act
    var action = async () => await sut.UpdateUserSubscriptionAsync("sub", request);

    // Assert
    var exception = await action.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().ContainSingle(e => e.Code == 1001);
  }

  /// <summary>U17: Verifies that invalid rate plan error throws CloudflareApiException.</summary>
  [Fact]
  public async Task UpdateUserSubscriptionAsync_InvalidRatePlan_ThrowsCloudflareApiException()
  {
    // Arrange
    var responseJson = HttpFixtures.CreateErrorResponse(1002, "Invalid rate plan");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);
    var request = new UpdateUserSubscriptionRequest(RatePlan: new RatePlanReference("invalid"));

    // Act
    var action = async () => await sut.UpdateUserSubscriptionAsync("sub", request);

    // Assert
    var exception = await action.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().ContainSingle(e => e.Code == 1002);
  }

  /// <summary>U18: Verifies that multiple API errors are all captured.</summary>
  [Fact]
  public async Task UpdateUserSubscriptionAsync_MultipleErrors_CapturesAllErrors()
  {
    // Arrange
    var responseJson = JsonSerializer.Serialize(new
    {
      success = false,
      errors = new[]
      {
        new { code = 1001, message = "Error 1" },
        new { code = 1002, message = "Error 2" }
      },
      messages = Array.Empty<object>(),
      result = (object?)null
    });
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);
    var request = new UpdateUserSubscriptionRequest();

    // Act
    var action = async () => await sut.UpdateUserSubscriptionAsync("sub", request);

    // Assert
    var exception = await action.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().HaveCount(2);
    exception.Which.Errors.Should().Contain(e => e.Code == 1001);
    exception.Which.Errors.Should().Contain(e => e.Code == 1002);
  }

  #endregion


  #region URL Encoding Tests (U20-U21)

  /// <summary>U20: Verifies that special characters in subscriptionId are URL encoded.</summary>
  [Fact]
  public async Task UpdateUserSubscriptionAsync_SpecialChars_UrlEncodesCorrectly()
  {
    // Arrange
    var subscriptionId = "sub/with:special&chars?";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription("sub"));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);
    var request = new UpdateUserSubscriptionRequest();

    // Act
    await sut.UpdateUserSubscriptionAsync(subscriptionId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    var path = capturedRequest!.RequestUri!.AbsolutePath;
    path.Should().Contain("sub%2Fwith%3Aspecial%26chars%3F");
    path.Should().NotContain("sub/with:special&chars?");
  }

  /// <summary>U21: Verifies that spaces in subscriptionId are URL encoded.</summary>
  [Fact]
  public async Task DeleteUserSubscriptionAsync_SpacesInId_UrlEncodesCorrectly()
  {
    // Arrange
    var subscriptionId = "sub with spaces";
    var successResponse = HttpFixtures.CreateSuccessResponse(new DeleteUserSubscriptionResult(subscriptionId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.DeleteUserSubscriptionAsync(subscriptionId);

    // Assert
    capturedRequest.Should().NotBeNull();
    var path = capturedRequest!.RequestUri!.AbsolutePath;
    path.Should().Contain("sub%20with%20spaces");
    path.Should().NotContain("sub with spaces");
  }

  #endregion


  #region Parameter Validation Tests (U26-U29)

  /// <summary>U26: Verifies that UpdateUserSubscriptionAsync throws for null subscriptionId.</summary>
  [Fact]
  public async Task UpdateUserSubscriptionAsync_NullSubscriptionId_ThrowsArgumentException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);
    var request = new UpdateUserSubscriptionRequest();

    // Act
    var action = async () => await sut.UpdateUserSubscriptionAsync(null!, request);

    // Assert
    await action.Should().ThrowAsync<ArgumentException>()
      .WithParameterName("subscriptionId");
  }

  /// <summary>U27: Verifies that UpdateUserSubscriptionAsync throws for whitespace subscriptionId.</summary>
  [Fact]
  public async Task UpdateUserSubscriptionAsync_WhitespaceSubscriptionId_ThrowsArgumentException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);
    var request = new UpdateUserSubscriptionRequest();

    // Act
    var action = async () => await sut.UpdateUserSubscriptionAsync("   ", request);

    // Assert
    await action.Should().ThrowAsync<ArgumentException>()
      .WithParameterName("subscriptionId");
  }

  /// <summary>U28: Verifies that UpdateUserSubscriptionAsync throws for null request.</summary>
  [Fact]
  public async Task UpdateUserSubscriptionAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var action = async () => await sut.UpdateUserSubscriptionAsync("sub", null!);

    // Assert
    await action.Should().ThrowAsync<ArgumentNullException>()
      .WithParameterName("request");
  }

  /// <summary>U29: Verifies that DeleteUserSubscriptionAsync throws for null subscriptionId.</summary>
  [Fact]
  public async Task DeleteUserSubscriptionAsync_NullSubscriptionId_ThrowsArgumentException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var action = async () => await sut.DeleteUserSubscriptionAsync(null!);

    // Assert
    await action.Should().ThrowAsync<ArgumentException>()
      .WithParameterName("subscriptionId");
  }

  #endregion


  #region Helpers

  /// <summary>Creates a list response JSON for subscriptions.</summary>
  private static string CreateListResponse(IReadOnlyList<Subscription> subscriptions)
  {
    return JsonSerializer.Serialize(new
    {
      success = true,
      errors = Array.Empty<object>(),
      messages = Array.Empty<object>(),
      result = subscriptions
    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
  }

  /// <summary>Creates a test Subscription entity.</summary>
  private static Subscription CreateTestSubscription(string id)
  {
    return new Subscription(
      Id: id,
      State: SubscriptionState.Paid,
      Price: 49.99m,
      Currency: "USD",
      Frequency: SubscriptionFrequency.Monthly,
      RatePlan: new RatePlan("plan-id", "Test Plan", "USD"),
      CurrentPeriodStart: DateTime.UtcNow.AddDays(-15),
      CurrentPeriodEnd: DateTime.UtcNow.AddDays(15));
  }

  #endregion
}
