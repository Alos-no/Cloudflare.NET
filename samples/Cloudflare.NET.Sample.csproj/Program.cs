namespace Cloudflare.NET.Sample;

using Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Samples;

/// <summary>
///   Entry point for the Core REST sample. This demonstrates how to: - Build a Generic Host and wire up the
///   Cloudflare REST client. - Run sample scenarios for various API features like Zones, Accounts (R2), and Security. -
///   Ensure created resources are cleaned up after the sample runs.
/// </summary>
public static class Program
{
  #region Methods

  /// <summary>
  ///   Main entry point. Sets up the Host, resolves the REST client and sample classes, and runs a series of
  ///   end-to-end scenarios.
  /// </summary>
  public static async Task Main(string[] args)
  {
    var builder = Host.CreateApplicationBuilder(args);

    // Configure logging.
    builder.Logging.ClearProviders();
    builder.Logging.AddSimpleConsole(o =>
    {
      o.SingleLine      = true;
      o.IncludeScopes   = true;
      o.TimestampFormat = "HH:mm:ss.fff ";
    });
    builder.Logging.SetMinimumLevel(LogLevel.Information);

    // Register the Cloudflare REST client.
    builder.Services.AddCloudflareApiClient(builder.Configuration);

    // Register sample classes.
    builder.Services.AddTransient<ZoneSamples>();
    builder.Services.AddTransient<AccountSamples>();
    builder.Services.AddTransient<SecuritySamples>();
    builder.Services.AddTransient<CustomHostnameSamples>();
    builder.Services.AddTransient<UserSamples>();
    builder.Services.AddTransient<WorkerSamples>();
    builder.Services.AddTransient<TurnstileSamples>();
    builder.Services.AddTransient<SubscriptionSamples>();

    using var host          = builder.Build();
    var       logger        = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("CloudflareSample");
    var       configuration = host.Services.GetRequiredService<IConfiguration>();

    // Validate configuration.
    var options = host.Services.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;
    var zoneId  = configuration["Cloudflare:ZoneId"];

    if (IsSecretMissing(options.ApiToken, "ApiToken", logger) ||
        IsSecretMissing(options.AccountId, "AccountId", logger) ||
        IsSecretMissing(zoneId, "ZoneId", logger))
      return; // Errors are logged in the helper.

    // Get the base domain name from the zone details for use in other samples.
    var baseDomain = await GetBaseDomainAsync(host.Services, zoneId!, logger);

    if (string.IsNullOrEmpty(baseDomain))
    {
      logger.LogError("Could not determine base domain from Zone ID. Aborting.");
      return;
    }

    // Resolve sample runners from DI container.
    var zoneSamples           = host.Services.GetRequiredService<ZoneSamples>();
    var accountSamples        = host.Services.GetRequiredService<AccountSamples>();
    var securitySamples       = host.Services.GetRequiredService<SecuritySamples>();
    var customHostnameSamples = host.Services.GetRequiredService<CustomHostnameSamples>();
    var userSamples           = host.Services.GetRequiredService<UserSamples>();
    var workerSamples         = host.Services.GetRequiredService<WorkerSamples>();
    var turnstileSamples      = host.Services.GetRequiredService<TurnstileSamples>();
    var subscriptionSamples   = host.Services.GetRequiredService<SubscriptionSamples>();

    logger.LogInformation("Starting Cloudflare.NET SDK Samples...");
    logger.LogInformation("Using AccountId: {AccountId}, ZoneId: {ZoneId}", options.AccountId, zoneId);

    // ========================================================================
    // ZONE OPERATIONS (F01-F05)
    // ========================================================================

    // F01: Zone CRUD (list, get, create, edit, delete, activation check).
    await Runner.RunAsync(logger, "F01: Zone CRUD",
                          () => zoneSamples.RunZoneCrudSamplesAsync(zoneId!));

    // F02: Zone Holds (get, create, update, remove).
    await Runner.RunAsync(logger, "F02: Zone Holds",
                          () => zoneSamples.RunZoneHoldSamplesAsync(zoneId!));

    // F03: Zone Settings (get, set).
    await Runner.RunAsync(logger, "F03: Zone Settings",
                          () => zoneSamples.RunZoneSettingsSamplesAsync(zoneId!));

    // F04: DNS Record CRUD via IDnsApi (create, get, list, update, patch, delete, batch, import/export).
    await Runner.RunAsync(logger, "F04: DNS Record CRUD",
                          () => zoneSamples.RunDnsRecordCrudSamplesAsync(zoneId!));

    // F05: DNS Record Scanning (trigger, review, submit).
    await Runner.RunAsync(logger, "F05: DNS Record Scanning",
                          () => zoneSamples.RunDnsRecordScanningSamplesAsync(zoneId!));

    // Legacy combined DNS samples (for backwards compatibility).
    await Runner.RunAsync(logger, "Zone/DNS Management",
                          () => zoneSamples.RunDnsSamplesAsync(zoneId!));

    // ========================================================================
    // ACCOUNT OPERATIONS (F06-F10)
    // ========================================================================

    // F06: Account Management (list, get, update accounts).
    await Runner.RunAsync(logger, "F06: Account Management",
                          () => accountSamples.RunAccountManagementSamplesAsync(options.AccountId!));

    // F07: Account Audit Logs (list, filter audit logs).
    await Runner.RunAsync(logger, "F07: Account Audit Logs",
                          () => accountSamples.RunAccountAuditLogsSamplesAsync(options.AccountId!));

    // F08: Account API Tokens (CRUD, verify, roll, permission groups).
    await Runner.RunAsync(logger, "F08: Account API Tokens",
                          () => accountSamples.RunAccountApiTokensSamplesAsync(options.AccountId!));

    // F09: Account Members (list, get, create, update, delete members).
    await Runner.RunAsync(logger, "F09: Account Members",
                          () => accountSamples.RunAccountMembersSamplesAsync(options.AccountId!));

    // F10: Account Roles (list, get roles).
    await Runner.RunAsync(logger, "F10: Account Roles",
                          () => accountSamples.RunAccountRolesSamplesAsync(options.AccountId!));

    // R2 Bucket operations (existing sample).
    await Runner.RunAsync(logger, "Account/R2 Management",
                          () => accountSamples.RunR2SamplesAsync(zoneId!, baseDomain));

    // ========================================================================
    // USER OPERATIONS (F11, F14-F17)
    // ========================================================================

    // F14: User Management (get, edit user profile).
    await Runner.RunAsync(logger, "F14: User Management",
                          userSamples.RunUserManagementSamplesAsync);

    // F11: User Memberships (list, get, update, delete memberships).
    await Runner.RunAsync(logger, "F11: User Memberships",
                          userSamples.RunUserMembershipsSamplesAsync);

    // F15: User Audit Logs (list user audit logs).
    await Runner.RunAsync(logger, "F15: User Audit Logs",
                          userSamples.RunUserAuditLogsSamplesAsync);

    // F16: User Invitations (list, get, respond to invitations).
    await Runner.RunAsync(logger, "F16: User Invitations",
                          userSamples.RunUserInvitationsSamplesAsync);

    // F17: User API Tokens (CRUD, verify, roll, permission groups).
    await Runner.RunAsync(logger, "F17: User API Tokens",
                          userSamples.RunUserApiTokensSamplesAsync);

    // ========================================================================
    // WORKERS (F12)
    // ========================================================================

    // F12: Worker Routes (list, get, create, update, delete routes).
    await Runner.RunAsync(logger, "F12: Worker Routes",
                          () => workerSamples.RunWorkerRoutesSamplesAsync(zoneId!, baseDomain));

    // ========================================================================
    // TURNSTILE (F13)
    // ========================================================================

    // F13: Turnstile Widgets (CRUD, secret rotation).
    await Runner.RunAsync(logger, "F13: Turnstile Widgets",
                          () => turnstileSamples.RunTurnstileWidgetsSamplesAsync(options.AccountId!));

    // ========================================================================
    // SUBSCRIPTIONS (F18-F20)
    // ========================================================================

    // F18: Account Subscriptions (list, create, update, delete).
    await Runner.RunAsync(logger, "F18: Account Subscriptions",
                          () => subscriptionSamples.RunAccountSubscriptionsSamplesAsync(options.AccountId!));

    // F19: User Subscriptions (list, update, delete).
    await Runner.RunAsync(logger, "F19: User Subscriptions",
                          subscriptionSamples.RunUserSubscriptionsSamplesAsync);

    // F20: Zone Subscriptions (get, create, update, list rate plans).
    await Runner.RunAsync(logger, "F20: Zone Subscriptions",
                          () => subscriptionSamples.RunZoneSubscriptionsSamplesAsync(zoneId!));

    // ========================================================================
    // SECURITY OPERATIONS (existing samples)
    // ========================================================================

    await Runner.RunAsync(logger, "Security/Zone Firewall",
                          () => securitySamples.RunZoneFirewallSamplesAsync(zoneId!));

    await Runner.RunAsync(logger, "Security/Account Firewall",
                          securitySamples.RunAccountFirewallSamplesAsync);

    await Runner.RunAsync(logger, "Custom Hostnames (Cloudflare for SaaS)",
                          () => customHostnameSamples.RunCustomHostnameSamplesAsync(zoneId!, baseDomain));

    logger.LogInformation("All sample scenarios complete.");
  }

  /// <summary>Fetches zone details to get the domain name for use in samples that create hostnames.</summary>
  private static async Task<string> GetBaseDomainAsync(IServiceProvider services, string zoneId, ILogger logger)
  {
    var cf = services.GetRequiredService<ICloudflareApiClient>();

    try
    {
      var zone = await cf.Zones.GetZoneDetailsAsync(zoneId);
      return zone.Name;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to get zone details. Check your credentials and ZoneId.");
      return string.Empty;
    }
  }

  /// <summary>Checks if a configuration value is missing and logs an error if it is.</summary>
  private static bool IsSecretMissing(string? value, string name, ILogger logger)
  {
    if (string.IsNullOrWhiteSpace(value) || value.Equals("from-user-secrets", StringComparison.OrdinalIgnoreCase))
    {
      logger.LogError(
        "Configuration value 'Cloudflare:{Name}' is missing. Please configure appsettings.json, environment variables, or user secrets.",
        name);
      return true;
    }

    return false;
  }

  #endregion
}
