namespace Cloudflare.NET.Tests.UnitTests;

using Accounts.Models;
using Shared.Fixtures;

/// <summary>
///   Contains unit tests for <see cref="R2Jurisdiction" /> S3 endpoint helper methods.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class R2JurisdictionTests
{
  #region GetS3EndpointUrl Tests

  /// <summary>Verifies that Default jurisdiction returns the global (non-subdomain) endpoint.</summary>
  [Fact]
  public void GetS3EndpointUrl_DefaultJurisdiction_ReturnsGlobalEndpoint()
  {
    // Arrange
    const string accountId = "abc123";
    var jurisdiction = R2Jurisdiction.Default;

    // Act
    var endpointUrl = jurisdiction.GetS3EndpointUrl(accountId);

    // Assert
    endpointUrl.Should().Be("https://abc123.r2.cloudflarestorage.com");
  }


  /// <summary>Verifies that EuropeanUnion jurisdiction returns the EU-specific endpoint.</summary>
  [Fact]
  public void GetS3EndpointUrl_EuropeanUnionJurisdiction_ReturnsEuEndpoint()
  {
    // Arrange
    const string accountId = "abc123";
    var jurisdiction = R2Jurisdiction.EuropeanUnion;

    // Act
    var endpointUrl = jurisdiction.GetS3EndpointUrl(accountId);

    // Assert
    endpointUrl.Should().Be("https://abc123.eu.r2.cloudflarestorage.com");
  }


  /// <summary>Verifies that FedRamp jurisdiction returns the FedRAMP-specific endpoint.</summary>
  [Fact]
  public void GetS3EndpointUrl_FedRampJurisdiction_ReturnsFedRampEndpoint()
  {
    // Arrange
    const string accountId = "abc123";
    var jurisdiction = R2Jurisdiction.FedRamp;

    // Act
    var endpointUrl = jurisdiction.GetS3EndpointUrl(accountId);

    // Assert
    endpointUrl.Should().Be("https://abc123.fedramp.r2.cloudflarestorage.com");
  }


  /// <summary>Verifies that custom (future) jurisdiction values produce correct endpoint URLs.</summary>
  [Fact]
  public void GetS3EndpointUrl_CustomJurisdiction_ReturnsCustomEndpoint()
  {
    // Arrange - Simulating a future jurisdiction not yet defined in SDK
    const string accountId = "abc123";
    R2Jurisdiction jurisdiction = "apac";

    // Act
    var endpointUrl = jurisdiction.GetS3EndpointUrl(accountId);

    // Assert
    endpointUrl.Should().Be("https://abc123.apac.r2.cloudflarestorage.com");
  }


  /// <summary>Verifies that empty jurisdiction value (constructed via default) is treated as Default.</summary>
  [Fact]
  public void GetS3EndpointUrl_EmptyJurisdiction_ReturnsGlobalEndpoint()
  {
    // Arrange
    const string accountId = "abc123";
    var jurisdiction = default(R2Jurisdiction);

    // Act
    var endpointUrl = jurisdiction.GetS3EndpointUrl(accountId);

    // Assert
    endpointUrl.Should().Be("https://abc123.r2.cloudflarestorage.com");
  }


  /// <summary>Verifies that GetS3EndpointUrl throws when accountId is null.</summary>
  [Fact]
  public void GetS3EndpointUrl_NullAccountId_ThrowsArgumentException()
  {
    // Arrange
    var jurisdiction = R2Jurisdiction.Default;

    // Act
    var action = () => jurisdiction.GetS3EndpointUrl(null!);

    // Assert
    action.Should().Throw<ArgumentException>()
          .WithParameterName("accountId");
  }


  /// <summary>Verifies that GetS3EndpointUrl throws when accountId is empty.</summary>
  [Fact]
  public void GetS3EndpointUrl_EmptyAccountId_ThrowsArgumentException()
  {
    // Arrange
    var jurisdiction = R2Jurisdiction.Default;

    // Act
    var action = () => jurisdiction.GetS3EndpointUrl("");

    // Assert
    action.Should().Throw<ArgumentException>()
          .WithParameterName("accountId");
  }


  /// <summary>Verifies that GetS3EndpointUrl throws when accountId is whitespace.</summary>
  [Fact]
  public void GetS3EndpointUrl_WhitespaceAccountId_ThrowsArgumentException()
  {
    // Arrange
    var jurisdiction = R2Jurisdiction.Default;

    // Act
    var action = () => jurisdiction.GetS3EndpointUrl("   ");

    // Assert
    action.Should().Throw<ArgumentException>()
          .WithParameterName("accountId");
  }


  /// <summary>Verifies that all known jurisdictions produce correct S3 endpoints.</summary>
  [Theory]
  [InlineData("default", "abc123", "https://abc123.r2.cloudflarestorage.com")]
  [InlineData("eu", "abc123", "https://abc123.eu.r2.cloudflarestorage.com")]
  [InlineData("fedramp", "abc123", "https://abc123.fedramp.r2.cloudflarestorage.com")]
  [InlineData("EU", "abc123", "https://abc123.eu.r2.cloudflarestorage.com")]
  [InlineData("FEDRAMP", "abc123", "https://abc123.fedramp.r2.cloudflarestorage.com")]
  public void GetS3EndpointUrl_WithVariousJurisdictions_ReturnsCorrectEndpoint(string jurisdictionValue,
                                                                                string accountId,
                                                                                string expectedUrl)
  {
    // Arrange
    var jurisdiction = new R2Jurisdiction(jurisdictionValue);

    // Act
    var endpointUrl = jurisdiction.GetS3EndpointUrl(accountId);

    // Assert
    endpointUrl.Should().Be(expectedUrl);
  }

  #endregion


  #region GetS3Subdomain Tests

  /// <summary>Verifies that Default jurisdiction returns null subdomain.</summary>
  [Fact]
  public void GetS3Subdomain_DefaultJurisdiction_ReturnsNull()
  {
    // Arrange
    var jurisdiction = R2Jurisdiction.Default;

    // Act
    var subdomain = jurisdiction.GetS3Subdomain();

    // Assert
    subdomain.Should().BeNull();
  }


  /// <summary>Verifies that empty/default-constructed jurisdiction returns null subdomain.</summary>
  [Fact]
  public void GetS3Subdomain_EmptyJurisdiction_ReturnsNull()
  {
    // Arrange
    var jurisdiction = default(R2Jurisdiction);

    // Act
    var subdomain = jurisdiction.GetS3Subdomain();

    // Assert
    subdomain.Should().BeNull();
  }


  /// <summary>Verifies that EuropeanUnion jurisdiction returns "eu" subdomain.</summary>
  [Fact]
  public void GetS3Subdomain_EuropeanUnionJurisdiction_ReturnsEu()
  {
    // Arrange
    var jurisdiction = R2Jurisdiction.EuropeanUnion;

    // Act
    var subdomain = jurisdiction.GetS3Subdomain();

    // Assert
    subdomain.Should().Be("eu");
  }


  /// <summary>Verifies that FedRamp jurisdiction returns "fedramp" subdomain.</summary>
  [Fact]
  public void GetS3Subdomain_FedRampJurisdiction_ReturnsFedramp()
  {
    // Arrange
    var jurisdiction = R2Jurisdiction.FedRamp;

    // Act
    var subdomain = jurisdiction.GetS3Subdomain();

    // Assert
    subdomain.Should().Be("fedramp");
  }


  /// <summary>Verifies that custom jurisdiction returns lowercase subdomain.</summary>
  [Fact]
  public void GetS3Subdomain_CustomJurisdiction_ReturnsLowercaseValue()
  {
    // Arrange
    R2Jurisdiction jurisdiction = "APAC";

    // Act
    var subdomain = jurisdiction.GetS3Subdomain();

    // Assert
    subdomain.Should().Be("apac");
  }


  /// <summary>Verifies that "default" string value (case-insensitive) returns null subdomain.</summary>
  [Theory]
  [InlineData("default")]
  [InlineData("DEFAULT")]
  [InlineData("Default")]
  public void GetS3Subdomain_DefaultStringValue_ReturnsNull(string jurisdictionValue)
  {
    // Arrange
    var jurisdiction = new R2Jurisdiction(jurisdictionValue);

    // Act
    var subdomain = jurisdiction.GetS3Subdomain();

    // Assert
    subdomain.Should().BeNull();
  }

  #endregion


  #region Static Property Tests

  /// <summary>Verifies that static jurisdiction properties have correct values.</summary>
  [Fact]
  public void StaticProperties_HaveCorrectValues()
  {
    // Assert
    R2Jurisdiction.Default.Value.Should().Be("default");
    R2Jurisdiction.EuropeanUnion.Value.Should().Be("eu");
    R2Jurisdiction.FedRamp.Value.Should().Be("fedramp");
  }


  /// <summary>Verifies that static jurisdiction properties produce correct endpoints.</summary>
  [Fact]
  public void StaticProperties_ProduceCorrectEndpoints()
  {
    // Arrange
    const string accountId = "test-account";

    // Assert
    R2Jurisdiction.Default.GetS3EndpointUrl(accountId).Should().Be("https://test-account.r2.cloudflarestorage.com");
    R2Jurisdiction.EuropeanUnion.GetS3EndpointUrl(accountId).Should().Be("https://test-account.eu.r2.cloudflarestorage.com");
    R2Jurisdiction.FedRamp.GetS3EndpointUrl(accountId).Should().Be("https://test-account.fedramp.r2.cloudflarestorage.com");
  }

  #endregion
}
