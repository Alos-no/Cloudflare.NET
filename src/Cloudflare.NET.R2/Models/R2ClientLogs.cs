namespace Cloudflare.NET.R2.Models;

using Microsoft.Extensions.Logging;

/// <summary>
///   Contains logging definitions for the R2Client. For .NET 6+, these use source-generated LoggerMessage for high
///   performance. For .NET Standard 2.1, manual implementations are used.
/// </summary>
internal static partial class R2ClientLogs
{
#if NET6_0_OR_GREATER

  #region Source-Generated Logging (NET6+)

  [LoggerMessage(EventId = 1001, Level = LogLevel.Debug, Message = "Successfully uploaded to s3://{Bucket}/{Key} via single PUT.")]
  public static partial void UploadedSinglePart(this ILogger logger, string bucket, string key);

  [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "AWS SDK Error during single-part upload to s3://{Bucket}/{Key}")]
  public static partial void UploadSinglePartFailed(this ILogger logger, Exception ex, string bucket, string key);

  [LoggerMessage(EventId = 1003, Level = LogLevel.Debug, Message = "Successfully uploaded to s3://{Bucket}/{Key} via multipart.")]
  public static partial void UploadedMultipart(this ILogger logger, string bucket, string key);

  [LoggerMessage(EventId = 1004, Level = LogLevel.Error, Message = "Multipart upload failed for s3://{Bucket}/{Key}. Aborting...")]
  public static partial void MultipartFailed(this ILogger logger, Exception ex, string bucket, string key);

  [LoggerMessage(EventId = 1005, Level = LogLevel.Debug, Message = "Successfully downloaded s3://{Bucket}/{Key}.")]
  public static partial void DownloadedObject(this ILogger logger, string bucket, string key);

  [LoggerMessage(EventId = 1006, Level = LogLevel.Error, Message = "AWS SDK Error during download from s3://{Bucket}/{Key}")]
  public static partial void DownloadFailed(this ILogger logger, Exception ex, string bucket, string key);

  [LoggerMessage(EventId = 1007, Level = LogLevel.Debug, Message = "Successfully deleted s3://{Bucket}/{Key}.")]
  public static partial void DeletedObject(this ILogger logger, string bucket, string key);

  [LoggerMessage(EventId = 1008, Level = LogLevel.Error, Message = "AWS SDK Error during delete of s3://{Bucket}/{Key}")]
  public static partial void DeleteFailed(this ILogger logger, Exception ex, string bucket, string key);

  [LoggerMessage(EventId = 1009, Level = LogLevel.Information, Message = "Successfully deleted {Count} objects from bucket {BucketName}.")]
  public static partial void DeletedMultipleObjects(this ILogger logger, int count, string bucketName);

  [LoggerMessage(EventId = 1010, Level = LogLevel.Information, Message = "Attempting to clear all objects from bucket: {BucketName}")]
  public static partial void ClearingBucket(this ILogger logger, string bucketName);

  [LoggerMessage(EventId = 1011, Level = LogLevel.Error, Message = "Failed to list objects while clearing bucket {BucketName}")]
  public static partial void ClearBucketListFailed(this ILogger logger, Exception ex, string bucketName);

  [LoggerMessage(EventId = 1012, Level = LogLevel.Information,
                 Message = "Successfully cleared bucket {BucketName}, consuming {Ops} Class A operations.")]
  public static partial void ClearedBucket(this ILogger logger, string bucketName, long ops);

  [LoggerMessage(EventId = 1013, Level = LogLevel.Debug,
                 Message = "Successfully listed {Count} objects in s3://{Bucket} with prefix {Prefix}.")]
  public static partial void ListedObjects(this ILogger logger, int count, string bucket, string? prefix);

  [LoggerMessage(EventId = 1014, Level = LogLevel.Error, Message = "AWS SDK Error while listing s3://{Bucket}/{Prefix}")]
  public static partial void ListObjectsFailed(this ILogger logger, Exception ex, string bucket, string? prefix);

  [LoggerMessage(EventId = 1015, Level = LogLevel.Debug,
                 Message = "Initiated multipart upload for s3://{Bucket}/{Key} with UploadId {UploadId}")]
  public static partial void InitiatedMultipartUpload(this ILogger logger, string bucket, string key, string uploadId);

  [LoggerMessage(EventId = 1016, Level = LogLevel.Error, Message = "Failed to initiate multipart upload for s3://{Bucket}/{Key}")]
  public static partial void InitiateMultipartUploadFailed(this ILogger logger, Exception ex, string bucket, string key);

  [LoggerMessage(EventId = 1017, Level = LogLevel.Debug, Message = "Successfully completed multipart upload for s3://{Bucket}/{Key}")]
  public static partial void CompletedMultipartUpload(this ILogger logger, string bucket, string key);

  [LoggerMessage(EventId = 1018, Level = LogLevel.Error, Message = "Failed to complete multipart upload for s3://{Bucket}/{Key}")]
  public static partial void CompleteMultipartUploadFailed(this ILogger logger, Exception ex, string bucket, string key);

  [LoggerMessage(EventId = 1019, Level = LogLevel.Information, Message = "Successfully aborted multipart upload {UploadId}")]
  public static partial void AbortedMultipartUpload(this ILogger logger, string uploadId);

  [LoggerMessage(EventId = 1020, Level = LogLevel.Error, Message = "Failed to abort multipart upload {UploadId}")]
  public static partial void AbortMultipartUploadFailed(this ILogger logger, Exception ex, string uploadId);

  [LoggerMessage(EventId = 1021, Level = LogLevel.Warning,
                 Message = "A batch delete failed for bucket {BucketName}. Adding all keys from batch to failed list.")]
  public static partial void BatchDeleteFailedContinueOnError(this ILogger logger, Exception ex, string bucketName);

  [LoggerMessage(EventId = 1022, Level = LogLevel.Error,
                 Message = "A batch delete failed for bucket {BucketName} and continueOnError is false.")]
  public static partial void BatchDeleteFailedStopOnError(this ILogger logger, Exception ex, string bucketName);

  [LoggerMessage(EventId = 1023, Level = LogLevel.Warning,
                 Message =
                   "Unable to delete any objects in the current batch for bucket {BucketName}. Aborting clear operation to prevent an infinite loop.")]
  public static partial void ClearBucketDeleteBatchFailedFull(this ILogger logger, string bucketName);

  [LoggerMessage(EventId = 1024, Level = LogLevel.Error,
                 Message = "Failed to delete a batch of objects while clearing bucket {BucketName} and continueOnError is false.")]
  public static partial void ClearBucketDeleteFailedStopOnError(this ILogger logger, Exception ex, string bucketName);

  [LoggerMessage(EventId = 1025, Level = LogLevel.Warning,
                 Message =
                   "Failed to delete a batch of {Count} objects while clearing bucket {BucketName}. Continuing because continueOnError is true.")]
  public static partial void ClearBucketDeleteBatchFailedPartial(this ILogger logger, Exception ex, int count, string bucketName);

  [LoggerMessage(EventId = 1026, Level = LogLevel.Error, Message = "Failed to list parts for upload {UploadId}")]
  public static partial void ListPartsFailed(this ILogger logger, Exception ex, string uploadId);

  [LoggerMessage(EventId = 1027, Level = LogLevel.Error, Message = "Failed to generate a presigned URL for Key={Key} in Bucket={Bucket}")]
  public static partial void PresignedUrlGenerationFailed(this ILogger logger, Exception ex, string key, string bucket);

  [LoggerMessage(EventId = 1028, Level = LogLevel.Critical,
                 Message =
                   "Inconsistent pagination from R2 for upload {UploadId}: IsTruncated is true, but NextPartNumberMarker is null. Aborting to prevent infinite loop.")]
  public static partial void ListPartsPaginationInconsistency(this ILogger logger, string uploadId);

  #endregion

