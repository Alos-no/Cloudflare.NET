namespace Cloudflare.NET.Zones.CustomHostnames;

/// <summary>Provides centralized, strongly-typed constants for Cloudflare for SaaS (Custom Hostnames) features.</summary>
public static class CustomHostnameConstants
{
  /// <summary>Constants for Server Name Indication (SNI) configuration.</summary>
  public static class Sni
  {
    #region Constants & Statics

    /// <summary>
    ///   Special value that causes the Host header from the incoming request to be used as the SNI value when
    ///   Cloudflare connects to the origin server.
    /// </summary>
    /// <remarks>
    ///   Use this value for <c>custom_origin_sni</c> when you want the SNI to dynamically match the Host header of each
    ///   request, rather than a fixed hostname.
    /// </remarks>
    public const string UseRequestHostHeader = ":request_host_header:";

    #endregion
  }

  /// <summary>Constants for recommended TLS cipher suites.</summary>
  public static class Ciphers
  {
    #region Constants & Statics

    /// <summary>ECDHE-ECDSA with AES-128-GCM and SHA-256.</summary>
    public const string EcdheEcdsaAes128GcmSha256 = "ECDHE-ECDSA-AES128-GCM-SHA256";

    /// <summary>ECDHE-RSA with AES-128-GCM and SHA-256.</summary>
    public const string EcdheRsaAes128GcmSha256 = "ECDHE-RSA-AES128-GCM-SHA256";

    /// <summary>ECDHE-ECDSA with AES-256-GCM and SHA-384.</summary>
    public const string EcdheEcdsaAes256GcmSha384 = "ECDHE-ECDSA-AES256-GCM-SHA384";

    /// <summary>ECDHE-RSA with AES-256-GCM and SHA-384.</summary>
    public const string EcdheRsaAes256GcmSha384 = "ECDHE-RSA-AES256-GCM-SHA384";

    /// <summary>ECDHE-ECDSA with ChaCha20-Poly1305 and SHA-256.</summary>
    public const string EcdheEcdsaChacha20Poly1305 = "ECDHE-ECDSA-CHACHA20-POLY1305";

    /// <summary>ECDHE-RSA with ChaCha20-Poly1305 and SHA-256.</summary>
    public const string EcdheRsaChacha20Poly1305 = "ECDHE-RSA-CHACHA20-POLY1305";

    #endregion
  }
}
