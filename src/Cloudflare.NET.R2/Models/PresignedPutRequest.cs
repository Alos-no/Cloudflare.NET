namespace Cloudflare.NET.R2.Models;

using Amazon.S3.Model;

/// <summary>Defines the parameters for creating a presigned PUT URL, which allows for direct, secure uploads.</summary>
/// <param name="Key">The object key (path) where the file will be stored in the bucket.</param>
/// <param name="ExpiresAfter">The duration for which the presigned URL will be valid.</param>
/// <param name="ContentLength">The exact size of the file to be uploaded, in bytes. This is enforced by R2.</param>
/// <param name="ContentType">The MIME type of the file to be uploaded (e.g., "image/jpeg"). This is enforced by R2.</param>
/// <param name="Conditions">Optional S3 POST policy conditions to enforce additional constraints on the upload.</param>
/// <param name="HeadersToSign">
///   An optional dictionary of additional headers to include in the signature, enforcing them on
///   the client-side upload.
/// </param>
/// <param name="Checksum">
///   An optional digest to bind the uploaded bytes to. Its algorithm's header is signed into the URL, the client
///   must send it, and R2 rejects the upload with 400 <c>BadDigest</c> (storing nothing) when the arriving bytes
///   do not hash to the stated digest. All five <see cref="R2ChecksumAlgorithm" /> values are verified on a
///   single-part PUT. When the same header also appears in <paramref name="HeadersToSign" />, this parameter wins.
/// </param>
public record PresignedPutRequest(
  string                               Key,
  TimeSpan                             ExpiresAfter,
  long                                 ContentLength,
  string                               ContentType,
  IEnumerable<S3PostCondition>?        Conditions    = null,
  IReadOnlyDictionary<string, string>? HeadersToSign = null,
  UploadChecksum?                      Checksum      = null
);
