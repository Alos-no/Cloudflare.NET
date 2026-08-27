namespace Cloudflare.NET.R2;

using Amazon.S3;
using Amazon.S3.Model;
using Exceptions;
using Microsoft.Extensions.Logging;
using Models;

/// <summary>Implements the client that interacts with Cloudflare R2's S3-compatible API.</summary>
public class R2Client : IR2Client, IDisposable
{
  #region Properties & Fields - Non-Public

  private readonly ILogger<R2Client> _logger;
  private readonly IAmazonS3         _s3Client;

  #endregion

  #region Constants & Statics

  /// <summary>Use multipart for uploads above 50MB.</summary>
  internal const long R2MutlipartFileSizeThreshold = 50L * 1024 * 1024;
  /// <summary>Cloudflare R2 has a maximum file size of 5 GiB for a single PutObject operation.</summary>
  internal const long R2MaxSinglePartUploadSize = 5L * 1024 * 1024 * 1024;
  /// <summary>Cloudflare R2 has a maximum total object size of 5 TiB for multipart uploads.</summary>
  internal const long R2MaxMultipartFileSize = 5L * 1024 * 1024 * 1024 * 1024;
  /// <summary>Cloudflare R2 has a minimum part size of 5 MiB for multipart uploads.</summary>
  internal const long R2MinPartSize = 5L * 1024 * 1024;
  /// <summary>Cloudflare R2 has a maximum part size of 5 GiB.</summary>
  internal const long R2MaxPartSize = 5L * 1024 * 1024 * 1024;
  /// <summary>The default chunk size for multipart uploads if not specified by the user (50 MiB).</summary>
  internal const long DefaultPartSize = 50L * 1024 * 1024;
  /// <summary>The maximum number of keys allowed in a single DeleteObjects request.</summary>
  internal const int MaxKeysPerDelete = 1000;
  /// <summary>The maximum number of keys S3 returns in a single list page.</summary>
  internal const int MaxKeysPerListPage = 1000;
  /// <summary>The maximum number of parts allowed in a single multipart upload.</summary>
  internal const int MaxPartsPerUpload = 10000;

  #endregion

  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="R2Client" /> class. This is the designated constructor for
  ///   dependency injection.
  /// </summary>
  /// <param name="loggerFactory">The logger factory used to create a typed logger.</param>
  /// <param name="s3Client">The underlying S3-compatible client.</param>
  public R2Client(ILoggerFactory loggerFactory, IAmazonS3 s3Client)
  {
    _logger   = loggerFactory.CreateLogger<R2Client>();
    _s3Client = s3Client;
  }

  /// <summary>Disposes the underlying S3 client.</summary>
  public void Dispose()
  {
    _s3Client.Dispose();
    GC.SuppressFinalize(this);
  }

  #endregion

  #region Methods Impl

  /// <inheritdoc />
  public Task<R2Result> UploadAsync(string            bucketName,
                                    string            objectKey,
                                    string            filePath,
                                    long?             partSize          = null,
                                    CancellationToken cancellationToken = default)
    => UploadAsync(bucketName, objectKey, filePath, partSize, null, null, null, cancellationToken);

  /// <inheritdoc />
  public Task<R2Result> UploadAsync(string            bucketName,
                                    string            objectKey,
                                    string            filePath,
                                    long?             partSize,
                                    string?           contentType,
                                    UploadChecksum?   checksum          = null,
                                    string?           cacheControl      = null,
                                    CancellationToken cancellationToken = default)
  {
    var fileInfo = new FileInfo(filePath);

    if (fileInfo.Length > R2MaxMultipartFileSize)
      throw new ArgumentException($"File size ({fileInfo.Length} bytes) exceeds the maximum R2 object size of 5 TiB.",
                                  nameof(filePath));

    if (fileInfo.Length < R2MutlipartFileSizeThreshold)
      return UploadSinglePartAsync(bucketName, objectKey, filePath, contentType, checksum, cacheControl, cancellationToken);

    // A checksum digests the whole object, but a multipart upload is verified per part and this client
    // does not compute per-part digests, so the caller's digest could never be checked on this path.
    ThrowIfChecksumOnMultipartPath(checksum);

    return UploadMultipartAsync(bucketName, objectKey, filePath, partSize, contentType, cacheControl, cancellationToken);
  }

  /// <inheritdoc />
  public Task<R2Result> UploadAsync(string            bucketName,
                                    string            objectKey,
                                    Stream            fileStream,
                                    long?             partSize          = null,
                                    CancellationToken cancellationToken = default)
    => UploadAsync(bucketName, objectKey, fileStream, partSize, null, null, null, cancellationToken);

  /// <inheritdoc />
  public Task<R2Result> UploadAsync(string            bucketName,
                                    string            objectKey,
                                    Stream            fileStream,
                                    long?             partSize,
                                    string?           contentType,
                                    UploadChecksum?   checksum          = null,
                                    string?           cacheControl      = null,
                                    CancellationToken cancellationToken = default)
  {
    if (fileStream is { CanSeek: true, Length: > R2MaxMultipartFileSize })
      throw new ArgumentException($"Stream length ({fileStream.Length} bytes) exceeds the maximum R2 object size of 5 TiB.",
                                  nameof(fileStream));

    if (fileStream is { CanSeek: true, Length: < R2MutlipartFileSizeThreshold })
      return UploadSinglePartAsync(bucketName, objectKey, fileStream, contentType, checksum, cacheControl, cancellationToken);

    // If we can't determine the length or it's large, use multipart.
    // A checksum digests the whole object, but a multipart upload is verified per part and this client
    // does not compute per-part digests, so the caller's digest could never be checked on this path.
    ThrowIfChecksumOnMultipartPath(checksum);

    return UploadMultipartAsync(bucketName, objectKey, fileStream, partSize, contentType, cacheControl, cancellationToken);
  }

  /// <inheritdoc />
  public Task<R2Result> UploadSinglePartAsync(string            bucketName,
                                              string            objectKey,
                                              string            filePath,
                                              CancellationToken cancellationToken = default)
    => UploadSinglePartAsync(bucketName, objectKey, filePath, null, null, null, cancellationToken);

  /// <inheritdoc />
  public async Task<R2Result> UploadSinglePartAsync(string            bucketName,
                                                    string            objectKey,
                                                    string            filePath,
                                                    string?           contentType,
                                                    UploadChecksum?   checksum          = null,
                                                    string?           cacheControl      = null,
                                                    CancellationToken cancellationToken = default)
  {
    await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

    return await UploadSinglePartAsync(bucketName, objectKey, fileStream, contentType, checksum, cacheControl, cancellationToken);
  }

  /// <inheritdoc />
  public Task<R2Result> UploadSinglePartAsync(string            bucketName,
                                              string            objectKey,
                                              Stream            inputStream,
                                              CancellationToken cancellationToken = default)
    => UploadSinglePartAsync(bucketName, objectKey, inputStream, null, null, null, cancellationToken);

