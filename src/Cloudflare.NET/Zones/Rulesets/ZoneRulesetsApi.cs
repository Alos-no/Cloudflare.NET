namespace Cloudflare.NET.Zones.Rulesets;

using Core;
using Security.Rulesets.Models;

/// <summary>Implements the API for managing Rulesets at the zone level.</summary>
public class ZoneRulesetsApi(HttpClient httpClient)
  : ApiResource(httpClient), IZoneRulesetsApi
{
  #region Methods Impl

  /// <inheritdoc />
  public async Task<IReadOnlyList<Ruleset>> ListAsync(string zoneId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/rulesets";
    return await GetAsync<IReadOnlyList<Ruleset>>(endpoint, cancellationToken);
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
  public async Task<Ruleset> UpdatePhaseEntrypointAsync(string            zoneId,
                                                        string            phase,
                                                        IEnumerable<Rule> rules,
                                                        CancellationToken cancellationToken = default)
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
