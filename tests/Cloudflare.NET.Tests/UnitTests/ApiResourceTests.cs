namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using Accounts;
using Microsoft.Extensions.Options;
using Shared.Fixtures;

/// <summary>
///   Contains unit tests for the abstract <see cref="ApiResource" /> class, tested via a
///   concrete implementation (<see cref="AccountsApi" />).
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class ApiResourceTests
{
  #region Methods

  /// <summary>
  ///   Verifies that the response processing throws an InvalidOperationException when the
  ///   API returns success: false.
  /// </summary>
  [Fact]
  public async Task ProcessResponse_WhenSuccessIsFalse_ThrowsInvalidOperationException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(10000, "Authentication error");
    var mockHandler   = HttpFixtures.GetMockHttpMessageHandler(errorResponse, HttpStatusCode.OK);
    var httpClient    = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options       = Options.Create(new CloudflareApiOptions { AccountId = "test-id" });
    var sut           = new AccountsApi(httpClient, options);

    // Act
    var action = async () => await sut.DeleteR2BucketAsync("any-bucket");

    // Assert
    // Verify that the exception message correctly reports the failure and includes the API error.
    // The wildcard '*' is used because the full message includes the raw response body.
    await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Cloudflare API returned a failure response: [10000] Authentication error*");
  }

  /// <summary>Verifies that a non-success HTTP status code results in an HttpRequestException.</summary>
  [Fact]
  public async Task ProcessResponse_OnNonSuccessStatusCode_ThrowsHttpRequestException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("Internal Server Error", HttpStatusCode.InternalServerError);
    var httpClient  = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options     = Options.Create(new CloudflareApiOptions { AccountId = "test-id" });
    var sut         = new AccountsApi(httpClient, options);

    // Act
    var action = async () => await sut.DeleteR2BucketAsync("any-bucket");

    // Assert
    // Verify that the correct exception is thrown and that it contains the status code.
    (await action.Should().ThrowAsync<HttpRequestException>())
      .Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
  }

  /// <summary>
  ///   Verifies that an invalid JSON response results in a JsonException during
  ///   deserialization.
  /// </summary>
  [Fact]
  public async Task ProcessResponse_OnInvalidJson_ThrowsJsonException()
  {
    // Arrange
    var invalidJsonResponse = "{ not json }";
    var mockHandler         = HttpFixtures.GetMockHttpMessageHandler(invalidJsonResponse, HttpStatusCode.OK);
    var httpClient          = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options             = Options.Create(new CloudflareApiOptions { AccountId = "test-id" });
    var sut                 = new AccountsApi(httpClient, options);

    // Act
    var action = async () => await sut.DeleteR2BucketAsync("any-bucket");

    // Assert
    // Verify that a JsonException is thrown and that the message indicates a deserialization failure.
    (await action.Should().ThrowAsync<JsonException>())
      .WithMessage("Failed to deserialize Cloudflare API response.*");
  }

  #endregion
}
