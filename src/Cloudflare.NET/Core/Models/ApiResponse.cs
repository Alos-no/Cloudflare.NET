namespace Cloudflare.NET.Core.Models;

using System.Text.Json.Serialization;

/// <summary>Represents the standard envelope for all Cloudflare API responses.</summary>
/// <typeparam name="T">The type of the 'result' payload.</typeparam>
public sealed record ApiResponse<T>(
  [property: JsonPropertyName("success")]
  bool Success,
  [property: JsonPropertyName("errors")]
  IReadOnlyList<ApiError> Errors,
  [property: JsonPropertyName("messages")]
  IReadOnlyList<ApiMessage> Messages,
  [property: JsonPropertyName("result")]
  T? Result
);

/// <summary>Represents a single error object in a Cloudflare API response.</summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
public sealed record ApiError(
  [property: JsonPropertyName("code")]
  int Code,
  [property: JsonPropertyName("message")]
  string Message
);

/// <summary>Represents a single message object in a Cloudflare API response.</summary>
/// <param name="Code">The message code.</param>
/// <param name="Message">The message text.</param>
public sealed record ApiMessage(
  [property: JsonPropertyName("code")]
  int Code,
  [property: JsonPropertyName("message")]
  string Message
);
