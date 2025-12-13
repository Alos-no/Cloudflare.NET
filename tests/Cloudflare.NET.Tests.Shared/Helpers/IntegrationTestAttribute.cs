namespace Cloudflare.NET.Tests.Shared.Helpers;

using Xunit;

/// <summary>
///   A custom attribute for integration tests. This attribute automatically skips the test if any required secrets
///   are not configured. It discovers all <see cref="IIntegrationTestSecretValidator" /> implementations in the loaded
///   assemblies to determine the full set of required secrets.
/// </summary>
/// <remarks>
///   <para>
///     This attribute extends <see cref="SkippableFactAttribute" /> to support dynamic runtime skipping
///     via <see cref="Skip.If" /> and <see cref="SkipException" />. This allows tests to be skipped
///     at runtime when permission validation fails.
///   </para>
///   <para>
///     For parameterized tests (tests with <c>[InlineData]</c>), use <see cref="IntegrationTestTheoryAttribute" /> instead.
///   </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IntegrationTestAttribute : SkippableFactAttribute
{
  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="IntegrationTestAttribute" /> class.</summary>
  public IntegrationTestAttribute()
  {
    var skipMessage = IntegrationTestHelper.GetSkipMessage();

    if (skipMessage is not null)
      Skip = skipMessage;
  }

  #endregion
}
