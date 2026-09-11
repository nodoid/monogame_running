using System;
using System.Collections.Generic;
using MonsterMaze.Gameplay;

namespace MonsterMaze.Data;

public sealed class HighScoreEntry
{
    public string Name { get; set; } = "";
    public int Score { get; set; }
    public GameOutcome Outcome { get; set; }
    public DateTime Date { get; set; }
}

/// <summary>
/// The top six scores for each difficulty level. Only six are ever kept: a seventh-best score
/// simply doesn't make the table.
/// </summary>
public sealed class HighScoreTable
{
    public const int MaxEntries = 6;
    public const int MaxNameLength = 10;

    private readonly List<HighScoreEntry>[] _tables = { new(), new(), new() };

    public IReadOnlyList<HighScoreEntry> this[Difficulty difficulty] => _tables[(int)difficulty];

    /// <summary>True if this score would earn a place in the table.</summary>
    public bool Qualifies(Difficulty difficulty, int score)
    {
        List<HighScoreEntry> table = _tables[(int)difficulty];
        return score > 0 && (table.Count < MaxEntries || score > table[table.Count - 1].Score);
    }

    /// <summary>
    /// Adds an entry in score order and returns its position (0 is the top), or -1 if it didn't
    /// make the table. A new score only beats an old one if it is strictly higher.
    /// </summary>
    public int Add(Difficulty difficulty, HighScoreEntry entry)
    {
        List<HighScoreEntry> table = _tables[(int)difficulty];

        int index = table.Count;
        for (int i = 0; i < table.Count; i++)
        {
            if (entry.Score > table[i].Score)
            {
                index = i;
                break;
            }
        }

        if (index >= MaxEntries)
            return -1;

        table.Insert(index, entry);
        if (table.Count > MaxEntries)
            table.RemoveAt(table.Count - 1);
        return index;
    }

    /// <summary>A table pre-filled with scores to beat, as every good arcade game has.</summary>
    public static HighScoreTable CreateDefault()
    {
        var table = new HighScoreTable();
        string[] names = { "REX", "DINO", "RUNNER", "EXPLORER", "LUCKY", "ROOKIE" };
        foreach (Difficulty difficulty in Enum.GetValues<Difficulty>())
        {
            int multiplier = (int)difficulty + 1;
            for (int i = 0; i < names.Length; i++)
            {
                table.Add(difficulty, new HighScoreEntry
                {
                    Name = names[i],
                    Score = (3000 - i * 500) * multiplier,
                    Outcome = i % 2 == 0 ? GameOutcome.Escaped : GameOutcome.Eaten,
                    Date = new DateTime(2026, 1, 1)
                });
            }
        }

        return table;
    }

    public static HighScoreTable FromSaveData(SaveData data)
    {
        HighScoreTable defaults = CreateDefault();
        var table = new HighScoreTable();
        Load(table, Difficulty.Easy, data.Easy, defaults);
        Load(table, Difficulty.Normal, data.Normal, defaults);
        Load(table, Difficulty.Hard, data.Hard, defaults);
        return table;
    }

    public SaveData ToSaveData(Settings settings) => new()
    {
        Settings = settings,
        Easy = new List<HighScoreEntry>(_tables[(int)Difficulty.Easy]),
        Normal = new List<HighScoreEntry>(_tables[(int)Difficulty.Normal]),
        Hard = new List<HighScoreEntry>(_tables[(int)Difficulty.Hard])
    };

    private static void Load(HighScoreTable table, Difficulty difficulty, List<HighScoreEntry> saved,
        HighScoreTable defaults)
    {
        // A missing table (a first run, or an older save file) gets the default scores.
        IReadOnlyList<HighScoreEntry> source = saved ?? (IReadOnlyList<HighScoreEntry>)defaults[difficulty];
        foreach (HighScoreEntry entry in source)
        {
            if (entry != null && !string.IsNullOrWhiteSpace(entry.Name))
                table.Add(difficulty, entry);
        }
    }
}
