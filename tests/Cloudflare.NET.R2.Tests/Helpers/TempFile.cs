namespace Cloudflare.NET.R2.Tests.Helpers;

/// <summary>
///   A helper class to create a temporary file with random data for testing uploads. The
///   file is automatically deleted when the object is disposed.
/// </summary>
public sealed class TempFile : IDisposable
{
  #region Constructors

  public TempFile(long sizeInBytes)
  {
    FileSize = sizeInBytes;
    FilePath = Path.GetTempFileName();
    var data = new byte[sizeInBytes];

    Random.Shared.NextBytes(data);

    File.WriteAllBytes(FilePath, data);
  }

  public void Dispose()
  {
    if (File.Exists(FilePath))
      File.Delete(FilePath);

    GC.SuppressFinalize(this);
  }

  #endregion

  #region Properties & Fields - Public

  public string FilePath { get; }
  public long   FileSize { get; }

  #endregion
}
