namespace Cloudflare.NET.R2;

using Amazon.S3.Model;
using Exceptions;
using Models;

/// <summary>
///   Defines the contract for a client that interacts with Cloudflare R2's S3-compatible
///   API, with robust error handling and metric reporting.
/// </summary>
public interface IR2Client
{
  /// <summary>
  ///   Uploads a file from a local path, automatically choosing between a single PUT
  ///   request or a multipart upload based on the file size.
  /// </summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="filePath">The path to the local file to upload.</param>
  /// <param name="partSize">
  ///   The desired size in bytes for each part in a multipart upload. If null,
  ///   a sensible default is used. The value is clamped between 5MiB and 5GiB.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the metrics of the operation.</returns>
  /// <exception cref="ArgumentException">Thrown if the file size exceeds R2's 5 TiB limit.</exception>
  /// <exception cref="CloudflareR2OperationException">Thrown if the upload fails.</exception>
  /// <exception cref="FileNotFoundException">
  ///   Thrown if the specified <paramref name="filePath" />
  ///   does not exist.
  /// </exception>
  Task<R2Result> UploadAsync(string            bucketName,
                             string            objectKey,
                             string            filePath,
                             long?             partSize          = null,
                             CancellationToken cancellationToken = default);

  /// <summary>
  ///   Uploads a file from a stream, automatically choosing between a single PUT request or
  ///   a multipart upload. If the stream is seekable, the choice is based on its length.
  ///   If it is not seekable, it will always attempt a multipart upload.
  /// </summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="fileStream">The stream to upload.</param>
  /// <param name="partSize">
  ///   The desired size in bytes for each part in a multipart upload. If null,
  ///   a sensible default is used. The value is clamped between 5MiB and 5GiB.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the metrics of the operation.</returns>
  /// <exception cref="ArgumentException">Thrown if the stream is seekable and its length exceeds R2's 5 TiB limit.</exception>
  /// <exception cref="CloudflareR2OperationException">Thrown if the upload fails.</exception>
  /// <exception cref="NotSupportedException">
  ///   Thrown if a multipart upload is attempted but the stream is not seekable.
  /// </exception>
  Task<R2Result> UploadAsync(string            bucketName,
                             string            objectKey,
                             Stream            fileStream,
                             long?             partSize          = null,
                             CancellationToken cancellationToken = default);

  /// <summary>
  ///   Uploads a file using a single PUT request. This method provides direct control and
  ///   should be used when the automatic selection in
  ///   <see cref="UploadAsync(string,string,string,long?,CancellationToken)" /> is not
  ///   desired.
  /// </summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="filePath">The path to the local file to upload.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the metrics of the operation.</returns>
  /// <exception cref="ArgumentException">Thrown if the file size exceeds the 5 GiB single-part upload limit.</exception>
  /// <exception cref="CloudflareR2OperationException">Thrown if the upload fails.</exception>
  Task<R2Result> UploadSinglePartAsync(string            bucketName,
                                       string            objectKey,
                                       string            filePath,
                                       CancellationToken cancellationToken = default);

  /// <summary>
  ///   Uploads a file from a stream using a single PUT request. This method provides direct
  ///   control and should be used when the automatic selection in
  ///   <see cref="UploadAsync(string,string,Stream,long?,CancellationToken)" /> is not desired.
  /// </summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="inputStream">The stream to upload.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the metrics of the operation.</returns>
  /// <exception cref="ArgumentException">Thrown if the stream is seekable and its length exceeds the 5 GiB single-part upload limit.</exception>
  /// <exception cref="CloudflareR2OperationException">Thrown if the upload fails.</exception>
  Task<R2Result> UploadSinglePartAsync(string            bucketName,
                                       string            objectKey,
                                       Stream            inputStream,
                                       CancellationToken cancellationToken = default);

  /// <summary>
  ///   Uploads a file using a multipart upload. This method provides direct control over
  ///   multipart uploads.
  /// </summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="filePath">The path to the local file to upload.</param>
  /// <param name="partSize">
  ///   The desired size in bytes for each part. If null, a sensible default is
  ///   used. The value is clamped between 5MiB and 5GiB.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the aggregate metrics of the operation.</returns>
  /// <exception cref="ArgumentException">Thrown if the file size exceeds R2's 5 TiB limit.</exception>
  /// <exception cref="CloudflareR2OperationException">Thrown if any part of the upload fails.</exception>
  Task<R2Result> UploadMultipartAsync(string            bucketName,
                                      string            objectKey,
                                      string            filePath,
                                      long?             partSize          = null,
                                      CancellationToken cancellationToken = default);

