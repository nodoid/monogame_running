using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonsterMaze.Audio;
using MonsterMaze.Data;
using MonsterMaze.Gameplay;
using MonsterMaze.UI;

namespace MonsterMaze.Screens;

/// <summary>
/// "New high score!" The player types a name with big arcade-style letter keys, or opens the
/// phone's own keyboard. Names are kept short and simple so they fit the table.
/// </summary>
public sealed class NameEntryScreen : GameScreen
{
    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int Columns = 7;
    private const int KeySize = 128;
    private const int KeyGap = 14;

    private static readonly string[] RankNames = { "1ST", "2ND", "3RD", "4TH", "5TH", "6TH" };

    private readonly GameSession _session;
    private readonly StringBuilder _name = new();
    private readonly List<Button> _letterKeys = new();
    private readonly Button _space = new("SPACE");
    private readonly Button _delete = new("DEL", Icons.Back);
    private readonly Button _keyboard = new(null, Icons.Keyboard);
    private readonly Button _done = new("DONE", Icons.Check);
    private Task<string> _keyboardTask;
    private int _rank;
    private float _time;

    public NameEntryScreen(GameSession session)
    {
        _session = session;
        foreach (char letter in Letters)
            _letterKeys.Add(new Button(letter.ToString()) { TextSize = 64f, Color = Palette.ButtonSecondary });
    }

    public override void Load()
    {
        // Offer the name used last time.
        _name.Append(Sanitise(Game.Settings.PlayerName));

        // Work out where the score will land, to tell the player.
        IReadOnlyList<HighScoreEntry> table = Game.HighScores[_session.Difficulty];
        _rank = 0;
        while (_rank < table.Count && table[_rank].Score >= _session.Score)
            _rank++;

        Game.Audio.Play(Sounds.NewHighScore);
    }

