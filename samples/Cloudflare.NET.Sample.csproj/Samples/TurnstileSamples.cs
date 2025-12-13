namespace Cloudflare.NET.Sample.Samples;

using Microsoft.Extensions.Logging;
using Turnstile.Models;

/// <summary>
///   Demonstrates Turnstile API operations including:
///   <list type="bullet">
///     <item><description>F13: Turnstile Widgets (CRUD, secret rotation)</description></item>
///   </list>
/// </summary>
/// <remarks>
///   <para>
///     Turnstile is Cloudflare's CAPTCHA alternative that provides bot protection
///     without user friction. Widgets are account-scoped and can be configured
///     with different modes (invisible, managed, non-interactive).
///   </para>
///   <para>
///     <b>Important:</b> Widget secrets are only returned on creation and rotation.
///     Store them securely as they cannot be retrieved again.
///   </para>
/// </remarks>
public class TurnstileSamples(ICloudflareApiClient cf, ILogger<TurnstileSamples> logger)
{
  #region Methods - Turnstile Widgets (F13)

  /// <summary>
  ///   Demonstrates Turnstile Widget operations.
  ///   <para>
  ///     Widgets can operate in three modes:
  ///     - Managed: User sees a checkbox challenge
  ///     - Non-Interactive: Runs automatically, shows loading indicator
  ///     - Invisible: Runs automatically, completely invisible to users
  ///   </para>
  /// </summary>
  public async Task<List<Func<Task>>> RunTurnstileWidgetsSamplesAsync(string accountId)
  {
    var cleanupActions = new List<Func<Task>>();
    logger.LogInformation("=== F13: Turnstile Widgets Operations ===");

    // 1. List all Turnstile widgets.
    logger.LogInformation("--- Listing Turnstile Widgets ---");
    var widgetsPage = await cf.Turnstile.ListWidgetsAsync(accountId, new ListTurnstileWidgetsFilters(PerPage: 10));
    logger.LogInformation("Existing widgets: {Count}", widgetsPage.Items.Count);

    foreach (var widget in widgetsPage.Items.Take(5))
    {
      logger.LogInformation("  Widget: {Name}", widget.Name);
      logger.LogInformation("    Sitekey: {Sitekey}", widget.Sitekey);
      logger.LogInformation("    Mode:    {Mode}", widget.Mode);
      logger.LogInformation("    Domains: {Domains}", string.Join(", ", widget.Domains ?? []));
    }

    // 2. List all widgets with automatic pagination.
    logger.LogInformation("--- Listing All Widgets (paginated) ---");
    var widgetCount = 0;

    await foreach (var widget in cf.Turnstile.ListAllWidgetsAsync(accountId))
    {
      widgetCount++;

      if (widgetCount > 20)
        break;
    }

    logger.LogInformation("Total widgets: {Count}+", widgetCount);

    // 3. Create a new Turnstile widget.
    logger.LogInformation("--- Creating Turnstile Widget ---");

    var widgetName = $"SDK Sample Widget {Guid.NewGuid():N}";

    try
    {
      var createRequest = new CreateTurnstileWidgetRequest(
        Name:    widgetName,
        Domains: new[] { "localhost", "127.0.0.1" },  // Safe test domains
        Mode:    WidgetMode.Managed,                   // User sees a checkbox
        BotFightMode: false,                           // Don't enable Super Bot Fight Mode
        Region:       "world"                          // Global region (string value)
      );

      var createdWidget = await cf.Turnstile.CreateWidgetAsync(accountId, createRequest);
      logger.LogInformation("Created widget: {Name}", createdWidget.Name);
      logger.LogInformation("  Sitekey: {Sitekey}", createdWidget.Sitekey);
      logger.LogInformation("  Secret:  {Secret}", createdWidget.Secret);
      logger.LogInformation("");
      logger.LogInformation("  *** IMPORTANT: Store the secret securely! ***");
      logger.LogInformation("  It cannot be retrieved again after creation.");
      logger.LogInformation("");
      logger.LogInformation("  Mode:    {Mode}", createdWidget.Mode);
      logger.LogInformation("  Domains: {Domains}", string.Join(", ", createdWidget.Domains ?? []));

      // Add cleanup action.
      cleanupActions.Add(async () =>
      {
        logger.LogInformation("Deleting Turnstile widget: {Sitekey}", createdWidget.Sitekey);
        await cf.Turnstile.DeleteWidgetAsync(accountId, createdWidget.Sitekey);
        logger.LogInformation("Deleted Turnstile widget: {Sitekey}", createdWidget.Sitekey);
      });

      // 4. Get the created widget.
      logger.LogInformation("--- Getting Widget Details ---");
      var widgetDetails = await cf.Turnstile.GetWidgetAsync(accountId, createdWidget.Sitekey);
      logger.LogInformation("Widget Details:");
      logger.LogInformation("  Sitekey:   {Sitekey}", widgetDetails.Sitekey);
      logger.LogInformation("  Name:      {Name}", widgetDetails.Name);
      logger.LogInformation("  Mode:      {Mode}", widgetDetails.Mode);
      logger.LogInformation("  Domains:   {Domains}", string.Join(", ", widgetDetails.Domains ?? []));
      logger.LogInformation("  Created:   {CreatedOn}", widgetDetails.CreatedOn);

      // Note: Secret is NOT returned on GET - only on creation and rotation!
      logger.LogInformation("  Secret:    (not returned on GET - only on creation/rotation)");

      // 5. Update the widget.
      logger.LogInformation("--- Updating Widget ---");
      var updateRequest = new UpdateTurnstileWidgetRequest(
        Name:    $"{widgetName} [Updated]",
        Domains: new[] { "localhost", "127.0.0.1", "example.com" },
        Mode:    WidgetMode.NonInteractive  // Change to non-interactive mode
      );
      var updatedWidget = await cf.Turnstile.UpdateWidgetAsync(accountId, createdWidget.Sitekey, updateRequest);
      logger.LogInformation("Updated widget:");
      logger.LogInformation("  Name:    {Name}", updatedWidget.Name);
      logger.LogInformation("  Mode:    {Mode}", updatedWidget.Mode);
      logger.LogInformation("  Domains: {Domains}", string.Join(", ", updatedWidget.Domains ?? []));

      // 6. Rotate the widget secret.
      logger.LogInformation("--- Rotating Widget Secret ---");
      logger.LogInformation("Rotating secret (with 2-hour grace period for old secret)...");
      var rotateResult = await cf.Turnstile.RotateSecretAsync(accountId, createdWidget.Sitekey, invalidateImmediately: false);
      logger.LogInformation("New secret: {Secret}", rotateResult.Secret);
      logger.LogInformation("");
      logger.LogInformation("  The old secret remains valid for 2 hours.");
      logger.LogInformation("  Use invalidateImmediately: true to revoke immediately.");
    }
    catch (Exception ex)
    {
      logger.LogWarning("Turnstile widget operation failed: {Message}", ex.Message);
    }

    // 7. Widget mode comparison.
    logger.LogInformation("--- Widget Modes ---");
    logger.LogInformation("Available modes:");
    logger.LogInformation("  Managed:        User sees a checkbox, may need to complete a challenge");
    logger.LogInformation("  NonInteractive: Runs automatically, shows a loading indicator");
    logger.LogInformation("  Invisible:      Runs in background, completely invisible to users");
    logger.LogInformation("");
    logger.LogInformation("Integration steps:");
    logger.LogInformation("  1. Add Turnstile script to your page:");
    logger.LogInformation("     <script src=\"https://challenges.cloudflare.com/turnstile/v0/api.js\" async></script>");
    logger.LogInformation("  2. Add widget container: <div class=\"cf-turnstile\" data-sitekey=\"YOUR_SITEKEY\"></div>");
    logger.LogInformation("  3. Server-side: Verify the token using the secret key");

    return cleanupActions;
  }

  #endregion
}
