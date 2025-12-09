namespace Cloudflare.NET.Zones.CustomHostnames.Models;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>Defines the status of a custom hostname in its lifecycle.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum CustomHostnameStatus
{
  /// <summary>The hostname is pending initial validation.</summary>
  [EnumMember(Value = "pending")] Pending,

  /// <summary>The hostname is active and serving traffic.</summary>
  [EnumMember(Value = "active")] Active,

  /// <summary>The hostname is active and changes are being redeployed.</summary>
  [EnumMember(Value = "active_redeploying")]
  ActiveRedeploying,

  /// <summary>The hostname has been moved away from this zone.</summary>
  [EnumMember(Value = "moved")] Moved,

  /// <summary>The hostname is pending deletion.</summary>
  [EnumMember(Value = "pending_deletion")]
  PendingDeletion,

  /// <summary>The hostname has been deleted.</summary>
  [EnumMember(Value = "deleted")] Deleted,

  /// <summary>The hostname is blocked due to abuse or policy violation.</summary>
  [EnumMember(Value = "blocked")] Blocked,

  /// <summary>The hostname is pending and blocked.</summary>
  [EnumMember(Value = "pending_blocked")]
  PendingBlocked,

  /// <summary>The hostname is pending migration.</summary>
  [EnumMember(Value = "pending_migration")]
  PendingMigration,

  /// <summary>The hostname is pending provisioning.</summary>
  [EnumMember(Value = "pending_provisioned")]
  PendingProvisioned,

  /// <summary>The hostname has been provisioned.</summary>
  [EnumMember(Value = "provisioned")] Provisioned,

  /// <summary>Test status: pending validation.</summary>
  [EnumMember(Value = "test_pending")] TestPending,

  /// <summary>Test status: active.</summary>
  [EnumMember(Value = "test_active")] TestActive,

  /// <summary>Test status: active for apex domain.</summary>
  [EnumMember(Value = "test_active_apex")]
  TestActiveApex,

  /// <summary>Test status: blocked.</summary>
  [EnumMember(Value = "test_blocked")] TestBlocked,

  /// <summary>Test status: validation failed.</summary>
  [EnumMember(Value = "test_failed")] TestFailed
}

/// <summary>Defines the status of an SSL certificate for a custom hostname.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum SslStatus
{
  // ─────────────────────────────────────────────────────────────────────────────
  // Initialization States
  // ─────────────────────────────────────────────────────────────────────────────

  /// <summary>The certificate is being initialized.</summary>
  [EnumMember(Value = "initializing")] Initializing,

  /// <summary>The certificate initialization timed out.</summary>
  [EnumMember(Value = "initializing_timed_out")]
  InitializingTimedOut,

  // ─────────────────────────────────────────────────────────────────────────────
  // Validation States
  // ─────────────────────────────────────────────────────────────────────────────

  /// <summary>The certificate is pending domain control validation.</summary>
  [EnumMember(Value = "pending_validation")]
  PendingValidation,

  /// <summary>The certificate domain control validation timed out.</summary>
  [EnumMember(Value = "validation_timed_out")]
  ValidationTimedOut,

  // ─────────────────────────────────────────────────────────────────────────────
  // Issuance States
  // ─────────────────────────────────────────────────────────────────────────────

  /// <summary>The certificate is pending issuance from the CA.</summary>
  [EnumMember(Value = "pending_issuance")]
  PendingIssuance,

  /// <summary>The certificate issuance timed out.</summary>
  [EnumMember(Value = "issuance_timed_out")]
  IssuanceTimedOut,

  // ─────────────────────────────────────────────────────────────────────────────
  // Deployment States
  // ─────────────────────────────────────────────────────────────────────────────

  /// <summary>The certificate is pending deployment to the edge.</summary>
  [EnumMember(Value = "pending_deployment")]
  PendingDeployment,

  /// <summary>The certificate deployment timed out.</summary>
  [EnumMember(Value = "deployment_timed_out")]
  DeploymentTimedOut,

  /// <summary>The certificate is being deployed to staging environment.</summary>
  [EnumMember(Value = "staging_deployment")]
  StagingDeployment,

  /// <summary>The certificate is active in staging environment.</summary>
  [EnumMember(Value = "staging_active")] StagingActive,

  /// <summary>The certificate deployment is being held.</summary>
  [EnumMember(Value = "holding_deployment")]
  HoldingDeployment,

  // ─────────────────────────────────────────────────────────────────────────────
  // Active States
  // ─────────────────────────────────────────────────────────────────────────────

  /// <summary>The certificate is active and serving traffic.</summary>
  [EnumMember(Value = "active")] Active,

  /// <summary>A backup certificate has been issued.</summary>
  [EnumMember(Value = "backup_issued")] BackupIssued,

  // ─────────────────────────────────────────────────────────────────────────────
  // Expiration States
  // ─────────────────────────────────────────────────────────────────────────────

  /// <summary>The certificate is pending expiration and renewal.</summary>
  [EnumMember(Value = "pending_expiration")]
  PendingExpiration,

  /// <summary>The certificate has expired.</summary>
  [EnumMember(Value = "expired")] Expired,

  // ─────────────────────────────────────────────────────────────────────────────
  // Deactivation and Deletion States
  // ─────────────────────────────────────────────────────────────────────────────

  /// <summary>The certificate is being deactivated.</summary>
  [EnumMember(Value = "deactivating")] Deactivating,

  /// <summary>The certificate is inactive.</summary>
  [EnumMember(Value = "inactive")] Inactive,

  /// <summary>The certificate is pending deletion along with its custom hostname.</summary>
  [EnumMember(Value = "pending_deletion")]
  PendingDeletion,

  /// <summary>The certificate deletion timed out.</summary>
  [EnumMember(Value = "deletion_timed_out")]
  DeletionTimedOut,

  /// <summary>The certificate is pending cleanup after deletion.</summary>
  [EnumMember(Value = "pending_cleanup")]
  PendingCleanup,

  /// <summary>The certificate has been deleted.</summary>
  [EnumMember(Value = "deleted")] Deleted
}

