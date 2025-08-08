namespace Cloudflare.NET.Tests;

using System.Runtime.CompilerServices;

public static class ModuleInitializer
{
  #region Methods

  [ModuleInitializer]
  public static void Initialize()
  {
    // The shared initializer in Cloudflare.NET.Tests.Shared.dll is run automatically by the runtime.
    // No explicit call is needed.

    // Enable Verify to handle HTTP content and requests. This is specific to Cloudflare.NET.Tests.
    VerifyHttp.Initialize();
  }

  #endregion
}
