namespace Cloudflare.NET.Analytics.Tests.IntegrationTests;

using Fixtures;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Models;
using Xunit.Abstractions;

/// <summary>Contains integration tests for the <see cref="AnalyticsApi" /> class.</summary>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class AnalyticsApiIntegrationTests : IClassFixture<AnalyticsApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IAnalyticsApi _sut;

  /// <summary>The account ID from configuration.</summary>
  private readonly string _accountId;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="AnalyticsApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides a configured analytics client.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public AnalyticsApiIntegrationTests(AnalyticsApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut       = fixture.AnalyticsApi;
    _accountId = TestConfiguration.CloudflareSettings.AccountId;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion

  #region Methods

  /// <summary>
  ///   Verifies that a valid GraphQL query can be sent and a well-formed response is received without errors. This
  ///   test confirms authentication and end-to-end connectivity.
  /// </summary>
  [IntegrationTest]
  public async Task CanSendValidQueryAndReceiveSuccessResponse()
  {
    // Arrange
    // A simple, valid query to get account-level information.
    // The r2StorageAdaptiveGroups node requires a filter, so we provide a broad one.
    var request = new GraphQLRequest
    {
      Query = @"
        query GetAccounts($accountTag: String!, $startTime: Time!, $endTime: Time!) {
          viewer {
            accounts(filter: { accountTag: $accountTag }) {
              r2StorageAdaptiveGroups(
                limit: 1,
                filter: { datetime_geq: $startTime, datetime_leq: $endTime }
              ) {
                max { objectCount }
              }
            }
          }
        }",
      Variables = new
      {
        accountTag = _accountId,
        // Use a recent, short time window for this test.
        startTime = DateTime.UtcNow.AddDays(-1),
        endTime   = DateTime.UtcNow
      }
    };

    // Act
    // Define the action of sending the query.
    var action = async () => await _sut.SendQueryAsync<GraphQLResponse>(request);

    // Assert
    // The query should execute without throwing an exception, which proves that
    // authentication, serialization, and the GraphQL endpoint are all working correctly.
    var result = await action.Should().NotThrowAsync();
    result.Subject.Should().NotBeNull();
    result.Subject.Viewer.Should().NotBeNull();
    result.Subject.Viewer.Accounts.Should().NotBeNull();
  }

  #endregion
}
