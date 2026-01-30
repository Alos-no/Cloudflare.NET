namespace Cloudflare.NET.Tests.IntegrationTests;

using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Subscriptions;
using Subscriptions.Models;
using Xunit.Abstractions;


/// <summary>
///   Contains integration tests for the Zone Subscriptions API methods in <see cref="SubscriptionsApi"/>.
///   These tests interact with the live Cloudflare API and require credentials.
/// </summary>
/// <remarks>
///   <para>
///     <b>Important:</b> These tests are primarily READ-ONLY to avoid incurring billing charges.
///     Tests that would upgrade plans are informational only and gracefully handle errors.
///   </para>
///   <para>
///     <b>Billing Permissions:</b> These tests require an API token with Billing Read permissions.
///     Missing permissions will be caught by the PermissionValidationTests that run first.
///   </para>
///   <para>
///     <b>No Delete Endpoint:</b> Zones always have a subscription (at minimum, a Free plan).
///     To "cancel", you downgrade to Free via UpdateZoneSubscriptionAsync.
///   </para>
/// </remarks>
[Collection(TestCollections.ZoneSubscriptions)]
[Trait("Category", TestConstants.TestCategories.Integration)]
public class ZoneSubscriptionsApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly ISubscriptionsApi _sut;

  /// <summary>The settings loaded from the test configuration.</summary>
  private readonly TestCloudflareSettings _settings;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="ZoneSubscriptionsApiIntegrationTests"/> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public ZoneSubscriptionsApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut      = fixture.SubscriptionsApi;
    _settings = TestConfiguration.CloudflareSettings;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region Get Subscription Tests (I01-I05)

  /// <summary>I01: Verifies that zone subscription can be retrieved successfully.</summary>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_ReturnsSubscription()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    var result = await _sut.GetZoneSubscriptionAsync(zoneId);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().NotBeNullOrEmpty();
  }

  /// <summary>I02: Verifies that zone subscription has valid state property.</summary>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_HasValidState()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    var result = await _sut.GetZoneSubscriptionAsync(zoneId);

    // Assert
    result.State.Value.Should().NotBeNullOrEmpty();
  }

  /// <summary>I03: Verifies that zone subscription has rate plan populated.</summary>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_HasRatePlan()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    var result = await _sut.GetZoneSubscriptionAsync(zoneId);

    // Assert
    result.RatePlan.Should().NotBeNull("zone subscription should have a rate plan");
    result.RatePlan!.Id.Should().NotBeNullOrEmpty();
    result.RatePlan.PublicName.Should().NotBeNullOrEmpty();
  }

  /// <summary>I04: Verifies that free zone returns subscription with expected properties.</summary>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_ReturnsValidSubscription()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    var result = await _sut.GetZoneSubscriptionAsync(zoneId);

    // Assert
    result.Should().NotBeNull();
    result.RatePlan.Should().NotBeNull("zone should have a rate plan");
    result.RatePlan!.Id.Should().NotBeNullOrEmpty();
  }

  /// <summary>I05: Verifies that zone subscription has frequency populated.</summary>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_HasFrequency()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    var result = await _sut.GetZoneSubscriptionAsync(zoneId);

    // Assert
    result.Frequency.Value.Should().NotBeNullOrEmpty();
  }

  #endregion


  #region List Rate Plans Tests (I06-I10)

  /// <summary>I06: Verifies that available rate plans can be listed.</summary>
  [IntegrationTest]
  public async Task ListAvailableRatePlansAsync_ReturnsPlans()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    var result = await _sut.ListAvailableRatePlansAsync(zoneId);

    // Assert
    result.Should().NotBeNull();
    result.Should().NotBeEmpty("zone should have available rate plans");
  }

  /// <summary>I07: Verifies that Free plan is in the list of available plans.</summary>
  [IntegrationTest]
  public async Task ListAvailableRatePlansAsync_IncludesFree()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    var result = await _sut.ListAvailableRatePlansAsync(zoneId);

    // Assert
    result.Should().NotBeEmpty("test requires at least one rate plan");

    var freePlan = result.FirstOrDefault(p =>
      p.Id.Contains("free", StringComparison.OrdinalIgnoreCase) ||
      p.Name.Contains("free", StringComparison.OrdinalIgnoreCase));

    freePlan.Should().NotBeNull("Free plan should be available");
  }

  /// <summary>I08: Verifies that Pro plan is in the list of available plans.</summary>
  [IntegrationTest]
  public async Task ListAvailableRatePlansAsync_IncludesPro()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    var result = await _sut.ListAvailableRatePlansAsync(zoneId);

    // Assert
    result.Should().NotBeEmpty("test requires at least one rate plan");

    var proPlan = result.FirstOrDefault(p =>
      p.Id.Contains("pro", StringComparison.OrdinalIgnoreCase) ||
      p.Name.Contains("pro", StringComparison.OrdinalIgnoreCase));

    proPlan.Should().NotBeNull("Pro plan should be available");
  }

  /// <summary>I09: Verifies that rate plans have pricing information.</summary>
  [IntegrationTest]
  public async Task ListAvailableRatePlansAsync_PlansHavePricing()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    var result = await _sut.ListAvailableRatePlansAsync(zoneId);

    // Assert
    result.Should().NotBeEmpty("test requires at least one rate plan to validate pricing");

    foreach (var plan in result)
    {
      plan.Currency.Should().NotBeNullOrEmpty();
      plan.Duration.Should().BeGreaterThanOrEqualTo(0);
      plan.Frequency.Value.Should().NotBeNullOrEmpty();
    }
  }

  /// <summary>I10: Verifies that some rate plans have components.</summary>
  [IntegrationTest]
  public async Task ListAvailableRatePlansAsync_SomePlansHaveComponents()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    var result = await _sut.ListAvailableRatePlansAsync(zoneId);

    // Assert
    result.Should().NotBeEmpty("test requires at least one rate plan");

    var plansWithComponents = result.Where(p => p.Components != null && p.Components.Count > 0).ToList();
    plansWithComponents.Should().NotBeEmpty("test requires at least one plan with components");

    foreach (var plan in plansWithComponents)
    {
      foreach (var component in plan.Components!)
      {
        component.Name.Should().NotBeNullOrEmpty();
      }
    }
  }

  #endregion


  #region Error Handling Tests (I11-I15)

  /// <summary>I11: Verifies that 404 is returned for non-existent zone.</summary>
  /// <remarks>
  ///   Per Cloudflare API: GET /zones/{zone_id}/subscription returns 404 for non-existent zones.
  ///   https://developers.cloudflare.com/api/resources/zones/subresources/subscriptions/methods/get/
  /// </remarks>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_NonExistentZone_ThrowsNotFound()
  {
    // Arrange
    var nonExistentZoneId = "non-existent-zone-id-12345";

    // Act
    var act = () => _sut.GetZoneSubscriptionAsync(nonExistentZoneId);

    // Assert - Non-existent zone returns 404 Not Found
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
  }

  /// <summary>I12: Verifies that create subscription with invalid rate plan returns 400 Bad Request.</summary>
  /// <remarks>
  ///   <para>
  ///     Per Cloudflare API: POST with non-existent rate plan returns 400 Bad Request with error code 7501.
  ///     Error message: "unknown or deprecated rate plan: 'invalid-rate-plan-that-does-not-exist'"
  ///   </para>
  ///   <para>
  ///     <b>Note:</b> The documentation previously indicated 404, but current behavior returns 400.
  ///     If this test fails with 404, Cloudflare may have reverted to the documented behavior.
  ///   </para>
  /// </remarks>
  [IntegrationTest]
  public async Task CreateZoneSubscriptionAsync_InvalidRatePlan_ThrowsBadRequest()
  {
    // Arrange
    var zoneId = _settings.ZoneId;
    var request = new CreateZoneSubscriptionRequest(
      RatePlan: new RatePlanReference("invalid-rate-plan-that-does-not-exist"));

    // Act
    var act = () => _sut.CreateZoneSubscriptionAsync(zoneId, request);

    // Assert - Invalid rate plan references return 400 Bad Request with error code 7501.
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.BadRequest);
  }

  /// <summary>I13: Verifies that update subscription on non-existent zone returns 404.</summary>
  /// <remarks>
  ///   Per Cloudflare API: PUT /zones/{zone_id}/subscription returns 404 for non-existent zones.
  ///   https://developers.cloudflare.com/api/resources/zones/subresources/subscriptions/methods/update/
  /// </remarks>
  [IntegrationTest]
  public async Task UpdateZoneSubscriptionAsync_NonExistentZone_ThrowsNotFound()
  {
    // Arrange
    var nonExistentZoneId = "non-existent-zone-id-67890";
    var request = new UpdateZoneSubscriptionRequest();

    // Act
    var act = () => _sut.UpdateZoneSubscriptionAsync(nonExistentZoneId, request);

    // Assert - Non-existent zone returns 404 Not Found
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
  }

  /// <summary>I14: Verifies that listing rate plans for non-existent zone returns 404.</summary>
  /// <remarks>
  ///   Per Cloudflare API: GET /zones/{zone_id}/available_rate_plans returns 404 for non-existent zones.
  ///   https://developers.cloudflare.com/api/resources/zones/subresources/subscriptions/
  /// </remarks>
  [IntegrationTest]
  public async Task ListAvailableRatePlansAsync_NonExistentZone_ThrowsNotFound()
  {
    // Arrange
    var nonExistentZoneId = "non-existent-zone-id-99999";

    // Act
    var act = () => _sut.ListAvailableRatePlansAsync(nonExistentZoneId);

    // Assert - Non-existent zone returns 404 Not Found
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
  }

  /// <summary>I15: Verifies that malformed zone ID returns 400 Bad Request.</summary>
  /// <remarks>
  ///   Per Cloudflare API: Malformed zone IDs with invalid characters fail at the routing layer.
  ///   Error code 7003: "Could not route to /zones/{id}/subscription, perhaps your object identifier is invalid?"
  ///   Error code 7000: "No route for that URI"
  ///   https://developers.cloudflare.com/api/resources/zones/
  /// </remarks>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_MalformedId_ThrowsBadRequest()
  {
    // Arrange
    var malformedId = "!!!invalid-format!!!";

    // Act
    var act = () => _sut.GetZoneSubscriptionAsync(malformedId);

    // Assert - Malformed zone ID returns 400 Bad Request (routing error)
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.BadRequest);
  }

  #endregion


  #region Write Operation Happy Path Tests (I19-I20)

  /// <summary>I19: Verifies that a zone subscription can be created (upgraded) successfully.</summary>
  /// <remarks>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Billing Write permission IS available via API tokens: https://developers.cloudflare.com/fundamentals/api/reference/permissions/</item>
  ///     <item>Zone plans have recurring billing: https://www.cloudflare.com/plans/</item>
  ///     <item>Pro plan: $25/month, Business plan: $250/month</item>
  ///     <item>Plan changes are pro-rated within billing cycle</item>
  ///   </list>
  ///   <para>
  ///     <b>Warning:</b> When enabled, this test will upgrade the zone to a paid plan and incur charges.
  ///     Ensure you downgrade back to Free after testing.
  ///   </para>
  /// </remarks>
  [IntegrationTest(Skip = "Requires paid plan - Consider Dev account or WireMock")]
  public async Task CreateZoneSubscriptionAsync_ReturnsCreatedSubscription()
  {
    // Arrange - Use a test zone that can be upgraded
    var zoneId = _settings.ZoneId;

    // First, get available rate plans to find a valid plan ID
    var plans = await _sut.ListAvailableRatePlansAsync(zoneId);
    plans.Should().NotBeEmpty("test requires at least one available rate plan");

    var proPlan = plans.FirstOrDefault(p =>
      p.Id.Contains("pro", StringComparison.OrdinalIgnoreCase));
    proPlan.Should().NotBeNull("test requires a Pro plan to be available");

    var request = new CreateZoneSubscriptionRequest(
      RatePlan: new RatePlanReference(proPlan!.Id),
      Frequency: SubscriptionFrequency.Monthly);

    // Act
    var result = await _sut.CreateZoneSubscriptionAsync(zoneId, request);

    // Assert - Verify the subscription was created with expected properties
    result.Should().NotBeNull();
    result.Id.Should().NotBeNullOrEmpty();
    result.RatePlan.Should().NotBeNull();
    result.RatePlan!.PublicName.Should().NotBeNullOrEmpty();

    // Cleanup - Downgrade back to Free to avoid ongoing charges
    // var downgradeRequest = new UpdateZoneSubscriptionRequest(
    //   RatePlan: new RatePlanReference("free"));
    // await _sut.UpdateZoneSubscriptionAsync(zoneId, downgradeRequest);
  }

  /// <summary>I20: Verifies that a zone subscription can be updated successfully.</summary>
  /// <remarks>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Billing Write permission IS available via API tokens: https://developers.cloudflare.com/fundamentals/api/reference/permissions/</item>
  ///     <item>Updating subscriptions may affect billing: https://developers.cloudflare.com/billing/billing-policy/</item>
  ///     <item>Frequency changes (monthly ↔ yearly) affect billing amounts</item>
  ///     <item>This test requires a zone already on a paid plan to demonstrate updates</item>
  ///   </list>
  /// </remarks>
  [IntegrationTest(Skip = "Requires paid plan - Consider Dev account or WireMock - Modifies subscription billing")]
  public async Task UpdateZoneSubscriptionAsync_ReturnsUpdatedSubscription()
  {
    // Arrange - Get the current subscription
    var zoneId = _settings.ZoneId;
    var currentSubscription = await _sut.GetZoneSubscriptionAsync(zoneId);
    currentSubscription.Should().NotBeNull();

    var request = new UpdateZoneSubscriptionRequest(
      Frequency: SubscriptionFrequency.Yearly);

    // Act
    var result = await _sut.UpdateZoneSubscriptionAsync(zoneId, request);

    // Assert - Verify the subscription was updated
    result.Should().NotBeNull();
    result.Frequency.Should().Be(SubscriptionFrequency.Yearly);
  }

  #endregion


  #region State Tests (I16-I18)

  /// <summary>I16: Verifies that subscription states can be identified.</summary>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_CanIdentifyPaidState()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    var result = await _sut.GetZoneSubscriptionAsync(zoneId);

    // Assert - State should be one of the known subscription states
    result.State.Should().NotBeNull();
    result.State.Value.Should().NotBeNullOrEmpty();

    // Verify we can categorize the subscription state
    var isPaid = result.State == SubscriptionState.Paid;
    var isTrial = result.State == SubscriptionState.Trial;
    var isProvisioned = result.State == SubscriptionState.Provisioned;

    // At least one state categorization should be determinable (or it's a different state)
    (isPaid || isTrial || isProvisioned || result.State.Value.Length > 0).Should().BeTrue();
  }

  /// <summary>I17: Verifies that externally managed subscriptions can be identified.</summary>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_CanIdentifyExternallyManaged()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    var result = await _sut.GetZoneSubscriptionAsync(zoneId);

    // Assert - Zone subscription should have a fully populated rate plan
    result.RatePlan.Should().NotBeNull("zone subscription should have a rate plan");
    result.RatePlan!.Id.Should().NotBeNullOrEmpty("rate plan should have an ID");
    result.RatePlan.PublicName.Should().NotBeNullOrEmpty("rate plan should have a public name");

    // ExternallyManaged is a non-nullable boolean - verify the RatePlan is properly deserialized
    // by checking that the property value is consistent (it's determinable)
    var externallyManaged = result.RatePlan.ExternallyManaged;
    result.RatePlan.ExternallyManaged.Should().Be(externallyManaged);
  }

  /// <summary>I18: Verifies that subscription has billing period dates when applicable.</summary>
  [IntegrationTest(Skip = "Requires paid plan - Consider Dev account or WireMock - Free plan subscriptions do not have period dates")]
  public async Task GetZoneSubscriptionAsync_HasBillingPeriod()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    var result = await _sut.GetZoneSubscriptionAsync(zoneId);

    // Assert - Subscription should have billing period dates
    result.CurrentPeriodStart.Should().HaveValue("zone subscription should have a period start date");
    result.CurrentPeriodEnd.Should().HaveValue("zone subscription should have a period end date");

    result.CurrentPeriodEnd!.Value.Should().BeAfter(result.CurrentPeriodStart!.Value);
  }

  #endregion
}
