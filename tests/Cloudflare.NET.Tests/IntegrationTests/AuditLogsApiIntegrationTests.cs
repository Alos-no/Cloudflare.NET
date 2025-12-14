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
    var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

    // Assert
    result.Should().NotBeNull("API should return a valid paginated result");
    result.Items.Should().NotBeNull("Items collection should not be null");

    // Audit logs endpoint returns cursor pagination info for navigating through results
    result.CursorInfo.Should().NotBeNull("audit logs endpoint returns cursor pagination info");
    result.CursorInfo!.Count.Should().BeGreaterThanOrEqualTo(0, "Count should be non-negative");
  }

  /// <summary>I02: Verifies that GetAccountAuditLogsAsync with limit returns at most the specified number of logs.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_WithLimit_ReturnsAtMostLimitLogs()
  {
    // Arrange
    var filters = CreateDefaultFilters(limit: 5);

    // Act
    var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeNull();
    result.Items.Count.Should().BeLessThanOrEqualTo(5, "API should respect the limit parameter");
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
    var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeNull();
    result.Items.Should().NotBeEmpty("account should have audit logs within the last 7 days");

    // All returned logs should have time within the range (allowing for some clock skew)
    foreach (var log in result.Items)
    {
      log.Timestamp.Should().BeOnOrAfter(since.AddMinutes(-5), "log time should be after the since filter");
      log.Timestamp.Should().BeOnOrBefore(before.AddMinutes(5), "log time should be before the before filter");
    }
  }

  /// <summary>I04: Verifies that GetAllAccountAuditLogsAsync yields all pages of audit logs.</summary>
  [IntegrationTest]
  public async Task GetAllAccountAuditLogsAsync_YieldsAllPages()
  {
    // Arrange - Use filters to limit scope and prevent excessive API calls
    var filters = CreateDefaultFilters(daysBack: 1, limit: 10);

    // Act
    var logs = new List<AuditLog>();
    var maxLogs = 25; // Safety limit to avoid excessive iteration

    await foreach (var log in _sut.GetAllAccountAuditLogsAsync(_accountId, filters))
    {
      logs.Add(log);

      if (logs.Count >= maxLogs)
        break;
    }

    // Assert - Enumeration should complete without error
    logs.Should().NotBeNull("enumeration should produce a valid list");
    logs.Should().NotBeEmpty("account should have audit logs from recent API activity");
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
    var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeNull();

    // All returned logs should have action type "create"
    foreach (var log in result.Items)
    {
      log.Action.Type.Should().Be("create", "filter should return only 'create' action types");
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
    var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeNull();

    // All returned logs should have action result true (success)
    foreach (var log in result.Items)
    {
      log.Action.Result.Should().BeTrue("filter should return only successful results");
    }
  }

  /// <summary>I07: Verifies that filtering by actor email returns only matching actor's logs.</summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires specific audit log data that cannot be created via API.</b></para>
  ///   <para>
  ///     Audit logs are generated by account activity and cannot be created programmatically.
  ///     This test requires logs with actor email addresses, which may not exist in all accounts.
  ///   </para>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Audit logs are read-only: https://developers.cloudflare.com/fundamentals/setup/account/account-security/review-audit-logs/</item>
  ///     <item>Actor email depends on authentication method used for the action</item>
  ///   </list>
  /// </remarks>
  [IntegrationTest(Skip = "Requires specific audit log data (logs with actor emails). TODO: Add email to user secrets")]
  public async Task GetAccountAuditLogsAsync_FilterByActorEmail_ReturnsOnlyMatchingActorLogs()
  {
    // Arrange - Use a known email that would exist in the account's audit logs
    var since = DateTime.UtcNow.AddDays(-7);
    var before = DateTime.UtcNow;
    var knownEmail = "user@example.com"; // Would be replaced with actual account owner email

    // Act - Filter by that email
    var filters = new ListAuditLogsFilters(
      ActorEmails: new[] { knownEmail },
      Since: since,
      Before: before,
      Limit: 20);
    var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeNull();
    result.Items.Should().NotBeEmpty("filtering by a known email should return results");

    // All returned logs should have the matching actor email
    foreach (var log in result.Items)
    {
      log.Actor.Email.Should().Be(knownEmail, "filter should return only logs from the specified actor");
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
    var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeNull();
    result.Items.Should().NotBeEmpty("zone filter should return logs when zone has recent activity");

    // All returned logs should have the matching zone ID
    foreach (var log in result.Items)
    {
      log.Zone.Should().NotBeNull("zone-filtered logs should include zone context");
      log.Zone!.Id.Should().Be(_zoneId, "zone filter should return only logs for the specified zone");
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
    var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeNull();

    // No returned logs should have action type "delete"
    foreach (var log in result.Items)
    {
      log.Action.Type.Should().NotBe("delete", "excluded action types should not appear in results");
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
    var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

    // Assert
    result.Should().NotBeNull("API should return a valid response");
    result.Items.Should().NotBeNull("Items collection should never be null");
    result.Items.Should().NotBeEmpty("account should have audit logs from recent API activity");

    foreach (var log in result.Items)
    {
      log.Id.Should().NotBeNullOrEmpty("log should have an ID");
      log.Account.Should().NotBeNull("log should have account context");
      log.Account.Id.Should().NotBeNullOrEmpty("account should have an ID");
      log.Action.Should().NotBeNull("log should have action details");
      log.Actor.Should().NotBeNull("log should have actor information");
    }
  }

  /// <summary>I11: Verifies that the when timestamp is a valid DateTime.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_WhenTimestampIsValidDateTime()
  {
    // Arrange
    var filters = CreateDefaultFilters();

    // Act
    var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeEmpty("account should have audit logs from recent API activity");

    foreach (var log in result.Items)
    {
      log.Timestamp.Should().BeAfter(DateTime.MinValue, "timestamp should be a valid date");
      log.Timestamp.Should().BeBefore(DateTime.UtcNow.AddMinutes(5), "timestamp should not be in the future");
    }
  }

  /// <summary>I12: Verifies that actor information is present in audit logs.</summary>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_ActorInfoIsPresent()
  {
    // Arrange
    var filters = CreateDefaultFilters();

    // Act
    var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeEmpty("account should have audit logs from recent API activity");

    foreach (var log in result.Items)
    {
      // Actor object is always present. Id and Email vary by action type (user vs system actions).
      log.Actor.Should().NotBeNull("log should have actor information");
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
    var result = await _sut.GetAccountAuditLogsAsync(_accountId, filters);

    // Assert - Should return empty results, not an error
    result.Should().NotBeNull();
    result.Items.Should().BeEmpty("no logs should exist in the future");
  }

  /// <summary>I14: Verifies that an invalid account ID format returns HTTP 404.</summary>
  /// <remarks>
  ///   Invalid account ID formats (not 32-character hex strings) return 404 NotFound
  ///   with error code 7003 "Could not route to..." because Cloudflare's routing layer
  ///   cannot match the path to a valid account endpoint.
  /// </remarks>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_InvalidAccountId_Returns404()
  {
    // Arrange - Must provide time range even for invalid account
    var filters = CreateDefaultFilters();

    // Act
    var act = () => _sut.GetAccountAuditLogsAsync("invalid-account-id-that-does-not-exist", filters);

    // Assert - Invalid format returns 404 (routing error)
    await act.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
  }

  /// <summary>I15: Verifies that insufficient permissions returns HTTP 403 Forbidden.</summary>
  /// <remarks>
  ///   <para>
  ///     Cloudflare returns 403 Forbidden with error code 10000 "Authentication error" for
  ///     non-existent account IDs that are in valid format (32-character hex strings).
  ///     This is intentional security behavior to prevent account enumeration attacks -
  ///     by returning the same 403 for both non-existent and unauthorized accounts,
  ///     attackers cannot discover which account IDs are valid.
  ///   </para>
  ///   <para>
  ///     See: https://authress.io/knowledge-base/articles/choosing-the-right-http-error-code-401-403-404
  ///   </para>
  /// </remarks>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_InsufficientPermissions_Returns403()
  {
    // Arrange - Use a valid format account ID that doesn't exist
    var unauthorizedAccountId = "00000000000000000000000000000000";
    var filters = CreateDefaultFilters();

    // Act
    var act = () => _sut.GetAccountAuditLogsAsync(unauthorizedAccountId, filters);

    // Assert - Cloudflare returns 403 to prevent account enumeration (security best practice)
    await act.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.Forbidden);
  }

  /// <summary>I16: Verifies that a malformed account ID with special characters returns HTTP 400.</summary>
  /// <remarks>
  ///   Malformed account IDs containing special characters that are not valid in URL paths
  ///   return 400 BadRequest with error code 7003 "Could not route to..." because the
  ///   request cannot be parsed correctly at the routing layer.
  /// </remarks>
  [IntegrationTest]
  public async Task GetAccountAuditLogsAsync_MalformedAccountId_ReturnsError()
  {
    // Arrange - Use clearly malformed ID with special characters
    var malformedAccountId = "!@#$%^&*()";
    var filters = CreateDefaultFilters();

    // Act
    var act = () => _sut.GetAccountAuditLogsAsync(malformedAccountId, filters);

    // Assert - Special characters return 400 BadRequest (routing/parsing error)
    await act.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.BadRequest);
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
