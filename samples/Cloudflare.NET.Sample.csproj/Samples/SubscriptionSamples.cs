namespace Cloudflare.NET.Sample.Samples;

using Microsoft.Extensions.Logging;
using Subscriptions.Models;

/// <summary>
///   Demonstrates Subscriptions API operations including:
///   <list type="bullet">
///     <item><description>F18: Account Subscriptions (list, create, update, delete)</description></item>
///     <item><description>F19: User Subscriptions (list, update, delete)</description></item>
///     <item><description>F20: Zone Subscriptions (get, create, update, list rate plans)</description></item>
///   </list>
/// </summary>
/// <remarks>
///   <para>
///     <b>Billing Permissions Required:</b> Subscriptions API requires tokens with
///     Billing Read and/or Billing Write permissions.
///   </para>
///   <para>
///     <b>Cost Warning:</b> Creating or updating subscriptions may incur charges.
///     Use test/sandbox accounts for development.
///   </para>
///   <para>
///     <b>Externally Managed Subscriptions:</b> Some subscriptions are managed
///     outside of Cloudflare (e.g., through partners) and cannot be modified via API.
///   </para>
/// </remarks>
public class SubscriptionSamples(ICloudflareApiClient cf, ILogger<SubscriptionSamples> logger)
{
  #region Methods - Account Subscriptions (F18)

  /// <summary>
  ///   Demonstrates Account Subscriptions operations.
  ///   <para>
  ///     Account subscriptions include Workers, R2, Images, and other account-level products.
  ///     Requires Billing Read permission to list, Billing Write to modify.
  ///   </para>
  /// </summary>
  public async Task RunAccountSubscriptionsSamplesAsync(string accountId)
  {
    logger.LogInformation("=== F18: Account Subscriptions Operations ===");

    // 1. List all account subscriptions.
    logger.LogInformation("--- Listing Account Subscriptions ---");

    try
    {
      var subscriptions = await cf.Subscriptions.ListAccountSubscriptionsAsync(accountId);
      logger.LogInformation("Account subscriptions: {Count}", subscriptions.Count);

      foreach (var sub in subscriptions.Take(5))
      {
        logger.LogInformation("  Subscription: {Id}", sub.Id);
        logger.LogInformation("    Rate Plan: {RatePlan}", sub.RatePlan?.PublicName ?? "N/A");
        logger.LogInformation("    Price:     {Price} {Currency}", sub.Price, sub.Currency);
        logger.LogInformation("    Frequency: {Frequency}", sub.Frequency);
        logger.LogInformation("    State:     {State}", sub.State);

        if (sub.CurrentPeriodStart.HasValue)
          logger.LogInformation("    Period:    {Start} to {End}", sub.CurrentPeriodStart, sub.CurrentPeriodEnd);

        if (sub.ComponentValues is { Count: > 0 })
        {
          logger.LogInformation("    Components:");

          foreach (var component in sub.ComponentValues.Take(3))
            logger.LogInformation("      - {Name}: {Value} (default: {Default})", component.Name, component.Value, component.Default ?? 0);
        }
      }

      if (subscriptions.Count > 5)
        logger.LogInformation("  ... and {Count} more", subscriptions.Count - 5);

      if (subscriptions.Count == 0)
        logger.LogInformation("No account subscriptions found.");
    }
    catch (Exception ex)
    {
      logger.LogWarning("Failed to list account subscriptions: {Message}", ex.Message);
      logger.LogInformation("Note: This operation requires Billing Read permission.");
    }

    // Note: Create, update, and delete are Preview operations and may incur charges.
    // They are demonstrated conceptually to avoid accidental billing changes.
    logger.LogInformation("--- Account Subscription Management (Conceptual) ---");
    logger.LogInformation("To create a subscription, use CreateAccountSubscriptionAsync with:");
    logger.LogInformation("  - RatePlan: Reference to the plan ID");
    logger.LogInformation("  - Frequency: Monthly or Yearly");
    logger.LogInformation("  - ComponentValues (optional): Usage-based components");
    logger.LogInformation("");
    logger.LogInformation("To update a subscription, use UpdateAccountSubscriptionAsync.");
    logger.LogInformation("To cancel a subscription, use DeleteAccountSubscriptionAsync.");
    logger.LogInformation("");
    logger.LogInformation("WARNING: These operations may incur billing charges!");
  }

  #endregion


  #region Methods - User Subscriptions (F19)

