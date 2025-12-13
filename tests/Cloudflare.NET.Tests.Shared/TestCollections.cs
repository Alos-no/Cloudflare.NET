namespace Cloudflare.NET.Tests.Shared;

using Xunit;
using Xunit.Abstractions;


/// <summary>
///   Custom test collection orderer that ensures PermissionValidation tests run first.
///   <para>
///     This orderer prioritizes collections whose names start with '!' (such as "!PermissionValidation")
///     to ensure API token permissions are validated before any other integration tests run.
///     If permission validation fails, the remaining tests won't waste time on 403 errors.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     To use this orderer, add the following assembly-level attribute to the test project:
///     <code>[assembly: TestCollectionOrderer("Cloudflare.NET.Tests.Shared.IntegrationTestCollectionOrderer", "Cloudflare.NET.Tests.Shared")]</code>
///   </para>
///   <para>
///     Collections starting with '!' are treated as high-priority and sorted alphabetically among themselves.
///     All other collections are sorted alphabetically after the high-priority ones.
///   </para>
///   <para>
///     <b>Note:</b> Test collection definitions (the <c>[CollectionDefinition]</c> marker classes) must be defined
///     in the same assembly as the tests that use them. This orderer is intentionally placed in the Shared
///     assembly so it can be referenced by any test project via the assembly-level attribute.
///   </para>
/// </remarks>
public class IntegrationTestCollectionOrderer : ITestCollectionOrderer
{
  /// <summary>
  ///   Orders test collections with high-priority collections (starting with '!') first, then alphabetically.
  /// </summary>
  /// <param name="testCollections">The test collections to order.</param>
  /// <returns>The ordered test collections.</returns>
  public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
  {
    // Collections starting with '!' are high-priority and run first.
    // This ensures permission validation happens before other tests.
    return testCollections.OrderBy(collection =>
    {
      var displayName = collection.DisplayName ?? string.Empty;

      // High-priority collections (starting with '!') get priority 0, others get priority 1.
      var priority = displayName.StartsWith('!') ? 0 : 1;

      // Return tuple for ordering: first by priority, then alphabetically.
      return (priority, displayName);
    });
  }
}
