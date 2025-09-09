namespace Cloudflare.NET.Security.Firewall.Models;

/// <summary>Defines the filtering and pagination options for listing IP Access Rules.</summary>
/// <param name="Notes">A case-insensitive substring to match against the rule's notes.</param>
/// <param name="Mode">The exact mode to filter by.</param>
/// <param name="Match">Whether to match all or any of the provided filters. Default is 'all'.</param>
/// <param name="ConfigurationTarget">The target to filter by (e.g., ip, country).</param>
/// <param name="ConfigurationValue">The value for the target to filter by.</param>
/// <param name="Page">The page number of the result set.</param>
/// <param name="PerPage">The number of rules per page.</param>
/// <param name="Order">The field to order the results by.</param>
/// <param name="Direction">The direction to sort the results.</param>
public record ListAccessRulesFilters(
  string?               Notes               = null,
  AccessRuleMode?       Mode                = null,
  FilterMatch?          Match               = null,
  AccessRuleTarget?     ConfigurationTarget = null,
  string?               ConfigurationValue  = null,
  int?                  Page                = null,
  int?                  PerPage             = null,
  AccessRuleOrderField? Order               = null,
  ListOrderDirection?   Direction           = null
);
