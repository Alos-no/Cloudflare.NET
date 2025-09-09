namespace Cloudflare.NET.Accounts.Rulesets;

using Core;
using Microsoft.Extensions.Options;
using Security.Rulesets.Models;

/// <summary>Implements the API for managing Rulesets at the account level.</summary>
public class AccountRulesetsApi(HttpClient httpClient, IOptions<CloudflareApiOptions> options)
  : ApiResource(httpClient), IAccountRulesetsApi
{
  #region Properties & Fields - Non-Public

  private readonly string _accountId = options.Value.AccountId;

  #endregion

  #region Methods Impl

  /// <inheritdoc />
  public async Task<IReadOnlyList<Ruleset>> ListAsync(CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/rulesets";
    return await GetAsync<IReadOnlyList<Ruleset>>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> GetAsync(string rulesetId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/rulesets/{rulesetId}";
    return await GetAsync<Ruleset>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> CreateAsync(CreateRulesetRequest request, CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/rulesets";
    return await PostAsync<Ruleset>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> UpdateAsync(string               rulesetId,
                                         UpdateRulesetRequest request,
                                         CancellationToken    cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/rulesets/{rulesetId}";
    return await PutAsync<Ruleset>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteAsync(string rulesetId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/rulesets/{rulesetId}";
    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> GetPhaseEntrypointAsync(string phase, CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/rulesets/phases/{phase}/entrypoint";
    return await GetAsync<Ruleset>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> UpdatePhaseEntrypointAsync(string            phase,
                                                        IEnumerable<Rule> rules,
                                                        CancellationToken cancellationToken = default)
  {
    var payload  = new { rules };
    var endpoint = $"accounts/{_accountId}/rulesets/phases/{phase}/entrypoint";
    return await PutAsync<Ruleset>(endpoint, payload, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> AddRuleAsync(string rulesetId, CreateRuleRequest rule, CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/rulesets/{rulesetId}/rules";
    return await PostAsync<Ruleset>(endpoint, rule, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> UpdateRuleAsync(string rulesetId, string ruleId, object rule, CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/rulesets/{rulesetId}/rules/{ruleId}";
    return await PatchAsync<Ruleset>(endpoint, rule, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> DeleteRuleAsync(string rulesetId, string ruleId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/rulesets/{rulesetId}/rules/{ruleId}";
    return await DeleteAsync<Ruleset>(endpoint, cancellationToken);
  }

  #endregion
}
