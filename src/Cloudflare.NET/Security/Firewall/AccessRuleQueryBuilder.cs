namespace Cloudflare.NET.Security.Firewall;

using Core.Internal;
using Models;

/// <summary>
///   A helper class responsible for building the query string for listing IP Access Rules
///   based on a set of filters.
/// </summary>
internal static class AccessRuleQueryBuilder
{
  #region Methods

  /// <summary>Builds a URL query string from the provided filter object.</summary>
  /// <param name="filters">The filters to apply.</param>
  /// <returns>A URL-encoded query string, or an empty string if no filters are provided.</returns>
  internal static string Build(ListAccessRulesFilters? filters)
  {
    if (filters is null)
      return string.Empty;

    var queryParams = new List<string>();

    if (!string.IsNullOrWhiteSpace(filters.Notes))
      queryParams.Add($"notes={Uri.EscapeDataString(filters.Notes)}");

    if (filters.Mode.HasValue)
      queryParams.Add($"mode={EnumHelper.GetEnumMemberValue(filters.Mode.Value)}");

    if (filters.Match.HasValue)
      queryParams.Add($"match={EnumHelper.GetEnumMemberValue(filters.Match.Value)}");

    if (filters.ConfigurationTarget.HasValue)
      queryParams.Add($"configuration.target={EnumHelper.GetEnumMemberValue(filters.ConfigurationTarget.Value)}");

    if (!string.IsNullOrWhiteSpace(filters.ConfigurationValue))
      queryParams.Add($"configuration.value={Uri.EscapeDataString(filters.ConfigurationValue)}");

    if (filters.Page.HasValue)
      queryParams.Add($"page={filters.Page.Value}");

    if (filters.PerPage.HasValue)
      queryParams.Add($"per_page={filters.PerPage.Value}");

    if (filters.Order.HasValue)
      queryParams.Add($"order={EnumHelper.GetEnumMemberValue(filters.Order.Value)}");

    if (filters.Direction.HasValue)
      queryParams.Add($"direction={EnumHelper.GetEnumMemberValue(filters.Direction.Value)}");

    return queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
  }

  #endregion
}
