// Suppress obsolete warnings for tests that use deprecated methods for bucket management.
// The R2 client tests use the deprecated IAccountsApi methods for bucket lifecycle management.
#pragma warning disable CS0618

namespace Cloudflare.NET.R2.Tests.IntegrationTests;

using System.Net;
using System.Security.Cryptography;
using Accounts;
using Amazon.S3;
using Amazon.S3.Model;
using Exceptions;
using Fixtures;
using FluentAssertions;
using Helpers;
using Microsoft.Extensions.DependencyInjection;
using Models;
using NET.Tests.Shared.Fixtures;
using NET.Tests.Shared.Helpers;
using Xunit.Abstractions;

/// <summary>
///   Pins, against live R2, the checksum verification behavior that <see cref="R2ChecksumAlgorithm" /> encodes:
///   which algorithms R2 verifies on a presigned single-part PUT and on a presigned multipart part upload, and
///   that a wrong digest is rejected with 400 <c>BadDigest</c> while storing or recording nothing.
/// </summary>
/// <remarks>
///   <para>
///     These tests are the library's early warning if Cloudflare changes R2's checksum handling. The split they
///     pin: all five algorithms are verified on a single-part PUT; crc32, crc32c and md5 are verified on part
///     uploads. The remaining cell of the matrix, that R2 answers 501 <c>NotImplemented</c> to a part upload
///     carrying a SHA-1 or SHA-256 header, cannot be reached through the library because URL generation refuses
///     those combinations (covered by unit tests on <c>CreatePresignedUploadPartUrl</c>).
///   </para>
///   <para>
///     The CRC digests are computed by local bitwise implementations rather than a package, because the test
///     project takes no new dependencies. Their correctness is itself proven here: R2 accepts the digests they
///     produce and rejects deliberately wrong ones.
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class R2ChecksumIntegrationTests : IClassFixture<R2ClientTestFixture>, IAsyncLifetime
{
  #region Properties & Fields - Non-Public

  private readonly IR2Client         _sut;
  private readonly IAccountsApi      _accountsApi;
  private readonly ITestOutputHelper _output;
  private readonly string            _bucketName = $"cfnet-r2-checksum-test-{Guid.NewGuid():N}";

  #endregion


  #region Constructors

  public R2ChecksumIntegrationTests(R2ClientTestFixture fixture, ITestOutputHelper output)
  {
    _sut         = fixture.R2Client;
    _accountsApi = fixture.AccountsApi;
    _output      = output;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region Methods Impl

  public Task InitializeAsync()
  {
    return _accountsApi.CreateR2BucketAsync(_bucketName);
  }

  public async Task DisposeAsync()
  {
    // Best-effort cleanup.
    try
    {
      await _sut.ClearBucketAsync(_bucketName, true);
    }
    catch (Exception)
    {
      // ignore failures during cleanup
    }

    try
    {
      await _accountsApi.DeleteR2BucketAsync(_bucketName);
    }
    catch (Exception)
    {
      // ignore failures during cleanup
    }
  }

  #endregion


  #region Methods

  [IntegrationTestTheory]
  // Every algorithm the library admits must be verified by R2 on a single-part PUT: the upload succeeds when
  // the digest matches the bytes.
  [InlineData("crc32")]
  [InlineData("crc32c")]
  [InlineData("sha1")]
  [InlineData("sha256")]
  [InlineData("md5")]
  public async Task PresignedPut_WithCorrectChecksum_StoresTheObject(string algorithmName)
  {
    // Arrange
    R2ChecksumAlgorithm.TryParse(algorithmName, out var algorithm).Should().BeTrue();

    var key      = $"checksum-put-ok-{algorithmName}-{Guid.NewGuid():N}.bin";
    var payload  = RandomPayload(2048);
    var checksum = UploadChecksum.FromDigestBytes(algorithm, ComputeDigest(algorithm, payload));

    var url = _sut.CreatePresignedPutUrl(_bucketName, new PresignedPutRequest(
                                           key,
                                           TimeSpan.FromMinutes(5),
                                           payload.Length,
                                           "application/octet-stream",
                                           Checksum: checksum));

    // Act - the client sends the digest header the signature obliges it to send.
    var response = await SendPutAsync(url, payload, "application/octet-stream", checksum);

    await ReportAsync($"presigned PUT with a correct {algorithmName} digest", response);

    // Assert - R2 verified the digest against the bytes and stored the object.
    response.IsSuccessStatusCode.Should().BeTrue($"R2 verifies {algorithmName} on a single-part PUT and the digest matches");

    var listing = await _sut.ListObjectsAsync(_bucketName, key);
    listing.Data.Should().ContainSingle().Which.Size.Should().Be(payload.Length);
  }

  [IntegrationTestTheory]
  // The rejection half of the same matrix: a digest of the wrong bytes is refused with 400 BadDigest and
  // nothing is stored. This is what distinguishes a verified digest from a merely transported one.
  [InlineData("crc32")]
  [InlineData("crc32c")]
  [InlineData("sha1")]
  [InlineData("sha256")]
  [InlineData("md5")]
  public async Task PresignedPut_WithWrongChecksum_IsRejectedWithBadDigestAndStoresNothing(string algorithmName)
  {
    // Arrange - the digest is computed over a payload that differs from the one uploaded in its first byte,
    // so it is well-formed and correctly sized but wrong.
    R2ChecksumAlgorithm.TryParse(algorithmName, out var algorithm).Should().BeTrue();

    var key           = $"checksum-put-bad-{algorithmName}-{Guid.NewGuid():N}.bin";
    var payload       = RandomPayload(2048);
    var otherPayload  = (byte[])payload.Clone();
    otherPayload[0]  ^= 0xFF;
    var wrongChecksum = UploadChecksum.FromDigestBytes(algorithm, ComputeDigest(algorithm, otherPayload));

    var url = _sut.CreatePresignedPutUrl(_bucketName, new PresignedPutRequest(
                                           key,
                                           TimeSpan.FromMinutes(5),
                                           payload.Length,
                                           "application/octet-stream",
                                           Checksum: wrongChecksum));

    // Act - the client sends the signed digest header, but the bytes do not hash to it.
    var response = await SendPutAsync(url, payload, "application/octet-stream", wrongChecksum);

    await ReportAsync($"presigned PUT with a wrong {algorithmName} digest", response);

    // Assert - R2 rejects the upload with 400 BadDigest and stores nothing.
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"the {algorithmName} digest does not match the uploaded bytes");
    (await response.Content.ReadAsStringAsync()).Should().Contain("BadDigest");

    var listing = await _sut.ListObjectsAsync(_bucketName, key);
    listing.Data.Should().BeEmpty("the rejected upload must not have stored an object");
  }

  [IntegrationTestTheory]
  // The algorithms whose IsSupportedForPartUploads is true must be verified by R2 on a multipart part upload:
  // the part is accepted and recorded when the digest matches.
  [InlineData("crc32")]
  [InlineData("crc32c")]
  [InlineData("md5")]
  public async Task PresignedUploadPart_WithCorrectChecksum_RecordsThePart(string algorithmName)
  {
    // Arrange
    R2ChecksumAlgorithm.TryParse(algorithmName, out var algorithm).Should().BeTrue();

    var key      = $"checksum-part-ok-{algorithmName}-{Guid.NewGuid():N}.bin";
    var payload  = RandomPayload(1024);
    var checksum = UploadChecksum.FromDigestBytes(algorithm, ComputeDigest(algorithm, payload));
    var initiate = await _sut.InitiateMultipartUploadAsync(_bucketName, key);
    var uploadId = initiate.Data;

    try
    {
      var url = _sut.CreatePresignedUploadPartUrl(_bucketName, new PresignedUploadPartRequest(
                                                    key,
                                                    uploadId,
                                                    1,
                                                    TimeSpan.FromMinutes(5),
                                                    payload.Length,
                                                    "application/octet-stream",
                                                    Checksum: checksum));

      // Act
      var response = await SendPutAsync(url, payload, null, checksum);

      await ReportAsync($"part upload with a correct {algorithmName} digest", response);

      // Assert - R2 verified the digest and recorded the part against the upload.
      response.IsSuccessStatusCode.Should().BeTrue($"R2 verifies {algorithmName} on part uploads and the digest matches");

      var parts = await _sut.ListPartsAsync(_bucketName, key, uploadId);
      parts.Data.Should().ContainSingle().Which.PartNumber.Should().Be(1);
    }
    finally
    {
      await _sut.AbortMultipartUploadAsync(_bucketName, key, uploadId);
    }
  }

  [IntegrationTestTheory]
  // The rejection half for parts: a wrong digest is refused with 400 BadDigest and no part is recorded.
  [InlineData("crc32")]
  [InlineData("crc32c")]
  [InlineData("md5")]
  public async Task PresignedUploadPart_WithWrongChecksum_IsRejectedAndRecordsNoPart(string algorithmName)
  {
    // Arrange - as in the single-part rejection case, the digest is computed over a payload differing in one
    // byte from the one uploaded.
    R2ChecksumAlgorithm.TryParse(algorithmName, out var algorithm).Should().BeTrue();

    var key           = $"checksum-part-bad-{algorithmName}-{Guid.NewGuid():N}.bin";
    var payload       = RandomPayload(1024);
    var otherPayload  = (byte[])payload.Clone();
    otherPayload[0]  ^= 0xFF;
    var wrongChecksum = UploadChecksum.FromDigestBytes(algorithm, ComputeDigest(algorithm, otherPayload));
    var initiate      = await _sut.InitiateMultipartUploadAsync(_bucketName, key);
    var uploadId      = initiate.Data;

    try
    {
      var url = _sut.CreatePresignedUploadPartUrl(_bucketName, new PresignedUploadPartRequest(
                                                    key,
                                                    uploadId,
                                                    1,
                                                    TimeSpan.FromMinutes(5),
                                                    payload.Length,
                                                    "application/octet-stream",
                                                    Checksum: wrongChecksum));

      // Act
      var response = await SendPutAsync(url, payload, null, wrongChecksum);

      await ReportAsync($"part upload with a wrong {algorithmName} digest", response);

      // Assert - R2 rejects the part with 400 BadDigest and records nothing against the upload.
      response.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"the {algorithmName} digest does not match the uploaded bytes");
      (await response.Content.ReadAsStringAsync()).Should().Contain("BadDigest");

      var parts = await _sut.ListPartsAsync(_bucketName, key, uploadId);
      parts.Data.Should().BeEmpty("the rejected part must not have been recorded against the upload");
    }
    finally
    {
      await _sut.AbortMultipartUploadAsync(_bucketName, key, uploadId);
    }
  }

  [IntegrationTest]
  public async Task MultipartUpload_WithBatchGeneratedUrlsCarryingPerPartChecksums_RecordsEveryPartAndCompletes()
  {
    // Arrange - the end-to-end proof for the batch URL generator's per-part checksums: two parts, each URL
    // signing that part's own crc32 digest, uploaded and assembled into the final object. Both parts are the
    // minimum size because this generator requires every part except the last to match the first part's size.
    var       key             = $"checksum-batch-{Guid.NewGuid():N}.bin";
    const int minimumPartSize = 5 * 1024 * 1024;
    var       payloads        = new Dictionary<int, byte[]>
    {
      [1] = RandomPayload(minimumPartSize),
      [2] = RandomPayload(minimumPartSize)
    };
    var expectedSize = payloads.Values.Sum(p => (long)p.Length);

    var checksums = payloads.ToDictionary(
      p => p.Key,
      p => UploadChecksum.FromDigestBytes(R2ChecksumAlgorithm.Crc32, ComputeDigest(R2ChecksumAlgorithm.Crc32, p.Value)));

    var initiate  = await _sut.InitiateMultipartUploadAsync(_bucketName, key);
    var uploadId  = initiate.Data;
    var succeeded = false;

    try
    {
      var urls = _sut.CreatePresignedUploadPartsUrls(_bucketName, new PresignedUploadPartsRequest(
                                                       key,
                                                       uploadId,
                                                       TimeSpan.FromMinutes(10),
                                                       payloads.ToDictionary(p => p.Key, p => (long)p.Value.Length),
                                                       ChecksumsByPartNumber: checksums));

      urls.Should().HaveCount(2, "one URL per requested part");

      // Act - each part is sent with its own digest header, as its URL's signature obliges.
      var partETags = new List<PartETag>();

      foreach (var (partNumber, payload) in payloads.OrderBy(p => p.Key))
      {
        var response = await SendPutAsync(urls[partNumber], payload, null, checksums[partNumber]);

        await ReportAsync($"batch-generated part {partNumber} with its own crc32 digest", response);
        response.IsSuccessStatusCode.Should().BeTrue($"part {partNumber}'s digest matches its bytes");

        // The ETag the client must feed back into the complete call comes from the part upload response.
        var eTag = response.Headers.ETag?.Tag ?? throw new InvalidOperationException($"R2 returned no ETag for part {partNumber}.");
        partETags.Add(new PartETag(partNumber, eTag));
      }

      var parts = await _sut.ListPartsAsync(_bucketName, key, uploadId);
      parts.Data.Should().HaveCount(2, "R2 should record both parts it answered with 200");

      await _sut.CompleteMultipartUploadAsync(_bucketName, key, uploadId, partETags);
      succeeded = true;

      // Assert - the object exists at the combined size, so per-part digest verification did not disturb
      // assembly.
      var listing = await _sut.ListObjectsAsync(_bucketName, key);
      listing.Data.Should().ContainSingle().Which.Size.Should().Be(expectedSize);
    }
    finally
    {
      // A completed upload no longer exists, so only abort when the assembly did not happen.
      if (!succeeded)
        await _sut.AbortMultipartUploadAsync(_bucketName, key, uploadId);
    }
  }

  [IntegrationTestTheory]
  // The direct server-side upload path: the client itself sends the bytes and copies the caller's digest
  // onto the matching PutObjectRequest property. Every admitted algorithm must be verified by R2 there
  // exactly as on a presigned PUT.
  [InlineData("crc32")]
  [InlineData("crc32c")]
  [InlineData("sha1")]
  [InlineData("sha256")]
  [InlineData("md5")]
  public async Task DirectUpload_WithCorrectChecksum_StoresTheObject(string algorithmName)
  {
    // Arrange
    R2ChecksumAlgorithm.TryParse(algorithmName, out var algorithm).Should().BeTrue();

    var       key      = $"direct-checksum-ok-{algorithmName}-{Guid.NewGuid():N}.bin";
    var       payload  = RandomPayload(2048);
    var       checksum = UploadChecksum.FromDigestBytes(algorithm, ComputeDigest(algorithm, payload));
    using var stream   = new MemoryStream(payload);

    // Act
    await _sut.UploadSinglePartAsync(_bucketName, key, stream, "application/octet-stream", checksum);

    // Assert - R2 verified the digest against the bytes and stored the object.
    var listing = await _sut.ListObjectsAsync(_bucketName, key);
    listing.Data.Should().ContainSingle().Which.Size.Should().Be(payload.Length);
  }

  [IntegrationTestTheory]
  // The rejection half: a digest of the wrong bytes fails the upload with BadDigest and nothing is
  // stored. The library surfaces the failure as its usual single-part upload exception.
  [InlineData("crc32")]
  [InlineData("crc32c")]
  [InlineData("sha1")]
  [InlineData("sha256")]
  [InlineData("md5")]
  public async Task DirectUpload_WithWrongChecksum_ThrowsAndStoresNothing(string algorithmName)
  {
    // Arrange - the digest is computed over a payload that differs from the one uploaded in its first
    // byte, so it is well-formed and correctly sized but wrong.
    R2ChecksumAlgorithm.TryParse(algorithmName, out var algorithm).Should().BeTrue();

    var key           = $"direct-checksum-bad-{algorithmName}-{Guid.NewGuid():N}.bin";
    var payload       = RandomPayload(2048);
    var otherPayload  = (byte[])payload.Clone();
    otherPayload[0]  ^= 0xFF;
    var wrongChecksum = UploadChecksum.FromDigestBytes(algorithm, ComputeDigest(algorithm, otherPayload));

    using var stream = new MemoryStream(payload);

    // Act
    var action = () => _sut.UploadSinglePartAsync(_bucketName, key, stream, "application/octet-stream", wrongChecksum);

    // Assert - R2 answers 400 BadDigest; the library wraps it in its single-part upload exception.
    var ex = await action.Should().ThrowAsync<CloudflareR2OperationException>();
    ex.Which.InnerException.Should().BeOfType<AmazonS3Exception>()
      .Which.ErrorCode.Should().Be("BadDigest", $"the {algorithmName} digest does not match the uploaded bytes");

    var listing = await _sut.ListObjectsAsync(_bucketName, key);
    listing.Data.Should().BeEmpty("the rejected upload must not have stored an object");
  }

  #endregion


  #region Methods - Non-Public

  /// <summary>Builds a byte payload of the requested size filled with random data.</summary>
  /// <param name="size">The number of bytes to generate.</param>
  private static byte[] RandomPayload(int size)
  {
    var payload = new byte[size];
    Random.Shared.NextBytes(payload);

    return payload;
  }

  /// <summary>Computes the raw digest of a payload under one of the admitted algorithms.</summary>
  /// <param name="algorithm">The algorithm to hash with.</param>
  /// <param name="payload">The bytes to hash.</param>
  /// <returns>The raw digest bytes, of the algorithm's exact digest length.</returns>
  private static byte[] ComputeDigest(R2ChecksumAlgorithm algorithm, byte[] payload)
  {
    if (algorithm == R2ChecksumAlgorithm.Sha1)
      return SHA1.HashData(payload);

    if (algorithm == R2ChecksumAlgorithm.Sha256)
      return SHA256.HashData(payload);

    if (algorithm == R2ChecksumAlgorithm.Md5)
      return MD5.HashData(payload);

    if (algorithm == R2ChecksumAlgorithm.Crc32)
      return Crc32BigEndian(payload, 0xEDB88320u);

    if (algorithm == R2ChecksumAlgorithm.Crc32C)
      return Crc32BigEndian(payload, 0x82F63B78u);

    throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm.Value, "No digest implementation for this algorithm.");
  }

  /// <summary>
  ///   Computes a 32-bit reflected CRC and returns it as the 4 big-endian bytes the S3 checksum headers carry.
  ///   The IEEE polynomial (0xEDB88320 reflected) yields crc32; the Castagnoli polynomial (0x82F63B78
  ///   reflected) yields crc32c.
  /// </summary>
  /// <param name="payload">The bytes to compute the CRC over.</param>
  /// <param name="reflectedPolynomial">The reflected form of the CRC polynomial.</param>
  private static byte[] Crc32BigEndian(byte[] payload, uint reflectedPolynomial)
  {
    // Standard reflected CRC: initial value all ones, one bit at a time, final complement.
    var crc = 0xFFFFFFFFu;

    foreach (var currentByte in payload)
    {
      crc ^= currentByte;

      for (var bit = 0; bit < 8; bit++)
        crc = (crc & 1) != 0 ? (crc >> 1) ^ reflectedPolynomial : crc >> 1;
    }

    crc = ~crc;

    // The x-amz-checksum-* headers carry the digest big-endian, most significant byte first.
    return [(byte)(crc >> 24), (byte)(crc >> 16), (byte)(crc >> 8), (byte)crc];
  }

  /// <summary>
  ///   Sends a PUT to a presigned URL carrying a digest header. <c>Content-MD5</c> is a content header in
  ///   HttpClient's model and must be attached to the content, while the <c>x-amz-checksum-*</c> headers are
  ///   plain request headers.
  /// </summary>
  /// <param name="url">The presigned URL to send to.</param>
  /// <param name="payload">The bytes to upload.</param>
  /// <param name="contentType">The Content-Type to declare, or null to send none.</param>
  /// <param name="checksum">The digest to present in the algorithm's header.</param>
  private static async Task<HttpResponseMessage> SendPutAsync(string         url,
                                                              byte[]         payload,
                                                              string?        contentType,
                                                              UploadChecksum checksum)
  {
    using var httpClient = new HttpClient();
    using var message    = new HttpRequestMessage(HttpMethod.Put, url);

    // ByteArrayContent sets Content-Length to the payload's own size, which is what the URL was signed for.
    var content = new ByteArrayContent(payload);

    if (contentType is not null)
      content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

    message.Content = content;

    // TryAddWithoutValidation is required because these are not headers HttpClient models natively; the
    // content headers collection owns Content-MD5 and refuses it on the request headers collection.
    if (checksum.Algorithm == R2ChecksumAlgorithm.Md5)
      content.Headers.TryAddWithoutValidation(checksum.Algorithm.HeaderName, checksum.Base64Digest);
    else
      message.Headers.TryAddWithoutValidation(checksum.Algorithm.HeaderName, checksum.Base64Digest);

    return await httpClient.SendAsync(message);
  }

  /// <summary>
  ///   Writes R2's status code and body to the test output, so that a failing run records exactly what R2 did
  ///   rather than only that an expectation was missed.
  /// </summary>
  /// <param name="what">A description of the request that produced this response.</param>
  /// <param name="response">The response R2 returned.</param>
  private async Task ReportAsync(string what, HttpResponseMessage response)
  {
    var body = await response.Content.ReadAsStringAsync();

    _output.WriteLine($"R2 answered the {what} with {(int)response.StatusCode} {response.StatusCode}.");

    if (!string.IsNullOrWhiteSpace(body))
      _output.WriteLine($"  Body: {body}");
  }

  #endregion
}
