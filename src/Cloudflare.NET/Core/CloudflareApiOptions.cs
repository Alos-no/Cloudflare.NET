namespace Cloudflare.NET.Core;

/// <summary>Configures the automatic retry policy for rate-limited requests (HTTP 429).</summary>
public class RateLimitingOptions
{
  #region Properties & Fields - Public

  /// <summary>
  ///   Gets or sets a value indicating whether automatic rate-limit handling is enabled. Defaults to
  ///   <see langword="false" />.
  /// </summary>
  public bool IsEnabled { get; set; }

  /// <summary>Gets or sets the maximum number of retries for a rate-limited request. Defaults to 2.</summary>
  public int MaxRetries { get; set; } = 2;

  /// <summary>
  ///   Gets or sets the maximum number of concurrent requests allowed by the client-side rate limiter. Defaults to
  ///   25. This helps prevent sending a burst of requests that would trigger a 429 response.
  /// </summary>
  public int PermitLimit { get; set; } = 20;

  /// <summary>Gets or sets the number of requests that can be queued when the concurrency limit is reached. Defaults to 50.</summary>
  public int QueueLimit { get; set; } = 50;

  #endregion
}

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

  /// <summary>
  ///   Gets or sets the default timeout for a single HTTP request attempt. Defaults to 30 seconds. This can be
  ///   configured in appsettings.json (e.g., "00:00:30").
  /// </summary>
  public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

  /// <summary>The URL for the Cloudflare GraphQL API. Defaults to the standard endpoint.</summary>
  public string GraphQlApiUrl { get; set; } = "https://api.cloudflare.com/client/v4/graphql";

  /// <summary>Gets or sets the configuration for automatic rate-limit handling.</summary>
  public RateLimitingOptions RateLimiting { get; set; } = new();

  #endregion
}
