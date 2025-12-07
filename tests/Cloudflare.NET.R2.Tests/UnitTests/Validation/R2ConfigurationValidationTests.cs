namespace Cloudflare.NET.R2.Tests.UnitTests.Validation;

using Configuration;
using Core;
using Exceptions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NET.Tests.Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for R2 startup configuration validation, verifying that
///   <c>ValidateOnStart()</c> properly validates R2 options at application startup.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class R2ConfigurationValidationTests
{
  #region Properties & Fields - Non-Public

  private readonly ITestOutputHelper _output;

  #endregion

  #region Constructors

  public R2ConfigurationValidationTests(ITestOutputHelper output)
  {
    _output = output;
  }

  #endregion

  #region Default Client Startup Validation Tests

  /// <summary>
  ///   Verifies that building the service provider with missing R2 credentials throws OptionsValidationException.
  /// </summary>
  [Fact]
  public void AddCloudflareR2Client_WithMissingCredentials_ThrowsOnStartup()
  {
    // Arrange
    var services = CreateServiceCollection();

    // Register Cloudflare options with AccountId (required for R2)
    services.AddCloudflareApiClient(options =>
    {
      options.AccountId = "test-account-id";
      options.ApiToken  = "test-token";
    });

    // Register R2 with missing credentials
    services.AddCloudflareR2Client(options =>
    {
      options.AccessKeyId     = ""; // Missing
      options.SecretAccessKey = ""; // Missing
    });

    var serviceProvider = services.BuildServiceProvider();

    // Act - Trigger validation by getting IOptions<R2Settings>
    var action = () =>
    {
      var options = serviceProvider.GetRequiredService<IOptions<R2Settings>>();
      _ = options.Value; // Force validation
    };

    // Assert
    action.Should().Throw<OptionsValidationException>()
          .WithMessage("*R2 AccessKeyId is required*");
  }


  /// <summary>
  ///   Verifies that building the service provider with missing AccountId throws OptionsValidationException.
  /// </summary>
  [Fact]
  public void AddCloudflareR2Client_WithMissingAccountId_ThrowsOnStartup()
  {
    // Arrange
    var services = CreateServiceCollection();

    // Register Cloudflare options WITHOUT AccountId
    services.AddCloudflareApiClient(options =>
    {
      options.ApiToken  = "test-token";
      options.AccountId = ""; // Missing
    });

    // Register R2 with valid credentials
    services.AddCloudflareR2Client(options =>
    {
      options.AccessKeyId     = "valid-access-key";
      options.SecretAccessKey = "valid-secret-key";
    });

    var serviceProvider = services.BuildServiceProvider();

    // Act - Trigger validation by getting IOptions<CloudflareApiOptions>
    var action = () =>
    {
      var options = serviceProvider.GetRequiredService<IOptions<CloudflareApiOptions>>();
      _ = options.Value; // Force validation
    };

    // Assert
    action.Should().Throw<OptionsValidationException>()
          .WithMessage("*Cloudflare AccountId is required*");
  }


  /// <summary>
  ///   Verifies that building the service provider with valid options succeeds.
  /// </summary>
  [Fact]
  public void AddCloudflareR2Client_WithValidOptions_DoesNotThrow()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(options =>
    {
      options.AccountId = "test-account-id";
      options.ApiToken  = "test-token";
    });

    services.AddCloudflareR2Client(options =>
    {
      options.AccessKeyId     = "valid-access-key";
      options.SecretAccessKey = "valid-secret-key";
      // EndpointUrl and Region use defaults
    });

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var action = () =>
    {
      var client = serviceProvider.GetRequiredService<IR2Client>();
      client.Should().NotBeNull();
    };

    // Assert
    action.Should().NotThrow();
  }

  #endregion


  #region Default Values Tests

  /// <summary>
  ///   Verifies that EndpointUrl has a sensible default value.
  /// </summary>
  [Fact]
  public void R2Settings_HasDefaultEndpointUrl()
  {
    // Arrange
    var settings = new R2Settings();

    // Assert
    settings.EndpointUrl.Should().Be("https://{0}.r2.cloudflarestorage.com");
  }


  /// <summary>
  ///   Verifies that Region has a sensible default value.
  /// </summary>
  [Fact]
  public void R2Settings_HasDefaultRegion()
  {
    // Arrange
    var settings = new R2Settings();

    // Assert
    settings.Region.Should().Be("auto");
  }


  /// <summary>
  ///   Verifies that only AccessKeyId, SecretAccessKey, and AccountId are required with defaults in place.
  /// </summary>
  [Fact]
  public void AddCloudflareR2Client_WithOnlyRequiredFields_Succeeds()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(options =>
    {
      options.AccountId = "test-account-id";
      options.ApiToken  = "test-token";
    });

    services.AddCloudflareR2Client(options =>
    {
      options.AccessKeyId     = "valid-access-key";
      options.SecretAccessKey = "valid-secret-key";
      // Don't set EndpointUrl or Region - they should use defaults
    });

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var r2Client = serviceProvider.GetService<IR2Client>();

    // Assert
    r2Client.Should().NotBeNull();
  }

  #endregion


  #region Named Client Validation Tests

  /// <summary>
  ///   Verifies that named client validation happens at client creation, not at startup.
  /// </summary>
  [Fact]
  public void AddCloudflareR2Client_Named_ValidationHappensOnClientCreation()
  {
    // Arrange
    const string clientName = "invalid-client";

    var services = CreateServiceCollection();

    // Register a named client with invalid configuration (missing credentials)
    services.AddCloudflareR2Client(clientName, options =>
    {
      options.AccessKeyId     = ""; // Invalid
      options.SecretAccessKey = ""; // Invalid
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act - Creating the client should fail with CloudflareR2ConfigurationException
    var action = () => factory.CreateClient(clientName);

    // Assert
    action.Should().Throw<CloudflareR2ConfigurationException>()
          .WithMessage("*R2 AccessKeyId is required*");
  }


  /// <summary>
  ///   Verifies that named client validation includes all missing fields in the error message.
  /// </summary>
  [Fact]
  public void AddCloudflareR2Client_Named_WithMultipleMissingFields_ReportsAllErrors()
  {
    // Arrange
    const string clientName = "invalid-client";

    var services = CreateServiceCollection();

    // Register a named client with multiple missing fields
    services.AddCloudflareR2Client(clientName, options =>
    {
      options.AccessKeyId     = "";
      options.SecretAccessKey = "";
      // AccountId also missing
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var action = () => factory.CreateClient(clientName);

    // Assert - Should report multiple errors
    action.Should().Throw<CloudflareR2ConfigurationException>()
          .Which.Message.Should().Contain("AccessKeyId")
          .And.Contain("SecretAccessKey")
          .And.Contain("AccountId");
  }

  #endregion


  #region Error Message Quality Tests

  /// <summary>
  ///   Verifies that validation errors mention "Cloudflare R2" for clear identification.
  /// </summary>
  [Fact]
  public void AddCloudflareR2Client_ValidationErrors_MentionCloudflareR2()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(options =>
    {
      options.AccountId = "test-account-id";
      options.ApiToken  = "test-token";
    });

    services.AddCloudflareR2Client(options =>
    {
      options.AccessKeyId     = "";
      options.SecretAccessKey = "valid-secret";
    });

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var action = () =>
    {
      var options = serviceProvider.GetRequiredService<IOptions<R2Settings>>();
      _ = options.Value;
    };

    // Assert
    action.Should().Throw<OptionsValidationException>()
          .WithMessage("*Cloudflare R2*");
  }

  #endregion


  #region Helper Methods

  /// <summary>Creates a service collection with common test dependencies.</summary>
  private ServiceCollection CreateServiceCollection()
  {
    var services = new ServiceCollection();

    // Add logging that pipes to xUnit test output.
    services.AddLogging(builder => builder.AddProvider(new XunitTestOutputLoggerProvider { Current = _output }));

    return services;
  }

  #endregion
}
