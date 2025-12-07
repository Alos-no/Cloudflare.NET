namespace Cloudflare.NET.Tests.UnitTests.Validation;

using Core;
using Core.Validation;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Shared.Fixtures;

/// <summary>
///   Contains unit tests for the <see cref="CloudflareApiOptionsValidator" /> class and
///   <see cref="CloudflareValidationRequirements" /> configuration.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class CloudflareApiOptionsValidatorTests
{
  #region Default Requirements Tests

  /// <summary>Verifies that validation passes when all default required fields are provided.</summary>
  [Fact]
  public void Validate_WithValidDefaultOptions_ReturnsSuccess()
  {
    // Arrange
    var validator = new CloudflareApiOptionsValidator(CloudflareValidationRequirements.Default);
    var options = new CloudflareApiOptions
    {
      ApiToken = "valid-api-token"
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Succeeded.Should().BeTrue();
    result.Failed.Should().BeFalse();
  }


  /// <summary>Verifies that validation fails when ApiToken is missing with default requirements.</summary>
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Validate_WithMissingApiToken_ReturnsFailed(string? apiToken)
  {
    // Arrange
    var validator = new CloudflareApiOptionsValidator(CloudflareValidationRequirements.Default);
    var options = new CloudflareApiOptions
    {
      ApiToken = apiToken!
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.Failures.Should().ContainSingle()
          .Which.Should().Contain("Cloudflare ApiToken is required");
  }


  /// <summary>Verifies that the error message includes the correct configuration path for default options.</summary>
  [Fact]
  public void Validate_WithMissingApiToken_ErrorMessageIncludesConfigPath()
  {
    // Arrange
    var validator = new CloudflareApiOptionsValidator(CloudflareValidationRequirements.Default);
    var options   = new CloudflareApiOptions();

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.Failures.Should().ContainSingle()
          .Which.Should().Contain("Cloudflare:ApiToken");
  }


  /// <summary>Verifies that the error message includes the correct configuration path for named options.</summary>
  [Fact]
  public void Validate_WithNamedOptions_ErrorMessageIncludesNamedConfigPath()
  {
    // Arrange
    const string clientName = "production";

    var options = new CloudflareApiOptions();

    // Act - Use the static ValidateConfiguration method which is used by factories
    // and doesn't skip named options (unlike the instance Validate method which
    // skips named options to allow factories to handle validation with custom exceptions).
    var result = CloudflareApiOptionsValidator.ValidateConfiguration(
      clientName, options, CloudflareValidationRequirements.Default);

    // Assert
    result.Failed.Should().BeTrue();
    result.Failures.Should().ContainSingle()
          .Which.Should().Contain($"Cloudflare:{clientName}:ApiToken");
  }

  #endregion


  #region Analytics Requirements Tests

  /// <summary>Verifies that Analytics validation passes when all required fields are provided.</summary>
  [Fact]
  public void Validate_WithValidAnalyticsOptions_ReturnsSuccess()
  {
    // Arrange
    var validator = new CloudflareApiOptionsValidator(CloudflareValidationRequirements.ForAnalytics);
    var options = new CloudflareApiOptions
    {
      ApiToken      = "valid-api-token",
      GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql"
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Succeeded.Should().BeTrue();
  }


  /// <summary>Verifies that Analytics validation fails when GraphQlApiUrl is missing.</summary>
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Validate_ForAnalytics_WithMissingGraphQlApiUrl_ReturnsFailed(string? graphQlApiUrl)
  {
    // Arrange
    var validator = new CloudflareApiOptionsValidator(CloudflareValidationRequirements.ForAnalytics);
    var options = new CloudflareApiOptions
    {
      ApiToken      = "valid-api-token",
      GraphQlApiUrl = graphQlApiUrl!
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.Failures.Should().ContainSingle()
          .Which.Should().Contain("Cloudflare GraphQlApiUrl is required");
  }


  /// <summary>Verifies that Analytics validation reports multiple errors when multiple fields are missing.</summary>
  [Fact]
  public void Validate_ForAnalytics_WithMultipleMissingFields_ReturnsAllErrors()
  {
    // Arrange
    var validator = new CloudflareApiOptionsValidator(CloudflareValidationRequirements.ForAnalytics);
    var options = new CloudflareApiOptions
    {
      ApiToken      = "",
      GraphQlApiUrl = ""
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.Failures.Should().HaveCount(2);
    result.Failures.Should().Contain(f => f.Contains("ApiToken"));
    result.Failures.Should().Contain(f => f.Contains("GraphQlApiUrl"));
  }

  #endregion


  #region R2 Requirements Tests

  /// <summary>Verifies that R2 validation passes when AccountId is provided (ApiToken not required for R2).</summary>
  [Fact]
  public void Validate_WithValidR2Options_ReturnsSuccess()
  {
    // Arrange
    var validator = new CloudflareApiOptionsValidator(CloudflareValidationRequirements.ForR2);
    var options = new CloudflareApiOptions
    {
      AccountId = "test-account-id"
      // Note: ApiToken is NOT required for R2 (uses S3 credentials instead)
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Succeeded.Should().BeTrue();
  }


  /// <summary>Verifies that R2 validation fails when AccountId is missing.</summary>
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Validate_ForR2_WithMissingAccountId_ReturnsFailed(string? accountId)
  {
    // Arrange
    var validator = new CloudflareApiOptionsValidator(CloudflareValidationRequirements.ForR2);
    var options = new CloudflareApiOptions
    {
      AccountId = accountId!
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.Failures.Should().ContainSingle()
          .Which.Should().Contain("Cloudflare AccountId is required");
  }


  /// <summary>Verifies that R2 validation does NOT require ApiToken.</summary>
  [Fact]
  public void Validate_ForR2_WithMissingApiToken_StillSucceeds()
  {
    // Arrange
    var validator = new CloudflareApiOptionsValidator(CloudflareValidationRequirements.ForR2);
    var options = new CloudflareApiOptions
    {
      AccountId = "test-account-id",
      ApiToken  = "" // Empty, but not required for R2
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Succeeded.Should().BeTrue();
  }

  #endregion


  #region Custom Requirements Tests

  /// <summary>Verifies that custom requirements can be created and validated correctly.</summary>
  [Fact]
  public void Validate_WithCustomRequirements_ValidatesOnlySpecifiedFields()
  {
    // Arrange - require only AccountId and ApiBaseUrl
    var requirements = new CloudflareValidationRequirements
    {
      RequireApiToken    = false,
      RequireAccountId   = true,
      RequireApiBaseUrl  = true,
      RequireGraphQlApiUrl = false
    };

    var validator = new CloudflareApiOptionsValidator(requirements);
    var options = new CloudflareApiOptions
    {
      AccountId  = "test-account",
      ApiBaseUrl = "https://api.cloudflare.com/client/v4/"
      // ApiToken and GraphQlApiUrl not provided, but not required
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Succeeded.Should().BeTrue();
  }


  /// <summary>Verifies that validation fails when ApiBaseUrl is required but missing.</summary>
  [Fact]
  public void Validate_WithRequiredApiBaseUrl_AndMissing_ReturnsFailed()
  {
    // Arrange
    var requirements = new CloudflareValidationRequirements
    {
      RequireApiToken   = false,
      RequireApiBaseUrl = true
    };

    var validator = new CloudflareApiOptionsValidator(requirements);
    var options = new CloudflareApiOptions
    {
      ApiBaseUrl = ""
    };

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Failed.Should().BeTrue();
    result.Failures.Should().ContainSingle()
          .Which.Should().Contain("Cloudflare ApiBaseUrl is required");
  }

  #endregion


  #region Error Message Quality Tests

  /// <summary>Verifies that error messages include helpful guidance about where to find credentials.</summary>
  [Fact]
  public void Validate_ErrorMessages_IncludeHelpfulGuidance()
  {
    // Arrange
    var validator = new CloudflareApiOptionsValidator(CloudflareValidationRequirements.Default);
    var options   = new CloudflareApiOptions();

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Failed.Should().BeTrue();
    var errorMessage = result.Failures.First();

    // Should mention Cloudflare dashboard
    errorMessage.Should().Contain("Cloudflare dashboard");

    // Should mention API Tokens
    errorMessage.Should().Contain("API Tokens");
  }


  /// <summary>Verifies that AccountId error message mentions where to find it.</summary>
  [Fact]
  public void Validate_AccountIdError_IncludesLocationGuidance()
  {
    // Arrange
    var validator = new CloudflareApiOptionsValidator(CloudflareValidationRequirements.ForR2);
    var options   = new CloudflareApiOptions();

    // Act
    var result = validator.Validate(Options.DefaultName, options);

    // Assert
    result.Failed.Should().BeTrue();
    var errorMessage = result.Failures.First();

    // Should mention where to find Account ID
    errorMessage.Should().Contain("Cloudflare dashboard");
  }

  #endregion


  #region Preset Requirements Tests

  /// <summary>Verifies that CloudflareValidationRequirements.Default only requires ApiToken.</summary>
  [Fact]
  public void DefaultRequirements_OnlyRequiresApiToken()
  {
    // Assert
    var defaults = CloudflareValidationRequirements.Default;

    defaults.RequireApiToken.Should().BeTrue();
    defaults.RequireAccountId.Should().BeFalse();
    defaults.RequireGraphQlApiUrl.Should().BeFalse();
    defaults.RequireApiBaseUrl.Should().BeFalse();
  }


  /// <summary>Verifies that CloudflareValidationRequirements.ForAnalytics requires ApiToken and GraphQlApiUrl.</summary>
  [Fact]
  public void ForAnalyticsRequirements_RequiresApiTokenAndGraphQlUrl()
  {
    // Assert
    var analytics = CloudflareValidationRequirements.ForAnalytics;

    analytics.RequireApiToken.Should().BeTrue();
    analytics.RequireGraphQlApiUrl.Should().BeTrue();
    analytics.RequireAccountId.Should().BeFalse();
    analytics.RequireApiBaseUrl.Should().BeFalse();
  }


  /// <summary>Verifies that CloudflareValidationRequirements.ForR2 only requires AccountId (not ApiToken).</summary>
  [Fact]
  public void ForR2Requirements_RequiresOnlyAccountId()
  {
    // Assert
    var r2 = CloudflareValidationRequirements.ForR2;

    r2.RequireAccountId.Should().BeTrue();
    r2.RequireApiToken.Should().BeFalse();
    r2.RequireGraphQlApiUrl.Should().BeFalse();
    r2.RequireApiBaseUrl.Should().BeFalse();
  }

  #endregion
}
