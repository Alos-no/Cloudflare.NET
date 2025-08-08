namespace Cloudflare.NET.Analytics.Tests.Fixtures;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
///   An xUnit class fixture that sets up a dependency injection container with the full
///   Cloudflare.NET SDK (Core and Analytics) registered. This provides a configured IAnalyticsApi
///   instance for use in integration tests.
/// </summary>
public class AnalyticsApiTestFixture : IDisposable
{
  #region Properties & Fields - Non-Public

  /// <summary>The configured service provider.</summary>
  private readonly IServiceProvider _serviceProvider;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="AnalyticsApiTestFixture" /> class.</summary>
  public AnalyticsApiTestFixture()
  {
    // Create a host builder to configure the DI container.
    var builder = Host.CreateApplicationBuilder();

    // Use the SDK's own extension methods to register the API clients.
    // This ensures we are testing the actual DI configuration of the library.
    // The configuration (including secrets) is loaded from TestConfiguration.
    builder.Services.AddCloudflareApiClient(TestConfiguration.Configuration);
    builder.Services.AddCloudflareAnalytics();

    // Build the service provider.
    var host = builder.Build();
    _serviceProvider = host.Services;
  }

  /// <summary>Disposes of the underlying service provider.</summary>
  public void Dispose()
  {
    // The host manages the lifetime of the service provider, so disposing the host is correct.
    if (_serviceProvider is IDisposable disposable)
      disposable.Dispose();
    GC.SuppressFinalize(this);
  }

  #endregion

  #region Properties & Fields - Public

  /// <summary>Gets the fully configured Analytics API client from the DI container.</summary>
  public IAnalyticsApi AnalyticsApi => _serviceProvider.GetRequiredService<IAnalyticsApi>();

  #endregion
}
