namespace Cloudflare.NET.R2;

using Amazon.S3.Model;
using Exceptions;
using Models;

/// <summary>
///   Defines the contract for a client that interacts with Cloudflare R2's S3-compatible API, with robust error
///   handling and metric reporting.
/// </summary>
public interface IR2Client
{
  /// <summary>
  ///   Uploads a file from a local path, automatically choosing between a single PUT request or a multipart upload
  ///   based on the file size.
  /// </summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="filePath">The path to the local file to upload.</param>
  /// <param name="partSize">
  ///   The desired size in bytes for each part in a multipart upload. If null, a sensible default is
  ///   used. The value is clamped between 5MiB and 5GiB.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the metrics of the operation.</returns>
  /// <exception cref="ArgumentException">Thrown if the file size exceeds R2's 5 TiB limit.</exception>
  /// <exception cref="CloudflareR2OperationException">Thrown if the upload fails.</exception>
  /// <exception cref="FileNotFoundException">Thrown if the specified <paramref name="filePath" /> does not exist.</exception>
  Task<R2Result> UploadAsync(string            bucketName,
                             string            objectKey,
                             string            filePath,
                             long?             partSize          = null,
                             CancellationToken cancellationToken = default);

  /// <summary>
  ///   Uploads a file from a local path, automatically choosing between a single PUT request or a multipart upload
  ///   based on the file size, and records the object's content type.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Passing <see langword="null" /> or a blank <paramref name="contentType" /> leaves the property unset, so R2
  ///     applies its own default and this method behaves exactly like
  ///     <see cref="UploadAsync(string,string,string,long?,CancellationToken)" />. A non-blank value is applied
  ///     verbatim; the type is never inferred from the file extension, the bytes, or the object key. When the file is
  ///     large enough to go multipart, the type is recorded on the initiate request, the only place S3 reads the
  ///     assembled object's <c>Content-Type</c> from.
  ///   </para>
  ///   <para>
  ///     A <paramref name="checksum" /> binds the upload to a digest of the whole object: R2 hashes the arriving
  ///     bytes and fails the upload with <c>BadDigest</c>, storing nothing, when they do not hash to the stated
  ///     digest. Because the digest covers the whole object while a multipart upload is verified per part, a checksum
  ///     is only accepted for files small enough for a single PUT; a multipart-sized file with a checksum throws
  ///     before anything is sent.
  ///   </para>
  /// </remarks>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="filePath">The path to the local file to upload.</param>
  /// <param name="partSize">
  ///   The desired size in bytes for each part in a multipart upload. If null, a sensible default is
  ///   used. The value is clamped between 5MiB and 5GiB.
  /// </param>
  /// <param name="contentType">The MIME type to record for the object, for example <c>image/webp</c>.</param>
  /// <param name="checksum">An optional digest of the whole object's bytes for R2 to verify on a single-part upload.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the metrics of the operation.</returns>
  /// <exception cref="ArgumentException">
  ///   Thrown if the file size exceeds R2's 5 TiB limit, or if a <paramref name="checksum" /> accompanies a file
  ///   large enough to go multipart.
  /// </exception>
  /// <exception cref="CloudflareR2OperationException">Thrown if the upload fails.</exception>
  /// <exception cref="FileNotFoundException">Thrown if the specified <paramref name="filePath" /> does not exist.</exception>
  Task<R2Result> UploadAsync(string            bucketName,
                             string            objectKey,
                             string            filePath,
                             long?             partSize,
                             string?           contentType,
                             UploadChecksum?   checksum          = null,
                             CancellationToken cancellationToken = default);

  /// <summary>
  ///   Uploads a file from a stream, automatically choosing between a single PUT request or a multipart upload. If
  ///   the stream is seekable, the choice is based on its length. If it is not seekable, it will always attempt a multipart
  ///   upload.
  /// </summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="fileStream">The stream to upload.</param>
  /// <param name="partSize">
  ///   The desired size in bytes for each part in a multipart upload. If null, a sensible default is
  ///   used. The value is clamped between 5MiB and 5GiB.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the metrics of the operation.</returns>
  /// <exception cref="ArgumentException">Thrown if the stream is seekable and its length exceeds R2's 5 TiB limit.</exception>
  /// <exception cref="CloudflareR2OperationException">Thrown if the upload fails.</exception>
  /// <exception cref="NotSupportedException">Thrown if a multipart upload is attempted but the stream is not seekable.</exception>
  Task<R2Result> UploadAsync(string            bucketName,
                             string            objectKey,
                             Stream            fileStream,
                             long?             partSize          = null,
                             CancellationToken cancellationToken = default);

