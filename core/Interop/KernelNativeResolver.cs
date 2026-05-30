using System.Reflection;
using System.Runtime.InteropServices;

namespace GravyBox.Core.Interop;

/// <summary>
/// Selects the correct native kernel binary per platform. Binaries are laid out
/// as <c>runtimes/{rid}/native/&lt;lib&gt;</c> beside the running application
/// (the .NET-idiomatic RID layout); this resolver maps the bare P/Invoke name to
/// the host's RID folder. Registered from <see cref="GravyBoxKernel"/>'s static
/// constructor, so it is installed before the first kernel P/Invoke.
/// </summary>
internal static class KernelNativeResolver
{
    internal static void Register()
    {
        NativeLibrary.SetDllImportResolver(typeof(KernelNativeResolver).Assembly, Resolve);
    }

    private static nint Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // Only intercept the kernel; let everything else fall to default probing.
        if (!string.Equals(libraryName, GravyBoxKernel.LibraryName, StringComparison.Ordinal))
        {
            return 0;
        }

        string fileName = NativeFileName(libraryName);
        string ridPath = Path.Combine(
            AppContext.BaseDirectory, "runtimes", HostRid(), "native", fileName);

        if (NativeLibrary.TryLoad(ridPath, out nint handle))
        {
            return handle;
        }

        // Fallbacks: a flat copy beside the assembly, then default OS probing.
        if (NativeLibrary.TryLoad(fileName, assembly, searchPath, out handle))
        {
            return handle;
        }

        return 0;
    }

    private static string HostRid()
    {
        string os =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
            throw new PlatformNotSupportedException("Unsupported OS for the GravyBox kernel.");

        string arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException(
                $"Unsupported architecture for the GravyBox kernel: {RuntimeInformation.ProcessArchitecture}."),
        };

        return $"{os}-{arch}";
    }

    private static string NativeFileName(string libraryName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"{libraryName}.dll";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return $"lib{libraryName}.dylib";
        }

        return $"lib{libraryName}.so";
    }
}
