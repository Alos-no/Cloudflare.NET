namespace Cloudflare.NET.Accounts.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>
///   Represents a jurisdictional restriction for R2 bucket data storage.
///   <para>
///     Jurisdictions provide a guarantee that objects stored in the bucket will remain within a specific
///     geographic or regulatory boundary. Unlike location hints, jurisdictions are enforced constraints.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing custom values
///     for new jurisdictions that may be added to the Cloudflare API in the future.
///   </para>
///   <para><strong>Important: When to Pass Jurisdiction to API Methods</strong></para>
///   <para>
///     If you create a bucket with a jurisdiction (e.g., <see cref="EuropeanUnion" /> or <see cref="FedRamp" />),
///     you <strong>must pass the same jurisdiction value to all subsequent API operations</strong> on that bucket.
///     Without it, the API returns error 10006 "The specified bucket does not exist" even though the bucket exists.
///   </para>
///   <para>
///     The jurisdiction is required for:
///     <c>GetAsync</c>, <c>DeleteAsync</c>, all CORS operations, all lifecycle operations, all custom domain
///     operations, all managed domain operations, all lock operations, and all Sippy operations.
///   </para>
///   <para>
///     The jurisdiction is <em>not</em> required for: <c>ListAsync</c> / <c>ListAllAsync</c> (returns all buckets
///     with their jurisdiction in the response) and <c>CreateTempCredentialsAsync</c> (bucket is in the request body).
///   </para>
///   <para>
///     <strong>Best Practice:</strong> Store the jurisdiction value when creating a bucket and reuse it for all
///     subsequent operations on that bucket.
///   </para>
///   <para>
///     When accessing jurisdictional buckets via the S3 API, you must specify the jurisdiction in the endpoint:
///     <c>https://{account_id}.{jurisdiction}.r2.cloudflarestorage.com</c>
///   </para>
///   <para>
///     The <see cref="FedRamp" /> jurisdiction requires Enterprise customer status. Contact your Cloudflare
///     account team for access.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Create an EU jurisdiction bucket
///   var bucket = await api.Buckets.CreateAsync("my-bucket", jurisdiction: R2Jurisdiction.EuropeanUnion);
///
///   // IMPORTANT: Pass the same jurisdiction for all subsequent operations
///   var details = await api.Buckets.GetAsync("my-bucket", R2Jurisdiction.EuropeanUnion);
///   await api.Buckets.SetCorsAsync("my-bucket", corsPolicy, R2Jurisdiction.EuropeanUnion);
///   await api.Buckets.DeleteAsync("my-bucket", R2Jurisdiction.EuropeanUnion);
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/r2/reference/data-location/" />
/// <seealso href="https://developers.cloudflare.com/data-localization/how-to/r2/" />
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<R2Jurisdiction>))]
public readonly struct R2Jurisdiction : IExtensibleEnum<R2Jurisdiction>, IEquatable<R2Jurisdiction>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this jurisdiction.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this jurisdiction.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values

  /// <summary>No jurisdictional restriction.</summary>
  /// <remarks>
  ///   Data may be stored in any Cloudflare data center globally.
  ///   This is the default when no jurisdiction is specified.
  /// </remarks>
  public static R2Jurisdiction Default { get; } = new("default");

  /// <summary>European Union jurisdiction.</summary>
  /// <remarks>
  ///   <para>
  ///     Objects are guaranteed to be stored within the European Union, helping meet GDPR
  ///     and other EU data residency requirements.
  ///   </para>
  ///   <para>
  ///     When using the S3 API, use the endpoint: <c>https://{account_id}.eu.r2.cloudflarestorage.com</c>
  ///   </para>
  /// </remarks>
  public static R2Jurisdiction EuropeanUnion { get; } = new("eu");

  /// <summary>FedRAMP jurisdiction.</summary>
  /// <remarks>
  ///   <para>
  ///     Objects are stored in compliance with FedRAMP (Federal Risk and Authorization Management Program)
  ///     requirements for U.S. federal government data.
  ///   </para>
  ///   <para>
  ///     This jurisdiction requires Enterprise customer status. Contact your Cloudflare account team
  ///     or Cloudflare Support for access.
  ///   </para>
  ///   <para>
  ///     When using the S3 API, use the endpoint: <c>https://{account_id}.fedramp.r2.cloudflarestorage.com</c>
  ///   </para>
  /// </remarks>
  public static R2Jurisdiction FedRamp { get; } = new("fedramp");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="R2Jurisdiction" /> with the specified value.</summary>
  /// <param name="value">The string value representing the jurisdiction.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public R2Jurisdiction(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static R2Jurisdiction Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to an <see cref="R2Jurisdiction" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator R2Jurisdiction(string value) => new(value);

  /// <summary>Implicitly converts an <see cref="R2Jurisdiction" /> to its string value.</summary>
  /// <param name="jurisdiction">The jurisdiction to convert.</param>
  public static implicit operator string(R2Jurisdiction jurisdiction) => jurisdiction.Value;

  /// <summary>Determines whether two <see cref="R2Jurisdiction" /> values are equal.</summary>
  public static bool operator ==(R2Jurisdiction left, R2Jurisdiction right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="R2Jurisdiction" /> values are not equal.</summary>
  public static bool operator !=(R2Jurisdiction left, R2Jurisdiction right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(R2Jurisdiction other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is R2Jurisdiction other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion


  #region S3 Endpoint Helpers

  /// <summary>
  ///   Gets the S3-compatible endpoint URL for this jurisdiction.
  /// </summary>
  /// <param name="accountId">The Cloudflare account ID.</param>
  /// <returns>The full S3 endpoint URL for this jurisdiction.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var endpoint = R2Jurisdiction.EuropeanUnion.GetS3EndpointUrl("abc123");
  ///   // Returns: "https://abc123.eu.r2.cloudflarestorage.com"
  ///
  ///   var defaultEndpoint = R2Jurisdiction.Default.GetS3EndpointUrl("abc123");
  ///   // Returns: "https://abc123.r2.cloudflarestorage.com"
  ///   </code>
  /// </example>
  public string GetS3EndpointUrl(string accountId)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

    var subdomain = GetS3Subdomain();

    return subdomain is null
      ? $"https://{accountId}.r2.cloudflarestorage.com"
      : $"https://{accountId}.{subdomain}.r2.cloudflarestorage.com";
  }


  /// <summary>
  ///   Gets the S3 endpoint subdomain for this jurisdiction, or <c>null</c> for the default jurisdiction.
  /// </summary>
  /// <returns>
  ///   The subdomain string (e.g., <c>"eu"</c>, <c>"fedramp"</c>) for jurisdictional endpoints,
  ///   or <c>null</c> for the default (global) jurisdiction.
  /// </returns>
  /// <example>
  ///   <code>
  ///   R2Jurisdiction.EuropeanUnion.GetS3Subdomain(); // Returns: "eu"
  ///   R2Jurisdiction.FedRamp.GetS3Subdomain();       // Returns: "fedramp"
  ///   R2Jurisdiction.Default.GetS3Subdomain();       // Returns: null
  ///   </code>
  /// </example>
  public string? GetS3Subdomain()
  {
    // Default jurisdiction has no subdomain.
    if (string.IsNullOrEmpty(Value) ||
        string.Equals(Value, "default", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    return Value.ToLowerInvariant();
  }

  #endregion
}