  /// <summary>
  ///   Uploads a file from a stream, automatically choosing between a single PUT request or a multipart upload, and
  ///   records the object's content type. If the stream is seekable, the choice is based on its length. If it is not
  ///   seekable, it will always attempt a multipart upload.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Passing <see langword="null" /> or a blank <paramref name="contentType" /> leaves the property unset, so R2
  ///     applies its own default and this method behaves exactly like
  ///     <see cref="UploadAsync(string,string,Stream,long?,CancellationToken)" />. A non-blank value is applied
  ///     verbatim; the type is never inferred from the stream contents or the object key. When the stream goes
  ///     multipart, the type is recorded on the initiate request, the only place S3 reads the assembled object's
  ///     <c>Content-Type</c> from.
  ///   </para>
  ///   <para>
  ///     A <paramref name="checksum" /> binds the upload to a digest of the whole object: R2 hashes the arriving
  ///     bytes and fails the upload with <c>BadDigest</c>, storing nothing, when they do not hash to the stated
  ///     digest. Because the digest covers the whole object while a multipart upload is verified per part, a checksum
  ///     is only accepted when the stream takes the single PUT path; a stream that would go multipart (too large, or
  ///     not seekable) with a checksum throws before anything is sent.
  ///   </para>
  /// </remarks>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="fileStream">The stream to upload.</param>
  /// <param name="partSize">
  ///   The desired size in bytes for each part in a multipart upload. If null, a sensible default is
  ///   used. The value is clamped between 5MiB and 5GiB.
  /// </param>
  /// <param name="contentType">The MIME type to record for the object, for example <c>image/webp</c>.</param>
  /// <param name="checksum">An optional digest of the whole object's bytes for R2 to verify on a single-part upload.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the metrics of the operation.</returns>
  /// <exception cref="ArgumentException">
  ///   Thrown if the stream is seekable and its length exceeds R2's 5 TiB limit, or if a <paramref name="checksum" />
  ///   accompanies a stream that would go multipart.
  /// </exception>
  /// <exception cref="CloudflareR2OperationException">Thrown if the upload fails.</exception>
  /// <exception cref="NotSupportedException">Thrown if a multipart upload is attempted but the stream is not seekable.</exception>
  Task<R2Result> UploadAsync(string            bucketName,
                             string            objectKey,
                             Stream            fileStream,
                             long?             partSize,
                             string?           contentType,
                             UploadChecksum?   checksum          = null,
                             CancellationToken cancellationToken = default);

  /// <summary>
  ///   Uploads a file using a single PUT request. This method provides direct control and should be used when the
  ///   automatic selection in <see cref="UploadAsync(string,string,string,long?,CancellationToken)" /> is not desired.
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
  ///   Uploads a file using a single PUT request, recording the object's content type and optionally binding the
  ///   upload to a checksum.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Passing <see langword="null" /> or a blank <paramref name="contentType" /> leaves the property unset, so R2
  ///     applies its own default and this method behaves exactly like
  ///     <see cref="UploadSinglePartAsync(string,string,string,CancellationToken)" />. A non-blank value is applied
  ///     verbatim; the type is never inferred from the file extension, the bytes, or the object key.
  ///   </para>
  ///   <para>
  ///     A <paramref name="checksum" /> binds the upload to a digest of the object's bytes. R2 hashes what actually
  ///     arrives and fails the upload with <c>BadDigest</c>, storing nothing, when the bytes do not hash to the
  ///     stated digest (verified against live R2 for every <see cref="R2ChecksumAlgorithm" /> on single-part
  ///     uploads).
  ///   </para>
  /// </remarks>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="filePath">The path to the local file to upload.</param>
  /// <param name="contentType">The MIME type to record for the object, for example <c>image/webp</c>.</param>
  /// <param name="checksum">An optional digest of the object's bytes for R2 to verify.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the metrics of the operation.</returns>
  /// <exception cref="ArgumentException">Thrown if the file size exceeds the 5 GiB single-part upload limit.</exception>
  /// <exception cref="CloudflareR2OperationException">Thrown if the upload fails.</exception>
  Task<R2Result> UploadSinglePartAsync(string            bucketName,
                                       string            objectKey,
                                       string            filePath,
                                       string?           contentType,
                                       UploadChecksum?   checksum          = null,
                                       CancellationToken cancellationToken = default);

