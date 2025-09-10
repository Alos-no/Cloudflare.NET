namespace Cloudflare.NET.Tests.UnitTests.Security;

using System.Net;
using Microsoft.Extensions.Logging;
using NET.Security.Firewall.Models;
using Shared.Fixtures;
using Xunit.Abstractions;
using Zones.Firewall;

[Trait("Category", TestConstants.TestCategories.Unit)]
public class ZoneLockdownApiUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly ILoggerFactory _loggerFactory;

  #endregion

  #region Constructors

  public ZoneLockdownApiUnitTests(ITestOutputHelper output)
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
    var request = new CreateLockdownRequest(
      ["example.com/admin*"],
      [new(LockdownTarget.Ip, "192.0.2.1")],
      Description: "Admin Lockdown"
    );

    var expectedResponse = new Lockdown(
      "lockdown-id-123",
      request.Urls,
      request.Configurations,
      false,
      request.Description,
      DateTimeOffset.UtcNow,
      DateTimeOffset.UtcNow
    );

    var                 successResponse = HttpFixtures.CreateSuccessResponse(expectedResponse);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZoneLockdownApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.CreateAsync(zoneId, request);

    // Assert
    result.Should().BeEquivalentTo(expectedResponse, options => options.ComparingByMembers<DateTimeOffset>());
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/firewall/lockdowns");
    var content = await capturedRequest.Content!.ReadFromJsonAsync<CreateLockdownRequest>();
    content.Should().BeEquivalentTo(request);
  }

  #endregion
}
