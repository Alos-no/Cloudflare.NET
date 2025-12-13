namespace Cloudflare.NET.Tests.Shared.Helpers;

/// <summary>
///   Marks a test that deals with non-standard, unexpected, or buggy Cloudflare API behavior.
///   This attribute is a documentation marker to indicate that the test assertions may not follow
///   standard REST API conventions due to Cloudflare's implementation quirks.
/// </summary>
/// <remarks>
///   <para>
///     Use this attribute when a test must assert against behavior that deviates from expected
///     REST API standards due to Cloudflare's API implementation. For example:
///   </para>
///   <list type="bullet">
///     <item>API returns 405 MethodNotAllowed instead of 404 NotFound for non-existent resources</item>
///     <item>API returns misleading error codes or messages</item>
///     <item>API behavior differs from official documentation</item>
///     <item>API has undocumented rate limits or restrictions</item>
///   </list>
///   <para>
///     When using this attribute, always include a <see cref="BugDescription"/> explaining the quirk
///     and a <see cref="ReferenceUrl"/> linking to community discussions or documentation that confirms
///     this is Cloudflare behavior, not an SDK bug.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   [IntegrationTest]
///   [CloudflareInternalBug(
///     BugDescription = "Cloudflare returns 405 MethodNotAllowed instead of 404 NotFound for non-existent subscriptions",
///     ReferenceUrl = "https://community.cloudflare.com/t/put-method-not-allowed-for-the-api-token-authentication-scheme/381292")]
///   public async Task UpdateSubscription_NonExistent_ThrowsError()
///   {
///     // Assert against actual Cloudflare behavior (405), not expected REST behavior (404)
///   }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class CloudflareInternalBugAttribute : Attribute
{
  #region Properties

  /// <summary>
  ///   Gets or sets the description of the Cloudflare API bug or unexpected behavior.
  ///   This should explain what the expected behavior would be vs. what Cloudflare actually does.
  /// </summary>
  public string BugDescription { get; set; } = string.Empty;

  /// <summary>
  ///   Gets or sets a URL to Cloudflare Community, documentation, or other reference that confirms
  ///   this behavior is a known Cloudflare issue rather than an SDK bug.
  /// </summary>
  public string ReferenceUrl { get; set; } = string.Empty;

  #endregion
}
