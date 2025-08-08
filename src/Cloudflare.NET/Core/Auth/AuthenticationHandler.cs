namespace Cloudflare.NET.Core.Auth;

using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

/// <summary>
///   A delegating handler that injects the Cloudflare API token into the Authorization
///   header of each outgoing request.
/// </summary>
/// <remarks>Initializes a new instance of the <see cref="AuthenticationHandler" /> class.</remarks>
/// <param name="options">The Cloudflare API options containing the token.</param>
public class AuthenticationHandler(IOptions<CloudflareApiOptions> options) : DelegatingHandler
{
  #region Properties & Fields - Non-Public

  /// <summary>The Cloudflare API token used for authentication.</summary>
  private readonly string _apiToken = options.Value.ApiToken;

  #endregion

  #region Methods Impl

  /// <summary>Overrides the SendAsync method to add the Authorization header.</summary>
  /// <param name="request">The HTTP request message.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The HTTP response message.</returns>
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    // Set the authorization header for the API token.
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

    // Continue the request pipeline.
    return await base.SendAsync(request, cancellationToken);
  }

  #endregion
}
