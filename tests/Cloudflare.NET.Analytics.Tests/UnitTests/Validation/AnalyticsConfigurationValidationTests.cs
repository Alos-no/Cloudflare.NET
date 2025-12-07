namespace Cloudflare.NET.Analytics.Tests.UnitTests.Validation;

using Core;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for Analytics startup configuration validation, verifying that
///   <c>ValidateOnStart()</c> properly validates Analytics options at application startup.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class AnalyticsConfigurationValidationTests
{
  #region Properties & Fields - Non-Public

  private readonly ITestOutputHelper _output;

  #endregion

  #region Constructors

  public AnalyticsConfigurationValidationTests(ITestOutputHelper output)
  {
    _output = output;
  }

  #endregion

  #region Default Client Startup Validation Tests

  /// <summary>
  ///   Verifies that building the service provider with missing ApiToken throws OptionsValidationException.
  /// </summary>
  [Fact]
  public void AddCloudflareAnalytics_WithMissingApiToken_ThrowsOnStartup()
  {
    // Arrange
    var services = CreateServiceCollection();

    // Register Cloudflare options without ApiToken
    services.AddCloudflareApiClient(options =>
    {
      options.ApiToken      = ""; // Missing
      options.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    // Register Analytics
    services.AddCloudflareAnalytics();

    var serviceProvider = services.BuildServiceProvider();

    // Act - Trigger validation by getting IOptions
    var action = () =>
    {
      var options = serviceProvider.GetRequiredService<IOptions<CloudflareApiOptions>>();
      _ = options.Value; // Force validation
    };

    // Assert
    action.Should().Throw<OptionsValidationException>()
          .WithMessage("*Cloudflare ApiToken is required*");
  }


  /// <summary>
  ///   Verifies that building the service provider with missing GraphQlApiUrl throws OptionsValidationException.
  /// </summary>
  [Fact]
  public void AddCloudflareAnalytics_WithMissingGraphQlApiUrl_ThrowsOnStartup()
  {
    // Arrange
    var services = CreateServiceCollection();

    // Register Cloudflare options without GraphQlApiUrl
    services.AddCloudflareApiClient(options =>
    {
      options.ApiToken      = "valid-token";
      options.GraphQlApiUrl = ""; // Missing
    });

    // Register Analytics
    services.AddCloudflareAnalytics();

    var serviceProvider = services.BuildServiceProvider();

    // Act - Trigger validation
    var action = () =>
    {
      var options = serviceProvider.GetRequiredService<IOptions<CloudflareApiOptions>>();
      _ = options.Value;
    };

    // Assert
    action.Should().Throw<OptionsValidationException>()
          .WithMessage("*Cloudflare GraphQlApiUrl is required*");
  }


  /// <summary>
  ///   Verifies that building the service provider with valid options succeeds.
  /// </summary>
  [Fact]
  public void AddCloudflareAnalytics_WithValidOptions_DoesNotThrow()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(options =>
    {
      options.ApiToken      = "valid-token";
      options.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    services.AddCloudflareAnalytics();

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var action = () =>
    {
      var client = serviceProvider.GetRequiredService<IAnalyticsApi>();
      client.Should().NotBeNull();
    };

    // Assert
    action.Should().NotThrow();
  }


  /// <summary>
  ///   Verifies that validation reports multiple errors when multiple fields are missing.
  /// </summary>
  [Fact]
  public void AddCloudflareAnalytics_WithMultipleMissingFields_ReportsAllErrors()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(options =>
    {
      options.ApiToken      = ""; // Missing
      options.GraphQlApiUrl = ""; // Missing
    });

    services.AddCloudflareAnalytics();

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var action = () =>
    {
      var options = serviceProvider.GetRequiredService<IOptions<CloudflareApiOptions>>();
      _ = options.Value;
    };

    // Assert - Should report both errors
    var exception = action.Should().Throw<OptionsValidationException>().Which;
    exception.Message.Should().Contain("ApiToken");
    exception.Message.Should().Contain("GraphQlApiUrl");
  }

  #endregion


  #region Named Client Validation Tests

  /// <summary>
  ///   Verifies that named client validation happens at client creation, not at startup.
  /// </summary>
  [Fact]
  public void AddCloudflareAnalytics_Named_ValidationHappensOnClientCreation()
  {
    // Arrange
    const string clientName = "invalid-client";

    var services = CreateServiceCollection();

    // Register a named Cloudflare client with invalid configuration
    services.AddCloudflareApiClient(clientName, options =>
    {
      options.ApiToken      = ""; // Invalid
      options.GraphQlApiUrl = ""; // Invalid
    });

    services.AddCloudflareAnalytics(clientName);

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IAnalyticsApiFactory>();

    // Act - Creating the client should fail
    var action = () => factory.CreateClient(clientName);

    // Assert
    action.Should().Throw<InvalidOperationException>()
          .WithMessage("*Cloudflare ApiToken is required*");
  }


  /// <summary>
  ///   Verifies that named client error message includes the client name.
  /// </summary>
  [Fact]
  public void AddCloudflareAnalytics_Named_ErrorMessageIncludesClientName()
  {
    // Arrange
    const string clientName = "my-analytics-client";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(clientName, options =>
    {
      options.ApiToken      = ""; // Invalid
      options.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    services.AddCloudflareAnalytics(clientName);

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IAnalyticsApiFactory>();

    // Act
    var action = () => factory.CreateClient(clientName);

    // Assert - Error message should mention the client name
    action.Should().Throw<InvalidOperationException>()
          .WithMessage($"*{clientName}*");
  }

  #endregion


  #region Default Values Tests

  /// <summary>
  ///   Verifies that CloudflareApiOptions has a default GraphQlApiUrl.
  /// </summary>
  [Fact]
  public void CloudflareApiOptions_HasDefaultGraphQlApiUrl()
  {
    // Arrange
    var options = new CloudflareApiOptions();

    // Assert
    options.GraphQlApiUrl.Should().Be("https://api.cloudflare.com/client/v4/graphql");
  }


  /// <summary>
  ///   Verifies that with default GraphQlApiUrl, only ApiToken is required.
  /// </summary>
  [Fact]
  public void AddCloudflareAnalytics_WithOnlyApiToken_Succeeds()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(options =>
    {
      options.ApiToken = "valid-token";
      // GraphQlApiUrl uses default
    });

    services.AddCloudflareAnalytics();

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var client = serviceProvider.GetService<IAnalyticsApi>();

    // Assert
    client.Should().NotBeNull();
  }

  #endregion


  #region Error Message Quality Tests

  /// <summary>
  ///   Verifies that validation errors mention "Cloudflare" for clear identification.
  /// </summary>
  [Fact]
  public void AddCloudflareAnalytics_ValidationErrors_MentionCloudflare()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(options =>
    {
      options.ApiToken      = "";
      options.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    services.AddCloudflareAnalytics();

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var action = () =>
    {
      var options = serviceProvider.GetRequiredService<IOptions<CloudflareApiOptions>>();
      _ = options.Value;
    };

    // Assert
    action.Should().Throw<OptionsValidationException>()
          .WithMessage("*Cloudflare*");
  }


  /// <summary>
  ///   Verifies that validation errors include configuration path guidance.
  /// </summary>
  [Fact]
  public void AddCloudflareAnalytics_ValidationErrors_IncludeConfigPath()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(options =>
    {
      options.ApiToken = "";
    });

    services.AddCloudflareAnalytics();

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
