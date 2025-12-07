namespace Cloudflare.NET.R2.Tests.UnitTests;

using Core;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NET.Tests.Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>
///   Contains unit tests for the named R2 client feature, including the <see cref="IR2ClientFactory" /> and keyed
///   services registration.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class NamedR2ClientTests
{
  #region Properties & Fields - Non-Public

  private readonly ITestOutputHelper _output;

  #endregion

  #region Constructors

  public NamedR2ClientTests(ITestOutputHelper output)
  {
    _output = output;
  }

  #endregion

  #region Methods

  /// <summary>Verifies that the factory can create a named R2 client with the correct configuration.</summary>
  [Fact]
  public void CreateClient_WithRegisteredName_ReturnsClient()
  {
    // Arrange
    const string clientName = "primary";

    var services = CreateServiceCollection();

    // Register the Cloudflare API options (required for Account ID).
    services.AddCloudflareApiClient(clientName, options =>
    {
      options.AccountId = "test-account-id";
      options.ApiToken  = "test-token";
    });

    // Register the named R2 client.
    services.AddCloudflareR2Client(clientName, options =>
    {
      options.AccessKeyId     = "test-access-key";
      options.SecretAccessKey = "test-secret-key";
      options.EndpointUrl     = "https://{0}.r2.cloudflarestorage.com";
      options.Region          = "auto";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var client = factory.CreateClient(clientName);

    // Assert
    client.Should().NotBeNull();
    client.Should().BeOfType<R2Client>();
  }


  /// <summary>
  ///   Verifies that the factory throws an appropriate exception when creating a client with an unregistered name
  ///   (missing R2 settings).
  /// </summary>
  [Fact]
  public void CreateClient_WithUnregisteredName_ThrowsInvalidOperationException()
  {
    // Arrange
    const string registeredName   = "primary";
    const string unregisteredName = "backup";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(registeredName, options =>
    {
      options.AccountId = "test-account-id";
      options.ApiToken  = "test-token";
    });

    services.AddCloudflareR2Client(registeredName, options =>
    {
      options.AccessKeyId     = "test-access-key";
      options.SecretAccessKey = "test-secret-key";
      options.EndpointUrl     = "https://{0}.r2.cloudflarestorage.com";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var action = () => factory.CreateClient(unregisteredName);

    // Assert
    action.Should().Throw<InvalidOperationException>()
          .WithMessage($"*No R2 client configuration found for name '{unregisteredName}'*");
  }


  /// <summary>Verifies that the factory throws when the Cloudflare Account ID is missing for the named client.</summary>
  [Fact]
  public void CreateClient_WithMissingAccountId_ThrowsInvalidOperationException()
  {
    // Arrange
    const string clientName = "primary";

    var services = CreateServiceCollection();

    // Register R2 settings but NOT the Cloudflare API options with Account ID.
    services.AddCloudflareR2Client(clientName, options =>
    {
      options.AccessKeyId     = "test-access-key";
      options.SecretAccessKey = "test-secret-key";
      options.EndpointUrl     = "https://{0}.r2.cloudflarestorage.com";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var action = () => factory.CreateClient(clientName);

    // Assert
    action.Should().Throw<InvalidOperationException>()
          .WithMessage("*Account ID is missing*");
  }


  /// <summary>Verifies that the factory caches clients and returns the same instance for the same name.</summary>
  [Fact]
  public void CreateClient_CalledTwiceWithSameName_ReturnsCachedInstance()
  {
    // Arrange
    const string clientName = "primary";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(clientName, options =>
    {
      options.AccountId = "test-account-id";
      options.ApiToken  = "test-token";
    });

    services.AddCloudflareR2Client(clientName, options =>
    {
      options.AccessKeyId     = "test-access-key";
      options.SecretAccessKey = "test-secret-key";
      options.EndpointUrl     = "https://{0}.r2.cloudflarestorage.com";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

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

    services.AddCloudflareApiClient("valid", o => o.ApiToken = "token");
    services.AddCloudflareR2Client("valid", options =>
    {
      options.AccessKeyId     = "key";
      options.SecretAccessKey = "secret";
      options.EndpointUrl     = "https://{0}.r2.cloudflarestorage.com";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var action = () => factory.CreateClient(invalidName!);

    // Assert
    action.Should().Throw<ArgumentException>();
  }

  /// <summary>Verifies that named R2 clients can be resolved using keyed services.</summary>
  [Fact]
  public void KeyedServices_WithRegisteredName_ResolvesCorrectClient()
  {
    // Arrange
    const string clientName = "primary";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(clientName, options =>
    {
      options.AccountId = "test-account-id";
      options.ApiToken  = "test-token";
    });

    services.AddCloudflareR2Client(clientName, options =>
    {
      options.AccessKeyId     = "test-access-key";
      options.SecretAccessKey = "test-secret-key";
      options.EndpointUrl     = "https://{0}.r2.cloudflarestorage.com";
    });

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var client = serviceProvider.GetKeyedService<IR2Client>(clientName);

    // Assert
    client.Should().NotBeNull();
    client.Should().BeOfType<R2Client>();
  }


  /// <summary>Verifies that keyed services returns null for unregistered names.</summary>
  [Fact]
  public void KeyedServices_WithUnregisteredName_ReturnsNull()
  {
    // Arrange
    const string registeredName   = "primary";
    const string unregisteredName = "backup";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(registeredName, o =>
    {
      o.AccountId = "account";
      o.ApiToken  = "token";
    });

    services.AddCloudflareR2Client(registeredName, options =>
    {
      options.AccessKeyId     = "key";
      options.SecretAccessKey = "secret";
      options.EndpointUrl     = "https://{0}.r2.cloudflarestorage.com";
    });

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var client = serviceProvider.GetKeyedService<IR2Client>(unregisteredName);

    // Assert
    client.Should().BeNull();
  }

  /// <summary>Verifies that multiple named R2 clients can be registered and resolved independently.</summary>
  [Fact]
  public void CreateClient_WithMultipleNames_ReturnsDistinctClients()
  {
    // Arrange
    const string primaryName = "primary";
    const string backupName  = "backup";

    var services = CreateServiceCollection();

    // Register primary.
    services.AddCloudflareApiClient(primaryName, o =>
    {
      o.AccountId = "primary-account";
      o.ApiToken  = "primary-token";
    });

    services.AddCloudflareR2Client(primaryName, options =>
    {
      options.AccessKeyId     = "primary-key";
      options.SecretAccessKey = "primary-secret";
      options.EndpointUrl     = "https://{0}.r2.cloudflarestorage.com";
    });

    // Register backup.
    services.AddCloudflareApiClient(backupName, o =>
    {
      o.AccountId = "backup-account";
      o.ApiToken  = "backup-token";
    });

    services.AddCloudflareR2Client(backupName, options =>
    {
      options.AccessKeyId     = "backup-key";
      options.SecretAccessKey = "backup-secret";
      options.EndpointUrl     = "https://{0}.r2.cloudflarestorage.com";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var primaryClient = factory.CreateClient(primaryName);
    var backupClient  = factory.CreateClient(backupName);

    // Assert
    primaryClient.Should().NotBeNull();
    backupClient.Should().NotBeNull();
    primaryClient.Should().NotBeSameAs(backupClient);
  }


  /// <summary>Verifies that named R2 clients can coexist with the default (unnamed) client.</summary>
  [Fact]
  public void NamedClient_CoexistsWithDefaultClient()
  {
    // Arrange
    var services = CreateServiceCollection();

    // Register a default Cloudflare API client and R2 client.
    services.AddCloudflareApiClient(options =>
    {
      options.AccountId = "default-account";
      options.ApiToken  = "default-token";
    });

    services.AddCloudflareR2Client(options =>
    {
      options.AccessKeyId     = "default-key";
      options.SecretAccessKey = "default-secret";
      options.EndpointUrl     = "https://{0}.r2.cloudflarestorage.com";
    });

    // Register a named R2 client.
    services.AddCloudflareApiClient("named", o =>
    {
      o.AccountId = "named-account";
      o.ApiToken  = "named-token";
    });

    services.AddCloudflareR2Client("named", options =>
    {
      options.AccessKeyId     = "named-key";
      options.SecretAccessKey = "named-secret";
      options.EndpointUrl     = "https://{0}.r2.cloudflarestorage.com";
    });

    var serviceProvider = services.BuildServiceProvider();

    // Act
    var defaultClient = serviceProvider.GetRequiredService<IR2Client>();
    var factory       = serviceProvider.GetRequiredService<IR2ClientFactory>();
    var namedClient   = factory.CreateClient("named");

    // Assert
    defaultClient.Should().NotBeNull();
    namedClient.Should().NotBeNull();
    defaultClient.Should().NotBeSameAs(namedClient);
  }

  /// <summary>Verifies that disposing the factory disposes all cached clients.</summary>
  [Fact]
  public void Dispose_DisposesAllCachedClients()
  {
    // Arrange
    const string clientName = "primary";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(clientName, options =>
    {
      options.AccountId = "test-account-id";
      options.ApiToken  = "test-token";
    });

    services.AddCloudflareR2Client(clientName, options =>
    {
      options.AccessKeyId     = "test-access-key";
      options.SecretAccessKey = "test-secret-key";
      options.EndpointUrl     = "https://{0}.r2.cloudflarestorage.com";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Create a client to populate the cache.
    var client = factory.CreateClient(clientName);
    client.Should().NotBeNull();

    // Act
    ((IDisposable)factory).Dispose();

    // Assert - after disposal, creating a new client should throw.
    var action = () => factory.CreateClient(clientName);
    action.Should().Throw<ObjectDisposedException>();
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
