namespace Cloudflare.NET.Tests.IntegrationTests;

using Cloudflare.NET.AuditLogs;
using Cloudflare.NET.AuditLogs.Models;
using Cloudflare.NET.Security.Firewall.Models;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for the User Audit Logs methods in <see cref="AuditLogsApi" /> (F15).
///   These tests interact with the live Cloudflare API and require an account-scoped API token.
///   <para>
///     <b>Note:</b> User audit logs show actions taken BY the authenticated user across all their accounts.
///     Logs are read-only and retained for 18 months (v1 API).
///   </para>
///   <para>
///     <b>Permissions:</b> Audit Logs is an ACCOUNT-scoped permission ("Audit Logs Read"), not user-scoped.
///     Missing permissions will be caught by the PermissionValidationTests that run first.
///   </para>
/// </summary>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class UserAuditLogsApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IAuditLogsApi _sut;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="UserAuditLogsApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides an account-scoped API client.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public UserAuditLogsApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut = fixture.AuditLogsApi;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region List User Audit Logs Tests (I01-I04)

  /// <summary>I01: Verifies that ListUserAuditLogsAsync returns a valid CursorPaginatedResult.</summary>
  [IntegrationTest]
  public async Task ListUserAuditLogsAsync_ReturnsValidResult()
  {
    // Arrange
    var filters = CreateDefaultFilters();

    // Act
    var result = await _sut.ListUserAuditLogsAsync(filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeNull();
  }

  /// <summary>I02: Verifies that returned logs have action timestamps populated.</summary>
  [IntegrationTest]
  public async Task ListUserAuditLogsAsync_LogsHaveTimestamps()
  {
    // Arrange
    var filters = CreateDefaultFilters();

    // Act
    var result = await _sut.ListUserAuditLogsAsync(filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeEmpty("test requires at least one audit log to validate timestamps");

    foreach (var log in result.Items)
    {
      log.Timestamp.Should().BeAfter(DateTime.MinValue, "when timestamp should be a valid date");
      log.Timestamp.Should().BeBefore(DateTime.UtcNow.AddMinutes(5), "when timestamp should not be in the future");
    }
  }

  /// <summary>I03: Verifies that actor info is populated (though email may be null for system/token actions).</summary>
  [IntegrationTest]
  public async Task ListUserAuditLogsAsync_ActorInfoRepresentsAuthenticatedUser()
  {
    // Arrange
    var filters = CreateDefaultFilters();

    // Act
    var result = await _sut.ListUserAuditLogsAsync(filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeEmpty("test requires at least one audit log to validate actor info");

    // For user audit logs, the actor should have some identifier (id, email, ip, or type)
    // Note: Actor.Email may be null for system actions or token-based operations
    var log = result.Items[0];
    log.Actor.Should().NotBeNull("log should have actor information");

    // At least one actor identifier should be present
    var hasActorIdentifier = !string.IsNullOrEmpty(log.Actor.Id)
                             || !string.IsNullOrEmpty(log.Actor.Email)
                             || !string.IsNullOrEmpty(log.Actor.Ip)
                             || !string.IsNullOrEmpty(log.Actor.Type);
    hasActorIdentifier.Should().BeTrue("actor should have at least one identifier (id, email, ip, or type)");
  }

  /// <summary>I04: Verifies that action info includes type and result.</summary>
  [IntegrationTest]
  public async Task ListUserAuditLogsAsync_ActionInfoIncludesTypeAndResult()
  {
    // Arrange
    var filters = CreateDefaultFilters();

    // Act
    var result = await _sut.ListUserAuditLogsAsync(filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeEmpty("test requires at least one audit log to validate action info");

    foreach (var log in result.Items)
    {
      log.Action.Should().NotBeNull("log should have action details");
      log.Action.Type.Should().NotBeNullOrEmpty("action should have a type");
      // Result is a non-nullable boolean - just verify it can be accessed (always true or false)
      _ = log.Action.Result;
    }
  }

  #endregion


  #region Filter Tests (I05-I09)

  /// <summary>I05: Verifies that filtering by date range returns only logs in range.</summary>
  [IntegrationTest]
  public async Task ListUserAuditLogsAsync_FilterByDateRange_ReturnsLogsInRange()
  {
    // Arrange - Last 3 days only
    var since = DateTime.UtcNow.AddDays(-3);
    var before = DateTime.UtcNow;
    var filters = new ListAuditLogsFilters(Since: since, Before: before, Limit: 50);

    // Act
    var result = await _sut.ListUserAuditLogsAsync(filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeEmpty("test requires at least one audit log to validate date filtering");

    foreach (var log in result.Items)
    {
      log.Timestamp.Should().BeOnOrAfter(since.AddMinutes(-5), "log should be after since filter");
      log.Timestamp.Should().BeOnOrBefore(before.AddMinutes(5), "log should be before the before filter");
    }
  }

  /// <summary>I07: Verifies that sorting ascending returns oldest first.</summary>
  [IntegrationTest]
  public async Task ListUserAuditLogsAsync_SortAscending_ReturnsOldestFirst()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(
      Direction: ListOrderDirection.Ascending,
      Since: DateTime.UtcNow.AddDays(-7),
      Before: DateTime.UtcNow,
      Limit: 10);

    // Act
    var result = await _sut.ListUserAuditLogsAsync(filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().HaveCountGreaterThanOrEqualTo(2, "test requires at least 2 logs to validate sort order");

    // Verify ascending order (oldest first)
    for (int i = 1; i < result.Items.Count; i++)
    {
      result.Items[i].Timestamp.Should().BeOnOrAfter(result.Items[i - 1].Timestamp.AddMinutes(-1),
        "logs should be in ascending order");
    }

  }

  /// <summary>I08: Verifies that sorting descending returns newest first.</summary>
  [IntegrationTest]
  public async Task ListUserAuditLogsAsync_SortDescending_ReturnsNewestFirst()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(
      Direction: ListOrderDirection.Descending,
      Since: DateTime.UtcNow.AddDays(-7),
      Before: DateTime.UtcNow,
      Limit: 10);

    // Act
    var result = await _sut.ListUserAuditLogsAsync(filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().HaveCountGreaterThanOrEqualTo(2, "test requires at least 2 logs to validate sort order");

    // Verify descending order (newest first)
    for (int i = 1; i < result.Items.Count; i++)
    {
      result.Items[i].Timestamp.Should().BeOnOrBefore(result.Items[i - 1].Timestamp.AddMinutes(1),
        "logs should be in descending order");
    }
  }

  /// <summary>I09: Verifies that limit per page restricts result count.</summary>
  [IntegrationTest]
  public async Task ListUserAuditLogsAsync_LimitPerPage_RestrictsResultCount()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(
      Limit: 5,
      Since: DateTime.UtcNow.AddDays(-7),
      Before: DateTime.UtcNow);

    // Act
    var result = await _sut.ListUserAuditLogsAsync(filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Count.Should().BeLessThanOrEqualTo(5);
  }

  #endregion


  #region Pagination Tests (I10-I11)

  /// <summary>I10: Verifies that cursor pagination works to navigate to second page.</summary>
  /// <remarks>
  ///   This test requires sufficient audit log activity to have multiple pages of results.
  ///   The test looks back 14 days with a page size of 5, requiring at least 6 audit logs.
  /// </remarks>
  [IntegrationTest(Skip = "Requires at least 6 audit logs in 14 days for pagination - test environment has insufficient API activity")]
  public async Task ListUserAuditLogsAsync_CursorPagination_NavigatesToSecondPage()
  {
    // Arrange - Get first page with small limit
    var filters = new ListAuditLogsFilters(
      Limit: 5,
      Since: DateTime.UtcNow.AddDays(-14),
      Before: DateTime.UtcNow);

    // Act
    var firstPage = await _sut.ListUserAuditLogsAsync(filters);

    // Assert - First page
    firstPage.Should().NotBeNull();
    firstPage.Items.Should().NotBeEmpty("test requires audit logs to validate pagination");

    // FAIL if there's not enough data for multiple pages - test environment must have sufficient activity
    firstPage.CursorInfo.Should().NotBeNull(
      "test requires at least 6 audit logs in the last 14 days to validate pagination - " +
      "ensure the test account has sufficient API activity");
    firstPage.CursorInfo!.Cursor.Should().NotBeNullOrEmpty(
      "test requires a valid cursor for pagination - ensure the test account has sufficient API activity");

    // Get second page
    var secondPageFilters = filters with { Cursor = firstPage.CursorInfo.Cursor };
    var secondPage = await _sut.ListUserAuditLogsAsync(secondPageFilters);

    secondPage.Should().NotBeNull();
    secondPage.Items.Should().NotBeEmpty("second page should have logs");

    // Second page should have different IDs than first page
    var firstPageIds = firstPage.Items.Select(l => l.Id).ToHashSet();
    var secondPageIds = secondPage.Items.Select(l => l.Id).ToHashSet();
    firstPageIds.Overlaps(secondPageIds).Should().BeFalse("pages should not have overlapping logs");
  }

  /// <summary>I11: Verifies that ListAllUserAuditLogsAsync iterates through all pages.</summary>
  [IntegrationTest]
  public async Task ListAllUserAuditLogsAsync_IteratesThroughAllPages()
  {
    // Arrange - Use filters to limit scope
    var filters = new ListAuditLogsFilters(
      Limit: 10,
      Since: DateTime.UtcNow.AddDays(-3),
      Before: DateTime.UtcNow);

    // Act
    var logs = new List<AuditLog>();
    var maxLogs = 30; // Safety limit
    await foreach (var log in _sut.ListAllUserAuditLogsAsync(filters))
    {
      logs.Add(log);
      if (logs.Count >= maxLogs)
        break;
    }

    // Assert
    logs.Should().NotBeEmpty("ListAllUserAuditLogsAsync should yield at least one log");
  }

  #endregion


  #region Edge Cases (I12-I15)

  /// <summary>I12: Verifies that future date range returns empty results without error.</summary>
  [IntegrationTest]
  public async Task ListUserAuditLogsAsync_FutureDateRange_ReturnsEmptyResults()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(
      Since: DateTime.UtcNow.AddDays(1),
      Before: DateTime.UtcNow.AddDays(2));

    // Act
    var result = await _sut.ListUserAuditLogsAsync(filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().BeEmpty("no logs should exist in the future");
  }

  /// <summary>I13: Verifies that old date filter (>18 months) is rejected by the API.</summary>
  /// <remarks>
  ///   The v1 User Audit Logs API retains logs for 18 months. When requesting data outside
  ///   the retention window, the API returns a 400 Bad Request error with code 113.
  /// </remarks>
  [IntegrationTest]
  public async Task ListUserAuditLogsAsync_OldDateFilter_RejectedDueToRetention()
  {
    // Arrange - Filter for >18 months ago (outside v1 API retention period)
    var filters = new ListAuditLogsFilters(
      Since: DateTime.UtcNow.AddMonths(-20),
      Before: DateTime.UtcNow.AddMonths(-19));

    // Act & Assert - The API should reject the request with a 400 error
    // because the date range is outside the 18-month retention window
    Func<Task> act = async () => await _sut.ListUserAuditLogsAsync(filters);

    await act.Should().ThrowAsync<HttpRequestException>()
      .WithMessage("*18 months*", "API should reject dates outside retention window");
  }

  /// <summary>I14: Verifies that the API responds to valid authentication.</summary>
  [IntegrationTest]
  public async Task ListUserAuditLogsAsync_ValidToken_ReturnsResult()
  {
    // Arrange
    var filters = CreateDefaultFilters();

    // Act
    var result = await _sut.ListUserAuditLogsAsync(filters);

    // Assert - If we get here with a valid token, we should have a result
    result.Should().NotBeNull();
  }

  /// <summary>I15: Verifies that invalid cursor is handled gracefully (either error or empty results).</summary>
  [IntegrationTest]
  public async Task ListUserAuditLogsAsync_InvalidCursor_HandledGracefully()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(
      Cursor: "invalid-cursor-that-does-not-exist-12345",
      Since: DateTime.UtcNow.AddDays(-7),
      Before: DateTime.UtcNow);

    // Act & Assert - The API may return empty results, an error, or ignore the invalid cursor
    // Any of these behaviors is acceptable - what matters is it doesn't crash
    Func<Task> act = async () => await _sut.ListUserAuditLogsAsync(filters);

    // Either succeeds (with empty or non-empty results) or throws a handled exception
    await act.Should().NotThrowAsync<Exception>("API should handle invalid cursor gracefully");
  }

  #endregion


  #region Helper Methods

  /// <summary>Creates default filters with both required since and before parameters.</summary>
  /// <param name="daysBack">Number of days back for the since parameter (default 7).</param>
  /// <param name="limit">Maximum number of results (default 10).</param>
  /// <returns>A ListAuditLogsFilters with required time range parameters.</returns>
  private static ListAuditLogsFilters CreateDefaultFilters(int daysBack = 7, int limit = 10)
  {
    return new ListAuditLogsFilters(
      Since: DateTime.UtcNow.AddDays(-daysBack),
      Before: DateTime.UtcNow,
      Limit: limit);
  }

  #endregion
}
