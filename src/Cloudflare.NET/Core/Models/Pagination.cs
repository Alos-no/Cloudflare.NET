namespace Cloudflare.NET.Core.Models;

using System.Text.Json.Serialization;

/// <summary>Contains pagination information from a Cloudflare API list response that uses page numbers.</summary>
/// <param name="Page">The current page number.</param>
/// <param name="PerPage">The number of items per page.</param>
/// <param name="Count">The number of items in the current page.</param>
/// <param name="TotalCount">The total number of items across all pages.</param>
/// <param name="TotalPages">The total number of pages.</param>
/// <param name="Cursor">
///   Optional cursor for the next page. Some Cloudflare APIs (e.g., R2 Buckets) return cursor-based pagination
///   information in the standard <c>result_info</c> field rather than a separate <c>cursor_result_info</c> field.
/// </param>
public sealed record ResultInfo(
  [property: JsonPropertyName("page")]
  int Page,
  [property: JsonPropertyName("per_page")]
  int PerPage,
  [property: JsonPropertyName("count")]
  int Count,
  [property: JsonPropertyName("total_count")]
  int TotalCount,
  [property: JsonPropertyName("total_pages")]
  int TotalPages,
  [property: JsonPropertyName("cursor")]
  string? Cursor = null
);

/// <summary>Contains pagination information from a Cloudflare API list response that uses cursors.</summary>
/// <param name="Count">The number of items in the current page.</param>
/// <param name="PerPage">The number of items per page.</param>
/// <param name="Cursor">A token that can be used to fetch the next page of results.</param>
public sealed record CursorResultInfo(
  [property: JsonPropertyName("count")]
  int Count,
  [property: JsonPropertyName("per_page")]
  int PerPage,
  [property: JsonPropertyName("cursor")]
  string? Cursor = null
);

/// <summary>Represents the result of a single page from a page-based paginated API call.</summary>
/// <typeparam name="T">The type of the items in the result set.</typeparam>
/// <param name="Items">The items on the current page.</param>
/// <param name="PageInfo">The pagination metadata for the current page.</param>
public sealed record PagePaginatedResult<T>(IReadOnlyList<T> Items, ResultInfo? PageInfo);

/// <summary>Represents the result of a single page from a cursor-based paginated API call.</summary>
/// <typeparam name="T">The type of the items in the result set.</typeparam>
/// <param name="Items">The items on the current page.</param>
/// <param name="CursorInfo">The pagination metadata, including the cursor for the next page.</param>
public sealed record CursorPaginatedResult<T>(IReadOnlyList<T> Items, CursorResultInfo? CursorInfo);
