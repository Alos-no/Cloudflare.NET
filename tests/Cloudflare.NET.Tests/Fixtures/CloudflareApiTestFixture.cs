namespace Cloudflare.NET.Tests.Fixtures;

using Accounts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Fixtures;
using Zones;

/// <summary>
///   An xUnit class fixture that sets up a dependency injection container with the
///   Cloudflare.NET SDK registered. This provides configured API client instances for use in
///   integration tests.
/// </summary>
public class CloudflareApiTestFixture : IAsyncLifetime
{
  #region Properties & Fields - Non-Public

  /// <summary>The configured service provider.</summary>
  private readonly IServiceProvider _serviceProvider;

  /// <summary>The root API client resolved from the DI container.</summary>
  private readonly ICloudflareApiClient _apiClient;

  /// <summary>The test-specific settings, including the inferred BaseDomain.</summary>
  private readonly TestCloudflareSettings _settings;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="CloudflareApiTestFixture" /> class.</summary>
  public CloudflareApiTestFixture()
  {
    // Create a host builder to configure the DI container.
    var builder = Host.CreateApplicationBuilder();

    // Use the SDK's own extension method to register the API client.
    // This ensures we are testing the actual DI configuration of the library.
    // The configuration (including secrets) is loaded from TestConfiguration.
    builder.Services.AddCloudflareApiClient(TestConfiguration.Configuration);

    // Build the service provider.
    var host = builder.Build();
    _serviceProvider = host.Services;

    // Resolve the main client once.
    _apiClient = _serviceProvider.GetRequiredService<ICloudflareApiClient>();
    _settings = TestConfiguration.CloudflareSettings;
  }

  /// <summary>Disposes of the underlying service provider.</summary>
  public void Dispose()
  {
    // The host manages the lifetime of the service provider, so disposing the host is correct.
    if (_serviceProvider is IHost host)
      host.Dispose();
    else if (_serviceProvider is IDisposable disposable)
      disposable.Dispose();
    GC.SuppressFinalize(this);
  }

  #endregion

  #region Properties & Fields - Public

  /// <summary>Gets the fully configured Accounts API client from the DI container.</summary>
  public IAccountsApi AccountsApi => _apiClient.Accounts;

  /// <summary>Gets the fully configured Zones API client from the DI container.</summary>
  public IZonesApi ZonesApi => _apiClient.Zones;

  #endregion

  #region Methods Impl

  /// <summary>
  ///   Initializes the fixture by fetching the Zone details from the API to infer the
  ///   BaseDomain.
  /// </summary>
  public async Task InitializeAsync()
  {
    if (TestConfigurationValidator.IsSecretMissing(_settings.ZoneId) ||
        TestConfigurationValidator.IsSecretMissing(_settings.ApiToken))
      // Cannot infer the domain if the ZoneId or Token is missing. The test will be skipped
      // by the [IntegrationTest] attribute, but we avoid throwing an exception here.
      return;

    var zoneDetails = await ZonesApi.GetZoneDetailsAsync(_settings.ZoneId);
    _settings.BaseDomain = zoneDetails.Name;
  }

  /// <summary>Performs asynchronous cleanup.</summary>
  public Task DisposeAsync() => Task.CompletedTask;

  #endregion
}