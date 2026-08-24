namespace Cloudflare.NET.R2.Models;

using Amazon.S3.Model;

/// <summary>Defines the parameters for creating a presigned URL for a single part of a multipart upload.</summary>
/// <param name="Key">The object key (path).</param>
/// <param name="UploadId">The ID of the multipart upload.</param>
/// <param name="PartNumber">The number of the part to be uploaded.</param>
/// <param name="ExpiresAfter">The duration for which the URL is valid.</param>
/// <param name="ContentLength">The exact size of the part to be uploaded. This will be enforced by R2.</param>
/// <param name="ContentType">
///   The MIME type of the file to be uploaded (e.g., "application/octet-stream"). This is enforced
///   by R2.
/// </param>
/// <param name="Conditions">Optional S3 POST policy conditions to enforce additional constraints on the upload.</param>
/// <param name="HeadersToSign">
///   An optional dictionary of additional headers to include in the signature, enforcing them on
///   the client-side upload.
/// </param>
/// <param name="Checksum">
///   An optional digest to bind this part's bytes to. Its algorithm's header is signed into the URL, the client
///   must send it, and R2 rejects the part with 400 <c>BadDigest</c> (recording nothing) when the arriving bytes
///   do not hash to the stated digest. Only algorithms whose
///   <see cref="R2ChecksumAlgorithm.IsSupportedForPartUploads" /> is <see langword="true" /> are accepted here
///   (<see cref="R2ChecksumAlgorithm.Crc32" />, <see cref="R2ChecksumAlgorithm.Crc32C" />,
///   <see cref="R2ChecksumAlgorithm.Md5" />): R2 answers 501 <c>NotImplemented</c> to a part upload carrying a
///   SHA-1 or SHA-256 checksum header, so URL generation refuses those up front. When the same header also
///   appears in <paramref name="HeadersToSign" />, this parameter wins.
/// </param>
public record PresignedUploadPartRequest(
  string                               Key,
  string                               UploadId,
  int                                  PartNumber,
  TimeSpan                             ExpiresAfter,
  long                                 ContentLength,
  string                               ContentType,
  IEnumerable<S3PostCondition>?        Conditions    = null,
  IReadOnlyDictionary<string, string>? HeadersToSign = null,
  UploadChecksum?                      Checksum      = null
);