#else
  #region Manual Logging (NetStandard2.1)

  public static void UploadedSinglePart(this ILogger logger, string bucket, string key)
  {
    if (logger.IsEnabled(LogLevel.Debug))
      logger.LogDebug("Successfully uploaded to s3://{Bucket}/{Key} via single PUT.", bucket, key);
  }

  public static void UploadSinglePartFailed(this ILogger logger, Exception ex, string bucket, string key)
  {
    if (logger.IsEnabled(LogLevel.Error))
      logger.LogError(ex, "AWS SDK Error during single-part upload to s3://{Bucket}/{Key}", bucket, key);
  }

  public static void UploadedMultipart(this ILogger logger, string bucket, string key)
  {
    if (logger.IsEnabled(LogLevel.Debug))
      logger.LogDebug("Successfully uploaded to s3://{Bucket}/{Key} via multipart.", bucket, key);
  }

  public static void MultipartFailed(this ILogger logger, Exception ex, string bucket, string key)
  {
    if (logger.IsEnabled(LogLevel.Error))
      logger.LogError(ex, "Multipart upload failed for s3://{Bucket}/{Key}. Aborting...", bucket, key);
  }

  public static void DownloadedObject(this ILogger logger, string bucket, string key)
  {
    if (logger.IsEnabled(LogLevel.Debug))
      logger.LogDebug("Successfully downloaded s3://{Bucket}/{Key}.", bucket, key);
  }

  public static void DownloadFailed(this ILogger logger, Exception ex, string bucket, string key)
  {
    if (logger.IsEnabled(LogLevel.Error))
      logger.LogError(ex, "AWS SDK Error during download from s3://{Bucket}/{Key}", bucket, key);
  }

  public static void DeletedObject(this ILogger logger, string bucket, string key)
  {
    if (logger.IsEnabled(LogLevel.Debug))
      logger.LogDebug("Successfully deleted s3://{Bucket}/{Key}.", bucket, key);
  }

  public static void DeleteFailed(this ILogger logger, Exception ex, string bucket, string key)
  {
    if (logger.IsEnabled(LogLevel.Error))
      logger.LogError(ex, "AWS SDK Error during delete of s3://{Bucket}/{Key}", bucket, key);
  }

  public static void DeletedMultipleObjects(this ILogger logger, int count, string bucketName)
  {
    if (logger.IsEnabled(LogLevel.Information))
      logger.LogInformation("Successfully deleted {Count} objects from bucket {BucketName}.", count, bucketName);
  }

  public static void ClearingBucket(this ILogger logger, string bucketName)
  {
    if (logger.IsEnabled(LogLevel.Information))
      logger.LogInformation("Attempting to clear all objects from bucket: {BucketName}", bucketName);
  }

  public static void ClearBucketListFailed(this ILogger logger, Exception ex, string bucketName)
  {
    if (logger.IsEnabled(LogLevel.Error))
      logger.LogError(ex, "Failed to list objects while clearing bucket {BucketName}", bucketName);
  }

  public static void ClearedBucket(this ILogger logger, string bucketName, long ops)
  {
    if (logger.IsEnabled(LogLevel.Information))
      logger.LogInformation("Successfully cleared bucket {BucketName}, consuming {Ops} Class A operations.", bucketName, ops);
  }

  public static void ListedObjects(this ILogger logger, int count, string bucket, string? prefix)
  {
    if (logger.IsEnabled(LogLevel.Debug))
      logger.LogDebug("Successfully listed {Count} objects in s3://{Bucket} with prefix {Prefix}.", count, bucket, prefix);
  }

  public static void ListObjectsFailed(this ILogger logger, Exception ex, string bucket, string? prefix)
  {
    if (logger.IsEnabled(LogLevel.Error))
      logger.LogError(ex, "AWS SDK Error while listing s3://{Bucket}/{Prefix}", bucket, prefix);
  }

  public static void InitiatedMultipartUpload(this ILogger logger, string bucket, string key, string uploadId)
  {
    if (logger.IsEnabled(LogLevel.Debug))
      logger.LogDebug("Initiated multipart upload for s3://{Bucket}/{Key} with UploadId {UploadId}", bucket, key, uploadId);
  }

  public static void InitiateMultipartUploadFailed(this ILogger logger, Exception ex, string bucket, string key)
  {
    if (logger.IsEnabled(LogLevel.Error))
      logger.LogError(ex, "Failed to initiate multipart upload for s3://{Bucket}/{Key}", bucket, key);
  }

  public static void CompletedMultipartUpload(this ILogger logger, string bucket, string key)
  {
    if (logger.IsEnabled(LogLevel.Debug))
      logger.LogDebug("Successfully completed multipart upload for s3://{Bucket}/{Key}", bucket, key);
  }

  public static void CompleteMultipartUploadFailed(this ILogger logger, Exception ex, string bucket, string key)
  {
    if (logger.IsEnabled(LogLevel.Error))
      logger.LogError(ex, "Failed to complete multipart upload for s3://{Bucket}/{Key}", bucket, key);
  }

  public static void AbortedMultipartUpload(this ILogger logger, string uploadId)
  {
    if (logger.IsEnabled(LogLevel.Information))
      logger.LogInformation("Successfully aborted multipart upload {UploadId}", uploadId);
  }

  public static void AbortMultipartUploadFailed(this ILogger logger, Exception ex, string uploadId)
  {
    if (logger.IsEnabled(LogLevel.Error))
      logger.LogError(ex, "Failed to abort multipart upload {UploadId}", uploadId);
  }

  public static void BatchDeleteFailedContinueOnError(this ILogger logger, Exception ex, string bucketName)
  {
    if (logger.IsEnabled(LogLevel.Warning))
      logger.LogWarning(ex, "A batch delete failed for bucket {BucketName}. Adding all keys from batch to failed list.", bucketName);
  }

  public static void BatchDeleteFailedStopOnError(this ILogger logger, Exception ex, string bucketName)
  {
    if (logger.IsEnabled(LogLevel.Error))
      logger.LogError(ex, "A batch delete failed for bucket {BucketName} and continueOnError is false.", bucketName);
  }

  public static void ClearBucketDeleteBatchFailedFull(this ILogger logger, string bucketName)
  {
    if (logger.IsEnabled(LogLevel.Warning))
      logger.LogWarning(
        "Unable to delete any objects in the current batch for bucket {BucketName}. Aborting clear operation to prevent an infinite loop.",
        bucketName);
  }

  public static void ClearBucketDeleteFailedStopOnError(this ILogger logger, Exception ex, string bucketName)
  {
    if (logger.IsEnabled(LogLevel.Error))
      logger.LogError(ex, "Failed to delete a batch of objects while clearing bucket {BucketName} and continueOnError is false.", bucketName);
  }

  public static void ClearBucketDeleteBatchFailedPartial(this ILogger logger, Exception ex, int count, string bucketName)
  {
    if (logger.IsEnabled(LogLevel.Warning))
      logger.LogWarning(ex,
        "Failed to delete a batch of {Count} objects while clearing bucket {BucketName}. Continuing because continueOnError is true.",
        count, bucketName);
  }

  public static void ListPartsFailed(this ILogger logger, Exception ex, string uploadId)
  {
    if (logger.IsEnabled(LogLevel.Error))
      logger.LogError(ex, "Failed to list parts for upload {UploadId}", uploadId);
  }

  public static void PresignedUrlGenerationFailed(this ILogger logger, Exception ex, string key, string bucket)
  {
    if (logger.IsEnabled(LogLevel.Error))
      logger.LogError(ex, "Failed to generate a presigned URL for Key={Key} in Bucket={Bucket}", key, bucket);
  }

  public static void ListPartsPaginationInconsistency(this ILogger logger, string uploadId)
  {
    if (logger.IsEnabled(LogLevel.Critical))
      logger.LogCritical(
        "Inconsistent pagination from R2 for upload {UploadId}: IsTruncated is true, but NextPartNumberMarker is null. Aborting to prevent infinite loop.",
        uploadId);
  }

  #endregion

#endif
}
