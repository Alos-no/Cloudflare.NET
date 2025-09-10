namespace Cloudflare.NET.Security.Firewall.Models;

/// <summary>Defines the pagination options for listing User-Agent Blocking rules.</summary>
/// <param name="Page">The page number of the result set.</param>
/// <param name="PerPage">The number of rules per page.</param>
public record ListUaRulesFilters(
  int? Page    = null,
  int? PerPage = null
);
