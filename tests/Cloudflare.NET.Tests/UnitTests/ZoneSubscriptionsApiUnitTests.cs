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
///   Contains unit tests for Zone Subscriptions operations in <see cref="SubscriptionsApi"/>.
/// </summary>
/// <remarks>
///   <para>
///     These tests verify the zone-scoped subscription endpoints (/zones/{zoneId}/subscription).
///     Zone subscriptions include Get, Create, Update, and ListAvailableRatePlans operations.
///   </para>
///   <para>
///     Note: Zones always have a subscription (at minimum, a Free plan), so there is no delete endpoint.
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class ZoneSubscriptionsApiUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly ILoggerFactory _loggerFactory;

  #endregion


  #region Constructors

  public ZoneSubscriptionsApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion


  #region Request Construction Tests (U01-U09)

  /// <summary>U01: Verifies that GetZoneSubscriptionAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task GetZoneSubscriptionAsync_SendsCorrectRequest()
  {
    // Arrange
    var zoneId = "zone-123";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription("sub"));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.GetZoneSubscriptionAsync(zoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/zones/{zoneId}/subscription");
  }

  /// <summary>U02: Verifies that CreateZoneSubscriptionAsync sends a POST request to the correct endpoint.</summary>
  [Fact]
  public async Task CreateZoneSubscriptionAsync_SendsCorrectRequest()
  {
    // Arrange
    var zoneId = "zone-123";
    var request = new CreateZoneSubscriptionRequest(
      RatePlan: new RatePlanReference("pro"));
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription("sub"));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateZoneSubscriptionAsync(zoneId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/zones/{zoneId}/subscription");
  }

  /// <summary>U03: Verifies that CreateZoneSubscriptionAsync includes frequency in request body.</summary>
  [Fact]
  public async Task CreateZoneSubscriptionAsync_WithFrequency_IncludesFrequencyInBody()
  {
    // Arrange
    var request = new CreateZoneSubscriptionRequest(
      RatePlan: new RatePlanReference("pro"),
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
    await sut.CreateZoneSubscriptionAsync("zone", request);

    // Assert
    capturedBody.Should().Contain("\"frequency\":\"yearly\"");
    capturedBody.Should().Contain("\"rate_plan\"");
  }

  /// <summary>U04: Verifies that CreateZoneSubscriptionAsync includes component_values in request body.</summary>
  [Fact]
  public async Task CreateZoneSubscriptionAsync_WithComponents_IncludesComponentsInBody()
  {
    // Arrange
    var request = new CreateZoneSubscriptionRequest(
      RatePlan: new RatePlanReference("pro"),
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
    await sut.CreateZoneSubscriptionAsync("zone", request);

    // Assert
    capturedBody.Should().Contain("\"component_values\"");
    capturedBody.Should().Contain("\"name\":\"page_rules\"");
    capturedBody.Should().Contain("\"value\":100");
  }

  /// <summary>U05: Verifies that UpdateZoneSubscriptionAsync sends a PUT request to the correct endpoint.</summary>
  [Fact]
  public async Task UpdateZoneSubscriptionAsync_SendsCorrectRequest()
  {
    // Arrange
    var zoneId = "zone-456";
    var request = new UpdateZoneSubscriptionRequest(
      RatePlan: new RatePlanReference("business"));
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription("sub"));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateZoneSubscriptionAsync(zoneId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/zones/{zoneId}/subscription");
  }

  /// <summary>U06: Verifies that UpdateZoneSubscriptionAsync with only rate plan includes only rate_plan in body.</summary>
  [Fact]
  public async Task UpdateZoneSubscriptionAsync_RatePlanOnly_IncludesOnlyRatePlanInBody()
  {
    // Arrange
    var request = new UpdateZoneSubscriptionRequest(
      RatePlan: new RatePlanReference("free"));
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription("sub"));
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, async (req, _) =>
    {
      capturedBody = await req.Content!.ReadAsStringAsync();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateZoneSubscriptionAsync("zone", request);

    // Assert
    capturedBody.Should().Contain("\"rate_plan\"");
    capturedBody.Should().Contain("\"id\":\"free\"");
    capturedBody.Should().NotContain("\"frequency\"");
    capturedBody.Should().NotContain("\"component_values\"");
  }

  /// <summary>U07: Verifies that UpdateZoneSubscriptionAsync with only frequency includes only frequency in body.</summary>
  [Fact]
  public async Task UpdateZoneSubscriptionAsync_FrequencyOnly_IncludesOnlyFrequencyInBody()
  {
    // Arrange
    var request = new UpdateZoneSubscriptionRequest(
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
    await sut.UpdateZoneSubscriptionAsync("zone", request);

    // Assert
    capturedBody.Should().Contain("\"frequency\":\"monthly\"");
    capturedBody.Should().NotContain("\"rate_plan\"");
    capturedBody.Should().NotContain("\"component_values\"");
  }

  /// <summary>U08: Verifies that ListAvailableRatePlansAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task ListAvailableRatePlansAsync_SendsCorrectRequest()
  {
    // Arrange
    var zoneId = "zone-789";
    var successResponse = CreateRatePlansResponse(Array.Empty<ZoneRatePlan>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAvailableRatePlansAsync(zoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/zones/{zoneId}/available_rate_plans");
    capturedRequest.RequestUri.Query.Should().BeEmpty();
  }

  /// <summary>U09: Verifies that CreateZoneSubscriptionRequest omits null fields.</summary>
  [Fact]
  public async Task CreateZoneSubscriptionAsync_OnlyRatePlan_OmitsNullFields()
  {
    // Arrange
    var request = new CreateZoneSubscriptionRequest(
      RatePlan: new RatePlanReference("pro"));
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription("sub"));
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, async (req, _) =>
    {
      capturedBody = await req.Content!.ReadAsStringAsync();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateZoneSubscriptionAsync("zone", request);

    // Assert
    capturedBody.Should().Contain("\"rate_plan\"");
    capturedBody.Should().NotContain("\"frequency\"");
    capturedBody.Should().NotContain("\"component_values\"");
  }

  #endregion


  #region Response Deserialization Tests (U10-U20)

  /// <summary>U10: Verifies that Subscription full model deserializes correctly for GetZoneSubscription.</summary>
  [Fact]
  public async Task GetZoneSubscriptionAsync_FullModel_DeserializesCorrectly()
  {
    // Arrange
    var responseJson = """
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": {
          "id": "zone-sub-123",
          "state": "Paid",
          "price": 199.99,
          "currency": "USD",
          "frequency": "monthly",
          "rate_plan": {
            "id": "pro",
            "public_name": "Pro Plan",
            "currency": "USD",
            "scope": "zone",
            "externally_managed": false
          },
          "current_period_start": "2024-01-01T00:00:00Z",
          "current_period_end": "2024-02-01T00:00:00Z",
          "component_values": [
            { "name": "page_rules", "value": 50, "default": 10, "price": 5.00 }
          ]
        }
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneSubscriptionAsync("zone-123");

    // Assert
    result.Id.Should().Be("zone-sub-123");
    result.State.Should().Be(SubscriptionState.Paid);
    result.Price.Should().Be(199.99m);
    result.Currency.Should().Be("USD");
    result.Frequency.Should().Be(SubscriptionFrequency.Monthly);
    result.RatePlan.Should().NotBeNull();
    result.RatePlan!.Id.Should().Be("pro");
    result.RatePlan.PublicName.Should().Be("Pro Plan");
    result.RatePlan.Scope.Should().Be("zone");
    result.RatePlan.ExternallyManaged.Should().BeFalse();
    result.CurrentPeriodStart.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    result.CurrentPeriodEnd.Should().Be(new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc));
    result.ComponentValues.Should().HaveCount(1);
    result.ComponentValues![0].Name.Should().Be("page_rules");
  }

  /// <summary>U11: Verifies that Subscription minimal model deserializes correctly.</summary>
  [Fact]
  public async Task GetZoneSubscriptionAsync_MinimalModel_DeserializesCorrectly()
  {
    // Arrange
    var responseJson = """
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": {
          "id": "zone-sub-minimal",
          "state": "Paid",
          "price": 0.00,
          "currency": "USD",
          "frequency": "monthly"
        }
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneSubscriptionAsync("zone-123");

    // Assert
    result.Id.Should().Be("zone-sub-minimal");
    result.RatePlan.Should().BeNull();
    result.CurrentPeriodStart.Should().BeNull();
    result.CurrentPeriodEnd.Should().BeNull();
    result.ComponentValues.Should().BeNull();
  }

  /// <summary>U12: Verifies that all SubscriptionState values deserialize correctly.</summary>
  [Theory]
  [InlineData("Trial", "Trial")]
  [InlineData("Provisioned", "Provisioned")]
  [InlineData("Paid", "Paid")]
  [InlineData("AwaitingPayment", "AwaitingPayment")]
  [InlineData("Cancelled", "Cancelled")]
  [InlineData("Failed", "Failed")]
  [InlineData("Expired", "Expired")]
  [InlineData("NewFutureState", "NewFutureState")]
  public async Task GetZoneSubscriptionAsync_StateValues_DeserializeCorrectly(string stateValue, string expectedValue)
  {
    // Arrange
    var responseJson = $$"""
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": {
          "id": "zone-sub",
          "state": "{{stateValue}}",
          "price": 0.00,
          "currency": "USD",
          "frequency": "monthly"
        }
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneSubscriptionAsync("zone");

    // Assert
    result.State.Value.Should().Be(expectedValue);
  }

  /// <summary>U13: Verifies that all SubscriptionFrequency values deserialize correctly.</summary>
  [Theory]
  [InlineData("weekly", "weekly")]
  [InlineData("monthly", "monthly")]
  [InlineData("quarterly", "quarterly")]
  [InlineData("yearly", "yearly")]
  [InlineData("biannual", "biannual")]
  public async Task GetZoneSubscriptionAsync_FrequencyValues_DeserializeCorrectly(string frequencyValue, string expectedValue)
  {
    // Arrange
    var responseJson = $$"""
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": {
          "id": "zone-sub",
          "state": "Paid",
          "price": 0.00,
          "currency": "USD",
          "frequency": "{{frequencyValue}}"
        }
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneSubscriptionAsync("zone");

    // Assert
    result.Frequency.Value.Should().Be(expectedValue);
  }

  /// <summary>U14: Verifies that RatePlan nested object deserializes correctly.</summary>
  [Fact]
  public async Task CreateZoneSubscriptionAsync_RatePlanNested_DeserializesCorrectly()
  {
    // Arrange
    var responseJson = """
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": {
          "id": "zone-sub",
          "state": "Paid",
          "price": 500.00,
          "currency": "EUR",
          "frequency": "yearly",
          "rate_plan": {
            "id": "enterprise",
            "public_name": "Enterprise Plan",
            "currency": "EUR",
            "scope": "zone",
            "externally_managed": true
          }
        }
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);
    var request = new CreateZoneSubscriptionRequest(RatePlan: new RatePlanReference("enterprise"));

    // Act
    var result = await sut.CreateZoneSubscriptionAsync("zone", request);

    // Assert
    result.RatePlan.Should().NotBeNull();
    result.RatePlan!.Id.Should().Be("enterprise");
    result.RatePlan.PublicName.Should().Be("Enterprise Plan");
    result.RatePlan.Currency.Should().Be("EUR");
    result.RatePlan.Scope.Should().Be("zone");
    result.RatePlan.ExternallyManaged.Should().BeTrue();
  }

  /// <summary>U15: Verifies that SubscriptionComponent nested array deserializes correctly.</summary>
  [Fact]
  public async Task UpdateZoneSubscriptionAsync_ComponentsNested_DeserializeCorrectly()
  {
    // Arrange
    var responseJson = """
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": {
          "id": "zone-sub",
          "state": "Paid",
          "price": 99.00,
          "currency": "USD",
          "frequency": "monthly",
          "component_values": [
            { "name": "page_rules", "value": 100, "default": 20, "price": 10.00 },
            { "name": "dedicated_certificates", "value": 5, "default": 1, "price": 5.00 }
          ]
        }
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);
    var request = new UpdateZoneSubscriptionRequest();

    // Act
    var result = await sut.UpdateZoneSubscriptionAsync("zone", request);

    // Assert
    result.ComponentValues.Should().HaveCount(2);
    var components = result.ComponentValues!;
    components[0].Name.Should().Be("page_rules");
    components[0].Value.Should().Be(100);
    components[1].Name.Should().Be("dedicated_certificates");
    components[1].Value.Should().Be(5);
  }

  /// <summary>U16: Verifies that ZoneRatePlan full model deserializes correctly.</summary>
  [Fact]
  public async Task ListAvailableRatePlansAsync_FullModel_DeserializesCorrectly()
  {
    // Arrange
    var responseJson = """
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": [{
          "id": "pro",
          "name": "Pro Plan",
          "currency": "USD",
          "duration": 1,
          "frequency": "monthly",
          "components": [
            { "name": "page_rules", "default": 20, "unit_price": 5.00 },
            { "name": "dedicated_certificates", "default": 1, "unit_price": 5.00 }
          ]
        }]
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAvailableRatePlansAsync("zone");

    // Assert
    result.Should().HaveCount(1);
    var plan = result[0];
    plan.Id.Should().Be("pro");
    plan.Name.Should().Be("Pro Plan");
    plan.Currency.Should().Be("USD");
    plan.Duration.Should().Be(1);
    plan.Frequency.Should().Be(SubscriptionFrequency.Monthly);
    plan.Components.Should().HaveCount(2);
  }

  /// <summary>U17: Verifies that ZoneRatePlan with components deserializes correctly.</summary>
  [Fact]
  public async Task ListAvailableRatePlansAsync_WithComponents_DeserializesCorrectly()
  {
    // Arrange
    var responseJson = """
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": [{
          "id": "business",
          "name": "Business Plan",
          "currency": "USD",
          "duration": 12,
          "frequency": "yearly",
          "components": [
            { "name": "page_rules", "default": 50, "unit_price": 3.00 }
          ]
        }]
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAvailableRatePlansAsync("zone");

    // Assert
    result.Should().HaveCount(1);
    result[0].Components.Should().HaveCount(1);
    var components = result[0].Components!;
    components[0].Name.Should().Be("page_rules");
    components[0].Default.Should().Be(50);
    components[0].UnitPrice.Should().Be(3.00m);
  }

  /// <summary>U18: Verifies that RatePlanComponent deserializes correctly.</summary>
  [Fact]
  public async Task ListAvailableRatePlansAsync_RatePlanComponent_DeserializesCorrectly()
  {
    // Arrange
    var responseJson = """
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": [{
          "id": "pro",
          "name": "Pro",
          "currency": "USD",
          "duration": 1,
          "frequency": "monthly",
          "components": [
            { "name": "workers", "default": 10, "unit_price": 0.50 }
          ]
        }]
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAvailableRatePlansAsync("zone");

    // Assert
    var component = result[0].Components![0];
    component.Name.Should().Be("workers");
    component.Default.Should().Be(10);
    component.UnitPrice.Should().Be(0.50m);
  }

  /// <summary>U19: Verifies that empty rate plans list deserializes correctly.</summary>
  [Fact]
  public async Task ListAvailableRatePlansAsync_EmptyList_ReturnsEmptyList()
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
    var result = await sut.ListAvailableRatePlansAsync("zone");

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
  }

  /// <summary>U20: Verifies that multiple rate plans deserialize correctly.</summary>
  [Fact]
  public async Task ListAvailableRatePlansAsync_MultiplePlans_DeserializesCorrectly()
  {
    // Arrange
    var responseJson = """
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": [
          { "id": "free", "name": "Free", "currency": "USD", "duration": 1, "frequency": "monthly" },
          { "id": "pro", "name": "Pro", "currency": "USD", "duration": 1, "frequency": "monthly" },
          { "id": "business", "name": "Business", "currency": "USD", "duration": 1, "frequency": "monthly" },
          { "id": "enterprise", "name": "Enterprise", "currency": "USD", "duration": 12, "frequency": "yearly" }
        ]
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAvailableRatePlansAsync("zone");

    // Assert
    result.Should().HaveCount(4);
    result.Select(p => p.Id).Should().BeEquivalentTo(new[] { "free", "pro", "business", "enterprise" });
  }

  #endregion


  #region Error Handling Tests (U21-U24)

  /// <summary>U21: Verifies that API error (success=false) throws CloudflareApiException.</summary>
  [Fact]
  public async Task CreateZoneSubscriptionAsync_ApiError_ThrowsCloudflareApiException()
  {
    // Arrange - 2xx status with success=false throws CloudflareApiException.
    var responseJson = HttpFixtures.CreateErrorResponse(1001, "Invalid subscription");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);
    var request = new CreateZoneSubscriptionRequest(RatePlan: new RatePlanReference("invalid"));

    // Act
    var action = async () => await sut.CreateZoneSubscriptionAsync("zone", request);

    // Assert
    var exception = await action.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().ContainSingle(e => e.Code == 1001);
  }

  /// <summary>U22: Verifies that invalid rate plan error throws CloudflareApiException.</summary>
  [Fact]
  public async Task CreateZoneSubscriptionAsync_InvalidRatePlan_ThrowsCloudflareApiException()
  {
    // Arrange
    var responseJson = HttpFixtures.CreateErrorResponse(1002, "Invalid rate plan");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);
    var request = new CreateZoneSubscriptionRequest(RatePlan: new RatePlanReference("nonexistent_plan"));

    // Act
    var action = async () => await sut.CreateZoneSubscriptionAsync("zone", request);

    // Assert
    var exception = await action.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().ContainSingle(e => e.Code == 1002);
  }

  /// <summary>U23: Verifies that multiple API errors are all captured.</summary>
  [Fact]
  public async Task UpdateZoneSubscriptionAsync_MultipleErrors_CapturesAllErrors()
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
    var request = new UpdateZoneSubscriptionRequest();

    // Act
    var action = async () => await sut.UpdateZoneSubscriptionAsync("zone", request);

    // Assert
    var exception = await action.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().HaveCount(2);
    exception.Which.Errors.Should().Contain(e => e.Code == 1001);
    exception.Which.Errors.Should().Contain(e => e.Code == 1002);
  }

  #endregion


  #region URL Encoding Tests (U25-U26)

  /// <summary>U25: Verifies that special characters in zoneId are URL encoded.</summary>
  [Fact]
  public async Task GetZoneSubscriptionAsync_SpecialChars_UrlEncodesCorrectly()
  {
    // Arrange
    var zoneId = "zone/with:special&chars?";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription("sub"));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.GetZoneSubscriptionAsync(zoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    var path = capturedRequest!.RequestUri!.AbsolutePath;
    path.Should().Contain("zone%2Fwith%3Aspecial%26chars%3F");
    path.Should().NotContain("zone/with:special&chars?");
  }

  /// <summary>U26: Verifies that spaces in zoneId are URL encoded.</summary>
  [Fact]
  public async Task CreateZoneSubscriptionAsync_SpacesInId_UrlEncodesCorrectly()
  {
    // Arrange
    var zoneId = "zone with spaces";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription("sub"));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);
    var request = new CreateZoneSubscriptionRequest(RatePlan: new RatePlanReference("pro"));

    // Act
    await sut.CreateZoneSubscriptionAsync(zoneId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    var path = capturedRequest!.RequestUri!.AbsolutePath;
    path.Should().Contain("zone%20with%20spaces");
    path.Should().NotContain("zone with spaces");
  }

  #endregion


  #region Parameter Validation Tests (U31-U38)

  /// <summary>U31: Verifies that GetZoneSubscriptionAsync throws for null zoneId.</summary>
  [Fact]
  public async Task GetZoneSubscriptionAsync_NullZoneId_ThrowsArgumentException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var action = async () => await sut.GetZoneSubscriptionAsync(null!);

    // Assert
    await action.Should().ThrowAsync<ArgumentException>()
      .WithParameterName("zoneId");
  }

  /// <summary>U32: Verifies that GetZoneSubscriptionAsync throws for whitespace zoneId.</summary>
  [Fact]
  public async Task GetZoneSubscriptionAsync_WhitespaceZoneId_ThrowsArgumentException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var action = async () => await sut.GetZoneSubscriptionAsync("   ");

    // Assert
    await action.Should().ThrowAsync<ArgumentException>()
      .WithParameterName("zoneId");
  }

  /// <summary>U33: Verifies that CreateZoneSubscriptionAsync throws for null zoneId.</summary>
  [Fact]
  public async Task CreateZoneSubscriptionAsync_NullZoneId_ThrowsArgumentException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);
    var request = new CreateZoneSubscriptionRequest(RatePlan: new RatePlanReference("pro"));

    // Act
    var action = async () => await sut.CreateZoneSubscriptionAsync(null!, request);

    // Assert
    await action.Should().ThrowAsync<ArgumentException>()
      .WithParameterName("zoneId");
  }

  /// <summary>U34: Verifies that CreateZoneSubscriptionAsync throws for null request.</summary>
  [Fact]
  public async Task CreateZoneSubscriptionAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var action = async () => await sut.CreateZoneSubscriptionAsync("zone", null!);

    // Assert
    await action.Should().ThrowAsync<ArgumentNullException>()
      .WithParameterName("request");
  }

  /// <summary>U35: Verifies that UpdateZoneSubscriptionAsync throws for null zoneId.</summary>
  [Fact]
  public async Task UpdateZoneSubscriptionAsync_NullZoneId_ThrowsArgumentException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);
    var request = new UpdateZoneSubscriptionRequest();

    // Act
    var action = async () => await sut.UpdateZoneSubscriptionAsync(null!, request);

    // Assert
    await action.Should().ThrowAsync<ArgumentException>()
      .WithParameterName("zoneId");
  }

  /// <summary>U36: Verifies that UpdateZoneSubscriptionAsync throws for null request.</summary>
  [Fact]
  public async Task UpdateZoneSubscriptionAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var action = async () => await sut.UpdateZoneSubscriptionAsync("zone", null!);

    // Assert
    await action.Should().ThrowAsync<ArgumentNullException>()
      .WithParameterName("request");
  }

  /// <summary>U37: Verifies that ListAvailableRatePlansAsync throws for null zoneId.</summary>
  [Fact]
  public async Task ListAvailableRatePlansAsync_NullZoneId_ThrowsArgumentException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var action = async () => await sut.ListAvailableRatePlansAsync(null!);

    // Assert
    await action.Should().ThrowAsync<ArgumentException>()
      .WithParameterName("zoneId");
  }

  /// <summary>U38: Verifies that ListAvailableRatePlansAsync throws for whitespace zoneId.</summary>
  [Fact]
  public async Task ListAvailableRatePlansAsync_WhitespaceZoneId_ThrowsArgumentException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var action = async () => await sut.ListAvailableRatePlansAsync("   ");

    // Assert
    await action.Should().ThrowAsync<ArgumentException>()
      .WithParameterName("zoneId");
  }

  #endregion


  #region Helpers

  /// <summary>Creates a list response JSON for ZoneRatePlans.</summary>
  private static string CreateRatePlansResponse(IReadOnlyList<ZoneRatePlan> plans)
  {
    return JsonSerializer.Serialize(new
    {
      success = true,
      errors = Array.Empty<object>(),
      messages = Array.Empty<object>(),
      result = plans
    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
  }

  /// <summary>Creates a test Subscription entity.</summary>
  private static Subscription CreateTestSubscription(string id)
  {
    return new Subscription(
      Id: id,
      State: SubscriptionState.Paid,
      Price: 99.99m,
      Currency: "USD",
      Frequency: SubscriptionFrequency.Monthly,
      RatePlan: new RatePlan("pro", "Pro Plan", "USD", "zone"),
      CurrentPeriodStart: DateTime.UtcNow.AddDays(-15),
      CurrentPeriodEnd: DateTime.UtcNow.AddDays(15));
  }

  #endregion
}
