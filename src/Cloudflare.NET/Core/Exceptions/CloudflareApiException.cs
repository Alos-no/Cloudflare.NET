namespace Cloudflare.NET.Core.Exceptions;

using Models;

/// <summary>
///   Represents an exception thrown when the Cloudflare API returns a non-successful response (i.e., `success:
///   false`).
/// </summary>
public class CloudflareApiException : Exception
{
  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="CloudflareApiException" /> class.</summary>
  /// <param name="message">The message that describes the error.</param>
  /// <param name="errors">The list of specific errors returned by the Cloudflare API.</param>
  /// <param name="innerException">The exception that is the cause of the current exception, or a null reference.</param>
  public CloudflareApiException(string message, IReadOnlyList<ApiError> errors, Exception? innerException = null)
    : base(message, innerException)
  {
    Errors = errors;
  }

  #endregion

  #region Properties & Fields - Public

  /// <summary>Gets the detailed error information returned by the Cloudflare API.</summary>
  public IReadOnlyList<ApiError> Errors { get; }

  #endregion
}
