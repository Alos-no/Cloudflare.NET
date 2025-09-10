namespace Cloudflare.NET.Tests.UnitTests.Security;

using System.Net;
using Accounts.Rulesets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.Protected;
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

  /// <summary>
  ///   Verifies that ListAllAsync handles cursor-based pagination correctly. This pagination
  ///   style is common in newer Cloudflare APIs and is more resilient to data changes during
  ///   iteration than page/offset methods. [13, 14, 15]
  /// </summary>
  [Fact]
  public async Task ListAllAsync_ShouldHandlePaginationCorrectly()
  {
    // Arrange
    var accountId = "test-account-id";
    var rule1     = new Ruleset("id-1", "name1", "kind", "1", DateTime.UtcNow);
    var rule2     = new Ruleset("id-2", "name2", "kind", "1", DateTime.UtcNow);
    var cursor    = "next_page_cursor_token";
    var options   = Options.Create(new CloudflareApiOptions { AccountId = accountId });

    // First page response with a cursor
    var responsePage1 = HttpFixtures.CreateSuccessResponse(new[] { rule1 })
                                    .Replace("\"result\":",
                                             $"\"cursor_result_info\": {{ \"cursor\": \"{cursor}\" }}, \"result\":");

    // Second page response without a cursor
    var responsePage2 = HttpFixtures.CreateSuccessResponse(new[] { rule2 })
                                    .Replace("\"result\":",
                                             "\"cursor_result_info\": { \"cursor\": null }, \"result\":");

    var capturedRequests = new List<HttpRequestMessage>();
    var mockHandler      = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequests.Add(req))
               .Returns((HttpRequestMessage req, CancellationToken _) =>
               {
                 if (req.RequestUri!.ToString().Contains(cursor))
                   return Task.FromResult(new HttpResponseMessage
                                            { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage2) });

                 return Task.FromResult(new HttpResponseMessage
                                          { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage1) });
               });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new AccountRulesetsApi(httpClient, options, _loggerFactory);

    // Act
    var allRules = new List<Ruleset>();
    await foreach (var rule in sut.ListAllAsync())
      allRules.Add(rule);

    // Assert
    capturedRequests.Should().HaveCount(2);
    capturedRequests[0].RequestUri!.Query.Should().NotContain("cursor");
    capturedRequests[1].RequestUri!.Query.Should().Contain($"cursor={cursor}");
    allRules.Should().HaveCount(2);
    allRules.Select(r => r.Id).Should().ContainInOrder("id-1", "id-2");
  }

  #endregion
}