  /// <summary>
  ///   Uploads a file from a stream using a single PUT request. This method provides direct control and should be
  ///   used when the automatic selection in <see cref="UploadAsync(string,string,Stream,long?,CancellationToken)" /> is not
  ///   desired.
  /// </summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="inputStream">The stream to upload.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the metrics of the operation.</returns>
  /// <exception cref="ArgumentException">
  ///   Thrown if the stream is seekable and its length exceeds the 5 GiB single-part
  ///   upload limit.
  /// </exception>
  /// <exception cref="CloudflareR2OperationException">Thrown if the upload fails.</exception>
  Task<R2Result> UploadSinglePartAsync(string            bucketName,
                                       string            objectKey,
                                       Stream            inputStream,
                                       CancellationToken cancellationToken = default);

  /// <summary>
  ///   Uploads a file from a stream using a single PUT request, recording the object's content type and optionally
  ///   binding the upload to a checksum.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Passing <see langword="null" /> or a blank <paramref name="contentType" /> leaves the property unset, so R2
  ///     applies its own default and this method behaves exactly like
  ///     <see cref="UploadSinglePartAsync(string,string,Stream,CancellationToken)" />. A non-blank value is applied
  ///     verbatim; the type is never inferred from the stream contents or the object key.
  ///   </para>
  ///   <para>
  ///     A <paramref name="checksum" /> binds the upload to a digest of the object's bytes. R2 hashes what actually
  ///     arrives and fails the upload with <c>BadDigest</c>, storing nothing, when the bytes do not hash to the
  ///     stated digest (verified against live R2 for every <see cref="R2ChecksumAlgorithm" /> on single-part
  ///     uploads).
  ///   </para>
  /// </remarks>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="inputStream">The stream to upload.</param>
  /// <param name="contentType">The MIME type to record for the object, for example <c>image/webp</c>.</param>
  /// <param name="checksum">An optional digest of the object's bytes for R2 to verify.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the metrics of the operation.</returns>
  /// <exception cref="ArgumentException">
  ///   Thrown if the stream is seekable and its length exceeds the 5 GiB single-part
  ///   upload limit.
  /// </exception>
  /// <exception cref="CloudflareR2OperationException">Thrown if the upload fails.</exception>
  Task<R2Result> UploadSinglePartAsync(string            bucketName,
                                       string            objectKey,
                                       Stream            inputStream,
                                       string?           contentType,
                                       UploadChecksum?   checksum          = null,
                                       CancellationToken cancellationToken = default);

  /// <summary>Uploads a file using a multipart upload. This method provides direct control over multipart uploads.</summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="filePath">The path to the local file to upload.</param>
  /// <param name="partSize">
  ///   The desired size in bytes for each part. If null, a sensible default is used. The value is
  ///   clamped between 5MiB and 5GiB.
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