/// <summary>Defines the Domain Control Validation (DCV) method used to verify domain ownership.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum DcvMethod
{
  /// <summary>HTTP validation: serve a token at a specific URL.</summary>
  [EnumMember(Value = "http")] Http,

  /// <summary>TXT validation: add a TXT record to DNS.</summary>
  [EnumMember(Value = "txt")] Txt,

  /// <summary>Email validation: approve via email to domain contacts.</summary>
  [EnumMember(Value = "email")] Email
}

/// <summary>Defines the type of SSL certificate.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum CertificateType
{
  /// <summary>Domain Validation certificate (standard for custom hostnames).</summary>
  [EnumMember(Value = "dv")] Dv
}

/// <summary>Defines how the certificate chain is bundled.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum BundleMethod
{
  /// <summary>Ensures highest compatibility, even with outdated trust stores.</summary>
  [EnumMember(Value = "ubiquitous")] Ubiquitous,

  /// <summary>Uses the shortest chain with newest intermediates.</summary>
  [EnumMember(Value = "optimal")] Optimal,

  /// <summary>Verifies the chain but does not modify it.</summary>
  [EnumMember(Value = "force")] Force
}

/// <summary>Defines the Certificate Authority that issues SSL certificates.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum CertificateAuthority
{
  /// <summary>DigiCert Certificate Authority.</summary>
  [EnumMember(Value = "digicert")] Digicert,

  /// <summary>Google Trust Services Certificate Authority.</summary>
  [EnumMember(Value = "google")] Google,

  /// <summary>Let's Encrypt Certificate Authority.</summary>
  [EnumMember(Value = "lets_encrypt")] LetsEncrypt,

  /// <summary>SSL.com Certificate Authority.</summary>
  [EnumMember(Value = "ssl_com")] SslCom
}

/// <summary>Defines the minimum TLS version for SSL settings.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum MinTlsVersion
{
  /// <summary>TLS 1.0 (not recommended for security reasons).</summary>
  [EnumMember(Value = "1.0")] Tls10,

  /// <summary>TLS 1.1 (not recommended for security reasons).</summary>
  [EnumMember(Value = "1.1")] Tls11,

  /// <summary>TLS 1.2 (recommended minimum).</summary>
  [EnumMember(Value = "1.2")] Tls12,

  /// <summary>TLS 1.3 (most secure).</summary>
  [EnumMember(Value = "1.3")] Tls13
}

/// <summary>Defines the on/off toggle for SSL feature settings.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum SslToggle
{
  /// <summary>The feature is enabled.</summary>
  [EnumMember(Value = "on")] On,

  /// <summary>The feature is disabled.</summary>
  [EnumMember(Value = "off")] Off
}
