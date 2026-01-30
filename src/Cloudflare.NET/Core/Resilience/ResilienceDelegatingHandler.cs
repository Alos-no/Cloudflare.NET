namespace Cloudflare.NET.Core.Resilience;

using Polly;

/// <summary>
///   A <see cref="DelegatingHandler" /> that wraps HTTP requests with a resilience pipeline.
/// </summary>
/// <remarks>
///   <para>
///     This handler is used for dynamic client creation where we need to apply a resilience
///     pipeline without using the DI-based <c>AddResilienceHandler</c> extension method.
///   </para>
///   <para>
///     Note: This is a simplified implementation. The official <c>ResilienceHandler</c> from
///     <c>Microsoft.Extensions.Http.Resilience</c> became public in later versions (9.x+).
///     This class provides equivalent functionality for version 8.x compatibility.
///   </para>
/// </remarks>
internal sealed class ResilienceDelegatingHandler : DelegatingHandler
{
  #region Properties & Fields - Non-Public

  /// <summary>The resilience pipeline to apply to HTTP requests.</summary>
  private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

  #endregion


  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="ResilienceDelegatingHandler" /> class.
  /// </summary>
  /// <param name="pipeline">The resilience pipeline to apply to HTTP requests.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="pipeline" /> is null.</exception>
  public ResilienceDelegatingHandler(ResiliencePipeline<HttpResponseMessage> pipeline)
  {
    _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
  }


  /// <summary>
  ///   Initializes a new instance of the <see cref="ResilienceDelegatingHandler" /> class
  ///   with the specified inner handler.
  /// </summary>
  /// <param name="pipeline">The resilience pipeline to apply to HTTP requests.</param>
  /// <param name="innerHandler">The inner handler to delegate requests to.</param>
  /// <exception cref="ArgumentNullException">
  ///   Thrown when <paramref name="pipeline" /> or <paramref name="innerHandler" /> is null.
  /// </exception>
  public ResilienceDelegatingHandler(ResiliencePipeline<HttpResponseMessage> pipeline, HttpMessageHandler innerHandler)
    : base(innerHandler)
  {
    _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
  }

  #endregion


  #region Methods - Overrides

  /// <inheritdoc />
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    // Execute the request through the resilience pipeline.
    // The pipeline will handle retries, timeouts, circuit breaker, etc.
    return await _pipeline.ExecuteAsync(
      async token => await base.SendAsync(request, token).ConfigureAwait(false),
      cancellationToken).ConfigureAwait(false);
  }

  #endregion
}
