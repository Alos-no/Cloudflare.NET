namespace Cloudflare.NET.R2.Models;

/// <summary>
///   Represents the response from creating a presigned POST URL, which includes the URL to target and the form
///   fields that must be included in the multipart/form-data request.
/// </summary>
/// <param name="Url">The URL to which the POST request should be sent.</param>
/// <param name="Fields">A dictionary of form fields that must be included in the POST request.</param>
public record PresignedPostResponse(
  string                              Url,
  IReadOnlyDictionary<string, string> Fields
);
