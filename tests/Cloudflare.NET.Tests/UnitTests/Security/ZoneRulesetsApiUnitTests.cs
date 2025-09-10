namespace Cloudflare.NET.Tests.UnitTests.Security;

using System.Net;
using Microsoft.Extensions.Logging;
using NET.Security.Rulesets.Models;
using Shared.Fixtures;
using Xunit.Abstractions;
using Zones.Rulesets;

[Trait("Category", TestConstants.TestCategories.Unit)]
public class ZoneRulesetsApiUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly ILoggerFactory _loggerFactory;

  #endregion

  #region Constructors

  public ZoneRulesetsApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion

  #region Methods

  [Fact]
  public async Task GetPhaseEntrypointVersionAsync_ShouldConstructCorrectUri()
  {
    // Arrange
    var zoneId  = "test-zone-id";
    var phase   = "http_ratelimit";
    var version = "5";

    var                 successResponse = HttpFixtures.CreateSuccessResponse(default(Ruleset));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZoneRulesetsApi(httpClient, _loggerFactory);

    // Act
    await sut.GetPhaseEntrypointVersionAsync(zoneId, phase, version);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should()
                    .Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/rulesets/phases/{phase}/entrypoint/versions/{version}");
  }

  #endregion
}
