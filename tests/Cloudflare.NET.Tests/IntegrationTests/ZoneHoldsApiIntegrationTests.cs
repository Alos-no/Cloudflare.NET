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
  ///   Note: The 'hold' field reflects whether the hold is currently active, which depends
  ///   on the hold_after time. The API may set hold_after to a future date, meaning 'hold'
  ///   would be false until that time passes. We verify the hold was processed successfully
  ///   by checking that the returned object is not null.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires Enterprise plan.</b></para>
  ///   <para><b>API Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Zone holds are an Enterprise-only feature.</item>
  ///     <item>The 'hold' field is true when hold_after is in the past.</item>
  ///     <item>On non-Enterprise zones, the API accepts the request but 'hold' remains false.</item>
  ///     <item>API Ref: https://developers.cloudflare.com/api/resources/zones/subresources/holds/methods/create/</item>
  ///   </list>
  /// </remarks>
  [IntegrationTest(Skip = "Requires Enterprise plan - hold field remains false on Free/Pro/Business zones after creation")]
  public async Task CreateZoneHoldAsync_Basic_CreatesHoldSuccessfully()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    // Act
    var hold = await _sut.CreateZoneHoldAsync(_zoneId);

    // Assert - Verify the API call succeeded and returned a valid response.
    // Note: The 'hold' field may be false if hold_after is set to a future date by the API.
    // We verify the response is valid rather than asserting on the 'hold' value.
    hold.Should().NotBeNull("API should return a valid zone hold response");
    hold.Hold.Should().BeTrue("zone hold should be active after creation");

    // Verify by getting the hold status
    var fetchedHold = await _sut.GetZoneHoldAsync(_zoneId);
    fetchedHold.Should().NotBeNull("fetching zone hold should succeed");

    // Cleanup
    await _sut.RemoveZoneHoldAsync(_zoneId);
  }

  /// <summary>
  ///   I03: Verifies that a zone hold can be created with subdomain protection enabled.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires Enterprise plan.</b></para>
  ///   <para><b>API Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Zone holds are an Enterprise-only feature.</item>
  ///     <item>On non-Enterprise zones, the API accepts the request but does not store include_subdomains.</item>
  ///     <item>API Ref: https://developers.cloudflare.com/api/resources/zones/subresources/holds/methods/create/</item>
  ///   </list>
  /// </remarks>
  [IntegrationTest(Skip = "Requires Enterprise plan. Zone holds with include_subdomains are not applied on Free/Pro/Business plans.")]
  public async Task CreateZoneHoldAsync_WithSubdomains_IncludesSubdomainProtection()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    // Act
    var hold = await _sut.CreateZoneHoldAsync(_zoneId, includeSubdomains: true);

    // Assert
    hold.Should().NotBeNull();
    hold.Hold.Should().BeTrue("zone hold should be active after creation");
    hold.IncludeSubdomains.Should().BeTrue("subdomain protection should be enabled");

    // Cleanup
    await _sut.RemoveZoneHoldAsync(_zoneId);
  }

  /// <summary>
  ///   I04: Verifies that getting a zone hold after creation returns valid data.
  /// </summary>
  /// <remarks>
  ///   <para><b>API Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Zone holds are an Enterprise-only feature.</item>
  ///     <item>The 'hold' field is true only when hold_after is in the past.</item>
  ///     <item>On non-Enterprise zones, the 'hold' field may remain false regardless of hold_after.</item>
  ///     <item>API Ref: https://developers.cloudflare.com/api/resources/zones/subresources/holds/methods/get/</item>
  ///   </list>
  /// </remarks>
  [IntegrationTest(Skip = "Requires Enterprise plan - hold field remains false on Free/Pro/Business zones")]
  public async Task GetZoneHoldAsync_AfterCreation_ReturnsValidData()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();
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

  /// <summary>
  ///   I05: Verifies that a zone hold's hold_after can be updated to a future date.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires Enterprise plan.</b></para>
  ///   <para><b>API Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Zone holds are an Enterprise-only feature.</item>
  ///     <item>On non-Enterprise zones, the API accepts the request but does not store hold_after.</item>
  ///     <item>API Ref: https://developers.cloudflare.com/api/resources/zones/subresources/holds/methods/update/</item>
  ///   </list>
  /// </remarks>
  [IntegrationTest(Skip = "Requires Enterprise plan. Zone hold updates (hold_after) are not applied on Free/Pro/Business plans.")]
  public async Task UpdateZoneHoldAsync_SetHoldAfter_UpdatesSuccessfully()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();
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

  /// <summary>
  ///   I06: Verifies that a zone hold's include_subdomains can be enabled via update.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires Enterprise plan.</b></para>
  ///   <para><b>API Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Zone holds are an Enterprise-only feature.</item>
  ///     <item>On non-Enterprise zones, the API accepts the request but does not store include_subdomains.</item>
  ///     <item>API Ref: https://developers.cloudflare.com/api/resources/zones/subresources/holds/methods/update/</item>
  ///   </list>
  /// </remarks>
  [IntegrationTest(Skip = "Requires Enterprise plan. Zone hold updates (include_subdomains) are not applied on Free/Pro/Business plans.")]
  public async Task UpdateZoneHoldAsync_EnableSubdomains_UpdatesSuccessfully()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    // Create hold without subdomains first
    await _sut.CreateZoneHoldAsync(_zoneId, includeSubdomains: false);
    var request = new UpdateZoneHoldRequest(IncludeSubdomains: true);

    // Act
    var updatedHold = await _sut.UpdateZoneHoldAsync(_zoneId, request);

    // Assert
    updatedHold.Should().NotBeNull();
    updatedHold.IncludeSubdomains.Should().BeTrue("subdomain protection should be enabled after update");

    // Cleanup
    await _sut.RemoveZoneHoldAsync(_zoneId);
  }

  /// <summary>
  ///   I07: Verifies that a zone hold can be removed successfully.
  /// </summary>
  [IntegrationTest]
  public async Task RemoveZoneHoldAsync_WithExistingHold_RemovesSuccessfully()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();
    await _sut.CreateZoneHoldAsync(_zoneId);

    // Act
    var removedHold = await _sut.RemoveZoneHoldAsync(_zoneId);

    // Assert
    removedHold.Should().NotBeNull();
    removedHold.Hold.Should().BeFalse("zone hold should be inactive after removal");
  }

  /// <summary>
  ///   I08: Verifies the complete zone hold CRUD lifecycle works end-to-end.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires Enterprise plan.</b></para>
  ///   <para><b>API Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Zone holds are an Enterprise-only feature.</item>
  ///     <item>The 'hold' field is true only when hold_after is in the past.</item>
  ///     <item>On non-Enterprise zones, hold_after and include_subdomains are not stored.</item>
  ///     <item>API Ref: https://developers.cloudflare.com/api/resources/zones/subresources/holds/</item>
  ///   </list>
  /// </remarks>
  [IntegrationTest(Skip = "Requires Enterprise plan. Full zone hold lifecycle requires Enterprise features.")]
  public async Task ZoneHold_FullLifecycle_AllOperationsSucceed()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    // Step 1: Create hold
    var createdHold = await _sut.CreateZoneHoldAsync(_zoneId);
    createdHold.Hold.Should().BeTrue("Step 1: hold should be active after creation");

    // Step 2: Get hold
    var fetchedHold = await _sut.GetZoneHoldAsync(_zoneId);
    fetchedHold.Should().NotBeNull("Step 2: should be able to fetch zone hold");
    fetchedHold.Hold.Should().BeTrue("Step 2: hold should still be active after creation");

    // Step 3: Update hold with future date and subdomains
    var futureDate = DateTime.UtcNow.AddDays(30);
    var updateRequest = new UpdateZoneHoldRequest(HoldAfter: futureDate, IncludeSubdomains: true);
    var updatedHold = await _sut.UpdateZoneHoldAsync(_zoneId, updateRequest);
    updatedHold.Should().NotBeNull("Step 3: update should return a valid response");
    updatedHold.HoldAfter.Should().NotBeNull("Step 3: hold_after should be updated");
    updatedHold.IncludeSubdomains.Should().BeTrue("Step 3: include_subdomains should be enabled");

    // Step 4: Get updated hold and verify persistence
    var verifiedHold = await _sut.GetZoneHoldAsync(_zoneId);
    verifiedHold.IncludeSubdomains.Should().BeTrue("Step 4: include_subdomains change should persist");

    // Step 5: Remove hold
    var removedHold = await _sut.RemoveZoneHoldAsync(_zoneId);
    removedHold.Hold.Should().BeFalse("Step 5: hold should be inactive after removal");

    // Step 6: Verify removal
    var finalHold = await _sut.GetZoneHoldAsync(_zoneId);
    finalHold.Hold.Should().BeFalse("Step 6: hold should remain inactive");
  }

  /// <summary>
  ///   I09: Verifies that creating a hold on a zone that already has one is idempotent.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires Enterprise plan.</b></para>
  ///   <para><b>API Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Zone holds are an Enterprise-only feature.</item>
  ///     <item>The API behaves idempotently - creating a hold when one exists returns success.</item>
  ///     <item>On non-Enterprise zones, the 'hold' field remains false regardless of operations.</item>
  ///     <item>API Ref: https://developers.cloudflare.com/api/resources/zones/subresources/holds/methods/create/</item>
  ///   </list>
  /// </remarks>
  [IntegrationTest(Skip = "Requires Enterprise plan - hold field remains false on Free/Pro/Business zones")]
  public async Task CreateZoneHoldAsync_WhenHoldAlreadyExists_BehavesIdempotently()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();
    await _sut.CreateZoneHoldAsync(_zoneId);

    // Act - Create another hold on zone that already has one
    var secondHold = await _sut.CreateZoneHoldAsync(_zoneId);

    // Assert - API should be idempotent and return success
    secondHold.Should().NotBeNull("API should return a valid response");
    secondHold.Hold.Should().BeTrue("hold should still be active");

    // Cleanup
    await _sut.RemoveZoneHoldAsync(_zoneId);
  }

  /// <summary>
  ///   I10: Verifies that removing a hold from a zone that doesn't have one is idempotent.
  /// </summary>
  [IntegrationTest]
  public async Task RemoveZoneHoldAsync_WhenNoHoldExists_BehavesIdempotently()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();

    // Act - Remove a hold that doesn't exist
    var result = await _sut.RemoveZoneHoldAsync(_zoneId);

    // Assert - API should be idempotent and return success with Hold = false
    result.Should().NotBeNull("API should return a valid response");
    result.Hold.Should().BeFalse("hold should be false (no hold to remove)");
  }

  /// <summary>
  ///   I11: Verifies that setting hold_after to a past date updates the hold successfully.
  /// </summary>
  /// <remarks>
  ///   <para><b>API Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Zone holds are an Enterprise-only feature.</item>
  ///     <item>The 'hold' field is true only when hold_after is in the past.</item>
  ///     <item>On non-Enterprise zones, the 'hold' field may remain false regardless of hold_after.</item>
  ///     <item>API Ref: https://developers.cloudflare.com/api/resources/zones/subresources/holds/methods/update/</item>
  ///   </list>
  /// </remarks>
  [IntegrationTest(Skip = "Requires Enterprise plan - hold field remains false on Free/Pro/Business zones even with past hold_after")]
  public async Task UpdateZoneHoldAsync_WithPastDate_UpdatesSuccessfully()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();
    await _sut.CreateZoneHoldAsync(_zoneId);
    var pastDate = DateTime.UtcNow.AddDays(-1);
    var request = new UpdateZoneHoldRequest(HoldAfter: pastDate);

    // Act
    var updatedHold = await _sut.UpdateZoneHoldAsync(_zoneId, request);

    // Assert
    // Assert - Verify the API call succeeded.
    // Note: The 'hold' field should be true when hold_after is in the past on Enterprise zones.
    // On non-Enterprise zones, the behavior may differ.
    updatedHold.Should().NotBeNull("API should return a valid response");
    updatedHold.Hold.Should().BeTrue("hold should be active when hold_after is in the past");

    // Cleanup
    await _sut.RemoveZoneHoldAsync(_zoneId);
  }

  /// <summary>
  ///   I12: Verifies that a hold can be scheduled for far in the future.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires Enterprise plan.</b></para>
  ///   <para><b>API Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Zone holds are an Enterprise-only feature.</item>
  ///     <item>On non-Enterprise zones, the API accepts the request but does not store hold_after.</item>
  ///     <item>API Ref: https://developers.cloudflare.com/api/resources/zones/subresources/holds/methods/update/</item>
  ///   </list>
  /// </remarks>
  [IntegrationTest(Skip = "Requires Enterprise plan. Zone hold scheduling (hold_after) is not applied on Free/Pro/Business plans.")]
  public async Task UpdateZoneHoldAsync_WithFarFutureDate_SchedulesSuccessfully()
  {
    // Arrange
    await EnsureNoZoneHoldAsync();
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

  /// <summary>I13: Verifies that getting a zone hold for a non-existent zone returns HTTP 403 Forbidden.</summary>
  /// <remarks>
  ///   <para>
  ///     Cloudflare returns 403 Forbidden for non-existent zone IDs that are in valid format
  ///     (32-character hex strings). This is intentional security behavior to prevent zone
  ///     enumeration attacks - by returning the same 403 for both non-existent and unauthorized
  ///     zones, attackers cannot discover which zone IDs are valid.
  ///   </para>
  ///   <para>
  ///     See: https://authress.io/knowledge-base/articles/choosing-the-right-http-error-code-401-403-404
  ///   </para>
  /// </remarks>
  [IntegrationTest]
  public async Task GetZoneHoldAsync_WithNonExistentZone_ThrowsForbidden()
  {
    // Arrange - Use a valid format zone ID that doesn't exist
    var nonExistentZoneId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.GetZoneHoldAsync(nonExistentZoneId);

    // Assert - Cloudflare returns 403 to prevent zone enumeration (security best practice)
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.Forbidden);
  }

  /// <summary>I14: Verifies that getting a zone hold with a malformed zone ID returns HTTP 404 NotFound.</summary>
  /// <remarks>
  ///   Invalid zone ID formats (not 32-character hex strings) return 404 NotFound
  ///   with error code 7003 "Could not route to..." because Cloudflare's routing layer
  ///   cannot match the path to a valid zone endpoint.
  /// </remarks>
  [IntegrationTest]
  public async Task GetZoneHoldAsync_WithMalformedZoneId_ThrowsNotFound()
  {
    // Arrange
    var malformedZoneId = "invalid-zone-id-format!!!";

    // Act
    var action = async () => await _sut.GetZoneHoldAsync(malformedZoneId);

    // Assert
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
  }

  /// <summary>I15: Verifies that creating a zone hold with a malformed zone ID returns HTTP 404 NotFound.</summary>
  /// <remarks>
  ///   Invalid zone ID formats (not 32-character hex strings) return 404 NotFound
  ///   with error code 7003 "Could not route to..." because Cloudflare's routing layer
  ///   cannot match the path to a valid zone endpoint.
  /// </remarks>
  [IntegrationTest]
  public async Task CreateZoneHoldAsync_WithMalformedZoneId_ThrowsNotFound()
  {
    // Arrange
    var malformedZoneId = "invalid-zone-id-format!!!";

    // Act
    var action = async () => await _sut.CreateZoneHoldAsync(malformedZoneId);

    // Assert
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
  }

  /// <summary>I16: Verifies that removing a zone hold with a malformed zone ID returns HTTP 404 NotFound.</summary>
  /// <remarks>
  ///   Invalid zone ID formats (not 32-character hex strings) return 404 NotFound
  ///   with error code 7003 "Could not route to..." because Cloudflare's routing layer
  ///   cannot match the path to a valid zone endpoint.
  /// </remarks>
  [IntegrationTest]
  public async Task RemoveZoneHoldAsync_WithMalformedZoneId_ThrowsNotFound()
  {
    // Arrange
    var malformedZoneId = "invalid-zone-id-format!!!";

    // Act
    var action = async () => await _sut.RemoveZoneHoldAsync(malformedZoneId);

    // Assert
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
  }

  #endregion
}
