namespace Cloudflare.NET.Accounts.D1.Models;

using System.Text.Json.Serialization;
using Cloudflare.NET.Accounts.Models;

#region Database Types

/// <summary>Represents a D1 database.</summary>
/// <param name="Uuid">The unique identifier for the database (UUID format).</param>
/// <param name="Name">The name of the database.</param>
/// <param name="CreatedAt">Timestamp when the database was created (ISO 8601).</param>
/// <param name="FileSize">The database size in bytes.</param>
/// <param name="NumTables">Number of tables in the database.</param>
/// <param name="Version">Database version: "production" or "alpha".</param>
/// <param name="ReadReplication">Configuration for D1 read replication.</param>
/// <param name="RunningInRegion">Region code where the database is actually running (e.g., "WEUR", "ENAM"). Undocumented field present in API responses.</param>
public record D1Database(
  [property: JsonPropertyName("uuid")]
  string Uuid,

  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("created_at")]
  DateTimeOffset? CreatedAt = null,

  [property: JsonPropertyName("file_size")]
  long? FileSize = null,

  [property: JsonPropertyName("num_tables")]
  int? NumTables = null,

  [property: JsonPropertyName("version")]
  string? Version = null,

  [property: JsonPropertyName("read_replication")]
  D1ReadReplication? ReadReplication = null,

  [property: JsonPropertyName("running_in_region")]
  string? RunningInRegion = null
);

/// <summary>Read replication configuration for a D1 database.</summary>
/// <param name="Mode">The replication mode: "auto" to enable, "disabled" to disable.</param>
public record D1ReadReplication(
  [property: JsonPropertyName("mode")]
  string Mode
);

/// <summary>Filters for listing D1 databases.</summary>
/// <param name="Name">Filter by database name (partial match).</param>
/// <param name="Page">Page number of results to return (1-based).</param>
/// <param name="PerPage">Number of results per page.</param>
public record ListD1DatabasesFilters(
  string? Name = null,
  int? Page = null,
  int? PerPage = null
);

#endregion


#region Create/Update Types

/// <summary>Options for creating a D1 database.</summary>
/// <param name="Name">The name for the new database (required).</param>
/// <param name="PrimaryLocationHint">
///   Optional region hint for database placement.
///   Use R2LocationHint constants (e.g., R2LocationHint.WestNorthAmerica).
///   Ignored if Jurisdiction is specified.
/// </param>
/// <param name="Jurisdiction">
///   Optional jurisdictional restriction guaranteeing data residency.
///   Overrides PrimaryLocationHint when specified.
/// </param>
public record CreateD1DatabaseOptions(
  string Name,
  R2LocationHint? PrimaryLocationHint = null,
  D1Jurisdiction? Jurisdiction = null
);

/// <summary>Options for updating a D1 database.</summary>
/// <param name="ReadReplication">Configuration for read replication.</param>
public record UpdateD1DatabaseOptions(
  D1ReadReplication? ReadReplication = null
);

#endregion


#region Internal Request Types

/// <summary>Internal request body for creating a D1 database.</summary>
/// <param name="Name">The name for the new database.</param>
/// <param name="PrimaryLocationHint">Optional region hint for database placement.</param>
/// <param name="Jurisdiction">Optional jurisdictional restriction.</param>
internal record CreateD1DatabaseRequest(
  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("primary_location_hint")]
  string? PrimaryLocationHint = null,

  [property: JsonPropertyName("jurisdiction")]
  string? Jurisdiction = null
);

/// <summary>Internal request body for updating a D1 database.</summary>
/// <param name="ReadReplication">Configuration for read replication.</param>
internal record UpdateD1DatabaseRequest(
  [property: JsonPropertyName("read_replication")]
  D1ReadReplication? ReadReplication = null
);

#endregion
