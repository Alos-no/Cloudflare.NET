namespace Cloudflare.NET.R2.Models;

using Amazon.S3.Model;

/// <summary>
///   One page of an R2 prefix listing: the objects this <c>ListObjectsV2</c> call returned, plus the token that
///   fetches the next page.
/// </summary>
/// <remarks>
///   The caller-driven counterpart of <see cref="IR2Client.ListObjectsAsync" />, which walks every page internally.
///   A caller that must bound how many pages it reads per run, and resume later, needs one page and a token instead
///   of the whole prefix.
/// </remarks>
/// <param name="Objects">The objects in this page, in the order R2 returned them (ordinal by key).</param>
/// <param name="NextContinuationToken">
///   The token to pass on the next call to continue the listing, or <c>null</c> when this page is the last one.
/// </param>
/// <param name="IsTruncated">
///   <c>true</c> when R2 has more objects under the prefix than this page carries.
/// </param>
public record R2ObjectPage(
  IReadOnlyList<S3Object> Objects,
  string?                 NextContinuationToken,
  bool                    IsTruncated
);
