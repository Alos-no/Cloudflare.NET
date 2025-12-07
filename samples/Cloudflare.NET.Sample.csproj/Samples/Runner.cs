namespace Cloudflare.NET.Sample.Samples;

using Microsoft.Extensions.Logging;

/// <summary>Helper to run sample scenarios with consistent logging and resource cleanup.</summary>
public static class Runner
{
  #region Methods

  /// <summary>Executes a sample scenario, managing a list of cleanup actions.</summary>
  /// <param name="logger">The logger to use for the scenario.</param>
  /// <param name="scenarioName">The name of the scenario for logging.</param>
  /// <param name="scenarioAction">An async func that contains the sample logic and returns a list of cleanup funcs.</param>
  public static async Task RunAsync(
    ILogger                      logger,
    string                       scenarioName,
    Func<Task<List<Func<Task>>>> scenarioAction)
  {
    logger.LogInformation("--- Running Scenario: {ScenarioName} ---", scenarioName);

    var cleanupActions = new List<Func<Task>>();

    try
    {
      cleanupActions.AddRange(await scenarioAction());
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "An error occurred during the '{ScenarioName}' scenario.", scenarioName);
    }
    finally
    {
      if (cleanupActions.Any())
      {
        logger.LogInformation("--- Starting Cleanup for: {ScenarioName} ---", scenarioName);
        foreach (var cleanup in cleanupActions.AsEnumerable().Reverse())
          try
          {
            await cleanup();
          }
          catch (Exception ex)
          {
            logger.LogWarning(ex, "A cleanup action failed.");
          }

        logger.LogInformation("--- Cleanup Complete for: {ScenarioName} ---", scenarioName);
      }
      else
      {
        logger.LogInformation("--- Scenario Complete (No Cleanup): {ScenarioName} ---", scenarioName);
      }
    }
  }

  #endregion
}
