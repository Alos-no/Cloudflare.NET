namespace Cloudflare.NET.Tests.IntegrationTests;

using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Subscriptions;
using Subscriptions.Models;
using Xunit.Abstractions;


/// <summary>
///   Contains integration tests for the <see cref="SubscriptionsApi"/> class.
///   These tests interact with the live Cloudflare API and require credentials.
/// </summary>
/// <remarks>
///   <para>
///     <b>Important:</b> These tests are READ-ONLY to avoid incurring billing charges.
///     Subscription creation/modification tests are commented out as they may have cost implications.
///   </para>
///   <para>
///     <b>Billing Permissions:</b> These tests require an API token with Billing Read permissions.
///     Missing permissions will be caught by the PermissionValidationTests that run first.
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class AccountSubscriptionsApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly ISubscriptionsApi _sut;

  /// <summary>The settings loaded from the test configuration.</summary>
  private readonly TestCloudflareSettings _settings;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="AccountSubscriptionsApiIntegrationTests"/> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public AccountSubscriptionsApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut      = fixture.SubscriptionsApi;
    _settings = TestConfiguration.CloudflareSettings;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region List Subscriptions Tests (I01-I04)

  /// <summary>I01: Verifies that subscriptions can be listed successfully.</summary>
  [IntegrationTest]
  public async Task ListAccountSubscriptionsAsync_ReturnsSubscriptions()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountSubscriptionsAsync(accountId);

    // Assert - Verify the API contract (returns valid collection)
    result.Should().NotBeNull("API should return a valid response");
  }

  /// <summary>I02: Verifies that listing returns valid collection (empty or with subscriptions).</summary>
  [IntegrationTest]
  public async Task ListAccountSubscriptionsAsync_ReturnsValidCollection()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountSubscriptionsAsync(accountId);

    // Assert - Either returns empty list or subscriptions - both are valid
    result.Should().NotBeNull("API should return a valid response");
    // Count can be 0 or more - both are valid states
  }

  /// <summary>I03: Verifies that subscriptions have valid state property when present.</summary>
  [IntegrationTest]
  public async Task ListAccountSubscriptionsAsync_SubscriptionsHaveState()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountSubscriptionsAsync(accountId);

    // Assert
    result.Should().NotBeNull("API should return a valid response");
    result.Should().NotBeEmpty("account should have at least one subscription");

    foreach (var sub in result)
    {
      sub.State.Value.Should().NotBeNullOrEmpty("each subscription should have a valid state");
    }
  }

  /// <summary>I04: Verifies that subscriptions have rate plan structure when present.</summary>
  [IntegrationTest]
  public async Task ListAccountSubscriptionsAsync_SubscriptionsHaveRatePlan()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountSubscriptionsAsync(accountId);

    // Assert
    result.Should().NotBeNull("API should return a valid response");
    result.Should().NotBeEmpty("account should have at least one subscription");

    foreach (var sub in result)
    {
      sub.RatePlan.Should().NotBeNull("rate plan should not be null");
      sub.RatePlan.Id.Should().NotBeNullOrEmpty("rate plan should have an ID");
      sub.RatePlan.PublicName.Should().NotBeNullOrEmpty("rate plan should have a public name");
    }
  }

  #endregion


  #region Subscription Model Tests (I05-I08)

  /// <summary>I05: Verifies that subscription currency is populated when present.</summary>
  [IntegrationTest]
  public async Task ListAccountSubscriptionsAsync_SubscriptionsHaveCurrency()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountSubscriptionsAsync(accountId);

    // Assert
    result.Should().NotBeNull("API should return a valid response");
    result.Should().NotBeEmpty("account should have at least one subscription");

    foreach (var sub in result)
    {
      sub.Currency.Should().NotBeNullOrEmpty("each subscription should have a currency");
    }
  }

  /// <summary>I06: Verifies that subscription frequency is populated when present.</summary>
  [IntegrationTest]
  public async Task ListAccountSubscriptionsAsync_SubscriptionsHaveFrequency()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountSubscriptionsAsync(accountId);

    // Assert
    result.Should().NotBeNull("API should return a valid response");
    result.Should().NotBeEmpty("account should have at least one subscription");

    foreach (var sub in result)
    {
      sub.Frequency.Value.Should().NotBeNullOrEmpty("each subscription should have a valid frequency");
    }
  }

  /// <summary>I07: Verifies that subscription period dates are valid when present.</summary>
  /// <remarks>
  ///   Free plan subscriptions do not have period dates (they are perpetual).
  ///   Paid plan subscriptions have billing cycle dates.
  /// </remarks>
  [IntegrationTest(Skip = "Requires paid plan - Consider Dev account or WireMock - Free plan subscriptions do not have period dates")]
  public async Task ListAccountSubscriptionsAsync_ReturnsPeriodDates()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountSubscriptionsAsync(accountId);

    // Assert
    result.Should().NotBeNull("API should return a valid response");
    result.Should().NotBeEmpty("account should have at least one subscription");

    foreach (var sub in result)
    {
      sub.CurrentPeriodStart.Should().NotBeNull("paid subscription should have period start date");
      sub.CurrentPeriodEnd.Should().NotBeNull("paid subscription should have period end date");
      sub.CurrentPeriodEnd!.Value.Should().BeAfter(sub.CurrentPeriodStart!.Value,
        "period end date should be after start date");
    }
  }

  /// <summary>I08: Verifies that subscription component values have valid structure when present.</summary>
  [IntegrationTest]
  public async Task ListAccountSubscriptionsAsync_ReturnsComponentValues()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountSubscriptionsAsync(accountId);

    // Assert
    result.Should().NotBeNull("API should return a valid response");
    result.Should().NotBeEmpty("account should have at least one subscription");

    foreach (var sub in result)
    {
      // ComponentValues may be null for some subscription types - only verify structure when present.
      if (sub.ComponentValues != null)
      {
        foreach (var component in sub.ComponentValues)
        {
          component.Name.Should().NotBeNullOrEmpty("component should have a name");
        }
      }
    }
  }

  #endregion


  #region Error Handling Tests (I09-I12)

  /// <summary>I09: Verifies that an error is returned for non-existent subscription update.</summary>
  [IntegrationTest]
  [CloudflareInternalBug(
    BugDescription = "Cloudflare returns 405 MethodNotAllowed instead of 404 NotFound for non-existent subscriptions. " +
                     "The error message 'PUT method not allowed for the api_token authentication scheme' is misleading - " +
                     "it actually means 'resource not found'.",
    ReferenceUrl = "https://community.cloudflare.com/t/put-method-not-allowed-for-the-api-token-authentication-scheme/381292")]
  public async Task UpdateAccountSubscriptionAsync_NonExistent_ThrowsError()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentSubId = "non-existent-subscription-id-12345";
    var request = new UpdateAccountSubscriptionRequest(Frequency: SubscriptionFrequency.Monthly);

    // Act
    var act = () => _sut.UpdateAccountSubscriptionAsync(accountId, nonExistentSubId, request);

    // Assert - Cloudflare returns 405 MethodNotAllowed (misleadingly) instead of 404 NotFound
    // See: https://community.cloudflare.com/t/put-method-not-allowed-for-the-api-token-authentication-scheme/381292
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed);
  }

  /// <summary>I10: Verifies that an error is returned for non-existent subscription delete.</summary>
  [IntegrationTest]
  [CloudflareInternalBug(
    BugDescription = "Cloudflare returns 405 MethodNotAllowed instead of 404 NotFound for non-existent subscriptions. " +
                     "The error message 'DELETE method not allowed for the api_token authentication scheme' is misleading - " +
                     "it actually means 'resource not found'.",
    ReferenceUrl = "https://community.cloudflare.com/t/put-method-not-allowed-for-the-api-token-authentication-scheme/381292")]
  public async Task DeleteAccountSubscriptionAsync_NonExistent_ThrowsError()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentSubId = "non-existent-subscription-id-67890";

    // Act
    var act = () => _sut.DeleteAccountSubscriptionAsync(accountId, nonExistentSubId);

    // Assert - Cloudflare returns 405 MethodNotAllowed (misleadingly) instead of 404 NotFound
    // See: https://community.cloudflare.com/t/put-method-not-allowed-for-the-api-token-authentication-scheme/381292
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed);
  }

  /// <summary>I11: Verifies that invalid rate plan returns API error.</summary>
  [IntegrationTest]
  public async Task CreateAccountSubscriptionAsync_InvalidRatePlan_ThrowsBadRequest()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var request = new CreateAccountSubscriptionRequest(
      RatePlan: new RatePlanReference("invalid-rate-plan-that-does-not-exist"));

    // Act
    var act = () => _sut.CreateAccountSubscriptionAsync(accountId, request);

    // Assert - Invalid rate plan references return 400 Bad Request with error code 7501.
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.BadRequest);
  }

  /// <summary>I12: Verifies that malformed subscription ID returns API error.</summary>
  [IntegrationTest]
  [CloudflareInternalBug(
    BugDescription = "Cloudflare returns 405 MethodNotAllowed instead of 400 BadRequest for malformed subscription IDs. " +
                     "The error message 'PUT method not allowed for the api_token authentication scheme' is misleading.",
    ReferenceUrl = "https://community.cloudflare.com/t/put-method-not-allowed-for-the-api-token-authentication-scheme/381292")]
  public async Task UpdateAccountSubscriptionAsync_MalformedId_ThrowsError()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var malformedId = "!!!invalid-format!!!";
    var request = new UpdateAccountSubscriptionRequest();

    // Act
    var act = () => _sut.UpdateAccountSubscriptionAsync(accountId, malformedId, request);

    // Assert - Cloudflare returns 405 MethodNotAllowed (misleadingly) instead of 400 BadRequest
    // See: https://community.cloudflare.com/t/put-method-not-allowed-for-the-api-token-authentication-scheme/381292
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed);
  }

  #endregion


  #region Write Operation Happy Path Tests (I16-I18)

  /// <summary>I16: Verifies that a subscription can be created successfully.</summary>
  /// <remarks>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Billing Write permission IS available via API tokens: https://developers.cloudflare.com/fundamentals/api/reference/permissions/</item>
  ///     <item>Creating subscriptions WILL incur charges: https://www.cloudflare.com/plans/</item>
  ///     <item>Pro: $25/month, Business: $250/month per zone</item>
  ///   </list>
  ///   <para>
  ///     <b>Warning:</b> When enabled, this test will create a billable subscription.
  ///     Requires Billing Write permission AND will incur real charges.
  ///   </para>
  /// </remarks>
  [IntegrationTest(Skip = "Requires paid plan - Consider Dev account or WireMock - Creates billable subscription")]
  public async Task CreateAccountSubscriptionAsync_ReturnsCreatedSubscription()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var request = new CreateAccountSubscriptionRequest(
      RatePlan: new RatePlanReference("test_rate_plan_id"), // Replace with valid rate plan ID
      Frequency: SubscriptionFrequency.Monthly);

    // Act
    var result = await _sut.CreateAccountSubscriptionAsync(accountId, request);

    // Assert - Verify the subscription was created with expected properties
    result.Should().NotBeNull("API should return the created subscription");
    result.Id.Should().NotBeNullOrEmpty("created subscription should have an ID");
    result.RatePlan.Should().NotBeNull("created subscription should have a rate plan");
    result.Frequency.Should().Be(SubscriptionFrequency.Monthly, "frequency should match the request");

    // Cleanup - Delete the created subscription to avoid billing
    // await _sut.DeleteAccountSubscriptionAsync(accountId, result.Id);
  }

  /// <summary>I17: Verifies that a subscription can be updated successfully.</summary>
  /// <remarks>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Billing Write permission IS available via API tokens: https://developers.cloudflare.com/fundamentals/api/reference/permissions/</item>
  ///     <item>Updating subscriptions may affect billing: https://developers.cloudflare.com/billing/billing-policy/</item>
  ///     <item>Plan changes are pro-rated within billing cycle</item>
  ///   </list>
  ///   <para>
  ///     <b>Note:</b> This test requires an existing subscription and Billing Write permission.
  ///   </para>
  /// </remarks>
  [IntegrationTest(Skip = "Requires paid plan - Consider Dev account or WireMock - Modifies billing")]
  public async Task UpdateAccountSubscriptionAsync_ReturnsUpdatedSubscription()
  {
    // Arrange - Get an existing subscription to update
    var accountId = _settings.AccountId;
    var subscriptions = await _sut.ListAccountSubscriptionsAsync(accountId);
    subscriptions.Should().NotBeEmpty("test requires an existing subscription to update");
    var subscriptionToUpdate = subscriptions[0];

    var request = new UpdateAccountSubscriptionRequest(
      Frequency: SubscriptionFrequency.Yearly);

    // Act
    var result = await _sut.UpdateAccountSubscriptionAsync(accountId, subscriptionToUpdate.Id, request);

    // Assert - Verify the subscription was updated
    result.Should().NotBeNull("API should return the updated subscription");
    result.Id.Should().Be(subscriptionToUpdate.Id, "subscription ID should match");
    result.Frequency.Should().Be(SubscriptionFrequency.Yearly, "frequency should be updated");
  }

  /// <summary>I18: Verifies that a subscription can be deleted successfully.</summary>
  /// <remarks>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Billing Write permission IS available via API tokens: https://developers.cloudflare.com/fundamentals/api/reference/permissions/</item>
  ///     <item>Deleting subscriptions cancels paid services</item>
  ///   </list>
  ///   <para>
  ///     <b>Warning:</b> This test will permanently cancel a subscription. Only run with a
  ///     test subscription that was created specifically for testing.
  ///   </para>
  /// </remarks>
  [IntegrationTest(Skip = "Requires paid plan - Consider Dev account or WireMock - Cancels subscription")]
  public async Task DeleteAccountSubscriptionAsync_DeletesSuccessfully()
  {
    // Arrange - Create a subscription to delete (or use an existing test subscription)
    var accountId = _settings.AccountId;
    // In a real test, you would first create a subscription, then delete it
    var subscriptionIdToDelete = "test-subscription-id"; // Replace with actual subscription ID

    // Act
    await _sut.DeleteAccountSubscriptionAsync(accountId, subscriptionIdToDelete);

    // Assert - Verify the subscription is deleted by attempting to list and not finding it
    var subscriptions = await _sut.ListAccountSubscriptionsAsync(accountId);
    subscriptions.Should().NotContain(s => s.Id == subscriptionIdToDelete,
      "deleted subscription should no longer appear in the list");
  }

  #endregion


  #region State Tests (I13-I15)

  /// <summary>I13: Verifies that paid subscriptions can be identified when present.</summary>
  [IntegrationTest]
  public async Task ListAccountSubscriptionsAsync_CanIdentifyPaidSubscriptions()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountSubscriptionsAsync(accountId);

    // Assert - Verify API contract: subscriptions should have identifiable states
    result.Should().NotBeNull("API should return a valid response");

    // Paid subscriptions should have valid structure if present
    var paidSubscriptions = result.Where(s => s.State == SubscriptionState.Paid).ToList();
    foreach (var sub in paidSubscriptions)
    {
      sub.Id.Should().NotBeNullOrEmpty("paid subscription should have an ID");
      sub.State.Should().Be(SubscriptionState.Paid, "state should be Paid");
    }
  }

  /// <summary>I14: Verifies that subscriptions have valid state values.</summary>
  [IntegrationTest]
  public async Task ListAccountSubscriptionsAsync_SubscriptionsHaveVariousStates()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountSubscriptionsAsync(accountId);

    // Assert
    result.Should().NotBeNull("API should return a valid response");
    result.Should().NotBeEmpty("account should have at least one subscription");

    foreach (var sub in result)
    {
      sub.State.Value.Should().NotBeNullOrEmpty("each subscription should have a state value");
    }
  }

  /// <summary>I15: Verifies that externally managed subscriptions have valid structure.</summary>
  [IntegrationTest]
  public async Task ListAccountSubscriptionsAsync_CanIdentifyExternallyManaged()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountSubscriptionsAsync(accountId);

    // Assert - Verify API contract: externally managed subscriptions should have valid rate plans
    result.Should().NotBeNull("API should return a valid response");

    var externallyManaged = result.Where(s => s.RatePlan?.ExternallyManaged == true).ToList();
    foreach (var sub in externallyManaged)
    {
      sub.RatePlan.Should().NotBeNull("externally managed subscription should have a rate plan");
      sub.RatePlan!.ExternallyManaged.Should().BeTrue("subscription should be marked as externally managed");
    }
  }

  #endregion
}
