namespace Cloudflare.NET.Tests.UnitTests.Security;

using System.Net;
using Accounts.Rulesets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NET.Security;
using NET.Security.Rulesets.Models;
using Shared.Fixtures;
using Xunit.Abstractions;

[Trait("Category", TestConstants.TestCategories.Unit)]
public class AccountRulesetsApiUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly ILoggerFactory _loggerFactory;

  #endregion

  #region Constructors

  public AccountRulesetsApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion

  #region Methods

  [Fact]
  public async Task ListPhaseEntrypointVersionsAsync_ShouldConstructCorrectUri()
  {
    // Arrange
    var accountId = "test-account-id";
    var phase     = SecurityConstants.RulesetPhases.HttpRequestFirewallCustom;
    var filters   = new ListRulesetVersionsFilters { Page = 2, PerPage = 10 };

    var                 successResponse = HttpFixtures.CreateSuccessResponse(Array.Empty<Ruleset>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountRulesetsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.ListPhaseEntrypointVersionsAsync(phase, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should()
                    .Be(
                      $"https://api.cloudflare.com/client/v4/accounts/{accountId}/rulesets/phases/{phase}/entrypoint/versions?page=2&per_page=10");
  }

  #endregion
}
