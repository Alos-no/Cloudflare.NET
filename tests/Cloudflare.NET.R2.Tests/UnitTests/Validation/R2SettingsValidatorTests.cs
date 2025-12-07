namespace Cloudflare.NET.R2.Tests.UnitTests.Validation;

using Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NET.Tests.Shared.Fixtures;

/// <summary>Contains unit tests for the <see cref="R2SettingsValidator" /> class.</summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class R2SettingsValidatorTests
{
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


  /// <summary>Verifies that the default EndpointUrl contains the required placeholder.</summary>
  [Fact]
  public void DefaultEndpointUrl_ContainsPlaceholder()
  {
    // Assert
    R2Settings.DefaultEndpointUrl.Should().Contain("{0}");
  }


  /// <summary>Verifies that R2Settings defaults have correct values.</summary>
  [Fact]
  public void R2Settings_HasCorrectDefaults()
  {
    // Arrange
    var settings = new R2Settings();

    // Assert
    settings.EndpointUrl.Should().Be(R2Settings.DefaultEndpointUrl);
    settings.Region.Should().Be(R2Settings.DefaultRegion);
    settings.EndpointUrl.Should().Be("https://{0}.r2.cloudflarestorage.com");
    settings.Region.Should().Be("auto");
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
    var errorMessage = result.Failures.First();

    // Should mention R2 API tokens
    errorMessage.Should().Contain("R2 API Tokens");

    // Should mention Cloudflare dashboard
    errorMessage.Should().Contain("Cloudflare dashboard");
  }

  #endregion
}
