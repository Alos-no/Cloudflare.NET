namespace Cloudflare.NET.Accounts.Rulesets;

using Core;
using Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Security.Rulesets.Models;

/// <summary>Implements the API for managing Rulesets at the account level.</summary>
public class AccountRulesetsApi(
  HttpClient                     httpClient,
  IOptions<CloudflareApiOptions> options,
  ILoggerFactory                 loggerFactory)
  : ApiResource(httpClient, loggerFactory.CreateLogger<AccountRulesetsApi>()), IAccountRulesetsApi
{
  #region Properties & Fields - Non-Public

  private readonly string _accountId = options.Value.AccountId;

  #endregion

  #region Methods Impl

  /// <inheritdoc />
  public async Task<CursorPaginatedResult<Ruleset>> ListAsync(ListRulesetsFilters? filters           = null,
                                                              CancellationToken    cancellationToken = default)
  {
    var queryParams = new List<string>();

    if (filters?.PerPage is not null)
      queryParams.Add($"per_page={filters.PerPage}");
    if (!string.IsNullOrEmpty(filters?.Cursor))
      queryParams.Add($"cursor={filters.Cursor}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;

    var endpoint = $"accounts/{_accountId}/rulesets{queryString}";
    return await GetCursorPaginatedResultAsync<Ruleset>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<Ruleset> ListAllAsync(int? perPage = null, CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/rulesets";
    return GetCursorPaginatedAsync<Ruleset>(endpoint, perPage, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<PagePaginatedResult<Ruleset>> ListPhaseEntrypointVersionsAsync(
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

    var endpoint = $"accounts/{_accountId}/rulesets/phases/{phase}/entrypoint/versions{queryString}";
    return await GetPagePaginatedResultAsync<Ruleset>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Ruleset> GetPhaseEntrypointVersionAsync(string phase, string version, CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/rulesets/phases/{phase}/entrypoint/versions/{version}";
    return await GetAsync<Ruleset>(endpoint, cancellationToken);
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
  public async Task<Ruleset> UpdatePhaseEntrypointAsync(string                         phase,
                                                        IEnumerable<CreateRuleRequest> rules,
                                                        CancellationToken              cancellationToken = default)
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
