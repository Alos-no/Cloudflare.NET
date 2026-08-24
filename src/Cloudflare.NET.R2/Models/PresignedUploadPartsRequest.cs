namespace Cloudflare.NET.R2.Models;

/// <summary>Defines the parameters for creating a batch of presigned URLs for multiple parts of a multipart upload.</summary>
/// <param name="Key">The object key (path).</param>
/// <param name="UploadId">The ID of the multipart upload.</param>
/// <param name="ExpiresAfter">The duration for which the URLs will be valid.</param>
/// <param name="PartNumberAndLength">A dictionary mapping each part number to its specific content length.</param>
/// <param name="HeadersToSign">An optional dictionary of additional headers to include in the signatures for all parts.</param>
/// <param name="ChecksumsByPartNumber">
///   Optional digests to bind individual parts' bytes to, keyed by part number. Every part number used here must
///   also appear in <paramref name="PartNumberAndLength" />; a part without an entry gets no checksum header. Each
///   digest's algorithm header is signed into that part's URL, the client must send it, and R2 rejects the part
///   with 400 <c>BadDigest</c> (recording nothing) when the arriving bytes do not hash to the stated digest. Only
///   algorithms whose <see cref="R2ChecksumAlgorithm.IsSupportedForPartUploads" /> is <see langword="true" /> are
///   accepted (<see cref="R2ChecksumAlgorithm.Crc32" />, <see cref="R2ChecksumAlgorithm.Crc32C" />,
///   <see cref="R2ChecksumAlgorithm.Md5" />): R2 answers 501 <c>NotImplemented</c> to a part upload carrying a
///   SHA-1 or SHA-256 checksum header, so URL generation refuses those up front.
/// </param>
public record PresignedUploadPartsRequest(
  string                                    Key,
  string                                    UploadId,
  TimeSpan                                  ExpiresAfter,
  IReadOnlyDictionary<int, long>            PartNumberAndLength,
  IReadOnlyDictionary<string, string>?      HeadersToSign         = null,
  IReadOnlyDictionary<int, UploadChecksum>? ChecksumsByPartNumber = null
);
