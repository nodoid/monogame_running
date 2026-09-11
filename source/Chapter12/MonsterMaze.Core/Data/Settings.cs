using MonsterMaze.Rendering;

namespace MonsterMaze.Data;

/// <summary>The player's preferences.</summary>
public sealed class Settings
{
    public GraphicsQuality Quality { get; set; } = GraphicsQuality.High;

    /// <summary>False until the player picks a quality level; until then we pick one for the device.</summary>
    public bool QualityChosen { get; set; }
}
