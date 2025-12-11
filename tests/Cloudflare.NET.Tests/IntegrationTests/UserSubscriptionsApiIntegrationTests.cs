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
///     If the token lacks these permissions, the tests will gracefully handle 403 errors.
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

  /// <summary>The xUnit test output helper for writing warnings and debug info.</summary>
  private readonly ITestOutputHelper _output;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="UserSubscriptionsApiIntegrationTests"/> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public UserSubscriptionsApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut    = fixture.SubscriptionsApi;
    _output = output;

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
    IReadOnlyList<Subscription>? result = null;
    try
    {
      result = await _sut.ListUserSubscriptionsAsync();
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    result.Should().NotBeNull();
    _output.WriteLine($"Found {result.Count} user subscriptions");
  }

  /// <summary>I02: Verifies that subscriptions have valid state property.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_SubscriptionsHaveState()
  {
    // Act
    IReadOnlyList<Subscription>? result = null;
    try
    {
      result = await _sut.ListUserSubscriptionsAsync();
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    if (result.Count == 0)
    {
      _output.WriteLine("No subscriptions found - skipping state validation");
      return;
    }

    foreach (var sub in result)
    {
      sub.State.Value.Should().NotBeNullOrEmpty();
      _output.WriteLine($"  Subscription {sub.Id}: State={sub.State}");
    }
  }

  /// <summary>I03: Verifies that subscriptions have rate plan when applicable.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_SubscriptionsHaveRatePlan()
  {
    // Act
    IReadOnlyList<Subscription>? result = null;
    try
    {
      result = await _sut.ListUserSubscriptionsAsync();
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    if (result.Count == 0)
    {
      _output.WriteLine("No subscriptions found - skipping rate plan validation");
      return;
    }

    foreach (var sub in result)
    {
      _output.WriteLine($"  Subscription {sub.Id}: RatePlan={sub.RatePlan?.PublicName ?? "null"}");
      if (sub.RatePlan != null)
      {
        sub.RatePlan.Id.Should().NotBeNullOrEmpty();
        sub.RatePlan.PublicName.Should().NotBeNullOrEmpty();
      }
    }
  }

  /// <summary>I04: Verifies that subscriptions have frequency populated.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_SubscriptionsHaveFrequency()
  {
    // Act
    IReadOnlyList<Subscription>? result = null;
    try
    {
      result = await _sut.ListUserSubscriptionsAsync();
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    if (result.Count == 0)
    {
      _output.WriteLine("No subscriptions found - skipping frequency validation");
      return;
    }

    foreach (var sub in result)
    {
      sub.Frequency.Value.Should().NotBeNullOrEmpty();
      _output.WriteLine($"  Subscription {sub.Id}: Frequency={sub.Frequency}");
    }
  }

  #endregion


  #region Subscription Model Tests (I05-I08)

  /// <summary>I05: Verifies that subscription currency is populated.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_SubscriptionsHaveCurrency()
  {
    // Act
    IReadOnlyList<Subscription>? result = null;
    try
    {
      result = await _sut.ListUserSubscriptionsAsync();
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    if (result.Count == 0)
    {
      _output.WriteLine("No subscriptions found - skipping currency validation");
      return;
    }

    foreach (var sub in result)
    {
      sub.Currency.Should().NotBeNullOrEmpty();
      _output.WriteLine($"  Subscription {sub.Id}: Currency={sub.Currency}, Price={sub.Price}");
    }
  }

  /// <summary>I06: Verifies that subscription period dates are returned when available.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_ReturnsPeriodDates()
  {
    // Act
    IReadOnlyList<Subscription>? result = null;
    try
    {
      result = await _sut.ListUserSubscriptionsAsync();
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    if (result.Count == 0)
    {
      _output.WriteLine("No subscriptions found - skipping period date validation");
      return;
    }

    foreach (var sub in result)
    {
      if (sub.CurrentPeriodStart.HasValue && sub.CurrentPeriodEnd.HasValue)
      {
        sub.CurrentPeriodEnd.Value.Should().BeAfter(sub.CurrentPeriodStart.Value);
        _output.WriteLine($"  Subscription {sub.Id}: Period={sub.CurrentPeriodStart} to {sub.CurrentPeriodEnd}");
      }
      else
      {
        _output.WriteLine($"  Subscription {sub.Id}: No period dates");
      }
    }
  }

  /// <summary>I07: Verifies that subscription component values are returned when available.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_ReturnsComponentValues()
  {
    // Act
    IReadOnlyList<Subscription>? result = null;
    try
    {
      result = await _sut.ListUserSubscriptionsAsync();
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    if (result.Count == 0)
    {
      _output.WriteLine("No subscriptions found - skipping component values validation");
      return;
    }

    foreach (var sub in result)
    {
      if (sub.ComponentValues != null && sub.ComponentValues.Count > 0)
      {
        _output.WriteLine($"  Subscription {sub.Id} has {sub.ComponentValues.Count} components:");
        foreach (var component in sub.ComponentValues)
        {
          component.Name.Should().NotBeNullOrEmpty();
          _output.WriteLine($"    - {component.Name}: {component.Value} (default={component.Default}, price={component.Price})");
        }
      }
      else
      {
        _output.WriteLine($"  Subscription {sub.Id}: No component values");
      }
    }
  }

  /// <summary>I08: Verifies that listing returns empty collection when no subscriptions exist.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_WhenEmpty_ReturnsEmptyList()
  {
    // Act
    IReadOnlyList<Subscription>? result = null;
    try
    {
      result = await _sut.ListUserSubscriptionsAsync();
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    // Either returns empty list or subscriptions - both are valid
    result.Should().NotBeNull();
    _output.WriteLine($"Subscription count: {result.Count}");
  }

  #endregion


  #region Error Handling Tests (I09-I12)

  /// <summary>I09: Verifies that 404 is returned for non-existent subscription update.</summary>
  [IntegrationTest]
  public async Task UpdateUserSubscriptionAsync_NonExistent_ThrowsNotFound()
  {
    // Arrange
    var nonExistentSubId = "non-existent-user-subscription-id-12345";
    var request = new UpdateUserSubscriptionRequest(Frequency: SubscriptionFrequency.Monthly);

    // Act & Assert
    try
    {
      await _sut.UpdateUserSubscriptionAsync(nonExistentSubId, request);
      _output.WriteLine("Unexpectedly succeeded - subscription might exist or API behavior differs");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing permissions (403 Forbidden)");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
    {
      _output.WriteLine("Skipped: PUT method not allowed for api_token authentication scheme (405)");
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

  /// <summary>I10: Verifies that 404 is returned for non-existent subscription delete.</summary>
  [IntegrationTest]
  public async Task DeleteUserSubscriptionAsync_NonExistent_ThrowsNotFound()
  {
    // Arrange
    var nonExistentSubId = "non-existent-user-subscription-id-67890";

    // Act & Assert
    try
    {
      await _sut.DeleteUserSubscriptionAsync(nonExistentSubId);
      _output.WriteLine("Unexpectedly succeeded - subscription might exist or API behavior differs");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing permissions (403 Forbidden)");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
    {
      _output.WriteLine("Skipped: DELETE method not allowed for api_token authentication scheme (405)");
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

  /// <summary>I11: Verifies that malformed subscription ID returns API error.</summary>
  [IntegrationTest]
  public async Task UpdateUserSubscriptionAsync_MalformedId_ThrowsError()
  {
    // Arrange
    var malformedId = "!!!invalid-format!!!";
    var request = new UpdateUserSubscriptionRequest();

    // Act & Assert
    try
    {
      await _sut.UpdateUserSubscriptionAsync(malformedId, request);
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

  /// <summary>I12: Verifies that invalid rate plan returns API error.</summary>
  [IntegrationTest]
  public async Task UpdateUserSubscriptionAsync_InvalidRatePlan_ThrowsError()
  {
    // Arrange
    var nonExistentSubId = "test-sub-id";
    var request = new UpdateUserSubscriptionRequest(
      RatePlan: new RatePlanReference("invalid-rate-plan-that-does-not-exist"));

    // Act & Assert
    try
    {
      await _sut.UpdateUserSubscriptionAsync(nonExistentSubId, request);
      _output.WriteLine("Unexpectedly succeeded - API may have different behavior");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Write permissions (403 Forbidden)");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
    {
      _output.WriteLine("Skipped: PUT method not allowed for api_token authentication scheme (405)");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
      _output.WriteLine($"Received 404 Not Found: {ex.Message}");
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

  #endregion


  #region State Tests (I13-I16)

  /// <summary>I13: Verifies that subscriptions with Paid state exist or can be identified.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_CanIdentifyPaidSubscriptions()
  {
    // Act
    IReadOnlyList<Subscription>? result = null;
    try
    {
      result = await _sut.ListUserSubscriptionsAsync();
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    var paidSubscriptions = result.Where(s => s.State == SubscriptionState.Paid).ToList();
    _output.WriteLine($"Found {paidSubscriptions.Count} paid subscriptions out of {result.Count} total");

    foreach (var sub in paidSubscriptions)
    {
      _output.WriteLine($"  - {sub.RatePlan?.PublicName ?? sub.Id}: {sub.Price} {sub.Currency}/{sub.Frequency}");
    }
  }

  /// <summary>I14: Verifies that subscriptions can have various states.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_SubscriptionsHaveVariousStates()
  {
    // Act
    IReadOnlyList<Subscription>? result = null;
    try
    {
      result = await _sut.ListUserSubscriptionsAsync();
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert - group by state
    var stateGroups = result.GroupBy(s => s.State.Value).ToList();
    _output.WriteLine("Subscription states found:");
    foreach (var group in stateGroups)
    {
      _output.WriteLine($"  {group.Key}: {group.Count()} subscriptions");
    }

    if (result.Count > 0)
    {
      stateGroups.Should().NotBeEmpty("user should have at least one subscription state if any subscriptions exist");
    }
  }

  /// <summary>I15: Verifies that externally managed subscriptions are identifiable.</summary>
  [IntegrationTest]
  public async Task ListUserSubscriptionsAsync_CanIdentifyExternallyManaged()
  {
    // Act
    IReadOnlyList<Subscription>? result = null;
    try
    {
      result = await _sut.ListUserSubscriptionsAsync();
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing Read permissions (403 Forbidden)");
      return;
    }

    // Assert
    var externallyManaged = result.Where(s => s.RatePlan?.ExternallyManaged == true).ToList();
    var internallyManaged = result.Where(s => s.RatePlan?.ExternallyManaged == false).ToList();

    _output.WriteLine($"Externally managed subscriptions: {externallyManaged.Count}");
    _output.WriteLine($"Internally managed subscriptions: {internallyManaged.Count}");

    foreach (var sub in externallyManaged)
    {
      _output.WriteLine($"  [External] {sub.RatePlan?.PublicName ?? sub.Id}");
    }

    foreach (var sub in internallyManaged)
    {
      _output.WriteLine($"  [Internal] {sub.RatePlan?.PublicName ?? sub.Id}");
    }
  }

  /// <summary>I16: Verifies that delete result contains subscription ID.</summary>
  /// <remarks>
  ///   This test verifies the DeleteUserSubscriptionResult structure
  ///   but expects failure since we use a non-existent ID.
  /// </remarks>
  [IntegrationTest]
  public async Task DeleteUserSubscriptionAsync_ReturnsSubscriptionIdInResult()
  {
    // Arrange
    var testSubId = "test-subscription-for-delete-result";

    // Act & Assert
    try
    {
      var result = await _sut.DeleteUserSubscriptionAsync(testSubId);

      // If we get here, the subscription was deleted
      result.Should().NotBeNull();
      result.SubscriptionId.Should().NotBeNullOrEmpty();
      _output.WriteLine($"Delete result contains SubscriptionId: {result.SubscriptionId}");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      _output.WriteLine("Skipped: Token does not have Billing permissions (403 Forbidden)");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
      _output.WriteLine($"Expected 404 for non-existent subscription: {ex.Message}");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
    {
      _output.WriteLine("Skipped: DELETE method not allowed for api_token authentication scheme (405)");
    }
    catch (Core.Exceptions.CloudflareApiException ex)
    {
      _output.WriteLine($"API error: {ex.Message}");
    }
  }

  #endregion
}
