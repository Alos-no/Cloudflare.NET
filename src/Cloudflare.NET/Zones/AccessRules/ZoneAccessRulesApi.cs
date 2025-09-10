namespace Cloudflare.NET.Zones.AccessRules;

using Core;
using Core.Models;
using Microsoft.Extensions.Logging;
using Security.Firewall;
using Security.Firewall.Models;

/// <summary>Implements the API for managing IP Access Rules at the zone level.</summary>
public class ZoneAccessRulesApi(HttpClient httpClient, ILoggerFactory loggerFactory)
  : ApiResource(httpClient, loggerFactory.CreateLogger<ZoneAccessRulesApi>()), IZoneAccessRulesApi
{
  #region Methods Impl

  /// <inheritdoc />
  public async Task<PagePaginatedResult<AccessRule>> ListAsync(string                  zoneId,
                                                               ListAccessRulesFilters? filters           = null,
                                                               CancellationToken       cancellationToken = default)
  {
    var queryString = AccessRuleQueryBuilder.Build(filters);
    var endpoint    = $"zones/{zoneId}/firewall/access_rules/rules{queryString}";
    return await GetPagePaginatedResultAsync<AccessRule>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<AccessRule> ListAllAsync(string                  zoneId,
                                                   ListAccessRulesFilters? filters           = null,
                                                   CancellationToken       cancellationToken = default)
  {
    // Create a new filter object for the query builder, ensuring pagination parameters are not included.
    var listFilters = filters is not null
      ? filters with { Page = null, PerPage = null }
      : null;

    var queryString = AccessRuleQueryBuilder.Build(listFilters);
    var endpoint    = $"zones/{zoneId}/firewall/access_rules/rules{queryString}";

    // Use the base class helper to handle the pagination loop, passing the original per_page value.
    return GetPaginatedAsync<AccessRule>(endpoint, filters?.PerPage, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<AccessRule> GetAsync(string zoneId, string ruleId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/firewall/access_rules/rules/{ruleId}";
    return await GetAsync<AccessRule>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<AccessRule> CreateAsync(string                  zoneId,
                                            CreateAccessRuleRequest request,
                                            CancellationToken       cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/firewall/access_rules/rules";
    return await PostAsync<AccessRule>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<AccessRule> UpdateAsync(string                  zoneId,
                                            string                  ruleId,
                                            UpdateAccessRuleRequest request,
                                            CancellationToken       cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/firewall/access_rules/rules/{ruleId}";
    return await PatchAsync<AccessRule>(endpoint, request, cancellationToken);
  }


  /// <inheritdoc />
  public async Task DeleteAsync(string zoneId, string ruleId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/firewall/access_rules/rules/{ruleId}";
    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  #endregion
}
