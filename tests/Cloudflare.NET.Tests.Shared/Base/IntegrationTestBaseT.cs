namespace Cloudflare.NET.Tests.Shared.Base;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
///   Generic base class for integration tests targeting a specific API.
///   Provides automatic client creation and resource management.
/// </summary>
/// <typeparam name="TApi">The API interface being tested.</typeparam>
public abstract class IntegrationTestBase<TApi> : IntegrationTestBase
  where TApi : class
{
  #region Properties & Fields - Non-Public

  /// <summary>The Cloudflare API client.</summary>
  protected ICloudflareApiClient Client { get; private set; } = default!;

  /// <summary>The specific API being tested.</summary>
  protected TApi Api { get; private set; } = default!;

  /// <summary>Logger for test output.</summary>
  protected ILogger Logger { get; private set; } = default!;

  /// <summary>The service provider for dependency injection.</summary>
  private ServiceProvider? _serviceProvider;

  #endregion


  #region IAsyncLifetime

  /// <summary>
  ///   Initializes the test by building the service provider and creating the API client.
  /// </summary>
  /// <returns>A task representing the initialization.</returns>
  public override async Task InitializeAsync()
  {
    // Build service provider with real API client
    var services = new ServiceCollection();

    ConfigureServices(services);

    _serviceProvider = services.BuildServiceProvider();

    Client = _serviceProvider.GetRequiredService<ICloudflareApiClient>();
    Logger = _serviceProvider.GetRequiredService<ILogger<IntegrationTestBase<TApi>>>();

    // Get the specific API - implementer must override GetApi
    Api = GetApi(Client);

    await base.InitializeAsync();
    await SetupTestResourcesAsync();
  }

  /// <summary>
  ///   Disposes the test by cleaning up resources and the service provider.
  /// </summary>
  /// <returns>A task representing the disposal.</returns>
  public new async Task DisposeAsync()
  {
    await base.DisposeAsync();

    if (_serviceProvider != null)
    {
      await _serviceProvider.DisposeAsync();
    }
  }

  #endregion


  #region Virtual/Abstract Members

  /// <summary>
  ///   Gets the specific API from the client. Override to return the correct API.
  /// </summary>
  /// <param name="client">The Cloudflare API client.</param>
  /// <returns>The specific API interface.</returns>
  protected abstract TApi GetApi(ICloudflareApiClient client);

  /// <summary>
  ///   Override to set up test-specific resources after client initialization.
  /// </summary>
  /// <returns>A task representing the setup.</returns>
  protected virtual Task SetupTestResourcesAsync() => Task.CompletedTask;

  /// <summary>
  ///   Override to customize service configuration.
  /// </summary>
  /// <param name="services">The service collection to configure.</param>
  protected virtual void ConfigureServices(IServiceCollection services)
  {
    services.AddLogging(builder =>
    {
      builder.SetMinimumLevel(LogLevel.Debug);
      builder.AddConsole();
    });

    services.AddCloudflareApiClient(options =>
    {
      options.ApiToken = Settings.ApiToken;
      options.AccountId = Settings.AccountId;
    });
  }

  #endregion


  #region Helper Methods

  /// <summary>
  ///   Gets a service from the dependency injection container.
  /// </summary>
  /// <typeparam name="T">The service type.</typeparam>
  /// <returns>The resolved service.</returns>
  protected T GetService<T>() where T : class
  {
    if (_serviceProvider == null)
    {
      throw new InvalidOperationException(
        "Service provider not initialized. Ensure InitializeAsync has been called.");
    }

    return _serviceProvider.GetRequiredService<T>();
  }

  /// <summary>
  ///   Gets an optional service from the dependency injection container.
  /// </summary>
  /// <typeparam name="T">The service type.</typeparam>
  /// <returns>The resolved service, or null if not registered.</returns>
  protected T? GetOptionalService<T>() where T : class
  {
    return _serviceProvider?.GetService<T>();
  }

  #endregion
}
