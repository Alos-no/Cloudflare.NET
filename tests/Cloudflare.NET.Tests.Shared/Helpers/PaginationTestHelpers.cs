namespace Cloudflare.NET.Tests.Shared.Helpers;

using Cloudflare.NET.Core.Models;
using Xunit;

/// <summary>
///   Helpers for testing pagination behavior.
/// </summary>
public static class PaginationTestHelpers
{
  #region Page-Based Pagination

  /// <summary>
  ///   Asserts that a PagePaginatedResult has the expected pagination metadata.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="result">The paginated result.</param>
  /// <param name="expectedPage">Expected current page.</param>
  /// <param name="expectedPerPage">Expected items per page.</param>
  /// <param name="expectedTotalCount">Expected total count.</param>
  /// <param name="expectedTotalPages">Expected total pages.</param>
  public static void AssertPageInfo<T>(
    PagePaginatedResult<T> result,
    int expectedPage,
    int expectedPerPage,
    int expectedTotalCount,
    int expectedTotalPages)
  {
    Assert.NotNull(result.PageInfo);
    Assert.Equal(expectedPage, result.PageInfo!.Page);
    Assert.Equal(expectedPerPage, result.PageInfo.PerPage);
    Assert.Equal(expectedTotalCount, result.PageInfo.TotalCount);
    Assert.Equal(expectedTotalPages, result.PageInfo.TotalPages);
  }

  /// <summary>
  ///   Asserts that a PagePaginatedResult has the expected item count.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="result">The paginated result.</param>
  /// <param name="expectedCount">Expected item count on this page.</param>
  public static void AssertPageItemCount<T>(
    PagePaginatedResult<T> result,
    int expectedCount)
  {
    Assert.NotNull(result.PageInfo);
    Assert.Equal(expectedCount, result.PageInfo!.Count);
    Assert.Equal(expectedCount, result.Items.Count);
  }

  /// <summary>
  ///   Asserts that ListAll correctly iterates through multiple pages.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="items">Items yielded from ListAll.</param>
  /// <param name="expectedCount">Expected total item count.</param>
  /// <param name="capturedRequests">Captured HTTP requests for verification.</param>
  /// <param name="expectedPageCount">Expected number of page requests made.</param>
  public static void AssertPaginationIteratedAllPages<T>(
    List<T> items,
    int expectedCount,
    List<HttpRequestMessage> capturedRequests,
    int expectedPageCount)
  {
    Assert.Equal(expectedCount, items.Count);
    Assert.Equal(expectedPageCount, capturedRequests.Count);

    // Verify page numbers in requests
    for (var i = 0; i < expectedPageCount; i++)
    {
      var query = capturedRequests[i].RequestUri?.Query ?? string.Empty;
      Assert.Contains($"page={i + 1}", query);
    }
  }

  /// <summary>
  ///   Asserts that an empty paginated result is handled correctly.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="result">The paginated result.</param>
  public static void AssertEmptyPageResult<T>(PagePaginatedResult<T> result)
  {
    Assert.Empty(result.Items);
    Assert.NotNull(result.PageInfo);
    Assert.Equal(0, result.PageInfo!.TotalCount);
    Assert.Equal(0, result.PageInfo.TotalPages);
  }

  /// <summary>
  ///   Asserts that the pagination has more pages available.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="result">The paginated result.</param>
  public static void AssertHasMorePages<T>(PagePaginatedResult<T> result)
  {
    Assert.NotNull(result.PageInfo);
    Assert.True(result.PageInfo!.Page < result.PageInfo.TotalPages,
      $"Expected more pages but current page {result.PageInfo.Page} >= total pages {result.PageInfo.TotalPages}");
  }

  /// <summary>
  ///   Asserts that the pagination is on the last page.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="result">The paginated result.</param>
  public static void AssertIsLastPage<T>(PagePaginatedResult<T> result)
  {
    Assert.NotNull(result.PageInfo);
    Assert.Equal(result.PageInfo!.TotalPages, result.PageInfo.Page);
  }

  #endregion


  #region Cursor-Based Pagination

  /// <summary>
  ///   Asserts that a CursorPaginatedResult has the expected cursor metadata.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="result">The cursor-paginated result.</param>
  /// <param name="expectedCount">Expected item count on this page.</param>
  /// <param name="hasMorePages">Whether more pages are expected.</param>
  public static void AssertCursorInfo<T>(
    CursorPaginatedResult<T> result,
    int expectedCount,
    bool hasMorePages)
  {
    Assert.NotNull(result.CursorInfo);
    Assert.Equal(expectedCount, result.CursorInfo!.Count);

    if (hasMorePages)
    {
      Assert.NotNull(result.CursorInfo.Cursor);
    }
    else
    {
      Assert.Null(result.CursorInfo.Cursor);
    }
  }

