namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Runtime.CompilerServices;
using Moq.Protected;
using Shared.Fixtures;
using Zones;
using Zones.Models;

/// <summary>Contains unit tests for the <see cref="ZonesApi" /> class.</summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class ZonesApiUnitTests
{
  #region Methods

  /// <summary>Verifies that CreateCnameRecordAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task CreateCnameRecordAsync_SendsCorrectRequest()
  {
    // Arrange
    var zoneId      = "test-zone-id";
    var hostname    = "test.example.com";
    var cnameTarget = "target.example.com";

    var expectedResult  = new DnsRecord("dns-record-id", hostname, "CNAME");
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var capturedRequest = new HttpRequestMessage();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(successResponse) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient);

    // Act
    var result = await sut.CreateCnameRecordAsync(zoneId, hostname, cnameTarget);

    // Assert
    await Verify(new { capturedRequest, result }, GetSettings());
  }

  /// <summary>Verifies that DeleteDnsRecordAsync sends a correctly formatted DELETE request.</summary>
  [Fact]
  public async Task DeleteDnsRecordAsync_SendsCorrectRequest()
  {
    // Arrange
    var zoneId   = "test-zone-id";
    var recordId = "dns-record-to-delete-id";

    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var capturedRequest = new HttpRequestMessage();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(successResponse) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient);

    // Act
    await sut.DeleteDnsRecordAsync(zoneId, recordId);

    // Assert
    await Verify(capturedRequest, GetSettings());
  }

  /// <summary>
  ///   Verifies that FindDnsRecordByNameAsync sends a correctly formatted GET request and
  ///   returns the first result.
  /// </summary>
  [Fact]
  public async Task FindDnsRecordByNameAsync_SendsCorrectRequestAndReturnsFirstRecord()
  {
    // Arrange
    var zoneId   = "test-zone-id";
    var hostname = "findme.example.com";

    var record1         = new DnsRecord("id-1", hostname, "A");
    var record2         = new DnsRecord("id-2", hostname, "AAAA");
    var successResponse = HttpFixtures.CreateSuccessResponse(new[] { record1, record2 });

    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var capturedRequest = new HttpRequestMessage();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(successResponse) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient);

    // Act
    var result = await sut.FindDnsRecordByNameAsync(zoneId, hostname);

    // Assert
    await Verify(new { capturedRequest, result }, GetSettings());
  }

  /// <summary>Helper to create verification settings that include the method name.</summary>
  private static VerifySettings GetSettings([CallerMemberName] string methodName = "")
  {
    var settings = new VerifySettings();
    settings.UseMethodName(methodName);
    return settings;
  }

  #endregion
}
