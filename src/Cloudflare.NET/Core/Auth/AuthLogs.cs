namespace Cloudflare.NET.Core.Auth;

using Microsoft.Extensions.Logging;

/// <summary>Contains high-performance, source-generated logging definitions for the AuthenticationHandler.</summary>
internal static partial class AuthLogs
{
  #region Methods

  [LoggerMessage(
    EventId = 201,
    Level = LogLevel.Trace,
    Message = "Adding Authorization header to outgoing request for {RequestUri}")]
  public static partial void AddingAuthHeader(this ILogger logger, Uri? requestUri);

  #endregion
}
