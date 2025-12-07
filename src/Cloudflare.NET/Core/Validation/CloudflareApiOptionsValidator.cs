namespace Cloudflare.NET.Core.Validation;

using Microsoft.Extensions.Options;

/// <summary>
///   Validates <see cref="CloudflareApiOptions" /> to ensure all required configuration is present and valid. This
///   validator is invoked at startup when using <c>ValidateOnStart()</c>, providing immediate feedback for configuration
///   issues rather than waiting for the first API call to fail.
/// </summary>
/// <remarks>
///   <para>
///     This validator checks that the API token is provided, which is required for authentication with the Cloudflare
///     API. Additional validation (such as AccountId for R2 or GraphQL URL for Analytics) is performed by package-specific
///     validators.
///   </para>
///   <para>
///     The validation errors are designed to be clear and actionable, mentioning "Cloudflare" explicitly so developers
///     can quickly identify the source of configuration issues.
///   </para>
///   <para>
///     When registered as <see cref="IValidateOptions{TOptions}" />, this validator skips named options by default.
///     Factories should use <see cref="ValidateConfiguration" /> directly for explicit validation of named client
///     configurations with custom exception types.
///   </para>
/// </remarks>
public class CloudflareApiOptionsValidator : IValidateOptions<CloudflareApiOptions>
{
  #region Constants & Statics

  #region Constants

  /// <summary>The configuration section name for Cloudflare settings, used in error messages.</summary>
  internal const string ConfigSectionName = "Cloudflare";

  #endregion

  #endregion

  #region Properties & Fields - Non-Public

  /// <summary>The validation requirements for this validator instance.</summary>
  private readonly CloudflareValidationRequirements _requirements;

  /// <summary>
  ///   Whether to skip validation for named options. When true (default), the validator returns
  ///   <see cref="ValidateOptionsResult.Skip" /> for non-default option names, allowing factories to handle validation with
  ///   custom exception types.
  /// </summary>
  private readonly bool _skipNamedOptions;

  #endregion

  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="CloudflareApiOptionsValidator" /> class with default validation
  ///   requirements (ApiToken required) and default skip behavior for named options.
  /// </summary>
  public CloudflareApiOptionsValidator() : this(CloudflareValidationRequirements.Default, true) { }


  /// <summary>
  ///   Initializes a new instance of the <see cref="CloudflareApiOptionsValidator" /> class with specific validation
  ///   requirements and default skip behavior for named options.
  /// </summary>
  /// <param name="requirements">The validation requirements to apply.</param>
  public CloudflareApiOptionsValidator(CloudflareValidationRequirements requirements)
    : this(requirements, true) { }


  /// <summary>
  ///   Initializes a new instance of the <see cref="CloudflareApiOptionsValidator" /> class with specific validation
  ///   requirements and explicit skip behavior control.
  /// </summary>
  /// <param name="requirements">The validation requirements to apply.</param>
  /// <param name="skipNamedOptions">
  ///   If <see langword="true" />, the validator returns
  ///   <see cref="ValidateOptionsResult.Skip" /> for named options (non-default names). Set to <see langword="false" /> when
  ///   performing explicit validation in factories.
  /// </param>
  public CloudflareApiOptionsValidator(CloudflareValidationRequirements requirements, bool skipNamedOptions)
  {
    _requirements     = requirements;
    _skipNamedOptions = skipNamedOptions;
  }

  #endregion

  #region Methods Impl

  /// <inheritdoc />
  public ValidateOptionsResult Validate(string? name, CloudflareApiOptions options)
  {
    // Skip validation for named options when configured to do so. This allows factories to handle
    // validation with custom exception types (InvalidOperationException for Core/Analytics,
    // CloudflareR2ConfigurationException for R2).
    if (_skipNamedOptions && !string.IsNullOrEmpty(name) && name != Options.DefaultName)
      return ValidateOptionsResult.Skip;

    // Perform validation and return the result.
    return ValidateConfiguration(name, options, _requirements);
  }

  #endregion

  #region Methods

