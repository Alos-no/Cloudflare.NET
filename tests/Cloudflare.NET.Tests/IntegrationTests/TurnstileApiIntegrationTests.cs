namespace Cloudflare.NET.Tests.IntegrationTests;

using System.Net;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Turnstile;
using Turnstile.Models;
using Xunit.Abstractions;


/// <summary>
///   Contains integration tests for the Turnstile Widgets functionality in <see cref="TurnstileApi" />.
///   These tests interact with the live Cloudflare API and require credentials.
/// </summary>
/// <remarks>
///   <para>
///     <b>Important:</b> These tests create and delete Turnstile widgets in an account.
///     Widgets are cleaned up automatically after each test, but if tests fail
///     unexpectedly, you may have leftover test widgets that need manual cleanup.
///   </para>
///   <para>
///     Test widgets use names prefixed with "[Test] " for easy identification.
///     The token used for testing must have Turnstile Widget Write permissions.
///   </para>
///   <para>
///     <b>Note:</b> Turnstile is an account-level resource, so tests use AccountId not ZoneId.
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
[Collection(TestCollections.TurnstileWidgets)]
public class TurnstileApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly ITurnstileApi _sut;

  /// <summary>The settings loaded from the test configuration.</summary>
  private readonly TestCloudflareSettings _settings;

  /// <summary>List of widget sitekeys created during tests that need cleanup.</summary>
  private readonly List<string> _createdWidgetSitekeys = new();

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="TurnstileApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public TurnstileApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut      = fixture.TurnstileApi;
    _settings = TestConfiguration.CloudflareSettings;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region List Tests (I01-I02)

  /// <summary>I01: Verifies that widgets can be listed successfully.</summary>
  [IntegrationTest]
  public async Task ListWidgetsAsync_ReturnsWidgets()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListWidgetsAsync(accountId);

    // Assert - Verify API contract
    result.Should().NotBeNull("API should return a valid response");
    result.Items.Should().NotBeNull("Items collection should not be null");
    // Note: Account may or may not have existing widgets - empty is valid
  }

  /// <summary>I02: Verifies that listing widgets returns proper structure.</summary>
  [IntegrationTest]
  public async Task ListWidgetsAsync_ReturnsProperStructure()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListWidgetsAsync(accountId);

    // Assert - Verify API contract for widget structure
    result.Should().NotBeNull("API should return a valid response");
    foreach (var widget in result.Items)
    {
      widget.Sitekey.Should().NotBeNullOrEmpty("widget should have a sitekey");
      widget.Name.Should().NotBeNullOrEmpty("widget should have a name");
      widget.Domains.Should().NotBeNull("widget should have domains collection");
      widget.Mode.Value.Should().NotBeNullOrEmpty("widget should have a mode");
    }
  }

  #endregion


  #region Widget CRUD Tests (I03-I07)

  /// <summary>I03: Verifies that a widget can be created with basic settings.</summary>
  [IntegrationTest]
  public async Task CreateWidgetAsync_BasicSettings_CreatesWidget()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var widgetName = GenerateTestWidgetName();

    try
    {
      // Act
      var request = new CreateTurnstileWidgetRequest(
        Name: widgetName,
        Domains: ["test.example.com"],
        Mode: WidgetMode.Managed);
      var result = await _sut.CreateWidgetAsync(accountId, request);
      _createdWidgetSitekeys.Add(result.Sitekey);

      // Assert
      result.Should().NotBeNull("API should return the created widget");
      result.Sitekey.Should().NotBeNullOrEmpty("created widget should have a sitekey");
      result.Name.Should().Be(widgetName, "widget name should match request");
      result.Mode.Should().Be(WidgetMode.Managed, "widget mode should match request");
      result.Domains.Should().Contain("test.example.com", "widget domains should match request");
      result.Secret.Should().NotBeNullOrEmpty("Secret should be returned on widget creation");
    }
    finally
    {
      await CleanupTestWidgets(accountId);
    }
  }

  /// <summary>I04: Verifies that a widget can be created with all optional fields.</summary>
  /// <remarks>
  ///   <para>
  ///     Tests widget creation with both BotFightMode enabled and disabled. The test with
  ///     BotFightMode=true is skipped because it requires an entitlement not available on
  ///     free Cloudflare accounts. The Turnstile API returns:
  ///     <c>"not entitled to widgets with `bot_fight_mode` set to true"</c>
  ///   </para>
  ///   <para>
  ///     Bot Fight Mode is an enterprise feature that provides enhanced protection against
  ///     automated bot traffic. Testing this feature requires a paid Cloudflare plan with
  ///     the appropriate entitlement enabled.
  ///   </para>
  /// </remarks>
  [IntegrationTestTheory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task CreateWidgetAsync_AllFields_CreatesWidgetWithAllSettings(bool botFightMode)
  {
    // Skip bot_fight_mode=true test case - requires paid entitlement
    Skip.If(botFightMode, "Requires paid account - bot_fight_mode entitlement (paid Cloudflare feature)");

    // Arrange
    var accountId = _settings.AccountId;
    var widgetName = GenerateTestWidgetName();

    try
    {
      // Act
      var request = new CreateTurnstileWidgetRequest(
        Name: widgetName,
        Domains: ["test.example.com", "api.example.com"],
        Mode: WidgetMode.Invisible,
        BotFightMode: botFightMode,
        ClearanceLevel: ClearanceLevel.JsChallenge,
        EphemeralId: false,
        Offlabel: false,
        Region: "world");

      var result = await _sut.CreateWidgetAsync(accountId, request);
      _createdWidgetSitekeys.Add(result.Sitekey);

      // Assert
      result.Should().NotBeNull("API should return the created widget");
      result.Name.Should().Be(widgetName, "widget name should match request");
      result.Mode.Should().Be(WidgetMode.Invisible, "widget mode should match request");
      result.Domains.Should().HaveCount(2, "widget should have 2 domains");
      result.BotFightMode.Should().Be(botFightMode, "bot fight mode should match request");
    }
    finally
    {
      await CleanupTestWidgets(accountId);
    }
  }

  /// <summary>I05: Verifies that a widget can be retrieved by sitekey.</summary>
  [IntegrationTest]
  public async Task GetWidgetAsync_AfterCreation_ReturnsWidget()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var widgetName = GenerateTestWidgetName();

    try
    {
      // Create a widget first
      var createRequest = new CreateTurnstileWidgetRequest(
        Name: widgetName,
        Domains: ["test.example.com"],
        Mode: WidgetMode.Managed);
      var created = await _sut.CreateWidgetAsync(accountId, createRequest);
      _createdWidgetSitekeys.Add(created.Sitekey);

      // Act
      var result = await _sut.GetWidgetAsync(accountId, created.Sitekey);

      // Assert
      result.Should().NotBeNull("API should return the widget");
      result.Sitekey.Should().Be(created.Sitekey, "sitekey should match created widget");
      result.Name.Should().Be(widgetName, "name should match created widget");
      result.Secret.Should().NotBeNullOrEmpty("Secret is returned on get");
    }
    finally
    {
      await CleanupTestWidgets(accountId);
    }
  }

  /// <summary>I06: Verifies that a widget can be updated.</summary>
  [IntegrationTest]
  public async Task UpdateWidgetAsync_UpdateSettings_SettingsChange()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var originalName = GenerateTestWidgetName();
    var updatedName = GenerateTestWidgetName();

    try
    {
      // Create a widget first
      var createRequest = new CreateTurnstileWidgetRequest(
        Name: originalName,
        Domains: ["test.example.com"],
        Mode: WidgetMode.Managed);
      var created = await _sut.CreateWidgetAsync(accountId, createRequest);
      _createdWidgetSitekeys.Add(created.Sitekey);

      // Act
      var updateRequest = new UpdateTurnstileWidgetRequest(
        Name: updatedName,
        Domains: ["updated.example.com"],
        Mode: WidgetMode.NonInteractive);
      var updated = await _sut.UpdateWidgetAsync(accountId, created.Sitekey, updateRequest);

      // Assert
      updated.Should().NotBeNull("API should return the updated widget");
      updated.Sitekey.Should().Be(created.Sitekey, "sitekey should remain the same");
      updated.Name.Should().Be(updatedName, "name should be updated");
      updated.Mode.Should().Be(WidgetMode.NonInteractive, "mode should be updated");
      updated.Domains.Should().Contain("updated.example.com", "domains should be updated");
    }
    finally
    {
      await CleanupTestWidgets(accountId);
    }
  }

  /// <summary>I07: Verifies that a widget can be deleted.</summary>
  [IntegrationTest]
  public async Task DeleteWidgetAsync_DeletesWidget()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var widgetName = GenerateTestWidgetName();

    // Create a widget first
    var createRequest = new CreateTurnstileWidgetRequest(
      Name: widgetName,
      Domains: ["test.example.com"],
      Mode: WidgetMode.Managed);
    var created = await _sut.CreateWidgetAsync(accountId, createRequest);

    // Act
    await _sut.DeleteWidgetAsync(accountId, created.Sitekey);

    // Assert - Trying to get the deleted widget should throw
    var action = async () => await _sut.GetWidgetAsync(accountId, created.Sitekey);
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
  }

  #endregion


  #region Secret Rotation Tests (I08-I09)

  /// <summary>I08: Verifies that secret can be rotated with grace period.</summary>
  [IntegrationTest]
  public async Task RotateSecretAsync_GracePeriod_ReturnsNewSecret()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var widgetName = GenerateTestWidgetName();

    try
    {
      // Create a widget first
      var createRequest = new CreateTurnstileWidgetRequest(
        Name: widgetName,
        Domains: ["test.example.com"],
        Mode: WidgetMode.Managed);
      var created = await _sut.CreateWidgetAsync(accountId, createRequest);
      _createdWidgetSitekeys.Add(created.Sitekey);

      var originalSecret = created.Secret;

      // Act - Rotate with grace period (invalidateImmediately: false)
      var result = await _sut.RotateSecretAsync(accountId, created.Sitekey, invalidateImmediately: false);

      // Assert
      result.Should().NotBeNull("API should return the rotated secret");
      result.Secret.Should().NotBeNullOrEmpty("new secret should be provided");
      result.Secret.Should().NotBe(originalSecret, "New secret should be different from original");
    }
    finally
    {
      await CleanupTestWidgets(accountId);
    }
  }

  /// <summary>I09: Verifies that secret can be rotated with immediate invalidation.</summary>
  [IntegrationTest]
  public async Task RotateSecretAsync_Immediate_ReturnsNewSecret()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var widgetName = GenerateTestWidgetName();

    try
    {
      // Create a widget first
      var createRequest = new CreateTurnstileWidgetRequest(
        Name: widgetName,
        Domains: ["test.example.com"],
        Mode: WidgetMode.Managed);
      var created = await _sut.CreateWidgetAsync(accountId, createRequest);
      _createdWidgetSitekeys.Add(created.Sitekey);

      var originalSecret = created.Secret;

      // Act - Rotate with immediate invalidation
      var result = await _sut.RotateSecretAsync(accountId, created.Sitekey, invalidateImmediately: true);

      // Assert
      result.Should().NotBeNull("API should return the rotated secret");
      result.Secret.Should().NotBeNullOrEmpty("new secret should be provided");
      result.Secret.Should().NotBe(originalSecret, "New secret should be different from original");
    }
    finally
    {
      await CleanupTestWidgets(accountId);
    }
  }

  #endregion


  #region Full Lifecycle Tests (I10)

  /// <summary>I10: Verifies the complete widget lifecycle: create, get, update, rotate secret, delete.</summary>
  [IntegrationTest]
  public async Task WidgetLifecycle_CreateGetUpdateRotateDelete_Succeeds()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var originalName = GenerateTestWidgetName();
    var updatedName = GenerateTestWidgetName();

    string? widgetSitekey = null;

    try
    {
      // 1. Create
      var createRequest = new CreateTurnstileWidgetRequest(
        Name: originalName,
        Domains: ["test.example.com"],
        Mode: WidgetMode.Managed);
      var created = await _sut.CreateWidgetAsync(accountId, createRequest);
      widgetSitekey = created.Sitekey;
      created.Name.Should().Be(originalName, "Step 1: Created widget should have correct name");
      created.Secret.Should().NotBeNullOrEmpty("Step 1: Created widget should have a secret");

      // 2. Get
      var retrieved = await _sut.GetWidgetAsync(accountId, widgetSitekey);
      retrieved.Sitekey.Should().Be(widgetSitekey, "Step 2: Retrieved sitekey should match");
      retrieved.Name.Should().Be(originalName, "Step 2: Retrieved name should match");
      retrieved.Secret.Should().NotBeNullOrEmpty("Step 2: Secret is returned on get");

      // 3. Update
      var updateRequest = new UpdateTurnstileWidgetRequest(
        Name: updatedName,
        Domains: ["updated.example.com"],
        Mode: WidgetMode.Invisible);
      var updated = await _sut.UpdateWidgetAsync(accountId, widgetSitekey, updateRequest);
      updated.Name.Should().Be(updatedName, "Step 3: Updated name should match");
      updated.Mode.Should().Be(WidgetMode.Invisible, "Step 3: Updated mode should match");

      // 4. Rotate Secret
      var originalSecret = created.Secret;
      var rotated = await _sut.RotateSecretAsync(accountId, widgetSitekey);
      rotated.Secret.Should().NotBe(originalSecret, "Step 4: Rotated secret should be different");

      // 5. Delete
      await _sut.DeleteWidgetAsync(accountId, widgetSitekey);
      widgetSitekey = null; // Mark as deleted

      // 6. Verify deletion
      var action = async () => await _sut.GetWidgetAsync(accountId, created.Sitekey);
      await action.Should().ThrowAsync<HttpRequestException>()
        .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
    }
    finally
    {
      // Cleanup if something failed before deletion
      if (widgetSitekey != null)
      {
        try
        {
          await _sut.DeleteWidgetAsync(accountId, widgetSitekey);
        }
        catch
        {
          // Ignore cleanup errors
        }
      }
    }
  }

  #endregion


  #region Error Handling Tests (I11-I14)

  /// <summary>I11: Verifies that getting a non-existent widget returns HTTP 404 NotFound.</summary>
  /// <remarks>
  ///   Non-existent Turnstile widget sitekeys return 404 NotFound with error code 10404.
  /// </remarks>
  [IntegrationTest]
  public async Task GetWidgetAsync_NonExistent_ThrowsHttpRequestException()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentSitekey = "0x0000000000000000000000";

    // Act
    var action = async () => await _sut.GetWidgetAsync(accountId, nonExistentSitekey);

    // Assert
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
  }

  /// <summary>I12: Verifies that deleting a non-existent widget returns HTTP 404 NotFound.</summary>
  /// <remarks>
  ///   Non-existent Turnstile widget sitekeys return 404 NotFound with error code 10404.
  /// </remarks>
  [IntegrationTest]
  public async Task DeleteWidgetAsync_NonExistent_ThrowsHttpRequestException()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentSitekey = "0x0000000000000000000000";

    // Act
    var action = async () => await _sut.DeleteWidgetAsync(accountId, nonExistentSitekey);

    // Assert
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
  }

  /// <summary>I13: Verifies that rotating secret for non-existent widget returns HTTP 404 NotFound.</summary>
  /// <remarks>
  ///   Non-existent Turnstile widget sitekeys return 404 NotFound with error code 10404.
  /// </remarks>
  [IntegrationTest]
  public async Task RotateSecretAsync_NonExistent_ThrowsHttpRequestException()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentSitekey = "0x0000000000000000000000";

    // Act
    var action = async () => await _sut.RotateSecretAsync(accountId, nonExistentSitekey);

    // Assert
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
  }

  /// <summary>I14: Verifies that invalid account ID format returns HTTP 404 NotFound.</summary>
  /// <remarks>
  ///   Invalid account ID formats (not 32-character hex strings) return 404 NotFound
  ///   with error code 7003 "Could not route to..." because Cloudflare's routing layer
  ///   cannot match the path to a valid account endpoint.
  /// </remarks>
  [IntegrationTest]
  public async Task ListWidgetsAsync_InvalidAccountId_ThrowsHttpRequestException()
  {
    // Arrange
    var invalidAccountId = "invalid-account-id-format";

    // Act
    var action = async () => await _sut.ListWidgetsAsync(invalidAccountId);

    // Assert
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
  }

  #endregion


  #region Pagination Tests (I15-I16)

  /// <summary>I15: Verifies that ListAllWidgetsAsync yields all widgets across pages.</summary>
  [IntegrationTest]
  public async Task ListAllWidgetsAsync_YieldsAllWidgets()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var widgets = new List<TurnstileWidget>();
    await foreach (var widget in _sut.ListAllWidgetsAsync(accountId))
    {
      widgets.Add(widget);
    }

    // Assert - Verify structure of any returned widgets
    foreach (var widget in widgets)
    {
      widget.Sitekey.Should().NotBeNullOrEmpty("each widget should have a sitekey");
      widget.Name.Should().NotBeNullOrEmpty("each widget should have a name");
    }
  }

  /// <summary>I16: Verifies pagination filters work correctly.</summary>
  [IntegrationTest]
  public async Task ListWidgetsAsync_WithPagination_ReturnsCorrectPage()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var filters = new ListTurnstileWidgetsFilters(Page: 1, PerPage: 5);

    // Act
    var result = await _sut.ListWidgetsAsync(accountId, filters);

    // Assert
    result.Should().NotBeNull("API should return a valid response");
    result.Items.Count.Should().BeLessThanOrEqualTo(5, "should respect PerPage limit");

    // PageInfo is required for paginated responses.
    result.PageInfo.Should().NotBeNull("pagination request should return PageInfo");
    result.PageInfo!.Page.Should().Be(1, "should be on page 1");
    result.PageInfo.PerPage.Should().Be(5, "should have PerPage of 5");
  }

  #endregion


  #region Helpers

  /// <summary>
  ///   Generates a unique test widget name to avoid conflicts with existing widgets.
  ///   Uses a prefix for easy identification.
  /// </summary>
  /// <returns>A unique test widget name.</returns>
  private static string GenerateTestWidgetName()
  {
    var guid = Guid.NewGuid().ToString("N")[..8];
    return $"[Test] Widget {guid}";
  }

  /// <summary>Cleans up test widgets created during tests.</summary>
  private async Task CleanupTestWidgets(string accountId)
  {
    foreach (var sitekey in _createdWidgetSitekeys)
    {
      try
      {
        await _sut.DeleteWidgetAsync(accountId, sitekey);
      }
      catch
      {
        // Ignore cleanup errors
      }
    }

    _createdWidgetSitekeys.Clear();
  }

  #endregion
}
