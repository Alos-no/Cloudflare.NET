namespace Cloudflare.NET.Tests.Fixtures;

using Accounts;
using ApiTokens;
using AuditLogs;
using Cloudflare.NET.Tests.Shared;
using Dns;
using Members;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Roles;
using Shared.Fixtures;
using Subscriptions;
using Turnstile;
using User;
using Workers;
using Zones;

/// <summary>
///   An xUnit class fixture that sets up a dependency injection container with the Cloudflare.NET SDK registered.
///   This provides configured API client instances for use in integration tests.
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

    // The provider is added as a singleton so it can be resolved and updated by test classes.
    var xunitProvider = new XunitTestOutputLoggerProvider();
    builder.Services.AddSingleton(xunitProvider);

    // Always surface logs in tests: single-line, timestamps, and scopes for traceability.
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

    // Use the SDK's own extension method to register the API client.
    // This ensures we are testing the actual DI configuration of the library.
    // The configuration (including secrets) is loaded from TestConfiguration.
    builder.Services.AddCloudflareApiClient(TestConfiguration.Configuration);

    // Build the service provider.
    var host = builder.Build();
    _serviceProvider = host.Services;

    // Resolve the main client once.
    _apiClient = _serviceProvider.GetRequiredService<ICloudflareApiClient>();
    _settings  = TestConfiguration.CloudflareSettings;
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

  /// <summary>Gets the underlying service provider for resolving services in tests.</summary>
  public IServiceProvider ServiceProvider => _serviceProvider;

  /// <summary>Gets the fully configured Accounts API client from the DI container.</summary>
  public IAccountsApi AccountsApi => _apiClient.Accounts;

  /// <summary>Gets the fully configured User API client from the DI container.</summary>
  public IUserApi UserApi => _apiClient.User;

  /// <summary>Gets the fully configured Zones API client from the DI container.</summary>
  public IZonesApi ZonesApi => _apiClient.Zones;

  /// <summary>Gets the fully configured DNS API client from the DI container.</summary>
  public IDnsApi DnsApi => _apiClient.Dns;

  /// <summary>Gets the fully configured Audit Logs API client from the DI container.</summary>
  public IAuditLogsApi AuditLogsApi => _apiClient.AuditLogs;

  /// <summary>Gets the fully configured API Tokens API client from the DI container.</summary>
  public IApiTokensApi ApiTokensApi => _apiClient.ApiTokens;

  /// <summary>Gets the fully configured Roles API client from the DI container.</summary>
  public IRolesApi RolesApi => _apiClient.Roles;

  /// <summary>Gets the fully configured Members API client from the DI container.</summary>
  public IMembersApi MembersApi => _apiClient.Members;

  /// <summary>Gets the fully configured Subscriptions API client from the DI container.</summary>
  public ISubscriptionsApi SubscriptionsApi => _apiClient.Subscriptions;

  /// <summary>Gets the fully configured Workers API client from the DI container.</summary>
  public IWorkersApi WorkersApi => _apiClient.Workers;

  /// <summary>Gets the fully configured Turnstile API client from the DI container.</summary>
  public ITurnstileApi TurnstileApi => _apiClient.Turnstile;

  #endregion

  #region Methods Impl

  /// <summary>Initializes the fixture by fetching the Zone details from the API to infer the BaseDomain.</summary>
  public async Task InitializeAsync()
  {
    if (TestConfigurationValidator.IsSecretMissing(_settings.ZoneId) ||
        TestConfigurationValidator.IsSecretMissing(_settings.ApiToken) ||
        TestConfigurationValidator.IsSecretMissing(_settings.AccountId))
      // Cannot validate or infer the domain if required secrets are missing.
      // The test will be skipped by the [IntegrationTest] attribute.
      return;

    // Run permission validation lazily (only once across all fixtures).
    // This ensures validation completes before any tests run, even with parallelization.
    PermissionValidationRunner.InitializeAccountValidation(ApiTokensApi, _settings.AccountId);
    await PermissionValidationRunner.EnsureAccountValidationAsync();
    
    // NOTE: Uncomment if you want to skip the test if user validation hasn't run yet.
    /*Skip.If(
      !PermissionValidationState.UserValidationCompleted,
      "User token permission validation has not run yet. Ensure PermissionValidationTests runs first.");*/

    // If ANY permission validation failed (account OR user), skip ALL tests.
    // This ensures we don't run partial test suites with cryptic 403 errors.
    PermissionValidationState.EnsureAccountPermissionsValidated();
    PermissionValidationState.EnsureUserPermissionsValidated();

    var zoneDetails = await ZonesApi.GetZoneDetailsAsync(_settings.ZoneId);
    _settings.BaseDomain = zoneDetails.Name;
  }

  /// <summary>Performs asynchronous cleanup.</summary>
  public Task DisposeAsync() => Task.CompletedTask;

  #endregion
}
