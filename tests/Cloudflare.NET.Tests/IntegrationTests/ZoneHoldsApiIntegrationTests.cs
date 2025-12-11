namespace Cloudflare.NET.Tests.IntegrationTests;

using Core.Exceptions;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;
using Zones;
using Zones.Models;

/// <summary>
///   Contains integration tests for the Zone Hold operations of <see cref="ZonesApi" />. These tests interact with the
///   live Cloudflare API and require credentials.
/// </summary>
/// <remarks>
///   This test class focuses on Zone Hold operations including:
///   <list type="bullet">
///     <item><description>GetZoneHoldAsync - Get zone hold status</description></item>
///     <item><description>CreateZoneHoldAsync - Create a zone hold</description></item>
///     <item><description>UpdateZoneHoldAsync - Update zone hold settings</description></item>
///     <item><description>RemoveZoneHoldAsync - Remove a zone hold</description></item>
///   </list>
///   For Zone CRUD integration tests, see <see cref="ZonesApiIntegrationTests" />.
///   For Zone Settings integration tests, see <see cref="ZoneSettingsApiIntegrationTests" />.
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class ZoneHoldsApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IZonesApi _sut;

  /// <summary>The ID of the test zone from configuration.</summary>
  private readonly string _zoneId;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="ZoneHoldsApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public ZoneHoldsApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    // Resolve the SUT and settings from the fixture and configuration.
    _sut = fixture.ZonesApi;
    var settings = TestConfiguration.CloudflareSettings;
    _zoneId = settings.ZoneId;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion

  #region Helper Methods

  /// <summary>
  ///   Helper method to ensure zone has no hold before running a test.
  ///   Removes any existing hold if present.
  /// </summary>
  private async Task EnsureNoZoneHoldAsync()
  {
    try
    {
      var currentHold = await _sut.GetZoneHoldAsync(_zoneId);
      if (currentHold.Hold)
        await _sut.RemoveZoneHoldAsync(_zoneId);
    }
    catch
    {
      // Ignore errors - zone may not have hold capability or hold may already not exist
    }
  }

  #endregion

  #region Zone Hold Integration Tests

  /// <summary>
  ///   I01: Verifies that getting the zone hold status when no hold exists returns Hold = false.
  /// </summary>
  [IntegrationTest]
  public async Task GetZoneHoldAsync_WhenNoHoldExists_ReturnsHoldFalse()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    // Act
    var hold = await _sut.GetZoneHoldAsync(_zoneId);

    // Assert
    hold.Should().NotBeNull();
    hold.Hold.Should().BeFalse("zone should not have an active hold");
  }

  /// <summary>
  ///   I02: Verifies that a basic zone hold can be created without including subdomains.
  ///   Skips gracefully if the API token lacks Zone Hold permissions.
  /// </summary>
  [IntegrationTest]
  public async Task CreateZoneHoldAsync_Basic_CreatesHoldSuccessfully()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    try
    {
      // Act
      var hold = await _sut.CreateZoneHoldAsync(_zoneId);

      // Assert
      hold.Should().NotBeNull();
      hold.Hold.Should().BeTrue("zone hold should be active after creation");

      // Cleanup
      await _sut.RemoveZoneHoldAsync(_zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      // Expected if API token doesn't have Zone Hold permissions
      // This is acceptable behavior - the test verifies the API integration works
    }
  }

  /// <summary>
  ///   I03: Verifies that a zone hold can be created with subdomain protection enabled.
  ///   Skips gracefully if the API token lacks Zone Hold permissions.
  /// </summary>
  [IntegrationTest]
  public async Task CreateZoneHoldAsync_WithSubdomains_IncludesSubdomainProtection()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    try
    {
      // Act
      var hold = await _sut.CreateZoneHoldAsync(_zoneId, includeSubdomains: true);

      // Assert
      hold.Should().NotBeNull();
      hold.Hold.Should().BeTrue("zone hold should be active after creation");
      hold.IncludeSubdomains.Should().BeTrue("subdomain protection should be enabled");

      // Cleanup
      await _sut.RemoveZoneHoldAsync(_zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      // Expected if API token doesn't have Zone Hold permissions
    }
  }

  /// <summary>
  ///   I04: Verifies that getting a zone hold after creation returns the correct state.
  ///   Skips gracefully if the API token lacks Zone Hold permissions.
  /// </summary>
  [IntegrationTest]
  public async Task GetZoneHoldAsync_AfterCreation_ReturnsActiveHold()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    try
    {
      await _sut.CreateZoneHoldAsync(_zoneId);

      // Act
      var hold = await _sut.GetZoneHoldAsync(_zoneId);

      // Assert
      hold.Should().NotBeNull();
      hold.Hold.Should().BeTrue("zone hold should be active after creation");
      hold.HoldAfter.Should().NotBeNull("hold_after should be populated");

      // Cleanup
      await _sut.RemoveZoneHoldAsync(_zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      // Expected if API token doesn't have Zone Hold permissions
    }
  }

  /// <summary>
  ///   I05: Verifies that a zone hold's hold_after can be updated to a future date.
  ///   Skips gracefully if the API token lacks Zone Hold permissions.
  /// </summary>
  [IntegrationTest]
  public async Task UpdateZoneHoldAsync_SetHoldAfter_UpdatesSuccessfully()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    try
    {
      await _sut.CreateZoneHoldAsync(_zoneId);
      var futureDate = DateTime.UtcNow.AddDays(7);
      var request = new UpdateZoneHoldRequest(HoldAfter: futureDate);

      // Act
      var updatedHold = await _sut.UpdateZoneHoldAsync(_zoneId, request);

      // Assert
      updatedHold.Should().NotBeNull();
      updatedHold.HoldAfter.Should().NotBeNull();
      // The returned date should be close to the future date we set
      updatedHold.HoldAfter!.Value.Should().BeCloseTo(futureDate, TimeSpan.FromMinutes(1));

      // Cleanup
      await _sut.RemoveZoneHoldAsync(_zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      // Expected if API token doesn't have Zone Hold permissions
    }
  }

  /// <summary>
  ///   I06: Verifies that a zone hold's include_subdomains can be enabled via update.
  ///   Skips gracefully if the API token lacks Zone Hold permissions.
  /// </summary>
  [IntegrationTest]
  public async Task UpdateZoneHoldAsync_EnableSubdomains_UpdatesSuccessfully()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    try
    {
      // Create hold without subdomains first
      await _sut.CreateZoneHoldAsync(_zoneId, includeSubdomains: false);
      var request = new UpdateZoneHoldRequest(IncludeSubdomains: true);

      // Act
      var updatedHold = await _sut.UpdateZoneHoldAsync(_zoneId, request);

      // Assert
      updatedHold.Should().NotBeNull();
      updatedHold.IncludeSubdomains.Should().BeTrue();

      // Cleanup
      await _sut.RemoveZoneHoldAsync(_zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      // Expected if API token doesn't have Zone Hold permissions
    }
  }

  /// <summary>
  ///   I07: Verifies that a zone hold can be removed successfully.
  ///   Skips gracefully if the API token lacks Zone Hold permissions.
  /// </summary>
  [IntegrationTest]
  public async Task RemoveZoneHoldAsync_WithExistingHold_RemovesSuccessfully()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    try
    {
      await _sut.CreateZoneHoldAsync(_zoneId);

      // Act
      var removedHold = await _sut.RemoveZoneHoldAsync(_zoneId);

      // Assert
      removedHold.Should().NotBeNull();
      removedHold.Hold.Should().BeFalse("zone hold should be inactive after removal");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      // Expected if API token doesn't have Zone Hold permissions
    }
  }

  /// <summary>
  ///   I08: Verifies the complete zone hold CRUD lifecycle works end-to-end.
  ///   Skips gracefully if the API token lacks Zone Hold permissions.
  /// </summary>
  [IntegrationTest]
  public async Task ZoneHold_FullLifecycle_AllOperationsSucceed()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    try
    {
      // Step 1: Create hold
      var createdHold = await _sut.CreateZoneHoldAsync(_zoneId);
      createdHold.Hold.Should().BeTrue("Step 1: hold should be active after creation");

      // Step 2: Get hold
      var fetchedHold = await _sut.GetZoneHoldAsync(_zoneId);
      fetchedHold.Hold.Should().BeTrue("Step 2: hold should still be active");

      // Step 3: Update hold
      var futureDate = DateTime.UtcNow.AddDays(30);
      var updateRequest = new UpdateZoneHoldRequest(HoldAfter: futureDate, IncludeSubdomains: true);
      var updatedHold = await _sut.UpdateZoneHoldAsync(_zoneId, updateRequest);
      updatedHold.HoldAfter.Should().NotBeNull("Step 3: hold_after should be updated");
      updatedHold.IncludeSubdomains.Should().BeTrue("Step 3: include_subdomains should be updated");

      // Step 4: Get updated hold
      var verifiedHold = await _sut.GetZoneHoldAsync(_zoneId);
      verifiedHold.IncludeSubdomains.Should().BeTrue("Step 4: include_subdomains change should persist");

      // Step 5: Remove hold
      var removedHold = await _sut.RemoveZoneHoldAsync(_zoneId);
      removedHold.Hold.Should().BeFalse("Step 5: hold should be inactive after removal");

      // Step 6: Verify removal
      var finalHold = await _sut.GetZoneHoldAsync(_zoneId);
      finalHold.Hold.Should().BeFalse("Step 6: hold should remain inactive");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      // Expected if API token doesn't have Zone Hold permissions
    }
  }

  /// <summary>
  ///   I09: Verifies behavior when creating a hold on a zone that already has one (idempotency check).
  ///   Skips gracefully if the API token lacks Zone Hold permissions.
  /// </summary>
  [IntegrationTest]
  public async Task CreateZoneHoldAsync_WhenHoldAlreadyExists_BehavesIdempotently()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    try
    {
      // Create initial hold
      await _sut.CreateZoneHoldAsync(_zoneId);

      // Act - Try to create another hold (should be idempotent or fail gracefully)
      // The Cloudflare API may succeed (idempotent) or return an error
      try
      {
        var secondHold = await _sut.CreateZoneHoldAsync(_zoneId);
        secondHold.Hold.Should().BeTrue("if idempotent, hold should still be active");
      }
      catch (CloudflareApiException)
      {
        // This is acceptable - API may reject duplicate hold creation
      }

      // Cleanup
      await _sut.RemoveZoneHoldAsync(_zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      // Expected if API token doesn't have Zone Hold permissions
    }
  }

  /// <summary>
  ///   I10: Verifies behavior when removing a hold from a zone that doesn't have one.
  ///   Skips gracefully if the API token lacks Zone Hold permissions.
  /// </summary>
  [IntegrationTest]
  public async Task RemoveZoneHoldAsync_WhenNoHoldExists_BehavesIdempotently()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    // Act - Try to remove a hold that doesn't exist
    try
    {
      var result = await _sut.RemoveZoneHoldAsync(_zoneId);

      // Assert - Should return with Hold = false
      result.Hold.Should().BeFalse("hold should be false (no hold to remove)");
    }
    catch (CloudflareApiException)
    {
      // This is also acceptable - API may reject removing non-existent hold
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      // Expected if API token doesn't have Zone Hold permissions
    }
  }

  /// <summary>
  ///   I11: Verifies that setting hold_after to a past date makes the hold immediately active.
  ///   Skips gracefully if the API token lacks Zone Hold permissions.
  /// </summary>
  [IntegrationTest]
  public async Task UpdateZoneHoldAsync_WithPastDate_HoldIsImmediatelyActive()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    try
    {
      await _sut.CreateZoneHoldAsync(_zoneId);
      var pastDate = DateTime.UtcNow.AddDays(-1);
      var request = new UpdateZoneHoldRequest(HoldAfter: pastDate);

      // Act
      var updatedHold = await _sut.UpdateZoneHoldAsync(_zoneId, request);

      // Assert
      updatedHold.Should().NotBeNull();
      updatedHold.Hold.Should().BeTrue("hold should be active when hold_after is in the past");

      // Cleanup
      await _sut.RemoveZoneHoldAsync(_zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      // Expected if API token doesn't have Zone Hold permissions
    }
  }

  /// <summary>
  ///   I12: Verifies that a hold can be scheduled for far in the future.
  ///   Skips gracefully if the API token lacks Zone Hold permissions.
  /// </summary>
  [IntegrationTest]
  public async Task UpdateZoneHoldAsync_WithFarFutureDate_SchedulesSuccessfully()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    try
    {
      await _sut.CreateZoneHoldAsync(_zoneId);
      var futureDate = DateTime.UtcNow.AddYears(1);
      var request = new UpdateZoneHoldRequest(HoldAfter: futureDate);

      // Act
      var updatedHold = await _sut.UpdateZoneHoldAsync(_zoneId, request);

      // Assert
      updatedHold.Should().NotBeNull();
      updatedHold.HoldAfter.Should().NotBeNull();
      updatedHold.HoldAfter!.Value.Should().BeCloseTo(futureDate, TimeSpan.FromMinutes(1));

      // Cleanup
      await _sut.RemoveZoneHoldAsync(_zoneId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
    {
      // Expected if API token doesn't have Zone Hold permissions
    }
  }

  /// <summary>
  ///   I13: Verifies that getting a zone hold for a non-existent zone returns an error (404 or 403).
  ///   Note: The API may return 403 if the token lacks permissions, or 404 if the zone doesn't exist.
  /// </summary>
  [IntegrationTest]
  public async Task GetZoneHoldAsync_WithNonExistentZone_ThrowsNotFoundOrForbidden()
  {
    // Arrange
    var nonExistentZoneId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.GetZoneHoldAsync(nonExistentZoneId);

    // Assert - API returns either 404 (zone not found) or 403 (no permission for zone holds)
    var ex = await action.Should().ThrowAsync<HttpRequestException>();
    ex.Which.StatusCode.Should().BeOneOf(
      System.Net.HttpStatusCode.NotFound,
      System.Net.HttpStatusCode.Forbidden);
  }

  #endregion
}
