namespace Cloudflare.NET.R2.Exceptions;

using Models;

/// <summary>
///   Represents an exception thrown during a paginated list operation where the operation
///   failed mid-stream. This exception provides access to the data that was successfully
///   retrieved before the failure occurred.
/// </summary>
/// <typeparam name="T">The type of the items being listed.</typeparam>
public class CloudflareR2ListException<T> : CloudflareR2OperationException
{
  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="CloudflareR2ListException{T}" /> class.</summary>
  /// <param name="message">The message that describes the error.</param>
  /// <param name="partialData">The data that was successfully retrieved.</param>
  /// <param name="partialMetrics">The metrics accumulated before the error occurred.</param>
  /// <param name="innerException">The underlying exception from the S3 SDK.</param>
  public CloudflareR2ListException(string           message,
                                   IReadOnlyList<T> partialData,
                                   R2Result         partialMetrics,
                                   Exception        innerException)
    : base(message, partialMetrics, innerException)
  {
    PartialData = partialData;
  }

  #endregion

  #region Properties & Fields - Public

  /// <summary>Gets the list of items that were successfully fetched before the operation failed.</summary>
  public IReadOnlyList<T> PartialData { get; }

  #endregion
}
