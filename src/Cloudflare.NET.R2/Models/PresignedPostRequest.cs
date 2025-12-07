namespace Cloudflare.NET.R2.Models;

using Amazon.S3.Model;

/// <summary>Defines the parameters for creating a presigned POST URL for browser-based uploads.</summary>
/// <param name="Key">The object key (path) where the file will be stored in the bucket.</param>
/// <param name="ExpiresAfter">The duration for which the presigned URL will be valid.</param>
/// <param name="ContentType">
///   An optional but recommended condition to enforce the MIME type of the uploaded file (e.g.,
///   "image/jpeg").
/// </param>
/// <param name="ContentLengthRange">
///   An optional but recommended condition to enforce the minimum and maximum size of the
///   uploaded file in bytes.
/// </param>
/// <param name="Conditions">
///   An optional collection of custom S3 Post conditions to include in the policy, allowing for
///   advanced validation scenarios.
/// </param>
/// <param name="HeadersToSign">
///   An optional dictionary of headers to include in the policy, enforcing them on the
///   client-side upload. For POST, these are added as fields in the policy document.
/// </param>
public record PresignedPostRequest(
  string                               Key,
  TimeSpan                             ExpiresAfter,
  string?                              ContentType        = null,
  (long Min, long Max)?                ContentLengthRange = null,
  IEnumerable<S3PostCondition>?        Conditions         = null,
  IReadOnlyDictionary<string, string>? HeadersToSign      = null
);
