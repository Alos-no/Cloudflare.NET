namespace Cloudflare.NET.Core;

using System.Net.Http.Headers;

/// <summary>
///   Configures <see cref="HttpClient" /> instances for Cloudflare API communication.
///   Used by both DI registration and dynamic client creation paths to ensure
///   consistent HTTP client configuration.
/// </summary>
/// <remarks>
///   <para>
///     This class centralizes the HttpClient configuration logic that was previously
///     duplicated between the DI registration path and the dynamic client creation path.
///   </para>
///   <para>
///     Configuration includes:
///   </para>
///   <list type="bullet">
///     <item>
///       <description>
///         <b>Base Address</b> - Set from <see cref="CloudflareApiOptions.ApiBaseUrl" />.
///       </description>
///     </item>
///     <item>
///       <description>
///         <b>Timeout</b> - Set to a long value (5 minutes) to allow the resilience pipeline
///         to handle timeouts. The actual timeout is controlled by the pipeline.
///       </description>
///     </item>
///     <item>
///       <description>
///         <b>Authorization Header</b> - Optionally set from <see cref="CloudflareApiOptions.ApiToken" />.
///       </description>
///     </item>
///   </list>
/// </remarks>
public static class CloudflareHttpClientConfigurator
{
  #region Constants

  /// <summary>
  ///   The HttpClient timeout. This is intentionally long to allow the resilience
  ///   pipeline's timeout strategies to be the effective timeout controllers.
  /// </summary>
  /// <remarks>
  ///   Ref: https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience#httpclient-timeout
  /// </remarks>
  private static readonly TimeSpan HttpClientTimeout = TimeSpan.FromMinutes(5);

  #endregion


  #region Methods - Public

  /// <summary>
  ///   Configures an <see cref="HttpClient" /> for Cloudflare API communication.
  /// </summary>
  /// <param name="client">The HttpClient to configure.</param>
  /// <param name="options">The Cloudflare API options containing configuration values.</param>
  /// <param name="setAuthorizationHeader">
  ///   If true, sets the Authorization header from the options. Set to false when
  ///   authentication is handled separately (e.g., via <see cref="Auth.AuthenticationHandler" />).
  /// </param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="client" /> or <paramref name="options" /> is null.</exception>
  /// <exception cref="InvalidOperationException">Thrown when <see cref="CloudflareApiOptions.ApiBaseUrl" /> is null or whitespace.</exception>
  /// <remarks>
  ///   <para>
  ///     The HttpClient timeout is set to a long value (5 minutes) to ensure that the resilience
  ///     pipeline's timeout strategies are the effective timeout controllers. Without this, the
  ///     HttpClient's default 100-second timeout would interfere with retry attempts.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  /// // For DI registration path (auth handled by AuthenticationHandler)
  /// CloudflareHttpClientConfigurator.Configure(httpClient, options, setAuthorizationHeader: false);
  ///
  /// // For named clients or dynamic clients (auth header set directly)
  /// CloudflareHttpClientConfigurator.Configure(httpClient, options, setAuthorizationHeader: true);
  /// </code>
  /// </example>
  public static void Configure(HttpClient           client,
                               CloudflareApiOptions options,
                               bool                 setAuthorizationHeader = true)
  {
    ArgumentNullException.ThrowIfNull(client);
    ArgumentNullException.ThrowIfNull(options);

    // Validate the API base URL.
    if (string.IsNullOrWhiteSpace(options.ApiBaseUrl))
      throw new InvalidOperationException(
        "Cloudflare API Base URL is missing. Please configure it in the 'Cloudflare' settings section.");

    // Set the base address for all requests.
    client.BaseAddress = new Uri(options.ApiBaseUrl);

    // Set a long HttpClient.Timeout so that our resilience pipeline's TotalRequestTimeout is the effective timeout.
    // Ref: https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience#httpclient-timeout
    client.Timeout = HttpClientTimeout;

    // Optionally set the Authorization header.
    if (setAuthorizationHeader && !string.IsNullOrWhiteSpace(options.ApiToken))
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiToken);
  }


  /// <summary>
  ///   Configures only the base properties of an <see cref="HttpClient" /> (base address and timeout),
  ///   without setting the Authorization header.
  /// </summary>
  /// <param name="client">The HttpClient to configure.</param>
  /// <param name="options">The Cloudflare API options containing configuration values.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="client" /> or <paramref name="options" /> is null.</exception>
  /// <exception cref="InvalidOperationException">Thrown when <see cref="CloudflareApiOptions.ApiBaseUrl" /> is null or whitespace.</exception>
  /// <remarks>
  ///   <para>
  ///     This overload is provided for backward compatibility and convenience when authentication
  ///     is handled by a separate <see cref="Auth.AuthenticationHandler" />.
  ///   </para>
  /// </remarks>
  public static void ConfigureBase(HttpClient client, CloudflareApiOptions options)
  {
    Configure(client, options, setAuthorizationHeader: false);
  }

  #endregion
}
