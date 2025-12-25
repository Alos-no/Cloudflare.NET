namespace Cloudflare.NET.Accounts.D1.Models;

using System.Text.Json.Serialization;

#region Export Options

/// <summary>Options for filtering what data to include in a database export.</summary>
/// <param name="NoData">If true, export only table definitions (schema), not contents.</param>
/// <param name="NoSchema">If true, export only table contents (data), not definitions.</param>
/// <param name="Tables">Filter export to specific tables. Empty or null means all tables.</param>
public record D1ExportDumpOptions(
  [property: JsonPropertyName("no_data")]
  bool? NoData = null,

  [property: JsonPropertyName("no_schema")]
  bool? NoSchema = null,

  [property: JsonPropertyName("tables")]
  IReadOnlyList<string>? Tables = null
);

#endregion


#region Export Request/Response

/// <summary>Request for initiating or polling a database export.</summary>
/// <param name="OutputFormat">The output format. Use "polling" for async exports.</param>
/// <param name="CurrentBookmark">Bookmark from previous poll response (for polling).</param>
/// <param name="DumpOptions">Optional filtering options for the export.</param>
internal record D1ExportRequest(
  [property: JsonPropertyName("output_format")]
  string OutputFormat = "polling",

  [property: JsonPropertyName("current_bookmark")]
  string? CurrentBookmark = null,

  [property: JsonPropertyName("dump_options")]
  D1ExportDumpOptions? DumpOptions = null
);

/// <summary>Response from a database export operation.</summary>
/// <param name="AtBookmark">Time-travel bookmark for polling. Stable for duration of export.</param>
/// <param name="Status">Export status: "active", "complete", or "error".</param>
/// <param name="Result">Export result containing download URL (when status is "complete").</param>
/// <param name="Error">Error message (only present when status is "error").</param>
/// <param name="Type">Operation type: "export".</param>
/// <param name="Success">Whether the operation was successful.</param>
/// <param name="Messages">Status messages from the operation.</param>
public record D1ExportResponse(
  [property: JsonPropertyName("at_bookmark")]
  string? AtBookmark = null,

  [property: JsonPropertyName("status")]
  string? Status = null,

  [property: JsonPropertyName("result")]
  D1ExportResultDetails? Result = null,

  [property: JsonPropertyName("error")]
  string? Error = null,

  [property: JsonPropertyName("type")]
  string? Type = null,

  [property: JsonPropertyName("success")]
  bool Success = false,

  [property: JsonPropertyName("messages")]
  IReadOnlyList<string>? Messages = null
);

/// <summary>Export result details containing the download URL.</summary>
/// <param name="Filename">The filename of the exported SQL file.</param>
/// <param name="SignedUrl">Temporary signed URL to download the SQL file.</param>
public record D1ExportResultDetails(
  [property: JsonPropertyName("filename")]
  string? Filename = null,

  [property: JsonPropertyName("signed_url")]
  string? SignedUrl = null
);

#endregion