  /// <inheritdoc />
  public async Task<R2Result> UploadSinglePartAsync(string            bucketName,
                                                    string            objectKey,
                                                    Stream            inputStream,
                                                    string?           contentType,
                                                    UploadChecksum?   checksum          = null,
                                                    string?           cacheControl      = null,
                                                    CancellationToken cancellationToken = default)
  {
    // Pre-flight check for single-part upload size limit if the stream is seekable.
    if (inputStream.CanSeek && inputStream.Length > R2MaxSinglePartUploadSize)
      throw new ArgumentException(
        $"Stream length ({inputStream.Length} bytes) exceeds the maximum size for a single-part upload (5 GiB).",
        nameof(inputStream));

    // Assume 1 Class A op for the attempt.
    var metrics = new R2Result(1);

    // Capture the length before the stream is consumed by the S3 client.
    var ingressBytes = inputStream.CanSeek ? inputStream.Length : -1; // -1 indicates unknown size

    try
    {
      var request = new PutObjectRequest
      {
        BucketName  = bucketName,
        Key         = objectKey,
        InputStream = inputStream,
        // R2 requires payload signing to be disabled.
        DisablePayloadSigning = true,
        // R2 also requires the default SDK checksum validation to be disabled to prevent signature mismatch errors.
        DisableDefaultChecksumValidation = true
      };

      // A blank content type is treated as absent, so R2 applies its own default; the value is never
      // inferred from the key or the bytes.
      if (!string.IsNullOrWhiteSpace(contentType))
        request.ContentType = contentType;

      // A blank Cache-Control is treated as absent the same way. R2 persists the stored value and serves
      // it on every GET, which is what drives edge and browser caching.
      if (!string.IsNullOrWhiteSpace(cacheControl))
        request.Headers.CacheControl = cacheControl;

      // The caller's digest goes on the request property matching its algorithm; the SDK sends it as a
      // plain header and R2 fails the upload with BadDigest, storing nothing, when the bytes do not hash
      // to it (verified against live R2 for all five algorithms, 2026-08-25).
      if (checksum is not null)
        ApplyChecksum(request, checksum);

      await _s3Client.PutObjectAsync(request, cancellationToken);
      _logger.UploadedSinglePart(bucketName, objectKey);

      return new R2Result(1, IngressBytes: ingressBytes);
    }
    catch (AmazonS3Exception ex)
    {
      _logger.UploadSinglePartFailed(ex, bucketName, objectKey);
      // If the stream is seekable, we can determine how many bytes were transferred before the error.
      if (inputStream.CanSeek)
        metrics = metrics with { IngressBytes = inputStream.Position };

      throw new CloudflareR2OperationException($"Single-part upload failed for s3://{bucketName}/{objectKey}", metrics, ex);
    }
  }

  /// <inheritdoc />
  public Task<R2Result> UploadMultipartAsync(string            bucketName,
                                             string            objectKey,
                                             string            filePath,
                                             long?             partSize          = null,
                                             CancellationToken cancellationToken = default)
    => UploadMultipartAsync(bucketName, objectKey, filePath, partSize, null, null, cancellationToken);

  /// <inheritdoc />
  public async Task<R2Result> UploadMultipartAsync(string            bucketName,
                                                   string            objectKey,
                                                   string            filePath,
                                                   long?             partSize,
                                                   string?           contentType,
                                                   string?           cacheControl      = null,
                                                   CancellationToken cancellationToken = default)
  {
    await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

    return await UploadMultipartAsync(bucketName, objectKey, fileStream, partSize, contentType, cacheControl, cancellationToken);
  }

  /// <inheritdoc />
  public Task<R2Result> UploadMultipartAsync(string            bucketName,
                                             string            objectKey,
                                             Stream            inputStream,
                                             long?             partSize          = null,
                                             CancellationToken cancellationToken = default)
    => UploadMultipartAsync(bucketName, objectKey, inputStream, partSize, null, null, cancellationToken);

  /// <inheritdoc />
  public async Task<R2Result> UploadMultipartAsync(string            bucketName,
                                                   string            objectKey,
                                                   Stream            inputStream,
                                                   long?             partSize,
                                                   string?           contentType,
                                                   string?           cacheControl      = null,
                                                   CancellationToken cancellationToken = default)
  {
    // Pre-flight check for total object size limit. This is the definitive check for multipart.
    if (inputStream.CanSeek && inputStream.Length > R2MaxMultipartFileSize)
      throw new ArgumentException($"Stream length ({inputStream.Length} bytes) exceeds the maximum R2 object size of 5 TiB.",
                                  nameof(inputStream));

    if (!inputStream.CanSeek)
      // For a non-seekable stream, we would need to read it into chunks in memory or a temporary file.
      // This is complex and memory-intensive, so we'll consider it out of scope for this implementation.
      throw new NotSupportedException(
        "Multipart upload from a non-seekable stream is not currently supported. The stream must support seeking to determine its length and to be read in parts.");

    var fileSize = inputStream.Length;

    // Clamp the user-provided part size between the allowed R2 limits.
    var chunkSize = Math.Clamp(partSize.GetValueOrDefault(DefaultPartSize), R2MinPartSize, R2MaxPartSize);

    // Pre-flight check for part count before initiating the upload.
    var numParts = (long)Math.Ceiling((double)fileSize / chunkSize);

    if (numParts > MaxPartsPerUpload)
      throw new ArgumentException(
        $"The calculated number of parts ({numParts}) exceeds the R2 maximum of {MaxPartsPerUpload} parts. Consider increasing the part size.",
        nameof(partSize));

    var totalMetrics = new R2Result();

    // 1. Initiate (1 Class A op). The content type and Cache-Control travel on the initiate request, the
    // only place S3 reads the assembled object's headers from; the initiate overload treats a blank value
    // as absent.
    var initResult = await InitiateMultipartUploadAsync(bucketName, objectKey, contentType, cacheControl, cancellationToken);

    totalMetrics += initResult.Metrics;
    var uploadId = initResult.Data;

    var  uploadParts  = new List<PartETag>();
    long filePosition = 0;

    try
    {
      for (var i = 1; filePosition < fileSize; i++)
      {
        cancellationToken.ThrowIfCancellationRequested();

        // The last part can be smaller than the chunk size.
        var currentPartSize = Math.Min(chunkSize, fileSize - filePosition);

        // For a seekable stream, we set the position for each part.
        inputStream.Position = filePosition;

        var uploadRequest = new UploadPartRequest
        {
          BucketName  = bucketName,
          Key         = objectKey,
          UploadId    = uploadId,
          PartNumber  = i,
          PartSize    = currentPartSize,
          InputStream = inputStream,
          // R2 requires payload signing to be disabled for each part.
          DisablePayloadSigning = true,
          // R2 also requires the default SDK checksum validation to be disabled to prevent signature mismatch errors.
          DisableDefaultChecksumValidation = true
        };

        // 2. Upload part (1 Class A op per part)
        // We must account for the Class A operation *before* the call, so the attempt is
        // always counted, even on failure.
        totalMetrics += new R2Result(1);
        var partResponse = await _s3Client.UploadPartAsync(uploadRequest, cancellationToken);

        uploadParts.Add(new PartETag(partResponse.PartNumber!.Value, partResponse.ETag));

        // If the upload succeeds, we add the ingress bytes.
        totalMetrics += new R2Result(IngressBytes: currentPartSize);
        filePosition += currentPartSize;
      }

      // 3. Complete (1 Class A op)
      var completeResult = await CompleteMultipartUploadAsync(bucketName, objectKey, uploadId, uploadParts, cancellationToken);

      totalMetrics += completeResult;

      _logger.UploadedMultipart(bucketName, objectKey);

      return totalMetrics with { IngressBytes = fileSize };
    }
    catch (Exception ex)
    {
      _logger.MultipartFailed(ex, bucketName, objectKey);

      // If the stream is seekable, we can calculate how many bytes of the *failing* part were transferred.
      // `filePosition` holds the total size of successfully uploaded parts.
      // `inputStream.Position` is where the stream reader stopped on error.
      if (inputStream.CanSeek)
        totalMetrics += new R2Result(IngressBytes: inputStream.Position - filePosition);

      // 4. Abort on failure (Free operation, but we still perform it)
      totalMetrics += await AbortMultipartUploadAsync(bucketName, objectKey, uploadId, CancellationToken.None);

      throw new CloudflareR2OperationException($"Multipart upload failed for s3://{bucketName}/{objectKey} and was aborted.",
                                               totalMetrics, ex);
    }
  }

