namespace Cloudflare.NET.Tests.UnitTests.Validation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for startup configuration validation, verifying that <c>ValidateOnStart()</c>
///   properly validates options at application startup.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class ConfigurationValidationTests
{
  #region Properties & Fields - Non-Public

  private readonly ITestOutputHelper _output;

  #endregion

  #region Constructors

  public ConfigurationValidationTests(ITestOutputHelper output)
  {
    _output = output;
  }

  #endregion

  #region Methods

  #region Named Client Validation Tests

  /// <summary>
  ///   Verifies that named client validation happens at client creation, not at startup. Named clients don't use
  ///   ValidateOnStart because they may be configured dynamically.
  /// </summary>
  [Fact]
  public void AddCloudflareApiClient_Named_ValidationHappensOnClientCreation()
  {
    // Arrange
    const string clientName = "invalid-client";

    var services = CreateServiceCollection();

    // Register a named client with invalid configuration
    services.AddCloudflareApiClient(clientName, options =>
    {
      options.ApiToken = ""; // Invalid
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    // Act - Build provider should succeed (validation happens later)
    // Creating the client should fail
    var action = () => factory.CreateClient(clientName);

    // Assert
    action.Should().Throw<InvalidOperationException>()
          .WithMessage("*Cloudflare ApiToken is required*");
  }

  #endregion

  /// <summary>Creates a service collection with common test dependencies.</summary>
  private ServiceCollection CreateServiceCollection()
  {
    var services = new ServiceCollection();

    // Add logging that pipes to xUnit test output.
    services.AddLogging(builder => builder.AddProvider(new XunitTestOutputLoggerProvider { Current = _output }));

    return services;
  }

  #endregion

  #region Core API Client Validation Tests

  /// <summary>Verifies that building the service provider with missing ApiToken throws OptionsValidationException.</summary>
  [Fact]
  public void AddCloudflareApiClient_WithMissingApiToken_ThrowsOnStartup()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(options =>
    {
      options.ApiToken = ""; // Empty - should fail validation
    });

    var serviceProvider = services.BuildServiceProvider();

    // Act - Trigger validation by getting IOptions
    var action = () =>
    {
      // ValidateOnStart validation is triggered when the host starts or when options are first accessed
      var options = serviceProvider.GetRequiredService<IOptions<CloudflareApiOptions>>();
      _ = options.Value; // Force validation
    };

    // Assert
    action.Should().Throw<OptionsValidationException>()
          .WithMessage("*Cloudflare ApiToken is required*");
  }


  /// <summary>Verifies that building the service provider with valid options succeeds.</summary>
  [Fact]
  public void AddCloudflareApiClient_WithValidOptions_DoesNotThrow()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(options => { options.ApiToken = "valid-token"; });

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var action = () =>
    {
      var client = serviceProvider.GetRequiredService<ICloudflareApiClient>();
      client.Should().NotBeNull();
    };

    // Assert
    action.Should().NotThrow();
  }


  /// <summary>Verifies that the error message includes the correct configuration path.</summary>
  [Fact]
  public void AddCloudflareApiClient_ValidationError_IncludesConfigPath()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(options => { options.ApiToken = ""; });

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var action = () =>
    {
      var options = serviceProvider.GetRequiredService<IOptions<CloudflareApiOptions>>();
      _ = options.Value;
    };

    // Assert
    action.Should().Throw<OptionsValidationException>()
          .WithMessage("*Cloudflare:ApiToken*");
  }

  #endregion
}
