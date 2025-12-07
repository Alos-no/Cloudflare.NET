namespace Cloudflare.NET.R2.Configuration;

/// <summary>Represents R2-specific settings for the S3-compatible API, typically loaded from configuration.</summary>
public class R2Settings
{
  #region Properties & Fields - Public

  /// <summary>
  ///   The R2 S3 API endpoint URL. It must contain a format placeholder '{0}' for the Account ID. Defaults to the
  ///   standard Cloudflare R2 endpoint: https://{account_id}.r2.cloudflarestorage.com
  /// </summary>
  public string EndpointUrl { get; set; } = DefaultEndpointUrl;

  /// <summary>The R2 region. Defaults to "auto", which is the standard region for Cloudflare R2.</summary>
  public string Region { get; set; } = DefaultRegion;

  /// <summary>The R2 Access Key ID (from user secrets or environment variables). Required.</summary>
  public string AccessKeyId { get; set; } = string.Empty;

  /// <summary>The R2 Secret Access Key (from user secrets or environment variables). Required.</summary>
  public string SecretAccessKey { get; set; } = string.Empty;

  #endregion

  #region Constants

  /// <summary>
  ///   The default R2 S3 API endpoint URL template. Contains a format placeholder '{0}' for the Account ID. This is
  ///   the standard Cloudflare R2 endpoint: https://{account_id}.r2.cloudflarestorage.com
  /// </summary>
  public const string DefaultEndpointUrl = "https://{0}.r2.cloudflarestorage.com";

  /// <summary>The default R2 region. Cloudflare R2 uses "auto" as the region for all operations.</summary>
  public const string DefaultRegion = "auto";

  #endregion
}
