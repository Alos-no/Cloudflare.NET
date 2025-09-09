namespace Cloudflare.NET.Accounts.AccessRules;

using Core;
using Microsoft.Extensions.Options;
using Security.Firewall;
using Security.Firewall.Models;

/// <summary>Implements the API for managing IP Access Rules at the account level.</summary>
public class AccountAccessRulesApi(HttpClient httpClient, IOptions<CloudflareApiOptions> options)
  : ApiResource(httpClient), IAccountAccessRulesApi
{
  #region Properties & Fields - Non-Public

  private readonly string _accountId = options.Value.AccountId;

  #endregion

  #region Methods Impl

  /// <inheritdoc />
  public async Task<IReadOnlyList<AccessRule>> ListAsync(ListAccessRulesFilters? filters           = null,
                                                         CancellationToken       cancellationToken = default)
  {
    var queryString = AccessRuleQueryBuilder.Build(filters);
    var endpoint    = $"accounts/{_accountId}/firewall/access_rules/rules{queryString}";
    return await GetAsync<IReadOnlyList<AccessRule>>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<AccessRule> GetAsync(string ruleId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/firewall/access_rules/rules/{ruleId}";
    return await GetAsync<AccessRule>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<AccessRule> CreateAsync(CreateAccessRuleRequest request, CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/firewall/access_rules/rules";
    return await PostAsync<AccessRule>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<AccessRule> UpdateAsync(string                  ruleId,
                                            UpdateAccessRuleRequest request,
                                            CancellationToken       cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/firewall/access_rules/rules/{ruleId}";
    return await PatchAsync<AccessRule>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteAsync(string ruleId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/firewall/access_rules/rules/{ruleId}";
    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  #endregion
}
