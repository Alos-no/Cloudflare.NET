namespace Cloudflare.NET.Tests.IntegrationTests;

/// <summary>
///   Defines test collections for integration tests that share mutable resources and must run sequentially. Tests
///   in the same collection run one at a time; tests in different collections can run in parallel.
/// </summary>
/// <remarks>
///   <para>
///     xUnit runs tests from different test classes in parallel by default. When multiple tests operate on the same
///     Cloudflare resources (e.g., custom hostnames in a zone, rulesets for a phase), they can interfere with each other,
///     causing flaky test failures.
///   </para>
///   <para>
///     By grouping related tests into collections, we ensure sequential execution for tests that share mutable state
///     while allowing unrelated tests to run in parallel for faster overall execution.
///   </para>
/// </remarks>
public static class TestCollections
{
  #region Constants & Statics

  /// <summary>
  ///   Collection for tests that create, modify, or delete custom hostnames in a shared zone. These tests must run
  ///   sequentially to avoid race conditions during pagination and cleanup.
  /// </summary>
  public const string CustomHostnames = "CustomHostnames";

  /// <summary>
  ///   Collection for tests that modify zone rulesets (WAF custom rules, managed rules, rate limiting). These tests
  ///   share the same phase entrypoints and must run sequentially to avoid version conflicts.
  /// </summary>
  public const string ZoneRulesets = "ZoneRulesets";

  /// <summary>
  ///   Collection for tests that create access rules using reserved IP addresses. These tests must run sequentially
  ///   because Cloudflare rejects duplicate rules for the same IP.
  /// </summary>
  public const string AccessRules = "AccessRules";

  /// <summary>
  ///   Collection for tests that modify the authenticated user's profile. These tests must run sequentially
  ///   to ensure profile state is properly managed and restored.
  /// </summary>
  public const string UserProfile = "UserProfile";

  /// <summary>
  ///   Collection for tests that create, modify, or delete DNS records in a shared zone. These tests must run
  ///   sequentially to avoid race conditions during batch operations and cleanup.
  /// </summary>
  public const string DnsOperations = "DnsOperations";

  /// <summary>
  ///   Collection for tests that trigger DNS record scanning and manage scan review queue.
  ///   These tests must run sequentially because scanning is async and affects zone state.
  /// </summary>
  public const string DnsScan = "DnsScan";

  /// <summary>
  ///   Collection for tests that create, modify, or delete API tokens. These tests must run
  ///   sequentially to avoid race conditions during token lifecycle operations.
  /// </summary>
  public const string ApiTokens = "ApiTokens";

  /// <summary>
  ///   Collection for tests that interact with user invitations. These tests must run
  ///   sequentially because responding to invitations is a one-time operation.
  /// </summary>
  public const string UserInvitations = "UserInvitations";

  /// <summary>
  ///   Collection for tests that interact with user memberships. These tests must run
  ///   sequentially because membership operations (accept/reject/delete) can affect state.
  /// </summary>
  public const string UserMemberships = "UserMemberships";

  /// <summary>
  ///   Collection for tests that create, modify, or delete Worker routes in a zone.
  ///   These tests must run sequentially to avoid race conditions during route operations.
  /// </summary>
  public const string WorkerRoutes = "WorkerRoutes";

  /// <summary>
  ///   Collection for tests that create, modify, or delete Turnstile widgets in an account.
  ///   These tests must run sequentially to avoid race conditions during widget operations.
  /// </summary>
  public const string TurnstileWidgets = "TurnstileWidgets";

  /// <summary>
  ///   Collection for tests that interact with user subscriptions.
  ///   These tests must run sequentially to avoid race conditions during subscription operations.
  /// </summary>
  public const string UserSubscriptions = "UserSubscriptions";

  /// <summary>
  ///   Collection for tests that interact with zone subscriptions.
  ///   These tests must run sequentially to avoid race conditions during subscription operations.
  /// </summary>
  public const string ZoneSubscriptions = "ZoneSubscriptions";

  #endregion
}

/// <summary>Marker class for the CustomHostnames test collection.</summary>
[CollectionDefinition(TestCollections.CustomHostnames)]
public class CustomHostnamesCollection;

/// <summary>Marker class for the ZoneRulesets test collection.</summary>
[CollectionDefinition(TestCollections.ZoneRulesets)]
public class ZoneRulesetsCollection;

/// <summary>Marker class for the AccessRules test collection.</summary>
[CollectionDefinition(TestCollections.AccessRules)]
public class AccessRulesCollection;

/// <summary>Marker class for the UserProfile test collection.</summary>
[CollectionDefinition(TestCollections.UserProfile)]
public class UserProfileCollection;

/// <summary>Marker class for the DnsOperations test collection.</summary>
[CollectionDefinition(TestCollections.DnsOperations)]
public class DnsOperationsCollection;

/// <summary>Marker class for the DnsScan test collection.</summary>
[CollectionDefinition(TestCollections.DnsScan)]
public class DnsScanCollection;

/// <summary>Marker class for the ApiTokens test collection.</summary>
[CollectionDefinition(TestCollections.ApiTokens)]
public class ApiTokensCollection;

/// <summary>Marker class for the UserInvitations test collection.</summary>
[CollectionDefinition(TestCollections.UserInvitations)]
public class UserInvitationsCollection;

/// <summary>Marker class for the UserMemberships test collection.</summary>
[CollectionDefinition(TestCollections.UserMemberships)]
public class UserMembershipsCollection;

/// <summary>Marker class for the WorkerRoutes test collection.</summary>
[CollectionDefinition(TestCollections.WorkerRoutes)]
public class WorkerRoutesCollection;

/// <summary>Marker class for the TurnstileWidgets test collection.</summary>
[CollectionDefinition(TestCollections.TurnstileWidgets)]
public class TurnstileWidgetsCollection;

/// <summary>Marker class for the UserSubscriptions test collection.</summary>
[CollectionDefinition(TestCollections.UserSubscriptions)]
public class UserSubscriptionsCollection;

/// <summary>Marker class for the ZoneSubscriptions test collection.</summary>
[CollectionDefinition(TestCollections.ZoneSubscriptions)]
public class ZoneSubscriptionsCollection;
