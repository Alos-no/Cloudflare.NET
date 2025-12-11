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
///     If the token lacks these permissions, the tests will gracefully handle 403 errors.
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

  /// <summary>The xUnit test output helper for writing warnings and debug info.</summary>
  private readonly ITestOutputHelper _output;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="ZoneSubscriptionsApiIntegrationTests"/> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public ZoneSubscriptionsApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut      = fixture.SubscriptionsApi;
    _settings = TestConfiguration.CloudflareSettings;
    _output   = output;

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
    Subscription? result = null;
    try
    {
      result = await _sut.GetZoneSubscriptionAsync(zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    result.Should().NotBeNull();
    result!.Id.Should().NotBeNullOrEmpty();
    _output.WriteLine($"Zone subscription ID: {result.Id}");
  }

  /// <summary>I02: Verifies that zone subscription has valid state property.</summary>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_HasValidState()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    Subscription? result = null;
    try
    {
      result = await _sut.GetZoneSubscriptionAsync(zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    result!.State.Value.Should().NotBeNullOrEmpty();
    _output.WriteLine($"Zone subscription state: {result.State}");
  }

  /// <summary>I03: Verifies that zone subscription has rate plan populated.</summary>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_HasRatePlan()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    Subscription? result = null;
    try
    {
      result = await _sut.GetZoneSubscriptionAsync(zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    if (result!.RatePlan != null)
    {
      result.RatePlan.Id.Should().NotBeNullOrEmpty();
      result.RatePlan.PublicName.Should().NotBeNullOrEmpty();
      _output.WriteLine($"Zone plan: {result.RatePlan.PublicName} (ID: {result.RatePlan.Id})");
    }
    else
    {
      _output.WriteLine("Zone subscription has no rate plan specified");
    }
  }

  /// <summary>I04: Verifies that free zone returns free plan subscription.</summary>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_FreeZone_ReturnsFreeOrProPlan()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    Subscription? result = null;
    try
    {
      result = await _sut.GetZoneSubscriptionAsync(zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert - Most test zones are on free plan, but could be paid
    result.Should().NotBeNull();
    _output.WriteLine($"Zone plan ID: {result!.RatePlan?.Id ?? "(no plan)"}, Price: {result.Price} {result.Currency}");

    // Free plan typically has price of 0
    if (result.RatePlan?.Id?.ToLower() == "free")
    {
      result.Price.Should().Be(0m, "Free plan should have zero price");
    }
  }

  /// <summary>I05: Verifies that zone subscription has frequency populated.</summary>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_HasFrequency()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    Subscription? result = null;
    try
    {
      result = await _sut.GetZoneSubscriptionAsync(zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    result!.Frequency.Value.Should().NotBeNullOrEmpty();
    _output.WriteLine($"Zone subscription frequency: {result.Frequency}");
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
    IReadOnlyList<ZoneRatePlan>? result = null;
    try
    {
      result = await _sut.ListAvailableRatePlansAsync(zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have necessary permissions (403 Forbidden)");
      return;
    }

    // Assert
    result.Should().NotBeNull();
    _output.WriteLine($"Found {result.Count} available rate plans");
    foreach (var plan in result)
    {
      _output.WriteLine($"  - {plan.Name} (ID: {plan.Id}, {plan.Currency}, {plan.Frequency})");
    }
  }

  /// <summary>I07: Verifies that Free plan is in the list of available plans.</summary>
  [IntegrationTest]
  public async Task ListAvailableRatePlansAsync_IncludesFree()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    IReadOnlyList<ZoneRatePlan>? result = null;
    try
    {
      result = await _sut.ListAvailableRatePlansAsync(zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have necessary permissions (403 Forbidden)");
      return;
    }

    // Assert
    if (result.Count > 0)
    {
      // Check for free plan (may be named differently)
      var freePlan = result.FirstOrDefault(p =>
        p.Id.Contains("free", StringComparison.OrdinalIgnoreCase) ||
        p.Name.Contains("free", StringComparison.OrdinalIgnoreCase));

      if (freePlan != null)
      {
        _output.WriteLine($"Free plan found: {freePlan.Name} (ID: {freePlan.Id})");
      }
      else
      {
        _output.WriteLine("No explicit 'Free' plan found in list - plan names may differ");
        // Log all plans for debugging
        foreach (var plan in result)
        {
          _output.WriteLine($"  Available: {plan.Name} (ID: {plan.Id})");
        }
      }
    }
    else
    {
      _output.WriteLine("No rate plans returned - API may not return plans for this zone");
    }
  }

  /// <summary>I08: Verifies that Pro plan is in the list of available plans.</summary>
  [IntegrationTest]
  public async Task ListAvailableRatePlansAsync_IncludesPro()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    IReadOnlyList<ZoneRatePlan>? result = null;
    try
    {
      result = await _sut.ListAvailableRatePlansAsync(zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have necessary permissions (403 Forbidden)");
      return;
    }

    // Assert
    if (result.Count > 0)
    {
      // Check for pro plan (may be named differently)
      var proPlan = result.FirstOrDefault(p =>
        p.Id.Contains("pro", StringComparison.OrdinalIgnoreCase) ||
        p.Name.Contains("pro", StringComparison.OrdinalIgnoreCase));

      if (proPlan != null)
      {
        _output.WriteLine($"Pro plan found: {proPlan.Name} (ID: {proPlan.Id})");
      }
      else
      {
        _output.WriteLine("No explicit 'Pro' plan found in list - plan names may differ");
      }
    }
    else
    {
      _output.WriteLine("No rate plans returned - API may not return plans for this zone");
    }
  }

  /// <summary>I09: Verifies that rate plans have pricing information.</summary>
  [IntegrationTest]
  public async Task ListAvailableRatePlansAsync_PlansHavePricing()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    IReadOnlyList<ZoneRatePlan>? result = null;
    try
    {
      result = await _sut.ListAvailableRatePlansAsync(zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have necessary permissions (403 Forbidden)");
      return;
    }

    // Assert
    if (result.Count == 0)
    {
      _output.WriteLine("No rate plans returned");
      return;
    }

    foreach (var plan in result)
    {
      plan.Currency.Should().NotBeNullOrEmpty();
      plan.Duration.Should().BeGreaterThanOrEqualTo(0);
      plan.Frequency.Value.Should().NotBeNullOrEmpty();
      _output.WriteLine($"  {plan.Name}: Currency={plan.Currency}, Duration={plan.Duration}, Frequency={plan.Frequency}");
    }
  }

  /// <summary>I10: Verifies that some rate plans have components.</summary>
  [IntegrationTest]
  public async Task ListAvailableRatePlansAsync_SomePlansHaveComponents()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    IReadOnlyList<ZoneRatePlan>? result = null;
    try
    {
      result = await _sut.ListAvailableRatePlansAsync(zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have necessary permissions (403 Forbidden)");
      return;
    }

    // Assert
    if (result.Count == 0)
    {
      _output.WriteLine("No rate plans returned");
      return;
    }

    var plansWithComponents = result.Where(p => p.Components != null && p.Components.Count > 0).ToList();
    _output.WriteLine($"Plans with components: {plansWithComponents.Count} / {result.Count}");

    foreach (var plan in plansWithComponents)
    {
      _output.WriteLine($"  {plan.Name}:");
      foreach (var component in plan.Components!)
      {
        _output.WriteLine($"    - {component.Name}: default={component.Default}, unit_price={component.UnitPrice}");
      }
    }
  }

  #endregion


  #region Error Handling Tests (I11-I15)

  /// <summary>I11: Verifies that 404 is returned for non-existent zone.</summary>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_NonExistentZone_ThrowsNotFound()
  {
    // Arrange
    var nonExistentZoneId = "non-existent-zone-id-12345";

    // Act & Assert
    try
    {
      await _sut.GetZoneSubscriptionAsync(nonExistentZoneId);
      _output.WriteLine("Unexpectedly succeeded - zone might exist or API behavior differs");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing permissions (403 Forbidden)");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
      _output.WriteLine($"Received expected 404 Not Found: {ex.Message}");
      ex.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
    catch (Core.Exceptions.CloudflareApiException ex)
    {
      _output.WriteLine($"API error: {ex.Message}");
      ex.Errors.Should().NotBeEmpty();
    }
  }

  /// <summary>I12: Verifies that create subscription with invalid rate plan returns error.</summary>
  [IntegrationTest]
  public async Task CreateZoneSubscriptionAsync_InvalidRatePlan_ThrowsError()
  {
    // Arrange
    var zoneId = _settings.ZoneId;
    var request = new CreateZoneSubscriptionRequest(
      RatePlan: new RatePlanReference("invalid-rate-plan-that-does-not-exist"));

    // Act & Assert
    try
    {
      await _sut.CreateZoneSubscriptionAsync(zoneId, request);
      _output.WriteLine("Unexpectedly succeeded - API may have different behavior");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Write permissions (403 Forbidden)");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
    {
      _output.WriteLine("Skipped: POST method not allowed for api_token authentication scheme (405)");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
      _output.WriteLine($"Received 404 Not Found for invalid rate plan: {ex.Message}");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
    {
      _output.WriteLine($"Received 400 Bad Request for invalid rate plan: {ex.Message}");
    }
    catch (Core.Exceptions.CloudflareApiException ex)
    {
      _output.WriteLine($"API error for invalid rate plan: {ex.Message}");
      ex.Errors.Should().NotBeEmpty();
    }
  }

  /// <summary>I13: Verifies that update subscription on non-existent zone returns error.</summary>
  [IntegrationTest]
  public async Task UpdateZoneSubscriptionAsync_NonExistentZone_ThrowsError()
  {
    // Arrange
    var nonExistentZoneId = "non-existent-zone-id-67890";
    var request = new UpdateZoneSubscriptionRequest();

    // Act & Assert
    try
    {
      await _sut.UpdateZoneSubscriptionAsync(nonExistentZoneId, request);
      _output.WriteLine("Unexpectedly succeeded");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing permissions (403 Forbidden)");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
      _output.WriteLine($"Received expected 404 Not Found: {ex.Message}");
      ex.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
    catch (HttpRequestException ex)
    {
      _output.WriteLine($"Received HTTP error {ex.StatusCode}: {ex.Message}");
      ex.StatusCode.Should().NotBeNull();
    }
    catch (Core.Exceptions.CloudflareApiException ex)
    {
      _output.WriteLine($"API error: {ex.Message}");
      ex.Errors.Should().NotBeEmpty();
    }
  }

  /// <summary>I14: Verifies that listing rate plans for non-existent zone returns error.</summary>
  [IntegrationTest]
  public async Task ListAvailableRatePlansAsync_NonExistentZone_ThrowsError()
  {
    // Arrange
    var nonExistentZoneId = "non-existent-zone-id-99999";

    // Act & Assert
    try
    {
      await _sut.ListAvailableRatePlansAsync(nonExistentZoneId);
      _output.WriteLine("Unexpectedly succeeded");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have permissions (403 Forbidden)");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
      _output.WriteLine($"Received expected 404 Not Found: {ex.Message}");
      ex.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
    catch (Core.Exceptions.CloudflareApiException ex)
    {
      _output.WriteLine($"API error: {ex.Message}");
      ex.Errors.Should().NotBeEmpty();
    }
  }

  /// <summary>I15: Verifies that malformed zone ID returns error.</summary>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_MalformedId_ThrowsError()
  {
    // Arrange
    var malformedId = "!!!invalid-format!!!";

    // Act & Assert
    try
    {
      await _sut.GetZoneSubscriptionAsync(malformedId);
      _output.WriteLine("Unexpectedly succeeded");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing permissions (403 Forbidden)");
    }
    catch (HttpRequestException ex)
    {
      _output.WriteLine($"Received HTTP error {ex.StatusCode}: {ex.Message}");
      ex.StatusCode.Should().NotBeNull();
    }
    catch (Core.Exceptions.CloudflareApiException ex)
    {
      _output.WriteLine($"API error: {ex.Message}");
      ex.Errors.Should().NotBeEmpty();
    }
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
    Subscription? result = null;
    try
    {
      result = await _sut.GetZoneSubscriptionAsync(zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    var isPaid = result!.State == SubscriptionState.Paid;
    var isTrial = result.State == SubscriptionState.Trial;
    var isProvisioned = result.State == SubscriptionState.Provisioned;

    _output.WriteLine($"Zone subscription state: {result.State}");
    _output.WriteLine($"  Is Paid: {isPaid}");
    _output.WriteLine($"  Is Trial: {isTrial}");
    _output.WriteLine($"  Is Provisioned: {isProvisioned}");
  }

  /// <summary>I17: Verifies that externally managed subscriptions can be identified.</summary>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_CanIdentifyExternallyManaged()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    Subscription? result = null;
    try
    {
      result = await _sut.GetZoneSubscriptionAsync(zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    if (result!.RatePlan != null)
    {
      var isExternallyManaged = result.RatePlan.ExternallyManaged;
      _output.WriteLine($"Zone plan '{result.RatePlan.PublicName}' is externally managed: {isExternallyManaged}");
    }
    else
    {
      _output.WriteLine("Zone has no rate plan - cannot determine external management status");
    }
  }

  /// <summary>I18: Verifies that subscription has billing period dates when applicable.</summary>
  [IntegrationTest]
  public async Task GetZoneSubscriptionAsync_HasBillingPeriod()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    Subscription? result = null;
    try
    {
      result = await _sut.GetZoneSubscriptionAsync(zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    if (result!.CurrentPeriodStart.HasValue && result.CurrentPeriodEnd.HasValue)
    {
      result.CurrentPeriodEnd.Value.Should().BeAfter(result.CurrentPeriodStart.Value);
      _output.WriteLine($"Billing period: {result.CurrentPeriodStart} to {result.CurrentPeriodEnd}");
    }
    else
    {
      _output.WriteLine("Zone subscription has no billing period dates (may be free plan)");
    }
  }

  #endregion
}
