namespace Cloudflare.NET.Core.Models;

using Microsoft.Extensions.Logging;

/// <summary>
///   Contains source-generated logging definitions for the ApiResource using LoggerMessage for high performance.
/// </summary>
internal static partial class ApiResourceLogs
{
  [LoggerMessage(
    EventId = 101,
    Level = LogLevel.Trace,
    Message = "Sending {Method} request to {RequestUri}")]
  public static partial void SendingRequest(this ILogger logger, string method, string requestUri);

  [LoggerMessage(
    EventId = 102,
    Level = LogLevel.Debug,
    Message = "Received response with status code {StatusCode} for request to {RequestUri}")]
  public static partial void ReceivedResponse(this ILogger logger, System.Net.HttpStatusCode statusCode, Uri? requestUri);

  [LoggerMessage(
    EventId = 103,
    Level = LogLevel.Error,
    Message =
      "Cloudflare API request to {RequestUri} failed with status code {StatusCode} ({ReasonPhrase}). Response Body: {ResponseBody}")]
  public static partial void RequestFailed(
    this ILogger logger,
    Uri?         requestUri,
    int          statusCode,
    string?      reasonPhrase,
    string       responseBody);

  [LoggerMessage(
    EventId = 104,
    Level = LogLevel.Warning,
    Message = "Cloudflare diagnostics: CF-RAY(s)={CfRays}; Retry-After={RetryAfter}; Date={Date}")]
  public static partial void LogDiagnostics(this ILogger logger, string cfRays, string retryAfter, string date);

  [LoggerMessage(
    EventId = 105,
    Level = LogLevel.Error,
    Message = "Failed to deserialize Cloudflare API response from {RequestUri}. Raw response: {ResponseBody}")]
  public static partial void DeserializationFailed(this ILogger logger, Exception ex, Uri? requestUri, string responseBody);

  [LoggerMessage(
    EventId = 106,
    Level = LogLevel.Warning,
    Message =
      "Cloudflare API request to {RequestUri} returned a failure response: {ErrorMessages}. Raw response: {ResponseBody}")]
  public static partial void ApiReturnedFailure(
    this ILogger logger,
    Uri?         requestUri,
    string       errorMessages,
    string       responseBody);

  [LoggerMessage(
    EventId = 107,
    Level = LogLevel.Trace,
    Message = "Successfully processed successful response from {RequestUri}")]
  public static partial void ProcessedSuccessResponse(this ILogger logger, Uri? requestUri);
}
