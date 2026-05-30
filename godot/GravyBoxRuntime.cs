using Godot;
using GravyBox.Core.Interop;

namespace GravyBox.Editor;

/// <summary>
/// Minimal runtime smoke-test node. On _Ready it asks the native Rust kernel for
/// a string (crossing the C# -> Rust FFI seam) and logs it, so the full path —
/// Rust -> C# Core binding -> Godot node -> stdout -> ./headless capture — can be
/// confirmed end to end.
/// </summary>
public partial class GravyBoxRuntime : Node
{
    public override void _Ready()
    {
        string fromKernel = GravyBoxKernel.Hello();
        GD.Print($"GravyBoxRuntime: kernel says: {fromKernel}");
    }
}
