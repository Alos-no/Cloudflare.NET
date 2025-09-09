namespace Cloudflare.NET.Zones.Firewall;

using Core;
using Security.Firewall.Models;

/// <summary>Implements the API for managing User-Agent blocking rules.</summary>
public class ZoneUaRulesApi(HttpClient httpClient)
  : ApiResource(httpClient), IZoneUaRulesApi
{
  #region Methods Impl

  /// <inheritdoc />
  public async Task<IReadOnlyList<UaRule>> ListAsync(string zoneId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/firewall/ua_rules";
    return await GetAsync<IReadOnlyList<UaRule>>(endpoint, cancellationToken);
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