  /// <inheritdoc />
  public async Task<R2Result> DownloadFileAsync(string            bucketName,
                                                string            objectKey,
                                                string            downloadPath,
                                                CancellationToken cancellationToken = default)
  {
    await using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write);

    return await DownloadFileAsync(bucketName, objectKey, fileStream, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<R2Result> DownloadFileAsync(string            bucketName,
                                                string            objectKey,
                                                Stream            outputStream,
                                                CancellationToken cancellationToken = default)
  {
    // Assume 1 Class B op for the attempt.
    var metrics = new R2Result(ClassBOperations: 1);

    try
    {
      var request = new GetObjectRequest { BucketName = bucketName, Key = objectKey };

      // GetObject is a Class B operation.
      using var response = await _s3Client.GetObjectAsync(request, cancellationToken);

      await response.ResponseStream.CopyToAsync(outputStream, cancellationToken);

      var fileSize = response.ContentLength;
      _logger.DownloadedObject(bucketName, objectKey);

      return new R2Result(ClassBOperations: 1, EgressBytes: fileSize);
    }
    catch (AmazonS3Exception ex)
    {
      _logger.DownloadFailed(ex, bucketName, objectKey);
      // If the output stream is seekable, we can determine how many bytes were written before the error.
      if (outputStream.CanSeek)
        metrics = metrics with { EgressBytes = outputStream.Position };

      throw new CloudflareR2OperationException($"Download failed for s3://{bucketName}/{objectKey}", metrics, ex);
    }
  }

  /// <inheritdoc />
  public async Task<R2Result> DeleteObjectAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
  {
    // According to documentation, DeleteObject is a free operation.
    var metrics = new R2Result();

    try
    {
      var request = new DeleteObjectRequest { BucketName = bucketName, Key = objectKey };

      // DeleteObject is a Class A operation.
      await _s3Client.DeleteObjectAsync(request, cancellationToken);
      _logger.DeletedObject(bucketName, objectKey);

      return metrics;
    }
    catch (AmazonS3Exception ex)
    {
      _logger.DeleteFailed(ex, bucketName, objectKey);
      throw new CloudflareR2OperationException($"Delete failed for s3://{bucketName}/{objectKey}", metrics, ex);
    }
  }

  /// <inheritdoc />
  public async Task<R2Result> DeleteObjectsAsync(string              bucketName,
                                                 IEnumerable<string> objectKeys,
                                                 bool                continueOnError   = true,
                                                 CancellationToken   cancellationToken = default)
  {
    var totalMetrics = new R2Result();
    var failedKeys   = new List<string>();
    var exceptions   = new List<Exception>();
    var keysToDelete = objectKeys.ToList();

    if (keysToDelete.Count == 0)
      return totalMetrics;

    // The DeleteObjects API can handle a maximum of 1000 keys per request.
    for (var i = 0; i < keysToDelete.Count; i += MaxKeysPerDelete)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var batch     = keysToDelete.Skip(i).Take(MaxKeysPerDelete).ToList();
      var batchKeys = batch.Select(key => new KeyVersion { Key = key }).ToList();
      var deleteRequest = new DeleteObjectsRequest
      {
        BucketName = bucketName,
        Objects    = batchKeys
      };

      try
      {
        // DeleteObjects is free.
        totalMetrics += new R2Result();

        var response = await _s3Client.DeleteObjectsAsync(deleteRequest, cancellationToken);

        // The AWS SDK can, in fact, return a null list if there are no errors. This check handles that case.
        if (response.DeleteErrors is not null && response.DeleteErrors.Count > 0)
        {
          var batchFailedKeys = response.DeleteErrors.Select(e => e.Key).ToList();

          failedKeys.AddRange(batchFailedKeys);

          // Create an exception for each error reported by the API for aggregation.
          foreach (var error in response.DeleteErrors)
            exceptions.Add(new AmazonS3Exception($"Failed to delete key '{error.Key}': Code={error.Code}, Message={error.Message}"));

          if (!continueOnError)
            throw new CloudflareR2BatchException<string>(
              $"Batch delete failed for {batchFailedKeys.Count} keys and continueOnError is false.",
              failedKeys, totalMetrics, new AggregateException(exceptions));
        }
      }
      catch (AmazonS3Exception ex)
      {
        exceptions.Add(ex);
        if (continueOnError)
        {
          _logger.BatchDeleteFailedContinueOnError(ex, bucketName);
          failedKeys.AddRange(batch);
        }
        else
        {
          _logger.BatchDeleteFailedStopOnError(ex, bucketName);
          throw new CloudflareR2BatchException<string>(
            "A batch delete API call failed and continueOnError is false.",
            batch, totalMetrics, new AggregateException(exceptions));
        }
      }
    }

    if (failedKeys.Count > 0)
      throw new CloudflareR2BatchException<string>(
        $"{failedKeys.Count} objects failed to delete from bucket {bucketName}.",
        failedKeys.Distinct().ToList(), // Ensure unique keys
        totalMetrics, new AggregateException(exceptions));

    _logger.DeletedMultipleObjects(keysToDelete.Count, bucketName);
    return totalMetrics;
  }

