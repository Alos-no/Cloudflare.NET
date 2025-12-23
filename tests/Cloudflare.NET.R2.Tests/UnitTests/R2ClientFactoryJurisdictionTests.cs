namespace Cloudflare.NET.R2.Tests.UnitTests;

using Accounts.Models;
using Core;
using Exceptions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NET.Tests.Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>
///   Contains unit tests for the <see cref="IR2ClientFactory" /> jurisdiction-aware methods, including
///   <see cref="IR2ClientFactory.GetClient(R2Jurisdiction)" /> and
///   <see cref="IR2ClientFactory.GetClient(string, R2Jurisdiction)" />.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class R2ClientFactoryJurisdictionTests
{
  #region Properties & Fields - Non-Public

  private readonly ITestOutputHelper _output;

  #endregion


  #region Constructors

  public R2ClientFactoryJurisdictionTests(ITestOutputHelper output)
  {
    _output = output;
  }

  #endregion


  #region GetClient(R2Jurisdiction) Tests

  /// <summary>Verifies that GetClient with Default jurisdiction returns a valid client.</summary>
  [Fact]
  public void GetClient_WithDefaultJurisdiction_ReturnsClient()
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
      options.AccessKeyId     = "test-access-key";
      options.SecretAccessKey = "test-secret-key";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var client = factory.GetClient(R2Jurisdiction.Default);

    // Assert
    client.Should().NotBeNull();
    client.Should().BeOfType<R2Client>();
  }


  /// <summary>Verifies that GetClient with EU jurisdiction returns a valid client.</summary>
  [Fact]
  public void GetClient_WithEuJurisdiction_ReturnsClient()
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
      options.AccessKeyId     = "test-access-key";
      options.SecretAccessKey = "test-secret-key";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var client = factory.GetClient(R2Jurisdiction.EuropeanUnion);

    // Assert
    client.Should().NotBeNull();
    client.Should().BeOfType<R2Client>();
  }


  /// <summary>Verifies that GetClient with FedRAMP jurisdiction returns a valid client.</summary>
  [Fact]
  public void GetClient_WithFedRampJurisdiction_ReturnsClient()
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
      options.AccessKeyId     = "test-access-key";
      options.SecretAccessKey = "test-secret-key";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var client = factory.GetClient(R2Jurisdiction.FedRamp);

    // Assert
    client.Should().NotBeNull();
    client.Should().BeOfType<R2Client>();
  }


  /// <summary>Verifies that GetClient caches clients by jurisdiction.</summary>
  [Fact]
  public void GetClient_CalledTwiceWithSameJurisdiction_ReturnsCachedInstance()
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
      options.AccessKeyId     = "test-access-key";
      options.SecretAccessKey = "test-secret-key";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var client1 = factory.GetClient(R2Jurisdiction.EuropeanUnion);
    var client2 = factory.GetClient(R2Jurisdiction.EuropeanUnion);

    // Assert
    client1.Should().BeSameAs(client2);
  }


  /// <summary>Verifies that GetClient with different jurisdictions returns different clients.</summary>
  [Fact]
  public void GetClient_WithDifferentJurisdictions_ReturnsDifferentClients()
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
      options.AccessKeyId     = "test-access-key";
      options.SecretAccessKey = "test-secret-key";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var defaultClient = factory.GetClient(R2Jurisdiction.Default);
    var euClient      = factory.GetClient(R2Jurisdiction.EuropeanUnion);
    var fedRampClient = factory.GetClient(R2Jurisdiction.FedRamp);

    // Assert
    defaultClient.Should().NotBeSameAs(euClient);
    defaultClient.Should().NotBeSameAs(fedRampClient);
    euClient.Should().NotBeSameAs(fedRampClient);
  }


  /// <summary>
  ///   Verifies that GetClient with jurisdiction throws OptionsValidationException when default credentials are missing.
  ///   Note: Default (unnamed) clients use ValidateOnStart() which throws OptionsValidationException,
  ///   while named clients throw CloudflareR2ConfigurationException via factory validation.
  /// </summary>
  [Fact]
  public void GetClient_WithMissingCredentials_ThrowsOptionsValidationException()
  {
    // Arrange
    var services = CreateServiceCollection();

    // Register Cloudflare options but NOT R2 credentials
    services.AddCloudflareApiClient(options =>
    {
      options.AccountId = "test-account-id";
      options.ApiToken  = "test-token";
    });

    // Register R2 client with empty credentials (not providing valid credentials)
    services.AddCloudflareR2Client(options =>
    {
      options.AccessKeyId     = "";
      options.SecretAccessKey = "";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var action = () => factory.GetClient(R2Jurisdiction.Default);

    // Assert - Default clients use ValidateOnStart() which throws OptionsValidationException
    action.Should().Throw<Microsoft.Extensions.Options.OptionsValidationException>()
          .WithMessage("*AccessKeyId is required*");
  }


  /// <summary>Verifies that GetClient throws when the factory is disposed.</summary>
  [Fact]
  public void GetClient_AfterDispose_ThrowsObjectDisposedException()
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
      options.AccessKeyId     = "test-access-key";
      options.SecretAccessKey = "test-secret-key";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    ((IDisposable)factory).Dispose();

    var action = () => factory.GetClient(R2Jurisdiction.Default);

    // Assert
    action.Should().Throw<ObjectDisposedException>();
  }

  #endregion


  #region GetClient(string name, R2Jurisdiction) Tests

  /// <summary>Verifies that GetClient with name and jurisdiction returns a valid client.</summary>
  [Fact]
  public void GetClient_WithNameAndJurisdiction_ReturnsClient()
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
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var client = factory.GetClient(clientName, R2Jurisdiction.EuropeanUnion);

    // Assert
    client.Should().NotBeNull();
    client.Should().BeOfType<R2Client>();
  }


  /// <summary>
  ///   Verifies that GetClient with name and jurisdiction caches by composite key (name, jurisdiction).
  /// </summary>
  [Fact]
  public void GetClient_CalledTwiceWithSameNameAndJurisdiction_ReturnsCachedInstance()
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
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var client1 = factory.GetClient(clientName, R2Jurisdiction.EuropeanUnion);
    var client2 = factory.GetClient(clientName, R2Jurisdiction.EuropeanUnion);

    // Assert
    client1.Should().BeSameAs(client2);
  }


  /// <summary>
  ///   Verifies that GetClient with same name but different jurisdictions returns different clients.
  /// </summary>
  [Fact]
  public void GetClient_SameNameDifferentJurisdictions_ReturnsDifferentClients()
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
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var euClient      = factory.GetClient(clientName, R2Jurisdiction.EuropeanUnion);
    var fedRampClient = factory.GetClient(clientName, R2Jurisdiction.FedRamp);

    // Assert
    euClient.Should().NotBeSameAs(fedRampClient);
  }


  /// <summary>
  ///   Verifies that GetClient with different names but same jurisdiction returns different clients.
  /// </summary>
  [Fact]
  public void GetClient_DifferentNamesSameJurisdiction_ReturnsDifferentClients()
  {
    // Arrange
    const string primaryName = "primary";
    const string backupName  = "backup";

    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(primaryName, options =>
    {
      options.AccountId = "primary-account-id";
      options.ApiToken  = "primary-token";
    });

    services.AddCloudflareR2Client(primaryName, options =>
    {
      options.AccessKeyId     = "primary-key";
      options.SecretAccessKey = "primary-secret";
    });

    services.AddCloudflareApiClient(backupName, options =>
    {
      options.AccountId = "backup-account-id";
      options.ApiToken  = "backup-token";
    });

    services.AddCloudflareR2Client(backupName, options =>
    {
      options.AccessKeyId     = "backup-key";
      options.SecretAccessKey = "backup-secret";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var primaryEuClient = factory.GetClient(primaryName, R2Jurisdiction.EuropeanUnion);
    var backupEuClient  = factory.GetClient(backupName, R2Jurisdiction.EuropeanUnion);

    // Assert
    primaryEuClient.Should().NotBeSameAs(backupEuClient);
  }


  /// <summary>Verifies that GetClient throws ArgumentException when name is null.</summary>
  [Fact]
  public void GetClient_WithNullName_ThrowsArgumentException()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient("valid", o => o.ApiToken = "token");
    services.AddCloudflareR2Client("valid", options =>
    {
      options.AccessKeyId     = "key";
      options.SecretAccessKey = "secret";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var action = () => factory.GetClient(null!, R2Jurisdiction.Default);

    // Assert
    action.Should().Throw<ArgumentException>();
  }


  /// <summary>Verifies that GetClient throws ArgumentException when name is empty.</summary>
  [Fact]
  public void GetClient_WithEmptyName_ThrowsArgumentException()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient("valid", o => o.ApiToken = "token");
    services.AddCloudflareR2Client("valid", options =>
    {
      options.AccessKeyId     = "key";
      options.SecretAccessKey = "secret";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var action = () => factory.GetClient("", R2Jurisdiction.Default);

    // Assert
    action.Should().Throw<ArgumentException>();
  }


  /// <summary>Verifies that GetClient throws ArgumentException when name is whitespace.</summary>
  [Fact]
  public void GetClient_WithWhitespaceName_ThrowsArgumentException()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient("valid", o => o.ApiToken = "token");
    services.AddCloudflareR2Client("valid", options =>
    {
      options.AccessKeyId     = "key";
      options.SecretAccessKey = "secret";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var action = () => factory.GetClient("   ", R2Jurisdiction.Default);

    // Assert
    action.Should().Throw<ArgumentException>();
  }


  /// <summary>
  ///   Verifies that GetClient with name and jurisdiction throws when the factory is disposed.
  /// </summary>
  [Fact]
  public void GetClient_NamedAfterDispose_ThrowsObjectDisposedException()
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
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    ((IDisposable)factory).Dispose();

    var action = () => factory.GetClient(clientName, R2Jurisdiction.Default);

    // Assert
    action.Should().Throw<ObjectDisposedException>();
  }

  #endregion


  #region GetClient(name) with Configured Jurisdiction Tests

  /// <summary>
  ///   Verifies that GetClient(name) uses the configured jurisdiction from R2Settings.
  /// </summary>
  [Fact]
  public void GetClient_UsesConfiguredJurisdiction()
  {
    // Arrange
    const string clientName = "eu-client";

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
      options.Jurisdiction    = R2Jurisdiction.EuropeanUnion;
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var client = factory.GetClient(clientName);

    // Assert
    client.Should().NotBeNull();
    client.Should().BeOfType<R2Client>();
  }


  /// <summary>
  ///   Verifies that GetClient(name) caches by the configured jurisdiction.
  /// </summary>
  [Fact]
  public void GetClient_CachesByConfiguredJurisdiction()
  {
    // Arrange
    const string clientName = "eu-client";

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
      options.Jurisdiction    = R2Jurisdiction.EuropeanUnion;
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var client1 = factory.GetClient(clientName);
    var client2 = factory.GetClient(clientName);

    // Assert
    client1.Should().BeSameAs(client2);
  }


  /// <summary>
  ///   Verifies that clients with different configured jurisdictions are cached separately.
  /// </summary>
  [Fact]
  public void GetClient_DifferentConfiguredJurisdictions_CachedSeparately()
  {
    // Arrange
    const string euClientName      = "eu-client";
    const string fedRampClientName = "fedramp-client";

    var services = CreateServiceCollection();

    // EU client
    services.AddCloudflareApiClient(euClientName, options =>
    {
      options.AccountId = "test-account-id";
      options.ApiToken  = "test-token";
    });

    services.AddCloudflareR2Client(euClientName, options =>
    {
      options.AccessKeyId     = "test-access-key";
      options.SecretAccessKey = "test-secret-key";
      options.Jurisdiction    = R2Jurisdiction.EuropeanUnion;
    });

    // FedRAMP client
    services.AddCloudflareApiClient(fedRampClientName, options =>
    {
      options.AccountId = "test-account-id";
      options.ApiToken  = "test-token";
    });

    services.AddCloudflareR2Client(fedRampClientName, options =>
    {
      options.AccessKeyId     = "test-access-key";
      options.SecretAccessKey = "test-secret-key";
      options.Jurisdiction    = R2Jurisdiction.FedRamp;
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var euClient      = factory.GetClient(euClientName);
    var fedRampClient = factory.GetClient(fedRampClientName);

    // Assert
    euClient.Should().NotBeSameAs(fedRampClient);
  }


  /// <summary>
  ///   Verifies that GetClient(name) using configured jurisdiction and GetClient(name, differentJurisdiction)
  ///   with an override return different cached instances.
  /// </summary>
  [Fact]
  public void GetClient_ConfiguredJurisdictionVsOverride_ReturnsDifferentClients()
  {
    // Arrange
    const string clientName = "prod";

    var services = CreateServiceCollection();

    // Configure "prod" with EU jurisdiction
    services.AddCloudflareApiClient(clientName, options =>
    {
      options.AccountId = "test-account";
      options.ApiToken  = "test-token";
    });

    services.AddCloudflareR2Client(clientName, options =>
    {
      options.AccessKeyId     = "test-key";
      options.SecretAccessKey = "test-secret";
      options.Jurisdiction    = R2Jurisdiction.EuropeanUnion; // Configured as EU
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var configuredClient = factory.GetClient(clientName);                            // Uses configured EU jurisdiction
    var overrideClient   = factory.GetClient(clientName, R2Jurisdiction.FedRamp);    // Overrides to FedRAMP

    // Assert - Different clients because different cache keys: ("prod", EU) vs ("prod", FedRAMP)
    configuredClient.Should().NotBeSameAs(overrideClient,
      "GetClient(name) using configured jurisdiction should return a different instance than GetClient(name, differentJurisdiction)");
  }

  #endregion


  #region Mixed Usage Pattern Tests

  /// <summary>
  ///   Verifies that GetClient(jurisdiction) and GetClient(name, jurisdiction) work together
  ///   and produce correctly cached clients.
  /// </summary>
  [Fact]
  public void MixedUsage_DefaultAndNamedClients_CacheSeparately()
  {
    // Arrange
    const string namedClient = "named";

    var services = CreateServiceCollection();

    // Default client
    services.AddCloudflareApiClient(options =>
    {
      options.AccountId = "default-account";
      options.ApiToken  = "default-token";
    });

    services.AddCloudflareR2Client(options =>
    {
      options.AccessKeyId     = "default-key";
      options.SecretAccessKey = "default-secret";
    });

    // Named client
    services.AddCloudflareApiClient(namedClient, options =>
    {
      options.AccountId = "named-account";
      options.ApiToken  = "named-token";
    });

    services.AddCloudflareR2Client(namedClient, options =>
    {
      options.AccessKeyId     = "named-key";
      options.SecretAccessKey = "named-secret";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Act
    var defaultEuClient = factory.GetClient(R2Jurisdiction.EuropeanUnion);
    var namedEuClient   = factory.GetClient(namedClient, R2Jurisdiction.EuropeanUnion);

    // Assert - Different clients (different credentials/accounts)
    defaultEuClient.Should().NotBeSameAs(namedEuClient);
  }


  /// <summary>
  ///   Verifies that disposing the factory disposes all jurisdiction-specific cached clients.
  /// </summary>
  [Fact]
  public void Dispose_DisposesAllJurisdictionCachedClients()
  {
    // Arrange
    var services = CreateServiceCollection();

    services.AddCloudflareApiClient(options =>
    {
      options.AccountId = "test-account";
      options.ApiToken  = "test-token";
    });

    services.AddCloudflareR2Client(options =>
    {
      options.AccessKeyId     = "test-key";
      options.SecretAccessKey = "test-secret";
    });

    var serviceProvider = services.BuildServiceProvider();
    var factory         = serviceProvider.GetRequiredService<IR2ClientFactory>();

    // Create clients for multiple jurisdictions to populate cache
    var defaultClient = factory.GetClient(R2Jurisdiction.Default);
    var euClient      = factory.GetClient(R2Jurisdiction.EuropeanUnion);
    var fedRampClient = factory.GetClient(R2Jurisdiction.FedRamp);

    defaultClient.Should().NotBeNull();
    euClient.Should().NotBeNull();
    fedRampClient.Should().NotBeNull();

    // Act
    ((IDisposable)factory).Dispose();

    // Assert - All factory methods should throw after disposal
    var action1 = () => factory.GetClient(R2Jurisdiction.Default);
    var action2 = () => factory.GetClient(R2Jurisdiction.EuropeanUnion);

    action1.Should().Throw<ObjectDisposedException>();
    action2.Should().Throw<ObjectDisposedException>();
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
