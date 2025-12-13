namespace Cloudflare.NET.Roles;


/// <summary>
///   Constants for Cloudflare account roles.
///   <para>
///     These are the standard role names returned by the Cloudflare API.
///     Role availability varies by account type and subscription plan.
///   </para>
/// </summary>
/// <remarks>
///   <para><b>Documentation:</b> https://developers.cloudflare.com/fundamentals/manage-members/roles/</para>
///   <para>
///     Not all roles are available on all account types. Free accounts may have
///     access to a limited subset of roles. Use <see cref="IRolesApi.ListAccountRolesAsync"/>
///     to discover which roles are available for a specific account.
///   </para>
/// </remarks>
public static class RoleConstants
{
  #region Core Administrative Roles

  /// <summary>
  ///   Super Administrator with all privileges.
  ///   <para>Full administrative access to all account features and settings.</para>
  /// </summary>
  public const string SuperAdministrator = "Super Administrator - All Privileges";

  /// <summary>
  ///   Standard Administrator role.
  ///   <para>Administrative access to the entire account.</para>
  /// </summary>
  public const string Administrator = "Administrator";

  /// <summary>
  ///   Read-only administrator access.
  ///   <para>Can view all settings but cannot make changes.</para>
  /// </summary>
  public const string AdministratorReadOnly = "Administrator Read Only";

  /// <summary>
  ///   Minimal account access role.
  ///   <para>Most restricted role with minimal permissions.</para>
  /// </summary>
  public const string MinimalAccountAccess = "Minimal Account Access";

  #endregion


  #region Analytics and Monitoring Roles

  /// <summary>Analytics access role.</summary>
  public const string Analytics = "Analytics";

  /// <summary>Audit logs viewer role.</summary>
  public const string AuditLogsViewer = "Audit Logs Viewer";

  /// <summary>Log share role with full access.</summary>
  public const string LogShare = "Log Share";

  /// <summary>Log share reader role (read-only).</summary>
  public const string LogShareReader = "Log Share Reader";

  #endregion


  #region DNS and Domain Roles

  /// <summary>DNS management role.</summary>
  public const string Dns = "DNS";

  /// <summary>Zone versioning (account-wide) role.</summary>
  public const string ZoneVersioning = "Zone Versioning (Account-Wide)";

  /// <summary>Zone versioning read access (account-wide) role.</summary>
  public const string ZoneVersioningRead = "Zone Versioning Read (Account-Wide)";

  #endregion


  #region Security Roles

  /// <summary>Firewall management role.</summary>
  public const string Firewall = "Firewall";

  /// <summary>WAF (Web Application Firewall) management role.</summary>
  public const string Waf = "WAF";

  /// <summary>Bot management (account-wide) role.</summary>
  public const string BotManagement = "Bot Management (Account-wide)";

  /// <summary>Page Shield role.</summary>
  public const string PageShield = "Page Shield";

  /// <summary>Page Shield read-only role.</summary>
  public const string PageShieldRead = "Page Shield Read";

  /// <summary>Trust and Safety role.</summary>
  public const string TrustAndSafety = "Trust and Safety";

  #endregion


  #region Zero Trust and Access Roles

  /// <summary>Cloudflare Access role.</summary>
  public const string CloudflareAccess = "Cloudflare Access";

  /// <summary>Cloudflare Zero Trust full access role.</summary>
  public const string CloudflareZeroTrust = "Cloudflare Zero Trust";

  /// <summary>Cloudflare Zero Trust read-only role.</summary>
  public const string CloudflareZeroTrustReadOnly = "Cloudflare Zero Trust Read Only";

  /// <summary>Cloudflare Zero Trust reporting role.</summary>
  public const string CloudflareZeroTrustReporting = "Cloudflare Zero Trust Reporting";

  /// <summary>Cloudflare Zero Trust PII access role.</summary>
  public const string CloudflareZeroTrustPii = "Cloudflare Zero Trust PII";

  /// <summary>Cloudflare Zero Trust DNS locations write role.</summary>
  public const string CloudflareZeroTrustDnsLocationsWrite = "Cloudflare Zero Trust DNS Locations Write";

  /// <summary>Cloudflare Gateway role.</summary>
  public const string CloudflareGateway = "Cloudflare Gateway";

  /// <summary>Cloudflare DEX (Digital Experience) role.</summary>
  public const string CloudflareDex = "Cloudflare DEX";

  /// <summary>Cloudflare CASB role.</summary>
  public const string CloudflareCasb = "Cloudflare CASB";

  /// <summary>Cloudflare CASB read-only role.</summary>
  public const string CloudflareCasbRead = "Cloudflare CASB Read";

  #endregion


  #region API and Developer Roles

  /// <summary>API Gateway role.</summary>
  public const string ApiGateway = "API Gateway";

  /// <summary>API Gateway read-only role.</summary>
  public const string ApiGatewayRead = "API Gateway Read";

  /// <summary>Workers Platform admin role.</summary>
  public const string WorkersPlatformAdmin = "Workers Platform Admin";

  /// <summary>Workers Platform read-only role.</summary>
  public const string WorkersPlatformReadOnly = "Workers Platform (Read-only)";

  /// <summary>Hyperdrive admin role.</summary>
  public const string HyperdriveAdmin = "Hyperdrive Admin";

