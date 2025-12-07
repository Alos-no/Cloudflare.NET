namespace Cloudflare.NET.Accounts.AccessRules;

using Core;
using Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Security.Firewall;
using Security.Firewall.Models;

/// <summary>Implements the API for managing IP Access Rules at the account level.</summary>
public class AccountAccessRulesApi(
  HttpClient                     httpClient,
  IOptions<CloudflareApiOptions> options,
  ILoggerFactory                 loggerFactory)
  : ApiResource(httpClient, loggerFactory.CreateLogger<AccountAccessRulesApi>()), IAccountAccessRulesApi
{
  #region Properties & Fields - Non-Public

  private readonly string _accountId = options.Value.AccountId;

  #endregion

  #region Methods Impl

  /// <inheritdoc />
  public async Task<PagePaginatedResult<AccessRule>> ListAsync(ListAccessRulesFilters? filters           = null,
                                                               CancellationToken       cancellationToken = default)
  {
    var queryString = AccessRuleQueryBuilder.Build(filters);
    var endpoint    = $"accounts/{_accountId}/firewall/access_rules/rules{queryString}";

    return await GetPagePaginatedResultAsync<AccessRule>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<AccessRule> ListAllAsync(ListAccessRulesFilters? filters           = null,
                                                   CancellationToken       cancellationToken = default)
  {
    // Create a new filter object for the query builder, ensuring pagination parameters are not included.
    var listFilters = filters is not null
      ? filters with { Page = null, PerPage = null }
      : null;

    var queryString = AccessRuleQueryBuilder.Build(listFilters);
    var endpoint    = $"accounts/{_accountId}/firewall/access_rules/rules{queryString}";

    // Use the base class helper to handle the pagination loop, passing the original per_page value.
    return GetPaginatedAsync<AccessRule>(endpoint, filters?.PerPage, cancellationToken);
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
