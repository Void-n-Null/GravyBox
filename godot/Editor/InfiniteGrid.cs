using Godot;

namespace GravyBox.Editor;

/// <summary>
/// Full-screen infinite ground grid (option A). Owns a fullscreen quad whose
/// shader reconstructs the y=0 plane per pixel and draws the grid there, so the
/// node's transform is irrelevant — only its uniforms matter.
///
/// The grid plane is the XZ ground for now; subdivisions are driven by
/// <see cref="CellSize"/>, which will later track the editor's active snap step
/// (DOC-8 power-of-two units). When orthographic views arrive, the same shader is
/// reused with a different plane orientation.
/// </summary>
[GlobalClass]
public partial class InfiniteGrid : MeshInstance3D
{
    /// <summary>World units between minor grid lines.</summary>
    [Export] public float CellSize { get; set; } = 64.0f;

    /// <summary>A major (brighter) line is drawn every N minor cells.</summary>
    [Export] public float MajorEvery { get; set; } = 8.0f;

    /// <summary>Approximate line half-width, in pixels.</summary>
    [Export] public float LineWidth { get; set; } = 1.0f;

    /// <summary>World distance at which the grid begins to fade out.</summary>
    [Export] public float FadeStart { get; set; } = 4000.0f;

    /// <summary>World distance at which the grid is fully faded.</summary>
    [Export] public float FadeEnd { get; set; } = 12000.0f;

    [Export] public Color MinorColor { get; set; } = new(0.40f, 0.40f, 0.40f, 1.0f);
    [Export] public Color MajorColor { get; set; } = new(0.65f, 0.65f, 0.65f, 1.0f);
    [Export] public Color XAxisColor { get; set; } = new(0.85f, 0.27f, 0.33f, 1.0f);
    [Export] public Color ZAxisColor { get; set; } = new(0.30f, 0.55f, 0.95f, 1.0f);

    private ShaderMaterial _material = null!;

    public override void _Ready()
    {
        // The shader places the quad in clip space; size 2 maps VERTEX.xy to NDC.
        Mesh = new QuadMesh { Size = new Vector2(2.0f, 2.0f) };

        // The quad's model-space AABB is meaningless (it lives in clip space), so a
        // huge custom AABB prevents the renderer from frustum-culling it whenever
        // the camera looks away from the origin.
        CustomAabb = new Aabb(new Vector3(-1e6f, -1e6f, -1e6f), new Vector3(2e6f, 2e6f, 2e6f));
        CastShadow = ShadowCastingSetting.Off;
        GIMode = GIModeEnum.Disabled;

        var shader = GD.Load<Shader>("res://Editor/Shaders/infinite_grid.gdshader");
        _material = new ShaderMaterial { Shader = shader };
        MaterialOverride = _material;
        PushUniforms();
    }

    /// <summary>Push all exported settings to the shader. Call after changing any.</summary>
    public void PushUniforms()
    {
        // Safe before _Ready(): the material is created there and PushUniforms() is
        // called again with the then-current export values.
        if (_material is null)
        {
            return;
        }

        _material.SetShaderParameter("cell_size", CellSize);
        _material.SetShaderParameter("major_every", MajorEvery);
        _material.SetShaderParameter("line_width", LineWidth);
        _material.SetShaderParameter("fade_start", FadeStart);
        _material.SetShaderParameter("fade_end", FadeEnd);
        _material.SetShaderParameter("minor_color", MinorColor);
        _material.SetShaderParameter("major_color", MajorColor);
        _material.SetShaderParameter("x_axis_color", XAxisColor);
        _material.SetShaderParameter("z_axis_color", ZAxisColor);
    }
}
