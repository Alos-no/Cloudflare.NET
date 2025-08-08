namespace Cloudflare.NET.R2.Exceptions;

using Models;

/// <summary>
///   Represents an exception thrown for a failed R2 API operation, such as a 404 on a
///   GetObject call or a 403 on a PutObject call.
/// </summary>
public class CloudflareR2OperationException : CloudflareR2Exception
{
  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="CloudflareR2OperationException" />
  ///   class.
  /// </summary>
  /// <param name="message">The message that describes the error.</param>
  /// <param name="partialMetrics">The metrics accumulated before the error occurred.</param>
  /// <param name="innerException">The underlying exception from the S3 SDK.</param>
  public CloudflareR2OperationException(string message, R2Result partialMetrics, Exception innerException)
    : base(message, partialMetrics, innerException) { }

  #endregion
}