  /// <summary>Uploads a file using a multipart upload, recording the content type the assembled object will carry.</summary>
  /// <remarks>
  ///   <para>
  ///     The type is recorded on the initiate request, because S3 reads the assembled object's <c>Content-Type</c>
  ///     from the initiate call and never from the individual parts.
  ///   </para>
  ///   <para>
  ///     Passing <see langword="null" /> or a blank <paramref name="contentType" /> leaves the property unset, so R2
  ///     applies its own default and this method behaves exactly like
  ///     <see cref="UploadMultipartAsync(string,string,string,long?,CancellationToken)" />. A non-blank value is
  ///     applied verbatim; the type is never inferred from the file extension, the bytes, or the object key.
  ///   </para>
  /// </remarks>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="filePath">The path to the local file to upload.</param>
  /// <param name="partSize">
  ///   The desired size in bytes for each part. If null, a sensible default is used. The value is
  ///   clamped between 5MiB and 5GiB.
  /// </param>
  /// <param name="contentType">The MIME type to record for the assembled object, for example <c>image/webp</c>.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the aggregate metrics of the operation.</returns>
  /// <exception cref="ArgumentException">Thrown if the file size exceeds R2's 5 TiB limit.</exception>
  /// <exception cref="CloudflareR2OperationException">Thrown if any part of the upload fails.</exception>
  Task<R2Result> UploadMultipartAsync(string            bucketName,
                                      string            objectKey,
                                      string            filePath,
                                      long?             partSize,
                                      string?           contentType,
                                      CancellationToken cancellationToken = default);

  /// <summary>
  ///   Uploads a file from a stream using a multipart upload. The stream must be seekable. This method provides
  ///   direct control over multipart uploads.
  /// </summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="inputStream">The stream to upload. Must be seekable.</param>
  /// <param name="partSize">
  ///   The desired size in bytes for each part. If null, a sensible default is used. The value is
  ///   clamped between 5MiB and 5GiB.
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

  /// <summary>
  ///   Uploads a file from a stream using a multipart upload, recording the content type the assembled object will
  ///   carry. The stream must be seekable.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The type is recorded on the initiate request, because S3 reads the assembled object's <c>Content-Type</c>
  ///     from the initiate call and never from the individual parts.
  ///   </para>
  ///   <para>
  ///     Passing <see langword="null" /> or a blank <paramref name="contentType" /> leaves the property unset, so R2
  ///     applies its own default and this method behaves exactly like
  ///     <see cref="UploadMultipartAsync(string,string,Stream,long?,CancellationToken)" />. A non-blank value is
  ///     applied verbatim; the type is never inferred from the stream contents or the object key.
  ///   </para>
  /// </remarks>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key (path) for the object in the bucket.</param>
  /// <param name="inputStream">The stream to upload. Must be seekable.</param>
  /// <param name="partSize">
  ///   The desired size in bytes for each part. If null, a sensible default is used. The value is
  ///   clamped between 5MiB and 5GiB.
  /// </param>
  /// <param name="contentType">The MIME type to record for the assembled object, for example <c>image/webp</c>.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the aggregate metrics of the operation.</returns>
  /// <exception cref="ArgumentException">Thrown if the stream length exceeds R2's 5 TiB limit.</exception>
  /// <exception cref="CloudflareR2OperationException">Thrown if any part of the upload fails.</exception>
  /// <exception cref="NotSupportedException">Thrown if the provided stream is not seekable.</exception>
  Task<R2Result> UploadMultipartAsync(string            bucketName,
                                      string            objectKey,
                                      Stream            inputStream,
                                      long?             partSize,
                                      string?           contentType,
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
  /// <returns>An <see cref="R2Result" /> detailing the metrics of the delete operation (should be zero).</returns>
  /// <exception cref="CloudflareR2OperationException">Thrown if the delete fails.</exception>
  Task<R2Result> DeleteObjectAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);

  /// <summary>Deletes multiple objects from an R2 bucket in batches. This is a free operation.</summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKeys">An enumeration of object keys to delete.</param>
  /// <param name="continueOnError">
  ///   If true, the operation will continue even if some batches fail, throwing an exception
  ///   only at the end. If false, it will stop on the first error.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the total metrics of all attempted operations (should be zero).</returns>
  /// <exception cref="CloudflareR2BatchException{T}">
  ///   Thrown if one or more objects could not be deleted. Contains a list of
  ///   the failed keys.
  /// </exception>
  Task<R2Result> DeleteObjectsAsync(string              bucketName,
                                    IEnumerable<string> objectKeys,
                                    bool                continueOnError   = true,
                                    CancellationToken   cancellationToken = default);

