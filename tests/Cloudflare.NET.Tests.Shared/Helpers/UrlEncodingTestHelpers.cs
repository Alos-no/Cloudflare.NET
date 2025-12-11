namespace Cloudflare.NET.Tests.Shared.Helpers;

using Xunit;

/// <summary>
///   Helpers for validating URL encoding in request URIs.
///   Use this to verify that special characters in IDs are properly encoded.
/// </summary>
public static class UrlEncodingTestHelpers
{
  #region Methods

  /// <summary>
  ///   Asserts that a captured request has the expected path with properly encoded segments.
  /// </summary>
  /// <param name="request">The captured HttpRequestMessage.</param>
  /// <param name="expectedPathTemplate">Path template like "/zones/{0}/dns_records/{1}".</param>
  /// <param name="pathParams">Values that should be URL-encoded in the path.</param>
  public static void AssertPathIsEncoded(
    HttpRequestMessage request,
    string expectedPathTemplate,
    params string[] pathParams)
  {
    var encodedParams = pathParams.Select(Uri.EscapeDataString).ToArray();
    var expectedPath = string.Format(expectedPathTemplate, encodedParams);

    Assert.Equal(expectedPath, request.RequestUri?.AbsolutePath);
  }

  /// <summary>
  ///   Asserts that a query parameter is properly encoded.
  /// </summary>
  /// <param name="request">The captured HttpRequestMessage.</param>
  /// <param name="paramName">Query parameter name.</param>
  /// <param name="expectedValue">Raw value (will be encoded for comparison).</param>
  public static void AssertQueryParamIsEncoded(
    HttpRequestMessage request,
    string paramName,
    string expectedValue)
  {
    var query = request.RequestUri?.Query ?? string.Empty;
    var expectedEncoded = $"{paramName}={Uri.EscapeDataString(expectedValue)}";

    Assert.Contains(expectedEncoded, query);
  }

  /// <summary>
  ///   Asserts that the request path contains a properly encoded value.
  /// </summary>
  /// <param name="request">The captured HttpRequestMessage.</param>
  /// <param name="rawValue">The raw value that should appear encoded in the path.</param>
  public static void AssertPathContainsEncoded(
    HttpRequestMessage request,
    string rawValue)
  {
    var path = request.RequestUri?.AbsolutePath ?? string.Empty;
    var encodedValue = Uri.EscapeDataString(rawValue);

    Assert.Contains(encodedValue, path);
  }

  /// <summary>
  ///   Asserts that special characters in path parameters are properly encoded.
  /// </summary>
  /// <param name="request">The captured HttpRequestMessage.</param>
  /// <param name="specialChars">Characters that should be encoded (e.g., "/", "+", " ").</param>
  public static void AssertSpecialCharsEncoded(
    HttpRequestMessage request,
    params char[] specialChars)
  {
    var path = request.RequestUri?.AbsolutePath ?? string.Empty;

    // Skip the leading slash and protocol segments
    var pathWithoutLeadingSlash = path.TrimStart('/');

    foreach (var c in specialChars)
    {
      var encoded = Uri.EscapeDataString(c.ToString());

      // Only check chars that should be encoded (their encoded form differs from raw)
      if (encoded != c.ToString())
      {
        // The raw character should not appear unencoded in the path segments
        // (except for the path separator slashes which are allowed)
        var segments = pathWithoutLeadingSlash.Split('/');
        foreach (var segment in segments)
        {
          Assert.DoesNotContain(c.ToString(), segment);
        }
      }
    }
  }

  /// <summary>
  ///   Asserts that a request URI matches the expected pattern with encoded path segments.
  /// </summary>
  /// <param name="request">The captured HttpRequestMessage.</param>
  /// <param name="expectedMethod">Expected HTTP method.</param>
  /// <param name="expectedPathTemplate">Path template like "/client/v4/zones/{0}".</param>
  /// <param name="pathParams">Values that should be URL-encoded in the path.</param>
  public static void AssertRequestWithEncodedPath(
    HttpRequestMessage request,
    HttpMethod expectedMethod,
    string expectedPathTemplate,
    params string[] pathParams)
  {
    Assert.Equal(expectedMethod, request.Method);
    AssertPathIsEncoded(request, expectedPathTemplate, pathParams);
  }

  /// <summary>
  ///   Verifies that all query parameters in the request are properly formatted.
  /// </summary>
  /// <param name="request">The captured HttpRequestMessage.</param>
  /// <param name="expectedParams">Dictionary of expected parameter names and raw values.</param>
  public static void AssertQueryParamsEncoded(
    HttpRequestMessage request,
    Dictionary<string, string> expectedParams)
  {
    foreach (var (paramName, rawValue) in expectedParams)
    {
      AssertQueryParamIsEncoded(request, paramName, rawValue);
    }
  }

  #endregion


  #region Test Values

  /// <summary>
  ///   Common test values for URL encoding tests.
  /// </summary>
  public static class TestValues
  {
    /// <summary>Value containing forward slashes.</summary>
    public const string WithSlash = "value/with/slashes";

    /// <summary>Value containing plus signs.</summary>
    public const string WithPlus = "value+with+plus";

    /// <summary>Value containing spaces.</summary>
    public const string WithSpace = "value with spaces";

    /// <summary>Value containing special characters.</summary>
    public const string WithSpecialChars = "value@#$%^&*";

    /// <summary>Value containing unicode characters (é è).</summary>
    public const string WithUnicode = "valueéè";

    /// <summary>Value containing ampersand (important for query strings).</summary>
    public const string WithAmpersand = "value&param=test";

    /// <summary>Value containing equals sign.</summary>
    public const string WithEquals = "value=test";

    /// <summary>Value containing question mark.</summary>
    public const string WithQuestionMark = "value?query";

    /// <summary>Value containing hash/fragment identifier.</summary>
    public const string WithHash = "value#fragment";

    /// <summary>All test values for comprehensive testing.</summary>
    public static IEnumerable<string> All =>
    [
      WithSlash,
      WithPlus,
      WithSpace,
      WithSpecialChars,
      WithUnicode,
      WithAmpersand,
      WithEquals,
      WithQuestionMark,
      WithHash
    ];

    /// <summary>Characters that require URL encoding in path segments.</summary>
    public static char[] SpecialCharsForPath => ['/', '+', ' ', '@', '#', '$', '%', '^', '&', '*', '?', '='];

    /// <summary>Characters that require URL encoding in query parameters.</summary>
    public static char[] SpecialCharsForQuery => ['+', ' ', '&', '=', '#'];
  }

  #endregion
}
