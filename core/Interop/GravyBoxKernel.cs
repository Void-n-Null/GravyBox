using System.Runtime.InteropServices;

namespace GravyBox.Core.Interop;

/// <summary>
/// Managed entry point to the native GravyBox kernel (Rust, C ABI). The native
/// library is selected per-platform at load time by
/// <see cref="KernelNativeResolver"/>; callers only see managed types.
/// </summary>
public static partial class GravyBoxKernel
{
    /// <summary>Bare library name; the resolver maps it to the per-RID binary.</summary>
    internal const string LibraryName = "gravybox_kernel";

    static GravyBoxKernel()
    {
        // Runs before any P/Invoke below (static members force the static ctor),
        // so the per-platform native resolver is installed in time.
        KernelNativeResolver.Register();
    }

    [LibraryImport(LibraryName, EntryPoint = "gbox_hello")]
    private static partial nint GboxHello();

    [LibraryImport(LibraryName, EntryPoint = "gbox_string_free")]
    private static partial void GboxStringFree(nint s);

    /// <summary>
    /// Ask the kernel for its greeting string. The native allocation is released
    /// before returning, so the caller owns only the managed copy.
    /// </summary>
    public static string Hello()
    {
        nint ptr = GboxHello();
        if (ptr == 0)
        {
            return string.Empty;
        }

        try
        {
            return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
        }
        finally
        {
            // Rust allocated it; Rust must free it.
            GboxStringFree(ptr);
        }
    }
}
