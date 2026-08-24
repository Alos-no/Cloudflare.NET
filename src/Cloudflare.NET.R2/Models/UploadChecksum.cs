namespace Cloudflare.NET.R2.Models;

using System.Diagnostics;

/// <summary>
///   A digest a presigned upload URL binds the uploaded bytes to: the algorithm, and the base64 text of the
///   digest the bytes must hash to.
/// </summary>
/// <remarks>
///   <para>
///     Passing an instance to <see cref="PresignedPutRequest" />, <see cref="PresignedUploadPartRequest" /> or
///     <see cref="PresignedUploadPartsRequest" /> signs the algorithm's header
///     (<see cref="R2ChecksumAlgorithm.HeaderName" />) into the URL. The client must then send that exact header,
///     or the signature no longer matches and R2 answers 403. When the client does send it, R2 hashes the arriving
///     bytes and rejects the upload with 400 <c>BadDigest</c> if they do not hash to the stated digest, storing
///     nothing.
///   </para>
///   <para>
///     The constructor rejects a digest whose base64 text is malformed or whose decoded length is not exactly what
///     the algorithm produces, so a URL is never signed for a digest R2 could not possibly accept. Use
///     <see cref="TryCreate" /> to validate untrusted algorithm and digest strings without exceptions, such as
///     values arriving in an API request.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // From raw digest bytes computed locally:
///   var checksum = UploadChecksum.FromDigestBytes(R2ChecksumAlgorithm.Sha256, SHA256.HashData(fileBytes));
///
///   // From untrusted strings a client supplied:
///   if (!UploadChecksum.TryCreate(request.Algorithm, request.Digest, out var uploadChecksum))
///       return BadRequest("Unknown checksum algorithm or malformed digest.");
///   </code>
/// </example>
[DebuggerDisplay("{Algorithm.Value,nq}: {Base64Digest}")]
public sealed record UploadChecksum
{
  #region Constructors

  /// <summary>Creates a checksum after validating the digest against the algorithm's exact digest length.</summary>
  /// <param name="algorithm">The algorithm the digest was computed with.</param>
  /// <param name="base64Digest">The base64 text of the digest the uploaded bytes must hash to.</param>
  /// <exception cref="ArgumentException">
  ///   Thrown when <paramref name="algorithm" /> is the uninitialized default, when <paramref name="base64Digest" />
  ///   is not well-formed base64, or when its decoded length differs from the algorithm's
  ///   <see cref="R2ChecksumAlgorithm.DigestByteLength" />.
  /// </exception>
  public UploadChecksum(R2ChecksumAlgorithm algorithm, string base64Digest)
  {
    ArgumentNullException.ThrowIfNull(base64Digest);

    // The largest digest any admitted algorithm produces is 32 bytes (SHA-256); 64 leaves headroom so a
    // too-long digest is detected by length comparison rather than by a buffer failure.
    Span<byte> decoded = stackalloc byte[64];

    if (!Convert.TryFromBase64String(base64Digest, decoded, out var decodedByteCount))
      throw new ArgumentException($"The digest is not well-formed base64: '{base64Digest}'.", nameof(base64Digest));

    if (decodedByteCount != algorithm.DigestByteLength)
      throw new ArgumentException(
        $"A {algorithm.Value} digest is exactly {algorithm.DigestByteLength} bytes, but the supplied digest decodes to {decodedByteCount} bytes.",
        nameof(base64Digest));

    Algorithm    = algorithm;
    Base64Digest = base64Digest;
  }

  #endregion


  #region Properties & Fields - Public

  /// <summary>The algorithm the digest was computed with.</summary>
  public R2ChecksumAlgorithm Algorithm { get; }

  /// <summary>The base64 text of the digest the uploaded bytes must hash to.</summary>
  public string Base64Digest { get; }

  #endregion


  #region Methods

  /// <summary>Creates a checksum from raw digest bytes, base64-encoding them.</summary>
  /// <param name="algorithm">The algorithm the digest was computed with.</param>
  /// <param name="digestBytes">The raw digest bytes, exactly <see cref="R2ChecksumAlgorithm.DigestByteLength" /> of them.</param>
  /// <returns>The validated checksum.</returns>
  /// <exception cref="ArgumentException">Thrown when the byte count differs from what the algorithm produces.</exception>
  public static UploadChecksum FromDigestBytes(R2ChecksumAlgorithm algorithm, ReadOnlySpan<byte> digestBytes)
  {
    if (digestBytes.Length != algorithm.DigestByteLength)
      throw new ArgumentException(
        $"A {algorithm.Value} digest is exactly {algorithm.DigestByteLength} bytes, but {digestBytes.Length} bytes were supplied.",
        nameof(digestBytes));

    return new UploadChecksum(algorithm, Convert.ToBase64String(digestBytes));
  }

  /// <summary>
  ///   Validates an untrusted algorithm name and digest text, producing a checksum only when the name is an
  ///   algorithm R2 verifies and the digest decodes to exactly the length that algorithm produces.
  /// </summary>
  /// <param name="algorithmName">The algorithm name to validate, case-insensitively (for example <c>"sha256"</c>).</param>
  /// <param name="base64Digest">The base64 digest text to validate.</param>
  /// <param name="checksum">The validated checksum, when both inputs are acceptable.</param>
  /// <returns><see langword="true" /> when both inputs are acceptable.</returns>
  public static bool TryCreate(string? algorithmName, string? base64Digest, out UploadChecksum? checksum)
  {
    checksum = null;

    if (base64Digest is null || !R2ChecksumAlgorithm.TryParse(algorithmName, out var algorithm))
      return false;

    Span<byte> decoded = stackalloc byte[64];

    if (!Convert.TryFromBase64String(base64Digest, decoded, out var decodedByteCount)
        || decodedByteCount != algorithm.DigestByteLength)
      return false;

    checksum = new UploadChecksum(algorithm, base64Digest);

    return true;
  }

  #endregion
}
