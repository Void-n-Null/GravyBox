using Godot;

namespace GravyBox.Editor;

/// <summary>
/// Crisp free-fly editor camera (no momentum). Press Z to toggle flying mode: in
/// flying mode the cursor is captured and aims the camera, WASD moves view-relative,
/// Space/Ctrl move along world up/down, Shift boosts, and the scroll wheel tunes fly
/// speed. Press Z again to return to clicking mode (cursor freed). Perspective only
/// for now; the controller is kept small so an orthographic/projection mode can be
/// layered on when Hammer-style views arrive.
/// </summary>
[GlobalClass]
public partial class FlyCamera : Camera3D
{
    /// <summary>Base fly speed in world units per second.</summary>
    [Export] public float BaseSpeed { get; set; } = 512.0f;

    /// <summary>Speed multiplier while Shift is held.</summary>
    [Export] public float BoostMultiplier { get; set; } = 4.0f;

    /// <summary>Look sensitivity in radians per pixel of mouse motion.</summary>
    [Export] public float MouseSensitivity { get; set; } = 0.003f;

    /// <summary>Multiplicative step applied to BaseSpeed per scroll notch.</summary>
    [Export(PropertyHint.Range, "1.05,3.0,0.05")] public float SpeedStep { get; set; } = 1.2f;

    [Export] public float MinSpeed { get; set; } = 8.0f;
    [Export] public float MaxSpeed { get; set; } = 16384.0f;

    private static readonly float PitchLimit = Mathf.DegToRad(89.0f);

    private float _yaw;
    private float _pitch;
    private bool _flying;

    public override void _Ready()
    {
        Current = true;

        // Seed yaw/pitch from whatever transform the scene placed us at, by aiming
        // at the origin once. Rotation is read back in the same YXZ euler order we
        // re-apply each frame, so the hand-off is consistent.
        LookAt(Vector3.Zero, Vector3.Up);
        _yaw = Rotation.Y;
        _pitch = Rotation.X;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventKey { Pressed: true, Echo: false, Keycode: Key.Z }:
                SetFlying(!_flying);
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp } when _flying:
                BaseSpeed = Mathf.Min(BaseSpeed * SpeedStep, MaxSpeed);
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown } when _flying:
                BaseSpeed = Mathf.Max(BaseSpeed / SpeedStep, MinSpeed);
                break;
            case InputEventMouseMotion mm when _flying:
                _yaw -= mm.Relative.X * MouseSensitivity;
                _pitch = Mathf.Clamp(_pitch - mm.Relative.Y * MouseSensitivity, -PitchLimit, PitchLimit);
                Rotation = new Vector3(_pitch, _yaw, 0.0f);
                break;
        }
    }

    private void SetFlying(bool flying)
    {
        _flying = flying;
        Input.MouseMode = flying ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
    }

    public override void _Process(double delta)
    {
        if (!_flying)
        {
            return;
        }

        Basis b = GlobalTransform.Basis;
        Vector3 dir = Vector3.Zero;

        if (Input.IsKeyPressed(Key.W)) dir -= b.Z; // camera looks down -Z
        if (Input.IsKeyPressed(Key.S)) dir += b.Z;
        if (Input.IsKeyPressed(Key.A)) dir -= b.X;
        if (Input.IsKeyPressed(Key.D)) dir += b.X;
        if (Input.IsKeyPressed(Key.Space)) dir += Vector3.Up;
        if (Input.IsKeyPressed(Key.Ctrl)) dir -= Vector3.Up;

        if (dir == Vector3.Zero)
        {
            return;
        }

        float speed = BaseSpeed * (Input.IsKeyPressed(Key.Shift) ? BoostMultiplier : 1.0f);
        GlobalPosition += dir.Normalized() * speed * (float)delta;
    }
}
