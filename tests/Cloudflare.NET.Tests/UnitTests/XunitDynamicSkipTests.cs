namespace Cloudflare.NET.Tests.UnitTests;

/// <summary>
///   Tests to verify that Xunit.SkippableFact's Skip.If() mechanism works correctly.
///   These tests demonstrate dynamic runtime skipping.
/// </summary>
public class XunitDynamicSkipTests
{
  /// <summary>
  ///   This test should be SKIPPED (not failed) because it calls Skip.If(true).
  ///   This verifies Xunit.SkippableFact handles it correctly.
  /// </summary>
  [SkippableFact]
  public void Test_ShouldBeSkipped_WhenSkipIfCalledWithTrue()
  {
    // This test deliberately skips using Skip.If.
    // If Xunit.SkippableFact handles this correctly, this test will show as "Skipped".
    Skip.If(true, "This test is intentionally skipped to verify the Skip.If() mechanism works.");

    // This line should never be reached.
    Assert.Fail("This should not be reached!");
  }

  /// <summary>
  ///   This test should PASS - Skip.If(false) should not skip.
  /// </summary>
  [SkippableFact]
  public void Test_ShouldPass_WhenSkipIfCalledWithFalse()
  {
    Skip.If(false, "This should not trigger a skip.");
    Assert.True(true);
  }

  /// <summary>
  ///   This test should PASS - it's a normal Fact to verify the test run itself works.
  /// </summary>
  [Fact]
  public void Test_ShouldPass_NormalFact()
  {
    Assert.True(true);
  }
}
