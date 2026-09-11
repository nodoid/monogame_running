using System.Collections.Generic;

namespace MonsterMaze.Audio;

/// <summary>The names of every sound, matching their paths under Content/Audio.</summary>
public static class Sounds
{
    public const string UiClick = "Sfx/ui_click";
    public const string UiSelect = "Sfx/ui_select";
    public const string UiBack = "Sfx/ui_back";
    public const string TitleMusic = "Music/title_music";

    public static readonly string[] Footsteps =
        { "Sfx/footstep_1", "Sfx/footstep_2", "Sfx/footstep_3", "Sfx/footstep_4" };

    /// <summary>Every sound the game loads at start-up.</summary>
    public static IEnumerable<string> All()
    {
        yield return UiClick;
        yield return UiSelect;
        yield return UiBack;
        yield return TitleMusic;
        foreach (string footstep in Footsteps)
            yield return footstep;
    }
}
