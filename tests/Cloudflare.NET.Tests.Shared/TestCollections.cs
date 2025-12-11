namespace Cloudflare.NET.Tests.Shared;

using Xunit;

/// <summary>
///   Defines test collection names for controlling test parallelization.
///   Tests in the same collection run sequentially; different collections run in parallel.
/// </summary>
public static class TestCollections
{
  /// <summary>Zone CRUD tests that must run sequentially.</summary>
  public const string ZoneCrud = "Zone CRUD";

  /// <summary>DNS record tests that must run sequentially within a zone.</summary>
  public const string DnsRecords = "DNS Records";

  /// <summary>Zone holds tests that must run sequentially.</summary>
  public const string ZoneHolds = "Zone Holds";

  /// <summary>Zone settings tests that must run sequentially.</summary>
  public const string ZoneSettings = "Zone Settings";

  /// <summary>Account management tests that must run sequentially.</summary>
  public const string AccountManagement = "Account Management";

  /// <summary>Account member tests that must run sequentially.</summary>
  public const string AccountMembers = "Account Members";

  /// <summary>Account roles tests that must run sequentially.</summary>
  public const string AccountRoles = "Account Roles";

  /// <summary>API token tests that must run sequentially.</summary>
  public const string ApiTokens = "API Tokens";

  /// <summary>Audit log tests that must run sequentially.</summary>
  public const string AuditLogs = "Audit Logs";

  /// <summary>User management tests that must run sequentially.</summary>
  public const string UserManagement = "User Management";

  /// <summary>User memberships tests that must run sequentially.</summary>
  public const string UserMemberships = "User Memberships";

  /// <summary>User invitations tests that must run sequentially.</summary>
  public const string UserInvitations = "User Invitations";

  /// <summary>Turnstile widget tests that must run sequentially.</summary>
  public const string TurnstileWidgets = "Turnstile Widgets";

  /// <summary>Worker route tests that must run sequentially.</summary>
  public const string WorkerRoutes = "Worker Routes";

  /// <summary>Subscription tests that must run sequentially.</summary>
  public const string Subscriptions = "Subscriptions";
}


#region Collection Definitions

/// <summary>Zone CRUD test collection definition.</summary>
[CollectionDefinition(TestCollections.ZoneCrud, DisableParallelization = true)]
public class ZoneCrudCollection;

/// <summary>DNS Records test collection definition.</summary>
[CollectionDefinition(TestCollections.DnsRecords, DisableParallelization = true)]
public class DnsRecordsCollection;

/// <summary>Zone Holds test collection definition.</summary>
[CollectionDefinition(TestCollections.ZoneHolds, DisableParallelization = true)]
public class ZoneHoldsCollection;

/// <summary>Zone Settings test collection definition.</summary>
[CollectionDefinition(TestCollections.ZoneSettings, DisableParallelization = true)]
public class ZoneSettingsCollection;

/// <summary>Account Management test collection definition.</summary>
[CollectionDefinition(TestCollections.AccountManagement, DisableParallelization = true)]
public class AccountManagementCollection;

/// <summary>Account Members test collection definition.</summary>
[CollectionDefinition(TestCollections.AccountMembers, DisableParallelization = true)]
public class AccountMembersCollection;

/// <summary>Account Roles test collection definition.</summary>
[CollectionDefinition(TestCollections.AccountRoles, DisableParallelization = true)]
public class AccountRolesCollection;

/// <summary>API Tokens test collection definition.</summary>
[CollectionDefinition(TestCollections.ApiTokens, DisableParallelization = true)]
public class ApiTokensCollection;

/// <summary>Audit Logs test collection definition.</summary>
[CollectionDefinition(TestCollections.AuditLogs, DisableParallelization = true)]
public class AuditLogsCollection;

/// <summary>User Management test collection definition.</summary>
[CollectionDefinition(TestCollections.UserManagement, DisableParallelization = true)]
public class UserManagementCollection;

/// <summary>User Memberships test collection definition.</summary>
[CollectionDefinition(TestCollections.UserMemberships, DisableParallelization = true)]
public class UserMembershipsCollection;

/// <summary>User Invitations test collection definition.</summary>
[CollectionDefinition(TestCollections.UserInvitations, DisableParallelization = true)]
public class UserInvitationsCollection;

/// <summary>Turnstile Widgets test collection definition.</summary>
[CollectionDefinition(TestCollections.TurnstileWidgets, DisableParallelization = true)]
public class TurnstileWidgetsCollection;

/// <summary>Worker Routes test collection definition.</summary>
[CollectionDefinition(TestCollections.WorkerRoutes, DisableParallelization = true)]
public class WorkerRoutesCollection;

/// <summary>Subscriptions test collection definition.</summary>
[CollectionDefinition(TestCollections.Subscriptions, DisableParallelization = true)]
public class SubscriptionsCollection;

#endregion
