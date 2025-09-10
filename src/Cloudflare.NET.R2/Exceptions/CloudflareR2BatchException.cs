namespace Cloudflare.NET.R2.Exceptions;

using Models;

/// <summary>
///   Represents an exception thrown during a batch operation (e.g., DeleteObjectsAsync)
///   where some items failed to be processed. This exception contains the list of items
///   that failed, allowing for targeted retries.
/// </summary>
/// <typeparam name="T">The type of the items that failed.</typeparam>
public class CloudflareR2BatchException<T> : CloudflareR2OperationException
{
  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="CloudflareR2BatchException{T}" /> class.</summary>
  /// <param name="message">The message that describes the error.</param>
  /// <param name="failedItems">The list of items that failed.</param>
  /// <param name="partialMetrics">The total metrics accumulated during the operation.</param>
  /// <param name="innerException">The underlying exception from the S3 SDK.</param>
  public CloudflareR2BatchException(string           message,
                                    IReadOnlyList<T> failedItems,
                                    R2Result         partialMetrics,
                                    Exception        innerException)
    : base(message, partialMetrics, innerException)
  {
    FailedItems = failedItems;
  }

  #endregion

  #region Properties & Fields - Public

  /// <summary>Gets the list of items that could not be processed.</summary>
  public IReadOnlyList<T> FailedItems { get; }

  #endregion
}
