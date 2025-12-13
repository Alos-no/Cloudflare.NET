namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using Cloudflare.NET.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Shared.Fixtures;
using Subscriptions;
using Subscriptions.Models;
using Xunit.Abstractions;


/// <summary>Contains unit tests for the <see cref="SubscriptionsApi"/> class.</summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class AccountSubscriptionsApiUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly ILoggerFactory _loggerFactory;

  #endregion


  #region Constructors

  public AccountSubscriptionsApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion


  #region Request Construction Tests (U01-U08)

  /// <summary>U01: Verifies that ListAccountSubscriptionsAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var successResponse = CreateListResponse(Array.Empty<Subscription>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAccountSubscriptionsAsync(accountId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/subscriptions");
    capturedRequest.RequestUri.Query.Should().BeEmpty();
  }

  /// <summary>U02: Verifies that CreateAccountSubscriptionAsync sends a POST request with correct body.</summary>
  [Fact]
  public async Task CreateAccountSubscriptionAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var request = new CreateAccountSubscriptionRequest(
      RatePlan: new RatePlanReference("enterprise_plan_id"));
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription("sub-1"));
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, async (req, _) =>
    {
      capturedRequest = req;
      capturedBody = await req.Content!.ReadAsStringAsync();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateAccountSubscriptionAsync(accountId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/subscriptions");
    capturedBody.Should().NotBeNullOrEmpty();
    capturedBody.Should().Contain("\"rate_plan\"");
    capturedBody.Should().Contain("\"id\":\"enterprise_plan_id\"");
  }

  /// <summary>U03: Verifies that CreateAccountSubscriptionAsync includes frequency when specified.</summary>
  [Fact]
  public async Task CreateAccountSubscriptionAsync_WithFrequency_IncludesFrequencyInBody()
  {
    // Arrange
    var accountId = "test-account-id";
    var request = new CreateAccountSubscriptionRequest(
      RatePlan: new RatePlanReference("plan_id"),
      Frequency: SubscriptionFrequency.Monthly);
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription("sub-1"));
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, async (req, _) =>
    {
      capturedBody = await req.Content!.ReadAsStringAsync();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateAccountSubscriptionAsync(accountId, request);

    // Assert
    capturedBody.Should().Contain("\"frequency\":\"monthly\"");
  }

  /// <summary>U04: Verifies that CreateAccountSubscriptionAsync includes component_values when specified.</summary>
  [Fact]
  public async Task CreateAccountSubscriptionAsync_WithComponents_IncludesComponentsInBody()
  {
    // Arrange
    var accountId = "test-account-id";
    var request = new CreateAccountSubscriptionRequest(
      RatePlan: new RatePlanReference("plan_id"),
      ComponentValues: new[]
      {
        new SubscriptionComponentValue("page_rules", 50),
        new SubscriptionComponentValue("workers", 10)
      });
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription("sub-1"));
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, async (req, _) =>
    {
      capturedBody = await req.Content!.ReadAsStringAsync();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateAccountSubscriptionAsync(accountId, request);

    // Assert
    capturedBody.Should().Contain("\"component_values\"");
    capturedBody.Should().Contain("\"name\":\"page_rules\"");
    capturedBody.Should().Contain("\"value\":50");
    capturedBody.Should().Contain("\"name\":\"workers\"");
    capturedBody.Should().Contain("\"value\":10");
  }

  /// <summary>U05: Verifies that UpdateAccountSubscriptionAsync sends a PUT request to the correct endpoint.</summary>
  [Fact]
  public async Task UpdateAccountSubscriptionAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var subscriptionId = "sub-123";
    var request = new UpdateAccountSubscriptionRequest(
      RatePlan: new RatePlanReference("new_plan_id"));
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription(subscriptionId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateAccountSubscriptionAsync(accountId, subscriptionId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/subscriptions/{subscriptionId}");
  }

  /// <summary>U06: Verifies that UpdateAccountSubscriptionAsync includes only rate_plan when specified.</summary>
  [Fact]
  public async Task UpdateAccountSubscriptionAsync_OnlyRatePlan_IncludesOnlyRatePlanInBody()
  {
    // Arrange
    var request = new UpdateAccountSubscriptionRequest(
      RatePlan: new RatePlanReference("new_plan"));
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription("sub"));
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, async (req, _) =>
    {
      capturedBody = await req.Content!.ReadAsStringAsync();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateAccountSubscriptionAsync("acc", "sub", request);

    // Assert
    capturedBody.Should().Contain("\"rate_plan\"");
    capturedBody.Should().NotContain("\"frequency\"");
    capturedBody.Should().NotContain("\"component_values\"");
  }

  /// <summary>U07: Verifies that UpdateAccountSubscriptionAsync includes only frequency when specified.</summary>
  [Fact]
  public async Task UpdateAccountSubscriptionAsync_OnlyFrequency_IncludesOnlyFrequencyInBody()
  {
    // Arrange
    var request = new UpdateAccountSubscriptionRequest(
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
    await sut.UpdateAccountSubscriptionAsync("acc", "sub", request);

    // Assert
    capturedBody.Should().Contain("\"frequency\":\"yearly\"");
    capturedBody.Should().NotContain("\"rate_plan\"");
    capturedBody.Should().NotContain("\"component_values\"");
  }

  /// <summary>U08: Verifies that DeleteAccountSubscriptionAsync sends a DELETE request to the correct endpoint.</summary>
  [Fact]
  public async Task DeleteAccountSubscriptionAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId = "test-account-id";
    var subscriptionId = "sub-456";
    var successResponse = HttpFixtures.CreateSuccessResponse(new object());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.DeleteAccountSubscriptionAsync(accountId, subscriptionId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{accountId}/subscriptions/{subscriptionId}");
  }

  #endregion


  #region Response Deserialization Tests (U09-U28)

  /// <summary>U09: Verifies that Subscription full model deserializes all properties correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesFullModel()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": [{
        ""id"": ""sub-full"",
        ""state"": ""Paid"",
        ""price"": 200.00,
        ""currency"": ""USD"",
        ""frequency"": ""monthly"",
        ""rate_plan"": {
          ""id"": ""plan-enterprise"",
          ""public_name"": ""Enterprise Plan"",
          ""currency"": ""USD"",
          ""scope"": ""account"",
          ""externally_managed"": false
        },
        ""current_period_start"": ""2025-01-01T00:00:00Z"",
        ""current_period_end"": ""2025-02-01T00:00:00Z"",
        ""component_values"": [
          { ""name"": ""page_rules"", ""value"": 100, ""default"": 10, ""price"": 5.00 }
        ]
      }]
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc123");

    // Assert
    result.Should().HaveCount(1);
    var sub = result[0];
    sub.Id.Should().Be("sub-full");
    sub.State.Should().Be(SubscriptionState.Paid);
    sub.Price.Should().Be(200.00m);
    sub.Currency.Should().Be("USD");
    sub.Frequency.Should().Be(SubscriptionFrequency.Monthly);
    sub.RatePlan.Should().NotBeNull();
    sub.RatePlan!.Id.Should().Be("plan-enterprise");
    sub.RatePlan.PublicName.Should().Be("Enterprise Plan");
    sub.RatePlan.Currency.Should().Be("USD");
    sub.RatePlan.Scope.Should().Be("account");
    sub.RatePlan.ExternallyManaged.Should().BeFalse();
    sub.CurrentPeriodStart.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    sub.CurrentPeriodEnd.Should().Be(new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc));
    sub.ComponentValues.Should().HaveCount(1);
    sub.ComponentValues![0].Name.Should().Be("page_rules");
    sub.ComponentValues[0].Value.Should().Be(100);
    sub.ComponentValues[0].Default.Should().Be(10);
    sub.ComponentValues[0].Price.Should().Be(5.00m);
  }

  /// <summary>U10: Verifies that Subscription with minimal fields deserializes correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesMinimalModel()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": [{
        ""id"": ""sub-minimal"",
        ""state"": ""Trial"",
        ""price"": 0,
        ""currency"": ""USD"",
        ""frequency"": ""monthly""
      }]
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc123");

    // Assert
    result.Should().HaveCount(1);
    var sub = result[0];
    sub.Id.Should().Be("sub-minimal");
    sub.RatePlan.Should().BeNull();
    sub.CurrentPeriodStart.Should().BeNull();
    sub.CurrentPeriodEnd.Should().BeNull();
    sub.ComponentValues.Should().BeNull();
  }

  /// <summary>U11: Verifies that SubscriptionState.Trial deserializes correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesStateTrial()
  {
    // Arrange
    var jsonResponse = CreateListResponseJson("sub-1", "Trial");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    result[0].State.Should().Be(SubscriptionState.Trial);
  }

  /// <summary>U12: Verifies that SubscriptionState.Provisioned deserializes correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesStateProvisioned()
  {
    // Arrange
    var jsonResponse = CreateListResponseJson("sub-1", "Provisioned");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    result[0].State.Should().Be(SubscriptionState.Provisioned);
  }

  /// <summary>U13: Verifies that SubscriptionState.Paid deserializes correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesStatePaid()
  {
    // Arrange
    var jsonResponse = CreateListResponseJson("sub-1", "Paid");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    result[0].State.Should().Be(SubscriptionState.Paid);
  }

  /// <summary>U14: Verifies that SubscriptionState.AwaitingPayment deserializes correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesStateAwaitingPayment()
  {
    // Arrange
    var jsonResponse = CreateListResponseJson("sub-1", "AwaitingPayment");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    result[0].State.Should().Be(SubscriptionState.AwaitingPayment);
  }

  /// <summary>U15: Verifies that SubscriptionState.Cancelled deserializes correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesStateCancelled()
  {
    // Arrange
    var jsonResponse = CreateListResponseJson("sub-1", "Cancelled");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    result[0].State.Should().Be(SubscriptionState.Cancelled);
  }

  /// <summary>U16: Verifies that SubscriptionState.Failed deserializes correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesStateFailed()
  {
    // Arrange
    var jsonResponse = CreateListResponseJson("sub-1", "Failed");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    result[0].State.Should().Be(SubscriptionState.Failed);
  }

  /// <summary>U17: Verifies that SubscriptionState.Expired deserializes correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesStateExpired()
  {
    // Arrange
    var jsonResponse = CreateListResponseJson("sub-1", "Expired");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    result[0].State.Should().Be(SubscriptionState.Expired);
  }

  /// <summary>U18: Verifies that unknown SubscriptionState values are handled by extensible enum.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesUnknownState()
  {
    // Arrange
    var jsonResponse = CreateListResponseJson("sub-1", "FutureUnknownState");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    result[0].State.Value.Should().Be("FutureUnknownState");
  }

  /// <summary>U19: Verifies that SubscriptionFrequency.Weekly deserializes correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesFrequencyWeekly()
  {
    // Arrange
    var jsonResponse = CreateListResponseJsonWithFrequency("sub-1", "weekly");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    result[0].Frequency.Should().Be(SubscriptionFrequency.Weekly);
  }

  /// <summary>U20: Verifies that SubscriptionFrequency.Monthly deserializes correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesFrequencyMonthly()
  {
    // Arrange
    var jsonResponse = CreateListResponseJsonWithFrequency("sub-1", "monthly");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    result[0].Frequency.Should().Be(SubscriptionFrequency.Monthly);
  }

  /// <summary>U21: Verifies that SubscriptionFrequency.Quarterly deserializes correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesFrequencyQuarterly()
  {
    // Arrange
    var jsonResponse = CreateListResponseJsonWithFrequency("sub-1", "quarterly");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    result[0].Frequency.Should().Be(SubscriptionFrequency.Quarterly);
  }

  /// <summary>U22: Verifies that SubscriptionFrequency.Yearly deserializes correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesFrequencyYearly()
  {
    // Arrange
    var jsonResponse = CreateListResponseJsonWithFrequency("sub-1", "yearly");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    result[0].Frequency.Should().Be(SubscriptionFrequency.Yearly);
  }

  /// <summary>U23: Verifies that unknown SubscriptionFrequency values are handled by extensible enum.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesUnknownFrequency()
  {
    // Arrange
    var jsonResponse = CreateListResponseJsonWithFrequency("sub-1", "biannual");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    result[0].Frequency.Value.Should().Be("biannual");
  }

  /// <summary>U24: Verifies that nested RatePlan deserializes correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesRatePlan()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": [{
        ""id"": ""sub-1"",
        ""state"": ""Paid"",
        ""price"": 100,
        ""currency"": ""USD"",
        ""frequency"": ""monthly"",
        ""rate_plan"": {
          ""id"": ""plan-pro"",
          ""public_name"": ""Pro Plan"",
          ""currency"": ""EUR"",
          ""scope"": ""zone""
        }
      }]
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    var ratePlan = result[0].RatePlan;
    ratePlan.Should().NotBeNull();
    ratePlan!.Id.Should().Be("plan-pro");
    ratePlan.PublicName.Should().Be("Pro Plan");
    ratePlan.Currency.Should().Be("EUR");
    ratePlan.Scope.Should().Be("zone");
  }

  /// <summary>U25: Verifies that RatePlan.ExternallyManaged deserializes correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesExternallyManagedRatePlan()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": [{
        ""id"": ""sub-1"",
        ""state"": ""Paid"",
        ""price"": 100,
        ""currency"": ""USD"",
        ""frequency"": ""monthly"",
        ""rate_plan"": {
          ""id"": ""plan-partner"",
          ""public_name"": ""Partner Plan"",
          ""currency"": ""USD"",
          ""externally_managed"": true
        }
      }]
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    result[0].RatePlan!.ExternallyManaged.Should().BeTrue();
  }

  /// <summary>U26: Verifies that nested SubscriptionComponent list deserializes correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesComponents()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": [{
        ""id"": ""sub-1"",
        ""state"": ""Paid"",
        ""price"": 100,
        ""currency"": ""USD"",
        ""frequency"": ""monthly"",
        ""component_values"": [
          { ""name"": ""page_rules"", ""value"": 50, ""default"": 10, ""price"": 2.50 },
          { ""name"": ""workers"", ""value"": 5 }
        ]
      }]
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    var components = result[0].ComponentValues;
    components.Should().HaveCount(2);
    components![0].Name.Should().Be("page_rules");
    components[0].Value.Should().Be(50);
    components[0].Default.Should().Be(10);
    components[0].Price.Should().Be(2.50m);
    components[1].Name.Should().Be("workers");
    components[1].Value.Should().Be(5);
    components[1].Default.Should().BeNull();
    components[1].Price.Should().BeNull();
  }

  /// <summary>U27: Verifies that period dates deserialize correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesPeriodDates()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": [{
        ""id"": ""sub-1"",
        ""state"": ""Paid"",
        ""price"": 100,
        ""currency"": ""USD"",
        ""frequency"": ""monthly"",
        ""current_period_start"": ""2025-06-01T12:30:45Z"",
        ""current_period_end"": ""2025-07-01T12:30:45Z""
      }]
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    result[0].CurrentPeriodStart.Should().Be(new DateTime(2025, 6, 1, 12, 30, 45, DateTimeKind.Utc));
    result[0].CurrentPeriodEnd.Should().Be(new DateTime(2025, 7, 1, 12, 30, 45, DateTimeKind.Utc));
  }

  /// <summary>U28: Verifies that empty subscription list deserializes correctly.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_DeserializesEmptyList()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": []
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListAccountSubscriptionsAsync("acc");

    // Assert
    result.Should().BeEmpty();
  }

  #endregion


  #region Error Handling Tests (U29-U32)

  /// <summary>U29: Verifies API error envelope throws CloudflareApiException.</summary>
  [Fact]
  public async Task CreateAccountSubscriptionAsync_WhenApiError_ThrowsCloudflareApiException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10000, ""message"": ""Invalid rate plan"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(() =>
      sut.CreateAccountSubscriptionAsync("acc", new CreateAccountSubscriptionRequest(new RatePlanReference("invalid"))));
    exception.Errors.Should().HaveCount(1);
    exception.Errors[0].Code.Should().Be(10000);
  }

  /// <summary>U30: Verifies invalid rate plan API error.</summary>
  [Fact]
  public async Task CreateAccountSubscriptionAsync_InvalidRatePlan_ThrowsCloudflareApiException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 1001, ""message"": ""Rate plan not found"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(() =>
      sut.CreateAccountSubscriptionAsync("acc", new CreateAccountSubscriptionRequest(new RatePlanReference("invalid-plan"))));
    exception.Errors.Should().Contain(e => e.Message.Contains("Rate plan"));
  }

  /// <summary>U31: Verifies multiple errors in response are captured in CloudflareApiException.</summary>
  [Fact]
  public async Task UpdateAccountSubscriptionAsync_WhenMultipleErrors_CapturesAllErrors()
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
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(() =>
      sut.UpdateAccountSubscriptionAsync("acc", "sub", new UpdateAccountSubscriptionRequest()));
    exception.Errors.Should().HaveCount(2);
  }

  #endregion


  #region URL Encoding Tests (U33-U34)

  /// <summary>U33: Verifies that ListAccountSubscriptionsAsync properly URL-encodes the account ID.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_WithSpecialChars_UrlEncodesAccountId()
  {
    // Arrange
    var accountId = "abc+def/ghi";
    var successResponse = CreateListResponse(Array.Empty<Subscription>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAccountSubscriptionsAsync(accountId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.AbsolutePath.Should().Contain("abc%2Bdef%2Fghi");
  }

  /// <summary>U34: Verifies that UpdateAccountSubscriptionAsync properly URL-encodes the subscription ID.</summary>
  [Fact]
  public async Task UpdateAccountSubscriptionAsync_WithSpecialChars_UrlEncodesSubscriptionId()
  {
    // Arrange
    var accountId = "acc-123";
    var subscriptionId = "sub+with/special";
    var successResponse = HttpFixtures.CreateSuccessResponse(CreateTestSubscription(subscriptionId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateAccountSubscriptionAsync(accountId, subscriptionId, new UpdateAccountSubscriptionRequest());

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.AbsolutePath.Should().Contain("sub%2Bwith%2Fspecial");
  }

  #endregion


  #region Parameter Validation Tests (U41-U48)

  /// <summary>U41: Verifies that ListAccountSubscriptionsAsync throws on null accountId.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_NullAccountId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.ListAccountSubscriptionsAsync(null!));
  }

  /// <summary>U42: Verifies that ListAccountSubscriptionsAsync throws on whitespace accountId.</summary>
  [Fact]
  public async Task ListAccountSubscriptionsAsync_WhitespaceAccountId_ThrowsArgumentException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() => sut.ListAccountSubscriptionsAsync("   "));
  }

  /// <summary>U43: Verifies that CreateAccountSubscriptionAsync throws on null accountId.</summary>
  [Fact]
  public async Task CreateAccountSubscriptionAsync_NullAccountId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
      sut.CreateAccountSubscriptionAsync(null!, new CreateAccountSubscriptionRequest(new RatePlanReference("plan"))));
  }

  /// <summary>U44: Verifies that CreateAccountSubscriptionAsync throws on null request.</summary>
  [Fact]
  public async Task CreateAccountSubscriptionAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
      sut.CreateAccountSubscriptionAsync("acc", null!));
  }

  /// <summary>U45: Verifies that UpdateAccountSubscriptionAsync throws on null accountId.</summary>
  [Fact]
  public async Task UpdateAccountSubscriptionAsync_NullAccountId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
      sut.UpdateAccountSubscriptionAsync(null!, "sub", new UpdateAccountSubscriptionRequest()));
  }

  /// <summary>U46: Verifies that UpdateAccountSubscriptionAsync throws on null subscriptionId.</summary>
  [Fact]
  public async Task UpdateAccountSubscriptionAsync_NullSubscriptionId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
      sut.UpdateAccountSubscriptionAsync("acc", null!, new UpdateAccountSubscriptionRequest()));
  }

  /// <summary>U47: Verifies that DeleteAccountSubscriptionAsync throws on null accountId.</summary>
  [Fact]
  public async Task DeleteAccountSubscriptionAsync_NullAccountId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
      sut.DeleteAccountSubscriptionAsync(null!, "sub"));
  }

  /// <summary>U48: Verifies that DeleteAccountSubscriptionAsync throws on null subscriptionId.</summary>
  [Fact]
  public async Task DeleteAccountSubscriptionAsync_NullSubscriptionId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new SubscriptionsApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
      sut.DeleteAccountSubscriptionAsync("acc", null!));
  }

  #endregion


  #region Helper Methods

  /// <summary>Creates a test Subscription instance with default or custom values.</summary>
  private static Subscription CreateTestSubscription(string? id = null, SubscriptionState? state = null)
  {
    return new Subscription(
      id ?? "test-sub-123",
      state ?? SubscriptionState.Paid,
      Price: 100.00m,
      Currency: "USD",
      Frequency: SubscriptionFrequency.Monthly,
      RatePlan: new RatePlan("plan-123", "Test Plan", "USD")
    );
  }

  /// <summary>Creates a success response JSON for a list of subscriptions.</summary>
  private static string CreateListResponse(IEnumerable<Subscription> items)
  {
    return JsonSerializer.Serialize(
      new
      {
        success = true,
        errors = Array.Empty<object>(),
        messages = Array.Empty<object>(),
        result = items
      },
      new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
  }

  /// <summary>Creates a JSON response with a single subscription having the specified state.</summary>
  private static string CreateListResponseJson(string id, string state)
  {
    return $@"{{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": [{{
        ""id"": ""{id}"",
        ""state"": ""{state}"",
        ""price"": 100,
        ""currency"": ""USD"",
        ""frequency"": ""monthly""
      }}]
    }}";
  }

  /// <summary>Creates a JSON response with a single subscription having the specified frequency.</summary>
  private static string CreateListResponseJsonWithFrequency(string id, string frequency)
  {
    return $@"{{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": [{{
        ""id"": ""{id}"",
        ""state"": ""Paid"",
        ""price"": 100,
        ""currency"": ""USD"",
        ""frequency"": ""{frequency}""
      }}]
    }}";
  }

  #endregion
}
