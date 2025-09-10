namespace Cloudflare.NET.Zones.Firewall;

using Core;
using Core.Models;
using Microsoft.Extensions.Logging;
using Security.Firewall.Models;

/// <summary>Implements the API for managing User-Agent blocking rules.</summary>
public class ZoneUaRulesApi(HttpClient httpClient, ILoggerFactory loggerFactory)
  : ApiResource(httpClient, loggerFactory.CreateLogger<ZoneUaRulesApi>()), IZoneUaRulesApi
{
  #region Methods Impl

  /// <inheritdoc />
  public async Task<PagePaginatedResult<UaRule>> ListAsync(string              zoneId,
                                                           ListUaRulesFilters? filters           = null,
                                                           CancellationToken   cancellationToken = default)
  {
    var queryParams = new List<string>();

    if (filters?.Page is not null)
      queryParams.Add($"page={filters.Page}");
    if (filters?.PerPage is not null)
      queryParams.Add($"per_page={filters.PerPage}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;

    var endpoint = $"zones/{zoneId}/firewall/ua_rules{queryString}";
    return await GetPagePaginatedResultAsync<UaRule>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<UaRule> ListAllAsync(string zoneId, int? perPage = null, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/firewall/ua_rules";
    return GetPaginatedAsync<UaRule>(endpoint, perPage, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<UaRule> GetAsync(string zoneId, string ruleId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/firewall/ua_rules/{ruleId}";
    return await GetAsync<UaRule>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<UaRule> CreateAsync(string zoneId, CreateUaRuleRequest request, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/firewall/ua_rules";
    return await PostAsync<UaRule>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<UaRule> UpdateAsync(string              zoneId,
                                        string              ruleId,
                                        UpdateUaRuleRequest request,
                                        CancellationToken   cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/firewall/ua_rules/{ruleId}";
    return await PutAsync<UaRule>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<string> DeleteAsync(string zoneId, string ruleId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/firewall/ua_rules/{ruleId}";
    var result   = await DeleteAsync<IdResponse>(endpoint, cancellationToken);
    return result.Id;
  }

  #endregion

  private record IdResponse(string Id);
}
