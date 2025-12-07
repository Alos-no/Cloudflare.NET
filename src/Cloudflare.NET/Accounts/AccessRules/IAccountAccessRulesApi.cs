namespace Cloudflare.NET.Accounts.AccessRules;

using Core.Models;
using Security.Firewall.Models;

/// <summary>
///   <para>Defines the contract for managing IP Access Rules at the account level.</para>
///   <para>
///     Account-level rules apply to all zones within the account that do not have their own zone-level rules
///     overriding them.
///   </para>
/// </summary>
public interface IAccountAccessRulesApi
{
  /// <summary>Lists all IP Access Rules for the account, allowing for manual pagination control.</summary>
  /// <remarks>
  ///   This method is intended for developers who need to control the pagination process manually. Use the properties
  ///   of the returned <see cref="PagePaginatedResult{T}" /> to determine if there are more pages and to construct the
  ///   filter for the next call.
  /// </remarks>
  /// <param name="filters">
  ///   Optional filters to apply to the list operation, such as filtering by mode or IP, and specifying
  ///   the page number.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A single page of access rules along with pagination information.</returns>
  Task<PagePaginatedResult<AccessRule>> ListAsync(ListAccessRulesFilters? filters = null, CancellationToken cancellationToken = default);

  /// <summary>Lists all IP Access Rules for the account, automatically handling pagination.</summary>
  /// <param name="filters">
  ///   Optional filters to apply to the list operation. Pagination parameters (Page, PerPage) will be
  ///   ignored.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An asynchronous stream of all access rules matching the criteria.</returns>
  IAsyncEnumerable<AccessRule> ListAllAsync(ListAccessRulesFilters? filters = null, CancellationToken cancellationToken = default);

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
  /// <param name="request">The request containing the fields to update (e.g., changing the notes or mode).</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated access rule.</returns>
  Task<AccessRule> UpdateAsync(string ruleId, UpdateAccessRuleRequest request, CancellationToken cancellationToken = default);

  /// <summary>Deletes an IP Access Rule from the account.</summary>
  /// <param name="ruleId">The ID of the rule to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  Task DeleteAsync(string ruleId, CancellationToken cancellationToken = default);
}
