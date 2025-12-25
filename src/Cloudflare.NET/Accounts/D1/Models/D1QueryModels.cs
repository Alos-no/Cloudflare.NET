namespace Cloudflare.NET.Accounts.D1.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

#region Query Request

/// <summary>Request for executing a SQL query.</summary>
/// <param name="Sql">The SQL statement to execute. Supports multiple statements separated by semicolons.</param>
/// <param name="Params">Optional array of parameter values for parameterized queries.</param>
/// <remarks>
///   Use parameterized queries with ? placeholders to prevent SQL injection.
///   Example: "SELECT * FROM users WHERE id = ?" with params ["123"]
/// </remarks>
public record D1QueryRequest(
  [property: JsonPropertyName("sql")]
  string Sql,

  [property: JsonPropertyName("params")]
  IReadOnlyList<object?>? Params = null
);

#endregion


#region Query Result Types

/// <summary>Result of a D1 query execution.</summary>
/// <typeparam name="TRow">The type of each row in the results.</typeparam>
/// <param name="Meta">Metadata about the query execution.</param>
/// <param name="Results">The query result rows.</param>
/// <param name="Success">Whether the query executed successfully.</param>
public record D1QueryResult<TRow>(
  [property: JsonPropertyName("meta")]
  D1QueryMeta Meta,

  [property: JsonPropertyName("results")]
  IReadOnlyList<TRow> Results,

  [property: JsonPropertyName("success")]
  bool Success
);

/// <summary>Non-generic query result for dynamic/untyped results.</summary>
/// <param name="Meta">Metadata about the query execution.</param>
/// <param name="Results">The query result rows as JSON elements.</param>
/// <param name="Success">Whether the query executed successfully.</param>
public record D1QueryResult(
  [property: JsonPropertyName("meta")]
  D1QueryMeta Meta,

  [property: JsonPropertyName("results")]
  IReadOnlyList<JsonElement> Results,

  [property: JsonPropertyName("success")]
  bool Success
);

#endregion


#region Query Metadata

/// <summary>Metadata about a D1 query execution.</summary>
/// <param name="ChangedDb">Whether the database was modified by this query.</param>
/// <param name="Changes">Approximate number of rows modified (from sqlite3_total_changes).</param>
/// <param name="Duration">Duration of SQL execution inside the database (ms).</param>
/// <param name="LastRowId">Row ID of last inserted row (for INTEGER PRIMARY KEY tables, null for WITHOUT ROWID tables).</param>
/// <param name="RowsRead">Number of rows read during execution (including indices).</param>
/// <param name="RowsWritten">Number of rows written during execution (including indices).</param>
/// <param name="ServedBy">Version of Cloudflare's backend Worker that returned the result.</param>
/// <param name="ServedByPrimary">Whether the query was handled by the primary instance.</param>
/// <param name="ServedByRegion">Region code of the instance that handled the query.</param>
/// <param name="SizeAfter">Database size in bytes after the query committed.</param>
/// <param name="Timings">Detailed timing information.</param>
/// <param name="TotalAttempts">Number of execution attempts for read-only queries (D1 auto-retries up to 2 times).</param>
public record D1QueryMeta(
  [property: JsonPropertyName("changed_db")]
  bool ChangedDb,

  [property: JsonPropertyName("changes")]
  int Changes,

  [property: JsonPropertyName("duration")]
  double Duration,

  [property: JsonPropertyName("last_row_id")]
  long LastRowId,

  [property: JsonPropertyName("rows_read")]
  long RowsRead,

  [property: JsonPropertyName("rows_written")]
  long RowsWritten,

  [property: JsonPropertyName("served_by")]
  string? ServedBy = null,

  [property: JsonPropertyName("served_by_primary")]
  bool? ServedByPrimary = null,

  [property: JsonPropertyName("served_by_region")]
  string? ServedByRegion = null,

  [property: JsonPropertyName("size_after")]
  long? SizeAfter = null,

  [property: JsonPropertyName("timings")]
  D1QueryTimings? Timings = null,

  [property: JsonPropertyName("total_attempts")]
  int? TotalAttempts = null
);

/// <summary>Detailed timing information for a D1 query.</summary>
/// <param name="SqlDurationMs">SQL execution duration in milliseconds.</param>
public record D1QueryTimings(
  [property: JsonPropertyName("sql_duration_ms")]
  double? SqlDurationMs = null
);

#endregion


#region Raw Query Result Types

/// <summary>Result of a raw query execution (array format).</summary>
/// <remarks>
///   Raw queries return rows as arrays instead of objects for better performance.
///   Column names are provided in the Columns array; row values are in the Rows array.
/// </remarks>
/// <param name="Meta">Metadata about the query execution.</param>
/// <param name="Results">The raw query result set containing columns and rows.</param>
/// <param name="Success">Whether the query executed successfully.</param>
public record D1RawQueryResult(
  [property: JsonPropertyName("meta")]
  D1QueryMeta Meta,

  [property: JsonPropertyName("results")]
  D1RawQueryResultSet Results,

  [property: JsonPropertyName("success")]
  bool Success
);

/// <summary>Result set from a raw query containing columns and rows.</summary>
/// <param name="Columns">Array of column names in SELECT order.</param>
/// <param name="Rows">Array of row arrays, each containing values in column order.</param>
public record D1RawQueryResultSet(
  [property: JsonPropertyName("columns")]
  IReadOnlyList<string> Columns,

  [property: JsonPropertyName("rows")]
  IReadOnlyList<IReadOnlyList<JsonElement>> Rows
);

#endregion