  /// <summary>Clears all objects from an R2 bucket by repeatedly listing and deleting them in batches.</summary>
  /// <remarks>
  ///   <para>
  ///     Deleting every object does not by itself leave the bucket deletable. A multipart upload that was
  ///     started and never completed or aborted keeps holding storage that object listing never reports, and
  ///     Cloudflare then refuses to delete the bucket, reporting that it is not empty even though no object is
  ///     visible. <paramref name="abortIncompleteMultipartUploads" /> controls whether this method also finds
  ///     those uploads and aborts them.
  ///   </para>
  /// </remarks>
  /// <param name="bucketName">The name of the bucket to clear.</param>
  /// <param name="continueOnError">If true, the operation will continue even if some delete batches fail.</param>
  /// <param name="abortIncompleteMultipartUploads">
  ///   When <c>true</c> (the default), the method finds every multipart upload left open in the bucket and
  ///   aborts each one after the objects are deleted, at the cost of one extra billable Class A operation for
  ///   the discovery call. Aborting an upload is itself free. Set this to <c>false</c> to delete only objects.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the total metrics of all list, delete, and abort operations.</returns>
  /// <exception cref="CloudflareR2BatchException{T}">Thrown if some objects could not be deleted.</exception>
  /// <exception cref="CloudflareR2ListException{T}">Thrown if listing objects or open multipart uploads fails.</exception>
  Task<R2Result> ClearBucketAsync(string            bucketName,
                                  bool              continueOnError                 = true,
                                  bool              abortIncompleteMultipartUploads = true,
                                  CancellationToken cancellationToken               = default);

  /// <summary>Lists all objects in an R2 bucket, optionally filtered by a prefix, handling pagination automatically.</summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="prefix">The prefix to filter the object listing by.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A result object containing a read-only list of all <see cref="S3Object" /> items found and aggregated metrics.</returns>
  /// <exception cref="CloudflareR2ListException{T}">
  ///   Thrown if listing fails mid-stream, containing any objects fetched
  ///   successfully.
  /// </exception>
  Task<R2Result<IReadOnlyList<S3Object>>> ListObjectsAsync(string            bucketName,
                                                           string?           prefix,
                                                           CancellationToken cancellationToken = default);

