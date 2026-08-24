// Suppress obsolete warnings for tests that use deprecated methods for bucket management.
// The R2 client tests use the deprecated IAccountsApi methods for bucket lifecycle management.
#pragma warning disable CS0618

namespace Cloudflare.NET.R2.Tests.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using Accounts;
using Amazon.S3.Model;
using Fixtures;
using FluentAssertions;
using Helpers;
using Microsoft.Extensions.DependencyInjection;
using Models;
using NET.Tests.Shared.Fixtures;
using NET.Tests.Shared.Helpers;
using Xunit.Abstractions;

/// <summary>
///   Verifies how R2 treats the <c>x-amz-server-side-encryption</c> header when a caller signs it into a
///   presigned upload URL through the <c>HeadersToSign</c> member of the presigned request records.
/// </summary>
/// <remarks>
///   <para>
///     A consuming platform can sign this header so that the signature itself forces the browser or other
///     client to send it: a client that omits a signed header produces a different canonical request, so the
///     signature no longer matches and R2 refuses the upload. These tests confirm that R2 both accepts the
///     header on the upload and enforces its presence through the signature.
///   </para>
///   <para>
///     R2 encrypts every object at rest whether or not this header is sent, so the header does not change how
///     the object is stored. Its value here is that a client cannot upload without presenting it.
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class R2ServerSideEncryptionIntegrationTests : IClassFixture<R2ClientTestFixture>, IAsyncLifetime
{
  #region Constants

  /// <summary>The S3 header naming the server-side encryption algorithm to apply to an uploaded object.</summary>
  private const string ServerSideEncryptionHeader = "x-amz-server-side-encryption";

  /// <summary>The only server-side encryption algorithm value in play here: S3-managed AES-256 keys.</summary>
  private const string Aes256 = "AES256";

  #endregion


  #region Properties & Fields - Non-Public

  private readonly IR2Client         _sut;
  private readonly IAccountsApi      _accountsApi;
  private readonly ITestOutputHelper _output;
  private readonly string            _bucketName = $"cfnet-r2-sse-test-{Guid.NewGuid():N}";

  #endregion


  #region Constructors

  public R2ServerSideEncryptionIntegrationTests(R2ClientTestFixture fixture, ITestOutputHelper output)
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

  [IntegrationTest]
  public async Task PresignedPut_SigningTheEncryptionHeader_SucceedsWhenTheClientSendsIt()
  {
    // Arrange
    var key         = $"sse-put-ok-{Guid.NewGuid():N}.bin";
    var contentType = "application/octet-stream";
    var payload     = RandomPayload(512);

    var url = _sut.CreatePresignedPutUrl(_bucketName, new PresignedPutRequest(
                                           key,
                                           TimeSpan.FromMinutes(5),
                                           payload.Length,
                                           contentType,
                                           HeadersToSign: new Dictionary<string, string>
                                           {
                                             [ServerSideEncryptionHeader] = Aes256
                                           }));

    // Act - the client presents the header it was told to present.
    var response = await SendPutAsync(url, payload, contentType, new Dictionary<string, string>
    {
      [ServerSideEncryptionHeader] = Aes256
    });

    await ReportAsync("presigned PUT sending the encryption header", response);

    // Assert - R2 accepts the upload and the object is stored at the expected size.
    response.IsSuccessStatusCode.Should().BeTrue("R2 should accept an upload that presents the signed encryption header");

    var listing = await _sut.ListObjectsAsync(_bucketName, key);
    listing.Data.Should().ContainSingle().Which.Size.Should().Be(payload.Length);
  }

  [IntegrationTest]
  public async Task PresignedPut_SigningTheEncryptionHeader_IsRejectedWhenTheClientOmitsIt()
  {
    // Arrange - the URL is signed exactly as in the passing case, so the only difference below is what the
    // client sends.
    var key         = $"sse-put-missing-{Guid.NewGuid():N}.bin";
    var contentType = "application/octet-stream";
    var payload     = RandomPayload(512);

    var url = _sut.CreatePresignedPutUrl(_bucketName, new PresignedPutRequest(
                                           key,
                                           TimeSpan.FromMinutes(5),
                                           payload.Length,
                                           contentType,
                                           HeadersToSign: new Dictionary<string, string>
                                           {
                                             [ServerSideEncryptionHeader] = Aes256
                                           }));

    // Act - the client omits the signed header.
    var response = await SendPutAsync(url, payload, contentType, null);

    await ReportAsync("presigned PUT omitting the encryption header", response);

    // Assert - the canonical request no longer matches what was signed, so R2 refuses the upload and stores
    // nothing. This is what makes the header mandatory for the client rather than merely suggested.
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                                    "omitting a signed header changes the canonical request, so the signature no longer matches");

    var listing = await _sut.ListObjectsAsync(_bucketName, key);
    listing.Data.Should().BeEmpty("the rejected upload must not have stored an object");
  }

  [IntegrationTest]
  public async Task PresignedUploadPart_SigningTheEncryptionHeader_SucceedsWhenTheClientSendsIt()
  {
    // Arrange
    var key      = $"sse-part-ok-{Guid.NewGuid():N}.bin";
    var payload  = RandomPayload(1024);
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
                                                    HeadersToSign: new Dictionary<string, string>
                                                    {
                                                      [ServerSideEncryptionHeader] = Aes256
                                                    }));

      // Act
      var response = await SendPutAsync(url, payload, null, new Dictionary<string, string>
      {
        [ServerSideEncryptionHeader] = Aes256
      });

      await ReportAsync("presigned part upload sending the encryption header", response);

      // Assert - R2 accepts the part, and the part becomes visible to a listing of the upload's parts.
      response.IsSuccessStatusCode.Should().BeTrue("R2 should accept a part that presents the signed encryption header");

      var parts = await _sut.ListPartsAsync(_bucketName, key, uploadId);
      parts.Data.Should().ContainSingle().Which.PartNumber.Should().Be(1);
    }
    finally
    {
      await _sut.AbortMultipartUploadAsync(_bucketName, key, uploadId);
    }
  }

  [IntegrationTest]
  public async Task PresignedUploadPart_SigningTheEncryptionHeader_IsRejectedWhenTheClientOmitsIt()
  {
    // Arrange
    var key      = $"sse-part-missing-{Guid.NewGuid():N}.bin";
    var payload  = RandomPayload(1024);
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
                                                    HeadersToSign: new Dictionary<string, string>
                                                    {
                                                      [ServerSideEncryptionHeader] = Aes256
                                                    }));

      // Act - the client omits the signed header.
      var response = await SendPutAsync(url, payload, null, null);

      await ReportAsync("presigned part upload omitting the encryption header", response);

      // Assert - R2 refuses the part, and no part is recorded against the upload.
      response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                                      "omitting a signed header changes the canonical request, so the signature no longer matches");

      var parts = await _sut.ListPartsAsync(_bucketName, key, uploadId);
      parts.Data.Should().BeEmpty("the rejected part must not have been recorded against the upload");
    }
    finally
    {
      await _sut.AbortMultipartUploadAsync(_bucketName, key, uploadId);
    }
  }

  [IntegrationTest]
  public async Task MultipartUpload_WithEveryPartSigningTheEncryptionHeader_CompletesNormally()
  {
    // Arrange - two parts, because a single-part upload would not exercise assembly. Every part except the
    // last must be at least 5 MiB, so the first part is exactly the minimum and the second is small.
    var       key            = $"sse-multipart-{Guid.NewGuid():N}.bin";
    const int minimumPartSize = 5 * 1024 * 1024;
    var       firstPart      = RandomPayload(minimumPartSize);
    var       secondPart     = RandomPayload(2048);
    var       expectedSize   = firstPart.Length + secondPart.Length;

    var initiate = await _sut.InitiateMultipartUploadAsync(_bucketName, key, "application/pdf");
    var uploadId = initiate.Data;
    var succeeded = false;

    try
    {
      var partETags = new List<PartETag>();
      var payloads  = new[] { firstPart, secondPart };

      // Act - upload each part through its own presigned URL, every one signing the encryption header.
      for (var partNumber = 1; partNumber <= payloads.Length; partNumber++)
      {
        var payload = payloads[partNumber - 1];

        var url = _sut.CreatePresignedUploadPartUrl(_bucketName, new PresignedUploadPartRequest(
                                                      key,
                                                      uploadId,
                                                      partNumber,
                                                      TimeSpan.FromMinutes(10),
                                                      payload.Length,
                                                      "application/octet-stream",
                                                      HeadersToSign: new Dictionary<string, string>
                                                      {
                                                        [ServerSideEncryptionHeader] = Aes256
                                                      }));

        var response = await SendPutAsync(url, payload, null, new Dictionary<string, string>
        {
          [ServerSideEncryptionHeader] = Aes256
        });

        await ReportAsync($"multipart part {partNumber} sending the encryption header", response);
        response.IsSuccessStatusCode.Should().BeTrue($"part {partNumber} presents the signed encryption header");

        // The ETag the client must feed back into the complete call comes from the part upload response.
        var eTag = response.Headers.ETag?.Tag ?? throw new InvalidOperationException($"R2 returned no ETag for part {partNumber}.");
        partETags.Add(new PartETag(partNumber, eTag));
      }

      // Assemble the object from the parts that were uploaded this way.
      await _sut.CompleteMultipartUploadAsync(_bucketName, key, uploadId, partETags);
      succeeded = true;

      // Assert - the object exists at the combined size of both parts, so assembly was unaffected by the
      // encryption header on the individual part uploads.
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

  [IntegrationTest]
  public async Task MultipartUpload_ThroughPresignedPartUrlsWithoutTheEncryptionHeader_CompletesNormally()
  {
    // Arrange - the control for the test above. It runs the identical flow through presigned part URLs with
    // no extra signed header, so that a difference in outcome can only come from the encryption header
    // itself rather than from presigned part uploads in general.
    var       key             = $"control-multipart-{Guid.NewGuid():N}.bin";
    const int minimumPartSize = 5 * 1024 * 1024;
    var       firstPart       = RandomPayload(minimumPartSize);
    var       secondPart      = RandomPayload(2048);
    var       expectedSize    = firstPart.Length + secondPart.Length;

    var initiate  = await _sut.InitiateMultipartUploadAsync(_bucketName, key);
    var uploadId  = initiate.Data;
    var succeeded = false;

    try
    {
      var partETags = new List<PartETag>();
      var payloads  = new[] { firstPart, secondPart };

      // Act
      for (var partNumber = 1; partNumber <= payloads.Length; partNumber++)
      {
        var payload = payloads[partNumber - 1];

        var url = _sut.CreatePresignedUploadPartUrl(_bucketName, new PresignedUploadPartRequest(
                                                      key,
                                                      uploadId,
                                                      partNumber,
                                                      TimeSpan.FromMinutes(10),
                                                      payload.Length,
                                                      "application/octet-stream"));

        var response = await SendPutAsync(url, payload, null, null);

        await ReportAsync($"control multipart part {partNumber} with no extra signed header", response);
        response.IsSuccessStatusCode.Should().BeTrue($"part {partNumber} matches its signature");

        var eTag = response.Headers.ETag?.Tag ?? throw new InvalidOperationException($"R2 returned no ETag for part {partNumber}.");
        partETags.Add(new PartETag(partNumber, eTag));
      }

      // The listing of parts is checked before assembly, because a part that R2 accepted but did not record
      // would otherwise only surface as a confusing failure inside the complete call.
      var parts = await _sut.ListPartsAsync(_bucketName, key, uploadId);
      _output.WriteLine($"Parts R2 records against the upload: {parts.Data.Count}");
      parts.Data.Should().HaveCount(2, "R2 should record both parts that it answered with 200");

      await _sut.CompleteMultipartUploadAsync(_bucketName, key, uploadId, partETags);
      succeeded = true;

      // Assert
      var listing = await _sut.ListObjectsAsync(_bucketName, key);
      listing.Data.Should().ContainSingle().Which.Size.Should().Be(expectedSize);
    }
    finally
    {
      if (!succeeded)
        await _sut.AbortMultipartUploadAsync(_bucketName, key, uploadId);
    }
  }

  [IntegrationTest]
  public async Task MultipartUpload_ThroughBatchGeneratedPartUrls_RecordsEveryPartAndCompletes()
  {
    // Arrange - the batch URL generator signs every part in one call. It is covered separately from the
    // single-part generator because the two build their requests independently, so a mistake in one is
    // not caught by a test of the other.
    // Both parts are the minimum size, because this generator requires every part except the last to match
    // the first part's size and rejects any part below 5 MiB.
    var       key             = $"batch-multipart-{Guid.NewGuid():N}.bin";
    const int minimumPartSize = 5 * 1024 * 1024;
    var       payloads        = new Dictionary<int, byte[]>
    {
      [1] = RandomPayload(minimumPartSize),
      [2] = RandomPayload(minimumPartSize)
    };
    var expectedSize = payloads.Values.Sum(p => (long)p.Length);

    var initiate  = await _sut.InitiateMultipartUploadAsync(_bucketName, key);
    var uploadId  = initiate.Data;
    var succeeded = false;

    try
    {
      var urls = _sut.CreatePresignedUploadPartsUrls(_bucketName, new PresignedUploadPartsRequest(
                                                       key,
                                                       uploadId,
                                                       TimeSpan.FromMinutes(10),
                                                       payloads.ToDictionary(p => p.Key, p => (long)p.Value.Length)));

      urls.Should().HaveCount(2, "one URL per requested part");

      // Act
      var partETags = new List<PartETag>();

      foreach (var (partNumber, payload) in payloads.OrderBy(p => p.Key))
      {
        var response = await SendPutAsync(urls[partNumber], payload, null, null);

        await ReportAsync($"batch-generated part {partNumber}", response);
        response.IsSuccessStatusCode.Should().BeTrue($"part {partNumber} matches the signature of its URL");

        var eTag = response.Headers.ETag?.Tag ?? throw new InvalidOperationException($"R2 returned no ETag for part {partNumber}.");
        partETags.Add(new PartETag(partNumber, eTag));
      }

      // Checking the recorded parts before completing distinguishes "R2 rejected the part" from "R2
      // accepted the request but treated it as something other than a part upload", which is what a URL
      // carrying the wrong query parameter names produces.
      var parts = await _sut.ListPartsAsync(_bucketName, key, uploadId);
      _output.WriteLine($"Parts R2 records against the upload: {parts.Data.Count}");
      parts.Data.Should().HaveCount(2, "R2 should record both parts it answered with 200");

      await _sut.CompleteMultipartUploadAsync(_bucketName, key, uploadId, partETags);
      succeeded = true;

      // Assert
      var listing = await _sut.ListObjectsAsync(_bucketName, key);
      listing.Data.Should().ContainSingle().Which.Size.Should().Be(expectedSize);
    }
    finally
    {
      if (!succeeded)
        await _sut.AbortMultipartUploadAsync(_bucketName, key, uploadId);
    }
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

  /// <summary>
  ///   Sends a PUT to a presigned URL, optionally attaching extra request headers such as the server-side
  ///   encryption header.
  /// </summary>
  /// <param name="url">The presigned URL to send to.</param>
  /// <param name="payload">The bytes to upload.</param>
  /// <param name="contentType">The Content-Type to declare, or null to send none.</param>
  /// <param name="extraHeaders">Request headers to attach beyond the content headers, or null for none.</param>
  private static async Task<HttpResponseMessage> SendPutAsync(string                               url,
                                                              byte[]                               payload,
                                                              string?                              contentType,
                                                              IReadOnlyDictionary<string, string>? extraHeaders)
  {
    using var httpClient = new HttpClient();
    using var message    = new HttpRequestMessage(HttpMethod.Put, url);

    // ByteArrayContent sets Content-Length to the payload's own size, which is what the URL was signed for.
    var content = new ByteArrayContent(payload);

    if (contentType is not null)
      content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

    message.Content = content;

    // TryAddWithoutValidation is required because these are not headers HttpClient models natively.
    if (extraHeaders is not null)
      foreach (var header in extraHeaders)
        message.Headers.TryAddWithoutValidation(header.Key, header.Value);

    return await httpClient.SendAsync(message);
  }

  /// <summary>
  ///   Writes R2's status code, encryption response header and body to the test output, so that a failing run
  ///   records exactly what R2 did rather than only that an expectation was missed.
  /// </summary>
  /// <param name="what">A description of the request that produced this response.</param>
  /// <param name="response">The response R2 returned.</param>
  private async Task ReportAsync(string what, HttpResponseMessage response)
  {
    var body = await response.Content.ReadAsStringAsync();

    var echoed = response.Headers.TryGetValues(ServerSideEncryptionHeader, out var values)
      ? string.Join(", ", values)
      : "(absent)";

    _output.WriteLine($"R2 answered the {what} with {(int)response.StatusCode} {response.StatusCode}.");
    _output.WriteLine($"  {ServerSideEncryptionHeader} in the response: {echoed}");

    if (!string.IsNullOrWhiteSpace(body))
      _output.WriteLine($"  Body: {body}");
  }

  #endregion
}
