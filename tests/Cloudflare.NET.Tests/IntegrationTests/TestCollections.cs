namespace Cloudflare.NET.Tests.IntegrationTests;

/// <summary>
///   Defines test collections for integration tests that share mutable resources and must run sequentially.
///   Tests in the same collection run one at a time; tests in different collections can run in parallel.
/// </summary>
/// <remarks>
///   <para>
///     xUnit runs tests from different test classes in parallel by default. When multiple tests operate on
///     the same Cloudflare resources (e.g., custom hostnames in a zone, rulesets for a phase), they can
///     interfere with each other, causing flaky test failures.
///   </para>
///   <para>
///     By grouping related tests into collections, we ensure sequential execution for tests that share
///     mutable state while allowing unrelated tests to run in parallel for faster overall execution.
///   </para>
/// </remarks>
public static class TestCollections
{
  /// <summary>
  ///   Collection for tests that create, modify, or delete custom hostnames in a shared zone.
  ///   These tests must run sequentially to avoid race conditions during pagination and cleanup.
  /// </summary>
  public const string CustomHostnames = "CustomHostnames";

  /// <summary>
  ///   Collection for tests that modify zone rulesets (WAF custom rules, managed rules, rate limiting).
  ///   These tests share the same phase entrypoints and must run sequentially to avoid version conflicts.
  /// </summary>
  public const string ZoneRulesets = "ZoneRulesets";
}


/// <summary>Marker class for the CustomHostnames test collection.</summary>
[CollectionDefinition(TestCollections.CustomHostnames)]
public class CustomHostnamesCollection;

/// <summary>Marker class for the ZoneRulesets test collection.</summary>
[CollectionDefinition(TestCollections.ZoneRulesets)]
public class ZoneRulesetsCollection;
