namespace Cloudflare.NET.R2.Models;

/// <summary>Represents a single part that has been uploaded as part of a multipart upload.</summary>
/// <param name="PartNumber">The part number.</param>
/// <param name="ETag">The ETag of the uploaded part.</param>
/// <param name="Size">The size of the part in bytes.</param>
/// <param name="LastModified">The date the part was last modified.</param>
public record ListedPart(
  int?      PartNumber,
  string    ETag,
  long?     Size,
  DateTime? LastModified
);
