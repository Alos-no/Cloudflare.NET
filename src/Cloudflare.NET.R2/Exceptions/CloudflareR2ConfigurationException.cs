namespace Cloudflare.NET.R2.Exceptions;

using Models;

/// <summary>Represents an exception thrown when the R2 client is misconfigured (e.g., missing credentials).</summary>
public class CloudflareR2ConfigurationException : CloudflareR2Exception
{
  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="CloudflareR2ConfigurationException" /> class.</summary>
  /// <param name="message">The message that describes the error.</param>
  public CloudflareR2ConfigurationException(string message) : base(message, new R2Result()) { }

  #endregion
}
