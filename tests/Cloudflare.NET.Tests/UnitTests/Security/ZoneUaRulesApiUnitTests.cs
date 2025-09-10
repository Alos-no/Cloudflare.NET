namespace Cloudflare.NET.Tests.UnitTests.Security;

using System.Net;
using Microsoft.Extensions.Logging;
using NET.Security.Firewall.Models;
using Shared.Fixtures;
using Xunit.Abstractions;
using Zones.Firewall;

[Trait("Category", TestConstants.TestCategories.Unit)]
public class ZoneUaRulesApiUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly ILoggerFactory _loggerFactory;

  #endregion

  #region Constructors

  public ZoneUaRulesApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion

  #region Methods

  [Fact]
  public async Task CreateAsync_SendsCorrectRequest()
  {
    // Arrange
    var zoneId = "test-zone-id";
    var request = new CreateUaRuleRequest(
      UaRuleMode.Block,
      new UaRuleConfiguration("ua", "BadBot/1.0"),
      Description: "Block BadBot"
    );

    var expectedResponse = new UaRule(
      "ua-rule-id-123",
      request.Mode,
      request.Configuration,
      false,
      request.Description
    );

    var                 successResponse = HttpFixtures.CreateSuccessResponse(expectedResponse);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZoneUaRulesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.CreateAsync(zoneId, request);

    // Assert
    result.Should().BeEquivalentTo(expectedResponse);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/firewall/ua_rules");
    var content = await capturedRequest.Content!.ReadFromJsonAsync<CreateUaRuleRequest>();
    content.Should().BeEquivalentTo(request);
  }

  #endregion
}
