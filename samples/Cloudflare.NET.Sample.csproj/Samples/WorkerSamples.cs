namespace Cloudflare.NET.Sample.Samples;

using Microsoft.Extensions.Logging;
using Workers.Models;

/// <summary>
///   Demonstrates Workers API operations including:
///   <list type="bullet">
///     <item><description>F12: Worker Routes (list, get, create, update, delete routes)</description></item>
///   </list>
/// </summary>
/// <remarks>
///   <para>
///     Worker routes are zone-scoped resources that determine which requests
///     are handled by which Workers based on URL pattern matching.
///   </para>
///   <para>
///     Note: Creating routes requires an existing Worker script deployed to the account.
///   </para>
/// </remarks>
public class WorkerSamples(ICloudflareApiClient cf, ILogger<WorkerSamples> logger)
{
  #region Methods - Worker Routes (F12)

  /// <summary>
  ///   Demonstrates Worker Routes operations.
  ///   <para>
  ///     Worker routes map URL patterns to Worker scripts. A route can:
  ///     - Execute a specific Worker script (when Script is set)
  ///     - Bypass Workers entirely (when Script is null/empty)
  ///   </para>
  /// </summary>
  public async Task<List<Func<Task>>> RunWorkerRoutesSamplesAsync(string zoneId, string baseDomain)
  {
    var cleanupActions = new List<Func<Task>>();
    logger.LogInformation("=== F12: Worker Routes Operations ===");

    // 1. List all Worker routes in the zone.
    logger.LogInformation("--- Listing Worker Routes ---");
    var routes = await cf.Workers.ListRoutesAsync(zoneId);
    logger.LogInformation("Existing routes: {Count}", routes.Count);

    foreach (var route in routes)
    {
      logger.LogInformation("  Route: {Pattern}", route.Pattern);
      logger.LogInformation("    Id:     {Id}", route.Id);
      logger.LogInformation("    Script: {Script}", route.Script ?? "(disabled/passthrough)");
    }

    // 2. Create a Worker route (demonstration).
    // Note: This requires an existing Worker script to be deployed to the account.
    // For demonstration, we'll create a "bypass" route (no script).
    logger.LogInformation("--- Creating Worker Route ---");

    var uniquePattern = $"_cfnet-worker-sample-{Guid.NewGuid():N}.{baseDomain}/*";

    try
    {
      // Create a bypass route (no Worker script).
      // This is useful for excluding certain paths from Worker processing.
      var createRequest = new CreateWorkerRouteRequest(
        Pattern: uniquePattern,
        Script:  null  // null or empty = bypass Workers
      );
      var createdRoute = await cf.Workers.CreateRouteAsync(zoneId, createRequest);
      logger.LogInformation("Created route: {Id}", createdRoute.Id);
      logger.LogInformation("  Pattern: {Pattern}", createdRoute.Pattern);
      logger.LogInformation("  Script:  {Script}", createdRoute.Script ?? "(bypass)");

      // Add cleanup action.
      cleanupActions.Add(async () =>
      {
        logger.LogInformation("Deleting Worker route: {Id}", createdRoute.Id);
        await cf.Workers.DeleteRouteAsync(zoneId, createdRoute.Id);
        logger.LogInformation("Deleted Worker route: {Id}", createdRoute.Id);
      });

      // 3. Get the created route.
      logger.LogInformation("--- Getting Route Details ---");
      var routeDetails = await cf.Workers.GetRouteAsync(zoneId, createdRoute.Id);
      logger.LogInformation("Route Details:");
      logger.LogInformation("  Id:      {Id}", routeDetails.Id);
      logger.LogInformation("  Pattern: {Pattern}", routeDetails.Pattern);
      logger.LogInformation("  Script:  {Script}", routeDetails.Script ?? "(bypass)");

      // 4. Update the route (modify pattern).
      logger.LogInformation("--- Updating Worker Route ---");
      var updatedPattern = $"_cfnet-worker-sample-updated-{Guid.NewGuid():N}.{baseDomain}/*";
      var updateRequest = new UpdateWorkerRouteRequest(
        Pattern: updatedPattern,
        Script:  null  // Keep as bypass
      );
      var updatedRoute = await cf.Workers.UpdateRouteAsync(zoneId, createdRoute.Id, updateRequest);
      logger.LogInformation("Updated route: {Id}", updatedRoute.Id);
      logger.LogInformation("  New Pattern: {Pattern}", updatedRoute.Pattern);
    }
    catch (Exception ex)
    {
      logger.LogWarning("Worker route operation failed: {Message}", ex.Message);
      logger.LogInformation("Note: Creating routes requires valid patterns matching your zone.");
    }

    // 5. Route pattern examples.
    logger.LogInformation("--- Worker Route Pattern Examples ---");
    logger.LogInformation("Pattern syntax:");
    logger.LogInformation("  example.com/*           - Match all paths on example.com");
    logger.LogInformation("  *.example.com/*         - Match all subdomains and paths");
    logger.LogInformation("  example.com/api/*       - Match /api and all subpaths");
    logger.LogInformation("  example.com/path        - Match exact path only");
    logger.LogInformation("");
    logger.LogInformation("Route behavior:");
    logger.LogInformation("  - Routes are evaluated in order of specificity");
    logger.LogInformation("  - Most specific route wins");
    logger.LogInformation("  - Script: null = bypass Workers (request goes to origin)");
    logger.LogInformation("  - Script: 'worker-name' = execute the named Worker");

    return cleanupActions;
  }

  #endregion
}
