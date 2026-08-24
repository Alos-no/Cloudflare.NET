namespace Cloudflare.NET.R2.Tests.UnitTests;

using FluentAssertions;
using Models;
using NET.Tests.Shared.Fixtures;

/// <summary>
///   Pins the validation contract of <see cref="UploadChecksum" />: a checksum can only exist when its digest is
///   well-formed base64 decoding to exactly the byte length its algorithm produces, so a presigned URL is never
///   signed for a digest R2 could not possibly accept.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class UploadChecksumTests
{
  #region Methods

  [Theory]
  // One digest of the correct raw length per algorithm: 4 bytes for the CRCs, 20 for SHA-1, 32 for SHA-256,
  // 16 for MD5. The byte values are arbitrary; only the length is validated at construction.
  [InlineData("crc32", 4)]
  [InlineData("crc32c", 4)]
  [InlineData("sha1", 20)]
  [InlineData("sha256", 32)]
  [InlineData("md5", 16)]
  public void Constructor_WithDigestOfExactAlgorithmLength_Succeeds(string algorithmName, int digestByteLength)
  {
    // Arrange
    R2ChecksumAlgorithm.TryParse(algorithmName, out var algorithm).Should().BeTrue();
    var base64Digest = Convert.ToBase64String(new byte[digestByteLength]);

    // Act
    var checksum = new UploadChecksum(algorithm, base64Digest);

    // Assert
    checksum.Algorithm.Should().Be(algorithm);
    checksum.Base64Digest.Should().Be(base64Digest);
  }

  [Fact]
  public void Constructor_WithMalformedBase64_ThrowsArgumentException()
  {
    // Act
    var action = () => new UploadChecksum(R2ChecksumAlgorithm.Sha256, "not-base64!");

    // Assert
    action.Should().Throw<ArgumentException>().WithParameterName("base64Digest");
  }

  [Theory]
  // A digest one byte short and one byte long of what SHA-256 produces (32 bytes): both must be refused,
  // because R2 rejects a mislengthed digest and the URL signed for it would be unusable.
  [InlineData(31)]
  [InlineData(33)]
  public void Constructor_WithDigestOfWrongLength_ThrowsArgumentException(int wrongByteLength)
  {
    // Arrange
    var base64Digest = Convert.ToBase64String(new byte[wrongByteLength]);

    // Act
    var action = () => new UploadChecksum(R2ChecksumAlgorithm.Sha256, base64Digest);

    // Assert
    action.Should().Throw<ArgumentException>().WithParameterName("base64Digest");
  }

  [Fact]
  public void Constructor_WithNullDigest_ThrowsArgumentNullException()
  {
    // Act
    var action = () => new UploadChecksum(R2ChecksumAlgorithm.Sha256, null!);

    // Assert
    action.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void FromDigestBytes_EncodesTheBytesAsBase64()
  {
    // Arrange
    var digestBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };

    // Act
    var checksum = UploadChecksum.FromDigestBytes(R2ChecksumAlgorithm.Crc32, digestBytes);

    // Assert
    checksum.Algorithm.Should().Be(R2ChecksumAlgorithm.Crc32);
    checksum.Base64Digest.Should().Be(Convert.ToBase64String(digestBytes));
  }

  [Fact]
  public void FromDigestBytes_WithWrongByteCount_ThrowsArgumentException()
  {
    // Act
    // A CRC32 digest is exactly 4 bytes; 5 bytes must be refused.
    var action = () => UploadChecksum.FromDigestBytes(R2ChecksumAlgorithm.Crc32, new byte[5]);

    // Assert
    action.Should().Throw<ArgumentException>().WithParameterName("digestBytes");
  }

  [Fact]
  public void TryCreate_WithValidInputs_ProducesTheChecksum()
  {
    // Arrange
    var base64Digest = Convert.ToBase64String(new byte[32]);

    // Act
    // The algorithm name is deliberately upper-cased: TryCreate accepts untrusted strings and resolution
    // is case-insensitive.
    var created = UploadChecksum.TryCreate("SHA256", base64Digest, out var checksum);

    // Assert
    created.Should().BeTrue();
    checksum.Should().NotBeNull();
    checksum!.Algorithm.Should().Be(R2ChecksumAlgorithm.Sha256);
    checksum.Base64Digest.Should().Be(base64Digest);
  }

  [Theory]
  // Each row breaks exactly one requirement: an algorithm name R2 does not verify, a null name, a null
  // digest, malformed base64, and a digest whose decoded length is not what the algorithm produces.
  [InlineData("sha512", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
  [InlineData(null, "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
  [InlineData("sha256", null)]
  [InlineData("sha256", "not-base64!")]
  [InlineData("sha256", "AAAA")]
  public void TryCreate_WithInvalidInputs_ReturnsFalseAndNull(string? algorithmName, string? base64Digest)
  {
    // Act
    var created = UploadChecksum.TryCreate(algorithmName, base64Digest, out var checksum);

    // Assert
    created.Should().BeFalse();
    checksum.Should().BeNull();
  }

  #endregion
}
