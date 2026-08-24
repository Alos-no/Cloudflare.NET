namespace Cloudflare.NET.R2.Models;

/// <summary>Defines the parameters for creating a presigned GET URL, which allows for direct, time-limited downloads.</summary>
/// <param name="Key">The object key (path) of the file to be downloaded from the bucket.</param>
/// <param name="ExpiresAfter">The duration for which the presigned URL will be valid.</param>
/// <param name="ResponseContentType">
///   An optional <c>response-content-type</c> override. Because the override is part of the signed query string, R2
///   stamps it as the <c>Content-Type</c> header of its response, and the URL holder cannot alter it after signing.
/// </param>
/// <param name="ResponseContentDisposition">
///   An optional <c>response-content-disposition</c> override. Because the override is part of the signed query
///   string, R2 stamps it as the <c>Content-Disposition</c> header of its response (e.g. an <c>attachment</c>
///   directive carrying a save-as filename), and the URL holder cannot alter it after signing.
/// </param>
public record PresignedGetRequest(
  string   Key,
  TimeSpan ExpiresAfter,
  string?  ResponseContentType        = null,
  string?  ResponseContentDisposition = null
);
