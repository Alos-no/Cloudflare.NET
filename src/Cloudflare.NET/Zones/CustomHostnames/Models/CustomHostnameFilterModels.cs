namespace Cloudflare.NET.Zones.CustomHostnames.Models;

using Security.Firewall.Models;

/// <summary>Defines the filtering and pagination options for listing custom hostnames.</summary>
/// <param name="Id">Filter by custom hostname ID.</param>
/// <param name="Hostname">Filter by hostname (partial match).</param>
/// <param name="Ssl">Filter by SSL certificate status.</param>
/// <param name="OrderBy">The field to order results by.</param>
/// <param name="Direction">The sort direction (ascending or descending).</param>
/// <param name="Page">The page number of results to retrieve.</param>
/// <param name="PerPage">The number of results per page (max 50).</param>
public record ListCustomHostnamesFilters(
  string?             Id        = null,
  string?             Hostname  = null,
  SslStatus?          Ssl       = null,
  string?             OrderBy   = null,
  ListOrderDirection? Direction = null,
  int?                Page      = null,
  int?                PerPage   = null
);