  /// <summary>
  ///   Demonstrates User Subscriptions operations.
  ///   <para>
  ///     User subscriptions are owned by the authenticated user.
  ///     Requires Billing Read permission to list, Billing Write to modify.
  ///   </para>
  /// </summary>
  public async Task RunUserSubscriptionsSamplesAsync()
  {
    logger.LogInformation("=== F19: User Subscriptions Operations ===");

    // 1. List all user subscriptions.
    logger.LogInformation("--- Listing User Subscriptions ---");

    try
    {
      var subscriptions = await cf.Subscriptions.ListUserSubscriptionsAsync();
      logger.LogInformation("User subscriptions: {Count}", subscriptions.Count);

      foreach (var sub in subscriptions.Take(5))
      {
        logger.LogInformation("  Subscription: {Id}", sub.Id);
        logger.LogInformation("    Rate Plan: {RatePlan}", sub.RatePlan?.PublicName ?? "N/A");
        logger.LogInformation("    Price:     {Price} {Currency}", sub.Price, sub.Currency);
        logger.LogInformation("    Frequency: {Frequency}", sub.Frequency);
        logger.LogInformation("    State:     {State}", sub.State);

        if (sub.CurrentPeriodStart.HasValue)
          logger.LogInformation("    Period:    {Start} to {End}", sub.CurrentPeriodStart, sub.CurrentPeriodEnd);

        if (sub.ComponentValues is { Count: > 0 })
        {
          logger.LogInformation("    Components:");

          foreach (var component in sub.ComponentValues.Take(3))
            logger.LogInformation("      - {Name}: {Value}", component.Name, component.Value);
        }
      }

      if (subscriptions.Count > 5)
        logger.LogInformation("  ... and {Count} more", subscriptions.Count - 5);

      if (subscriptions.Count == 0)
        logger.LogInformation("No user subscriptions found.");
    }
    catch (Exception ex)
    {
      logger.LogWarning("Failed to list user subscriptions: {Message}", ex.Message);
      logger.LogInformation("Note: This operation requires Billing Read permission.");
    }

    // Note: Update and delete are Preview operations and may affect billing.
    // They are demonstrated conceptually to avoid accidental changes.
    logger.LogInformation("--- User Subscription Management (Conceptual) ---");
    logger.LogInformation("To update a subscription, use UpdateUserSubscriptionAsync with:");
    logger.LogInformation("  - Frequency: Change billing cycle (Monthly/Yearly)");
    logger.LogInformation("  - RatePlan: Change the plan");
    logger.LogInformation("");
    logger.LogInformation("To cancel a subscription, use DeleteUserSubscriptionAsync.");
    logger.LogInformation("");
    logger.LogInformation("WARNING: These operations may affect billing!");
  }

  #endregion


  #region Methods - Zone Subscriptions (F20)

  /// <summary>
  ///   Demonstrates Zone Subscriptions operations.
  ///   <para>
  ///     Zone subscriptions determine the plan level (Free, Pro, Business, Enterprise)
  ///     for a specific zone. All zones have a subscription (at minimum, Free).
  ///   </para>
  /// </summary>
  public async Task RunZoneSubscriptionsSamplesAsync(string zoneId)
  {
    logger.LogInformation("=== F20: Zone Subscriptions Operations ===");

    // 1. Get zone subscription.
    logger.LogInformation("--- Getting Zone Subscription ---");

    try
    {
      var subscription = await cf.Subscriptions.GetZoneSubscriptionAsync(zoneId);
      logger.LogInformation("Zone Subscription:");
      logger.LogInformation("  Id:        {Id}", subscription.Id);
      logger.LogInformation("  Rate Plan: {RatePlan}", subscription.RatePlan?.PublicName ?? "N/A");
      logger.LogInformation("  Price:     {Price} {Currency}", subscription.Price, subscription.Currency);
      logger.LogInformation("  Frequency: {Frequency}", subscription.Frequency);
      logger.LogInformation("  State:     {State}", subscription.State);

      if (subscription.CurrentPeriodStart.HasValue)
        logger.LogInformation("  Period:    {Start} to {End}", subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd);

      // Display rate plan details.
      if (subscription.RatePlan is not null)
      {
        logger.LogInformation("  Rate Plan Details:");
        logger.LogInformation("    Id:            {Id}", subscription.RatePlan.Id);
        logger.LogInformation("    Name:          {Name}", subscription.RatePlan.PublicName);
        logger.LogInformation("    Externally Managed: {ExternallyManaged}", subscription.RatePlan.ExternallyManaged);
      }
    }
    catch (Exception ex)
    {
      logger.LogWarning("Failed to get zone subscription: {Message}", ex.Message);
      logger.LogInformation("Note: This operation requires Billing Read permission.");
    }

    // 2. List available rate plans.
    logger.LogInformation("--- Listing Available Rate Plans ---");

    try
    {
      var ratePlans = await cf.Subscriptions.ListAvailableRatePlansAsync(zoneId);
      logger.LogInformation("Available rate plans: {Count}", ratePlans.Count);

      foreach (var plan in ratePlans)
      {
        logger.LogInformation("  Plan: {Name}", plan.Name);
        logger.LogInformation("    Id:          {Id}", plan.Id);
        logger.LogInformation("    Currency:    {Currency}", plan.Currency);
        logger.LogInformation("    Frequency:   {Frequency}", plan.Frequency);
        logger.LogInformation("    Duration:    {Duration} periods", plan.Duration);

        if (plan.Components is { Count: > 0 })
        {
          logger.LogInformation("    Components:");

          foreach (var component in plan.Components.Take(3))
            logger.LogInformation("      - {Name}: default={Default}, unit price={UnitPrice}", component.Name, component.Default, component.UnitPrice);
        }
      }
    }
    catch (Exception ex)
    {
      logger.LogWarning("Failed to list rate plans: {Message}", ex.Message);
    }

    // Note: Create and update are Preview operations and WILL incur billing charges.
    // They are demonstrated conceptually to avoid accidental plan changes.
    logger.LogInformation("--- Zone Subscription Management (Conceptual) ---");
    logger.LogInformation("To upgrade a zone to a paid plan, use CreateZoneSubscriptionAsync with:");
    logger.LogInformation("  - RatePlan: Reference to the plan ID (e.g., 'pro', 'business')");
    logger.LogInformation("  - Frequency: Monthly or Yearly");
    logger.LogInformation("");
    logger.LogInformation("To change the plan, use UpdateZoneSubscriptionAsync.");
    logger.LogInformation("");
    logger.LogInformation("Common zone plans:");
    logger.LogInformation("  - Free:       Basic protection, limited features");
    logger.LogInformation("  - Pro:        Enhanced protection, WAF, image optimization");
    logger.LogInformation("  - Business:   Advanced features, SLA, priority support");
    logger.LogInformation("  - Enterprise: Custom limits, premium features, dedicated support");
    logger.LogInformation("");
    logger.LogInformation("WARNING: Upgrading plans WILL incur billing charges!");
  }

  #endregion
}
