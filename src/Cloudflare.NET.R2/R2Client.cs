namespace Cloudflare.NET.R2;

using Amazon.S3;
using Amazon.S3.Model;
using Exceptions;
using Logging;
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
  /// <summary>The maximum number of parts allowed in a single multipart upload.</summary>
  internal const int MaxPartsPerUpload = 10000;

  #endregion

  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="R2Client" /> class. This is the
  ///   designated constructor for dependency injection.
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
  {
    var fileInfo = new FileInfo(filePath);

    if (fileInfo.Length > R2MaxMultipartFileSize)
      throw new ArgumentException($"File size ({fileInfo.Length} bytes) exceeds the maximum R2 object size of 5 TiB.",
                                  nameof(filePath));

    if (fileInfo.Length < R2MutlipartFileSizeThreshold)
      return UploadSinglePartAsync(bucketName, objectKey, filePath, cancellationToken);

    return UploadMultipartAsync(bucketName, objectKey, filePath, partSize, cancellationToken);
  }

  /// <inheritdoc />
  public Task<R2Result> UploadAsync(string            bucketName,
                                    string            objectKey,
                                    Stream            fileStream,
                                    long?             partSize          = null,
                                    CancellationToken cancellationToken = default)
  {
    if (fileStream is { CanSeek: true, Length: > R2MaxMultipartFileSize })
      throw new ArgumentException($"Stream length ({fileStream.Length} bytes) exceeds the maximum R2 object size of 5 TiB.",
                                  nameof(fileStream));

    if (fileStream is { CanSeek: true, Length: < R2MutlipartFileSizeThreshold })
      return UploadSinglePartAsync(bucketName, objectKey, fileStream, cancellationToken);

    // If we can't determine the length or it's large, use multipart.
    return UploadMultipartAsync(bucketName, objectKey, fileStream, partSize, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<R2Result> UploadSinglePartAsync(string            bucketName,
                                                    string            objectKey,
                                                    string            filePath,
                                                    CancellationToken cancellationToken = default)
  {
    await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

    return await UploadSinglePartAsync(bucketName, objectKey, fileStream, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<R2Result> UploadSinglePartAsync(string            bucketName,
                                                    string            objectKey,
                                                    Stream            inputStream,
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
  public async Task<R2Result> UploadMultipartAsync(string            bucketName,
                                                   string            objectKey,
                                                   string            filePath,
                                                   long?             partSize,
                                                   CancellationToken cancellationToken = default)
  {
    await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

    return await UploadMultipartAsync(bucketName, objectKey, fileStream, partSize, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<R2Result> UploadMultipartAsync(string            bucketName,
                                                   string            objectKey,
                                                   Stream            inputStream,
                                                   long?             partSize,
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

    // 1. Initiate (1 Class A op)
    var initResult = await InitiateMultipartUploadAsync(bucketName, objectKey, cancellationToken);

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
                                               bool              continueOnError   = true,
                                               CancellationToken cancellationToken = default)
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

    if (allFailedKeys.Any())
      throw new CloudflareR2BatchException<string>(
        $"Failed to delete {allFailedKeys.Count} objects while clearing bucket {bucketName}.",
        allFailedKeys.Distinct().ToList(), totalMetrics, new AggregateException(allExceptions));

    _logger.ClearedBucket(bucketName, totalMetrics.ClassAOperations);
    return totalMetrics;
  }

  /// <inheritdoc />
  public async Task<R2Result<IReadOnlyList<S3Object>>> ListObjectsAsync(string            bucketName,
                                                                        string?           prefix,
                                                                        CancellationToken cancellationToken = default)
  {
    var totalMetrics = new R2Result();
    var allObjects   = new List<S3Object>();

    try
    {
      ListObjectsV2Response response;
      var                   request = new ListObjectsV2Request { BucketName = bucketName, Prefix = prefix };

      do
      {
        cancellationToken.ThrowIfCancellationRequested();

        // Account for the Class A operation before the call.
        totalMetrics += new R2Result(1);
        response     =  await _s3Client.ListObjectsV2Async(request, cancellationToken);

        // The S3Objects list can be null if the response contains no objects.
        if (response.S3Objects is not null)
          allObjects.AddRange(response.S3Objects);

        request.ContinuationToken = response.NextContinuationToken;
      } while (response.IsTruncated == true); // IsTruncated is type `bool?`

      _logger.ListedObjects(allObjects.Count, bucketName, prefix);
      return new R2Result<IReadOnlyList<S3Object>>(allObjects, totalMetrics);
    }
    catch (AmazonS3Exception ex)
    {
      _logger.ListObjectsFailed(ex, bucketName, prefix);
      throw new CloudflareR2ListException<S3Object>(
        $"Listing objects failed for s3://{bucketName}/{prefix}",
        allObjects, totalMetrics, ex);
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

      return GeneratePresignedUrl(presignedUrlRequest);
    }
    catch (AmazonS3Exception ex)
    {
      _logger.PresignedUrlGenerationFailed(ex, request.Key, bucketName);
      throw new CloudflareR2OperationException("Failed to generate presigned PUT URL.", new R2Result(), ex);
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
  public async Task<R2Result<string>> InitiateMultipartUploadAsync(string            bucketName,
                                                                   string            objectKey,
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
        Parameters =
        {
          // Per-part parameters must be added to the parameters collection to be included in the signature.
          ["uploadId"]   = request.UploadId,
          ["partNumber"] = request.PartNumber.ToString()
        },
        Headers =
        {
          // The Content-Length header is signed to enforce size on the provider side.
          ["Content-Length"] = request.ContentLength.ToString()
        }
      };

      if (request.HeadersToSign is not null)
        foreach (var header in request.HeadersToSign)
          presignedUrlRequest.Headers[header.Key] = header.Value;

      return GeneratePresignedUrl(presignedUrlRequest);
    }
    catch (AmazonS3Exception ex)
    {
      _logger.PresignedUrlGenerationFailed(ex, request.Key, bucketName);
      throw new CloudflareR2OperationException("Failed to generate presigned part URL.", new R2Result(), ex);
    }
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


    // Pre-size the dictionary to the exact number of parts to avoid reallocations.
    var urls = new Dictionary<int, string>(request.PartNumberAndLength.Count);

    try
    {
      // Create the request object once, outside the loop, to minimize allocations.
      // The parameters that change per part will be updated inside the loop.
      var presignedUrlRequest = new GetPreSignedUrlRequest
      {
        BucketName = bucketName,
        Key        = request.Key,
        Verb       = HttpVerb.PUT,
        Expires    = DateTime.UtcNow.Add(request.ExpiresAfter),
        Parameters =
        {
          ["uploadId"] = request.UploadId,
          // PartNumber will be updated in the loop. Initialize with a placeholder.
          ["partNumber"] = "0"
        },
        Headers =
        {
          // Content-Length will be updated in the loop. Initialize with a placeholder.
          ["Content-Length"] = "0"
        }
      };

      // Add headers that are fixed for all parts to the request object once.
      if (request.HeadersToSign is not null)
        foreach (var header in request.HeadersToSign)
          presignedUrlRequest.Headers[header.Key] = header.Value;

      // Iterate through the requested parts to generate a URL for each.
      foreach (var (partNumber, contentLength) in request.PartNumberAndLength)
      {
        // Update only the parameters that change per iteration.
        presignedUrlRequest.Parameters["partNumber"]  = partNumber.ToString();
        presignedUrlRequest.Headers["Content-Length"] = contentLength.ToString();

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