  /// <inheritdoc />
  public async Task<R2Result> ClearBucketAsync(string            bucketName,
                                               bool              continueOnError                = true,
                                               bool              abortIncompleteMultipartUploads = true,
                                               CancellationToken cancellationToken              = default)
  {
    _logger.ClearingBucket(bucketName);
    var  totalMetrics      = new R2Result();
    var  allFailedKeys     = new List<string>();
    var  allExceptions     = new List<Exception>();
    var  continuationToken = (string?)null;
    bool isTruncated;

    do
    {
      cancellationToken.ThrowIfCancellationRequested();

      IReadOnlyList<S3Object> objectsInPage;

      try
      {
        // A ListObjectsV2 call is one Class A operation. Account for it before the call.
        totalMetrics += new R2Result(1);

        var listRequest  = new ListObjectsV2Request { BucketName = bucketName, ContinuationToken = continuationToken };
        var listResponse = await _s3Client.ListObjectsV2Async(listRequest, cancellationToken);

        // The S3Objects list can be null if the response contains no objects.
        objectsInPage = listResponse.S3Objects ?? [];
        isTruncated   = listResponse.IsTruncated == true; // IsTruncated is type `bool?`
        // The continuation token is safe to use even when deleting objects from the current
        // page. The token marks a point in the key-sorted index, so the next request will
        // correctly start after the last key of this page, regardless of deletions.
        continuationToken = listResponse.NextContinuationToken;
      }
      catch (AmazonS3Exception ex)
      {
        _logger.ClearBucketListFailed(ex, bucketName);
        throw new CloudflareR2ListException<string>(
          "Listing objects failed during bucket clear operation.",
          allFailedKeys, totalMetrics, new AggregateException(allExceptions.Append(ex)));
      }

      if (objectsInPage.Count > 0)
        try
        {
          var keysToDelete = objectsInPage.Select(o => o.Key).ToList();

          totalMetrics += await DeleteObjectsAsync(bucketName, keysToDelete, continueOnError,
                                                   cancellationToken);
        }
        catch (CloudflareR2BatchException<string> ex)
        {
          totalMetrics += ex.PartialMetrics;

          allFailedKeys.AddRange(ex.FailedItems);

          if (ex.InnerException is not null)
            allExceptions.Add(ex.InnerException);

          // Infinite loop prevention: if an entire batch of objects could not be deleted,
          // there is no point in continuing to list and re-attempting the same failing objects.
          if (ex.FailedItems.Count == objectsInPage.Count)
          {
            _logger.ClearBucketDeleteBatchFailedFull(bucketName);
            isTruncated = false; // Force the loop to terminate.
          }
          else if (!continueOnError)
          {
            _logger.ClearBucketDeleteFailedStopOnError(ex, bucketName);
            throw; // Re-throw the batch exception
          }
          else
          {
            _logger.ClearBucketDeleteBatchFailedPartial(ex, ex.FailedItems.Count, bucketName);
          }
        }
    } while (isTruncated);

    // Deleting every object is not enough to leave the bucket deletable. An upload that was started and
    // never completed or aborted keeps holding storage that object listing never reports, and Cloudflare
    // then refuses to delete the bucket, reporting that it is not empty even though no object is visible.
    if (abortIncompleteMultipartUploads)
      try
      {
        totalMetrics += await AbortOpenMultipartUploadsAsync(bucketName, cancellationToken);
      }
      catch (Exception ex) when (ex is CloudflareR2ListException<MultipartUpload> or CloudflareR2OperationException)
      {
        // Objects that could not be deleted are the more actionable failure, so when both happen the batch
        // exception below reports them and carries this one inside its AggregateException.
        allExceptions.Add(ex);

        if (allFailedKeys.Count == 0)
          throw;
      }

    if (allFailedKeys.Any())
      throw new CloudflareR2BatchException<string>(
        $"Failed to delete {allFailedKeys.Count} objects while clearing bucket {bucketName}.",
        allFailedKeys.Distinct().ToList(), totalMetrics, new AggregateException(allExceptions));

    _logger.ClearedBucket(bucketName, totalMetrics.ClassAOperations);
    return totalMetrics;
  }


  /// <summary>
  ///   Discovers every multipart upload left open in the bucket and aborts each one, so that Cloudflare will
  ///   accept a subsequent delete of the bucket.
  /// </summary>
  /// <param name="bucketName">The name of the bucket to clean up.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> carrying the metrics of the discovery call and every abort call.</returns>
  private async Task<R2Result> AbortOpenMultipartUploadsAsync(string bucketName, CancellationToken cancellationToken)
  {
    // Pass no prefix: clearing the bucket means the whole bucket, not one prefix within it.
    var discovered   = await ListMultipartUploadsAsync(bucketName, null, cancellationToken);
    var totalMetrics = discovered.Metrics;

    foreach (var upload in discovered.Data)
    {
      cancellationToken.ThrowIfCancellationRequested();

      // Aborting an upload is a free operation, so this adds calls but no billable operations.
      totalMetrics += await AbortMultipartUploadAsync(bucketName, upload.Key, upload.UploadId, cancellationToken);
    }

    if (discovered.Data.Count > 0)
      _logger.AbortedOpenMultipartUploads(discovered.Data.Count, bucketName);

    return totalMetrics;
  }

  /// <inheritdoc />
  public async Task<R2Result<IReadOnlyList<S3Object>>> ListObjectsAsync(string            bucketName,
                                                                        string?           prefix,
                                                                        CancellationToken cancellationToken = default)
  {
    var totalMetrics      = new R2Result();
    var allObjects        = new List<S3Object>();
    var continuationToken = (string?)null;

    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();

      R2Result<R2ObjectPage> pageResult;

      try
      {
        // Ask for the largest page R2 allows: this method returns the whole prefix either way, so a smaller
        // page would only cost extra billable list calls.
        pageResult = await ListObjectsPageAsync(bucketName, prefix, MaxKeysPerListPage, continuationToken,
                                                cancellationToken);
      }
      catch (CloudflareR2ListException<S3Object> ex)
      {
        // ListObjectsPageAsync already logged the AWS SDK failure, but it knows only about its own single
        // call and so reports no objects. Re-throw carrying everything fetched by the earlier pages, which is
        // the partial listing this method has always promised its callers.
        throw new CloudflareR2ListException<S3Object>(
          $"Listing objects failed for s3://{bucketName}/{prefix}",
          allObjects, totalMetrics + ex.PartialMetrics, ex.InnerException ?? ex);
      }

      totalMetrics += pageResult.Metrics;
      allObjects.AddRange(pageResult.Data.Objects);

      if (!pageResult.Data.IsTruncated)
        break;

      // Continuing the walk needs the token the response returns. A provider that says there is more data
      // while returning no token leaves the token unchanged, so the next call would re-fetch this same page
      // for ever and re-add its objects to the accumulator on every pass. Throwing here is the same guard
      // ListPartsAsync and ListMultipartUploadsAsync apply to their own markers.
      if (string.IsNullOrEmpty(pageResult.Data.NextContinuationToken))
      {
        _logger.ListObjectsPaginationInconsistency(bucketName, prefix);

        throw new CloudflareR2ListException<S3Object>(
          $"Listing objects for s3://{bucketName}/{prefix} failed due to inconsistent pagination response from the provider (IsTruncated=true, but NextContinuationToken is null).",
          allObjects,
          totalMetrics,
          new InvalidOperationException("Inconsistent pagination response from S3-compatible provider."));
      }

      continuationToken = pageResult.Data.NextContinuationToken;
    }