  /// <summary>
  ///   Validates the specified <see cref="CloudflareApiOptions" /> against the given requirements. This method is
  ///   intended for use by factories to perform explicit validation with proper error messages that include the client name.
  /// </summary>
  /// <param name="name">
  ///   The options name. For named clients, this should be the client name to include in error messages.
  ///   For default clients, pass <see langword="null" /> or <see cref="Options.DefaultName" />.
  /// </param>
  /// <param name="options">The options to validate.</param>
  /// <param name="requirements">The validation requirements to apply.</param>
  /// <returns>A <see cref="ValidateOptionsResult" /> indicating success or failure with error messages.</returns>
  public static ValidateOptionsResult ValidateConfiguration(string?                          name,
                                                            CloudflareApiOptions             options,
                                                            CloudflareValidationRequirements requirements)
  {
    // Build the configuration path prefix for error messages.
    // For named clients, this will be "Cloudflare:ClientName", for default clients just "Cloudflare".
    var configPath = string.IsNullOrEmpty(name) || name == Options.DefaultName
      ? ConfigSectionName
      : $"{ConfigSectionName}:{name}";

    var failures = new List<string>();

    // Validate ApiToken is provided if required.
    if (requirements.RequireApiToken && string.IsNullOrWhiteSpace(options.ApiToken))
      failures.Add(
        $"Cloudflare ApiToken is required. " +
        $"Set '{configPath}:ApiToken' in your configuration or provide it programmatically. " +
        $"You can create API tokens in the Cloudflare dashboard under My Profile > API Tokens.");

    // Validate AccountId is provided if required.
    if (requirements.RequireAccountId && string.IsNullOrWhiteSpace(options.AccountId))
      failures.Add(
        $"Cloudflare AccountId is required. " +
        $"Set '{configPath}:AccountId' in your configuration or provide it programmatically. " +
        $"You can find your Account ID in the Cloudflare dashboard URL or on the Overview page.");

    // Validate GraphQlApiUrl is provided if required.
    if (requirements.RequireGraphQlApiUrl && string.IsNullOrWhiteSpace(options.GraphQlApiUrl))
      failures.Add(
        $"Cloudflare GraphQlApiUrl is required. " +
        $"Set '{configPath}:GraphQlApiUrl' in your configuration or provide it programmatically. " +
        $"The default value is 'https://api.cloudflare.com/client/v4/graphql'.");

    // Validate ApiBaseUrl is provided if required.
    if (requirements.RequireApiBaseUrl && string.IsNullOrWhiteSpace(options.ApiBaseUrl))
      failures.Add(
        $"Cloudflare ApiBaseUrl is required. " +
        $"Set '{configPath}:ApiBaseUrl' in your configuration or provide it programmatically. " +
        $"The default value is 'https://api.cloudflare.com/client/v4/'.");

    // Return validation result.
    if (failures.Count > 0)
      return ValidateOptionsResult.Fail(failures);

    return ValidateOptionsResult.Success;
  }

  #endregion
}

/// <summary>
///   Specifies which properties of <see cref="CloudflareApiOptions" /> are required for validation. This allows
///   different packages (Core, R2, Analytics) to have different validation requirements.
/// </summary>
public sealed class CloudflareValidationRequirements
{
  #region Constants & Statics

  /// <summary>
  ///   Default validation requirements: only <see cref="CloudflareApiOptions.ApiToken" /> is required. Suitable for
  ///   the Core API client.
  /// </summary>
  public static CloudflareValidationRequirements Default => new() { RequireApiToken = true };

  /// <summary>
  ///   Validation requirements for the Analytics client: <see cref="CloudflareApiOptions.ApiToken" /> and
  ///   <see cref="CloudflareApiOptions.GraphQlApiUrl" /> are required.
  /// </summary>
  public static CloudflareValidationRequirements ForAnalytics => new()
  {
    RequireApiToken      = true,
    RequireGraphQlApiUrl = true
  };

  /// <summary>
  ///   Validation requirements for the R2 client: <see cref="CloudflareApiOptions.AccountId" /> is required. The API
  ///   token is not required for R2 data plane operations (uses S3 credentials instead).
  /// </summary>
  public static CloudflareValidationRequirements ForR2 => new()
  {
    RequireApiToken  = false,
    RequireAccountId = true
  };

  #endregion

  #region Properties & Fields - Public

  /// <summary>
  ///   Gets a value indicating whether the <see cref="CloudflareApiOptions.ApiToken" /> is required. Defaults to
  ///   <see langword="true" />.
  /// </summary>
  public bool RequireApiToken { get; init; } = true;

  /// <summary>
  ///   Gets a value indicating whether the <see cref="CloudflareApiOptions.AccountId" /> is required. Defaults to
  ///   <see langword="false" />.
  /// </summary>
  public bool RequireAccountId { get; init; }

  /// <summary>
  ///   Gets a value indicating whether the <see cref="CloudflareApiOptions.GraphQlApiUrl" /> is required. Defaults to
  ///   <see langword="false" />.
  /// </summary>
  public bool RequireGraphQlApiUrl { get; init; }

  /// <summary>
  ///   Gets a value indicating whether the <see cref="CloudflareApiOptions.ApiBaseUrl" /> is required. Defaults to
  ///   <see langword="false" />.
  /// </summary>
  public bool RequireApiBaseUrl { get; init; }

  #endregion
}
