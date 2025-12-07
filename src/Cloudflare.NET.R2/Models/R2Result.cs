namespace Cloudflare.NET.R2.Models;

/// <summary>Represents the immutable, accumulated metrics of one or more R2 operations.</summary>
/// <param name="ClassAOperations">The number of billable Class A operations (e.g., writes, lists).</param>
/// <param name="ClassBOperations">The number of billable Class B operations (e.g., reads).</param>
/// <param name="IngressBytes">The number of bytes uploaded to R2.</param>
/// <param name="EgressBytes">The number of bytes downloaded from R2.</param>
public record R2Result(
  long ClassAOperations = 0,
  long ClassBOperations = 0,
  long IngressBytes     = 0,
  long EgressBytes      = 0
)
{
  /// <summary>Combines two R2Result instances by summing their metrics.</summary>
  /// <param name="a">The first result.</param>
  /// <param name="b">The second result.</param>
  /// <returns>A new R2Result with the accumulated metrics.</returns>
  public static R2Result operator +(R2Result a, R2Result b)
  {
    return new R2Result(
      a.ClassAOperations + b.ClassAOperations,
      a.ClassBOperations + b.ClassBOperations,
      a.IngressBytes + b.IngressBytes,
      a.EgressBytes + b.EgressBytes
    );
  }
}

/// <summary>Represents the result of an R2 operation that returns a data payload in addition to billable metrics.</summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
/// <param name="Data">The data payload returned by the operation.</param>
/// <param name="Metrics">The billable metrics for the operation.</param>
public record R2Result<T>(
  T        Data,
  R2Result Metrics
);
