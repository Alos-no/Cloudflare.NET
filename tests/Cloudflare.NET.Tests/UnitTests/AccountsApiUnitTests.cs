namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Runtime.CompilerServices;
using Accounts;
using Accounts.Models;
using Microsoft.Extensions.Options;
using Moq.Protected;
using Shared.Fixtures;

/// <summary>Contains unit tests for the <see cref="AccountsApi" /> class.</summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class AccountsApiUnitTests
{
  #region Methods

  /// <summary>Verifies that CreateR2BucketAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task CreateR2BucketAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket";

    var expectedResult  = new CreateBucketResponse(bucketName, DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(), "wnam", "eu", "Standard");
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var capturedRequest = new HttpRequestMessage();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(successResponse) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options);

    // Act
    var result = await sut.CreateR2BucketAsync(bucketName);

    // Assert
    // The Verify.Http extension will automatically capture the request content.
    await Verify(new { capturedRequest, result }, GetSettings());
  }

  /// <summary>Verifies that DeleteR2BucketAsync sends a correctly formatted DELETE request.</summary>
  [Fact]
  public async Task DeleteR2BucketAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket-to-delete";

    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var capturedRequest = new HttpRequestMessage();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(successResponse) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options);

    // Act
    // The task is awaited to ensure the operation completes before verification.
    await sut.DeleteR2BucketAsync(bucketName);

    // Assert
    await Verify(capturedRequest, GetSettings());
  }

  /// <summary>Verifies that AttachCustomDomainAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task AttachCustomDomainAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket";
    var hostname   = "r2.example.com";
    var zoneId     = "test-zone-id";

    var expectedResult  = new CustomDomainResponse(hostname, $"{hostname}.cdn.cloudflare.net", "pending_validation");
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var capturedRequest = new HttpRequestMessage();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(successResponse) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options);

    // Act
    var result = await sut.AttachCustomDomainAsync(bucketName, hostname, zoneId);

    // Assert
    await Verify(new { capturedRequest, result }, GetSettings());
  }

  /// <summary>Verifies that DisableDevUrlAsync sends a correctly formatted PUT request.</summary>
  [Fact]
  public async Task DisableDevUrlAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket";

    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var capturedRequest = new HttpRequestMessage();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(successResponse) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options);

    // Act
    await sut.DisableDevUrlAsync(bucketName);

    // Assert
    await Verify(capturedRequest, GetSettings());
  }

  /// <summary>Verifies that GetCustomDomainStatusAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetCustomDomainStatusAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket";
    var hostname   = "r2.example.com";

    // The real API response for an existing domain is more complex, so we mimic that structure for an accurate test.
    // This specifically tests the CustomDomainResponseConverter's ability to parse the nested status object.
    var responseBody =
      $@"{{
          ""success"": true,
          ""errors"": [],
          ""messages"": [],
          ""result"": {{
            ""domain"": ""{hostname}"",
            ""status"": {{
              ""ownership"": ""active"",
              ""ssl"": ""active""
            }},
            ""edgeHostname"": ""{hostname}.cdn.cloudflare.net""
          }}
        }}";

    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseBody, HttpStatusCode.OK);

    var capturedRequest = new HttpRequestMessage();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responseBody) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options);

    // Act
    var result = await sut.GetCustomDomainStatusAsync(bucketName, hostname);

    // Assert
    await Verify(new { capturedRequest, result }, GetSettings());
  }

  /// <summary>Verifies that DetachCustomDomainAsync sends a correctly formatted DELETE request.</summary>
  [Fact]
  public async Task DetachCustomDomainAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket";
    var hostname   = "r2.example.com";

    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var capturedRequest = new HttpRequestMessage();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(successResponse) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options);

    // Act
    await sut.DetachCustomDomainAsync(bucketName, hostname);

    // Assert
    await Verify(capturedRequest, GetSettings());
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
