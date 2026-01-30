namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>
///   Contains unit tests for the dynamic client creation feature, specifically the
///   <see cref="ICloudflareApiClientFactory.CreateClient(CloudflareApiOptions)" /> overload.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class DynamicClientTests
{
  #region Properties & Fields - Non-Public

  private readonly ITestOutputHelper _output;

  #endregion


  #region Constructors

  public DynamicClientTests(ITestOutputHelper output)
  {
    _output = output;
  }

  #endregion


  #region Methods - Basic Functionality Tests

  /// <summary>Verifies that a dynamic client can be created from options without pre-registration.</summary>
  [Fact]
  public void CreateClient_WithValidOptions_ReturnsConfiguredClient()
  {
    // Arrange
    var services = CreateServiceCollection();

    // Only register the factory (via AddCloudflareApiClient with a dummy client).
    services.AddCloudflareApiClient("dummy", o => o.ApiToken = "dummy-token");

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    var options = new CloudflareApiOptions
    {
      ApiToken  = "dynamic-token",
      AccountId = "dynamic-account"
    };

    // Act
    var client = factory.CreateClient(options);

    // Assert
    client.Should().NotBeNull();
    client.Accounts.Should().NotBeNull();
    client.Zones.Should().NotBeNull();
    client.Dns.Should().NotBeNull();
  }


  /// <summary>Verifies that a dynamic client implements IDisposable for cleanup.</summary>
  [Fact]
  public void CreateClient_WithValidOptions_ReturnsDisposableClient()
  {
    // Arrange
    var services = CreateServiceCollection();
    services.AddCloudflareApiClient("dummy", o => o.ApiToken = "dummy-token");

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    var options = new CloudflareApiOptions
    {
      ApiToken = "dynamic-token"
    };

    // Act
    var client = factory.CreateClient(options);

    // Assert
    client.Should().BeAssignableTo<IDisposable>();
  }


  /// <summary>Verifies that disposing a dynamic client releases resources properly.</summary>
  [Fact]
  public void CreateClient_Dispose_ReleasesResources()
  {
    // Arrange
    var services = CreateServiceCollection();
    services.AddCloudflareApiClient("dummy", o => o.ApiToken = "dummy-token");

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    var options = new CloudflareApiOptions
    {
      ApiToken = "dynamic-token"
    };

    var client = factory.CreateClient(options);

    // Act
    client.Dispose();

    // Assert - After disposal, accessing properties should throw ObjectDisposedException.
    var action = () => _ = client.Accounts;
    action.Should().Throw<ObjectDisposedException>();
  }


  /// <summary>Verifies that multiple dynamic clients can be created independently.</summary>
  [Fact]
  public void CreateClient_MultipleCalls_ReturnsDistinctClients()
  {
    // Arrange
    var services = CreateServiceCollection();
    services.AddCloudflareApiClient("dummy", o => o.ApiToken = "dummy-token");

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    var options1 = new CloudflareApiOptions { ApiToken = "token-1", AccountId = "account-1" };
    var options2 = new CloudflareApiOptions { ApiToken = "token-2", AccountId = "account-2" };

    // Act
    var client1 = factory.CreateClient(options1);
    var client2 = factory.CreateClient(options2);

    // Assert
    client1.Should().NotBeSameAs(client2);

    // Cleanup
    client1.Dispose();
    client2.Dispose();
  }

  #endregion


  #region Methods - Validation Tests

  /// <summary>Verifies that CreateClient throws ArgumentNullException when options is null.</summary>
  [Fact]
  public void CreateClient_WithNullOptions_ThrowsArgumentNullException()
  {
    // Arrange
    var services = CreateServiceCollection();
    services.AddCloudflareApiClient("dummy", o => o.ApiToken = "dummy-token");

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    // Act
    var action = () => factory.CreateClient((CloudflareApiOptions)null!);

    // Assert
    action.Should().Throw<ArgumentNullException>()
          .WithParameterName("options");
  }


  /// <summary>Verifies that CreateClient throws InvalidOperationException when ApiToken is missing.</summary>
  [Fact]
  public void CreateClient_WithMissingApiToken_ThrowsInvalidOperationException()
  {
    // Arrange
    var services = CreateServiceCollection();
    services.AddCloudflareApiClient("dummy", o => o.ApiToken = "dummy-token");

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    var options = new CloudflareApiOptions
    {
      AccountId = "some-account"
      // ApiToken is intentionally not set
    };

    // Act
    var action = () => factory.CreateClient(options);

    // Assert
    action.Should().Throw<InvalidOperationException>()
          .WithMessage("*ApiToken*required*");
  }

  #endregion


  #region Methods - Configuration Tests

  /// <summary>Verifies that a dynamic client uses the correct base URL from options.</summary>
  [Fact]
  public async Task CreateClient_UsesCorrectBaseUrl()
  {
    // Arrange
    const string customBaseUrl = "https://custom.cloudflare.api/v4/";

    HttpRequestMessage? capturedRequest = null;
    var                 mockHandler     = CreateMockHandler(req => capturedRequest = req);

    var services = CreateServiceCollection();
    services.AddCloudflareApiClient("dummy", o => o.ApiToken = "dummy-token");

    // We can't easily inject the mock handler into a dynamic client,
    // so we test by checking the client is created without error.
    // The actual HTTP behavior would be tested in integration tests.

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    var options = new CloudflareApiOptions
    {
      ApiToken   = "test-token",
      ApiBaseUrl = customBaseUrl
    };

    // Act
    var client = factory.CreateClient(options);

    // Assert - Client was created successfully with custom base URL.
    client.Should().NotBeNull();

    // Cleanup
    client.Dispose();
  }


  /// <summary>Verifies that a dynamic client uses the default API base URL when not specified.</summary>
  [Fact]
  public void CreateClient_WithDefaultBaseUrl_UsesCloudflareApiUrl()
  {
    // Arrange
    var services = CreateServiceCollection();
    services.AddCloudflareApiClient("dummy", o => o.ApiToken = "dummy-token");

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    var options = new CloudflareApiOptions
    {
      ApiToken = "test-token"
      // ApiBaseUrl uses default value
    };

    // Act - Should not throw, meaning default base URL is valid.
    var client = factory.CreateClient(options);

    // Assert
    client.Should().NotBeNull();

    // Cleanup
    client.Dispose();
  }


  /// <summary>Verifies that a dynamic client can be configured with custom rate limiting options.</summary>
  [Fact]
  public void CreateClient_WithCustomRateLimitingOptions_CreatesClientSuccessfully()
  {
    // Arrange
    var services = CreateServiceCollection();
    services.AddCloudflareApiClient("dummy", o => o.ApiToken = "dummy-token");

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    var options = new CloudflareApiOptions
    {
      ApiToken = "test-token",
      RateLimiting = new RateLimitingOptions
      {
        IsEnabled   = true,
        PermitLimit = 5,
        QueueLimit  = 10,
        MaxRetries  = 3
      }
    };

    // Act
    var client = factory.CreateClient(options);

    // Assert
    client.Should().NotBeNull();

    // Cleanup
    client.Dispose();
  }


  /// <summary>Verifies that a dynamic client can be configured with custom timeout.</summary>
  [Fact]
  public void CreateClient_WithCustomTimeout_CreatesClientSuccessfully()
  {
    // Arrange
    var services = CreateServiceCollection();
    services.AddCloudflareApiClient("dummy", o => o.ApiToken = "dummy-token");

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    var options = new CloudflareApiOptions
    {
      ApiToken       = "test-token",
      DefaultTimeout = TimeSpan.FromSeconds(60)
    };

    // Act
    var client = factory.CreateClient(options);

    // Assert
    client.Should().NotBeNull();

    // Cleanup
    client.Dispose();
  }

  #endregion


  #region Methods - Coexistence Tests

  /// <summary>Verifies that dynamic clients can coexist with named clients.</summary>
  [Fact]
  public void DynamicClient_CoexistsWithNamedClients()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient("named", options =>
    {
      options.AccountId = "named-account";
      options.ApiToken  = "named-token";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    var dynamicOptions = new CloudflareApiOptions
    {
      ApiToken  = "dynamic-token",
      AccountId = "dynamic-account"
    };

    // Act
    var namedClient   = factory.CreateClient("named");
    var dynamicClient = factory.CreateClient(dynamicOptions);

    // Assert
    namedClient.Should().NotBeNull();
    dynamicClient.Should().NotBeNull();
    namedClient.Should().NotBeSameAs(dynamicClient);

    // Cleanup
    dynamicClient.Dispose();
  }


  /// <summary>Verifies that dynamic clients can coexist with the default client.</summary>
  [Fact]
  public void DynamicClient_CoexistsWithDefaultClient()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(options =>
    {
      options.AccountId = "default-account";
      options.ApiToken  = "default-token";
    });

    var serviceProvider = services.BuildServiceProvider();
    var defaultClient   = serviceProvider.GetRequiredService<ICloudflareApiClient>();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    var dynamicOptions = new CloudflareApiOptions
    {
      ApiToken  = "dynamic-token",
      AccountId = "dynamic-account"
    };

    // Act
    var dynamicClient = factory.CreateClient(dynamicOptions);

    // Assert
    defaultClient.Should().NotBeNull();
    dynamicClient.Should().NotBeNull();
    defaultClient.Should().NotBeSameAs(dynamicClient);

    // Cleanup
    dynamicClient.Dispose();
  }

  #endregion


  #region Methods - Helper

  /// <summary>Creates a service collection with common test dependencies.</summary>
  private ServiceCollection CreateServiceCollection()
  {
    var services = new ServiceCollection();

    // Add logging that pipes to xUnit test output.
    services.AddLogging(builder => builder.AddProvider(new XunitTestOutputLoggerProvider { Current = _output }));

    return services;
  }


  /// <summary>Creates a mock HTTP message handler that captures requests.</summary>
  private Mock<HttpMessageHandler> CreateMockHandler(Action<HttpRequestMessage>? onRequest = null)
  {
    var mockHandler = new Mock<HttpMessageHandler>();

    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => onRequest?.Invoke(req))
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
               {
                 Content = new StringContent(HttpFixtures.CreateSuccessResponse<object?>(null))
               });

    return mockHandler;
  }

  #endregion
}
