namespace Cloudflare.NET.Security;

/// <summary>
///   Provides centralized, strongly-typed constants for Cloudflare security features, reducing the risk of errors
///   from using raw string literals.
/// </summary>
public static class SecurityConstants
{
  /// <summary>
  ///   Constants for the Cloudflare Ruleset Engine phases. These are used to identify the execution stage for a
  ///   ruleset.
  /// </summary>
  public static class RulesetPhases
  {
    #region Constants & Statics

    /// <summary>WAF Managed Rules. Deploys managed rulesets.</summary>
    public const string HttpRequestFirewallManaged = "http_request_firewall_managed";
    /// <summary>WAF Custom Rules. Deploys account/zone custom rules.</summary>
    public const string HttpRequestFirewallCustom = "http_request_firewall_custom";
    /// <summary>Rate Limiting Rules.</summary>
    public const string HttpRateLimit = "http_ratelimit";
    /// <summary>HTTP Request Header Modification.</summary>
    public const string HttpRequestLateTransform = "http_request_late_transform";
    /// <summary>HTTP Origin Rules.</summary>
    public const string HttpRequestOrigin = "http_request_origin";
    /// <summary>HTTP Request Sanitize.</summary>
    public const string HttpRequestSanitize = "http_request_sanitize";
    /// <summary>HTTP Request Dynamic Redirect.</summary>
    public const string HttpRequestDynamicRedirect = "http_request_dynamic_redirect";
    /// <summary>HTTP Response Header Modification.</summary>
    public const string HttpResponseHeadersTransform = "http_response_headers_transform";
    /// <summary>Log Custom Fields.</summary>
    public const string HttpLogCustomFields = "http_log_custom_fields";

    #endregion
  }

  /// <summary>Constants for the products that can be skipped using the 'skip' action.</summary>
  public static class SkipProducts
  {
    #region Constants & Statics

    public const string ZoneLockdown  = "zoneLockdown";
    public const string UaBlock       = "uaBlock";
    public const string Bic           = "bic";
    public const string Hot           = "hot";
    public const string SecurityLevel = "securityLevel";
    public const string RateLimit     = "rateLimit";
    public const string Waf           = "waf";

    #endregion
  }

  /// <summary>Constants related to Rate Limiting rules.</summary>
  public static class RateLimiting
  {
    /// <summary>The set of properties of a request to use for counting.</summary>
    public static class Characteristics
    {
      #region Constants & Statics

      public const string IpSource           = "ip.src";
      public const string UriPath            = "http.request.uri.path";
      public const string CfConnectingIp     = "http.request.headers[\"cf-connecting-ip\"]";
      public const string XForwardedFor      = "http.request.headers[\"x-forwarded-for\"]";
      public const string ColoId             = "cf.colo.id";
      public const string BotManagementScore = "cf.bot_management.score";
      public const string ThreatScore        = "cf.threat_score";
      public const string HttpMethod         = "http.request.method";

      #endregion
    }

    /// <summary>The valid time periods in seconds for a rate limiting rule.</summary>
    public static class Periods
    {
      #region Constants & Statics

      public const int P10_SECONDS  = 10;
      public const int P60_SECONDS  = 60;
      public const int P120_SECONDS = 120;
      public const int P300_SECONDS = 300;
      public const int P600_SECONDS = 600;

      #endregion
    }

    /// <summary>The valid content types for a custom response from a rate limiting rule.</summary>
    public static class ContentTypes
    {
      #region Constants & Statics

      public const string TextPlain       = "text/plain";
      public const string TextXml         = "text/xml";
      public const string ApplicationJson = "application/json";
      public const string TextHtml        = "text/html";

      #endregion
    }
  }
}
