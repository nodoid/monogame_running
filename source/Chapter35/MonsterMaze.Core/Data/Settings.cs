using MonsterMaze.Gameplay;
using MonsterMaze.Input;
using MonsterMaze.Rendering;

namespace MonsterMaze.Data;

/// <summary>The player's preferences.</summary>
public sealed class Settings
{
    public GraphicsQuality Quality { get; set; } = GraphicsQuality.High;

    /// <summary>False until the player picks a quality level; until then we pick one for the device.</summary>
    public bool QualityChosen { get; set; }

    public ControlScheme Controls { get; set; } = ControlScheme.Swipe;
    public bool Haptics { get; set; } = true;

    public Difficulty LastDifficulty { get; set; } = Difficulty.Normal;

    public float SoundVolume { get; set; } = 0.9f;
    public float MusicVolume { get; set; } = 0.7f;

    /// <summary>Tones down flashes and strobing for players who are sensitive to them.</summary>
    public bool ReduceFlashing { get; set; }

    /// <summary>Extra brightness, from -0.1 (darker) to +0.2 (brighter).</summary>
    public float Brightness { get; set; }
}
