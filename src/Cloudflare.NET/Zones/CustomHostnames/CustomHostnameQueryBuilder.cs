namespace Cloudflare.NET.Zones.CustomHostnames;

using Core.Internal;
using Models;

/// <summary>
///   A helper class responsible for building the query string for listing custom hostnames based on a set of
///   filters.
/// </summary>
internal static class CustomHostnameQueryBuilder
{
  #region Methods

  /// <summary>Builds a URL query string from the provided filter object.</summary>
  /// <param name="filters">The filters to apply.</param>
  /// <returns>A URL-encoded query string, or an empty string if no filters are provided.</returns>
  internal static string Build(ListCustomHostnamesFilters? filters)
  {
    if (filters is null)
      return string.Empty;

    var queryParams = new List<string>();

    // Filter by ID (exact match).
    if (!string.IsNullOrWhiteSpace(filters.Id))
      queryParams.Add($"id={Uri.EscapeDataString(filters.Id)}");

    // Filter by hostname (partial match).
    if (!string.IsNullOrWhiteSpace(filters.Hostname))
      queryParams.Add($"hostname={Uri.EscapeDataString(filters.Hostname)}");

    // Filter by SSL status.
    if (filters.Ssl.HasValue)
      queryParams.Add($"ssl={EnumHelper.GetEnumMemberValue(filters.Ssl.Value)}");

    // Sorting.
    if (!string.IsNullOrWhiteSpace(filters.OrderBy))
      queryParams.Add($"order={Uri.EscapeDataString(filters.OrderBy)}");

    if (filters.Direction.HasValue)
      queryParams.Add($"direction={EnumHelper.GetEnumMemberValue(filters.Direction.Value)}");

    // Pagination.
    if (filters.Page.HasValue)
      queryParams.Add($"page={filters.Page.Value}");

    if (filters.PerPage.HasValue)
      queryParams.Add($"per_page={filters.PerPage.Value}");

    return queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
  }

  #endregion
}
