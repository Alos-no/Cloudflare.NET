namespace Cloudflare.NET.Dns.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>
///   Represents a DNS record type supported by Cloudflare.
///   <para>
///     DNS record types define the type of data stored in a DNS record, such as IP addresses (A/AAAA),
///     mail servers (MX), or canonical names (CNAME).
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing custom values
///     for new record types that may be added to the Cloudflare API in the future.
///   </para>
///   <para>
///     Use the static properties for known record types (e.g., <see cref="A" />, <see cref="CNAME" />)
///     or create custom values using the constructor or implicit string conversion.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Using a known record type with IntelliSense
///   var type = DnsRecordType.CNAME;
///
///   // Creating a DNS record request
///   var request = new CreateDnsRecordRequest(DnsRecordType.A, "example.com", "192.0.2.1", 3600, true);
///
///   // Using implicit conversion from string
///   DnsRecordType customType = "NEW_RECORD_TYPE";
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/dns/manage-dns-records/reference/dns-record-types/" />
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<DnsRecordType>))]
public readonly struct DnsRecordType : IExtensibleEnum<DnsRecordType>, IEquatable<DnsRecordType>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this record type.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this record type.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values - Common Record Types

  /// <summary>A record - maps a hostname to an IPv4 address.</summary>
  /// <remarks>The most common DNS record type for pointing domains to servers.</remarks>
  public static DnsRecordType A { get; } = new("A");

  /// <summary>AAAA record - maps a hostname to an IPv6 address.</summary>
  /// <remarks>The IPv6 equivalent of an A record.</remarks>
  public static DnsRecordType AAAA { get; } = new("AAAA");

  /// <summary>CNAME record - creates an alias from one hostname to another.</summary>
  /// <remarks>Useful for pointing subdomains to other domains or services.</remarks>
  public static DnsRecordType CNAME { get; } = new("CNAME");

  /// <summary>MX record - specifies mail servers for the domain.</summary>
  /// <remarks>Used to route email to the correct mail servers.</remarks>
  public static DnsRecordType MX { get; } = new("MX");

  /// <summary>TXT record - holds arbitrary text data.</summary>
  /// <remarks>Commonly used for domain verification, SPF, DKIM, and DMARC records.</remarks>
  public static DnsRecordType TXT { get; } = new("TXT");

  /// <summary>NS record - specifies authoritative nameservers for the domain.</summary>
  /// <remarks>Delegates DNS resolution to specific nameservers.</remarks>
  public static DnsRecordType NS { get; } = new("NS");

  /// <summary>SOA record - contains administrative information about the zone.</summary>
  /// <remarks>Every DNS zone must have exactly one SOA record.</remarks>
  public static DnsRecordType SOA { get; } = new("SOA");

  /// <summary>PTR record - maps an IP address to a hostname (reverse DNS).</summary>
  /// <remarks>Used for reverse DNS lookups.</remarks>
  public static DnsRecordType PTR { get; } = new("PTR");

  #endregion


  #region Known Values - Service Records

  /// <summary>SRV record - specifies a host and port for specific services.</summary>
  /// <remarks>Used for services like VoIP, instant messaging, and other protocols.</remarks>
  public static DnsRecordType SRV { get; } = new("SRV");

  /// <summary>HTTPS record - provides connection information for HTTPS services.</summary>
  /// <remarks>Enables clients to learn about HTTPS alternative services.</remarks>
  public static DnsRecordType HTTPS { get; } = new("HTTPS");

  /// <summary>SVCB record - general service binding record.</summary>
  /// <remarks>The generic version of HTTPS records for other protocols.</remarks>
  public static DnsRecordType SVCB { get; } = new("SVCB");

  /// <summary>URI record - publishes mappings from hostnames to URIs.</summary>
  public static DnsRecordType URI { get; } = new("URI");

  /// <summary>NAPTR record - naming authority pointer for URI resolution.</summary>
  /// <remarks>Used in ENUM and SIP applications.</remarks>
  public static DnsRecordType NAPTR { get; } = new("NAPTR");

  #endregion


  #region Known Values - Security Records

  /// <summary>CAA record - specifies which Certificate Authorities can issue certificates.</summary>
  /// <remarks>Helps prevent unauthorized certificate issuance.</remarks>
  public static DnsRecordType CAA { get; } = new("CAA");

  /// <summary>DS record - Delegation Signer record for DNSSEC.</summary>
  /// <remarks>Used to establish chains of trust in DNSSEC.</remarks>
  public static DnsRecordType DS { get; } = new("DS");

  /// <summary>DNSKEY record - contains the public key for DNSSEC.</summary>
  /// <remarks>Used by resolvers to verify DNSSEC signatures.</remarks>
  public static DnsRecordType DNSKEY { get; } = new("DNSKEY");

  /// <summary>TLSA record - DANE certificate association.</summary>
  /// <remarks>Associates TLS server certificates with domain names.</remarks>
  public static DnsRecordType TLSA { get; } = new("TLSA");

  /// <summary>SSHFP record - SSH public key fingerprint.</summary>
  /// <remarks>Allows SSH clients to verify server host keys via DNS.</remarks>
  public static DnsRecordType SSHFP { get; } = new("SSHFP");

  /// <summary>CERT record - stores certificates in DNS.</summary>
  /// <remarks>Can store various types of certificates including X.509 and PGP.</remarks>
  public static DnsRecordType CERT { get; } = new("CERT");

  /// <summary>SMIMEA record - S/MIME certificate association.</summary>
  /// <remarks>Associates S/MIME certificates with email addresses.</remarks>
  public static DnsRecordType SMIMEA { get; } = new("SMIMEA");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="DnsRecordType" /> with the specified value.</summary>
  /// <param name="value">The string value representing the record type.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public DnsRecordType(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static DnsRecordType Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to a <see cref="DnsRecordType" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator DnsRecordType(string value) => new(value);

  /// <summary>Implicitly converts a <see cref="DnsRecordType" /> to its string value.</summary>
  /// <param name="recordType">The record type to convert.</param>
  public static implicit operator string(DnsRecordType recordType) => recordType.Value;

  /// <summary>Determines whether two <see cref="DnsRecordType" /> values are equal.</summary>
  public static bool operator ==(DnsRecordType left, DnsRecordType right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="DnsRecordType" /> values are not equal.</summary>
  public static bool operator !=(DnsRecordType left, DnsRecordType right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(DnsRecordType other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is DnsRecordType other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}
