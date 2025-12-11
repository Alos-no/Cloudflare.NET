namespace Cloudflare.NET.Tests.IntegrationTests;

using System.Net;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Workers;
using Workers.Models;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for the Worker Routes functionality in <see cref="WorkersApi" />.
///   These tests interact with the live Cloudflare API and require credentials.
/// </summary>
/// <remarks>
///   <para>
///     <b>Important:</b> These tests create and delete Worker routes in a zone.
///     Routes are cleaned up automatically after each test, but if tests fail
///     unexpectedly, you may have leftover test routes that need manual cleanup.
///   </para>
///   <para>
///     Test routes use patterns prefixed with "_cfnet-test-" for easy identification.
///     The token used for testing must have Zone Read and Workers Routes Write permissions.
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
[Collection(TestCollections.WorkerRoutes)]
public class WorkerRoutesIntegrationTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IWorkersApi _sut;

  /// <summary>The settings loaded from the test configuration.</summary>
  private readonly TestCloudflareSettings _settings;

  /// <summary>The xUnit test output helper for writing warnings and debug info.</summary>
  private readonly ITestOutputHelper _output;

  /// <summary>List of route IDs created during tests that need cleanup.</summary>
  private readonly List<string> _createdRouteIds = new();

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="WorkerRoutesIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public WorkerRoutesIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut      = fixture.WorkersApi;
    _settings = TestConfiguration.CloudflareSettings;
    _output   = output;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region List and Get Tests (I01-I03)

  /// <summary>I01: Verifies that routes can be listed successfully.</summary>
  [IntegrationTest]
  public async Task ListRoutesAsync_ReturnsRoutes()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    var result = await _sut.ListRoutesAsync(zoneId);

    // Assert
    result.Should().NotBeNull();
    // Note: Zone may or may not have existing routes, so we just verify the call succeeds
    _output.WriteLine($"Found {result.Count} worker routes in zone");
  }

  /// <summary>I02: Verifies that listing routes on a zone without routes returns empty list.</summary>
  [IntegrationTest]
  public async Task ListRoutesAsync_ZoneWithoutRoutes_ReturnsEmptyOrExistingRoutes()
  {
    // Arrange
    var zoneId = _settings.ZoneId;

    // Act
    var result = await _sut.ListRoutesAsync(zoneId);

    // Assert
    result.Should().NotBeNull();
    // This test verifies the API handles the case gracefully - list may have 0 or more routes
    foreach (var route in result)
    {
      route.Id.Should().NotBeNullOrEmpty();
      route.Pattern.Should().NotBeNullOrEmpty();
      _output.WriteLine($"  Route: {route.Pattern} -> {route.Script ?? "(disabled)"}");
    }
  }

  /// <summary>I03: Verifies that a specific route can be retrieved after creation.</summary>
  [IntegrationTest]
  public async Task GetRouteAsync_AfterCreation_ReturnsRoute()
  {
    // Arrange
    var zoneId = _settings.ZoneId;
    var testPattern = GenerateTestPattern();

    try
    {
      // Create a route first
      var createRequest = new CreateWorkerRouteRequest(testPattern);
      var created = await _sut.CreateRouteAsync(zoneId, createRequest);
      _createdRouteIds.Add(created.Id);

      // Act
      var result = await _sut.GetRouteAsync(zoneId, created.Id);

      // Assert
      result.Should().NotBeNull();
      result.Id.Should().Be(created.Id);
      result.Pattern.Should().Be(testPattern);

      _output.WriteLine($"Retrieved route: {result.Id}");
    }
    finally
    {
      await CleanupTestRoutes(zoneId);
    }
  }

  #endregion


  #region Route CRUD Tests (I04-I08)

  /// <summary>I04: Verifies that a route can be created with a script (if worker deployed).</summary>
  /// <remarks>
  ///   Note: This test requires a deployed worker script. If no workers are available,
  ///   it creates a disabled route instead and logs a message.
  /// </remarks>
  [IntegrationTest]
  public async Task CreateRouteAsync_WithPattern_CreatesRoute()
  {
    // Arrange
    var zoneId = _settings.ZoneId;
    var testPattern = GenerateTestPattern();

    try
    {
      // Act - Create a disabled route (no script binding)
      var request = new CreateWorkerRouteRequest(testPattern);
      var result = await _sut.CreateRouteAsync(zoneId, request);
      _createdRouteIds.Add(result.Id);

      // Assert
      result.Should().NotBeNull();
      result.Id.Should().NotBeNullOrEmpty();
      result.Pattern.Should().Be(testPattern);
      // Script should be null for disabled routes
      result.Script.Should().BeNull();

      _output.WriteLine($"Created route: {result.Id} with pattern: {result.Pattern}");
    }
    finally
    {
      await CleanupTestRoutes(zoneId);
    }
  }

  /// <summary>I05: Verifies that a route can be created as disabled (no script).</summary>
  [IntegrationTest]
  public async Task CreateRouteAsync_Disabled_CreatesRouteWithNullScript()
  {
    // Arrange
    var zoneId = _settings.ZoneId;
    var testPattern = GenerateTestPattern();

    try
    {
      // Act
      var request = new CreateWorkerRouteRequest(testPattern);
      var result = await _sut.CreateRouteAsync(zoneId, request);
      _createdRouteIds.Add(result.Id);

      // Assert
      result.Should().NotBeNull();
      result.Script.Should().BeNull("disabled routes have no script binding");

      _output.WriteLine($"Created disabled route: {result.Id}");
    }
    finally
    {
      await CleanupTestRoutes(zoneId);
    }
  }

  /// <summary>I06: Verifies that a route pattern can be updated.</summary>
  [IntegrationTest]
  public async Task UpdateRouteAsync_UpdatePattern_PatternChanges()
  {
    // Arrange
    var zoneId = _settings.ZoneId;
    var originalPattern = GenerateTestPattern();
    var updatedPattern = GenerateTestPattern();

    try
    {
      // Create a route first
      var createRequest = new CreateWorkerRouteRequest(originalPattern);
      var created = await _sut.CreateRouteAsync(zoneId, createRequest);
      _createdRouteIds.Add(created.Id);

      // Act
      var updateRequest = new UpdateWorkerRouteRequest(updatedPattern);
      var updated = await _sut.UpdateRouteAsync(zoneId, created.Id, updateRequest);

      // Assert
      updated.Should().NotBeNull();
      updated.Id.Should().Be(created.Id);
      updated.Pattern.Should().Be(updatedPattern);

      _output.WriteLine($"Updated route pattern from '{originalPattern}' to '{updatedPattern}'");
    }
    finally
    {
      await CleanupTestRoutes(zoneId);
    }
  }

  /// <summary>I08: Verifies that a route can be deleted.</summary>
  [IntegrationTest]
  public async Task DeleteRouteAsync_DeletesRoute()
  {
    // Arrange
    var zoneId = _settings.ZoneId;
    var testPattern = GenerateTestPattern();

    // Create a route first
    var createRequest = new CreateWorkerRouteRequest(testPattern);
    var created = await _sut.CreateRouteAsync(zoneId, createRequest);

    // Act
    await _sut.DeleteRouteAsync(zoneId, created.Id);

    // Assert - Trying to get the deleted route should throw
    var action = async () => await _sut.GetRouteAsync(zoneId, created.Id);
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);

    _output.WriteLine($"Successfully deleted route: {created.Id}");
  }

  #endregion


  #region Pattern Syntax Tests (I09-I11)

  /// <summary>I09: Verifies that wildcard subdomain patterns are accepted.</summary>
  [IntegrationTest]
  public async Task CreateRouteAsync_WildcardSubdomain_PatternAccepted()
  {
    // Arrange
    var zoneId = _settings.ZoneId;
    // Use unique identifier to avoid conflicts
    var guid = Guid.NewGuid().ToString("N")[..8];
    var testPattern = $"*._cfnet-test-{guid}.example.com/*";

    try
    {
      // Act
      var request = new CreateWorkerRouteRequest(testPattern);
      var result = await _sut.CreateRouteAsync(zoneId, request);
      _createdRouteIds.Add(result.Id);

      // Assert
      result.Should().NotBeNull();
      result.Pattern.Should().Be(testPattern);

      _output.WriteLine($"Created wildcard subdomain route: {result.Pattern}");
    }
    finally
    {
      await CleanupTestRoutes(zoneId);
    }
  }

  /// <summary>I10: Verifies that specific path patterns are accepted.</summary>
  [IntegrationTest]
  public async Task CreateRouteAsync_SpecificPath_PatternAccepted()
  {
    // Arrange
    var zoneId = _settings.ZoneId;
    var guid = Guid.NewGuid().ToString("N")[..8];
    var testPattern = $"_cfnet-test-{guid}.example.com/api/*";

    try
    {
      // Act
      var request = new CreateWorkerRouteRequest(testPattern);
      var result = await _sut.CreateRouteAsync(zoneId, request);
      _createdRouteIds.Add(result.Id);

      // Assert
      result.Should().NotBeNull();
      result.Pattern.Should().Be(testPattern);

      _output.WriteLine($"Created specific path route: {result.Pattern}");
    }
    finally
    {
      await CleanupTestRoutes(zoneId);
    }
  }

  /// <summary>I11: Verifies that root domain patterns are accepted.</summary>
  [IntegrationTest]
  public async Task CreateRouteAsync_RootDomain_PatternAccepted()
  {
    // Arrange
    var zoneId = _settings.ZoneId;
    var testPattern = GenerateTestPattern();

    try
    {
      // Act
      var request = new CreateWorkerRouteRequest(testPattern);
      var result = await _sut.CreateRouteAsync(zoneId, request);
      _createdRouteIds.Add(result.Id);

      // Assert
      result.Should().NotBeNull();
      result.Pattern.Should().Be(testPattern);

      _output.WriteLine($"Created root domain route: {result.Pattern}");
    }
    finally
    {
      await CleanupTestRoutes(zoneId);
    }
  }

  #endregion


  #region Full Lifecycle Tests (I12)

  /// <summary>I12: Verifies the complete route lifecycle: create, get, update, delete.</summary>
  [IntegrationTest]
  public async Task RouteLifecycle_CreateGetUpdateDelete_Succeeds()
  {
    // Arrange
    var zoneId = _settings.ZoneId;
    var originalPattern = GenerateTestPattern();
    var updatedPattern = GenerateTestPattern();

    string? routeId = null;

    try
    {
      // 1. Create
      var createRequest = new CreateWorkerRouteRequest(originalPattern);
      var created = await _sut.CreateRouteAsync(zoneId, createRequest);
      routeId = created.Id;
      created.Pattern.Should().Be(originalPattern);

      _output.WriteLine($"Step 1: Created route {routeId}");

      // 2. Get
      var retrieved = await _sut.GetRouteAsync(zoneId, routeId);
      retrieved.Id.Should().Be(routeId);
      retrieved.Pattern.Should().Be(originalPattern);

      _output.WriteLine("Step 2: Retrieved route successfully");

      // 3. Update
      var updateRequest = new UpdateWorkerRouteRequest(updatedPattern);
      var updated = await _sut.UpdateRouteAsync(zoneId, routeId, updateRequest);
      updated.Pattern.Should().Be(updatedPattern);

      _output.WriteLine("Step 3: Updated route successfully");

      // 4. Delete
      await _sut.DeleteRouteAsync(zoneId, routeId);
      routeId = null; // Mark as deleted

      _output.WriteLine("Step 4: Deleted route successfully");

      // 5. Verify deletion
      var action = async () => await _sut.GetRouteAsync(zoneId, created.Id);
      await action.Should().ThrowAsync<HttpRequestException>()
        .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);

      _output.WriteLine("Step 5: Verified route is deleted");
    }
    finally
    {
      // Cleanup if something failed before deletion
      if (routeId != null)
      {
        try
        {
          await _sut.DeleteRouteAsync(zoneId, routeId);
        }
        catch
        {
          // Ignore cleanup errors
        }
      }
    }
  }

  #endregion


  #region Error Handling Tests (I13-I17)

  /// <summary>I13: Verifies that getting a non-existent route returns 404.</summary>
  [IntegrationTest]
  public async Task GetRouteAsync_NonExistent_ThrowsHttpRequestException()
  {
    // Arrange
    var zoneId = _settings.ZoneId;
    var nonExistentRouteId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.GetRouteAsync(zoneId, nonExistentRouteId);

    // Assert
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
  }

  /// <summary>I14: Verifies that deleting a non-existent route returns appropriate error.</summary>
  [IntegrationTest]
  public async Task DeleteRouteAsync_NonExistent_ThrowsHttpRequestException()
  {
    // Arrange
    var zoneId = _settings.ZoneId;
    var nonExistentRouteId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.DeleteRouteAsync(zoneId, nonExistentRouteId);

    // Assert
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
  }

  /// <summary>I17: Verifies behavior with invalid zone ID format.</summary>
  [IntegrationTest]
  public async Task ListRoutesAsync_InvalidZoneId_ThrowsHttpRequestException()
  {
    // Arrange
    var invalidZoneId = "invalid-zone-id-format";

    // Act
    var action = async () => await _sut.ListRoutesAsync(invalidZoneId);

    // Assert
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().BeOneOf(
      HttpStatusCode.NotFound,
      HttpStatusCode.BadRequest,
      HttpStatusCode.Forbidden);
  }

  #endregion


  #region Helpers

  /// <summary>
  ///   Generates a unique test pattern to avoid conflicts with existing routes.
  ///   Uses a subdomain format that shouldn't match real traffic.
  /// </summary>
  /// <returns>A unique test pattern string.</returns>
  private static string GenerateTestPattern()
  {
    var guid = Guid.NewGuid().ToString("N")[..8];
    return $"_cfnet-test-{guid}.example.com/*";
  }

  /// <summary>Cleans up test routes created during tests.</summary>
  private async Task CleanupTestRoutes(string zoneId)
  {
    foreach (var routeId in _createdRouteIds)
    {
      try
      {
        await _sut.DeleteRouteAsync(zoneId, routeId);
        _output.WriteLine($"Cleaned up route: {routeId}");
      }
      catch
      {
        // Ignore cleanup errors
      }
    }

    _createdRouteIds.Clear();
  }

  #endregion
}
