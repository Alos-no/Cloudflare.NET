namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>
///   Contains unit tests for the named client feature, including the <see cref="ICloudflareApiClientFactory" /> and
///   keyed services registration.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class NamedClientTests
{
  #region Properties & Fields - Non-Public

  private readonly ITestOutputHelper _output;

  #endregion

  #region Constructors

  public NamedClientTests(ITestOutputHelper output)
  {
    _output = output;
  }

  #endregion

  #region Methods

  /// <summary>Verifies that the factory can create a named client with the correct configuration.</summary>
  [Fact]
  public void CreateClient_WithRegisteredName_ReturnsClientWithCorrectConfiguration()
  {
    // Arrange
    const string clientName = "production";
    const string accountId  = "prod-account-123";
    const string apiToken   = "prod-token-abc";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(clientName, options =>
    {
      options.AccountId = accountId;
      options.ApiToken  = apiToken;
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    // Act
    var client = factory.CreateClient(clientName);

    // Assert
    client.Should().NotBeNull();
    client.Should().BeOfType<CloudflareApiClient>();
    client.Accounts.Should().NotBeNull();
    client.Zones.Should().NotBeNull();
  }


  /// <summary>Verifies that the factory throws an appropriate exception when creating a client with an unregistered name.</summary>
  [Fact]
  public void CreateClient_WithUnregisteredName_ThrowsInvalidOperationException()
  {
    // Arrange
    const string registeredName   = "production";
    const string unregisteredName = "staging";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(registeredName, options =>
    {
      options.AccountId = "prod-account";
      options.ApiToken  = "prod-token";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    // Act
    var action = () => factory.CreateClient(unregisteredName);

    // Assert
    action.Should().Throw<InvalidOperationException>()
          .WithMessage($"*No Cloudflare API client configuration found for name '{unregisteredName}'*");
  }


  /// <summary>Verifies that multiple named clients can be registered and resolved independently.</summary>
  [Fact]
  public void CreateClient_WithMultipleRegisteredNames_ReturnsDistinctClients()
  {
    // Arrange
    const string prodName    = "production";
    const string stagingName = "staging";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(prodName, options =>
    {
      options.AccountId = "prod-account";
      options.ApiToken  = "prod-token";
    });

    services.AddCloudflareApiClient(stagingName, options =>
    {
      options.AccountId = "staging-account";
      options.ApiToken  = "staging-token";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    // Act
    var prodClient    = factory.CreateClient(prodName);
    var stagingClient = factory.CreateClient(stagingName);

    // Assert
    prodClient.Should().NotBeNull();
    stagingClient.Should().NotBeNull();
    prodClient.Should().NotBeSameAs(stagingClient);
  }


  /// <summary>Verifies that the factory throws <see cref="ArgumentException" /> when passed a null or empty name.</summary>
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void CreateClient_WithNullOrEmptyName_ThrowsArgumentException(string? invalidName)
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient("valid", options =>
    {
      options.AccountId = "account";
      options.ApiToken  = "token";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    // Act
    var action = () => factory.CreateClient(invalidName!);

    // Assert
    action.Should().Throw<ArgumentException>();
  }

  /// <summary>Verifies that named clients can be resolved using keyed services.</summary>
  [Fact]
  public void KeyedServices_WithRegisteredName_ResolvesCorrectClient()
  {
    // Arrange
    const string clientName = "production";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(clientName, options =>
    {
      options.AccountId = "prod-account";
      options.ApiToken  = "prod-token";
    });

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var client = serviceProvider.GetKeyedService<ICloudflareApiClient>(clientName);

    // Assert
    client.Should().NotBeNull();
    client.Should().BeOfType<CloudflareApiClient>();
  }


  /// <summary>
  ///   Verifies that keyed services returns null for unregistered names (when using GetKeyedService, not
  ///   GetRequiredKeyedService).
  /// </summary>
  [Fact]
  public void KeyedServices_WithUnregisteredName_ReturnsNull()
  {
    // Arrange
    const string registeredName   = "production";
    const string unregisteredName = "staging";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(registeredName, options =>
    {
      options.AccountId = "prod-account";
      options.ApiToken  = "prod-token";
    });

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var client = serviceProvider.GetKeyedService<ICloudflareApiClient>(unregisteredName);

    // Assert
    client.Should().BeNull();
  }

  /// <summary>Verifies that named clients can coexist with the default (unnamed) client.</summary>
  [Fact]
  public void NamedClient_CoexistsWithDefaultClient()
  {
    // Arrange
    var services = CreateServiceCollection();

    // Register a default client.
    services.AddCloudflareApiClient(options =>
    {
      options.AccountId = "default-account";
      options.ApiToken  = "default-token";
    });

    // Register a named client.
    services.AddCloudflareApiClient("named", options =>
    {
      options.AccountId = "named-account";
      options.ApiToken  = "named-token";
    });

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var defaultClient = serviceProvider.GetRequiredService<ICloudflareApiClient>();
    var factory       = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();
    var namedClient   = factory.CreateClient("named");

    // Assert
    defaultClient.Should().NotBeNull();
    namedClient.Should().NotBeNull();
    defaultClient.Should().NotBeSameAs(namedClient);
  }


  /// <summary>Verifies that the factory is registered as a singleton and shared across registrations.</summary>
  [Fact]
  public void Factory_IsSingleton_AcrossMultipleRegistrations()
  {
    // Arrange
    var services = CreateServiceCollection();

    // Register multiple named clients - each call should not replace the factory.
    services.AddCloudflareApiClient("client1", o => o.ApiToken = "token1");
    services.AddCloudflareApiClient("client2", o => o.ApiToken = "token2");
    services.AddCloudflareApiClient("client3", o => o.ApiToken = "token3");

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var factory1 = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();
    var factory2 = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

    // Assert
    factory1.Should().BeSameAs(factory2);
  }

  /// <summary>Verifies that a named client uses its own configuration for HTTP requests.</summary>
  [Fact]
  public async Task NamedClient_UsesCorrectHttpConfiguration()
  {
    // Arrange
    const string clientName = "production";
    const string accountId  = "prod-account-id";

    HttpRequestMessage? capturedRequest = null;
    var                 mockHandler     = new Mock<HttpMessageHandler>();

    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
               {
                 Content = new StringContent(HttpFixtures.CreateSuccessResponse<object?>(null))
               });

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(clientName, options =>
    {
      options.AccountId = accountId;
      options.ApiToken  = "test-token";
    });

    // Inject the mock handler for the named client.
    services.ConfigureAll<HttpClientFactoryOptions>(opts =>
    {
      opts.HttpMessageHandlerBuilderActions.Add(builder =>
      {
        builder.PrimaryHandler = mockHandler.Object;
      });
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();
    var client          = factory.CreateClient(clientName);

    // Act
    await client.Accounts.DeleteR2BucketAsync("test-bucket");

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain($"accounts/{accountId}");
    capturedRequest.Headers.Authorization.Should().NotBeNull();
    capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
    capturedRequest.Headers.Authorization.Parameter.Should().Be("test-token");
  }

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