    _logger.ListedObjects(allObjects.Count, bucketName, prefix);

    return new R2Result<IReadOnlyList<S3Object>>(allObjects, totalMetrics);
  }

  /// <inheritdoc />
  public async Task<R2Result<R2ObjectPage>> ListObjectsPageAsync(string            bucketName,
                                                                 string?           prefix,
                                                                 int               maxKeys,
                                                                 string?           continuationToken,
                                                                 CancellationToken cancellationToken = default)
  {
    // One list call, so exactly one Class A operation regardless of the outcome.
    var metrics = new R2Result(1);

    // Clamp to the S3 page ceiling; a caller asking for zero or less gets the full page.
    var effectiveMaxKeys = maxKeys <= 0 ? MaxKeysPerListPage : Math.Min(maxKeys, MaxKeysPerListPage);

    try
    {
      cancellationToken.ThrowIfCancellationRequested();

      var request = new ListObjectsV2Request
      {
        BucketName        = bucketName,
        Prefix            = prefix,
        MaxKeys           = effectiveMaxKeys,
        ContinuationToken = continuationToken
      };

      var response = await _s3Client.ListObjectsV2Async(request, cancellationToken);

      // The S3Objects list can be null if the response contains no objects.
      IReadOnlyList<S3Object> objects = response.S3Objects ?? [];

      // IsTruncated is type `bool?` on the SDK response.
      var isTruncated = response.IsTruncated == true;

      _logger.ListedObjectsPage(objects.Count, bucketName, prefix, isTruncated);

      var page = new R2ObjectPage(objects, response.NextContinuationToken, isTruncated);

      return new R2Result<R2ObjectPage>(page, metrics);
    }
    catch (AmazonS3Exception ex)
    {
      _logger.ListObjectsFailed(ex, bucketName, prefix);

      throw new CloudflareR2ListException<S3Object>(
        $"Listing a page of objects failed for s3://{bucketName}/{prefix}",
        [], metrics, ex);
    }
  }

  /// <inheritdoc />
  public async Task<R2Result<IReadOnlyList<MultipartUpload>>> ListMultipartUploadsAsync(
    string            bucketName,
    string?           prefix,
    CancellationToken cancellationToken = default)
  {
    var totalMetrics = new R2Result();
    var allUploads   = new List<MultipartUpload>();

    try
    {
      ListMultipartUploadsResponse response;

      var request = new ListMultipartUploadsRequest { BucketName = bucketName, Prefix = prefix };

      do
      {
        cancellationToken.ThrowIfCancellationRequested();

        // Account for the Class A operation before the call.
        totalMetrics += new R2Result(1);
        response     =  await _s3Client.ListMultipartUploadsAsync(request, cancellationToken);

        // The MultipartUploads list can be null when no upload is open under the prefix.
        if (response.MultipartUploads is not null)
          allUploads.AddRange(response.MultipartUploads);

        if (response.IsTruncated != true) // IsTruncated is type `bool?`
          break;

        // Continuing a multipart-upload listing needs BOTH markers the response returns. A provider that says there
        // is more data while returning neither marker leaves the request unchanged, so the next call would re-fetch
        // the same first page for ever and re-add its uploads to the accumulator on every pass. Throwing here is the
        // same guard ListPartsAsync applies to its own marker.
        if (string.IsNullOrEmpty(response.NextKeyMarker) && string.IsNullOrEmpty(response.NextUploadIdMarker))
        {
          _logger.ListMultipartUploadsPaginationInconsistency(bucketName, prefix);

          throw new CloudflareR2ListException<MultipartUpload>(
            $"Listing open multipart uploads for s3://{bucketName}/{prefix} failed due to inconsistent pagination response from the provider (IsTruncated=true, but neither NextKeyMarker nor NextUploadIdMarker is set).",
            allUploads,
            totalMetrics,
            new InvalidOperationException("Inconsistent pagination response from S3-compatible provider."));
        }

        request.KeyMarker      = response.NextKeyMarker;
        request.UploadIdMarker = response.NextUploadIdMarker;
      } while (true);

      _logger.ListedMultipartUploads(allUploads.Count, bucketName, prefix);

      return new R2Result<IReadOnlyList<MultipartUpload>>(allUploads, totalMetrics);
    }
    catch (AmazonS3Exception ex)
    {
      _logger.ListMultipartUploadsFailed(ex, bucketName, prefix);

      throw new CloudflareR2ListException<MultipartUpload>(
        $"Listing open multipart uploads failed for s3://{bucketName}/{prefix}",
        allUploads, totalMetrics, ex);
    }
  }

  /// <inheritdoc />
  public async Task<R2Result<IReadOnlyList<ListedPart>>> ListPartsAsync(string            bucketName,
                                                                        string            objectKey,
                                                                        string            uploadId,
                                                                        CancellationToken cancellationToken = default)
  {
    var totalMetrics = new R2Result();
    var allParts     = new List<ListedPart>();
    var request = new ListPartsRequest
    {
      BucketName = bucketName,
      Key        = objectKey,
      UploadId   = uploadId
    };

    while (true)
    {
      cancellationToken.ThrowIfCancellationRequested();

      try
      {
        // Account for the Class A operation before the call.
        totalMetrics += new R2Result(1);
        var response = await _s3Client.ListPartsAsync(request, cancellationToken);

        // R2 omits the part list entirely when the upload has no parts yet, which the AWS SDK surfaces as a
        // null collection rather than an empty one. Treat that as "no parts" instead of dereferencing it.
        if (response.Parts is not null)
          allParts.AddRange(response.Parts.Select(p => new ListedPart(p.PartNumber, p.ETag, p.Size, p.LastModified)));

        if (response.IsTruncated != true) // IsTruncated is type `bool?`
          break;

        if (response.NextPartNumberMarker is null)
        {
          // If the provider says there's more data but doesn't provide a marker, we cannot continue.
          // Throwing prevents an infinite loop of re-requesting the same first page.
          _logger.ListPartsPaginationInconsistency(uploadId);
          throw new CloudflareR2ListException<ListedPart>(
            $"Listing parts for uploadId {uploadId} failed due to inconsistent pagination response from the provider (IsTruncated=true, but NextPartNumberMarker is null).",
            allParts,
            totalMetrics,
            new InvalidOperationException("Inconsistent pagination response from S3-compatible provider."));
        }

        request.PartNumberMarker = response.NextPartNumberMarker.ToString(); // NextPartNumberMarker is type `int?`
      }
      catch (AmazonS3Exception ex)
      {
        _logger.ListPartsFailed(ex, uploadId);
        // Throw with the data we've managed to fetch so far.
        throw new CloudflareR2ListException<ListedPart>(
          $"Listing parts failed for uploadId {uploadId}",
          allParts, totalMetrics, ex);
      }
    }

    return new R2Result<IReadOnlyList<ListedPart>>(allParts, totalMetrics);
  }

  /// <inheritdoc />
  public string CreatePresignedPutUrl(string bucketName, PresignedPutRequest request)
  {
    try
    {
      var presignedUrlRequest = new GetPreSignedUrlRequest
      {
        BucketName = bucketName,
        Key        = request.Key,
        Verb       = HttpVerb.PUT,
        Expires    = DateTime.UtcNow.Add(request.ExpiresAfter),
        Headers =
        {
          // These headers are added to the signature, so the client MUST provide them with the exact same values.
          // This is how R2 enforces these constraints for a presigned PUT.
          ["Content-Type"]   = request.ContentType,
          ["Content-Length"] = request.ContentLength.ToString()
        },
        ContentType = request.ContentType
      };

      if (request.HeadersToSign is not null)
        foreach (var header in request.HeadersToSign)
          presignedUrlRequest.Headers[header.Key] = header.Value;

      // The typed checksum is applied after HeadersToSign, so on a collision the validated value wins.
      // R2 verifies every admitted algorithm on a single-part PUT (wrong digest: 400 BadDigest).
      if (request.Checksum is not null)
        presignedUrlRequest.Headers[request.Checksum.Algorithm.HeaderName] = request.Checksum.Base64Digest;

      // The typed Cache-Control is applied after HeadersToSign for the same collision rule. Signing the
      // header forces the client to send exactly this value (403 otherwise), and R2 stores it on the
      // object and serves it on every GET; a null or blank value leaves the header unsigned.
      if (!string.IsNullOrWhiteSpace(request.CacheControl))
        presignedUrlRequest.Headers.CacheControl = request.CacheControl;

      return GeneratePresignedUrl(presignedUrlRequest);
    }
    catch (AmazonS3Exception ex)
    {
      _logger.PresignedUrlGenerationFailed(ex, request.Key, bucketName);
      throw new CloudflareR2OperationException("Failed to generate presigned PUT URL.", new R2Result(), ex);
    }
  }

  /// <inheritdoc />
  public string CreatePresignedGetUrl(string bucketName, PresignedGetRequest request)
  {
    try
    {
      var presignedUrlRequest = new GetPreSignedUrlRequest
      {
        BucketName = bucketName,
        Key        = request.Key,
        Verb       = HttpVerb.GET,
        Expires    = DateTime.UtcNow.Add(request.ExpiresAfter)
      };

      // The overrides are part of the signed query string (response-content-type / response-content-disposition), so
      // R2 stamps them on its own response and the URL holder cannot change them after signing. They let a download
      // link name a media type and a save-as filename the stored object itself does not carry.
      if (request.ResponseContentType is not null)
        presignedUrlRequest.ResponseHeaderOverrides.ContentType = request.ResponseContentType;

      if (request.ResponseContentDisposition is not null)
        presignedUrlRequest.ResponseHeaderOverrides.ContentDisposition = request.ResponseContentDisposition;

      return GeneratePresignedUrl(presignedUrlRequest);
    }
    catch (AmazonS3Exception ex)
    {
      _logger.PresignedUrlGenerationFailed(ex, request.Key, bucketName);
      throw new CloudflareR2OperationException("Failed to generate presigned GET URL.", new R2Result(), ex);
    }
  }

