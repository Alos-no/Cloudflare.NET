namespace Cloudflare.NET.R2.Models;

using System.Diagnostics;

/// <summary>
///   A digest algorithm R2 verifies against the bytes of an upload, together with the request header its digest
///   travels in and the exact number of raw bytes its digest consists of.
/// </summary>
/// <remarks>
///   <para>
///     This set is closed on purpose. R2 rejects an upload whose bytes do not hash to the digest stated in one of
///     these headers, and that rejection is the entire value of signing the header into a presigned URL: the
///     signature forces the client to send the digest, and R2 does the verifying. A header R2 does not verify
///     would give a caller the impression its bytes were checked when nothing checked them, so no such header is
///     representable here.
///   </para>
///   <para>
///     Verified against live R2 on 2026-08-24, by signing a digest of the wrong bytes into a presigned URL and
///     observing the upload:
///   </para>
///   <list type="bullet">
///     <item>
///       <description>
///         On a single-part PUT, R2 verifies all five algorithms: a wrong digest is rejected with 400
///         <c>BadDigest</c> and nothing is stored.
///       </description>
///     </item>
///     <item>
///       <description>
///         On a multipart part upload, R2 verifies <see cref="Crc32" />, <see cref="Crc32C" /> and
///         <see cref="Md5" /> the same way, but answers 501 <c>NotImplemented</c> whenever a <see cref="Sha1" />
///         or <see cref="Sha256" /> header accompanies the part, regardless of the digest's correctness.
///         <see cref="IsSupportedForPartUploads" /> captures this split.
///       </description>
///     </item>
///   </list>
/// </remarks>
[DebuggerDisplay("{Value}")]
public readonly struct R2ChecksumAlgorithm : IEquatable<R2ChecksumAlgorithm>
{
  #region Constants & Statics

  /// <summary>The 32-bit cyclic redundancy check (IEEE polynomial). Verified on single-part PUTs and part uploads.</summary>
  public static R2ChecksumAlgorithm Crc32 { get; } = new("crc32", "x-amz-checksum-crc32", 4, isSupportedForPartUploads: true);

  /// <summary>The Castagnoli 32-bit cyclic redundancy check. Verified on single-part PUTs and part uploads.</summary>
  public static R2ChecksumAlgorithm Crc32C { get; } = new("crc32c", "x-amz-checksum-crc32c", 4, isSupportedForPartUploads: true);

  /// <summary>
  ///   The SHA-1 digest. Verified on single-part PUTs; R2 answers 501 <c>NotImplemented</c> when this header
  ///   accompanies a part upload.
  /// </summary>
  public static R2ChecksumAlgorithm Sha1 { get; } = new("sha1", "x-amz-checksum-sha1", 20, isSupportedForPartUploads: false);

  /// <summary>
  ///   The SHA-256 digest. Verified on single-part PUTs; R2 answers 501 <c>NotImplemented</c> when this header
  ///   accompanies a part upload.
  /// </summary>
  public static R2ChecksumAlgorithm Sha256 { get; } = new("sha256", "x-amz-checksum-sha256", 32, isSupportedForPartUploads: false);

  /// <summary>
  ///   The MD5 digest. Verified on single-part PUTs and part uploads. Its digest travels in the standard
  ///   <c>Content-MD5</c> header rather than an <c>x-amz-checksum-</c> header.
  /// </summary>
  public static R2ChecksumAlgorithm Md5 { get; } = new("md5", "Content-MD5", 16, isSupportedForPartUploads: true);

  /// <summary>Every algorithm R2 verifies, in a fixed order suitable for enumeration and diagnostics.</summary>
  public static IReadOnlyList<R2ChecksumAlgorithm> All { get; } = [Crc32, Crc32C, Sha1, Sha256, Md5];

  #endregion


  #region Properties & Fields - Non-Public

  /// <summary>Backing field for <see cref="Value" />; null only for <c>default(R2ChecksumAlgorithm)</c>.</summary>
  private readonly string? _value;

  #endregion


  #region Constructors

  /// <summary>Creates one of the fixed algorithm values. Private: the set of verified algorithms is closed.</summary>
  /// <param name="value">The lower-case wire name of the algorithm.</param>
  /// <param name="headerName">The request header the algorithm's digest travels in.</param>
  /// <param name="digestByteLength">The exact number of raw bytes a digest of this algorithm consists of.</param>
  /// <param name="isSupportedForPartUploads">Whether R2 accepts this algorithm's header on a multipart part upload.</param>
  private R2ChecksumAlgorithm(string value, string headerName, int digestByteLength, bool isSupportedForPartUploads)
  {
    _value                    = value;
    HeaderName                = headerName;
    DigestByteLength          = digestByteLength;
    IsSupportedForPartUploads = isSupportedForPartUploads;
  }

  #endregion


  #region Properties Impl - Public

  /// <summary>The lower-case wire name of the algorithm (for example <c>"sha256"</c>).</summary>
  public string Value => _value ?? throw new InvalidOperationException(
    $"An uninitialized {nameof(R2ChecksumAlgorithm)} has no value. Use one of the static values such as {nameof(Sha256)}, or {nameof(TryParse)}.");

  /// <summary>The request header this algorithm's digest travels in, and therefore the header a presigned URL signs.</summary>
  public string HeaderName { get; }

  /// <summary>The exact number of raw bytes a digest of this algorithm consists of, before base64 encoding.</summary>
  public int DigestByteLength { get; }

  /// <summary>
  ///   Whether R2 accepts this algorithm's header on a multipart part upload. When <see langword="false" />, R2
  ///   answers 501 <c>NotImplemented</c> to the part upload regardless of the digest's correctness.
  /// </summary>
  public bool IsSupportedForPartUploads { get; }

  #endregion


  #region Methods Impl

  /// <inheritdoc />
  public override string ToString() => Value;

  /// <inheritdoc />
  public bool Equals(R2ChecksumAlgorithm other) => string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is R2ChecksumAlgorithm other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => _value is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(_value);

  #endregion


  #region Methods

  /// <summary>Resolves an algorithm from its wire name, case-insensitively.</summary>
  /// <param name="name">The name to resolve (for example <c>"sha256"</c> or <c>"SHA256"</c>).</param>
  /// <param name="algorithm">The resolved algorithm, when the name matches one R2 verifies.</param>
  /// <returns><see langword="true" /> when the name matches an algorithm R2 verifies.</returns>
  public static bool TryParse(string? name, out R2ChecksumAlgorithm algorithm)
  {
    foreach (var candidate in All)
    {
      if (string.Equals(candidate.Value, name, StringComparison.OrdinalIgnoreCase))
      {
        algorithm = candidate;

        return true;
      }
    }

    algorithm = default;

    return false;
  }

  /// <summary>Equality by wire name, case-insensitively.</summary>
  public static bool operator ==(R2ChecksumAlgorithm left, R2ChecksumAlgorithm right) => left.Equals(right);

  /// <summary>Inequality by wire name, case-insensitively.</summary>
  public static bool operator !=(R2ChecksumAlgorithm left, R2ChecksumAlgorithm right) => !left.Equals(right);

  #endregion
}
