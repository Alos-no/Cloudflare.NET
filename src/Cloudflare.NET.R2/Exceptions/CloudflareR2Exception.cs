namespace Cloudflare.NET.R2.Exceptions;

using Models;

/// <summary>
///   <para>Represents the base class for exceptions thrown by the Cloudflare.NET.R2 client.</para>
///   <para>
///     It captures partial metrics accumulated before the failure, allowing for accurate cost tracking even in error
///     scenarios.
///   </para>
/// </summary>
public abstract class CloudflareR2Exception : Exception
{
  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="CloudflareR2Exception" /> class.</summary>
  /// <param name="message">The message that describes the error.</param>
  /// <param name="partialMetrics">The metrics accumulated before the error occurred.</param>
  /// <param name="innerException">The exception that is the cause of the current exception, or a null reference.</param>
  protected CloudflareR2Exception(string message, R2Result partialMetrics, Exception? innerException = null)
    : base(message, innerException)
  {
    PartialMetrics = partialMetrics;
  }

  #endregion

  #region Properties & Fields - Public

  /// <summary>
  ///   Gets the billable metrics that were consumed before the exception was thrown. This is critical for accurate
  ///   usage tracking, even in failure scenarios.
  /// </summary>
  public R2Result PartialMetrics { get; }

  #endregion
}
