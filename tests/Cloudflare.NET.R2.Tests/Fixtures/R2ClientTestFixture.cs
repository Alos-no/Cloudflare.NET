namespace Cloudflare.NET.R2.Tests.Fixtures;

using Accounts;
using Amazon.S3;
using Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NET.Tests.Shared.Fixtures;

/// <summary>
///   An xUnit class fixture that sets up a dependency injection container with the Cloudflare.NET.R2 SDK
///   registered. This provides a configured IR2Client instance for use in integration tests.
/// </summary>
public class R2ClientTestFixture : IDisposable
{
  #region Properties & Fields - Non-Public

  private readonly IServiceProvider _serviceProvider;

  #endregion

  #region Constructors

  public R2ClientTestFixture()
  {
    var builder = Host.CreateApplicationBuilder();

    // The provider is added as a singleton so it can be resolved and updated by test classes.
    var xunitProvider = new XunitTestOutputLoggerProvider();
    builder.Services.AddSingleton(xunitProvider);

    // Ensure logs are surfaced during test runs.
    builder.Logging.ClearProviders();
    builder.Logging.AddProvider(xunitProvider);
    builder.Logging.AddSimpleConsole(o =>
    {
      o.SingleLine      = true;
      o.IncludeScopes   = true;
      o.TimestampFormat = "HH:mm:ss.fff zzz ";
    });
    builder.Logging.SetMinimumLevel(LogLevel.Trace);
    builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
    builder.Logging.AddFilter("System", LogLevel.Warning);

    // Register core services first so R2 can resolve its dependencies
    builder.Services.AddCloudflareApiClient(TestConfiguration.Configuration);

    // Use the SDK's own extension method to register the R2 client.
    builder.Services.AddCloudflareR2Client(TestConfiguration.Configuration);

    var host = builder.Build();
    _serviceProvider = host.Services;
  }

  public void Dispose()
  {
    if (_serviceProvider is IHost host)
      host.Dispose();
    else if (_serviceProvider is IDisposable disposable)
      disposable.Dispose();
    GC.SuppressFinalize(this);
  }

  #endregion

  #region Properties & Fields - Public

  /// <summary>Gets the underlying service provider for resolving services in tests.</summary>
  public IServiceProvider ServiceProvider => _serviceProvider;

  public IR2Client R2Client => _serviceProvider.GetRequiredService<IR2Client>();

  /// <summary>Gets the fully configured Accounts API client from the DI container.</summary>
  public IAccountsApi AccountsApi => _serviceProvider.GetRequiredService<ICloudflareApiClient>().Accounts;

  /// <summary>Gets the underlying AWS S3 client configured for R2.</summary>
  public IAmazonS3 S3Client => _serviceProvider.GetRequiredService<IAmazonS3>();

  #endregion
}
