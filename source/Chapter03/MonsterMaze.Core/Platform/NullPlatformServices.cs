namespace MonsterMaze.Platform;

/// <summary>
/// A do-nothing implementation used when no platform services are supplied,
/// for example when running the game code from tests.
/// </summary>
public sealed class NullPlatformServices : IPlatformServices
{
    public string Name => "Unknown";

    public void SetOrientation(GameOrientation orientation)
    {
    }

    public Insets GetSafeAreaInsets() => Insets.Zero;
}
