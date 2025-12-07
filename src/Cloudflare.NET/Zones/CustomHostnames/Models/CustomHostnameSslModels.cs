namespace Cloudflare.NET.Zones.CustomHostnames.Models;

using System.Text.Json.Serialization;

/// <summary>Represents the SSL/TLS settings for a custom hostname.</summary>
/// <param name="Http2">Whether HTTP/2 is enabled.</param>
/// <param name="MinTlsVersion">The minimum TLS version allowed.</param>
/// <param name="Tls13">Whether TLS 1.3 is enabled.</param>
/// <param name="Ciphers">The list of allowed cipher suites.</param>
/// <param name="EarlyHints">Whether Early Hints is enabled.</param>
public record SslSettings(
  [property: JsonPropertyName("http2")]
  SslToggle? Http2 = null,
  [property: JsonPropertyName("min_tls_version")]
  MinTlsVersion? MinTlsVersion = null,
  [property: JsonPropertyName("tls_1_3")]
  SslToggle? Tls13 = null,
  [property: JsonPropertyName("ciphers")]
  IReadOnlyList<string>? Ciphers = null,
  [property: JsonPropertyName("early_hints")]
  SslToggle? EarlyHints = null
);

/// <summary>Represents the SSL configuration for creating or updating a custom hostname.</summary>
/// <param name="Method">The domain control validation method (http, txt, or email).</param>
/// <param name="Type">The type of certificate (dv for Domain Validation).</param>
/// <param name="Settings">Optional TLS settings.</param>
/// <param name="BundleMethod">How the certificate chain should be bundled.</param>
/// <param name="Wildcard">Whether the certificate should cover wildcard subdomains.</param>
/// <param name="CertificateAuthority">The preferred Certificate Authority.</param>
public record SslConfiguration(
  [property: JsonPropertyName("method")]
  DcvMethod Method,
  [property: JsonPropertyName("type")]
  CertificateType Type,
  [property: JsonPropertyName("settings")]
  SslSettings? Settings = null,
  [property: JsonPropertyName("bundle_method")]
  BundleMethod? BundleMethod = null,
  [property: JsonPropertyName("wildcard")]
  bool? Wildcard = null,
  [property: JsonPropertyName("certificate_authority")]
  CertificateAuthority? CertificateAuthority = null
);

/// <summary>Represents a Domain Control Validation record returned by the API.</summary>
/// <remarks>
///   The fields populated depend on the DCV method used:
///   <list type="bullet">
///     <item>
///       <description>TXT validation: <see cref="TxtName" /> and <see cref="TxtValue" /></description>
///     </item>
///     <item>
///       <description>HTTP validation: <see cref="HttpUrl" /> and <see cref="HttpBody" /></description>
///     </item>
///     <item>
///       <description>CNAME validation: <see cref="Cname" /> and <see cref="CnameTarget" /></description>
///     </item>
///   </list>
/// </remarks>
/// <param name="TxtName">The name of the TXT record for TXT validation.</param>
/// <param name="TxtValue">The value of the TXT record for TXT validation.</param>
/// <param name="HttpUrl">The URL to serve the token for HTTP validation.</param>
/// <param name="HttpBody">The token content for HTTP validation.</param>
/// <param name="Cname">The CNAME record name for CNAME validation.</param>
/// <param name="CnameTarget">The CNAME record target for CNAME validation.</param>
public record ValidationRecord(
  [property: JsonPropertyName("txt_name")]
  string? TxtName = null,
  [property: JsonPropertyName("txt_value")]
  string? TxtValue = null,
  [property: JsonPropertyName("http_url")]
  string? HttpUrl = null,
  [property: JsonPropertyName("http_body")]
  string? HttpBody = null,
  [property: JsonPropertyName("cname")]
  string? Cname = null,
  [property: JsonPropertyName("cname_target")]
  string? CnameTarget = null
);

/// <summary>Represents a validation error that occurred during SSL certificate issuance.</summary>
/// <param name="Message">The error message describing the validation failure.</param>
public record ValidationError(
  [property: JsonPropertyName("message")]
  string? Message
);

/// <summary>Represents the full SSL status and configuration returned by the API.</summary>
/// <param name="Id">The unique identifier of the SSL certificate.</param>
/// <param name="Status">The current status of the SSL certificate.</param>
/// <param name="Method">The domain control validation method used.</param>
/// <param name="Type">The type of certificate.</param>
/// <param name="Settings">The TLS settings configured.</param>
/// <param name="BundleMethod">How the certificate chain is bundled.</param>
/// <param name="Wildcard">Whether the certificate covers wildcard subdomains.</param>
/// <param name="CertificateAuthority">The Certificate Authority that issued the certificate.</param>
/// <param name="ValidationRecords">The DCV records for domain verification.</param>
/// <param name="ValidationErrors">Any errors encountered during validation.</param>
public record SslResponse(
  [property: JsonPropertyName("id")]
  string? Id,
  [property: JsonPropertyName("status")]
  SslStatus Status,
  [property: JsonPropertyName("method")]
  DcvMethod Method,
  [property: JsonPropertyName("type")]
  CertificateType Type,
  [property: JsonPropertyName("settings")]
  SslSettings? Settings = null,
  [property: JsonPropertyName("bundle_method")]
  BundleMethod? BundleMethod = null,
  [property: JsonPropertyName("wildcard")]
  bool? Wildcard = null,
  [property: JsonPropertyName("certificate_authority")]
  CertificateAuthority? CertificateAuthority = null,
  [property: JsonPropertyName("validation_records")]
  IReadOnlyList<ValidationRecord>? ValidationRecords = null,
  [property: JsonPropertyName("validation_errors")]
  IReadOnlyList<ValidationError>? ValidationErrors = null
);
