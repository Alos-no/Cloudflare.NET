namespace Cloudflare.NET.Analytics.Tests.UnitTests;

using System.Net;
using System.Text;
using System.Text.Json;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

/// <summary>
///   Contains unit tests for the named Analytics client feature, including the <see cref="IAnalyticsApiFactory" />
///   and keyed services registration.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class NamedAnalyticsClientTests
{
  #region Properties & Fields - Non-Public

  private readonly ITestOutputHelper _output;

  #endregion

  #region Constructors

  public NamedAnalyticsClientTests(ITestOutputHelper output)
  {
    _output = output;
  }

  #endregion

  #region Methods

  /// <summary>Verifies that the factory can create a named Analytics client with the correct configuration.</summary>
  [Fact]
  public void CreateClient_WithRegisteredName_ReturnsClient()
  {
    // Arrange
    const string clientName = "account1";

    var services = CreateServiceCollection();

    // Register the Cloudflare API options (required for API token and GraphQL URL).
    services.AddCloudflareApiClient(clientName, options =>
    {
      options.ApiToken      = "test-token";
      options.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    // Register the named Analytics client.
    services.AddCloudflareAnalytics(clientName);

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IAnalyticsApiFactory>();

    // Act
    var client = factory.CreateClient(clientName);

    // Assert
    client.Should().NotBeNull();
    client.Should().BeOfType<AnalyticsApi>();
  }


  /// <summary>
  ///   Verifies that the factory throws an appropriate exception when creating a client with an unregistered name
  ///   (missing Cloudflare API options).
  /// </summary>
  [Fact]
  public void CreateClient_WithUnregisteredName_ThrowsInvalidOperationException()
  {
    // Arrange
    const string registeredName   = "account1";
    const string unregisteredName = "account2";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(registeredName, options =>
    {
      options.ApiToken      = "test-token";
      options.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    services.AddCloudflareAnalytics(registeredName);

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IAnalyticsApiFactory>();

    // Act
    var action = () => factory.CreateClient(unregisteredName);

    // Assert - Now uses shared validator with clear error message
    action.Should().Throw<InvalidOperationException>()
          .WithMessage("*Cloudflare ApiToken is required*");
  }


  /// <summary>Verifies that the factory caches clients and returns the same instance for the same name.</summary>
  [Fact]
  public void CreateClient_CalledTwiceWithSameName_ReturnsCachedInstance()
  {
    // Arrange
    const string clientName = "account1";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(clientName, options =>
    {
      options.ApiToken      = "test-token";
      options.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    services.AddCloudflareAnalytics(clientName);

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IAnalyticsApiFactory>();

    // Act
    var client1 = factory.CreateClient(clientName);
    var client2 = factory.CreateClient(clientName);

    // Assert
    client1.Should().BeSameAs(client2);
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

    services.AddCloudflareApiClient("valid", o =>
    {
      o.ApiToken      = "token";
      o.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    services.AddCloudflareAnalytics("valid");

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IAnalyticsApiFactory>();

    // Act
    var action = () => factory.CreateClient(invalidName!);

    // Assert
    action.Should().Throw<ArgumentException>();
  }

  /// <summary>Verifies that named Analytics clients can be resolved using keyed services.</summary>
  [Fact]
  public void KeyedServices_WithRegisteredName_ResolvesCorrectClient()
  {
    // Arrange
    const string clientName = "account1";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(clientName, options =>
    {
      options.ApiToken      = "test-token";
      options.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    services.AddCloudflareAnalytics(clientName);

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var client = serviceProvider.GetKeyedService<IAnalyticsApi>(clientName);

    // Assert
    client.Should().NotBeNull();
    client.Should().BeOfType<AnalyticsApi>();
  }


  /// <summary>Verifies that keyed services returns null for unregistered names.</summary>
  [Fact]
  public void KeyedServices_WithUnregisteredName_ReturnsNull()
  {
    // Arrange
    const string registeredName   = "account1";
    const string unregisteredName = "account2";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(registeredName, o =>
    {
      o.ApiToken      = "token";
      o.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    services.AddCloudflareAnalytics(registeredName);

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var client = serviceProvider.GetKeyedService<IAnalyticsApi>(unregisteredName);

    // Assert
    client.Should().BeNull();
  }

  /// <summary>Verifies that multiple named Analytics clients can be registered and resolved independently.</summary>
  [Fact]
  public void CreateClient_WithMultipleNames_ReturnsDistinctClients()
  {
    // Arrange
    const string account1Name = "account1";
    const string account2Name = "account2";

    var services = CreateServiceCollection();

    // Register account1.
    services.AddCloudflareApiClient(account1Name, o =>
    {
      o.ApiToken      = "token1";
      o.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    services.AddCloudflareAnalytics(account1Name);

    // Register account2.
    services.AddCloudflareApiClient(account2Name, o =>
    {
      o.ApiToken      = "token2";
      o.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    services.AddCloudflareAnalytics(account2Name);

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IAnalyticsApiFactory>();

    // Act
    var account1Client = factory.CreateClient(account1Name);
    var account2Client = factory.CreateClient(account2Name);

    // Assert
    account1Client.Should().NotBeNull();
    account2Client.Should().NotBeNull();
    account1Client.Should().NotBeSameAs(account2Client);
  }


  /// <summary>Verifies that named Analytics clients can coexist with the default (unnamed) client.</summary>
  [Fact]
  public void NamedClient_CoexistsWithDefaultClient()
  {
    // Arrange
    var services = CreateServiceCollection();

    // Register a default Cloudflare API client and Analytics client.
    services.AddCloudflareApiClient(options =>
    {
      options.ApiToken      = "default-token";
      options.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    services.AddCloudflareAnalytics();

    // Register a named Analytics client.
    services.AddCloudflareApiClient("named", o =>
    {
      o.ApiToken      = "named-token";
      o.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    services.AddCloudflareAnalytics("named");

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var defaultClient = serviceProvider.GetRequiredService<IAnalyticsApi>();
    var factory       = serviceProvider.GetRequiredService<IAnalyticsApiFactory>();
    var namedClient   = factory.CreateClient("named");

    // Assert
    defaultClient.Should().NotBeNull();
    namedClient.Should().NotBeNull();
    defaultClient.Should().NotBeSameAs(namedClient);
  }

  /// <summary>Verifies that disposing the factory disposes all cached GraphQL clients.</summary>
  [Fact]
  public void Dispose_DisposesAllCachedClients()
  {
    // Arrange
    const string clientName = "account1";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(clientName, options =>
    {
      options.ApiToken      = "test-token";
      options.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    services.AddCloudflareAnalytics(clientName);

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IAnalyticsApiFactory>();

    // Create a client to populate the cache.
    var client = factory.CreateClient(clientName);
    client.Should().NotBeNull();

    // Act
    ((IDisposable)factory).Dispose();

    // Assert - after disposal, creating a new client should throw.
    var action = () => factory.CreateClient(clientName);
    action.Should().Throw<ObjectDisposedException>();
  }

  /// <summary>
  ///   Verifies that a message handler added through the named registration's HttpClient builder callback receives the
  ///   named client's GraphQL request.
  /// </summary>
  [Fact]
  public async Task NamedClient_WithHttpClientBuilderCallback_SendsRequestsThroughTheAddedHandler()
  {
    // Arrange
    const string clientName = "account1";

    var services = CreateServiceCollection();
    var handler  = new AnsweringRequestCountingHandler();

    services.AddCloudflareApiClient(clientName, options =>
    {
      options.ApiToken      = "test-token";
      options.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    services.AddCloudflareAnalytics(clientName, builder => builder.AddHttpMessageHandler(() => handler));

    var serviceProvider = services.BuildServiceProvider();
    var client          = serviceProvider.GetRequiredService<IAnalyticsApiFactory>().CreateClient(clientName);

    // Act
    await client.SendQueryAsync<JsonElement>(new GraphQLRequest("{ viewer { __typename } }"));

    // Assert
    handler.RequestCount.Should().Be(1);
  }

  /// <summary>
  ///   Verifies that a message handler added through the default registration's HttpClient builder callback receives the
  ///   default client's GraphQL request.
  /// </summary>
  [Fact]
  public async Task DefaultClient_WithHttpClientBuilderCallback_SendsRequestsThroughTheAddedHandler()
  {
    // Arrange
    var services = CreateServiceCollection();
    var handler  = new AnsweringRequestCountingHandler();

    services.AddCloudflareApiClient(options =>
    {
      options.ApiToken      = "test-token";
      options.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
    });

    services.AddCloudflareAnalytics(builder => builder.AddHttpMessageHandler(() => handler));

    var serviceProvider = services.BuildServiceProvider();
    var client          = serviceProvider.GetRequiredService<IAnalyticsApi>();

    // Act
    await client.SendQueryAsync<JsonElement>(new GraphQLRequest("{ viewer { __typename } }"));

    // Assert
    handler.RequestCount.Should().Be(1);
  }

  /// <summary>Counts the requests it receives and answers each with an empty GraphQL result, never calling the network.</summary>
  private sealed class AnsweringRequestCountingHandler : DelegatingHandler
  {
    /// <summary>The number of requests received.</summary>
    public int RequestCount { get; private set; }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      RequestCount++;

      return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent("""{"data":{"viewer":{"__typename":"viewer"}}}""", Encoding.UTF8, "application/json")
      });
    }
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