  /// <summary>Hyperdrive read-only role.</summary>
  public const string HyperdriveRead = "Hyperdrive Read";

  /// <summary>Vectorize admin role.</summary>
  public const string VectorizeAdmin = "Vectorize Admin";

  /// <summary>Vectorize read-only role.</summary>
  public const string VectorizeReadOnly = "Vectorize Read only";

  #endregion


  #region Storage Roles (R2, Stream, Images)

  /// <summary>Cloudflare R2 admin role.</summary>
  public const string CloudflareR2Admin = "Cloudflare R2 Admin";

  /// <summary>Cloudflare R2 read-only role.</summary>
  public const string CloudflareR2Read = "Cloudflare R2 Read";

  /// <summary>Cloudflare Stream role.</summary>
  public const string CloudflareStream = "Cloudflare Stream";

  /// <summary>Cloudflare Images role.</summary>
  public const string CloudflareImages = "Cloudflare Images";

  #endregion


  #region Performance and Caching Roles

  /// <summary>Cache purge role.</summary>
  public const string CachePurge = "Cache Purge";

  /// <summary>Load balancer management role.</summary>
  public const string LoadBalancer = "Load Balancer";

  /// <summary>SSL/TLS, caching, performance, page rules, and customization role.</summary>
  public const string SslTlsCachingPerformance = "SSL/TLS Caching Performance Page Rules and Customization";

  /// <summary>Waiting room admin role.</summary>
  public const string WaitingRoomAdmin = "Waiting Room Admin";

  /// <summary>Waiting room read-only role.</summary>
  public const string WaitingRoomRead = "Waiting Room Read";

  #endregion


  #region Email Security Roles

  /// <summary>Email configuration admin role.</summary>
  public const string EmailConfigurationAdmin = "Email Configuration Admin";

  /// <summary>Email integration admin role.</summary>
  public const string EmailIntegrationAdmin = "Email Integration Admin";

  /// <summary>Email security analyst role.</summary>
  public const string EmailSecurityAnalyst = "Email security Analyst";

  /// <summary>Email security read-only role.</summary>
  public const string EmailSecurityReadOnly = "Email security Read Only";

  /// <summary>Email security reporting role.</summary>
  public const string EmailSecurityReporting = "Email security Reporting";

  /// <summary>Email security policy admin role.</summary>
  public const string EmailSecurityPolicyAdmin = "Email security Policy Admin";

  #endregion


  #region Network and Magic Roles

  /// <summary>Network services write (Magic) role.</summary>
  public const string NetworkServicesWrite = "Network Services Write (Magic)";

  /// <summary>Network services read (Magic) role.</summary>
  public const string NetworkServicesRead = "Network Services Read (Magic)";

  /// <summary>Magic Network Monitoring role.</summary>
  public const string MagicNetworkMonitoring = "Magic Network Monitoring";

  /// <summary>Magic Network Monitoring admin role.</summary>
  public const string MagicNetworkMonitoringAdmin = "Magic Network Monitoring Admin";

  /// <summary>Magic Network Monitoring read-only role.</summary>
  public const string MagicNetworkMonitoringReadOnly = "Magic Network Monitoring Read-Only";

  #endregion


  #region Security Center Roles

  /// <summary>Security Center brand protection role.</summary>
  public const string SecurityCenterBrandProtection = "Security Center Brand Protection";

  /// <summary>Security Center Cloudforce One admin role.</summary>
  public const string SecurityCenterCloudforceOneAdmin = "Security Center Cloudforce One Admin";

  /// <summary>Security Center Cloudforce One read role.</summary>
  public const string SecurityCenterCloudforceOneRead = "Security Center Cloudforce One Read";

  #endregion


  #region Secrets and Connectivity Roles

  /// <summary>Secrets Store admin role.</summary>
  public const string SecretsStoreAdmin = "Secrets Store Admin";

  /// <summary>Secrets Store deployer role.</summary>
  public const string SecretsStoreDeployer = "Secrets Store Deployer";

  /// <summary>Secrets Store reporter role.</summary>
  public const string SecretsStoreReporter = "Secrets Store Reporter";

  /// <summary>Connectivity Directory read role.</summary>
  public const string ConnectivityDirectoryRead = "Connectivity Directory Read";

  /// <summary>Connectivity Directory bind role.</summary>
  public const string ConnectivityDirectoryBind = "Connectivity Directory Bind";

  /// <summary>Connectivity Directory admin role.</summary>
  public const string ConnectivityDirectoryAdmin = "Connectivity Directory Admin";

  #endregion


  #region Other Roles

  /// <summary>Billing management role.</summary>
  public const string Billing = "Billing";

  /// <summary>Turnstile management role.</summary>
  public const string Turnstile = "Turnstile";

  /// <summary>Turnstile read-only role.</summary>
  public const string TurnstileRead = "Turnstile Read";

  /// <summary>Zaraz admin role.</summary>
  public const string ZarazAdmin = "Zaraz Admin";

  /// <summary>Zaraz edit role.</summary>
  public const string ZarazEdit = "Zaraz Edit";

  /// <summary>Zaraz read-only role.</summary>
  public const string ZarazRead = "Zaraz Read";

  #endregion
}