  /// <summary>
  ///   Lists ONE page of objects under a prefix and returns the token that fetches the next page, leaving the walk
  ///   across pages to the caller.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     <see cref="ListObjectsAsync" /> pages internally and returns the whole prefix. A caller that must bound how
  ///     many pages it reads in one run, and resume the walk on a later run, needs the page and the token instead.
  ///   </para>
  ///   <para><paramref name="maxKeys" /> is clamped to the S3 page ceiling of 1000; a value at or below zero requests 1000.</para>
  /// </remarks>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="prefix">The prefix to filter the object listing by.</param>
  /// <param name="maxKeys">The maximum number of keys to return in this page (clamped to 1000).</param>
  /// <param name="continuationToken">
  ///   The token returned by the previous page, or <c>null</c> to start from the beginning of the prefix.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A result object containing one <see cref="R2ObjectPage" /> and the metrics of the single list call.</returns>
  /// <exception cref="CloudflareR2ListException{T}">Thrown if listing fails.</exception>
  Task<R2Result<R2ObjectPage>> ListObjectsPageAsync(string            bucketName,
                                                    string?           prefix,
                                                    int               maxKeys,
                                                    string?           continuationToken,
                                                    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Lists every multipart upload initiated under a prefix that has neither completed nor been aborted, handling
  ///   pagination internally.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     An open multipart upload's parts are invisible to <see cref="ListObjectsAsync" /> until the upload completes,
  ///     so draining the objects under a prefix can never end one. A teardown that must leave the prefix genuinely
  ///     empty discovers the open uploads here and aborts each one.
  ///   </para>
  ///   <para>
  ///     Pass the <c>UploadId</c> from each returned <see cref="MultipartUpload" /> to
  ///     <see cref="AbortMultipartUploadAsync" />. R2 has been observed to report an upload identifier here that
  ///     differs from the one it returned when the upload was started, so the value from this listing is the one to
  ///     use for the abort.
  ///   </para>
  ///   <para>
  ///     <see cref="ClearBucketAsync" /> performs this discovery and abort itself unless the caller opts out.
  ///   </para>
  /// </remarks>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="prefix">The key prefix to restrict discovery to, or <c>null</c> for the whole bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A result object containing every open multipart upload under the prefix and the aggregated metrics.</returns>
  /// <exception cref="CloudflareR2ListException{T}">
  ///   Thrown if listing fails mid-stream, containing any uploads fetched successfully.
  /// </exception>
  Task<R2Result<IReadOnlyList<MultipartUpload>>> ListMultipartUploadsAsync(string            bucketName,
                                                                          string?           prefix,
                                                                          CancellationToken cancellationToken = default);

  /// <summary>Lists the parts that have been uploaded for a specific multipart upload, transparently handling pagination.</summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key of the object.</param>
  /// <param name="uploadId">The ID of the multipart upload.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A result object containing the full list of parts and the aggregated metrics.</returns>
  /// <exception cref="CloudflareR2ListException{T}">
  ///   Thrown if listing fails mid-stream, containing any parts fetched
  ///   successfully.
  /// </exception>
  Task<R2Result<IReadOnlyList<ListedPart>>> ListPartsAsync(string            bucketName,
                                                           string            objectKey,
                                                           string            uploadId,
                                                           CancellationToken cancellationToken = default);

  /// <summary>Initiates a new multipart upload, letting R2 choose the assembled object's content type.</summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key for the object in the bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A result object containing the UploadId and operation metrics.</returns>
  /// <exception cref="CloudflareR2OperationException">Thrown if the operation fails.</exception>
  Task<R2Result<string>> InitiateMultipartUploadAsync(string            bucketName,
                                                      string            objectKey,
                                                      CancellationToken cancellationToken = default);

  /// <summary>Initiates a new multipart upload and records the content type the assembled object will carry.</summary>
  /// <remarks>
  ///   <para>
  ///     S3 reads the finished object's <c>Content-Type</c> from this request and never from the individual parts.
  ///     A caller that hands out presigned part URLs therefore has no later opportunity to set it: the content type
  ///     must be supplied here, when the upload starts.
  ///   </para>
  ///   <para>
  ///     Passing <see langword="null" /> or a blank string leaves the property unset, so R2 applies its own default
  ///     and this method behaves exactly like
  ///     <see cref="InitiateMultipartUploadAsync(string,string,CancellationToken)" />.
  ///   </para>
  /// </remarks>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key for the object in the bucket.</param>
  /// <param name="contentType">The MIME type to record for the assembled object, for example <c>application/pdf</c>.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A result object containing the UploadId and operation metrics.</returns>
  /// <exception cref="CloudflareR2OperationException">Thrown if the operation fails.</exception>
  Task<R2Result<string>> InitiateMultipartUploadAsync(string            bucketName,
                                                      string            objectKey,
                                                      string?           contentType,
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

  /// <summary>Aborts a multipart upload, deleting any parts that have already been uploaded. This is a free operation.</summary>
  /// <param name="bucketName">The name of the target bucket.</param>
  /// <param name="objectKey">The key of the object.</param>
  /// <param name="uploadId">The ID of the multipart upload to abort.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An <see cref="R2Result" /> detailing the metrics of the abort operation (should be zero).</returns>
  /// <exception cref="CloudflareR2OperationException">Thrown if the operation fails.</exception>
  Task<R2Result> AbortMultipartUploadAsync(string            bucketName,
                                           string            objectKey,
                                           string            uploadId,
                                           CancellationToken cancellationToken = default);

  /// <summary>
  ///   Creates a presigned PUT URL that allows for uploading a file directly to R2, enforcing constraints via signed
  ///   headers.
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

  /// <summary>
  ///   Creates a presigned GET URL that allows for downloading an object directly from R2, optionally enforcing
  ///   response header overrides via the signed query string.
  /// </summary>
  /// <param name="bucketName">The name of the bucket holding the object.</param>
  /// <param name="request">A request object defining the key, validity window, and response header overrides.</param>
  /// <returns>A string containing the generated presigned URL.</returns>
  /// <exception cref="CloudflareR2OperationException">Thrown if URL generation fails.</exception>
  string CreatePresignedGetUrl(string bucketName, PresignedGetRequest request);

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
