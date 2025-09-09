namespace Cloudflare.NET.Accounts.AccessRules;

using Security.Firewall.Models;

/// <summary>Defines the contract for managing IP Access Rules at the account level.</summary>
public interface IAccountAccessRulesApi
{
  /// <summary>Lists all IP Access Rules for the account.</summary>
  /// <param name="filters">Optional filters to apply to the list operation.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A read-only list of access rules.</returns>
  Task<IReadOnlyList<AccessRule>> ListAsync(ListAccessRulesFilters? filters = null, CancellationToken cancellationToken = default);

  /// <summary>Gets a single IP Access Rule for the account by its ID.</summary>
  /// <param name="ruleId">The ID of the rule to retrieve.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The requested access rule.</returns>
  Task<AccessRule> GetAsync(string ruleId, CancellationToken cancellationToken = default);

  /// <summary>Creates a new IP Access Rule for the account.</summary>
  /// <param name="request">The request containing the details of the rule to create.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The created access rule.</returns>
  Task<AccessRule> CreateAsync(CreateAccessRuleRequest request, CancellationToken cancellationToken = default);

  /// <summary>Updates an existing IP Access Rule for the account.</summary>
  /// <param name="ruleId">The ID of the rule to update.</param>
  /// <param name="request">The request containing the fields to update.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated access rule.</returns>
  Task<AccessRule> UpdateAsync(string ruleId, UpdateAccessRuleRequest request, CancellationToken cancellationToken = default);

  /// <summary>Deletes an IP Access Rule from the account.</summary>
  /// <param name="ruleId">The ID of the rule to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  Task DeleteAsync(string ruleId, CancellationToken cancellationToken = default);
}
