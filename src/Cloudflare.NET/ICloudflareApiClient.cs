namespace Cloudflare.NET;

using Accounts;
using ApiTokens;
using AuditLogs;
using Dns;
using Members;
using Roles;
using Subscriptions;
using Turnstile;
using User;
using Workers;
using Zones;

/// <summary>
///   Defines the contract for the primary client for interacting with the Cloudflare API. This client acts as a
///   facade, providing access to different API resource areas such as Accounts, User, and Zones.
/// </summary>
/// <remarks>
///   <para>
///     This is the main entry point for the SDK. It should be registered with a dependency injection container as a
///     transient service using the <c>AddCloudflareApiClient()</c> extension method.
///   </para>
///   <example>
///     Inject this interface into your services to access all Cloudflare APIs:
///     <code>
/// public class MyService(ICloudflareApiClient cf)
/// {
///     public async Task DoSomething()
///     {
///         var zones = await cf.Zones.ListZonesAsync();
///         var user = await cf.User.GetUserAsync();
///         // ...
///     }
/// }
/// </code>
///   </example>
/// </remarks>
public interface ICloudflareApiClient
{
  #region Properties & Fields - Public

  /// <summary>
  ///   Gets the API resource for managing Account-level resources. This includes R2 buckets, account-wide IP Access
  ///   Rules, and account-level WAF custom rulesets.
  /// </summary>
  IAccountsApi Accounts { get; }

  /// <summary>
  ///   Gets the API resource for managing the authenticated user's profile.
  ///   <para>
  ///     This includes retrieving and editing the current user's personal information,
  ///     such as name, country, and contact details.
  ///   </para>
  /// </summary>
  IUserApi User { get; }

  /// <summary>
  ///   Gets the API resource for managing Zone-level resources. This includes DNS records, zone-specific IP Access
  ///   Rules, WAF configurations, and other settings scoped to a specific zone.
  /// </summary>
  IZonesApi Zones { get; }

  /// <summary>
  ///   Gets the API resource for managing DNS records.
  ///   <para>
  ///     Provides complete CRUD operations for DNS records, including batch operations,
  ///     import/export functionality, and searching by hostname.
  ///   </para>
  /// </summary>
  /// <example>
  ///   <code>
  ///   // Create a DNS record
  ///   var record = await cf.Dns.CreateDnsRecordAsync(zoneId, new CreateDnsRecordRequest(
  ///     DnsRecordType.A, "www.example.com", "192.0.2.1"
  ///   ));
  ///
  ///   // List all DNS records
  ///   await foreach (var r in cf.Dns.ListAllDnsRecordsAsync(zoneId))
  ///   {
  ///     Console.WriteLine($"{r.Name}: {r.Content}");
  ///   }
  ///   </code>
  /// </example>
  IDnsApi Dns { get; }

  /// <summary>
  ///   Gets the API resource for accessing audit logs.
  ///   <para>
  ///     Audit logs provide a record of actions taken on account resources, useful for
  ///     security monitoring, compliance, and troubleshooting. Logs are retained for 30 days.
  ///   </para>
  /// </summary>
  /// <example>
  ///   <code>
  ///   // Get recent audit logs
  ///   var logs = await cf.AuditLogs.GetAccountAuditLogsAsync(accountId,
  ///     new ListAuditLogsFilters(Since: DateTime.UtcNow.AddDays(-7)));
  ///
  ///   foreach (var log in logs.Items)
  ///   {
  ///     Console.WriteLine($"{log.Action.Time}: {log.Action.Type} by {log.Actor.Email}");
  ///   }
  ///   </code>
  /// </example>
  IAuditLogsApi AuditLogs { get; }

  /// <summary>
  ///   Gets the API resource for managing API tokens.
  ///   <para>
  ///     API tokens provide fine-grained access control for Cloudflare API operations.
  ///     Each token has policies that define what resources and actions are allowed.
  ///   </para>
  /// </summary>
  /// <example>
  ///   <code>
  ///   // Create a new token
  ///   var result = await cf.ApiTokens.CreateAccountTokenAsync(accountId,
  ///     new CreateApiTokenRequest(
  ///       Name: "CI/CD Token",
  ///       Policies: new[]
  ///       {
  ///         new CreateTokenPolicyRequest(
  ///           Effect: "allow",
  ///           PermissionGroups: new[] { new TokenPermissionGroupReference(permGroupId) },
  ///           Resources: new Dictionary&lt;string, string&gt;
  ///           {
  ///             ["com.cloudflare.api.account.*"] = "*"
  ///           })
  ///       }));
  ///
  ///   Console.WriteLine($"Token: {result.Value}"); // Store securely!
  ///   </code>
  /// </example>
  IApiTokensApi ApiTokens { get; }

