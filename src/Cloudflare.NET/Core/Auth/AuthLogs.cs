namespace Cloudflare.NET.Core.Auth;

using Microsoft.Extensions.Logging;

/// <summary>
///   Contains source-generated logging definitions for the AuthenticationHandler using LoggerMessage for high performance.
/// </summary>
internal static partial class AuthLogs
{
  [LoggerMessage(
    EventId = 201,
    Level = LogLevel.Trace,
    Message = "Adding Authorization header to outgoing request for {RequestUri}")]
  public static partial void AddingAuthHeader(this ILogger logger, Uri? requestUri);
}
