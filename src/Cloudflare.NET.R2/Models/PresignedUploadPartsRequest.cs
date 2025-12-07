namespace Cloudflare.NET.R2.Models;

/// <summary>Defines the parameters for creating a batch of presigned URLs for multiple parts of a multipart upload.</summary>
/// <param name="Key">The object key (path).</param>
/// <param name="UploadId">The ID of the multipart upload.</param>
/// <param name="ExpiresAfter">The duration for which the URLs will be valid.</param>
/// <param name="PartNumberAndLength">A dictionary mapping each part number to its specific content length.</param>
/// <param name="HeadersToSign">An optional dictionary of additional headers to include in the signatures for all parts.</param>
public record PresignedUploadPartsRequest(
  string                               Key,
  string                               UploadId,
  TimeSpan                             ExpiresAfter,
  IReadOnlyDictionary<int, long>       PartNumberAndLength,
  IReadOnlyDictionary<string, string>? HeadersToSign = null
);