  /// <summary>
  ///   Gets the API resource for managing account roles.
  ///   <para>
  ///     Roles are predefined by Cloudflare and define sets of permissions
  ///     that can be assigned to account members. This API is read-only.
  ///   </para>
  /// </summary>
  /// <example>
  ///   <code>
  ///   // List all available roles
  ///   await foreach (var role in cf.Roles.ListAllAccountRolesAsync(accountId))
  ///   {
  ///     Console.WriteLine($"{role.Name}: {role.Description}");
  ///     if (role.Permissions.Dns?.Write == true)
  ///       Console.WriteLine("  - Can modify DNS");
  ///   }
  ///   </code>
  /// </example>
  IRolesApi Roles { get; }

  /// <summary>
  ///   Gets the API resource for managing account members.
  ///   <para>
  ///     Account members are users who have been granted access to a Cloudflare account.
  ///     Members are assigned roles that determine their permissions within the account.
  ///   </para>
  /// </summary>
  /// <example>
  ///   <code>
  ///   // List all account members
  ///   await foreach (var member in cf.Members.ListAllAccountMembersAsync(accountId))
  ///   {
  ///     Console.WriteLine($"{member.User.Email}: {member.Status}");
  ///     foreach (var role in member.Roles)
  ///       Console.WriteLine($"  - {role.Name}");
  ///   }
  ///
  ///   // Invite a new member
  ///   var newMember = await cf.Members.CreateAccountMemberAsync(accountId,
  ///     new CreateAccountMemberRequest(
  ///       Email: "newuser@example.com",
  ///       Roles: new[] { adminRoleId },
  ///       Status: MemberStatus.Pending));
  ///   </code>
  /// </example>
  IMembersApi Members { get; }

  /// <summary>
  ///   Gets the API resource for managing subscriptions.
  ///   <para>
  ///     Subscriptions manage billing plans and add-ons for Cloudflare services.
  ///     This API provides access to account-level subscription management.
  ///   </para>
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     <b>Billing Permissions Required:</b> Requires tokens with Billing Read/Write permissions.
  ///   </para>
  ///   <para>
  ///     <b>Cost Warning:</b> Creating or updating subscriptions may incur charges.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   // List account subscriptions
  ///   var subscriptions = await cf.Subscriptions.ListAccountSubscriptionsAsync(accountId);
  ///   foreach (var sub in subscriptions)
  ///   {
  ///     Console.WriteLine($"{sub.RatePlan?.PublicName}: {sub.State}");
  ///     Console.WriteLine($"  Price: {sub.Price} {sub.Currency}/{sub.Frequency}");
  ///   }
  ///
  ///   // Create a new subscription
  ///   var newSub = await cf.Subscriptions.CreateAccountSubscriptionAsync(accountId,
  ///     new CreateAccountSubscriptionRequest(
  ///       RatePlan: new RatePlanReference("rate_plan_id"),
  ///       Frequency: SubscriptionFrequency.Monthly));
  ///   </code>
  /// </example>
  ISubscriptionsApi Subscriptions { get; }

  /// <summary>
  ///   Gets the API resource for managing Worker routes.
  ///   <para>
  ///     Worker routes map URL patterns to Worker scripts, determining which requests
  ///     are handled by which Workers. Routes are zone-scoped resources.
  ///   </para>
  /// </summary>
  /// <example>
  ///   <code>
  ///   // List all Worker routes in a zone
  ///   var routes = await cf.Workers.ListRoutesAsync(zoneId);
  ///   foreach (var route in routes)
  ///   {
  ///     Console.WriteLine($"{route.Pattern} -> {route.Script ?? "(disabled)"}");
  ///   }
  ///
  ///   // Create a new route
  ///   var newRoute = await cf.Workers.CreateRouteAsync(zoneId,
  ///     new CreateWorkerRouteRequest(
  ///       Pattern: "api.example.com/*",
  ///       Script: "api-handler"));
  ///   </code>
  /// </example>
  IWorkersApi Workers { get; }

  /// <summary>
  ///   Gets the API resource for managing Turnstile widgets.
  ///   <para>
  ///     Turnstile is Cloudflare's CAPTCHA alternative that provides bot protection
  ///     without user friction. Widgets are account-scoped resources.
  ///   </para>
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     <b>Important:</b> Widget secrets are only returned on creation and rotation.
  ///     Store them securely as they cannot be retrieved again.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   // Create a Turnstile widget
  ///   var widget = await cf.Turnstile.CreateWidgetAsync(accountId,
  ///     new CreateTurnstileWidgetRequest(
  ///       Name: "Contact Form",
  ///       Domains: new[] { "example.com" },
  ///       Mode: WidgetMode.Managed));
  ///
  ///   // IMPORTANT: Store the secret securely - it won't be available again!
  ///   Console.WriteLine($"Sitekey: {widget.Sitekey}");
  ///   Console.WriteLine($"Secret: {widget.Secret}");
  ///
  ///   // Later, rotate the secret
  ///   var result = await cf.Turnstile.RotateSecretAsync(accountId, widget.Sitekey);
  ///   Console.WriteLine($"New secret: {result.Secret}");
  ///   </code>
  /// </example>
  ITurnstileApi Turnstile { get; }

  #endregion
}
