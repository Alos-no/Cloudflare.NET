namespace Cloudflare.NET.Core.Auth;

using Microsoft.Extensions.Logging;

/// <summary>
///   Contains logging definitions for the AuthenticationHandler. For .NET 6+, these use source-generated
///   LoggerMessage for high performance. For .NET Standard 2.1, manual implementations are used.
/// </summary>
internal static partial class AuthLogs
{
#if NET6_0_OR_GREATER
  #region Source-Generated Logging (NET6+)

  [LoggerMessage(
    EventId = 201,
    Level = LogLevel.Trace,
    Message = "Adding Authorization header to outgoing request for {RequestUri}")]
  public static partial void AddingAuthHeader(this ILogger logger, Uri? requestUri);

  #endregion

#else

  #region Manual Logging (NetStandard2.1)

  public static void AddingAuthHeader(this ILogger logger, Uri? requestUri)
  {
    if (logger.IsEnabled(LogLevel.Trace))
      logger.LogTrace("Adding Authorization header to outgoing request for {RequestUri}", requestUri);
  }

  #endregion

#endif
}
