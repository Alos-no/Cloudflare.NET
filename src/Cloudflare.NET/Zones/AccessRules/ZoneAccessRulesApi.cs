namespace Cloudflare.NET.Zones.AccessRules;

using Core;
using Security.Firewall;
using Security.Firewall.Models;

/// <summary>Implements the API for managing IP Access Rules at the zone level.</summary>
public class ZoneAccessRulesApi(HttpClient httpClient)
  : ApiResource(httpClient), IZoneAccessRulesApi
{
  #region Methods Impl

  /// <inheritdoc />
  public async Task<IReadOnlyList<AccessRule>> ListAsync(string                  zoneId,
                                                         ListAccessRulesFilters? filters           = null,
                                                         CancellationToken       cancellationToken = default)
  {
    var queryString = AccessRuleQueryBuilder.Build(filters);
    var endpoint    = $"zones/{zoneId}/firewall/access_rules/rules{queryString}";
    return await GetAsync<IReadOnlyList<AccessRule>>(endpoint, cancellationToken);
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
