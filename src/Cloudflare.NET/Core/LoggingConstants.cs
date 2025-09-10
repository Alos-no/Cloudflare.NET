namespace Cloudflare.NET.Core;

/// <summary>
///   Centralized logging categories for Cloudflare.NET. Use these instead of raw strings
///   to enable consistent, DRY category filtering and avoid typos.
/// </summary>
public static class LoggingConstants
{
  public static class Categories
  {
    #region Constants & Statics

    /// <summary>
    ///   Category used by the HTTP 429 rate limiting (Polly) resilience pipeline. This enables
    ///   tuning verbosity for rate-limit diagnostics independently.
    /// </summary>
    public const string HttpResilience = "Cloudflare.NET.Http.Resilience";

    /// <summary>
    ///   Category for the <c>Cloudflare.NET.Core.Auth.AuthenticationHandler</c>. This is
    ///   typically resolved automatically via <c>ILogger&lt;AuthenticationHandler&gt;</c>.
    /// </summary>
    public const string Authentication = "Cloudflare.NET.Core.Auth.AuthenticationHandler";

    /// <summary>
    ///   Category for the <c>Cloudflare.NET.Analytics.AnalyticsApi</c>. This is typically
    ///   resolved automatically via <c>ILogger&lt;AnalyticsApi&gt;</c>.
    /// </summary>
    public const string Analytics = "Cloudflare.NET.Analytics.AnalyticsApi";

    /// <summary>
    ///   Category for the <c>Cloudflare.NET.R2.R2Client</c>. This is typically resolved
    ///   automatically via <c>ILogger&lt;R2Client&gt;</c>.
    /// </summary>
    public const string R2 = "Cloudflare.NET.R2.R2Client";

    #endregion
  }
}