#if false // There is currently a NRE in the AWS SDK when using CreatePresignedPostUrl with R2.
  /// <inheritdoc />
  public async Task<PresignedPostResponse> CreatePresignedPostUrlAsync(string               bucketName,
                                                                       PresignedPostRequest request)
  {
    try
    {
      var s3Request = new CreatePresignedPostRequest
      {
        BucketName = bucketName,
        Key = request.Key,
        Expires = DateTime.UtcNow.Add(request.ExpiresAfter)
      };

      if (request.ContentType is not null)
        s3Request.Fields.Add("Content-Type", request.ContentType);

      if (request.HeadersToSign is not null)
        foreach (var header in request.HeadersToSign)
          s3Request.Fields.Add(header.Key, header.Value);

      if (request.ContentLengthRange is { } range)
        s3Request.Conditions.AddRange(S3PostCondition.ContentLengthRange(range.Min, range.Max));

      // Add the new flexible conditions.
      if (request.Conditions is not null)
        s3Request.Conditions.AddRange(request.Conditions);

      var response = await s3Client.CreatePresignedPostAsync(s3Request);

      return new PresignedPostResponse(response.Url, response.Fields);
    }
    catch (AmazonS3Exception ex)
    {
      _logger.PresignedUrlGenerationFailed(ex, request.Key, bucketName);
      throw new CloudflareR2OperationException("Failed to generate presigned POST URL.", new R2Result(), ex);
    }
  }
