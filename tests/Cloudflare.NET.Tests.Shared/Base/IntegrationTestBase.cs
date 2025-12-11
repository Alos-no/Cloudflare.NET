namespace Cloudflare.NET.Tests.Shared.Base;

using Xunit;

/// <summary>
///   Base class for integration tests with automatic resource cleanup.
///   Implements IAsyncLifetime for setup/teardown.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
  #region Properties & Fields - Non-Public

  /// <summary>Test configuration with credentials.</summary>
  protected TestCloudflareSettings Settings => TestConfiguration.CloudflareSettings;

  /// <summary>Account ID from configuration.</summary>
  protected string AccountId => Settings.AccountId;

  /// <summary>Zone ID from configuration.</summary>
  protected string ZoneId => Settings.ZoneId;

  /// <summary>Base domain from configuration.</summary>
  protected string BaseDomain => Settings.BaseDomain;

  /// <summary>Resources created during test that need cleanup.</summary>
  private readonly List<Func<Task>> _cleanupActions = [];

  #endregion


  #region IAsyncLifetime

  /// <summary>
  ///   Initializes the test. Override to set up test-specific resources.
  /// </summary>
  /// <returns>A task representing the initialization.</returns>
  public virtual Task InitializeAsync() => Task.CompletedTask;

  /// <summary>
  ///   Cleans up all registered resources in reverse order.
  /// </summary>
  /// <returns>A task representing the cleanup.</returns>
  public async Task DisposeAsync()
  {
    // Clean up in reverse order (LIFO)
    for (var i = _cleanupActions.Count - 1; i >= 0; i--)
    {
      try
      {
        await _cleanupActions[i]();
      }
      catch (Exception ex)
      {
        // Log but don't fail cleanup - other resources still need cleanup
        Console.WriteLine($"Cleanup failed: {ex.Message}");
      }
    }

    _cleanupActions.Clear();
  }

  #endregion


  #region Resource Management

  /// <summary>
  ///   Registers a cleanup action to be executed during teardown.
  /// </summary>
  /// <param name="cleanupAction">Async action to execute.</param>
  protected void RegisterCleanup(Func<Task> cleanupAction)
  {
    _cleanupActions.Add(cleanupAction);
  }

  /// <summary>
  ///   Registers a resource for cleanup using the provided delete function.
  /// </summary>
  /// <typeparam name="T">Resource ID type.</typeparam>
  /// <param name="resourceId">ID of the resource to delete.</param>
  /// <param name="deleteAction">Function to delete the resource.</param>
  protected void RegisterResourceForCleanup<T>(T resourceId, Func<T, Task> deleteAction)
  {
    RegisterCleanup(() => deleteAction(resourceId));
  }

  /// <summary>
  ///   Creates a resource and automatically registers it for cleanup.
  /// </summary>
  /// <typeparam name="TResource">Resource type.</typeparam>
  /// <param name="createAction">Function to create the resource.</param>
  /// <param name="getIdFunc">Function to extract the resource ID.</param>
  /// <param name="deleteAction">Function to delete the resource by ID.</param>
  /// <returns>The created resource.</returns>
  protected async Task<TResource> CreateWithCleanupAsync<TResource>(
    Func<Task<TResource>> createAction,
    Func<TResource, string> getIdFunc,
    Func<string, Task> deleteAction)
  {
    var resource = await createAction();
    var id = getIdFunc(resource);

    RegisterResourceForCleanup(id, deleteAction);

    return resource;
  }

  /// <summary>
  ///   Creates a resource with a composite key and registers for cleanup.
  /// </summary>
  /// <typeparam name="TResource">Resource type.</typeparam>
  /// <typeparam name="TKey">Key type (e.g., tuple of IDs).</typeparam>
  /// <param name="createAction">Function to create the resource.</param>
  /// <param name="getKeyFunc">Function to extract the composite key.</param>
  /// <param name="deleteAction">Function to delete the resource by key.</param>
  /// <returns>The created resource.</returns>
  protected async Task<TResource> CreateWithCleanupAsync<TResource, TKey>(
    Func<Task<TResource>> createAction,
    Func<TResource, TKey> getKeyFunc,
    Func<TKey, Task> deleteAction)
  {
    var resource = await createAction();
    var key = getKeyFunc(resource);

    RegisterCleanup(() => deleteAction(key));

    return resource;
  }

  #endregion


  #region Helpers

  /// <summary>
  ///   Generates a unique identifier for test resources.
  /// </summary>
  /// <returns>A unique test identifier.</returns>
  protected static string GenerateTestId() => $"test-{Guid.NewGuid().ToString("N")[..8]}";

  /// <summary>
  ///   Generates a unique test domain name.
  /// </summary>
  /// <returns>A unique DNS record name using the configured base domain.</returns>
  protected string GenerateTestRecordName() =>
    $"{GenerateTestId()}.{BaseDomain}";

  /// <summary>
  ///   Generates a unique email address for testing.
  /// </summary>
  /// <returns>A unique test email address.</returns>
  protected static string GenerateTestEmail() =>
    $"{GenerateTestId()}@example.com";

  /// <summary>
  ///   Generates a unique name for test resources.
  /// </summary>
  /// <param name="prefix">Prefix for the name.</param>
  /// <returns>A unique name with the given prefix.</returns>
  protected static string GenerateTestName(string prefix = "Test") =>
    $"{prefix}-{GenerateTestId()}";

  /// <summary>
  ///   Delays for a short period to allow API propagation.
  /// </summary>
  /// <param name="milliseconds">Delay in milliseconds (default: 1000).</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>A task representing the delay.</returns>
  protected static Task DelayForPropagationAsync(
    int milliseconds = 1000,
    CancellationToken cancellationToken = default) =>
    Task.Delay(milliseconds, cancellationToken);

  /// <summary>
  ///   Retries an action until it succeeds or times out.
  /// </summary>
  /// <typeparam name="T">Result type.</typeparam>
  /// <param name="action">Action to retry.</param>
  /// <param name="maxAttempts">Maximum number of attempts.</param>
  /// <param name="delayMs">Delay between attempts in milliseconds.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The result of the successful action.</returns>
  protected static async Task<T> RetryAsync<T>(
    Func<Task<T>> action,
    int maxAttempts = 3,
    int delayMs = 1000,
    CancellationToken cancellationToken = default)
  {
    Exception? lastException = null;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
      try
      {
        return await action();
      }
      catch (Exception ex)
      {
        lastException = ex;

        if (attempt < maxAttempts)
        {
          await Task.Delay(delayMs * attempt, cancellationToken);
        }
      }
    }

    throw new InvalidOperationException(
      $"Action failed after {maxAttempts} attempts",
      lastException);
  }

  #endregion
}
