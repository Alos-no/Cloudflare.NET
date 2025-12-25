namespace Cloudflare.NET.Accounts.D1.Models;

using System.Text.Json.Serialization;

#region Import Request

/// <summary>Request for initiating or polling a database import.</summary>
/// <param name="Action">The import action: "init" to start, "poll" to check status, "ingest" to complete.</param>
/// <param name="Etag">MD5 hash/ETag of the SQL file (required for "init" and "ingest" actions).</param>
/// <param name="Filename">Filename from init response (required for "ingest" action).</param>
/// <param name="CurrentBookmark">Bookmark from previous poll response (for polling).</param>
internal record D1ImportRequest(
  [property: JsonPropertyName("action")]
  string Action,

  [property: JsonPropertyName("etag")]
  string? Etag = null,

  [property: JsonPropertyName("filename")]
  string? Filename = null,

  [property: JsonPropertyName("current_bookmark")]
  string? CurrentBookmark = null
);

#endregion


#region Import Response

/// <summary>Response from a database import operation.</summary>
/// <param name="AtBookmark">Time-travel bookmark for polling.</param>
/// <param name="Status">Import status: "active", "complete", or "error".</param>
/// <param name="Result">Import result details.</param>
/// <param name="Error">Error message (only present when status is "error").</param>
/// <param name="Type">Operation type: "import".</param>
/// <param name="Success">Whether the operation was successful.</param>
/// <param name="Filename">Filename for upload (when action is "init").</param>
/// <param name="UploadUrl">Temporary URL for uploading SQL file (when action is "init").</param>
/// <param name="Messages">Status messages from the operation.</param>
public record D1ImportResponse(
  [property: JsonPropertyName("at_bookmark")]
  string? AtBookmark = null,

  [property: JsonPropertyName("status")]
  string? Status = null,

  [property: JsonPropertyName("result")]
  D1ImportResultDetails? Result = null,

  [property: JsonPropertyName("error")]
  string? Error = null,

  [property: JsonPropertyName("type")]
  string? Type = null,

  [property: JsonPropertyName("success")]
  bool Success = false,

  [property: JsonPropertyName("filename")]
  string? Filename = null,

  [property: JsonPropertyName("upload_url")]
  string? UploadUrl = null,

  [property: JsonPropertyName("messages")]
  IReadOnlyList<string>? Messages = null
);

/// <summary>Import result details.</summary>
/// <param name="NumQueries">Number of queries executed during import.</param>
/// <param name="Meta">Aggregated metadata from all import queries.</param>
public record D1ImportResultDetails(
  [property: JsonPropertyName("num_queries")]
  int? NumQueries = null,

  [property: JsonPropertyName("meta")]
  D1QueryMeta? Meta = null
);

#endregion
