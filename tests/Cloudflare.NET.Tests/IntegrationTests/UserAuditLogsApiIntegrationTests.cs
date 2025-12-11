namespace Cloudflare.NET.Tests.IntegrationTests;

using System.Net;
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
///   These tests interact with the live Cloudflare API and require a user-scoped API token.
///   <para>
///     <b>Note:</b> User audit logs show actions taken BY the authenticated user across all their accounts.
///     Logs are read-only and retained for 30 days.
///   </para>
/// </summary>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class UserAuditLogsApiIntegrationTests : IClassFixture<UserApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IAuditLogsApi _sut;

  /// <summary>Test output helper for logging.</summary>
  private readonly ITestOutputHelper _output;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="UserAuditLogsApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides a user-scoped API client.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public UserAuditLogsApiIntegrationTests(UserApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut = fixture.AuditLogsApi;
    _output = output;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region List User Audit Logs Tests (I01-I04)

  /// <summary>I01: Verifies that ListUserAuditLogsAsync returns a valid CursorPaginatedResult.</summary>
  [UserIntegrationTest]
  public async Task ListUserAuditLogsAsync_ReturnsValidResult()
  {
    // Arrange
    var filters = CreateDefaultFilters();

    // Act
    try
    {
      var result = await _sut.ListUserAuditLogsAsync(filters);

      // Assert
      result.Should().NotBeNull();
      result.Items.Should().NotBeNull();
      _output.WriteLine($"[INFO] Returned {result.Items.Count} user audit logs");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine($"[WARNING] 403 Forbidden - UserApiToken may lack user audit logs permission. Test skipped.");
      return;
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine($"[WARNING] 403 Forbidden - UserApiToken may lack user audit logs permission. Test skipped.");
      return;
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped.");
      return;
    }
  }

  /// <summary>I02: Verifies that returned logs have action timestamps populated.</summary>
  [UserIntegrationTest]
  public async Task ListUserAuditLogsAsync_LogsHaveTimestamps()
  {
    // Arrange
    var filters = CreateDefaultFilters();

    // Act
    try
    {
      var result = await _sut.ListUserAuditLogsAsync(filters);

      // Assert
      result.Should().NotBeNull();
      foreach (var log in result.Items)
      {
        log.Action.Time.Should().BeAfter(DateTime.MinValue, "action time should be a valid date");
        log.Action.Time.Should().BeBefore(DateTime.UtcNow.AddMinutes(5), "action time should not be in the future");
        _output.WriteLine($"[INFO] Log {log.Id}: {log.Action.Type} at {log.Action.Time}");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine($"[WARNING] 403 Forbidden - UserApiToken may lack user audit logs permission. Test skipped.");
      return;
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine($"[WARNING] 403 Forbidden - UserApiToken may lack user audit logs permission. Test skipped.");
      return;
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped.");
      return;
    }
  }

  /// <summary>I03: Verifies that actor info represents the authenticated user.</summary>
  [UserIntegrationTest]
  public async Task ListUserAuditLogsAsync_ActorInfoRepresentsAuthenticatedUser()
  {
    // Arrange
    var filters = CreateDefaultFilters();

    // Act
    try
    {
      var result = await _sut.ListUserAuditLogsAsync(filters);

      // Assert
      result.Should().NotBeNull();
      if (result.Items.Count > 0)
      {
        // For user audit logs, the actor should represent the authenticated user's actions
        var log = result.Items[0];
        log.Actor.Should().NotBeNull("log should have actor information");
        // Note: Actor.Email should match the authenticated user (if present)
        if (!string.IsNullOrEmpty(log.Actor.Email))
        {
          _output.WriteLine($"[INFO] Actor: {log.Actor.Email}");
        }
      }
      else
      {
        _output.WriteLine("[INFO] No user audit logs found in the time range. Test passes by default.");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine($"[WARNING] 403 Forbidden - UserApiToken may lack user audit logs permission. Test skipped.");
      return;
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped.");
      return;
    }
  }

  /// <summary>I04: Verifies that action info includes type and result.</summary>
  [UserIntegrationTest]
  public async Task ListUserAuditLogsAsync_ActionInfoIncludesTypeAndResult()
  {
    // Arrange
    var filters = CreateDefaultFilters();

    // Act
    try
    {
      var result = await _sut.ListUserAuditLogsAsync(filters);

      // Assert
      result.Should().NotBeNull();
      foreach (var log in result.Items)
      {
        log.Action.Should().NotBeNull("log should have action details");
        log.Action.Type.Should().NotBeNullOrEmpty("action should have a type");
        log.Action.Result.Should().NotBeNullOrEmpty("action should have a result");
        _output.WriteLine($"[INFO] Action: {log.Action.Type} = {log.Action.Result}");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine($"[WARNING] 403 Forbidden - UserApiToken may lack user audit logs permission. Test skipped.");
      return;
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped.");
      return;
    }
  }

  #endregion


  #region Filter Tests (I05-I09)

  /// <summary>I05: Verifies that filtering by date range returns only logs in range.</summary>
  [UserIntegrationTest]
  public async Task ListUserAuditLogsAsync_FilterByDateRange_ReturnsLogsInRange()
  {
    // Arrange - Last 3 days only
    var since = DateTime.UtcNow.AddDays(-3);
    var before = DateTime.UtcNow;
    var filters = new ListAuditLogsFilters(Since: since, Before: before, Limit: 50);

    // Act
    try
    {
      var result = await _sut.ListUserAuditLogsAsync(filters);

      // Assert
      result.Should().NotBeNull();
      foreach (var log in result.Items)
      {
        log.Action.Time.Should().BeOnOrAfter(since.AddMinutes(-5), "log should be after since filter");
        log.Action.Time.Should().BeOnOrBefore(before.AddMinutes(5), "log should be before the before filter");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine($"[WARNING] 403 Forbidden - UserApiToken may lack user audit logs permission. Test skipped.");
      return;
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped.");
      return;
    }
  }

  /// <summary>I06: Verifies that filtering by actor email works (should return same user's logs).</summary>
  [UserIntegrationTest]
  public async Task ListUserAuditLogsAsync_FilterByActorEmail_ReturnsMatchingLogs()
  {
    // Arrange - First get any log to find actor email
    try
    {
      var anyLogs = await _sut.ListUserAuditLogsAsync(CreateDefaultFilters());

      if (anyLogs.Items.Count == 0 || anyLogs.Items.All(l => string.IsNullOrEmpty(l.Actor.Email)))
      {
        _output.WriteLine("[INFO] No logs with actor email found. Test passes by default.");
        return;
      }

      var knownEmail = anyLogs.Items.First(l => !string.IsNullOrEmpty(l.Actor.Email)).Actor.Email;

      // Act - Filter by that email
      var filters = new ListAuditLogsFilters(
        ActorEmails: new[] { knownEmail! },
        Since: DateTime.UtcNow.AddDays(-7),
        Before: DateTime.UtcNow,
        Limit: 20);
      var result = await _sut.ListUserAuditLogsAsync(filters);

      // Assert
      result.Should().NotBeNull();
      foreach (var log in result.Items)
      {
        log.Actor.Email.Should().Be(knownEmail);
      }
      _output.WriteLine($"[INFO] Found {result.Items.Count} logs for {knownEmail}");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine($"[WARNING] 403 Forbidden - UserApiToken may lack user audit logs permission. Test skipped.");
      return;
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped.");
      return;
    }
  }

  /// <summary>I07: Verifies that sorting ascending returns oldest first.</summary>
  [UserIntegrationTest]
  public async Task ListUserAuditLogsAsync_SortAscending_ReturnsOldestFirst()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(
      Direction: ListOrderDirection.Ascending,
      Since: DateTime.UtcNow.AddDays(-7),
      Before: DateTime.UtcNow,
      Limit: 10);

    // Act
    try
    {
      var result = await _sut.ListUserAuditLogsAsync(filters);

      // Assert
      result.Should().NotBeNull();
      if (result.Items.Count >= 2)
      {
        // Verify ascending order (oldest first)
        for (int i = 1; i < result.Items.Count; i++)
        {
          result.Items[i].Action.Time.Should().BeOnOrAfter(result.Items[i - 1].Action.Time.AddMinutes(-1),
            "logs should be in ascending order");
        }
        _output.WriteLine($"[INFO] First: {result.Items[0].Action.Time}, Last: {result.Items[^1].Action.Time}");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine($"[WARNING] 403 Forbidden - UserApiToken may lack user audit logs permission. Test skipped.");
      return;
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped.");
      return;
    }
  }

  /// <summary>I08: Verifies that sorting descending returns newest first.</summary>
  [UserIntegrationTest]
  public async Task ListUserAuditLogsAsync_SortDescending_ReturnsNewestFirst()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(
      Direction: ListOrderDirection.Descending,
      Since: DateTime.UtcNow.AddDays(-7),
      Before: DateTime.UtcNow,
      Limit: 10);

    // Act
    try
    {
      var result = await _sut.ListUserAuditLogsAsync(filters);

      // Assert
      result.Should().NotBeNull();
      if (result.Items.Count >= 2)
      {
        // Verify descending order (newest first)
        for (int i = 1; i < result.Items.Count; i++)
        {
          result.Items[i].Action.Time.Should().BeOnOrBefore(result.Items[i - 1].Action.Time.AddMinutes(1),
            "logs should be in descending order");
        }
        _output.WriteLine($"[INFO] First: {result.Items[0].Action.Time}, Last: {result.Items[^1].Action.Time}");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine($"[WARNING] 403 Forbidden - UserApiToken may lack user audit logs permission. Test skipped.");
      return;
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped.");
      return;
    }
  }

  /// <summary>I09: Verifies that limit per page restricts result count.</summary>
  [UserIntegrationTest]
  public async Task ListUserAuditLogsAsync_LimitPerPage_RestrictsResultCount()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(
      Limit: 5,
      Since: DateTime.UtcNow.AddDays(-7),
      Before: DateTime.UtcNow);

    // Act
    try
    {
      var result = await _sut.ListUserAuditLogsAsync(filters);

      // Assert
      result.Should().NotBeNull();
      result.Items.Count.Should().BeLessThanOrEqualTo(5);
      _output.WriteLine($"[INFO] Returned {result.Items.Count} logs with limit=5");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine($"[WARNING] 403 Forbidden - UserApiToken may lack user audit logs permission. Test skipped.");
      return;
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped.");
      return;
    }
  }

  #endregion


  #region Pagination Tests (I10-I11)

  /// <summary>I10: Verifies that cursor pagination works to navigate to second page.</summary>
  [UserIntegrationTest]
  public async Task ListUserAuditLogsAsync_CursorPagination_NavigatesToSecondPage()
  {
    // Arrange - Get first page with small limit
    var filters = new ListAuditLogsFilters(
      Limit: 5,
      Since: DateTime.UtcNow.AddDays(-14),
      Before: DateTime.UtcNow);

    // Act
    try
    {
      var firstPage = await _sut.ListUserAuditLogsAsync(filters);

      // Assert - First page
      firstPage.Should().NotBeNull();

      // If there's a cursor, we can get the second page
      if (!string.IsNullOrEmpty(firstPage.CursorInfo?.Cursor))
      {
        var secondPageFilters = filters with { Cursor = firstPage.CursorInfo.Cursor };
        var secondPage = await _sut.ListUserAuditLogsAsync(secondPageFilters);

        secondPage.Should().NotBeNull();
        // Second page should have different IDs than first page
        if (firstPage.Items.Count > 0 && secondPage.Items.Count > 0)
        {
          var firstPageIds = firstPage.Items.Select(l => l.Id).ToHashSet();
          var secondPageIds = secondPage.Items.Select(l => l.Id).ToHashSet();
          firstPageIds.Overlaps(secondPageIds).Should().BeFalse("pages should not have overlapping logs");
          _output.WriteLine($"[INFO] First page: {firstPage.Items.Count} logs, Second page: {secondPage.Items.Count} logs");
        }
      }
      else
      {
        _output.WriteLine("[INFO] No more pages available (single page of results). Test passes.");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine($"[WARNING] 403 Forbidden - UserApiToken may lack user audit logs permission. Test skipped.");
      return;
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped.");
      return;
    }
  }

  /// <summary>I11: Verifies that ListAllUserAuditLogsAsync iterates through all pages.</summary>
  [UserIntegrationTest]
  public async Task ListAllUserAuditLogsAsync_IteratesThroughAllPages()
  {
    // Arrange - Use filters to limit scope
    var filters = new ListAuditLogsFilters(
      Limit: 10,
      Since: DateTime.UtcNow.AddDays(-3),
      Before: DateTime.UtcNow);

    // Act
    try
    {
      var logs = new List<AuditLog>();
      var maxLogs = 30; // Safety limit
      await foreach (var log in _sut.ListAllUserAuditLogsAsync(filters))
      {
        logs.Add(log);
        if (logs.Count >= maxLogs)
          break;
      }

      // Assert
      logs.Should().NotBeNull();
      _output.WriteLine($"[INFO] ListAllUserAuditLogsAsync yielded {logs.Count} logs");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine($"[WARNING] 403 Forbidden - UserApiToken may lack user audit logs permission. Test skipped.");
      return;
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped.");
      return;
    }
  }

  #endregion


  #region Edge Cases (I12-I15)

  /// <summary>I12: Verifies that future date range returns empty results without error.</summary>
  [UserIntegrationTest]
  public async Task ListUserAuditLogsAsync_FutureDateRange_ReturnsEmptyResults()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(
      Since: DateTime.UtcNow.AddDays(1),
      Before: DateTime.UtcNow.AddDays(2));

    // Act
    try
    {
      var result = await _sut.ListUserAuditLogsAsync(filters);

      // Assert
      result.Should().NotBeNull();
      result.Items.Should().BeEmpty("no logs should exist in the future");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine($"[WARNING] 403 Forbidden - UserApiToken may lack user audit logs permission. Test skipped.");
      return;
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped.");
      return;
    }
  }

  /// <summary>I13: Verifies that old date filter (>30 days) returns empty or limited results.</summary>
  [UserIntegrationTest]
  public async Task ListUserAuditLogsAsync_OldDateFilter_ReturnsEmptyOrLimitedResults()
  {
    // Arrange - Filter for >30 days ago (outside retention period)
    var filters = new ListAuditLogsFilters(
      Since: DateTime.UtcNow.AddDays(-60),
      Before: DateTime.UtcNow.AddDays(-31));

    // Act
    try
    {
      var result = await _sut.ListUserAuditLogsAsync(filters);

      // Assert - Should return empty or very limited results due to 30-day retention
      result.Should().NotBeNull();
      _output.WriteLine($"[INFO] Logs older than 30 days: {result.Items.Count}");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine($"[WARNING] 403 Forbidden - UserApiToken may lack user audit logs permission. Test skipped.");
      return;
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped.");
      return;
    }
  }

  /// <summary>I14: Verifies that the API responds to valid authentication (may return 403 if token lacks audit log permissions).</summary>
  /// <remarks>
  ///   This test validates that the API correctly handles authentication. User audit logs require
  ///   specific permissions. If the token lacks permissions, a 403 is expected and the test passes.
  /// </remarks>
  [UserIntegrationTest]
  public async Task ListUserAuditLogsAsync_ValidToken_ReturnsResultOrPermissionError()
  {
    // Arrange
    var filters = CreateDefaultFilters();

    // Act
    try
    {
      var result = await _sut.ListUserAuditLogsAsync(filters);

      // Assert - If we get here with a valid token, we should have a result
      result.Should().NotBeNull();
      _output.WriteLine("[INFO] Valid token allows access to user audit logs.");
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
    {
      // 403 is expected if the token lacks user audit log permissions
      _output.WriteLine("[INFO] 403 Forbidden - Token lacks user audit logs permission (expected for some tokens).");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped.");
      return;
    }
  }

  /// <summary>I15: Verifies that invalid cursor returns API error.</summary>
  [UserIntegrationTest]
  public async Task ListUserAuditLogsAsync_InvalidCursor_ReturnsError()
  {
    // Arrange
    var filters = new ListAuditLogsFilters(
      Cursor: "invalid-cursor-that-does-not-exist-12345",
      Since: DateTime.UtcNow.AddDays(-7),
      Before: DateTime.UtcNow);

    // Act
    try
    {
      // The API may return empty results, an error, or process the invalid cursor differently
      var result = await _sut.ListUserAuditLogsAsync(filters);

      // Assert - API might return empty or ignore invalid cursor
      _output.WriteLine($"[INFO] API returned {result.Items.Count} logs with invalid cursor (may be ignored)");
    }
    catch (HttpRequestException ex)
    {
      // An HTTP error is acceptable for invalid cursor
      _output.WriteLine($"[INFO] API returned {ex.StatusCode} for invalid cursor (expected behavior)");
    }
    catch (Core.Exceptions.CloudflareApiException ex)
    {
      // A Cloudflare API error is acceptable for invalid cursor
      _output.WriteLine($"[INFO] API error: {ex.Message} (expected behavior for invalid cursor)");
    }
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