#endif

  /// <summary>Generates a presigned URL using the underlying S3 client.</summary>
  /// <param name="request">The request for the presigned URL.</param>
  /// <returns>The generated presigned URL.</returns>
  /// <remarks>This method is virtual to allow for mocking in unit tests.</remarks>
  internal protected virtual string GeneratePresignedUrl(GetPreSignedUrlRequest request)
  {
    return _s3Client.GetPreSignedURL(request);
  }

  /// <inheritdoc />
  public Task<R2Result<string>> InitiateMultipartUploadAsync(string            bucketName,
                                                             string            objectKey,
                                                             CancellationToken cancellationToken = default)
  {
    // Defer to the overload that carries a content type. Passing null leaves the property unset on the
    // request, which is exactly the behaviour this overload has always had.
    return InitiateMultipartUploadAsync(bucketName, objectKey, null, null, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<R2Result<string>> InitiateMultipartUploadAsync(string            bucketName,
                                                                   string            objectKey,
                                                                   string?           contentType,
                                                                   string?           cacheControl      = null,
                                                                   CancellationToken cancellationToken = default)
  {
    var metrics = new R2Result(1);

    try
    {
      var request = new InitiateMultipartUploadRequest
      {
        BucketName = bucketName,
        Key        = objectKey
      };

      // S3 records the assembled object's Content-Type from this initiate request; the individual parts
      // cannot carry it. Only assign the property when the caller supplied a value, so that a null or
      // blank argument still lets R2 apply its own default rather than sending an empty header.
      if (!string.IsNullOrWhiteSpace(contentType))
        request.ContentType = contentType;

      // Cache-Control follows the same rule: it is stored on the assembled object from this initiate
      // request only, and a null or blank argument leaves the header unset.
      if (!string.IsNullOrWhiteSpace(cacheControl))
        request.Headers.CacheControl = cacheControl;

      var response = await _s3Client.InitiateMultipartUploadAsync(request, cancellationToken);

      _logger.InitiatedMultipartUpload(bucketName, objectKey, response.UploadId);
      return new R2Result<string>(response.UploadId, metrics);
    }
    catch (AmazonS3Exception ex)
    {
      _logger.InitiateMultipartUploadFailed(ex, bucketName, objectKey);
      throw new CloudflareR2OperationException($"Failed to initiate multipart upload for s3://{bucketName}/{objectKey}", metrics, ex);
    }
  }

  /// <inheritdoc />
  public string CreatePresignedUploadPartUrl(string bucketName, PresignedUploadPartRequest request)
  {
    try
    {
      var presignedUrlRequest = new GetPreSignedUrlRequest
      {
        BucketName = bucketName,
        Key        = request.Key,
        Verb       = HttpVerb.PUT,
        Expires    = DateTime.UtcNow.Add(request.ExpiresAfter),
        // UploadId and PartNumber must be set through these dedicated properties, which emit the
        // "uploadId" and "partNumber" query parameters that the S3 UploadPart operation is keyed on.
        // Putting them in the Parameters collection instead emits them as "x-uploadId" and
        // "x-partNumber", because that collection is for arbitrary custom parameters and the AWS SDK
        // prefixes those with "x-". R2 then sees a PUT with no recognisable upload identifier, answers
        // 200, and writes a plain object over the key instead of recording a part.
        UploadId   = request.UploadId,
        PartNumber = request.PartNumber,
        Headers =
        {
          // The Content-Length header is signed to enforce size on the provider side.
          ["Content-Length"] = request.ContentLength.ToString()
        }
      };

      if (request.HeadersToSign is not null)
        foreach (var header in request.HeadersToSign)
          presignedUrlRequest.Headers[header.Key] = header.Value;

      // The typed checksum is applied after HeadersToSign, so on a collision the validated value wins.
      // Refusing part-unsupported algorithms here spares the client a guaranteed 501 from R2.
      if (request.Checksum is not null)
      {
        ThrowIfChecksumUnsupportedForParts(request.Checksum);
        presignedUrlRequest.Headers[request.Checksum.Algorithm.HeaderName] = request.Checksum.Base64Digest;
      }

      return GeneratePresignedUrl(presignedUrlRequest);
    }
    catch (AmazonS3Exception ex)
    {
      _logger.PresignedUrlGenerationFailed(ex, request.Key, bucketName);
      throw new CloudflareR2OperationException("Failed to generate presigned part URL.", new R2Result(), ex);
    }
  }

  /// <summary>
  ///   Rejects a checksum whose algorithm R2 refuses on multipart part uploads. R2 answers 501
  ///   <c>NotImplemented</c> to a part upload carrying a SHA-1 or SHA-256 checksum header, whatever the digest's
  ///   value, so signing such a URL would only manufacture a guaranteed failure for the client holding it.
  /// </summary>
  /// <param name="checksum">The checksum a part URL request carried.</param>
  /// <exception cref="ArgumentException">Thrown when the algorithm is not verified by R2 on part uploads.</exception>
  private static void ThrowIfChecksumUnsupportedForParts(UploadChecksum checksum)
  {
    if (!checksum.Algorithm.IsSupportedForPartUploads)
      throw new ArgumentException(
        $"R2 answers 501 NotImplemented to a part upload carrying a {checksum.Algorithm.Value} checksum header. "
        + $"Use {R2ChecksumAlgorithm.Crc32.Value}, {R2ChecksumAlgorithm.Crc32C.Value} or {R2ChecksumAlgorithm.Md5.Value} for parts; "
        + "SHA-1 and SHA-256 are only verified on single-part PUTs.");
  }

  /// <summary>
  ///   Rejects a checksum on an upload that will go multipart. The caller's digest covers the whole object, but a
  ///   multipart upload is verified per part and this client does not compute per-part digests, so accepting the
  ///   digest here would silently skip the verification the caller asked for.
  /// </summary>
  /// <param name="checksum">The checksum the caller supplied, or null for none.</param>
  /// <exception cref="ArgumentException">Thrown when a checksum accompanies an upload taking the multipart path.</exception>
  private static void ThrowIfChecksumOnMultipartPath(UploadChecksum? checksum)
  {
    if (checksum is not null)
      throw new ArgumentException(
        "A checksum can only be verified on a single-part upload: the digest covers the whole object, but a multipart "
        + $"upload is verified per part and this client does not compute per-part digests. Use {nameof(UploadSinglePartAsync)} "
        + "for objects up to 5 GiB, or upload without a checksum.",
        nameof(checksum));
  }

  /// <summary>
  ///   Copies a caller-supplied digest onto the <see cref="PutObjectRequest" /> property matching its algorithm, so
  ///   the SDK sends it in the algorithm's header and R2 verifies the uploaded bytes against it.
  /// </summary>
  /// <param name="request">The request the digest is applied to.</param>
  /// <param name="checksum">The digest to apply.</param>
  private static void ApplyChecksum(PutObjectRequest request, UploadChecksum checksum)
  {
    if (checksum.Algorithm == R2ChecksumAlgorithm.Crc32)
      request.ChecksumCRC32 = checksum.Base64Digest;
    else if (checksum.Algorithm == R2ChecksumAlgorithm.Crc32C)
      request.ChecksumCRC32C = checksum.Base64Digest;
    else if (checksum.Algorithm == R2ChecksumAlgorithm.Sha1)
      request.ChecksumSHA1 = checksum.Base64Digest;
    else if (checksum.Algorithm == R2ChecksumAlgorithm.Sha256)
      request.ChecksumSHA256 = checksum.Base64Digest;
    else if (checksum.Algorithm == R2ChecksumAlgorithm.Md5)
      request.MD5Digest = checksum.Base64Digest;
    else
      // Unreachable while R2ChecksumAlgorithm stays a closed set; a new algorithm added there must be
      // mapped to its PutObjectRequest property here.
      throw new ArgumentOutOfRangeException(nameof(checksum), checksum.Algorithm.Value,
                                            "No PutObjectRequest property mapping for this checksum algorithm.");
  }

  /// <inheritdoc />
  public IReadOnlyDictionary<int, string> CreatePresignedUploadPartsUrls(
    string                      bucketName,
    PresignedUploadPartsRequest request)
  {
    // Pre-flight validation of part sizes for R2 compatibility.
    // R2 requires all parts except the last one to be the same size.
    if (request.PartNumberAndLength.Count > 1)
    {
      var sortedParts = request.PartNumberAndLength.OrderBy(kvp => kvp.Key).ToList();
      // Use the size of the first part (in sequence) as the reference for uniform size.
      var uniformPartSize = sortedParts.First().Value;

      // Check all parts *except the last one* for uniform size.
      for (var i = 0; i < sortedParts.Count - 1; i++)
      {
        var part = sortedParts[i];
        if (part.Value != uniformPartSize)
          throw new ArgumentException(
            $"R2 requires all multipart parts except the last to be the same size. Part {part.Key} has size {part.Value}, but the uniform part size is {uniformPartSize}.",
            nameof(request));

        if (part.Value is < R2MinPartSize or > R2MaxPartSize)
          throw new ArgumentException(
            $"Part {part.Key} has size {part.Value}, which is outside the allowed range of {R2MinPartSize} to {R2MaxPartSize} bytes.",
            nameof(request));
      }

      // Separately validate the last part's size. It can be smaller, but not larger.
      var lastPart = sortedParts.Last();
      if (lastPart.Value > uniformPartSize)
        throw new ArgumentException(
          $"The last part (Part {lastPart.Key}, Size {lastPart.Value}) cannot be larger than the uniform part size ({uniformPartSize}).",
          nameof(request));

      if (lastPart.Value is < R2MinPartSize or > R2MaxPartSize)
        throw new ArgumentException(
          $"The last part (Part {lastPart.Key}) has size {lastPart.Value}, which is outside the allowed range of {R2MinPartSize} to {R2MaxPartSize} bytes.",
          nameof(request));
    }
    else if (request.PartNumberAndLength.Count == 1)
    {
      // If there's only one part, it still needs to be within the min/max size limits.
      var singlePart = request.PartNumberAndLength.First();
      if (singlePart.Value is < R2MinPartSize or > R2MaxPartSize)
        throw new ArgumentException(
          $"Part {singlePart.Key} has size {singlePart.Value}, which is outside the allowed range of {R2MinPartSize} to {R2MaxPartSize} bytes.",
          nameof(request));
    }


    // Validate the per-part checksums before signing anything, so a bad entry fails the whole call
    // rather than producing a partial batch.
    if (request.ChecksumsByPartNumber is not null)
      foreach (var (partNumber, checksum) in request.ChecksumsByPartNumber)
      {
        if (!request.PartNumberAndLength.ContainsKey(partNumber))
          throw new ArgumentException(
            $"A checksum was supplied for part {partNumber}, but {nameof(request.PartNumberAndLength)} has no such part.",
            nameof(request));

        ThrowIfChecksumUnsupportedForParts(checksum);
      }

    // Pre-size the dictionary to the exact number of parts to avoid reallocations.
    var urls = new Dictionary<int, string>(request.PartNumberAndLength.Count);

    // The signature timestamp is computed once so every part URL in the batch expires at the same instant.
    var expiresAt = DateTime.UtcNow.Add(request.ExpiresAfter);

    try
    {
      // Iterate through the requested parts to generate a URL for each. A fresh request object is built
      // per part: each part may sign its own checksum header, and the AWS SDK's HeadersCollection offers
      // no way to remove a header once set, so a shared object would leak one part's digest header into
      // the URLs of every later part that has none.
      foreach (var (partNumber, contentLength) in request.PartNumberAndLength)
      {
        var presignedUrlRequest = new GetPreSignedUrlRequest
        {
          BucketName = bucketName,
          Key        = request.Key,
          Verb       = HttpVerb.PUT,
          Expires    = expiresAt,
          // UploadId and PartNumber must be set through these dedicated properties so that the URL carries
          // the "uploadId" and "partNumber" query parameters the S3 UploadPart operation is keyed on. The
          // Parameters collection is for arbitrary custom parameters and the AWS SDK prefixes those with
          // "x-", which would make R2 treat each part upload as a plain object PUT over the key.
          UploadId   = request.UploadId,
          PartNumber = partNumber,
          Headers =
          {
            ["Content-Length"] = contentLength.ToString()
          }
        };

        // Add the headers the caller wants signed into every part's URL.
        if (request.HeadersToSign is not null)
          foreach (var header in request.HeadersToSign)
            presignedUrlRequest.Headers[header.Key] = header.Value;

        // Bind this part's own digest into its URL. The typed checksum is applied after HeadersToSign, so
        // on a collision the validated value wins.
        if (request.ChecksumsByPartNumber is not null
            && request.ChecksumsByPartNumber.TryGetValue(partNumber, out var checksum))
          presignedUrlRequest.Headers[checksum.Algorithm.HeaderName] = checksum.Base64Digest;

        // Generate the signed URL for the current part and add it to the dictionary.
        urls[partNumber] = GeneratePresignedUrl(presignedUrlRequest);
      }

      return urls;
    }
    catch (Exception ex)
    {
      // If any single URL generation fails, wrap it in a custom exception.
      // The exception 'ex' will contain the specific part number that failed if debugged.
      _logger.PresignedUrlGenerationFailed(ex, request.Key, bucketName);
      throw new CloudflareR2OperationException(
        $"Failed to generate one or more presigned part URLs for upload {request.UploadId}.", new R2Result(), ex);
    }
  }

  /// <inheritdoc />
  public async Task<R2Result> CompleteMultipartUploadAsync(string                bucketName,
                                                           string                objectKey,
                                                           string                uploadId,
                                                           IEnumerable<PartETag> parts,
                                                           CancellationToken     cancellationToken = default)
  {
    var metrics = new R2Result(1);

    try
    {
      var request = new CompleteMultipartUploadRequest
      {
        BucketName = bucketName,
        Key        = objectKey,
        UploadId   = uploadId,
        PartETags  = parts.ToList()
      };

      await _s3Client.CompleteMultipartUploadAsync(request, cancellationToken);
      _logger.CompletedMultipartUpload(bucketName, objectKey);

      return metrics;
    }
    catch (AmazonS3Exception ex)
    {
      _logger.CompleteMultipartUploadFailed(ex, bucketName, objectKey);
      throw new CloudflareR2OperationException($"Failed to complete multipart upload for s3://{bucketName}/{objectKey}", metrics, ex);
    }
  }

  /// <inheritdoc />
  public async Task<R2Result> AbortMultipartUploadAsync(string            bucketName,
                                                        string            objectKey,
                                                        string            uploadId,
                                                        CancellationToken cancellationToken = default)
  {
    // According to the CF documentation, AbortMultipartUpload is a free operation.
    var metrics = new R2Result();

    try
    {
      var request = new AbortMultipartUploadRequest
      {
        BucketName = bucketName,
        Key        = objectKey,
        UploadId   = uploadId
      };

      await _s3Client.AbortMultipartUploadAsync(request, cancellationToken);
      _logger.AbortedMultipartUpload(uploadId);

      return metrics;
    }
    catch (AmazonS3Exception ex)
    {
      _logger.AbortMultipartUploadFailed(ex, uploadId);
      throw new CloudflareR2OperationException($"Failed to abort multipart upload {uploadId}", metrics, ex);
    }
  }

  #endregion
}
