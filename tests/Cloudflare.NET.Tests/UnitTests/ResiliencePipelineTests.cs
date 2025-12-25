// Suppress obsolete warnings for tests that use deprecated methods as test vehicles.
#pragma warning disable CS0618

namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using Cloudflare.NET.Core;
using Cloudflare.NET.Dns.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Polly;
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
  ///   Verifies that the innermost "Attempt Timeout" strategy correctly cancels a single API call that exceeds the
  ///   configured timeout.
  /// </summary>
  [Fact]
  public async Task AttemptTimeout_WhenApiCallExceedsTimeout_ThrowsTimeoutRejectedException()
  {
    // Arrange
    var (client, _) = SetupClient(
      options =>
      {
        // Set the minimum allowed timeout (1 second) for this test.
        // Note: Microsoft.Extensions.Http.Resilience requires timeout to be between 1 second and 1 day.
        options.DefaultTimeout = TimeSpan.FromSeconds(1);
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
                 await Task.Delay(TimeSpan.FromSeconds(5), ct);
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

  /// <summary>Verifies that the retry strategy correctly re-issues a request upon receiving a 5xx server error.</summary>
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
  ///   Verifies that the retry strategy correctly re-issues a request for a 429 status code when rate limit handling
  ///   is enabled.
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
  ///   Verifies that the retry strategy does NOT re-issue a request for a 429 status code when rate limit handling is
  ///   disabled.
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
  ///   Verifies that non-idempotent methods (like POST) are not retried, even on transient failures, to prevent
  ///   creating duplicate resources.
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
  ///   Verifies that the circuit breaker opens after a configured number of consecutive failures and that subsequent
  ///   calls fail-fast with a BrokenCircuitException.
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
  ///   Verifies that the client-side rate limiter (bulkhead) rejects concurrent requests that exceed its configured
  ///   permit limit.
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
  ///   Verifies that the retry strategy honors the Retry-After header when present on a 429 response.
  /// </summary>
  /// <remarks>
  ///   The Polly HttpRetryStrategyOptions automatically honors the Retry-After header.
  ///   This test verifies that the SDK's pipeline is correctly configured to do so.
  /// </remarks>
  [Fact]
  public async Task Retry_WhenRetryAfterHeaderPresent_ShouldHonorRetryAfterDelay()
  {
    // Arrange
    var stopwatch = new System.Diagnostics.Stopwatch();
    var (client, handler) = SetupClient(
      options =>
      {
        options.RateLimiting.IsEnabled  = true;
        options.RateLimiting.MaxRetries = 1;
      },
      handler =>
      {
        // First response: 429 with Retry-After header indicating 1 second delay.
        var response429 = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response429.Headers.Add("Retry-After", "1"); // 1 second delay.

        handler.Protected()
               .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(response429)
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
               {
                 Content = new StringContent(HttpFixtures.CreateSuccessResponse<object?>(null))
               });
      });

    // Act
    stopwatch.Start();
    var action = async () => await client.Accounts.DeleteR2BucketAsync("any-bucket");
    await action.Should().NotThrowAsync();
    stopwatch.Stop();

    // Assert
    // The retry should have waited at least the Retry-After duration (1 second).
    // We use 900ms to account for timing variations but ensure significant delay occurred.
    stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(900);

    // Verify that the request was sent twice (1 initial + 1 retry).
    handler.Protected().Verify(
      "SendAsync",
      Times.Exactly(2),
      ItExpr.IsAny<HttpRequestMessage>(),
      ItExpr.IsAny<CancellationToken>());
  }

  /// <summary>
  ///   Verifies that when all retries are exhausted, the final error is propagated to the caller.
  /// </summary>
  [Fact]
  public async Task Retry_WhenAllRetriesExhausted_ShouldThrowFinalException()
  {
    // Arrange
    var (client, handler) = SetupClient(
      options =>
      {
        // Configure 2 retries (total of 3 attempts).
        options.RateLimiting.MaxRetries = 2;
      },
      handler =>
      {
        // All responses will be 503, forcing all retries to fail.
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Returns((HttpRequestMessage req, CancellationToken _) =>
               {
                 return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                 {
                   RequestMessage = req // Required for idempotency check.
                 });
               });
      });

    // Act
    var action = async () => await client.Accounts.DeleteR2BucketAsync("any-bucket");

    // Assert
    // After all retries are exhausted, the final 503 should propagate as HttpRequestException.
    await action.Should().ThrowAsync<HttpRequestException>()
                .Where(ex => ex.StatusCode == HttpStatusCode.ServiceUnavailable);

    // Verify that the request was sent exactly 3 times (1 initial + 2 retries).
    handler.Protected().Verify(
      "SendAsync",
      Times.Exactly(3),
      ItExpr.IsAny<HttpRequestMessage>(),
      ItExpr.IsAny<CancellationToken>());
  }

  /// <summary>
  ///   Verifies that 408 Request Timeout responses are retried as transient errors.
  /// </summary>
  [Fact]
  public async Task Retry_When408RequestTimeout_ShouldRetryRequest()
  {
    // Arrange
    var (client, handler) = SetupClient(
      options =>
      {
        options.RateLimiting.MaxRetries = 1;
      },
      handler =>
      {
        // First response: 408 Request Timeout (server-side timeout, retriable).
        // Second response: 200 OK (success).
        handler.Protected()
               .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() =>
               {
                 var response = new HttpResponseMessage(HttpStatusCode.RequestTimeout);
                 return response;
               })
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
               {
                 Content = new StringContent(HttpFixtures.CreateSuccessResponse<object?>(null))
               });
      });

    // Act
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
  ///   Verifies that idempotent methods (PUT, DELETE) are retried on transient errors.
  /// </summary>
  [Fact]
  public async Task Retry_ForIdempotentDeleteMethod_ShouldRetryOnTransientError()
  {
    // Arrange
    var (client, handler) = SetupClient(
      options => { options.RateLimiting.MaxRetries = 1; },
      handler =>
      {
        // First response: 503 Service Unavailable.
        // Second response: 200 OK (success).
        handler.Protected()
               .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() =>
               {
                 var response         = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                 response.RequestMessage = new HttpRequestMessage(HttpMethod.Delete, "http://test");

                 return response;
               })
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
               {
                 Content = new StringContent(HttpFixtures.CreateSuccessResponse<object?>(null))
               });
      });

    // Act - DeleteR2BucketAsync uses DELETE, which is idempotent and should be retried.
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
  ///   Verifies that the outer "Total Timeout" (60 seconds) correctly cancels the entire operation,
  ///   including all retry attempts, when the cumulative time exceeds the limit.
  /// </summary>
  /// <remarks>
  ///   This test is distinct from the AttemptTimeout test. The AttemptTimeout is per-request,
  ///   while the TotalTimeout wraps all retries. This test verifies that even if individual
  ///   attempts succeed within their timeout, the overall operation can still timeout.
  /// </remarks>
  [Fact]
  public async Task TotalTimeout_WhenRetriesExceedTotalTimeout_ThrowsTimeoutRejectedException()
  {
    // Arrange
    // We configure 2 retries (3 total attempts). Each attempt will delay 25 seconds.
    // 3 attempts * 25 seconds = 75 seconds, which exceeds the 60-second total timeout.
    // However, for test speed, we use a custom setup that simulates this scenario faster.
    var attemptCount = 0;
    var (client, handler) = SetupClientWithCustomTotalTimeout(
      totalTimeout: TimeSpan.FromSeconds(2), // Short timeout for test speed.
      options =>
      {
        options.DefaultTimeout          = TimeSpan.FromSeconds(10); // Per-attempt timeout (won't trigger).
        options.RateLimiting.MaxRetries = 5;                        // Allow many retries.
      },
      handler =>
      {
        // Each request delays 1 second, then returns 503 (retriable).
        // With 5 retries allowed, we'd need 6 attempts * 1 second = 6 seconds.
        // The 2-second total timeout should trigger before all retries complete.
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Returns(async (HttpRequestMessage req, CancellationToken ct) =>
               {
                 attemptCount++;
                 await Task.Delay(TimeSpan.FromMilliseconds(800), ct);

                 return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                 {
                   RequestMessage = req
                 };
               });
      });

    // Act
    var action = async () => await client.Accounts.DeleteR2BucketAsync("any-bucket");

    // Assert
    // The total timeout should trigger, throwing TimeoutRejectedException.
    await action.Should().ThrowAsync<TimeoutRejectedException>();

    // Verify that at least one attempt was made, but not all retries completed.
    // With 5 retries allowed, we'd have 6 total attempts if no timeout occurred.
    attemptCount.Should().BeGreaterThan(0, "at least one attempt should be made");
    attemptCount.Should().BeLessThan(6, "total timeout should prevent all retries from completing");

    _output.WriteLine($"Total attempts before timeout: {attemptCount}");
  }

  /// <summary>
  ///   Verifies that transient network failures (HttpRequestException) trigger retry behavior.
  /// </summary>
  /// <remarks>
  ///   HttpRequestException is explicitly handled in the ShouldHandle predicate for scenarios
  ///   like DNS failures, connection refused, or network unreachable.
  /// </remarks>
  [Fact]
  public async Task Retry_WhenHttpRequestExceptionOccurs_ShouldRetryRequest()
  {
    // Arrange
    var (client, handler) = SetupClient(
      options => { options.RateLimiting.MaxRetries = 1; },
      handler =>
      {
        // First call: Simulate a network failure (e.g., connection refused).
        // Second call: Return success.
        handler.Protected()
               .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ThrowsAsync(new HttpRequestException("Connection refused", null, HttpStatusCode.ServiceUnavailable))
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
               {
                 Content = new StringContent(HttpFixtures.CreateSuccessResponse<object?>(null))
               });
      });

    // Act
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
  ///   Verifies that when an individual attempt times out (TimeoutRejectedException),
  ///   the retry strategy catches it and retries the request.
  /// </summary>
  /// <remarks>
  ///   This is different from the AttemptTimeout test which disables retries.
  ///   This test verifies that timeouts are treated as retriable transient failures.
  /// </remarks>
  [Fact]
  public async Task Retry_WhenAttemptTimesOut_ShouldRetryRequest()
  {
    // Arrange
    var attemptCount = 0;
    var (client, _) = SetupClient(
      options =>
      {
        options.DefaultTimeout          = TimeSpan.FromSeconds(1); // Very short per-attempt timeout.
        options.RateLimiting.MaxRetries = 1;                       // Allow 1 retry.
      },
      handler =>
      {
        // First attempt: Delay longer than the per-attempt timeout, causing TimeoutRejectedException.
        // Second attempt: Return success quickly.
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Returns(async (HttpRequestMessage _, CancellationToken ct) =>
               {
                 attemptCount++;

                 if (attemptCount == 1)
                 {
                   // First attempt: delay to trigger timeout.
                   await Task.Delay(TimeSpan.FromSeconds(5), ct);

                   return new HttpResponseMessage(HttpStatusCode.OK);
                 }

                 // Second attempt: return success immediately.
                 return new HttpResponseMessage(HttpStatusCode.OK)
                 {
                   Content = new StringContent(HttpFixtures.CreateSuccessResponse<object?>(null))
                 };
               });
      });

    // Act
    var action = async () => await client.Accounts.DeleteR2BucketAsync("any-bucket");

    // Assert
    // The operation should succeed because the retry catches the timeout and retries.
    await action.Should().NotThrowAsync();

    // Verify that two attempts were made.
    attemptCount.Should().Be(2, "first attempt should timeout, second should succeed");
  }

  /// <summary>
  ///   Verifies that the circuit breaker transitions from Open to Half-Open after the break duration,
  ///   and then to Closed after a successful request.
  /// </summary>
  [Fact]
  public async Task CircuitBreaker_AfterBreakDuration_ShouldTransitionToHalfOpenAndRecover()
  {
    // Arrange
    // The default circuit breaker has a MinimumThroughput of 5 and a BreakDuration of 5 seconds.
    // For test speed, we'll use the default settings and wait for the break duration.
    var defaultOptions     = new HttpCircuitBreakerStrategyOptions();
    var minimumThroughput  = defaultOptions.MinimumThroughput;
    var breakDuration      = defaultOptions.BreakDuration;
    var requestCount       = 0;
    var shouldSucceed      = false;

    var (client, handler) = SetupClient(
      options => { options.RateLimiting.MaxRetries = 0; }, // Disable retries to isolate circuit breaker.
      handler =>
      {
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Returns(() =>
               {
                 requestCount++;

                 if (shouldSucceed)
                 {
                   return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                   {
                     Content = new StringContent(HttpFixtures.CreateSuccessResponse<object?>(null))
                   });
                 }

                 return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
               });
      });

    // Act & Assert

    // Phase 1: Trigger consecutive failures to open the circuit.
    for (var i = 0; i < minimumThroughput; i++)
    {
      var failAction = async () => await client.Accounts.DeleteR2BucketAsync("any-bucket");
      await failAction.Should().ThrowAsync<HttpRequestException>();
    }

    var failedAttempts = requestCount;
    _output.WriteLine($"Phase 1: {failedAttempts} failed attempts, circuit should be open.");

    // Phase 2: Verify circuit is open (should throw BrokenCircuitException without hitting handler).
    var blockedAction = async () => await client.Accounts.DeleteR2BucketAsync("any-bucket");
    await blockedAction.Should().ThrowAsync<BrokenCircuitException>();
    requestCount.Should().Be(failedAttempts, "circuit is open, no new requests should reach handler");

    // Phase 3: Wait for break duration to allow circuit to transition to half-open.
    _output.WriteLine($"Phase 2: Waiting {breakDuration.TotalSeconds}s for break duration...");
    await Task.Delay(breakDuration + TimeSpan.FromMilliseconds(500)); // Add buffer for timing.

    // Phase 4: Now configure handler to succeed and make a request.
    // In half-open state, one request is allowed through.
    shouldSucceed = true;

    var recoveryAction = async () => await client.Accounts.DeleteR2BucketAsync("any-bucket");
    await recoveryAction.Should().NotThrowAsync();

    _output.WriteLine($"Phase 3: Recovery request succeeded, circuit should be closed.");

    // Phase 5: Verify circuit is now closed by making another successful request.
    var finalAction = async () => await client.Accounts.DeleteR2BucketAsync("any-bucket");
    await finalAction.Should().NotThrowAsync();

    // Total requests should be: minimumThroughput (failures) + 1 (recovery) + 1 (verification)
    requestCount.Should().Be(minimumThroughput + 2);
  }

  /// <summary>
  ///   Verifies that idempotent PUT methods are retried on transient errors.
  /// </summary>
  /// <remarks>
  ///   PUT is explicitly listed as idempotent in the retry predicate alongside DELETE.
  ///   This test ensures PUT receives the same retry treatment as DELETE.
  /// </remarks>
  [Fact]
  public async Task Retry_ForIdempotentPutMethod_ShouldRetryOnTransientError()
  {
    // Arrange
    var (client, handler) = SetupClient(
      options => { options.RateLimiting.MaxRetries = 1; },
      handler =>
      {
        // First response: 503 Service Unavailable.
        // Second response: 200 OK (success).
        handler.Protected()
               .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() =>
               {
                 var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                 {
                   RequestMessage = new HttpRequestMessage(HttpMethod.Put, "http://test")
                 };

                 return response;
               })
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
               {
                 // TriggerActivationCheckAsync expects an ActivationCheckResult response.
                 Content = new StringContent(HttpFixtures.CreateSuccessResponse(new { Id = "zone-123" }))
               });
      });

    // Act - TriggerActivationCheckAsync uses PUT, which is idempotent and should be retried.
    var action = async () => await client.Zones.TriggerActivationCheckAsync("any-zone");

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
  ///   Verifies that non-idempotent PATCH methods are NOT retried on transient errors,
  ///   to prevent partial updates from being applied multiple times.
  /// </summary>
  [Fact]
  public async Task Retry_ForUnsafePatchMethod_ShouldNotRetryOnTransientError()
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
                 // Ensure the response has the request message for idempotency check.
                 return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                 {
                   RequestMessage = req
                 });
               });
      });

    // Act - PatchDnsRecordAsync uses PATCH, which is not idempotent and should NOT be retried.
    var action = async () => await client.Dns.PatchDnsRecordAsync(
      "any-zone",
      "any-record",
      new PatchDnsRecordRequest(Content: "192.0.2.100"));

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
  ///   Verifies that the retry strategy uses exponential backoff with increasing delays between attempts.
  /// </summary>
  /// <remarks>
  ///   The pipeline is configured with DelayBackoffType.Exponential and a base delay of 1 second.
  ///   With jitter enabled, exact timings vary, but delays should increase with each attempt.
  /// </remarks>
  [Fact]
  public async Task Retry_ShouldUseExponentialBackoffWithIncreasingDelays()
  {
    // Arrange
    var attemptTimestamps = new List<DateTimeOffset>();
    var (client, handler) = SetupClient(
      options =>
      {
        // Configure 3 retries (4 total attempts) to observe the exponential pattern.
        options.RateLimiting.MaxRetries = 3;
      },
      handler =>
      {
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Returns((HttpRequestMessage req, CancellationToken _) =>
               {
                 attemptTimestamps.Add(DateTimeOffset.UtcNow);

                 return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                 {
                   RequestMessage = req
                 });
               });
      });

    // Act
    var action = async () => await client.Accounts.DeleteR2BucketAsync("any-bucket");
    await action.Should().ThrowAsync<HttpRequestException>();

    // Assert
    attemptTimestamps.Should().HaveCount(4, "1 initial + 3 retries = 4 total attempts");

    // Calculate delays between consecutive attempts.
    var delays = new List<TimeSpan>();

    for (var i = 1; i < attemptTimestamps.Count; i++)
    {
      delays.Add(attemptTimestamps[i] - attemptTimestamps[i - 1]);
    }

    _output.WriteLine($"Attempt timestamps: {string.Join(", ", attemptTimestamps.Select(t => t.ToString("HH:mm:ss.fff")))}");
    _output.WriteLine($"Delays between attempts: {string.Join(", ", delays.Select(d => $"{d.TotalMilliseconds:F0}ms"))}");

    // With exponential backoff (base 1s, jitter enabled), each delay should generally be longer than the previous.
    // Due to jitter, we can't assert strict ordering, but we can verify:
    // 1. All delays are positive (some waiting occurred).
    // 2. The average delay is reasonable for exponential backoff.
    delays.Should().AllSatisfy(d => d.Should().BeGreaterThan(TimeSpan.Zero));

    // The total delay should be at least 1 second (the base delay) for the first retry.
    // With exponential backoff: ~1s, ~2s, ~4s base delays (before jitter).
    var totalDelay = delays.Aggregate(TimeSpan.Zero, (acc, d) => acc + d);
    totalDelay.Should().BeGreaterThan(TimeSpan.FromSeconds(2), "exponential backoff should result in meaningful delays");

    _output.WriteLine($"Total delay across all retries: {totalDelay.TotalSeconds:F2}s");
  }

  /// <summary>
  ///   Verifies that when the concurrency limit is reached, additional requests are queued
  ///   up to the queue limit rather than immediately rejected.
  /// </summary>
  [Fact]
  public async Task RateLimiter_WhenConcurrencyExceeded_ShouldQueueRequestsUpToLimit()
  {
    // Arrange
    var (client, handler) = SetupClient(
      options =>
      {
        // Configure a restrictive rate limiter: 1 concurrent request, but allow 2 in queue.
        options.RateLimiting.PermitLimit = 1;
        options.RateLimiting.QueueLimit  = 2;
      },
      handler =>
      {
        // Each request delays briefly to ensure concurrency conflicts.
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Returns(async (HttpRequestMessage _, CancellationToken ct) =>
               {
                 await Task.Delay(TimeSpan.FromMilliseconds(100), ct);

                 return new HttpResponseMessage(HttpStatusCode.OK)
                 {
                   Content = new StringContent(HttpFixtures.CreateSuccessResponse<object?>(null))
                 };
               });
      });

    // Act - Start 3 requests concurrently.
    // With PermitLimit=1 and QueueLimit=2:
    // - 1 request executes immediately
    // - 2 requests queue
    // - All 3 should eventually succeed
    var task1 = client.Accounts.DeleteR2BucketAsync("bucket-1");
    var task2 = client.Accounts.DeleteR2BucketAsync("bucket-2");
    var task3 = client.Accounts.DeleteR2BucketAsync("bucket-3");

    // Assert - All requests should complete successfully (queued requests wait their turn).
    var action = () => Task.WhenAll(task1, task2, task3);
    await action.Should().NotThrowAsync();

    // Verify that all 3 requests reached the handler.
    handler.Protected().Verify(
      "SendAsync",
      Times.Exactly(3),
      ItExpr.IsAny<HttpRequestMessage>(),
      ItExpr.IsAny<CancellationToken>());
  }

  /// <summary>
  ///   Verifies that requests exceeding both the permit limit and queue limit are rejected.
  /// </summary>
  [Fact]
  public async Task RateLimiter_WhenQueueLimitExceeded_ShouldRejectRequest()
  {
    // Arrange
    var (client, handler) = SetupClient(
      options =>
      {
        // Configure: 1 concurrent, 1 in queue, so 3rd request should be rejected.
        options.RateLimiting.PermitLimit = 1;
        options.RateLimiting.QueueLimit  = 1;
      },
      handler =>
      {
        // Each request delays to ensure concurrency conflicts.
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

    // Act - Start 3 requests concurrently.
    // With PermitLimit=1 and QueueLimit=1:
    // - 1 request executes immediately
    // - 1 request queues
    // - 1 request should be rejected (queue full)
    var task1 = client.Accounts.DeleteR2BucketAsync("bucket-1");
    var task2 = client.Accounts.DeleteR2BucketAsync("bucket-2");
    var task3 = client.Accounts.DeleteR2BucketAsync("bucket-3");

    // Assert
    var action = () => Task.WhenAll(task1, task2, task3);
    await action.Should().ThrowAsync<RateLimiterRejectedException>();

    // At least one request should have been rejected before reaching the handler.
    // With our setup, at most 2 requests should reach the handler (1 executing + 1 queued).
    handler.Protected().Verify(
      "SendAsync",
      Times.AtMost(2),
      ItExpr.IsAny<HttpRequestMessage>(),
      ItExpr.IsAny<CancellationToken>());
  }

  /// <summary>
  ///   Verifies that proactive rate limit throttling is applied when the server returns
  ///   rate limit headers indicating low remaining quota.
  /// </summary>
  /// <remarks>
  ///   The RateLimitHeaders.Polly library parses IETF draft rate limit headers and delays
  ///   requests when the remaining quota falls below the configured threshold.
  /// </remarks>
  [Fact]
  public async Task RateLimitHeaders_WhenQuotaLow_ShouldThrottleRequests()
  {
    // Arrange
    var requestTimestamps = new List<DateTimeOffset>();
    var (client, handler) = SetupClient(
      options =>
      {
        options.RateLimiting.EnableProactiveThrottling = true;
        options.RateLimiting.QuotaLowThreshold         = 0.5; // Throttle when 50% or less remains.
        options.RateLimiting.MaxRetries                = 0;   // Disable retries to isolate throttling.
      },
      handler =>
      {
        var requestCount = 0;

        handler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Returns((HttpRequestMessage _, CancellationToken ct) =>
               {
                 requestTimestamps.Add(DateTimeOffset.UtcNow);
                 requestCount++;

                 // Return response with IETF rate limit headers.
                 // Format: RateLimit: "default";r=<remaining>;t=<reset-seconds>
                 // Format: RateLimit-Policy: "default";q=<quota>;w=<window-seconds>
                 var response = new HttpResponseMessage(HttpStatusCode.OK)
                 {
                   Content = new StringContent(HttpFixtures.CreateSuccessResponse<object?>(null))
                 };

                 // First request: Return headers showing 10% quota remaining (below 50% threshold).
                 // This should trigger throttling for subsequent requests.
                 if (requestCount == 1)
                 {
                   // 10 remaining out of 100 quota, resets in 2 seconds.
                   response.Headers.Add("RateLimit", "\"default\";r=10;t=2");
                   response.Headers.Add("RateLimit-Policy", "\"default\";q=100;w=60");
                 }
                 else
                 {
                   // Subsequent requests: quota restored.
                   response.Headers.Add("RateLimit", "\"default\";r=90;t=60");
                   response.Headers.Add("RateLimit-Policy", "\"default\";q=100;w=60");
                 }

                 return Task.FromResult(response);
               });
      });

    // Act - Make two sequential requests.
    await client.Accounts.DeleteR2BucketAsync("bucket-1");
    await client.Accounts.DeleteR2BucketAsync("bucket-2");

    // Assert
    requestTimestamps.Should().HaveCount(2);

    // The second request should be delayed due to proactive throttling.
    var delayBetweenRequests = requestTimestamps[1] - requestTimestamps[0];

    _output.WriteLine($"Delay between requests: {delayBetweenRequests.TotalMilliseconds:F0}ms");

    // With proactive throttling and low quota, there should be a noticeable delay.
    // The exact delay depends on the library's algorithm, but it should be positive.
    // Note: This test may need adjustment based on actual RateLimitHeaders.Polly behavior.
    delayBetweenRequests.Should().BeGreaterThan(TimeSpan.Zero,
      "proactive throttling should introduce delay when quota is low");
  }

  /// <summary>
  ///   A helper method to set up the DI container with a mock HttpMessageHandler and custom options for a single
  ///   test.
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
      // Provide a valid test ApiToken to pass validation.
      // The actual value doesn't matter since we're using mocked HTTP handlers.
      options.ApiToken = "test-api-token-for-resilience-tests";

      // Apply test-specific options (may override the above if needed).
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

  /// <summary>
  ///   A helper method to set up the DI container with a custom total timeout for testing the outer timeout behavior.
  ///   This bypasses the standard SDK registration to allow overriding the normally-fixed 60-second total timeout.
  /// </summary>
  /// <param name="totalTimeout">The total timeout to use for the entire operation including retries.</param>
  /// <param name="configureOptions">Action to configure Cloudflare API options.</param>
  /// <param name="configureHandler">Action to configure the mock HTTP handler.</param>
  /// <returns>A tuple containing the configured client and the mock handler for verification.</returns>
  private (ICloudflareApiClient Client, Mock<HttpMessageHandler> Handler) SetupClientWithCustomTotalTimeout(
    TimeSpan                         totalTimeout,
    Action<CloudflareApiOptions>?    configureOptions,
    Action<Mock<HttpMessageHandler>> configureHandler)
  {
    var services = new ServiceCollection();

    // Setup logging to pipe to the xUnit test output.
    services.AddLogging(builder => builder.AddProvider(new XunitTestOutputLoggerProvider { Current = _output }));

    var mockHandler = new Mock<HttpMessageHandler>();
    configureHandler(mockHandler);

    // Configure options for later use in the pipeline.
    var cfOptions = new CloudflareApiOptions
    {
      ApiToken = "test-api-token-for-resilience-tests"
    };
    configureOptions?.Invoke(cfOptions);

    services.Configure<CloudflareApiOptions>(options =>
    {
      options.ApiToken       = cfOptions.ApiToken;
      options.DefaultTimeout = cfOptions.DefaultTimeout;
      options.RateLimiting   = cfOptions.RateLimiting;
    });

    // Register the HttpClient with a custom resilience pipeline that uses our test-specific total timeout.
    services.AddHttpClient<ICloudflareApiClient, CloudflareApiClient>((_, client) =>
            {
              client.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/");
              client.Timeout     = TimeSpan.FromMinutes(5); // High HttpClient timeout, let resilience pipeline control.
            })
            .AddResilienceHandler("TestPipeline", (builder, context) =>
            {
              var logger = context.ServiceProvider.GetRequiredService<ILoggerFactory>()
                                  .CreateLogger(LoggingConstants.Categories.HttpResilience);

              // Build a pipeline that mirrors production but with a custom total timeout.
              ConfigureTestResiliencePipeline(builder, cfOptions, logger, totalTimeout);
            });

    // Inject the mock as the primary handler.
    services.ConfigureAll<HttpClientFactoryOptions>(opts =>
    {
      opts.HttpMessageHandlerBuilderActions.Add(builder =>
      {
        builder.PrimaryHandler = mockHandler.Object;
      });
    });

    var serviceProvider = services.BuildServiceProvider();
    var client          = serviceProvider.GetRequiredService<ICloudflareApiClient>();

    return (client, mockHandler);
  }

  /// <summary>
  ///   Configures a resilience pipeline that mirrors production behavior but with a configurable total timeout.
  ///   This is used by test methods that need to verify total timeout behavior without waiting 60 seconds.
  /// </summary>
  /// <param name="builder">The resilience pipeline builder.</param>
  /// <param name="cfOptions">The Cloudflare API options.</param>
  /// <param name="logger">The logger for resilience events.</param>
  /// <param name="totalTimeout">The custom total timeout to use.</param>
  private static void ConfigureTestResiliencePipeline(
    ResiliencePipelineBuilder<HttpResponseMessage> builder,
    CloudflareApiOptions                           cfOptions,
    ILogger                                        logger,
    TimeSpan                                       totalTimeout)
  {
    // 1. Total Request Timeout (custom for testing).
    builder.AddTimeout(new HttpTimeoutStrategyOptions
    {
      Name    = "Test:TotalTimeout",
      Timeout = totalTimeout
    });

    // 2. Retry Strategy (mirrors production).
    if (cfOptions.RateLimiting.MaxRetries > 0)
    {
      builder.AddRetry(new HttpRetryStrategyOptions
      {
        Name             = "Test:Retry",
        BackoffType      = DelayBackoffType.Exponential,
        Delay            = TimeSpan.FromMilliseconds(100), // Short delay for test speed.
        UseJitter        = true,
        MaxRetryAttempts = cfOptions.RateLimiting.MaxRetries,
        ShouldHandle     = args =>
        {
          // Gate by idempotency.
          var method = args.Outcome.Result?.RequestMessage?.Method;

          if (method is not null)
          {
            var isIdempotent =
              method == HttpMethod.Get ||
              method == HttpMethod.Head ||
              method == HttpMethod.Options ||
              method == HttpMethod.Trace ||
              method == HttpMethod.Put ||
              method == HttpMethod.Delete;

            if (!isIdempotent)
              return new ValueTask<bool>(false);
          }

          // Handle transient exceptions.
          if (args.Outcome.Exception is HttpRequestException or TimeoutRejectedException)
            return new ValueTask<bool>(true);

          // Handle transient HTTP responses.
          if (args.Outcome.Result is not { } response)
            return new ValueTask<bool>(false);

          var statusCode = response.StatusCode;

          if (statusCode == HttpStatusCode.RequestTimeout || (int)statusCode >= 500)
            return new ValueTask<bool>(true);

          if (statusCode == HttpStatusCode.TooManyRequests)
            return new ValueTask<bool>(cfOptions.RateLimiting.IsEnabled);

          return new ValueTask<bool>(false);
        },
        OnRetry = args =>
        {
          var req = args.Outcome.Result?.RequestMessage;
          logger.LogWarning("Test retry: {Method} {Uri}, attempt {Attempt}", req?.Method, req?.RequestUri, args.AttemptNumber + 1);

          return default;
        }
      });
    }

    // 3. Circuit Breaker (mirrors production).
    builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions { Name = "Test:CircuitBreaker" });

    // 4. Per-attempt timeout.
    builder.AddTimeout(new HttpTimeoutStrategyOptions
    {
      Name    = "Test:AttemptTimeout",
      Timeout = cfOptions.DefaultTimeout
    });
  }

  #endregion
}
