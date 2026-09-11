// @since 08
using System;
using Microsoft.Xna.Framework;

namespace MonsterMaze.Rendering;

/// <summary>
/// Turns a position and viewing angles into the view and projection matrices that 3D drawing needs.
/// Yaw 0 looks north (-Z) and increases as the camera turns clockwise (to the right).
/// </summary>
public sealed class Camera
{
    public Vector3 Position { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float Roll { get; set; }

    /// <summary>The vertical field of view, in radians.</summary>
    public float FieldOfView { get; set; } = MathHelper.ToRadians(60f);

    public float AspectRatio { get; set; } = 16f / 9f;
    public float NearPlane { get; set; } = 0.05f;
    public float FarPlane { get; set; } = 60f;

    public Matrix View { get; private set; } = Matrix.Identity;
    public Matrix Projection { get; private set; } = Matrix.Identity;
    public Vector3 Forward { get; private set; } = Vector3.Forward;

    /// <summary>Rebuilds the matrices from the position, yaw, pitch and roll.</summary>
    public void Update()
    {
        // MonoGame's yaw turns anticlockwise, so negate ours.
        Matrix rotation = Matrix.CreateFromYawPitchRoll(-Yaw, Pitch, Roll);
        Forward = Vector3.Transform(Vector3.Forward, rotation);
        Vector3 up = Vector3.Transform(Vector3.Up, rotation);

        View = Matrix.CreateLookAt(Position, Position + Forward, up);
        UpdateProjection();
    }

    /// <summary>Points the camera at a target instead of using yaw and pitch.</summary>
    public void LookAt(Vector3 position, Vector3 target)
    {
        Position = position;
        Forward = Vector3.Normalize(target - position);
        View = Matrix.CreateLookAt(position, target, Vector3.Up);
        UpdateProjection();
    }
#if CH10

    /// <summary>
    /// Keeps the horizontal field of view the same on every screen shape. A wide phone and a
    /// squarer tablet then show the same amount of corridor to the left and right; only the
    /// amount of floor and ceiling changes. Tall portrait screens are capped so they don't bulge.
    /// </summary>
    public void FitFieldOfView(float aspectRatio, float horizontalDegrees = 90f, float maxVerticalDegrees = 80f)
    {
        AspectRatio = aspectRatio;
        float horizontal = MathHelper.ToRadians(horizontalDegrees);
        float vertical = 2f * MathF.Atan(MathF.Tan(horizontal / 2f) / aspectRatio);
        FieldOfView = MathF.Min(vertical, MathHelper.ToRadians(maxVerticalDegrees));
    }
#endif

    private void UpdateProjection()
    {
        Projection = Matrix.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
    }
}
