namespace Cloudflare.NET.R2.Tests.UnitTests;

using FluentAssertions;
using Models;
using NET.Tests.Shared.Fixtures;

/// <summary>
///   Pins the fixed facts of <see cref="R2ChecksumAlgorithm" />: the wire names, the header each digest travels
///   in, the exact digest lengths, and which algorithms R2 verifies on multipart part uploads. These values were
///   established against live R2 (2026-08-24) and the API's whole point is that callers never have to guess them.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class R2ChecksumAlgorithmTests
{
  #region Methods

  [Fact]
  public void All_ContainsExactlyTheFiveVerifiedAlgorithms()
  {
    // Arrange & Act
    var all = R2ChecksumAlgorithm.All;

    // Assert
    all.Should().HaveCount(5);
    all.Should().ContainInOrder(
      R2ChecksumAlgorithm.Crc32,
      R2ChecksumAlgorithm.Crc32C,
      R2ChecksumAlgorithm.Sha1,
      R2ChecksumAlgorithm.Sha256,
      R2ChecksumAlgorithm.Md5);
  }

  [Theory]
  // Wire name, digest header, raw digest byte length, and whether R2 accepts the header on a part upload.
  // The CRC digests are 4 bytes, SHA-1 is 20, SHA-256 is 32, MD5 is 16; MD5 travels in the standard
  // Content-MD5 header while the others travel in x-amz-checksum-{name}.
  [InlineData("crc32", "x-amz-checksum-crc32", 4, true)]
  [InlineData("crc32c", "x-amz-checksum-crc32c", 4, true)]
  [InlineData("sha1", "x-amz-checksum-sha1", 20, false)]
  [InlineData("sha256", "x-amz-checksum-sha256", 32, false)]
  [InlineData("md5", "Content-MD5", 16, true)]
  public void EachAlgorithm_CarriesItsWireNameHeaderDigestLengthAndPartSupport(
    string wireName,
    string expectedHeaderName,
    int    expectedDigestByteLength,
    bool   expectedPartSupport)
  {
    // Arrange & Act
    R2ChecksumAlgorithm.TryParse(wireName, out var algorithm).Should().BeTrue();

    // Assert
    algorithm.Value.Should().Be(wireName);
    algorithm.HeaderName.Should().Be(expectedHeaderName);
    algorithm.DigestByteLength.Should().Be(expectedDigestByteLength);
    algorithm.IsSupportedForPartUploads.Should().Be(expectedPartSupport);
  }

  [Theory]
  // Resolution is case-insensitive so a name arriving from configuration or an API request in any casing
  // resolves to the same algorithm.
  [InlineData("SHA256")]
  [InlineData("Sha256")]
  [InlineData("sha256")]
  public void TryParse_IsCaseInsensitive(string name)
  {
    // Act
    var parsed = R2ChecksumAlgorithm.TryParse(name, out var algorithm);

    // Assert
    parsed.Should().BeTrue();
    algorithm.Should().Be(R2ChecksumAlgorithm.Sha256);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("sha512")]         // A real algorithm, but one R2 does not verify.
  [InlineData("crc64nvme")]      // Ditto: S3 defines it, R2 verification is unproven.
  [InlineData("x-amz-checksum-sha256")] // A header name is not a wire name.
  public void TryParse_WithUnknownName_ReturnsFalseAndDefault(string? name)
  {
    // Act
    var parsed = R2ChecksumAlgorithm.TryParse(name, out var algorithm);

    // Assert
    parsed.Should().BeFalse();
    algorithm.Should().Be(default(R2ChecksumAlgorithm));
  }

  [Fact]
  public void DefaultInstance_ThrowsOnValueAccess()
  {
    // Arrange
    // default(R2ChecksumAlgorithm) can only arise from uninitialized storage, never from the API surface,
    // so reading its wire name fails loudly instead of producing a null header downstream.
    var uninitialized = default(R2ChecksumAlgorithm);

    // Act
    var action = () => uninitialized.Value;

    // Assert
    action.Should().Throw<InvalidOperationException>();
  }

  [Fact]
  public void Equality_ComparesByWireNameCaseInsensitively()
  {
    // Arrange
    R2ChecksumAlgorithm.TryParse("CRC32", out var parsedUpperCase).Should().BeTrue();

    // Assert
    (parsedUpperCase == R2ChecksumAlgorithm.Crc32).Should().BeTrue();
    (R2ChecksumAlgorithm.Crc32 != R2ChecksumAlgorithm.Crc32C).Should().BeTrue();
    parsedUpperCase.GetHashCode().Should().Be(R2ChecksumAlgorithm.Crc32.GetHashCode());
  }

  [Fact]
  public void ToString_ReturnsTheWireName()
  {
    // Assert
    R2ChecksumAlgorithm.Md5.ToString().Should().Be("md5");
  }

  #endregion
}
