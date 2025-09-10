namespace Cloudflare.NET.Zones.Rulesets;

using Core;
using Core.Models;
using Microsoft.Extensions.Logging;
using Security.Rulesets.Models;

/// <summary>Implements the API for managing Rulesets at the zone level.</summary>
public class ZoneRulesetsApi(HttpClient httpClient, ILoggerFactory loggerFactory)
  : ApiResource(httpClient, loggerFactory.CreateLogger<ZoneRulesetsApi>()), IZoneRulesetsApi
{
  #region Methods Impl

  /// <inheritdoc />
  public async Task<CursorPaginatedResult<Ruleset>> ListAsync(string               zoneId,
                                                              ListRulesetsFilters? filters           = null,
                                                              CancellationToken    cancellationToken = default)
  {
    var queryParams = new List<string>();

    if (filters?.PerPage is not null)
      queryParams.Add($"per_page={filters.PerPage}");
    if (!string.IsNullOrEmpty(filters?.Cursor))
      queryParams.Add($"cursor={filters.Cursor}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;

    var endpoint = $"zones/{zoneId}/rulesets{queryString}";
    return await GetCursorPaginatedResultAsync<Ruleset>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<PagePaginatedResult<Ruleset>> ListPhaseEntrypointVersionsAsync(
    string                      zoneId,
    string                      phase,
    ListRulesetVersionsFilters? filters           = null,
    CancellationToken           cancellationToken = default)
  {
    var queryParams = new List<string>();

    if (filters?.Page is not null)
      queryParams.Add($"page={filters.Page}");
    if (filters?.PerPage is not null)
      queryParams.Add($"per_page={filters.PerPage}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;

    var endpoint = $"zones/{zoneId}/rulesets/phases/{phase}/entrypoint/versions{queryString}";
    return await GetPagePaginatedResultAsync<Ruleset>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> GetPhaseEntrypointVersionAsync(string            zoneId,
                                                            string            phase,
                                                            string            version,
                                                            CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/rulesets/phases/{phase}/entrypoint/versions/{version}";
    return await GetAsync<Ruleset>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<Ruleset> ListAllAsync(string zoneId, int? perPage = null, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/rulesets";
    return GetCursorPaginatedAsync<Ruleset>(endpoint, perPage, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> GetAsync(string zoneId, string rulesetId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/rulesets/{rulesetId}";
    return await GetAsync<Ruleset>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> CreateAsync(string               zoneId,
                                         CreateRulesetRequest request,
                                         CancellationToken    cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/rulesets";
    return await PostAsync<Ruleset>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> UpdateAsync(string               zoneId,
                                         string               rulesetId,
                                         UpdateRulesetRequest request,
                                         CancellationToken    cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/rulesets/{rulesetId}";
    return await PutAsync<Ruleset>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteAsync(string zoneId, string rulesetId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/rulesets/{rulesetId}";
    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> GetPhaseEntrypointAsync(string zoneId, string phase, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/rulesets/phases/{phase}/entrypoint";
    return await GetAsync<Ruleset>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> UpdatePhaseEntrypointAsync(string                         zoneId,
                                                        string                         phase,
                                                        IEnumerable<CreateRuleRequest> rules,
                                                        CancellationToken              cancellationToken = default)
  {
    var payload  = new { rules };
    var endpoint = $"zones/{zoneId}/rulesets/phases/{phase}/entrypoint";
    return await PutAsync<Ruleset>(endpoint, payload, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> AddRuleAsync(string            zoneId,
                                          string            rulesetId,
                                          CreateRuleRequest rule,
                                          CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/rulesets/{rulesetId}/rules";
    return await PostAsync<Ruleset>(endpoint, rule, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> UpdateRuleAsync(string            zoneId,
                                             string            rulesetId,
                                             string            ruleId,
                                             object            rule,
                                             CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/rulesets/{rulesetId}/rules/{ruleId}";
    return await PatchAsync<Ruleset>(endpoint, rule, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> DeleteRuleAsync(string zoneId, string rulesetId, string ruleId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/rulesets/{rulesetId}/rules/{ruleId}";
    return await DeleteAsync<Ruleset>(endpoint, cancellationToken);
  }

  #endregion
}
