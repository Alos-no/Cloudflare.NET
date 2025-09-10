namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Timeout;
using Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>
///   Contains unit tests for the custom resilience pipeline configured in
///   <see cref="ServiceCollectionExtensions" />.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class ResiliencePipelineTests
{
  #region Properties & Fields - Non-Public

  private readonly ITestOutputHelper _output;

  #endregion

  #region Constructors

  public ResiliencePipelineTests(ITestOutputHelper output)
  {
    _output = output;
  }

  #endregion

  #region Methods

  /// <summary>
  ///   Verifies that the innermost "Attempt Timeout" strategy correctly cancels a single API
  ///   call that exceeds the configured timeout.
  /// </summary>
  [Fact]
  public async Task AttemptTimeout_WhenApiCallExceedsTimeout_ThrowsTimeoutRejectedException()
  {
    // Arrange
    var (client, _) = SetupClient(
      options =>
      {
        // Set a very short timeout for this test.
        options.DefaultTimeout = TimeSpan.FromMilliseconds(100);
        // Disable retries to isolate the timeout behavior.
        options.RateLimiting.MaxRetries = 0;
      },
      handler =>
      {
        // This handler will delay longer than the timeout, forcing it to trigger.
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Returns(async (HttpRequestMessage _, CancellationToken ct) =>
               {
                 await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
                 // We should never get here because the timeout will cancel the task.
                 return new HttpResponseMessage(HttpStatusCode.OK);
               });
      });

    // Act
    var action = async () => await client.Accounts.DeleteR2BucketAsync("any-bucket");

    // Assert
    // The Polly pipeline should throw a TimeoutRejectedException when the attempt timeout is exceeded.
    await action.Should().ThrowAsync<TimeoutRejectedException>();
  }

  /// <summary>
  ///   Verifies that the retry strategy correctly re-issues a request upon receiving a 5xx
  ///   server error.
  /// </summary>
  [Fact]
  public async Task Retry_WhenApiReturnsTransient5xxError_ShouldRetryRequest()
  {
    // Arrange
    var (client, handler) = SetupClient(
      options =>
      {
        // Configure exactly one retry.
        options.RateLimiting.MaxRetries = 1;
      },
      handler =>
      {
        // The handler will fail once with a 503, then succeed with a 200.
        handler.Protected()
               .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
               {
                 Content = new StringContent(HttpFixtures.CreateSuccessResponse<object?>(null))
               });
      });

    // Act
    // This call should succeed because the retry will hit the successful response.
    var action = async () => await client.Accounts.DeleteR2BucketAsync("any-bucket");

    // Assert
    await action.Should().NotThrowAsync();

    // Verify that the request was sent twice (1 initial + 1 retry).
    handler.Protected().Verify(
      "SendAsync",
      Times.Exactly(2),
      ItExpr.IsAny<HttpRequestMessage>(),
      ItExpr.IsAny<CancellationToken>());
  }

  /// <summary>
  ///   Verifies that the retry strategy correctly re-issues a request for a 429 status code
  ///   when rate limit handling is enabled.
  /// </summary>
  [Fact]
  public async Task Retry_WhenRateLimitingIsEnabled_ShouldRetryOn429()
  {
    // Arrange
    var (client, handler) = SetupClient(
      options =>
      {
        options.RateLimiting.IsEnabled  = true;
        options.RateLimiting.MaxRetries = 1;
      },
      handler =>
      {
        var response429 = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        // The handler will fail once with a 429, then succeed with a 200.
        handler.Protected()
               .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(response429)
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
               {
                 Content = new StringContent(HttpFixtures.CreateSuccessResponse<object?>(null))
               });
      });

    // Act
    var action = async () => await client.Accounts.DeleteR2BucketAsync("any-bucket");

    // Assert
    await action.Should().NotThrowAsync();
    handler.Protected().Verify(
      "SendAsync",
      Times.Exactly(2),
      ItExpr.IsAny<HttpRequestMessage>(),
      ItExpr.IsAny<CancellationToken>());
  }

  /// <summary>
  ///   Verifies that the retry strategy does NOT re-issue a request for a 429 status code
  ///   when rate limit handling is disabled.
  /// </summary>
  [Fact]
  public async Task Retry_WhenRateLimitingIsDisabled_ShouldNotRetryOn429()
  {
    // Arrange
    var (client, handler) = SetupClient(
      options =>
      {
        // Ensure rate limit retries are explicitly disabled.
        options.RateLimiting.IsEnabled  = false;
        options.RateLimiting.MaxRetries = 1;
      },
      handler =>
      {
        // This handler will always return 429.
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
               {
                 // We provide a valid error body so the ApiResource throws HttpRequestException instead of JsonException.
                 Content = new StringContent(HttpFixtures.CreateErrorResponse(9999, "Rate Limited"))
               });
      });

    // Act
    var action = async () => await client.Zones.GetZoneDetailsAsync("any-zone");

    // Assert
    // The call should fail because retries for 429 are disabled.
    await action.Should().ThrowAsync<HttpRequestException>()
                .Where(ex => ex.StatusCode == HttpStatusCode.TooManyRequests);

    // Crucially, verify that the underlying handler was only called once.
    handler.Protected().Verify(
      "SendAsync",
      Times.Once(),
      ItExpr.IsAny<HttpRequestMessage>(),
      ItExpr.IsAny<CancellationToken>());
  }


  /// <summary>
  ///   Verifies that non-idempotent methods (like POST) are not retried, even on transient
  ///   failures, to prevent creating duplicate resources.
  /// </summary>
  [Fact]
  public async Task Retry_ForUnsafePostMethod_ShouldNotRetryOnTransientError()
  {
    // Arrange
    var (client, handler) = SetupClient(
      options => { options.RateLimiting.MaxRetries = 2; },
      handler =>
      {
        // This handler will always return a transient server error.
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Returns((HttpRequestMessage req, CancellationToken ct) =>
               {
                 // Ensure the retry predicate can observe the HTTP method:
                 // Real HttpClient handlers populate Response.RequestMessage automatically,
                 // but custom/mocked handlers may not. If left null, our ShouldHandle() method
                 // cannot gate by idempotency and POST would (incorrectly) be retried.
                 return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                 {
                   RequestMessage = req
                 });
               });
      });

    // Act
    // CreateR2BucketAsync uses POST, which is an unsafe method.
    var action = async () => await client.Accounts.CreateR2BucketAsync("any-bucket");

    // Assert
    await action.Should().ThrowAsync<HttpRequestException>();

    // Verify that the request was sent only once, with no retries.
    handler.Protected().Verify(
      "SendAsync",
      Times.Once(),
      ItExpr.IsAny<HttpRequestMessage>(),
      ItExpr.IsAny<CancellationToken>());
  }

  /// <summary>
  ///   Verifies that the circuit breaker opens after a configured number of consecutive
  ///   failures and that subsequent calls fail-fast with a BrokenCircuitException.
  /// </summary>
  [Fact]
  public async Task CircuitBreaker_AfterConsecutiveFailures_ShouldOpenAndBlockCalls()
  {
    // Arrange
    // The default circuit breaker has a MinimumThroughput of 5. After 5 consecutive failures,
    // the circuit should open.
    var minimumThroughput = new HttpCircuitBreakerStrategyOptions().MinimumThroughput;
    var (client, handler) = SetupClient(
      options =>
      {
        options.RateLimiting.MaxRetries = 0; // isolate the breaker
      },
      handler =>
      {
        // This handler will always fail.
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));
      });

    // Act & Assert

    // 1. Trigger consecutive failures to open the circuit.
    for (var i = 0; i < minimumThroughput; i++)
    {
      var action = async () => await client.Accounts.DeleteR2BucketAsync("any-bucket");
      await action.Should().ThrowAsync<HttpRequestException>();
    }

    // 2. The next call should fail-fast with a BrokenCircuitException.
    var finalAction = async () => await client.Accounts.DeleteR2BucketAsync("any-bucket");
    await finalAction.Should().ThrowAsync<BrokenCircuitException>();

    // 3. Verify the handler was only called 'minimumThroughput' times. The last call was blocked by the circuit.
    handler.Protected().Verify(
      "SendAsync",
      Times.Exactly(minimumThroughput),
      ItExpr.IsAny<HttpRequestMessage>(),
      ItExpr.IsAny<CancellationToken>());
  }

  /// <summary>
  ///   Verifies that the client-side rate limiter (bulkhead) rejects concurrent requests
  ///   that exceed its configured permit limit.
  /// </summary>
  [Fact]
  public async Task RateLimiter_WhenConcurrencyExceeded_ShouldRejectRequest()
  {
    // Arrange
    var (client, handler) = SetupClient(
      options =>
      {
        // Configure a very restrictive rate limiter: 1 concurrent request, no queue.
        options.RateLimiting.PermitLimit = 1;
        options.RateLimiting.QueueLimit  = 0;
      },
      handler =>
      {
        // This handler introduces a delay to ensure the first request is still "in-flight"
        // when the second one arrives, forcing a concurrency conflict.
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Returns(async (HttpRequestMessage _, CancellationToken ct) =>
               {
                 await Task.Delay(TimeSpan.FromMilliseconds(200), ct);
                 return new HttpResponseMessage(HttpStatusCode.OK)
                 {
                   Content = new StringContent(HttpFixtures.CreateSuccessResponse<object?>(null))
                 };
               });
      });

    // Act
    // Start two requests concurrently.
    var task1 = client.Accounts.DeleteR2BucketAsync("bucket-1");
    var task2 = client.Accounts.DeleteR2BucketAsync("bucket-2");

    // Act
    var action = () => Task.WhenAll(task1, task2);

    // Assert
    // One of the tasks should fail with RateLimiterRejectedException.
    await action.Should().ThrowAsync<RateLimiterRejectedException>();

    // Verify that the underlying handler was only called once. The second request was
    // rejected by the client-side limiter before it was sent.
    handler.Protected().Verify(
      "SendAsync",
      Times.Once(),
      ItExpr.IsAny<HttpRequestMessage>(),
      ItExpr.IsAny<CancellationToken>());
  }

  /// <summary>
  ///   A helper method to set up the DI container with a mock HttpMessageHandler and custom
  ///   options for a single test.
  /// </summary>
  private (ICloudflareApiClient Client, Mock<HttpMessageHandler> Handler) SetupClient(
    Action<CloudflareApiOptions>?    configureOptions,
    Action<Mock<HttpMessageHandler>> configureHandler)
  {
    var services = new ServiceCollection();

    // Setup logging to pipe to the xUnit test output.
    services.AddLogging(builder => builder.AddProvider(new XunitTestOutputLoggerProvider { Current = _output }));

    var mockHandler = new Mock<HttpMessageHandler>();
    configureHandler(mockHandler);

    // Use the real DI extension method from the SDK.
    services.AddCloudflareApiClient(options =>
    {
      // Apply test-specific options.
      configureOptions?.Invoke(options);
    });

    // Inject the mock as the *primary* handler for all factory-built clients
    services.ConfigureAll<HttpClientFactoryOptions>(opts =>
    {
      opts.HttpMessageHandlerBuilderActions.Add(builder =>
      {
        builder.PrimaryHandler = mockHandler.Object; // preserve the surrounding resilience + auth handlers
      });
    });

    var serviceProvider = services.BuildServiceProvider();

    // We must resolve the client from the DI container to ensure it's using the configured pipeline.
    var client = serviceProvider.GetRequiredService<ICloudflareApiClient>();

    return (client, mockHandler);
  }

  #endregion
}
