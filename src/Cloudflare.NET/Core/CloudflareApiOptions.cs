namespace Cloudflare.NET.Core;

/// <summary>Represents the configuration options required for the Cloudflare API client.</summary>
public class CloudflareApiOptions
{
  #region Properties & Fields - Public

  /// <summary>The base URL for the Cloudflare REST API. Defaults to the standard v4 endpoint.</summary>
  public string ApiBaseUrl { get; set; } = "https://api.cloudflare.com/client/v4/";

  /// <summary>The Cloudflare API token. This is required for authentication.</summary>
  public string ApiToken { get; set; } = string.Empty;

  /// <summary>The Cloudflare Account ID. Required for account-level API calls.</summary>
  public string AccountId { get; set; } = string.Empty;

  /// <summary>The URL for the Cloudflare GraphQL API. Defaults to the standard endpoint.</summary>
  public string GraphQlApiUrl { get; set; } = "https://api.cloudflare.com/client/v4/graphql";

  #endregion
}