  /// <summary>
  ///   Asserts that a CursorPaginatedResult has a cursor for the next page.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="result">The cursor-paginated result.</param>
  /// <returns>The cursor value for use in subsequent requests.</returns>
  public static string AssertHasCursor<T>(CursorPaginatedResult<T> result)
  {
    Assert.NotNull(result.CursorInfo);
    Assert.NotNull(result.CursorInfo!.Cursor);

    return result.CursorInfo.Cursor!;
  }

  /// <summary>
  ///   Asserts that a CursorPaginatedResult has no more pages.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="result">The cursor-paginated result.</param>
  public static void AssertNoCursor<T>(CursorPaginatedResult<T> result)
  {
    // CursorInfo can be null or Cursor can be null when there are no more pages
    Assert.True(result.CursorInfo == null || result.CursorInfo.Cursor == null,
      "Expected no cursor for last page");
  }

  /// <summary>
  ///   Asserts that an empty cursor-paginated result is handled correctly.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="result">The cursor-paginated result.</param>
  public static void AssertEmptyCursorResult<T>(CursorPaginatedResult<T> result)
  {
    Assert.Empty(result.Items);
    Assert.True(result.CursorInfo == null || result.CursorInfo.Cursor == null,
      "Expected no cursor for empty result");
  }

  /// <summary>
  ///   Asserts that cursor pagination correctly iterates through multiple pages.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="items">Items yielded from ListAll.</param>
  /// <param name="expectedCount">Expected total item count.</param>
  /// <param name="capturedRequests">Captured HTTP requests for verification.</param>
  /// <param name="expectedPageCount">Expected number of cursor requests made.</param>
  public static void AssertCursorPaginationIteratedAllPages<T>(
    List<T> items,
    int expectedCount,
    List<HttpRequestMessage> capturedRequests,
    int expectedPageCount)
  {
    Assert.Equal(expectedCount, items.Count);
    Assert.Equal(expectedPageCount, capturedRequests.Count);

    // First request should not have a cursor, subsequent requests should
    for (var i = 1; i < expectedPageCount; i++)
    {
      var query = capturedRequests[i].RequestUri?.Query ?? string.Empty;
      Assert.Contains("cursor=", query);
    }
  }

  #endregion


  #region Query Parameter Assertions

  /// <summary>
  ///   Asserts that pagination query parameters are correctly set in the request.
  /// </summary>
  /// <param name="request">The captured HTTP request.</param>
  /// <param name="expectedPage">Expected page number.</param>
  /// <param name="expectedPerPage">Expected per page value.</param>
  public static void AssertPageQueryParams(
    HttpRequestMessage request,
    int expectedPage,
    int expectedPerPage)
  {
    var query = request.RequestUri?.Query ?? string.Empty;

    Assert.Contains($"page={expectedPage}", query);
    Assert.Contains($"per_page={expectedPerPage}", query);
  }

  /// <summary>
  ///   Asserts that cursor pagination query parameters are correctly set.
  /// </summary>
  /// <param name="request">The captured HTTP request.</param>
  /// <param name="expectedCursor">Expected cursor value (null for first request).</param>
  /// <param name="expectedPerPage">Expected per page value.</param>
  public static void AssertCursorQueryParams(
    HttpRequestMessage request,
    string? expectedCursor,
    int expectedPerPage)
  {
    var query = request.RequestUri?.Query ?? string.Empty;

    Assert.Contains($"per_page={expectedPerPage}", query);

    if (expectedCursor != null)
    {
      Assert.Contains($"cursor={Uri.EscapeDataString(expectedCursor)}", query);
    }
    else
    {
      Assert.DoesNotContain("cursor=", query);
    }
  }

  #endregion


  #region Async Enumerable Helpers

  /// <summary>
  ///   Collects all items from an IAsyncEnumerable into a list.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="source">The async enumerable source.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List containing all items.</returns>
  public static async Task<List<T>> ToListAsync<T>(
    IAsyncEnumerable<T> source,
    CancellationToken cancellationToken = default)
  {
    var result = new List<T>();

    await foreach (var item in source.WithCancellation(cancellationToken))
    {
      result.Add(item);
    }

    return result;
  }

  /// <summary>
  ///   Collects items from an IAsyncEnumerable up to a specified count.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="source">The async enumerable source.</param>
  /// <param name="maxCount">Maximum items to collect.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>List containing up to maxCount items.</returns>
  public static async Task<List<T>> TakeAsync<T>(
    IAsyncEnumerable<T> source,
    int maxCount,
    CancellationToken cancellationToken = default)
  {
    var result = new List<T>();

    await foreach (var item in source.WithCancellation(cancellationToken))
    {
      result.Add(item);

      if (result.Count >= maxCount)
        break;
    }

    return result;
  }

  #endregion
}
