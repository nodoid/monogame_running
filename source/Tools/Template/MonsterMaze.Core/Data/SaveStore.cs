// @since 37
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonsterMaze.Data;

/// <summary>Everything we save: settings and the three high score tables.</summary>
public sealed class SaveData
{
    /// <summary>Bump this if the format changes, so old files can be upgraded when loaded.</summary>
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public Settings Settings { get; set; } = new();
    public List<HighScoreEntry> Easy { get; set; }
    public List<HighScoreEntry> Normal { get; set; }
    public List<HighScoreEntry> Hard { get; set; }
}

/// <summary>
/// System.Text.Json's source generator writes the (de)serialisation code at compile time. That
/// matters on iOS, where the app is compiled ahead of time and reflection-based JSON can break.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(SaveData))]
internal partial class SaveDataContext : JsonSerializerContext
{
}

/// <summary>
/// Saves and loads <see cref="SaveData"/> as JSON in the app's private storage, which survives
/// app updates and is included in the operating system's device backups.
/// </summary>
public sealed class SaveStore
{
    private readonly string _folder;
    private readonly string _path;
    private readonly string _backupPath;
    private readonly string _tempPath;

    public SaveStore(string folder)
    {
        _folder = folder;
        _path = Path.Combine(folder, "save.json");
        _backupPath = Path.Combine(folder, "save.backup.json");
        _tempPath = Path.Combine(folder, "save.tmp");
    }

    /// <summary>The app's private data folder on Android and iOS.</summary>
    public static string DefaultFolder() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MonsterMaze");

    /// <summary>Loads the save file. A damaged file falls back to the backup, then to defaults.</summary>
    public SaveData Load()
    {
        foreach (string path in new[] { _path, _backupPath })
        {
            try
            {
                if (!File.Exists(path))
                    continue;

                SaveData data = JsonSerializer.Deserialize(File.ReadAllText(path), SaveDataContext.Default.SaveData);
                if (data != null)
                    return data;
            }
            catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
            {
                // Unreadable or corrupted: try the next file.
            }
        }

        return new SaveData();
    }

    /// <summary>
    /// Saves safely: write a temporary file first, keep the previous save as a backup, then swap
    /// the new file into place. If the app is killed halfway through, one good copy always remains.
    /// </summary>
    public bool Save(SaveData data)
    {
        try
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(_tempPath, JsonSerializer.Serialize(data, SaveDataContext.Default.SaveData));
            if (File.Exists(_path))
                File.Copy(_path, _backupPath, overwrite: true);
            File.Move(_tempPath, _path, overwrite: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
