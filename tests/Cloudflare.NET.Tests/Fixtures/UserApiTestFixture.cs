namespace Cloudflare.NET.Tests.Fixtures;

using System.Net.Http.Headers;
using ApiTokens;
using AuditLogs;
using Cloudflare.NET.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Fixtures;
using User;

/// <summary>
///   An xUnit class fixture that sets up a User API client configured with a user-scoped API token.
///   <para>
///     This fixture is specifically designed for user profile integration tests that require user-level
///     authentication, which is different from account-scoped tokens used by other API endpoints.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     The Cloudflare User API requires user-level authentication. Account-scoped tokens (even with broad
///     permissions) cannot access user-level endpoints such as <c>GET /user</c> or <c>PATCH /user</c>.
///   </para>
///   <para>
///     This fixture creates a dedicated HttpClient with the <c>UserApiToken</c> from configuration,
///     separate from the main SDK's HttpClient which uses the account-scoped <c>ApiToken</c>.
///   </para>
/// </remarks>
public class UserApiTestFixture : IAsyncLifetime
{
  #region Properties & Fields - Non-Public

  /// <summary>The HttpClient configured with the user-scoped API token.</summary>
  private readonly HttpClient _httpClient;

  /// <summary>The service provider for DI services (logging).</summary>
  private readonly ServiceProvider _serviceProvider;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="UserApiTestFixture" /> class.</summary>
  public UserApiTestFixture()
  {
    var settings = TestConfiguration.CloudflareSettings;

    // Configure services for logging.
    var services = new ServiceCollection();

    // The provider is added as a singleton so it can be resolved and updated by test classes.
    var xunitProvider = new XunitTestOutputLoggerProvider();
    services.AddSingleton(xunitProvider);

    services.AddLogging(builder =>
    {
      builder.ClearProviders();
      builder.AddProvider(xunitProvider);
      builder.AddSimpleConsole(o =>
      {
        o.SingleLine      = true;
        o.IncludeScopes   = true;
        o.TimestampFormat = "HH:mm:ss.fff zzz ";
      });
      builder.SetMinimumLevel(LogLevel.Trace);
      builder.AddFilter("Microsoft", LogLevel.Warning);
      builder.AddFilter("System", LogLevel.Warning);
    });

    _serviceProvider = services.BuildServiceProvider();

    // Create an HttpClient configured with the user-scoped API token.
    _httpClient = new HttpClient
    {
      BaseAddress = new Uri(settings.ApiBaseUrl)
    };

    // Only set the authorization header if the UserApiToken is configured.
    // If missing, the test will be skipped by [UserIntegrationTest] attribute anyway.
    if (!TestConfigurationValidator.IsSecretMissing(settings.UserApiToken))
      _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.UserApiToken);

    // Create the API instances with the user-token-configured HttpClient.
    var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
    UserApi = new UserApi(_httpClient, loggerFactory);
    AuditLogsApi = new AuditLogsApi(_httpClient, loggerFactory);
    ApiTokensApi = new ApiTokensApi(_httpClient, loggerFactory);
  }

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the User API client configured with a user-scoped API token.</summary>
  public IUserApi UserApi { get; }

  /// <summary>Gets the Audit Logs API client configured with a user-scoped API token.</summary>
  public IAuditLogsApi AuditLogsApi { get; }

  /// <summary>Gets the API Tokens API client configured with a user-scoped API token.</summary>
  public IApiTokensApi ApiTokensApi { get; }

  /// <summary>Gets the underlying service provider for resolving services in tests.</summary>
  public IServiceProvider ServiceProvider => _serviceProvider;

  #endregion


  #region Methods Impl - IAsyncLifetime

  /// <summary>Initializes the fixture by validating user token permissions.</summary>
  public async Task InitializeAsync()
  {
    // Run permission validation lazily (only once across all fixtures).
    // This ensures validation completes before any tests run, even with parallelization.
    // NOTE: We must run BOTH validations, not just user validation.
    // The account validation needs an IApiTokensApi which we don't have here,
    // so we just ensure it has been run (by PermissionValidationTests or CloudflareApiTestFixture).
    PermissionValidationRunner.InitializeUserValidation(UserApi);
    await PermissionValidationRunner.EnsureUserValidationAsync();

    // NOTE: Uncomment if you want to skip the test if account validation hasn't run yet.
    /*Skip.If(
      !PermissionValidationState.AccountValidationCompleted,
      "Account token permission validation has not run yet. Ensure PermissionValidationTests runs first.");*/

    // If ANY permission validation failed (account OR user), skip ALL tests.
    // This ensures we don't run partial test suites with cryptic 403 errors.
    PermissionValidationState.EnsureAccountPermissionsValidated();
    PermissionValidationState.EnsureUserPermissionsValidated();
  }

  /// <summary>Disposes of the HTTP client and service provider.</summary>
  public Task DisposeAsync()
  {
    _httpClient.Dispose();
    _serviceProvider.Dispose();

    return Task.CompletedTask;
  }

  #endregion
}
