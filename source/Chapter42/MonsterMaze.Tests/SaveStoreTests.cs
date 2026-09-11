using System;
using System.IO;
using MonsterMaze.Data;
using MonsterMaze.Gameplay;
using MonsterMaze.Rendering;
using Xunit;

namespace MonsterMaze.Tests;

public sealed class SaveStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "MonsterMazeTests-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    [Fact]
    public void HighScoresAndSettingsSurviveARestart()
    {
        var table = HighScoreTable.CreateDefault();
        table.Add(Difficulty.Hard, new HighScoreEntry { Name = "WINNER", Score = 99_999, Outcome = GameOutcome.Escaped });
        var settings = new Settings { Quality = GraphicsQuality.Low, SoundVolume = 0.3f, PlayerName = "WINNER" };
        new SaveStore(_folder).Save(table.ToSaveData(settings));

        // A brand-new store, as if the app had been closed and opened again.
        SaveData loaded = new SaveStore(_folder).Load();
        HighScoreTable reloaded = HighScoreTable.FromSaveData(loaded);

        Assert.Equal("WINNER", reloaded[Difficulty.Hard][0].Name);
        Assert.Equal(99_999, reloaded[Difficulty.Hard][0].Score);
        Assert.Equal(GameOutcome.Escaped, reloaded[Difficulty.Hard][0].Outcome);
        Assert.Equal(GraphicsQuality.Low, loaded.Settings.Quality);
        Assert.Equal(0.3f, loaded.Settings.SoundVolume);
    }

    [Fact]
    public void ACorruptedSaveFallsBackToTheBackup()
    {
        var store = new SaveStore(_folder);
        var table = HighScoreTable.CreateDefault();
        table.Add(Difficulty.Easy, new HighScoreEntry { Name = "FIRST", Score = 50_000 });
        store.Save(table.ToSaveData(new Settings()));
        store.Save(table.ToSaveData(new Settings()));

        File.WriteAllText(Path.Combine(_folder, "save.json"), "{ this is not json");

        SaveData loaded = store.Load();
        Assert.Equal("FIRST", HighScoreTable.FromSaveData(loaded)[Difficulty.Easy][0].Name);
    }

    [Fact]
    public void AMissingSaveGivesDefaults()
    {
        SaveData loaded = new SaveStore(_folder).Load();
        HighScoreTable table = HighScoreTable.FromSaveData(loaded);

        Assert.NotNull(loaded.Settings);
        Assert.Equal(HighScoreTable.MaxEntries, table[Difficulty.Normal].Count);
    }
}
