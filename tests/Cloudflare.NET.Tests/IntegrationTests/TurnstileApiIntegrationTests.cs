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

  /// <summary>The xUnit test output helper for writing warnings and debug info.</summary>
  private readonly ITestOutputHelper _output;

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
    _output   = output;

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

    // Assert
    result.Should().NotBeNull();
    // Note: Account may or may not have existing widgets
    _output.WriteLine($"Found {result.Items.Count} Turnstile widgets in account");
  }

  /// <summary>I02: Verifies that listing widgets returns proper structure.</summary>
  [IntegrationTest]
  public async Task ListWidgetsAsync_ReturnsProperStructure()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListWidgetsAsync(accountId);

    // Assert
    result.Should().NotBeNull();
    foreach (var widget in result.Items)
    {
      widget.Sitekey.Should().NotBeNullOrEmpty();
      widget.Name.Should().NotBeNullOrEmpty();
      widget.Domains.Should().NotBeNull();
      _output.WriteLine($"  Widget: {widget.Name} ({widget.Sitekey}) - Mode: {widget.Mode.Value}");
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
      result.Should().NotBeNull();
      result.Sitekey.Should().NotBeNullOrEmpty();
      result.Name.Should().Be(widgetName);
      result.Mode.Should().Be(WidgetMode.Managed);
      result.Domains.Should().Contain("test.example.com");
      // Secret is returned on creation
      result.Secret.Should().NotBeNullOrEmpty("Secret should be returned on widget creation");

      _output.WriteLine($"Created widget: {result.Sitekey} with name: {result.Name}");
      _output.WriteLine($"  Secret: {result.Secret?[..8]}... (truncated)");
    }
    finally
    {
      await CleanupTestWidgets(accountId);
    }
  }

  /// <summary>I04: Verifies that a widget can be created with all optional fields.</summary>
  [IntegrationTest]
  public async Task CreateWidgetAsync_AllFields_CreatesWidgetWithAllSettings()
  {
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
        BotFightMode: true,
        ClearanceLevel: ClearanceLevel.JsChallenge,
        EphemeralId: false,
        Offlabel: false,
        Region: "world");
      var result = await _sut.CreateWidgetAsync(accountId, request);
      _createdWidgetSitekeys.Add(result.Sitekey);

      // Assert
      result.Should().NotBeNull();
      result.Name.Should().Be(widgetName);
      result.Mode.Should().Be(WidgetMode.Invisible);
      result.Domains.Should().HaveCount(2);
      result.BotFightMode.Should().BeTrue();

      _output.WriteLine($"Created widget with all options: {result.Sitekey}");
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
      result.Should().NotBeNull();
      result.Sitekey.Should().Be(created.Sitekey);
      result.Name.Should().Be(widgetName);
      // Note: Secret is NOT returned on get, only on create/rotate
      result.Secret.Should().BeNull("Secret should not be returned on get");

      _output.WriteLine($"Retrieved widget: {result.Sitekey}");
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
      updated.Should().NotBeNull();
      updated.Sitekey.Should().Be(created.Sitekey);
      updated.Name.Should().Be(updatedName);
      updated.Mode.Should().Be(WidgetMode.NonInteractive);
      updated.Domains.Should().Contain("updated.example.com");

      _output.WriteLine($"Updated widget from '{originalName}' to '{updatedName}'");
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

    _output.WriteLine($"Successfully deleted widget: {created.Sitekey}");
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
      result.Should().NotBeNull();
      result.Secret.Should().NotBeNullOrEmpty();
      result.Secret.Should().NotBe(originalSecret, "New secret should be different from original");

      _output.WriteLine($"Rotated secret with grace period");
      _output.WriteLine($"  Original: {originalSecret?[..8]}... (truncated)");
      _output.WriteLine($"  New: {result.Secret[..8]}... (truncated)");
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
      result.Should().NotBeNull();
      result.Secret.Should().NotBeNullOrEmpty();
      result.Secret.Should().NotBe(originalSecret, "New secret should be different from original");

      _output.WriteLine($"Rotated secret with immediate invalidation");
      _output.WriteLine($"  New secret: {result.Secret[..8]}... (truncated)");
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
      created.Name.Should().Be(originalName);
      created.Secret.Should().NotBeNullOrEmpty();

      _output.WriteLine($"Step 1: Created widget {widgetSitekey}");

      // 2. Get
      var retrieved = await _sut.GetWidgetAsync(accountId, widgetSitekey);
      retrieved.Sitekey.Should().Be(widgetSitekey);
      retrieved.Name.Should().Be(originalName);
      retrieved.Secret.Should().BeNull("Secret not returned on get");

      _output.WriteLine("Step 2: Retrieved widget successfully");

      // 3. Update
      var updateRequest = new UpdateTurnstileWidgetRequest(
        Name: updatedName,
        Domains: ["updated.example.com"],
        Mode: WidgetMode.Invisible);
      var updated = await _sut.UpdateWidgetAsync(accountId, widgetSitekey, updateRequest);
      updated.Name.Should().Be(updatedName);
      updated.Mode.Should().Be(WidgetMode.Invisible);

      _output.WriteLine("Step 3: Updated widget successfully");

      // 4. Rotate Secret
      var originalSecret = created.Secret;
      var rotated = await _sut.RotateSecretAsync(accountId, widgetSitekey);
      rotated.Secret.Should().NotBe(originalSecret);

      _output.WriteLine("Step 4: Rotated secret successfully");

      // 5. Delete
      await _sut.DeleteWidgetAsync(accountId, widgetSitekey);
      widgetSitekey = null; // Mark as deleted

      _output.WriteLine("Step 5: Deleted widget successfully");

      // 6. Verify deletion
      var action = async () => await _sut.GetWidgetAsync(accountId, created.Sitekey);
      await action.Should().ThrowAsync<HttpRequestException>()
        .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);

      _output.WriteLine("Step 6: Verified widget is deleted");
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

  /// <summary>I11: Verifies that getting a non-existent widget returns 404.</summary>
  [IntegrationTest]
  public async Task GetWidgetAsync_NonExistent_ThrowsHttpRequestException()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentSitekey = "0x0000000000000000000000";

    // Act
    var action = async () => await _sut.GetWidgetAsync(accountId, nonExistentSitekey);

    // Assert
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
  }

  /// <summary>I12: Verifies that deleting a non-existent widget returns appropriate error.</summary>
  [IntegrationTest]
  public async Task DeleteWidgetAsync_NonExistent_ThrowsHttpRequestException()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentSitekey = "0x0000000000000000000000";

    // Act
    var action = async () => await _sut.DeleteWidgetAsync(accountId, nonExistentSitekey);

    // Assert
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
  }

  /// <summary>I13: Verifies that rotating secret for non-existent widget returns error.</summary>
  [IntegrationTest]
  public async Task RotateSecretAsync_NonExistent_ThrowsHttpRequestException()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentSitekey = "0x0000000000000000000000";

    // Act
    var action = async () => await _sut.RotateSecretAsync(accountId, nonExistentSitekey);

    // Assert
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
  }

  /// <summary>I14: Verifies behavior with invalid account ID format.</summary>
  [IntegrationTest]
  public async Task ListWidgetsAsync_InvalidAccountId_ThrowsHttpRequestException()
  {
    // Arrange
    var invalidAccountId = "invalid-account-id-format";

    // Act
    var action = async () => await _sut.ListWidgetsAsync(invalidAccountId);

    // Assert
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().BeOneOf(
      HttpStatusCode.NotFound,
      HttpStatusCode.BadRequest,
      HttpStatusCode.Forbidden);
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

    // Assert
    _output.WriteLine($"ListAllWidgetsAsync yielded {widgets.Count} widgets");
    foreach (var widget in widgets)
    {
      widget.Sitekey.Should().NotBeNullOrEmpty();
      _output.WriteLine($"  - {widget.Name} ({widget.Sitekey})");
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
    result.Should().NotBeNull();
    result.Items.Count.Should().BeLessThanOrEqualTo(5);
    // PageInfo should be present
    if (result.PageInfo != null)
    {
      result.PageInfo.Page.Should().Be(1);
      result.PageInfo.PerPage.Should().Be(5);
    }

    _output.WriteLine($"Page 1 (max 5 per page): {result.Items.Count} widgets");
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
        _output.WriteLine($"Cleaned up widget: {sitekey}");
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
