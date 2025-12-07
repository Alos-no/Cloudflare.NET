namespace Cloudflare.NET.R2.Logging;

using Microsoft.Extensions.Logging;

/// <summary>
///   Contains high-performance, source-generated logging definitions for the R2Client. Using the LoggerMessage
///   pattern avoids boxing and template parsing for hot-path logs.
/// </summary>
internal static partial class R2ClientLogs
{
  #region Methods

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
}
