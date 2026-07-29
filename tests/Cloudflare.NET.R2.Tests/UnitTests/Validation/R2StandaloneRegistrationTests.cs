namespace Cloudflare.NET.R2.Tests.UnitTests.Validation;

using Core;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NET.Tests.Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>
///   Contains unit tests proving that the R2 client can be registered standalone from configuration, without a
///   preceding <c>AddCloudflareApiClient</c> call. The <c>IConfiguration</c> overloads are documented to bind both the
///   "R2" section (credentials) and the "Cloudflare" section (Account ID), so a consumer following the documented
///   <c>appsettings.json</c> shape must get a working client from a single registration call.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class R2StandaloneRegistrationTests
{
  #region Constants & Statics

  /// <summary>A syntactically valid Cloudflare account identifier used across the standalone registration tests.</summary>
  private const string TestAccountId = "0123456789abcdef0123456789abcdef";

  #endregion


  #region Properties & Fields - Non-Public

  private readonly ITestOutputHelper _output;

  #endregion


  #region Constructors

  public R2StandaloneRegistrationTests(ITestOutputHelper output)
  {
    _output = output;
  }

  #endregion


  #region Methods

  /// <summary>
  ///   Verifies that the default client resolves when only <c>AddCloudflareR2Client(IConfiguration)</c> is called and
  ///   the configuration carries the documented "Cloudflare" and "R2" sections.
  /// </summary>
  [Fact]
  public void AddCloudflareR2Client_FromConfigurationAlone_ResolvesClient()
  {
    // Arrange - configuration matching the documented appsettings.json shape.
    var configuration = BuildConfiguration(new Dictionary<string, string?>
    {
      ["Cloudflare:AccountId"] = TestAccountId,
      ["R2:AccessKeyId"]       = "test-access-key",
      ["R2:SecretAccessKey"]   = "test-secret-key",
    });

    var services = CreateServiceCollection();
    services.AddCloudflareR2Client(configuration);

    using var serviceProvider = services.BuildServiceProvider();

    // Act
    var client            = serviceProvider.GetRequiredService<IR2Client>();
    var cloudflareOptions = serviceProvider.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;

    // Assert - the client resolves and the Account ID was bound from the "Cloudflare" section.
    client.Should().NotBeNull();
    cloudflareOptions.AccountId.Should().Be(TestAccountId);
  }

  /// <summary>
  ///   Verifies that a named client resolves when only <c>AddCloudflareR2Client(name, IConfiguration)</c> is called,
  ///   binding "R2:{name}" for credentials and "Cloudflare:{name}" for the Account ID as documented.
  /// </summary>
  [Fact]
  public void AddCloudflareR2Client_NamedFromConfigurationAlone_ResolvesClient()
  {
    // Arrange - named sections as documented on the overload ("R2:{name}" and "Cloudflare:{name}").
    var configuration = BuildConfiguration(new Dictionary<string, string?>
    {
      ["Cloudflare:backup:AccountId"] = TestAccountId,
      ["R2:backup:AccessKeyId"]       = "backup-access-key",
      ["R2:backup:SecretAccessKey"]   = "backup-secret-key",
    });

    var services = CreateServiceCollection();
    services.AddCloudflareR2Client("backup", configuration);

    using var serviceProvider = services.BuildServiceProvider();

    // Act
    var client            = serviceProvider.GetRequiredKeyedService<IR2Client>("backup");
    var cloudflareOptions = serviceProvider.GetRequiredService<IOptionsMonitor<CloudflareApiOptions>>().Get("backup");

    // Assert - the named client resolves and the Account ID was bound from "Cloudflare:{name}".
    client.Should().NotBeNull();
    cloudflareOptions.AccountId.Should().Be(TestAccountId);
  }

  /// <summary>
  ///   Verifies that registering both the REST client and the R2 client from the same configuration keeps working:
  ///   both bind the same "Cloudflare" section, so the double bind must be harmless.
  /// </summary>
  [Fact]
  public void AddCloudflareR2Client_AlongsideApiClient_ResolvesBothClients()
  {
    // Arrange - the combined registration used by consumers who need both control plane and data plane.
    var configuration = BuildConfiguration(new Dictionary<string, string?>
    {
      ["Cloudflare:AccountId"] = TestAccountId,
      ["Cloudflare:ApiToken"]  = "test-api-token",
      ["R2:AccessKeyId"]       = "test-access-key",
      ["R2:SecretAccessKey"]   = "test-secret-key",
    });

    var services = CreateServiceCollection();
    services.AddCloudflareApiClient(configuration);
    services.AddCloudflareR2Client(configuration);

    using var serviceProvider = services.BuildServiceProvider();

    // Act
    var r2Client          = serviceProvider.GetRequiredService<IR2Client>();
    var cloudflareOptions = serviceProvider.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;

    // Assert - both registrations coexist and the shared options carry both values.
    r2Client.Should().NotBeNull();
    cloudflareOptions.AccountId.Should().Be(TestAccountId);
    cloudflareOptions.ApiToken.Should().Be("test-api-token");
  }

  /// <summary>Builds an in-memory configuration from the given key-value pairs.</summary>
  private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
  {
    return new ConfigurationBuilder()
           .AddInMemoryCollection(values)
           .Build();
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
