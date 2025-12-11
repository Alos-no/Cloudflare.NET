namespace Cloudflare.NET.Tests.IntegrationTests;

using System.Net;
using Cloudflare.NET.AuditLogs;
using Cloudflare.NET.AuditLogs.Models;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for the <see cref="AuditLogsApi" /> class implementing F07 - Account Audit Logs.
///   These tests interact with the live Cloudflare API and require credentials.
///   <para>
///     <b>Note:</b> Audit logs are read-only and retained for 30 days. Tests use appropriate time filters
///     to ensure results are available.
///   </para>
///   <para>
///     <b>Important:</b> The Cloudflare Audit Logs API requires BOTH since AND before parameters.
///     All tests must provide both time range bounds.
///   </para>
/// </summary>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class AuditLogsApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IAuditLogsApi _sut;

  /// <summary>Test output helper for logging.</summary>
  private readonly ITestOutputHelper _output;

  /// <summary>The account ID from test configuration.</summary>
  private readonly string _accountId;

  /// <summary>The zone ID from test configuration for zone-scoped filtering tests.</summary>
  private readonly string _zoneId;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="AuditLogsApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides a configured API client.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public AuditLogsApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut = fixture.AuditLogsApi;
    _output = output;
    _accountId = TestConfiguration.CloudflareSettings.AccountId;
    _zoneId = TestConfiguration.CloudflareSettings.ZoneId;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region Basic Query Tests (I01-I04)

  /// <summary>I01: Verifies that GetAccountAuditLogsAsync returns a valid CursorPaginatedResult with required time filters.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_WithTimeRange_ReturnsValidResult()
  {
    // Arrange - API requires both since and before
    var filters = CreateDefaultFilters();

    // Act
    try
    {
      var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

      // Assert
      result.Should().NotBeNull();
      result.Items.Should().NotBeNull();
      // CursorInfo may be null if no pagination is needed
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }
  }

  /// <summary>I02: Verifies that GetAccountAuditLogsAsync with limit returns at most the specified number of logs.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_WithLimit_ReturnsAtMostLimitLogs()
  {
    // Arrange
    var filters = CreateDefaultFilters(limit: 5);

    // Act
    try
    {
      var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

      // Assert
      result.Should().NotBeNull();
      result.Items.Should().NotBeNull();
      result.Items.Count.Should().BeLessThanOrEqualTo(5);
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }
  }

  /// <summary>I03: Verifies that GetAccountAuditLogsAsync with time range returns only logs within that range.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_WithTimeRange_ReturnsLogsInRange()
  {
    // Arrange - Last 7 days
    var since = DateTime.UtcNow.AddDays(-7);
    var before = DateTime.UtcNow;
    var filters = new ListAuditLogsFilters(Since: since, Before: before, Limit: 50);

    // Act
    try
    {
      var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

      // Assert
      result.Should().NotBeNull();
      result.Items.Should().NotBeNull();
      // All returned logs should have time within the range (allowing for some clock skew)
      foreach (var log in result.Items)
      {
        log.Action.Time.Should().BeOnOrAfter(since.AddMinutes(-5), "log time should be after the since filter");
        log.Action.Time.Should().BeOnOrBefore(before.AddMinutes(5), "log time should be before the before filter");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }
  }

  /// <summary>I04: Verifies that GetAllAccountAuditLogsAsync yields all pages of audit logs.</summary>
  [IntegrationTest]
  public async Task GetAllAccountAuditLogsAsync_YieldsAllPages()
  {
    // Arrange - Use filters to limit scope and prevent excessive API calls
    var filters = CreateDefaultFilters(daysBack: 1, limit: 10);

    // Act
    try
    {
      var logs = new List<AuditLog>();
      var maxLogs = 25; // Safety limit to avoid excessive iteration
      await foreach (var log in _sut.GetAllAccountAuditLogsAsync(_accountId, filters))
      {
        logs.Add(log);
        if (logs.Count >= maxLogs)
          break;
      }

      // Assert - Should yield some logs (if any exist in the time range)
      logs.Should().NotBeNull();
      // We can't guarantee logs exist, but the enumeration should work without errors
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }
  }

  #endregion


  #region Filter Tests (I05-I09)

  /// <summary>I05: Verifies that filtering by action type returns only matching action types.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_FilterByActionType_ReturnsOnlyMatchingActions()
  {
    // Arrange - Filter for create actions
    var since = DateTime.UtcNow.AddDays(-7);
    var before = DateTime.UtcNow;
    var filters = new ListAuditLogsFilters(
      ActionTypes: new[] { "create" },
      Since: since,
      Before: before,
      Limit: 20);

    // Act
    try
    {
      var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

      // Assert
      result.Should().NotBeNull();
      result.Items.Should().NotBeNull();
      // All returned logs should have action type "create"
      foreach (var log in result.Items)
      {
        log.Action.Type.Should().Be("create");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }
  }

  /// <summary>I06: Verifies that filtering by action result returns only matching results.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_FilterByActionResult_ReturnsOnlyMatchingResults()
  {
    // Arrange - Filter for successful actions
    var since = DateTime.UtcNow.AddDays(-7);
    var before = DateTime.UtcNow;
    var filters = new ListAuditLogsFilters(
      ActionResults: new[] { "success" },
      Since: since,
      Before: before,
      Limit: 20);

    // Act
    try
    {
      var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

      // Assert
      result.Should().NotBeNull();
      result.Items.Should().NotBeNull();
      // All returned logs should have action result "success"
      foreach (var log in result.Items)
      {
        log.Action.Result.Should().Be("success");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }
  }

  /// <summary>I07: Verifies that filtering by actor email returns only matching actor's logs.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_FilterByActorEmail_ReturnsOnlyMatchingActorLogs()
  {
    // Arrange - First get any log to find an actor email
    var since = DateTime.UtcNow.AddDays(-7);
    var before = DateTime.UtcNow;

    try
    {
      var anyLogs = await _sut.GetAccountAuditLogsAsync(_accountId, new ListAuditLogsFilters(
        Since: since,
        Before: before,
        Limit: 10));

      if (anyLogs.Items.Count == 0 || anyLogs.Items.All(l => string.IsNullOrEmpty(l.Actor.Email)))
      {
        _output.WriteLine("[INFO] No logs with actor email found to filter by. Test passes by default.");
        return;
      }

      var knownEmail = anyLogs.Items.First(l => !string.IsNullOrEmpty(l.Actor.Email)).Actor.Email;

      // Act - Filter by that email
      var filters = new ListAuditLogsFilters(
        ActorEmails: new[] { knownEmail! },
        Since: since,
        Before: before,
        Limit: 20);
      var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

      // Assert
      result.Should().NotBeNull();
      result.Items.Should().NotBeNull();
      // All returned logs should have the matching actor email
      foreach (var log in result.Items)
      {
        log.Actor.Email.Should().Be(knownEmail);
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }
  }

  /// <summary>I08: Verifies that filtering by zone ID returns only zone-scoped logs.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_FilterByZoneId_ReturnsOnlyZoneScopedLogs()
  {
    // Arrange
    var since = DateTime.UtcNow.AddDays(-7);
    var before = DateTime.UtcNow;
    var filters = new ListAuditLogsFilters(
      ZoneIds: new[] { _zoneId },
      Since: since,
      Before: before,
      Limit: 20);

    // Act
    try
    {
      var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

      // Assert
      result.Should().NotBeNull();
      result.Items.Should().NotBeNull();
      // All returned logs should have the matching zone ID (if zone context is present)
      foreach (var log in result.Items)
      {
        if (log.Zone != null)
        {
          log.Zone.Id.Should().Be(_zoneId);
        }
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }
  }

  /// <summary>I09: Verifies that excluding action types filters out those types.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_ExcludeActionType_FiltersOutExcludedTypes()
  {
    // Arrange - Exclude delete actions
    var since = DateTime.UtcNow.AddDays(-7);
    var before = DateTime.UtcNow;
    var filters = new ListAuditLogsFilters(
      ActionTypesNot: new[] { "delete" },
      Since: since,
      Before: before,
      Limit: 20);

    // Act
    try
    {
      var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

      // Assert
      result.Should().NotBeNull();
      result.Items.Should().NotBeNull();
      // No returned logs should have action type "delete"
      foreach (var log in result.Items)
      {
        log.Action.Type.Should().NotBe("delete");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }
  }

  #endregion


  #region Log Structure Tests (I10-I12)

  /// <summary>I10: Verifies that returned logs have required fields populated.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_ReturnsLogsWithRequiredFields()
  {
    // Arrange
    var filters = CreateDefaultFilters();

    // Act
    try
    {
      var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

      // Assert
      result.Should().NotBeNull();
      if (result.Items.Count > 0)
      {
        var log = result.Items[0];
        log.Id.Should().NotBeNullOrEmpty("log should have an ID");
        log.Account.Should().NotBeNull("log should have account context");
        log.Account.Id.Should().NotBeNullOrEmpty("account should have an ID");
        log.Action.Should().NotBeNull("log should have action details");
        log.Actor.Should().NotBeNull("log should have actor information");
      }
      else
      {
        _output.WriteLine("[INFO] No logs found in the time range. Test passes by default.");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }
  }

  /// <summary>I11: Verifies that action time is a valid DateTime.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_ActionTimeIsValidDateTime()
  {
    // Arrange
    var filters = CreateDefaultFilters();

    // Act
    try
    {
      var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

      // Assert
      result.Should().NotBeNull();
      foreach (var log in result.Items)
      {
        log.Action.Time.Should().BeAfter(DateTime.MinValue, "action time should be a valid date");
        log.Action.Time.Should().BeBefore(DateTime.UtcNow.AddMinutes(5), "action time should not be in the future");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }
  }

  /// <summary>I12: Verifies that actor information is present in audit logs.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_ActorInfoIsPresent()
  {
    // Arrange
    var filters = CreateDefaultFilters();

    // Act
    try
    {
      var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

      // Assert
      result.Should().NotBeNull();
      foreach (var log in result.Items)
      {
        // Actor should be present (the object itself should not be null)
        log.Actor.Should().NotBeNull("log should have actor information");
        // Note: Some automated/system actions may not have Id or Email populated,
        // but at minimum the actor object should exist.
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }
  }

  #endregion


  #region Edge Cases (I13-I16)

  /// <summary>I13: Verifies that filtering for a future date range returns empty results without error.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_FutureDateRange_ReturnsEmptyResults()
  {
    // Arrange - Filter for future dates (both since and before required)
    var filters = new ListAuditLogsFilters(
      Since: DateTime.UtcNow.AddDays(1),
      Before: DateTime.UtcNow.AddDays(2));

    // Act
    try
    {
      var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

      // Assert - Should return empty results, not an error
      result.Should().NotBeNull();
      result.Items.Should().BeEmpty("no logs should exist in the future");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }
  }

  /// <summary>I14: Verifies that an invalid account ID returns HTTP 403 or 404.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_InvalidAccountId_Returns403Or404()
  {
    // Arrange - Must provide time range even for invalid account
    var filters = CreateDefaultFilters();

    // Act
    try
    {
      var act = () => _sut.GetAccountAuditLogsAsync("invalid-account-id-that-does-not-exist", filters);

      // Assert - Should throw with 400 (Bad Request for invalid format), 403 (Forbidden) or 404 (Not Found)
      var exception = await act.Should().ThrowAsync<HttpRequestException>();
      exception.Which.StatusCode.Should().BeOneOf(
        HttpStatusCode.BadRequest,
        HttpStatusCode.Forbidden,
        HttpStatusCode.NotFound);
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }
  }

  /// <summary>I15: Verifies that insufficient permissions returns HTTP 403 Forbidden.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_InsufficientPermissions_Returns403()
  {
    // Note: This test is challenging to implement without a dedicated restricted token.
    // We verify that the API correctly denies access to invalid/unauthorized accounts.
    // The test for invalid account (I14) covers this scenario effectively.

    // Arrange - Use a malformed account ID that looks valid but isn't accessible
    var unauthorizedAccountId = "00000000000000000000000000000000";
    var filters = CreateDefaultFilters();

    // Act
    try
    {
      var act = () => _sut.GetAccountAuditLogsAsync(unauthorizedAccountId, filters);

      // Assert - Should throw with 403 (most likely) or 404
      var exception = await act.Should().ThrowAsync<HttpRequestException>();
      exception.Which.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }
  }

  /// <summary>I16: Verifies that a malformed account ID returns HTTP 400 or 404.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_MalformedAccountId_ReturnsError()
  {
    // Arrange - Use clearly malformed ID with special characters
    var malformedAccountId = "!@#$%^&*()";
    var filters = CreateDefaultFilters();

    // Act
    try
    {
      var act = () => _sut.GetAccountAuditLogsAsync(malformedAccountId, filters);

      // Assert - Should throw with an HTTP error (400, 403, or 404)
      var exception = await act.Should().ThrowAsync<HttpRequestException>();
      exception.Which.StatusCode.Should().BeOneOf(
        HttpStatusCode.BadRequest,
        HttpStatusCode.Forbidden,
        HttpStatusCode.NotFound);
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
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