    public override void Layout()
    {
        int gridWidth = Columns * KeySize + (Columns - 1) * KeyGap;
        float left = Size.X / 2f - gridWidth / 2f;
        float top = Size.Y * 0.44f;

        for (int i = 0; i < _letterKeys.Count; i++)
        {
            int column = i % Columns;
            int row = i / Columns;
            _letterKeys[i].Bounds = new Rectangle((int)(left + column * (KeySize + KeyGap)),
                (int)(top + row * (KeySize + KeyGap)), KeySize, KeySize);
        }

        // The last row of letters has room for the space and delete keys.
        float lastRow = top + 3 * (KeySize + KeyGap);
        float afterLetters = left + (Letters.Length % Columns) * (KeySize + KeyGap);
        _space.Bounds = new Rectangle((int)afterLetters, (int)lastRow, KeySize * 2 + KeyGap, KeySize);
        _space.TextSize = 50f;

        float bottomRow = lastRow + KeySize + KeyGap * 3;
        _delete.Bounds = new Rectangle((int)left, (int)bottomRow, KeySize * 2 + KeyGap, KeySize);
        _delete.Color = Palette.Danger;
        _delete.TextSize = 50f;
        _keyboard.Bounds = new Rectangle((int)(left + gridWidth - KeySize * 2 - KeyGap), (int)bottomRow,
            KeySize * 2 + KeyGap, KeySize);
        _keyboard.Color = Palette.ButtonSecondary;

        _done.Bounds = CentredRect(MathF.Min(bottomRow + KeySize + 150f, SafeArea.Bottom - 90f), 720, 160);
    }

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);
        _time += deltaSeconds;

        // While the phone's keyboard is open, wait for it to finish.
        if (_keyboardTask != null)
        {
            if (!_keyboardTask.IsCompleted)
                return;
            if (_keyboardTask.Status == TaskStatus.RanToCompletion && _keyboardTask.Result != null)
            {
                _name.Clear();
                _name.Append(Sanitise(_keyboardTask.Result));
            }

            _keyboardTask = null;
        }

        for (int i = 0; i < _letterKeys.Count; i++)
        {
            if (_letterKeys[i].Update(Input, deltaSeconds))
                Type(Letters[i]);
        }

        if (_space.Update(Input, deltaSeconds))
            Type(' ');
        if (_delete.Update(Input, deltaSeconds) || Input.IsKeyPressed(Keys.Back))
            DeleteLetter();

        // A physical keyboard works too.
        for (Keys key = Keys.A; key <= Keys.Z; key++)
        {
            if (Input.IsKeyPressed(key))
                Type((char)('A' + (key - Keys.A)));
        }

        if (Input.IsKeyPressed(Keys.Space))
            Type(' ');

        if (_keyboard.Update(Input, deltaSeconds))
        {
            // MonoGame shows the platform's native text entry and completes the task when it closes.
            _keyboardTask = KeyboardInput.Show("NEW HIGH SCORE", "Enter your name", _name.ToString());
        }

        if (_done.Update(Input, deltaSeconds) || Input.IsKeyPressed(Keys.Enter) || Input.BackPressed)
            Submit();
    }

    public override void Draw(GameTime gameTime)
    {
        BeginSpriteBatch();
        DrawBackground(Assets.MenuBackground, new Color(100, 100, 100));

        Fonts.DrawOutlined(SpriteBatch, "NEW HIGH SCORE!", new Vector2(Size.X / 2f, SafeArea.Top + 120f), 120f,
            Palette.Rainbow(_time * 0.5f), Color.Black, 6f);

        var settings = DifficultySettings.For(_session.Difficulty);
        Fonts.DrawShadowed(SpriteBatch, $"{RankNames[Math.Min(_rank, RankNames.Length - 1)]} PLACE ON {settings.Name}",
            new Vector2(Size.X / 2f, SafeArea.Top + 230f), 56f, settings.Color);
        Fonts.DrawOutlined(SpriteBatch, _session.Score.ToString("N0"), new Vector2(Size.X / 2f, SafeArea.Top + 340f),
            120f, Palette.Gold, Color.Black, 5f);

        // The name, with a blinking cursor while there is room for more.
        var box = CentredRect(Size.Y * 0.44f - 130f, 900, 150);
        NineSlice.Draw(SpriteBatch, Assets.Panel, box, 64, 40, Palette.Accent);
        string cursor = _name.Length < HighScoreTable.MaxNameLength && (int)(_time * 2.5f) % 2 == 0 ? "_" : " ";
        Fonts.DrawShadowed(SpriteBatch, _name + cursor, box.Center.ToVector2(), 84f, Color.White);

        foreach (Button key in _letterKeys)
            key.Draw(SpriteBatch, Assets);
        _space.Draw(SpriteBatch, Assets);
        _delete.Draw(SpriteBatch, Assets);
        _keyboard.Draw(SpriteBatch, Assets);
        _done.Draw(SpriteBatch, Assets);
        SpriteBatch.End();
    }

    private void Type(char letter)
    {
        if (_name.Length >= HighScoreTable.MaxNameLength || (letter == ' ' && (_name.Length == 0 || _name[^1] == ' ')))
            return;
        _name.Append(letter);
        Game.Audio.Play(Sounds.UiType);
    }

    private void DeleteLetter()
    {
        if (_name.Length == 0)
            return;
        _name.Length--;
        Game.Audio.Play(Sounds.UiType, 0.7f, -0.3f);
    }

    private void Submit()
    {
        string name = Sanitise(_name.ToString());
        if (name.Length == 0)
            name = "PLAYER";

        Game.Settings.PlayerName = name;
        int rank = Game.HighScores.Add(_session.Difficulty, new HighScoreEntry
        {
            Name = name,
            Score = _session.Score,
            Outcome = _session.Outcome,
            Date = DateTime.Now
        });
        Manager.SwitchTo(new HighScoreScreen(_session.Difficulty, rank));
    }

    /// <summary>
    /// Keeps names to capital letters, digits and single spaces, no longer than the table allows.
    /// Anything typed on the phone's keyboard passes through here too.
    /// </summary>
    public static string Sanitise(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        var result = new StringBuilder(HighScoreTable.MaxNameLength);
        foreach (char c in text.ToUpperInvariant())
        {
            if (result.Length >= HighScoreTable.MaxNameLength)
                break;
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                result.Append(c);
            else if (c == ' ' && result.Length > 0 && result[^1] != ' ')
                result.Append(' ');
        }

        return result.ToString().Trim();
    }
}
