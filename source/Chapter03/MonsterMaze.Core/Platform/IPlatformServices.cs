namespace MonsterMaze.Platform;

/// <summary>The two orientations the game uses: gameplay is landscape, everything else is portrait.</summary>
public enum GameOrientation
{
    Portrait,
    Landscape
}

/// <summary>
/// Everything the shared game code needs from the operating system. The Android and iOS
/// projects each provide an implementation, so the core never touches platform APIs.
/// </summary>
public interface IPlatformServices
{
    /// <summary>A friendly platform name, such as "Android" or "iOS".</summary>
    string Name { get; }

    /// <summary>Locks the display to portrait or landscape.</summary>
    void SetOrientation(GameOrientation orientation);

    /// <summary>
    /// The screen edges, in back buffer pixels, that are covered by notches, rounded corners
    /// or system bars. Anything the player needs to see or touch should stay inside them.
    /// </summary>
    Insets GetSafeAreaInsets();
}

/// <summary>Distances in pixels from each edge of the screen.</summary>
public readonly record struct Insets(int Left, int Top, int Right, int Bottom)
{
    public static readonly Insets Zero = new(0, 0, 0, 0);
}
