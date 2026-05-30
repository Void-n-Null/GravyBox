shader_type spatial;
// Full-screen infinite grid (option A). A fullscreen quad is positioned directly
// in clip space by the vertex stage; the fragment stage reconstructs the world
// ray for each pixel, intersects it with the ground plane (y = 0), and draws the
// grid at that world position. Truly infinite — no plane mesh, no camera-follow.
//
// Unverifiable headless (needs a GPU): visual correctness is checked interactively.
render_mode unshaded, cull_disabled, depth_draw_always, shadows_disabled, fog_disabled;

// --- Configuration (driven from InfiniteGrid.cs) -----------------------------
uniform float cell_size = 64.0;       // world units between minor lines
uniform float major_every = 8.0;      // a major line every N minor cells
uniform float line_width = 1.0;       // approx line half-width, in pixels
uniform float fade_start = 4000.0;    // world distance where the grid starts fading
uniform float fade_end = 12000.0;     // world distance where the grid is fully gone
uniform vec4 minor_color : source_color = vec4(0.40, 0.40, 0.40, 1.0);
uniform vec4 major_color : source_color = vec4(0.65, 0.65, 0.65, 1.0);
uniform vec4 x_axis_color : source_color = vec4(0.85, 0.27, 0.33, 1.0); // line z=0 (the X axis)
uniform vec4 z_axis_color : source_color = vec4(0.30, 0.55, 0.95, 1.0); // line x=0 (the Z axis)

// Two world-space points along the per-pixel view ray, unprojected in the vertex
// stage and interpolated across the quad.
varying vec3 world_near;
varying vec3 world_far;

vec3 unproject(vec2 ndc, float ndc_z, mat4 inv_proj, mat4 inv_view) {
    vec4 view = inv_proj * vec4(ndc, ndc_z, 1.0);
    view /= view.w;
    return (inv_view * view).xyz;
}

void vertex() {
    // QuadMesh size 2 => VERTEX.xy spans [-1, 1], i.e. NDC corners. Place the quad
    // across the whole screen.
    //
    // Godot uses a REVERSED-Z clip space: the near plane is at ndc z = 1.0 and the
    // far plane at z = 0.0. Unprojecting both gives two points along the view ray;
    // ordering them near->far makes the fragment ray point FORWARD (into the scene)
    // rather than back toward the camera.
    vec2 ndc = VERTEX.xy;
    world_near = unproject(ndc, 1.0, INV_PROJECTION_MATRIX, INV_VIEW_MATRIX);
    world_far = unproject(ndc, 0.0, INV_PROJECTION_MATRIX, INV_VIEW_MATRIX);
    POSITION = vec4(ndc, 1.0, 1.0);
}

// Coverage (0..1) of grid lines for a 2D world position sampled at the given cell
// size, anti-aliased by screen-space derivatives.
float grid_coverage(vec2 p, float size, float width_px) {
    vec2 coord = p / size;
    vec2 d = fwidth(coord) * max(width_px, 0.0001);
    vec2 g = abs(fract(coord - 0.5) - 0.5) / max(d, vec2(1e-8));
    float line = min(g.x, g.y);
    return 1.0 - min(line, 1.0);
}

// Coverage (0..1) of a single axis line located at world coordinate v = 0.
float axis_coverage(float v, float width_px) {
    float d = fwidth(v) * max(width_px, 0.0001);
    return 1.0 - min(abs(v) / max(d, 1e-8), 1.0);
}

void fragment() {
    vec3 cam = CAMERA_POSITION_WORLD;
    vec3 rd = normalize(world_far - world_near);

    // Intersect with the ground plane y = 0. Discard pixels that look away from
    // the plane (parallel or pointing to the wrong side).
    if (abs(rd.y) < 1e-7) {
        discard;
    }
    float t = -cam.y / rd.y;
    if (t <= 0.0) {
        discard;
    }
    vec3 hit = cam + rd * t;

    float minor = grid_coverage(hit.xz, cell_size, line_width);
    float major = grid_coverage(hit.xz, cell_size * major_every, line_width);
    float ax_x = axis_coverage(hit.z, line_width * 1.5); // z=0 => X axis
    float ax_z = axis_coverage(hit.x, line_width * 1.5); // x=0 => Z axis

    // Compose minor -> major -> axes, back to front.
    vec4 col = vec4(0.0);
    col = mix(col, minor_color, minor * minor_color.a);
    col = mix(col, major_color, major * major_color.a);
    col = mix(col, x_axis_color, ax_x);
    col = mix(col, z_axis_color, ax_z);

    // Distance fade + a gentle grazing-angle fade to kill horizon shimmer.
    float dist = length(hit - cam);
    float dist_fade = 1.0 - clamp((dist - fade_start) / max(fade_end - fade_start, 1e-3), 0.0, 1.0);
    float graze_fade = clamp(abs(rd.y) * 8.0, 0.0, 1.0);
    float alpha = col.a * dist_fade * graze_fade;

    if (alpha < 0.001) {
        discard;
    }

    ALBEDO = col.rgb;
    ALPHA = alpha;

    // Write true ground-plane depth so future opaque brushes occlude the grid
    // correctly (and vice versa). Discard above handles the gaps so empty cells
    // never pollute the depth buffer.
    vec4 clip = PROJECTION_MATRIX * VIEW_MATRIX * vec4(hit, 1.0);
    DEPTH = clip.z / clip.w;
}
