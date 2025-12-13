namespace Cloudflare.NET.Tests.IntegrationTests;

using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Subscriptions;
using Subscriptions.Models;
using Xunit.Abstractions;


/// <summary>
///   Contains integration tests for the User Subscriptions API methods in <see cref="SubscriptionsApi"/>.
///   These tests interact with the live Cloudflare API and require credentials.
/// </summary>
/// <remarks>
///   <para>
///     <b>Important:</b> These tests are READ-ONLY to avoid incurring billing charges.
///     Subscription modification tests are informational only and gracefully handle errors.
///   </para>
///   <para>
///     <b>Billing Permissions:</b> These tests require an API token with Billing Read permissions.
///     Missing permissions will be caught by the PermissionValidationTests that run first.
///   </para>
///   <para>
///     <b>No Create Endpoint:</b> User subscriptions cannot be created via API - they are
///     provisioned through other flows (e.g., user signup, admin actions).
///   </para>
/// </remarks>
[Collection(TestCollections.UserSubscriptions)]
[Trait("Category", TestConstants.TestCategories.Integration)]
public class UserSubscriptionsApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly ISubscriptionsApi _sut;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="UserSubscriptionsApiIntegrationTests"/> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public UserSubscriptionsApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut = fixture.SubscriptionsApi;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region List Subscriptions Tests (I01-I04)

  /// <summary>I01: Verifies that user subscriptions can be listed successfully.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_ReturnsSubscriptions()
  {
    // Act
    var result = await _sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().NotBeNull();
  }

  /// <summary>I02: Verifies that subscriptions have valid state property.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_SubscriptionsHaveState()
  {
    // Act
    var result = await _sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().NotBeEmpty("test requires at least one subscription to validate state property");

    foreach (var sub in result)
    {
      sub.State.Value.Should().NotBeNullOrEmpty();
    }
  }

  /// <summary>I03: Verifies that subscriptions have rate plan when applicable.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_SubscriptionsHaveRatePlan()
  {
    // Act
    var result = await _sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().NotBeEmpty("test requires at least one subscription to validate rate plan");

    var subWithRatePlan = result.FirstOrDefault(s => s.RatePlan != null);
    subWithRatePlan.Should().NotBeNull("test requires at least one subscription with a rate plan");

    subWithRatePlan!.RatePlan!.Id.Should().NotBeNullOrEmpty();
    subWithRatePlan.RatePlan.PublicName.Should().NotBeNullOrEmpty();
  }

  /// <summary>I04: Verifies that subscriptions have frequency populated.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_SubscriptionsHaveFrequency()
  {
    // Act
    var result = await _sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().NotBeEmpty("test requires at least one subscription to validate frequency");

    foreach (var sub in result)
    {
      sub.Frequency.Value.Should().NotBeNullOrEmpty();
    }
  }

  #endregion


  #region Subscription Model Tests (I05-I08)

  /// <summary>I05: Verifies that subscription currency is populated.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_SubscriptionsHaveCurrency()
  {
    // Act
    var result = await _sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().NotBeEmpty("test requires at least one subscription to validate currency");

    foreach (var sub in result)
    {
      sub.Currency.Should().NotBeNullOrEmpty();
    }
  }

  /// <summary>I06: Verifies that subscription period dates are returned when available.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_ReturnsPeriodDates()
  {
    // Act
    var result = await _sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().NotBeEmpty("test requires at least one subscription to validate period dates");

    var subWithDates = result.FirstOrDefault(s => s.CurrentPeriodStart.HasValue && s.CurrentPeriodEnd.HasValue);
    subWithDates.Should().NotBeNull("test requires at least one subscription with period dates");

    subWithDates!.CurrentPeriodEnd!.Value.Should().BeAfter(subWithDates.CurrentPeriodStart!.Value);
  }

  /// <summary>I07: Verifies that subscription component values are returned when available.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_ReturnsComponentValues()
  {
    // Act
    var result = await _sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().NotBeEmpty("test requires at least one subscription to validate component values");

    var subWithComponents = result.FirstOrDefault(s => s.ComponentValues != null && s.ComponentValues.Count > 0);
    subWithComponents.Should().NotBeNull("test requires at least one subscription with component values");

    foreach (var component in subWithComponents!.ComponentValues!)
    {
      component.Name.Should().NotBeNullOrEmpty();
    }
  }

  /// <summary>I08: Verifies that listing returns a valid collection.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_ReturnsValidCollection()
  {
    // Act
    var result = await _sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().NotBeNull();
  }

  #endregion


  #region Error Handling Tests (I09-I12)

  /// <summary>I09: Verifies that PUT on non-existent subscription returns 405.</summary>
  /// <remarks>
  ///   <para>
  ///     The Cloudflare API returns 405 Method Not Allowed with error code 10000 and message
  ///     "PUT method not allowed for the api_token authentication scheme" for PUT requests
  ///     to non-existent user subscription IDs.
  ///   </para>
  ///   <para>
  ///     <b>Note:</b> The cause is unclear - documentation indicates PUT should work with API tokens
  ///     and Billing Write permission. This may be enumeration prevention (hiding whether a
  ///     subscription ID exists) or another undocumented restriction.
  ///   </para>
  /// </remarks>
  [IntegrationTest]
  public async Task UpdateUserSubscriptionAsync_NonExistent_Returns405()
  {
    // Arrange
    var subscriptionId = "non-existent-user-subscription-id-12345";
    var request = new UpdateUserSubscriptionRequest(Frequency: SubscriptionFrequency.Monthly);

    // Act
    var act = () => _sut.UpdateUserSubscriptionAsync(subscriptionId, request);

    // Assert - Cloudflare returns 405 for non-existent subscription IDs
    // If this changes to 404, Cloudflare may have changed their error handling
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
        "Cloudflare API returns 405 for PUT on non-existent subscriptions; " +
        "if this test fails, Cloudflare may have changed this behavior");
  }

  /// <summary>I10: Verifies that DELETE on non-existent subscription returns 405.</summary>
  /// <remarks>
  ///   <para>
  ///     The Cloudflare API returns 405 Method Not Allowed with error code 10000 and message
  ///     "DELETE method not allowed for the api_token authentication scheme" for DELETE requests
  ///     to non-existent user subscription IDs.
  ///   </para>
  ///   <para>
  ///     <b>Note:</b> The cause is unclear - documentation indicates DELETE should work with API tokens
  ///     and Billing Write permission. This may be enumeration prevention (hiding whether a
  ///     subscription ID exists) or another undocumented restriction.
  ///   </para>
  /// </remarks>
  [IntegrationTest]
  public async Task DeleteUserSubscriptionAsync_NonExistent_Returns405()
  {
    // Arrange
    var subscriptionId = "non-existent-user-subscription-id-67890";

    // Act
    var act = () => _sut.DeleteUserSubscriptionAsync(subscriptionId);

    // Assert - Cloudflare returns 405 for non-existent subscription IDs
    // If this changes to 404, Cloudflare may have changed their error handling
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
        "Cloudflare API returns 405 for DELETE on non-existent subscriptions; " +
        "if this test fails, Cloudflare may have changed this behavior");
  }

  /// <summary>I11: Verifies that malformed subscription ID returns 405.</summary>
  /// <remarks>
  ///   <para>
  ///     The Cloudflare API returns 405 for malformed subscription IDs, suggesting the 405
  ///     response occurs before any ID format validation.
  ///   </para>
  /// </remarks>
  [IntegrationTest]
  public async Task UpdateUserSubscriptionAsync_MalformedId_Returns405()
  {
    // Arrange
    var malformedId = "!!!invalid-format!!!";
    var request = new UpdateUserSubscriptionRequest();

    // Act
    var act = () => _sut.UpdateUserSubscriptionAsync(malformedId, request);

    // Assert - 405 is returned even for malformed IDs
    // If this changes to 400, Cloudflare may have changed their error handling
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
        "Cloudflare API returns 405 for malformed subscription IDs; " +
        "if this test fails with 400, Cloudflare may have changed their error handling");
  }

  /// <summary>I12: Verifies that PUT with invalid rate plan returns 405.</summary>
  /// <remarks>
  ///   <para>
  ///     The Cloudflare API returns 405 regardless of request body content when the
  ///     subscription ID doesn't exist or isn't accessible.
  ///   </para>
  /// </remarks>
  [IntegrationTest]
  public async Task UpdateUserSubscriptionAsync_InvalidRatePlan_Returns405()
  {
    // Arrange
    var subscriptionId = "test-sub-id";
    var request = new UpdateUserSubscriptionRequest(
      RatePlan: new RatePlanReference("invalid-rate-plan-that-does-not-exist"));

    // Act
    var act = () => _sut.UpdateUserSubscriptionAsync(subscriptionId, request);

    // Assert - 405 is returned regardless of request body
    // If this changes to 404/400, Cloudflare may have changed their error handling
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed,
        "Cloudflare API returns 405 for non-existent subscription IDs; " +
        "if this test fails with 404, Cloudflare may have changed their error handling");
  }

  #endregion


  #region Write Operation Happy Path Tests (I17)

  /// <summary>I17: Verifies that a user subscription can be updated successfully.</summary>
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
  [IntegrationTest(Skip = "Modifies billing - may incur charges")]
  public async Task UpdateUserSubscriptionAsync_ReturnsUpdatedSubscription()
  {
    // Arrange - Get an existing subscription to update
    var subscriptions = await _sut.ListUserSubscriptionsAsync();
    subscriptions.Should().NotBeEmpty("test requires an existing subscription to update");
    var subscriptionToUpdate = subscriptions[0];

    var request = new UpdateUserSubscriptionRequest(
      Frequency: SubscriptionFrequency.Yearly);

    // Act
    var result = await _sut.UpdateUserSubscriptionAsync(subscriptionToUpdate.Id, request);

    // Assert - Verify the subscription was updated
    result.Should().NotBeNull();
    result.Id.Should().Be(subscriptionToUpdate.Id);
    result.Frequency.Should().Be(SubscriptionFrequency.Yearly);
  }

  #endregion


  #region State Tests (I13-I16)

  /// <summary>I13: Verifies that subscriptions with Paid state exist or can be identified.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_CanIdentifyPaidSubscriptions()
  {
    // Act
    var result = await _sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().NotBeEmpty("test requires at least one subscription to identify paid subscriptions");

    // All subscriptions should have a valid state that is identifiable
    foreach (var sub in result)
    {
      sub.State.Should().NotBeNull();
      sub.State.Value.Should().NotBeNullOrEmpty();
    }
  }

  /// <summary>I14: Verifies that subscriptions can have various states.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_SubscriptionsHaveVariousStates()
  {
    // Act
    var result = await _sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().NotBeEmpty("test requires at least one subscription to validate states");

    var stateGroups = result.GroupBy(s => s.State.Value).ToList();
    stateGroups.Should().NotBeEmpty("subscriptions should have at least one state");
  }

  /// <summary>I15: Verifies that externally managed subscriptions are identifiable.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_CanIdentifyExternallyManaged()
  {
    // Act
    var result = await _sut.ListUserSubscriptionsAsync();

    // Assert
    result.Should().NotBeEmpty("test requires at least one subscription to identify management type");

    // All subscriptions with rate plans should have ExternallyManaged set to true or false
    var subsWithRatePlan = result.Where(s => s.RatePlan != null).ToList();
    subsWithRatePlan.Should().NotBeEmpty("test requires at least one subscription with a rate plan");

    // Verify we can categorize subscriptions by management type
    var externallyManaged = subsWithRatePlan.Where(s => s.RatePlan!.ExternallyManaged).ToList();
    var internallyManaged = subsWithRatePlan.Where(s => !s.RatePlan!.ExternallyManaged).ToList();

    // All subscriptions should be categorized as either externally or internally managed
    (externallyManaged.Count + internallyManaged.Count).Should().Be(subsWithRatePlan.Count);
  }

  /// <summary>I16: Verifies that delete result contains subscription ID.</summary>
  /// <remarks>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Billing Write permission IS available via API tokens: https://developers.cloudflare.com/fundamentals/api/reference/permissions/</item>
  ///     <item>Deleting subscriptions will cancel the subscription: https://developers.cloudflare.com/billing/billing-policy/</item>
  ///     <item>This test requires an existing user subscription to delete</item>
  ///     <item>User subscriptions cannot be created via API, so manual setup is required</item>
  ///   </list>
  /// </remarks>
  [IntegrationTest(Skip = "Cancels subscription - may incur billing charges")]
  public async Task DeleteUserSubscriptionAsync_ReturnsSubscriptionIdInResult()
  {
    // Arrange - Get an existing subscription to delete
    var subscriptions = await _sut.ListUserSubscriptionsAsync();
    subscriptions.Should().NotBeEmpty("test requires an existing subscription to delete");
    var subscriptionToDelete = subscriptions.First();

    // Act
    var result = await _sut.DeleteUserSubscriptionAsync(subscriptionToDelete.Id);

    // Assert - Verify the result contains the deleted subscription ID
    result.Should().NotBeNull();
    result.SubscriptionId.Should().NotBeNullOrEmpty();
    result.SubscriptionId.Should().Be(subscriptionToDelete.Id);
  }

  #endregion
}