  /// <summary>
  ///   Uploads a file from a stream using a multipart upload. The stream must be seekable.
  ///   This method provides direct control over multipart uploads.
  /// </summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="inputStream">The stream to upload. Must be seekable.</param>
  /// <param name="partSize">
  ///   The desired size in bytes for each part. If null, a sensible default is
  ///   used. The value is clamped between 5MiB and 5GiB.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the aggregate metrics of the operation.</returns>
  /// <exception cref="ArgumentException">Thrown if the stream length exceeds R2's 5 TiB limit.</exception>
  /// <exception cref="CloudflareR2OperationException">Thrown if any part of the upload fails.</exception>
  /// <exception cref="NotSupportedException">Thrown if the provided stream is not seekable.</exception>
  Task<R2Result> UploadMultipartAsync(string            bucketName,
                                      string            objectKey,
                                      Stream            inputStream,
                                      long?             partSize          = null,
                                      CancellationToken cancellationToken = default);

  /// <summary>Downloads a file from an R2 bucket to a local file path.</summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key of the object to download.</param>
  /// <param name="downloadPath">The local path to save the downloaded file to.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the metrics, including egress bytes.</returns>
  /// <exception cref="CloudflareR2OperationException">Thrown if the download fails.</exception>
  Task<R2Result> DownloadFileAsync(string            bucketName,
                                   string            objectKey,
                                   string            downloadPath,
                                   CancellationToken cancellationToken = default);


  /// <summary>Downloads a file from an R2 bucket to a stream.</summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key of the object to download.</param>
  /// <param name="outputStream">The stream to write the downloaded data to.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the metrics, including egress bytes.</returns>
  /// <exception cref="CloudflareR2OperationException">Thrown if the download fails.</exception>
  Task<R2Result> DownloadFileAsync(string            bucketName,
                                   string            objectKey,
                                   Stream            outputStream,
                                   CancellationToken cancellationToken = default);

