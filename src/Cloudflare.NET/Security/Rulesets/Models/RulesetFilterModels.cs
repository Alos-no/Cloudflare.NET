namespace Cloudflare.NET.Security.Rulesets.Models;

/// <summary>Defines the pagination options for listing Rulesets.</summary>
/// <param name="PerPage">The number of rulesets to return per page.</param>
/// <param name="Cursor">The cursor for the next page of results.</param>
public record ListRulesetsFilters(
  int?    PerPage = null,
  string? Cursor  = null
);

/// <summary>Defines the pagination options for listing Ruleset versions.</summary>
/// <param name="Page">The page number of the result set.</param>
/// <param name="PerPage">The number of versions to return per page.</param>
public record ListRulesetVersionsFilters(
  int? Page    = null,
  int? PerPage = null
);
