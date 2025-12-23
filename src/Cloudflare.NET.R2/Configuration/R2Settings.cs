namespace Cloudflare.NET.R2.Configuration;

using Cloudflare.NET.Accounts.Models;

/// <summary>
///   Represents R2-specific settings for the S3-compatible API, typically loaded from configuration.
/// </summary>
public class R2Settings
{
  #region Properties & Fields - Public

  /// <summary>
  ///   The jurisdiction for this R2 client configuration. Determines which S3 endpoint to use
  ///   when <see cref="EndpointUrl" /> is not explicitly set.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Defaults to <see cref="R2Jurisdiction.Default" /> (no jurisdictional restriction),
  ///     which uses the global endpoint: <c>https://{account_id}.r2.cloudflarestorage.com</c>
  ///   </para>
  ///   <para>
  ///     When set to a specific jurisdiction (e.g., <see cref="R2Jurisdiction.EuropeanUnion" />),
  ///     the S3 endpoint URL is automatically computed to use the jurisdiction-specific endpoint.
  ///   </para>
  ///   <para>
  ///     If <see cref="EndpointUrl" /> is explicitly set, it takes precedence over the
  ///     jurisdiction-based endpoint calculation.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   Configuration via appsettings.json:
  ///   <code>
  ///   {
  ///     "R2": {
  ///       "AccessKeyId": "...",
  ///       "SecretAccessKey": "...",
  ///       "Jurisdiction": "eu"
  ///     }
  ///   }
  ///   </code>
  /// </example>
  public R2Jurisdiction Jurisdiction { get; set; } = R2Jurisdiction.Default;

  /// <summary>
  ///   The R2 S3 API endpoint URL. When set, must contain a format placeholder <c>{0}</c> for the Account ID.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     When <c>null</c> or empty (the default), the endpoint is automatically computed from
  ///     <see cref="Jurisdiction" />:
  ///     <list type="bullet">
  ///       <item><description>Default: <c>https://{account_id}.r2.cloudflarestorage.com</c></description></item>
  ///       <item><description>EU: <c>https://{account_id}.eu.r2.cloudflarestorage.com</c></description></item>
  ///       <item><description>FedRAMP: <c>https://{account_id}.fedramp.r2.cloudflarestorage.com</c></description></item>
  ///     </list>
  ///   </para>
  ///   <para>
  ///     When explicitly set, this value takes precedence over <see cref="Jurisdiction" />.
  ///     This allows for custom endpoint configurations or testing scenarios.
  ///   </para>
  /// </remarks>
  public string? EndpointUrl { get; set; }

  /// <summary>
  ///   The R2 region. Defaults to <c>"auto"</c>, which is the standard region for Cloudflare R2.
  /// </summary>
  public string Region { get; set; } = DefaultRegion;

  /// <summary>
  ///   The R2 Access Key ID (from user secrets or environment variables). Required.
  /// </summary>
  public string AccessKeyId { get; set; } = string.Empty;

  /// <summary>
  ///   The R2 Secret Access Key (from user secrets or environment variables). Required.
  /// </summary>
  public string SecretAccessKey { get; set; } = string.Empty;

  #endregion


  #region Constants

  /// <summary>
  ///   The default R2 region. Cloudflare R2 uses <c>"auto"</c> as the region for all operations.
  /// </summary>
  public const string DefaultRegion = "auto";

  #endregion


  #region Methods

  /// <summary>
  ///   Gets the effective endpoint URL for this configuration, computing from jurisdiction if not explicitly set.
  /// </summary>
  /// <param name="accountId">The Cloudflare account ID to substitute into the endpoint URL.</param>
  /// <returns>The fully-formed S3 endpoint URL ready for use with the AWS SDK.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> is null or whitespace.</exception>
  /// <remarks>
  ///   <para>Resolution order:</para>
  ///   <list type="number">
  ///     <item>
  ///       <description>
  ///         If <see cref="EndpointUrl" /> is set, use it with <c>{0}</c> replaced by account ID.
  ///       </description>
  ///     </item>
  ///     <item>
  ///       <description>
  ///         Otherwise, compute from <see cref="Jurisdiction" /> using
  ///         <see cref="R2Jurisdiction.GetS3EndpointUrl" />.
  ///       </description>
  ///     </item>
  ///   </list>
  /// </remarks>
  public string GetEffectiveEndpointUrl(string accountId)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

    // If user explicitly set EndpointUrl, use it with account ID substitution.
    if (!string.IsNullOrWhiteSpace(EndpointUrl))
      return string.Format(EndpointUrl, accountId);

    // Otherwise, compute from jurisdiction.
    return Jurisdiction.GetS3EndpointUrl(accountId);
  }


  /// <summary>
  ///   Gets the endpoint URL for a specific jurisdiction override, ignoring <see cref="EndpointUrl" />.
  /// </summary>
  /// <param name="accountId">The Cloudflare account ID.</param>
  /// <param name="jurisdictionOverride">The jurisdiction to use instead of <see cref="Jurisdiction" />.</param>
  /// <returns>The fully-formed S3 endpoint URL for the specified jurisdiction.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> is null or whitespace.</exception>
  /// <remarks>
  ///   This method always computes the endpoint from the jurisdiction parameter, ignoring both
  ///   <see cref="EndpointUrl" /> and <see cref="Jurisdiction" />. Used by <see cref="IR2ClientFactory" />
  ///   when creating jurisdiction-specific clients with
  ///   <see cref="IR2ClientFactory.GetClient(R2Jurisdiction)" /> or
  ///   <see cref="IR2ClientFactory.GetClient(string, R2Jurisdiction)" />.
  /// </remarks>
  public static string GetEndpointUrlForJurisdiction(string accountId, R2Jurisdiction jurisdictionOverride)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

    return jurisdictionOverride.GetS3EndpointUrl(accountId);
  }

  #endregion
}
