namespace Cloudflare.NET.R2.Configuration;

using Microsoft.Extensions.Options;

/// <summary>
///   Validates <see cref="R2Settings" /> to ensure all required configuration is present and valid.
///   This validator is invoked at startup when using <c>ValidateOnStart()</c>, providing immediate feedback
///   for configuration issues rather than waiting for the first API call to fail.
/// </summary>
/// <remarks>
///   <para>
///     This validator checks that the R2 S3 credentials (AccessKeyId and SecretAccessKey) are provided.
///     It also validates that the EndpointUrl, if customized, contains the required '{0}' placeholder
///     for the Account ID.
///   </para>
///   <para>
///     The validation errors are designed to be clear and actionable, mentioning "Cloudflare R2" explicitly
///     so developers can quickly identify the source of configuration issues.
///   </para>
///   <para>
///     For validation of <see cref="Core.CloudflareApiOptions" /> (AccountId), use
///     <see cref="Core.Validation.CloudflareApiOptionsValidator" /> with
///     <see cref="Core.Validation.CloudflareValidationRequirements.ForR2" />.
///   </para>
///   <para>
///     When registered as <see cref="IValidateOptions{TOptions}" />, this validator skips named options
///     by default. The <see cref="R2ClientFactory" /> should use <see cref="ValidateConfiguration" />
///     directly for explicit validation with <see cref="Exceptions.CloudflareR2ConfigurationException" />.
///   </para>
/// </remarks>
public class R2SettingsValidator : IValidateOptions<R2Settings>
{
  #region Constants

  /// <summary>The configuration section name for R2 settings, used in error messages.</summary>
  internal const string ConfigSectionName = "R2";

  #endregion


  #region Properties & Fields - Non-Public

  /// <summary>
  ///   Whether to skip validation for named options. When true (default), the validator returns
  ///   <see cref="ValidateOptionsResult.Skip" /> for non-default option names, allowing the factory
  ///   to handle validation with <see cref="Exceptions.CloudflareR2ConfigurationException" />.
  /// </summary>
  private readonly bool _skipNamedOptions;

  #endregion


  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="R2SettingsValidator" /> class with
  ///   default skip behavior for named options.
  /// </summary>
  public R2SettingsValidator() : this(skipNamedOptions: true) { }


  /// <summary>
  ///   Initializes a new instance of the <see cref="R2SettingsValidator" /> class with
  ///   explicit skip behavior control.
  /// </summary>
  /// <param name="skipNamedOptions">
  ///   If <see langword="true" />, the validator returns <see cref="ValidateOptionsResult.Skip" /> for
  ///   named options (non-default names). Set to <see langword="false" /> when performing explicit
  ///   validation in the factory.
  /// </param>
  public R2SettingsValidator(bool skipNamedOptions)
  {
    _skipNamedOptions = skipNamedOptions;
  }

  #endregion


  #region Methods Impl

  /// <inheritdoc />
  public ValidateOptionsResult Validate(string? name, R2Settings options)
  {
    // Skip validation for named options when configured to do so. This allows the R2ClientFactory
    // to handle validation with CloudflareR2ConfigurationException.
    if (_skipNamedOptions && !string.IsNullOrEmpty(name) && name != Options.DefaultName)
      return ValidateOptionsResult.Skip;

    // Perform validation and return the result.
    return ValidateConfiguration(name, options);
  }

  #endregion


  #region Methods (Static)

  /// <summary>
  ///   Validates the specified <see cref="R2Settings" /> and returns a result with error messages.
  ///   This method is intended for use by the <see cref="R2ClientFactory" /> to perform explicit
  ///   validation with proper error messages that include the client name.
  /// </summary>
  /// <param name="name">
  ///   The options name. For named clients, this should be the client name to include in error messages.
  ///   For default clients, pass <see langword="null" /> or <see cref="Options.DefaultName" />.
  /// </param>
  /// <param name="options">The R2 settings to validate.</param>
  /// <returns>A <see cref="ValidateOptionsResult" /> indicating success or failure with error messages.</returns>
  public static ValidateOptionsResult ValidateConfiguration(string? name, R2Settings options)
  {
    // Build the configuration path prefix for error messages.
    // For named clients, this will be "R2:ClientName", for default clients just "R2".
    var configPath = string.IsNullOrEmpty(name) || name == Options.DefaultName
      ? ConfigSectionName
      : $"{ConfigSectionName}:{name}";

    var failures = new List<string>();

    // Validate AccessKeyId is provided.
    if (string.IsNullOrWhiteSpace(options.AccessKeyId))
    {
      failures.Add(
        $"Cloudflare R2 AccessKeyId is required. " +
        $"Set '{configPath}:AccessKeyId' in your configuration or provide it programmatically. " +
        $"You can generate R2 API tokens in the Cloudflare dashboard under R2 > Manage R2 API Tokens.");
    }

    // Validate SecretAccessKey is provided.
    if (string.IsNullOrWhiteSpace(options.SecretAccessKey))
    {
      failures.Add(
        $"Cloudflare R2 SecretAccessKey is required. " +
        $"Set '{configPath}:SecretAccessKey' in your configuration or provide it programmatically. " +
        $"You can generate R2 API tokens in the Cloudflare dashboard under R2 > Manage R2 API Tokens.");
    }

    // Validate EndpointUrl contains the required placeholder for Account ID substitution.
    if (!string.IsNullOrWhiteSpace(options.EndpointUrl) && !options.EndpointUrl.Contains("{0}"))
    {
      failures.Add(
        $"Cloudflare R2 EndpointUrl must contain a '{{0}}' placeholder for the Account ID. " +
        $"The default value is '{R2Settings.DefaultEndpointUrl}'. " +
        $"If you've customized '{configPath}:EndpointUrl', ensure it includes '{{0}}' where the Account ID should be inserted.");
    }

    // Validate Region is provided (should never be empty with the default, but validate anyway).
    if (string.IsNullOrWhiteSpace(options.Region))
    {
      failures.Add(
        $"Cloudflare R2 Region is required. " +
        $"The default value is '{R2Settings.DefaultRegion}'. " +
        $"If you've customized '{configPath}:Region', ensure it is not empty.");
    }

    // Return validation result.
    if (failures.Count > 0)
      return ValidateOptionsResult.Fail(failures);

    return ValidateOptionsResult.Success;
  }

  #endregion
}
