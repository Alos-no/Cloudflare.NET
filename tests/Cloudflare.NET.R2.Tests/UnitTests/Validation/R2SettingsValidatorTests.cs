namespace Cloudflare.NET.R2.Tests.UnitTests.Validation;

using Accounts.Models;
using Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NET.Tests.Shared.Fixtures;

/// <summary>Contains unit tests for the <see cref="R2SettingsValidator" /> class.</summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class R2SettingsValidatorTests
{
  #region Methods

  #region SecretAccessKey Validation Tests

  /// <summary>Verifies that validation fails when SecretAccessKey is missing.</summary>
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Validate_WithMissingSecretAccessKey_ReturnsFailed(string? secretAccessKey)
  {
    // Arrange
    var validator = new R2SettingsValidator();
    var options = new R2Settings
    {
      AccessKeyId     = "valid-access-key",
      SecretAccessKey = secretAccessKey!
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.Failures.Should().Contain(f => f.Contains("SecretAccessKey is required"));
  }

  #endregion


  #region Region Validation Tests

  /// <summary>Verifies that validation fails when Region is missing (set to empty).</summary>
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Validate_WithMissingRegion_ReturnsFailed(string? region)
  {
    // Arrange
    var validator = new R2SettingsValidator();
    var options = new R2Settings
    {
      AccessKeyId     = "valid-access-key",
      SecretAccessKey = "valid-secret-key",
      Region          = region!
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.Failures.Should().Contain(f => f.Contains("Region is required"));
  }

  #endregion


  #region Multiple Errors Tests

  /// <summary>Verifies that validation reports all errors when multiple fields are missing.</summary>
  [Fact]
  public void Validate_WithMultipleMissingFields_ReturnsAllErrors()
  {
    // Arrange
    var validator = new R2SettingsValidator();
    var options = new R2Settings
    {
      AccessKeyId     = "",
      SecretAccessKey = "",
      Region          = ""
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.Failures.Should().HaveCount(3);
    result.Failures.Should().Contain(f => f.Contains("AccessKeyId"));
    result.Failures.Should().Contain(f => f.Contains("SecretAccessKey"));
    result.Failures.Should().Contain(f => f.Contains("Region"));
  }

  #endregion


  #region Error Message Quality Tests

  /// <summary>Verifies that error messages include helpful guidance about where to find credentials.</summary>
  [Fact]
  public void Validate_ErrorMessages_IncludeHelpfulGuidance()
  {
    // Arrange
    var validator = new R2SettingsValidator();
    var options = new R2Settings
    {
      AccessKeyId     = "",
      SecretAccessKey = "valid-secret"
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Failed.Should().BeTrue();
    var errorMessage = result.Failures!.First();

    // Should mention R2 API tokens
    errorMessage.Should().Contain("R2 API Tokens");

    // Should mention Cloudflare dashboard
    errorMessage.Should().Contain("Cloudflare dashboard");
  }

  #endregion

  #endregion

  #region Valid Configuration Tests

  /// <summary>Verifies that validation passes when all required fields are provided.</summary>
  [Fact]
  public void Validate_WithValidOptions_ReturnsSuccess()
  {
    // Arrange
    var validator = new R2SettingsValidator();
    var options = new R2Settings
    {
      AccessKeyId     = "valid-access-key",
      SecretAccessKey = "valid-secret-key"
      // EndpointUrl and Region have defaults
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Succeeded.Should().BeTrue();
    result.Failed.Should().BeFalse();
  }


  /// <summary>Verifies that validation passes with custom EndpointUrl containing placeholder.</summary>
  [Fact]
  public void Validate_WithCustomEndpointUrl_ContainingPlaceholder_ReturnsSuccess()
  {
    // Arrange
    var validator = new R2SettingsValidator();
    var options = new R2Settings
    {
      AccessKeyId     = "valid-access-key",
      SecretAccessKey = "valid-secret-key",
      EndpointUrl     = "https://custom-{0}.example.com"
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Succeeded.Should().BeTrue();
  }

  #endregion


  #region AccessKeyId Validation Tests

  /// <summary>Verifies that validation fails when AccessKeyId is missing.</summary>
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Validate_WithMissingAccessKeyId_ReturnsFailed(string? accessKeyId)
  {
    // Arrange
    var validator = new R2SettingsValidator();
    var options = new R2Settings
    {
      AccessKeyId     = accessKeyId!,
      SecretAccessKey = "valid-secret-key"
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.Failures.Should().Contain(f => f.Contains("AccessKeyId is required"));
  }


  /// <summary>Verifies that AccessKeyId error message includes the correct configuration path.</summary>
  [Fact]
  public void Validate_WithMissingAccessKeyId_ErrorMessageIncludesConfigPath()
  {
    // Arrange
    var validator = new R2SettingsValidator();
    var options = new R2Settings
    {
      AccessKeyId     = "",
      SecretAccessKey = "valid-secret-key"
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.Failures.Should().Contain(f => f.Contains("R2:AccessKeyId"));
  }


  /// <summary>Verifies that AccessKeyId error message includes the named configuration path for named clients.</summary>
  [Fact]
  public void Validate_WithMissingAccessKeyId_ForNamedClient_ErrorMessageIncludesNamedPath()
  {
    // Arrange
    const string clientName = "primary";

    var options = new R2Settings
    {
      AccessKeyId     = "",
      SecretAccessKey = "valid-secret-key"
    };

    // Act - Use the static ValidateConfiguration method which is used by factories
    // and doesn't skip named options (unlike the instance Validate method which
    // skips named options to allow factories to handle validation with custom exceptions).
    var result = R2SettingsValidator.ValidateConfiguration(clientName, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.Failures.Should().Contain(f => f.Contains($"R2:{clientName}:AccessKeyId"));
  }

  #endregion


  #region EndpointUrl Validation Tests

  /// <summary>Verifies that validation fails when EndpointUrl is provided but missing the placeholder.</summary>
  [Fact]
  public void Validate_WithEndpointUrl_MissingPlaceholder_ReturnsFailed()
  {
    // Arrange
    var validator = new R2SettingsValidator();
    var options = new R2Settings
    {
      AccessKeyId     = "valid-access-key",
      SecretAccessKey = "valid-secret-key",
      EndpointUrl     = "https://example.r2.cloudflarestorage.com" // Missing {0}
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.Failures.Should().Contain(f => f.Contains("EndpointUrl must contain a '{0}' placeholder"));
  }


  /// <summary>Verifies that GetEffectiveEndpointUrl computes correct URL for default jurisdiction.</summary>
  [Fact]
  public void GetEffectiveEndpointUrl_DefaultJurisdiction_ReturnsCorrectUrl()
  {
    // Arrange
    var settings = new R2Settings();

    // Act
    var endpointUrl = settings.GetEffectiveEndpointUrl("test-account-id");

    // Assert
    endpointUrl.Should().Be("https://test-account-id.r2.cloudflarestorage.com");
  }


  /// <summary>Verifies that R2Settings defaults have correct values.</summary>
  [Fact]
  public void R2Settings_HasCorrectDefaults()
  {
    // Arrange
    var settings = new R2Settings();

    // Assert
    settings.EndpointUrl.Should().BeNull();
    settings.Region.Should().Be(R2Settings.DefaultRegion);
    settings.Region.Should().Be("auto");
    settings.Jurisdiction.Should().Be(R2Jurisdiction.Default);
  }

  #endregion


  #region GetEffectiveEndpointUrl Jurisdiction Tests

  /// <summary>Verifies that GetEffectiveEndpointUrl computes correct URL for EU jurisdiction.</summary>
  [Fact]
  public void GetEffectiveEndpointUrl_EuJurisdiction_ReturnsEuEndpoint()
  {
    // Arrange
    var settings = new R2Settings
    {
      Jurisdiction = R2Jurisdiction.EuropeanUnion
    };

    // Act
    var endpointUrl = settings.GetEffectiveEndpointUrl("test-account");

    // Assert
    endpointUrl.Should().Be("https://test-account.eu.r2.cloudflarestorage.com");
  }


  /// <summary>Verifies that GetEffectiveEndpointUrl computes correct URL for FedRAMP jurisdiction.</summary>
  [Fact]
  public void GetEffectiveEndpointUrl_FedRampJurisdiction_ReturnsFedRampEndpoint()
  {
    // Arrange
    var settings = new R2Settings
    {
      Jurisdiction = R2Jurisdiction.FedRamp
    };

    // Act
    var endpointUrl = settings.GetEffectiveEndpointUrl("test-account");

    // Assert
    endpointUrl.Should().Be("https://test-account.fedramp.r2.cloudflarestorage.com");
  }


  /// <summary>Verifies that explicit EndpointUrl takes precedence over Jurisdiction.</summary>
  [Fact]
  public void GetEffectiveEndpointUrl_WithExplicitEndpointUrl_OverridesJurisdiction()
  {
    // Arrange
    var settings = new R2Settings
    {
      Jurisdiction = R2Jurisdiction.EuropeanUnion,
      EndpointUrl  = "https://{0}.custom.endpoint.com"
    };

    // Act
    var endpointUrl = settings.GetEffectiveEndpointUrl("test-account");

    // Assert - Explicit EndpointUrl should override Jurisdiction
    endpointUrl.Should().Be("https://test-account.custom.endpoint.com");
  }


  /// <summary>Verifies that GetEffectiveEndpointUrl throws when accountId is null.</summary>
  [Fact]
  public void GetEffectiveEndpointUrl_NullAccountId_ThrowsArgumentException()
  {
    // Arrange
    var settings = new R2Settings();

    // Act
    var action = () => settings.GetEffectiveEndpointUrl(null!);

    // Assert
    action.Should().Throw<ArgumentException>()
          .WithParameterName("accountId");
  }


  /// <summary>Verifies that GetEffectiveEndpointUrl throws when accountId is empty.</summary>
  [Fact]
  public void GetEffectiveEndpointUrl_EmptyAccountId_ThrowsArgumentException()
  {
    // Arrange
    var settings = new R2Settings();

    // Act
    var action = () => settings.GetEffectiveEndpointUrl("");

    // Assert
    action.Should().Throw<ArgumentException>()
          .WithParameterName("accountId");
  }


  /// <summary>Verifies that GetEffectiveEndpointUrl throws when accountId is whitespace.</summary>
  [Fact]
  public void GetEffectiveEndpointUrl_WhitespaceAccountId_ThrowsArgumentException()
  {
    // Arrange
    var settings = new R2Settings();

    // Act
    var action = () => settings.GetEffectiveEndpointUrl("   ");

    // Assert
    action.Should().Throw<ArgumentException>()
          .WithParameterName("accountId");
  }


  /// <summary>Verifies GetEffectiveEndpointUrl with all known jurisdictions.</summary>
  [Theory]
  [InlineData("default", "https://acct.r2.cloudflarestorage.com")]
  [InlineData("eu", "https://acct.eu.r2.cloudflarestorage.com")]
  [InlineData("fedramp", "https://acct.fedramp.r2.cloudflarestorage.com")]
  public void GetEffectiveEndpointUrl_WithVariousJurisdictions_ReturnsCorrectEndpoints(
    string jurisdictionValue,
    string expectedUrl)
  {
    // Arrange
    var settings = new R2Settings
    {
      Jurisdiction = new R2Jurisdiction(jurisdictionValue)
    };

    // Act
    var endpointUrl = settings.GetEffectiveEndpointUrl("acct");

    // Assert
    endpointUrl.Should().Be(expectedUrl);
  }

  #endregion


  #region GetEndpointUrlForJurisdiction Static Method Tests

  /// <summary>Verifies that GetEndpointUrlForJurisdiction returns correct URL for Default.</summary>
  [Fact]
  public void GetEndpointUrlForJurisdiction_Default_ReturnsGlobalEndpoint()
  {
    // Act
    var endpointUrl = R2Settings.GetEndpointUrlForJurisdiction("test-account", R2Jurisdiction.Default);

    // Assert
    endpointUrl.Should().Be("https://test-account.r2.cloudflarestorage.com");
  }


  /// <summary>Verifies that GetEndpointUrlForJurisdiction returns correct URL for EU.</summary>
  [Fact]
  public void GetEndpointUrlForJurisdiction_EuropeanUnion_ReturnsEuEndpoint()
  {
    // Act
    var endpointUrl = R2Settings.GetEndpointUrlForJurisdiction("test-account", R2Jurisdiction.EuropeanUnion);

    // Assert
    endpointUrl.Should().Be("https://test-account.eu.r2.cloudflarestorage.com");
  }


  /// <summary>Verifies that GetEndpointUrlForJurisdiction returns correct URL for FedRAMP.</summary>
  [Fact]
  public void GetEndpointUrlForJurisdiction_FedRamp_ReturnsFedRampEndpoint()
  {
    // Act
    var endpointUrl = R2Settings.GetEndpointUrlForJurisdiction("test-account", R2Jurisdiction.FedRamp);

    // Assert
    endpointUrl.Should().Be("https://test-account.fedramp.r2.cloudflarestorage.com");
  }


  /// <summary>Verifies that GetEndpointUrlForJurisdiction ignores the settings instance jurisdiction.</summary>
  [Fact]
  public void GetEndpointUrlForJurisdiction_IgnoresSettingsJurisdiction()
  {
    // Arrange - Settings has EU, but we're asking for FedRAMP
    var settings = new R2Settings
    {
      Jurisdiction = R2Jurisdiction.EuropeanUnion
    };

    // Act - Static method ignores instance settings
    var endpointUrl = R2Settings.GetEndpointUrlForJurisdiction("test-account", R2Jurisdiction.FedRamp);

    // Assert
    endpointUrl.Should().Be("https://test-account.fedramp.r2.cloudflarestorage.com");
  }


  /// <summary>Verifies that GetEndpointUrlForJurisdiction throws when accountId is null.</summary>
  [Fact]
  public void GetEndpointUrlForJurisdiction_NullAccountId_ThrowsArgumentException()
  {
    // Act
    var action = () => R2Settings.GetEndpointUrlForJurisdiction(null!, R2Jurisdiction.Default);

    // Assert
    action.Should().Throw<ArgumentException>()
          .WithParameterName("accountId");
  }


  /// <summary>Verifies that GetEndpointUrlForJurisdiction throws when accountId is empty.</summary>
  [Fact]
  public void GetEndpointUrlForJurisdiction_EmptyAccountId_ThrowsArgumentException()
  {
    // Act
    var action = () => R2Settings.GetEndpointUrlForJurisdiction("", R2Jurisdiction.Default);

    // Assert
    action.Should().Throw<ArgumentException>()
          .WithParameterName("accountId");
  }

  #endregion
}
