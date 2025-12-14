namespace Cloudflare.NET.Zones.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>
///   Represents a zone setting identifier.
///   <para>
///     Zone settings control various aspects of zone behavior including security,
///     performance, caching, and network configuration.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing custom values
///     for new settings that may be added to the Cloudflare API in the future.
///   </para>
///   <para>
///     Use the static properties for known settings (e.g., <see cref="Ssl" />, <see cref="MinTlsVersion" />)
///     or create custom values using the constructor or implicit string conversion.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Using a known setting with IntelliSense
///   var setting = await zones.GetZoneSettingAsync(zoneId, ZoneSettingId.MinTlsVersion);
///
///   // Using implicit conversion from string for new/unknown settings
///   ZoneSettingId customSetting = "new_setting";
///   var result = await zones.GetZoneSettingAsync(zoneId, customSetting);
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/api/resources/zones/subresources/settings/" />
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<ZoneSettingId>))]
public readonly struct ZoneSettingId : IExtensibleEnum<ZoneSettingId>, IEquatable<ZoneSettingId>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this setting identifier.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this setting identifier.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values - Security Settings

  /// <summary>Advanced DDoS protection (Business/Enterprise plans only).</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>This setting may not be editable on all plans.</para>
  /// </remarks>
  public static ZoneSettingId AdvancedDdos { get; } = new("advanced_ddos");

  /// <summary>Aegis - dedicated egress IPs for layer 7 WAF and CDN.</summary>
  /// <remarks>Value: Editable structure.</remarks>
  public static ZoneSettingId Aegis { get; } = new("aegis");

  /// <summary>Always use HTTPS - enforce HTTPS connections.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>When enabled, any HTTP request is redirected to HTTPS with a 301 redirect.</para>
  /// </remarks>
  public static ZoneSettingId AlwaysUseHttps { get; } = new("always_use_https");

  /// <summary>Automatic HTTPS Rewrites - automatically rewrite HTTP links to HTTPS.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Helps fix mixed content issues by rewriting insecure URLs in HTML responses.</para>
  /// </remarks>
  public static ZoneSettingId AutomaticHttpsRewrites { get; } = new("automatic_https_rewrites");

  /// <summary>Browser integrity check.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Evaluates incoming HTTP headers against known threats and blocks malicious requests.</para>
  /// </remarks>
  public static ZoneSettingId BrowserCheck { get; } = new("browser_check");

  /// <summary>Challenge passage TTL in seconds.</summary>
  /// <remarks>
  ///   <para>Value: integer (seconds)</para>
  ///   <para>Specifies how long a visitor can browse your site after completing a challenge. Recommended: 15-45 minutes.</para>
  /// </remarks>
  public static ZoneSettingId ChallengeTtl { get; } = new("challenge_ttl");

  /// <summary>TLS cipher suite allowlist in BoringSSL format.</summary>
  /// <remarks>
  ///   <para>Value: Custom cipher list (array of strings).</para>
  ///   <para>Allows specifying which cipher suites are permitted for TLS connections.</para>
  /// </remarks>
  public static ZoneSettingId Ciphers { get; } = new("ciphers");

  /// <summary>Email obfuscation - hide email addresses from bots.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Obfuscates email addresses on your website to prevent harvesting by bots.</para>
  /// </remarks>
  public static ZoneSettingId EmailObfuscation { get; } = new("email_obfuscation");

  /// <summary>Minimum TLS version requirement.</summary>
  /// <remarks>
  ///   <para>Value: "1.0", "1.1", "1.2", "1.3"</para>
  ///   <para>Specifies the minimum TLS protocol version clients must support to connect.</para>
  /// </remarks>
  public static ZoneSettingId MinTlsVersion { get; } = new("min_tls_version");

  /// <summary>Opportunistic encryption support.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Enables opportunistic encryption for browsers that support it.</para>
  /// </remarks>
  public static ZoneSettingId OpportunisticEncryption { get; } = new("opportunistic_encryption");

  /// <summary>Opportunistic Onion - Alt-Svc header for Tor connections.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Adds Alt-Svc header to allow Tor connections to your website.</para>
  /// </remarks>
  public static ZoneSettingId OpportunisticOnion { get; } = new("opportunistic_onion");

  /// <summary>Security level - controls how aggressive Cloudflare challenges visitors.</summary>
  /// <remarks>
  ///   <para>Value: "off", "essentially_off", "low", "medium", "high", "under_attack"</para>
  ///   <para>Higher levels challenge more visitors based on threat reputation.</para>
  /// </remarks>
  /// <seealso cref="ZoneSecurityLevel" />
  public static ZoneSettingId SecurityLevel { get; } = new("security_level");

  /// <summary>Security headers configuration.</summary>
  /// <remarks>Value: Editable structure containing security header settings.</remarks>
  public static ZoneSettingId SecurityHeaders { get; } = new("security_headers");

  /// <summary>Server-side excludes - hide content from suspicious visitors.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Hides specific content from visitors with a bad IP reputation.</para>
  /// </remarks>
  public static ZoneSettingId ServerSideExcludes { get; } = new("server_side_excludes");

  /// <summary>SSL/TLS encryption mode.</summary>
  /// <remarks>
  ///   <para>Value: "off", "flexible", "full", "strict"</para>
  ///   <para>Controls how traffic is encrypted between visitors, Cloudflare, and your origin server.</para>
  /// </remarks>
  /// <seealso cref="SslMode" />
  public static ZoneSettingId Ssl { get; } = new("ssl");

  /// <summary>SSL Recommender - automated SSL/TLS mode recommendation emails.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Receives email recommendations for optimal SSL/TLS configuration.</para>
  /// </remarks>
  public static ZoneSettingId SslRecommender { get; } = new("ssl_recommender");

  /// <summary>TLS 1.3 encryption support.</summary>
  /// <remarks>
  ///   <para>Value: "on", "off", "zrt" (with 0-RTT)</para>
  ///   <para>Enables TLS 1.3 protocol for improved security and performance.</para>
  /// </remarks>
  /// <seealso cref="Tls13Setting" />
  public static ZoneSettingId Tls13 { get; } = new("tls_1_3");

  /// <summary>TLS client certificate authentication (Enterprise only).</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Requires clients to present a valid TLS certificate to connect.</para>
  /// </remarks>
  public static ZoneSettingId TlsClientAuth { get; } = new("tls_client_auth");

  /// <summary>True-Client-IP header support.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Sends the visitor's original IP address to your origin in the True-Client-IP header.</para>
  /// </remarks>
  public static ZoneSettingId TrueClientIpHeader { get; } = new("true_client_ip_header");

  /// <summary>Web Application Firewall.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Enables the Cloudflare Web Application Firewall (WAF) for the zone.</para>
  /// </remarks>
  public static ZoneSettingId Waf { get; } = new("waf");

  #endregion


  #region Known Values - Performance Settings

  /// <summary>Always Online - serve cached content from Internet Archive if origin is offline.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Shows a cached version of your site when your origin server is unavailable.</para>
  /// </remarks>
  public static ZoneSettingId AlwaysOnline { get; } = new("always_online");

  /// <summary>Automatic Platform Optimization (APO) settings.</summary>
  /// <remarks>Value: Editable structure containing APO caching and optimization settings.</remarks>
  public static ZoneSettingId AutomaticPlatformOptimization { get; } = new("automatic_platform_optimization");

  /// <summary>Brotli compression for supported clients.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Applies Brotli compression to speed up page load times for supported browsers.</para>
  /// </remarks>
  public static ZoneSettingId Brotli { get; } = new("brotli");

  /// <summary>Browser cache TTL in seconds.</summary>
  /// <remarks>
  ///   <para>Value: integer (seconds); 0 = respect origin headers</para>
  ///   <para>Specifies how long browsers should cache assets from your site.</para>
  /// </remarks>
  public static ZoneSettingId BrowserCacheTtl { get; } = new("browser_cache_ttl");

  /// <summary>Cache level - controls caching behavior.</summary>
  /// <remarks>
  ///   <para>Value: "bypass", "basic", "simplified", "aggressive", "cache_everything"</para>
  ///   <para>Determines how Cloudflare caches content from your origin.</para>
  /// </remarks>
  /// <seealso cref="CacheLevel" />
  public static ZoneSettingId CacheLevel { get; } = new("cache_level");

  /// <summary>Development mode - bypass cache for 3 hours.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Temporarily bypasses Cloudflare's cache to see changes immediately during development.</para>
  /// </remarks>
  public static ZoneSettingId DevelopmentMode { get; } = new("development_mode");

  /// <summary>Early Hints - HTTP 103 response with Link headers.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Sends HTTP 103 responses to preload assets before the main response arrives.</para>
  /// </remarks>
  public static ZoneSettingId EarlyHints { get; } = new("early_hints");

  /// <summary>Cloudflare Fonts delivery optimization.</summary>
  /// <remarks>Value: Editable structure containing font optimization settings.</remarks>
  public static ZoneSettingId FontSettings { get; } = new("font_settings");

  /// <summary>HTTP/2 Edge Prioritization optimization.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Enables advanced HTTP/2 prioritization for improved performance.</para>
  /// </remarks>
  public static ZoneSettingId H2Prioritization { get; } = new("h2_prioritization");

  /// <summary>Hotlink protection - prevent image theft.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Prevents other websites from embedding images hosted on your site.</para>
  /// </remarks>
  public static ZoneSettingId HotlinkProtection { get; } = new("hotlink_protection");

  /// <summary>HTTP/2 protocol support.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Enables HTTP/2 protocol for visitors connecting to your site through Cloudflare.</para>
  /// </remarks>
  public static ZoneSettingId Http2 { get; } = new("http2");

  /// <summary>HTTP/3 protocol support (with QUIC).</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Enables HTTP/3 protocol for improved performance on unreliable networks.</para>
  /// </remarks>
  public static ZoneSettingId Http3 { get; } = new("http3");

  /// <summary>Image Resizing - on-demand image transformation service.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Allows resizing, cropping, and converting images on-the-fly.</para>
  /// </remarks>
  public static ZoneSettingId ImageResizing { get; } = new("image_resizing");

  /// <summary>Mirage - image optimization for mobile devices.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Optimizes image loading for mobile visitors with slow connections.</para>
  /// </remarks>
  public static ZoneSettingId Mirage { get; } = new("mirage");

  /// <summary>Image optimization (Polish).</summary>
  /// <remarks>
  ///   <para>Value: "off", "lossless", "lossy"</para>
  ///   <para>Optimizes images by removing metadata and applying compression.</para>
  /// </remarks>
  /// <seealso cref="PolishSetting" />
  public static ZoneSettingId Polish { get; } = new("polish");

  /// <summary>Prefetch Preload - prefetch URLs from response headers (Enterprise only).</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Prefetches URLs specified in Link headers to improve page load times.</para>
  /// </remarks>
  public static ZoneSettingId PrefetchPreload { get; } = new("prefetch_preload");

  /// <summary>Rocket Loader - JavaScript optimization.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Improves page rendering by asynchronously loading JavaScript.</para>
  /// </remarks>
  public static ZoneSettingId RocketLoader { get; } = new("rocket_loader");

  /// <summary>Sort query string for cache - normalize query parameters.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Sorts query string parameters alphabetically for improved cache hit ratio.</para>
  /// </remarks>
  public static ZoneSettingId SortQueryStringForCache { get; } = new("sort_query_string_for_cache");

  /// <summary>WebP image format delivery.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Converts images to WebP format for browsers that support it.</para>
  /// </remarks>
  public static ZoneSettingId Webp { get; } = new("webp");

  /// <summary>0-RTT Connection Resumption (Zero Round Trip Time).</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Allows TLS 1.3 connections to resume without a round trip, reducing latency.</para>
  /// </remarks>
  public static ZoneSettingId ZeroRtt { get; } = new("0rtt");

  #endregion


  #region Known Values - Network Settings

  /// <summary>IP geolocation header.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Adds the CF-IPCountry header containing visitor's country code to requests.</para>
  /// </remarks>
  public static ZoneSettingId IpGeolocation { get; } = new("ip_geolocation");

  /// <summary>IPv6 compatibility for subdomains.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Enables IPv6 support for your zone.</para>
  /// </remarks>
  public static ZoneSettingId Ipv6 { get; } = new("ipv6");

  /// <summary>Network Error Logging (NEL) reporting (Beta).</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Enables Network Error Logging for visibility into connectivity issues.</para>
  /// </remarks>
  public static ZoneSettingId Nel { get; } = new("nel");

  /// <summary>Orange to Orange - CNAME to other Cloudflare zones.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Allows CNAME records pointing to other Cloudflare-proxied domains.</para>
  /// </remarks>
  public static ZoneSettingId OrangeToOrange { get; } = new("orange_to_orange");

  /// <summary>Origin error page pass-through.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Passes through error pages from your origin instead of showing Cloudflare error pages.</para>
  /// </remarks>
  public static ZoneSettingId OriginErrorPagePassThru { get; } = new("origin_error_page_pass_thru");

  /// <summary>Maximum HTTP version to origin.</summary>
  /// <remarks>
  ///   <para>Value: "1", "2"</para>
  ///   <para>Sets the maximum HTTP version Cloudflare uses when connecting to your origin.</para>
  /// </remarks>
  public static ZoneSettingId OriginMaxHttpVersion { get; } = new("origin_max_http_version");

  /// <summary>Proxy read timeout duration in seconds.</summary>
  /// <remarks>
  ///   <para>Value: integer (seconds)</para>
  ///   <para>Maximum time Cloudflare waits for a response from your origin.</para>
  /// </remarks>
  public static ZoneSettingId ProxyReadTimeout { get; } = new("proxy_read_timeout");

  /// <summary>Pseudo IPv4 header setting.</summary>
  /// <remarks>
  ///   <para>Value: "off", "add_header", "overwrite_header"</para>
  ///   <para>Adds a pseudo IPv4 address for applications that don't support IPv6.</para>
  /// </remarks>
  /// <seealso cref="PseudoIpv4Setting" />
  public static ZoneSettingId PseudoIpv4 { get; } = new("pseudo_ipv4");

  /// <summary>Response buffering behavior.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Controls whether Cloudflare buffers responses before sending to visitors.</para>
  /// </remarks>
  public static ZoneSettingId ResponseBuffering { get; } = new("response_buffering");

  /// <summary>WebSocket connection support.</summary>
  /// <remarks>
  ///   <para>Value: "on" / "off"</para>
  ///   <para>Enables WebSocket connections through Cloudflare.</para>
  /// </remarks>
  public static ZoneSettingId Websockets { get; } = new("websockets");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="ZoneSettingId" /> with the specified value.</summary>
  /// <param name="value">The string value representing the setting identifier.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public ZoneSettingId(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static ZoneSettingId Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to a <see cref="ZoneSettingId" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator ZoneSettingId(string value) => new(value);

  /// <summary>Implicitly converts a <see cref="ZoneSettingId" /> to its string value.</summary>
  /// <param name="settingId">The setting identifier to convert.</param>
  public static implicit operator string(ZoneSettingId settingId) => settingId.Value;

  /// <summary>Determines whether two <see cref="ZoneSettingId" /> values are equal.</summary>
  public static bool operator ==(ZoneSettingId left, ZoneSettingId right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="ZoneSettingId" /> values are not equal.</summary>
  public static bool operator !=(ZoneSettingId left, ZoneSettingId right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(ZoneSettingId other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is ZoneSettingId other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}
