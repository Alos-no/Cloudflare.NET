namespace Cloudflare.NET.Tests.Shared.Helpers;

using Xunit;

/// <summary>
///   A custom attribute for parameterized integration tests. This attribute automatically skips the test if any
///   required secrets are not configured. It discovers all <see cref="IIntegrationTestSecretValidator" />
///   implementations in the loaded assemblies to determine the full set of required secrets.
/// </summary>
/// <remarks>
///   <para>
///     This attribute extends <see cref="SkippableTheoryAttribute" /> to support dynamic runtime skipping
///     via <see cref="Skip.If" /> and <see cref="SkipException" />. This allows tests to be skipped
///     at runtime when permission validation fails.
///   </para>
///   <para>
///     Use this attribute with <c>[InlineData]</c>, <c>[MemberData]</c>, or other data attributes for
///     parameterized tests. For non-parameterized tests, use <see cref="IntegrationTestAttribute" /> instead.
///   </para>
/// </remarks>
/// <example>
///   <code>
///     [IntegrationTestTheory]
///     [InlineData("value1")]
///     [InlineData("value2")]
///     public async Task MyTest_WithDifferentInputs(string input)
///     {
///       // Test implementation
///     }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IntegrationTestTheoryAttribute : SkippableTheoryAttribute
{
  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="IntegrationTestTheoryAttribute" /> class.</summary>
  public IntegrationTestTheoryAttribute()
  {
    var skipMessage = IntegrationTestHelper.GetSkipMessage();

    if (skipMessage is not null)
      Skip = skipMessage;
  }

  #endregion
}
