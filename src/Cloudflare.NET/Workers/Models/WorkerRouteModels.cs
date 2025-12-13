namespace Cloudflare.NET.Workers.Models;

using System.Text.Json.Serialization;

/// <summary>
///   Represents a Worker route that maps URL patterns to Worker scripts.
///   <para>
///     Routes determine which requests are handled by which Workers based
///     on URL pattern matching.
///   </para>
/// </summary>
/// <param name="Id">Unique route identifier.</param>
/// <param name="Pattern">URL pattern this route matches. Supports wildcards: <c>*</c> matches any characters.</param>
/// <param name="Script">Name of the Worker script to execute for matching requests. Can be null to disable Worker for matching routes.</param>
/// <example>
///   Pattern examples:
///   <list type="bullet">
///     <item><c>example.com/*</c> - All paths on domain</item>
///     <item><c>*.example.com/api/*</c> - API paths on subdomains</item>
///     <item><c>example.com/static/*</c> - Static asset paths</item>
///   </list>
/// </example>
public record WorkerRoute(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("pattern")]
  string Pattern,

  [property: JsonPropertyName("script")]
  string? Script = null
);


/// <summary>
///   Request to create a new Worker route.
/// </summary>
/// <param name="Pattern">URL pattern for the route.</param>
/// <param name="Script">Worker script name to bind. Use null to disable Workers for this pattern.</param>
public record CreateWorkerRouteRequest(
  [property: JsonPropertyName("pattern")]
  string Pattern,

  [property: JsonPropertyName("script")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Script = null
);


/// <summary>
///   Request to update an existing Worker route.
/// </summary>
/// <param name="Pattern">Updated URL pattern.</param>
/// <param name="Script">Updated Worker script name.</param>
public record UpdateWorkerRouteRequest(
  [property: JsonPropertyName("pattern")]
  string Pattern,

  [property: JsonPropertyName("script")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Script = null
);
