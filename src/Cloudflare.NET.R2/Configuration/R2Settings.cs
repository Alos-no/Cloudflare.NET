namespace Cloudflare.NET.R2.Configuration;

/// <summary>
///   Represents R2-specific settings for the S3-compatible API, typically loaded from
///   configuration.
/// </summary>
public class R2Settings
{
  #region Properties & Fields - Public

  /// <summary>
  ///   The R2 S3 API endpoint URL. It must contain a format placeholder '{0}' for the
  ///   Account ID.
  /// </summary>
  public string EndpointUrl { get; set; } = string.Empty;

  /// <summary>The R2 region (typically "auto").</summary>
  public string Region { get; set; } = "auto";

  /// <summary>The R2 Access Key ID (from user secrets or environment variables).</summary>
  public string AccessKeyId { get; set; } = string.Empty;

  /// <summary>The R2 Secret Access Key (from user secrets or environment variables).</summary>
  public string SecretAccessKey { get; set; } = string.Empty;

  #endregion
}