  /// <summary>Deletes a single object from an R2 bucket. This is a free operation.</summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key of the object to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   An <see cref="R2Result" /> detailing the metrics of the delete operation (should be
  ///   zero).
  /// </returns>
  /// <exception cref="CloudflareR2OperationException">Thrown if the delete fails.</exception>
  Task<R2Result> DeleteObjectAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);

  /// <summary>Deletes multiple objects from an R2 bucket in batches. This is a free operation.</summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKeys">An enumeration of object keys to delete.</param>
  /// <param name="continueOnError">
  ///   If true, the operation will continue even if some batches fail,
  ///   throwing an exception only at the end. If false, it will stop on the first error.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   An <see cref="R2Result" /> detailing the total metrics of all attempted operations
  ///   (should be zero).
  /// </returns>
  /// <exception cref="CloudflareR2BatchException{T}">
  ///   Thrown if one or more objects could not be
  ///   deleted. Contains a list of the failed keys.
  /// </exception>
  Task<R2Result> DeleteObjectsAsync(string              bucketName,
                                    IEnumerable<string> objectKeys,
                                    bool                continueOnError   = true,
                                    CancellationToken   cancellationToken = default);

  /// <summary>
  ///   Clears all objects from an R2 bucket by repeatedly listing and deleting them in
  ///   batches.
  /// </summary>
  /// <param name="bucketName">The name of the bucket to clear.</param>
  /// <param name="continueOnError">
  ///   If true, the operation will continue even if some delete batches
  ///   fail.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   An <see cref="R2Result" /> detailing the total metrics of all list and delete
  ///   operations.
  /// </returns>
  /// <exception cref="CloudflareR2BatchException{T}">Thrown if some objects could not be deleted.</exception>
  /// <exception cref="CloudflareR2ListException{T}">Thrown if listing objects fails.</exception>
  Task<R2Result> ClearBucketAsync(string            bucketName,
                                  bool              continueOnError   = true,
                                  CancellationToken cancellationToken = default);

  /// <summary>
  ///   Lists all objects in an R2 bucket, optionally filtered by a prefix, handling
  ///   pagination automatically.
  /// </summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="prefix">The prefix to filter the object listing by.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A result object containing a read-only list of all <see cref="S3Object" /> items
  ///   found and aggregated metrics.
  /// </returns>
  /// <exception cref="CloudflareR2ListException{T}">
  ///   Thrown if listing fails mid-stream, containing
  ///   any objects fetched successfully.
  /// </exception>
  Task<R2Result<IReadOnlyList<S3Object>>> ListObjectsAsync(string            bucketName,
                                                           string?           prefix,
                                                           CancellationToken cancellationToken = default);

  /// <summary>
  ///   Lists the parts that have been uploaded for a specific multipart upload,
  ///   transparently handling pagination.
  /// </summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key of the object.</param>
  /// <param name="uploadId">The ID of the multipart upload.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A result object containing the full list of parts and the aggregated metrics.</returns>
  /// <exception cref="CloudflareR2ListException{T}">
  ///   Thrown if listing fails mid-stream, containing
  ///   any parts fetched successfully.
  /// </exception>
  Task<R2Result<IReadOnlyList<ListedPart>>> ListPartsAsync(string            bucketName,
                                                           string            objectKey,
                                                           string            uploadId,
                                                           CancellationToken cancellationToken = default);

  /// <summary>Initiates a new multipart upload.</summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key for the object in the bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A result object containing the UploadId and operation metrics.</returns>
  /// <exception cref="CloudflareR2OperationException">Thrown if the operation fails.</exception>
  Task<R2Result<string>> InitiateMultipartUploadAsync(string            bucketName,
                                                      string            objectKey,
                                                      CancellationToken cancellationToken = default);

  /// <summary>Completes a multipart upload after all parts are uploaded.</summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key of the object.</param>
  /// <param name="uploadId">The ID of the multipart upload.</param>
  /// <param name="parts">A list of the part numbers and their corresponding ETags.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the metrics of the finalization operation.</returns>
  /// <exception cref="CloudflareR2OperationException">Thrown if the operation fails.</exception>
  Task<R2Result> CompleteMultipartUploadAsync(string                bucketName,
                                              string                objectKey,
                                              string                uploadId,
                                              IEnumerable<PartETag> parts,
                                              CancellationToken     cancellationToken = default);

  /// <summary>
  ///   Aborts a multipart upload, deleting any parts that have already been uploaded. This
  ///   is a free operation.
  /// </summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key of the object.</param>
  /// <param name="uploadId">The ID of the multipart upload to abort.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   An <see cref="R2Result" /> detailing the metrics of the abort operation (should be
  ///   zero).
  /// </returns>
  /// <exception cref="CloudflareR2OperationException">Thrown if the operation fails.</exception>
  Task<R2Result> AbortMultipartUploadAsync(string            bucketName,
                                           string            objectKey,
                                           string            uploadId,
                                           CancellationToken cancellationToken = default);

  /// <summary>
  ///   Creates a presigned PUT URL that allows for uploading a file directly to R2,
  ///   enforcing constraints via signed headers.
  /// </summary>
  /// <param name="bucketName">The name of the bucket where the upload will occur.</param>
  /// <param name="request">A request object defining the key and headers to enforce.</param>
  /// <returns>A string containing the generated presigned URL.</returns>
  /// <exception cref="CloudflareR2OperationException">Thrown if URL generation fails.</exception>
  string CreatePresignedPutUrl(string bucketName, PresignedPutRequest request);

  /// <summary>Creates a presigned URL for uploading a single part of a multipart upload.</summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="request">The parameters for the presigned part URL.</param>
  /// <returns>A string containing the generated presigned URL for the part.</returns>
  /// <exception cref="CloudflareR2OperationException">Thrown if URL generation fails.</exception>
  string CreatePresignedUploadPartUrl(string bucketName, PresignedUploadPartRequest request);

  /// <summary>Creates a batch of presigned URLs for uploading multiple parts of a multipart upload.</summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="request">The parameters for the presigned part URLs.</param>
  /// <returns>A dictionary mapping each part number to its generated presigned URL.</returns>
  /// <exception cref="CloudflareR2OperationException">Thrown if URL generation fails for any part.</exception>
  IReadOnlyDictionary<int, string> CreatePresignedUploadPartsUrls(string bucketName, PresignedUploadPartsRequest request);

#if false // There is currently a NRE in the AWS SDK when using CreatePresignedPostUrl with R2.
  /// <summary>Creates a presigned POST URL for browser-based uploads, with conditions.</summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="request">The parameters and conditions for the presigned POST.</param>
  /// <returns>A <see cref="PresignedPostResponse" /> containing the URL and required form fields.</returns>
  /// <exception cref="CloudflareR2OperationException">Thrown if URL generation fails.</exception>
  Task<PresignedPostResponse> CreatePresignedPostUrlAsync(string               bucketName,
                                                          PresignedPostRequest request);
#endif
}
