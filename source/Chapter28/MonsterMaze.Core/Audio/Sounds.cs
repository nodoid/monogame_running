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

    public const string WallBump = "Sfx/wall_bump";
    public const string RexStep = "Sfx/rex_step";
    public const string RexStepFar = "Sfx/rex_step_far";
    public const string RexRoar = "Sfx/rex_roar";
    public const string RexRoarFar = "Sfx/rex_roar_far";
    public const string RexSnort = "Sfx/rex_snort";
    public const string RexGrowl = "Sfx/rex_growl";
    public const string Heartbeat = "Sfx/heartbeat";
    public const string StingerSeen = "Sfx/stinger_seen";
    public const string NearMiss = "Sfx/near_miss";
    public const string Chomp = "Sfx/chomp";
    public const string EscapeFanfare = "Sfx/escape_fanfare";
    public const string AmbientLoop = "Music/ambient_loop";

    public static readonly string[] Drips = { "Sfx/drip_1", "Sfx/drip_2", "Sfx/drip_3" };

    public const string TensionLoop = "Music/tension_loop";
    public const string ExitHum = "Music/exit_hum";

    /// <summary>Every sound the game loads at start-up.</summary>
    public static IEnumerable<string> All()
    {
        yield return UiClick;
        yield return UiSelect;
        yield return UiBack;
        yield return TitleMusic;
        foreach (string footstep in Footsteps)
            yield return footstep;
        yield return WallBump;
        yield return RexStep;
        yield return RexStepFar;
        yield return RexRoar;
        yield return RexRoarFar;
        yield return RexSnort;
        yield return RexGrowl;
        yield return Heartbeat;
        yield return StingerSeen;
        yield return NearMiss;
        yield return Chomp;
        yield return EscapeFanfare;
        yield return AmbientLoop;
        foreach (string drip in Drips)
            yield return drip;
        yield return TensionLoop;
        yield return ExitHum;
    }
}
