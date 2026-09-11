using System;
using Microsoft.Xna.Framework;
using MonsterMaze.Gameplay;
using MonsterMaze.Mazes;
using MonsterMaze.Platform;
using MonsterMaze.UI;

namespace MonsterMaze.Screens;

/// <summary>
/// The game itself, played in landscape. This screen owns the maze, the player and Rex, and
/// wires together every system that makes the game: rendering, AI, warnings, scoring, sound
/// and effects.
/// </summary>
public sealed class GameplayScreen : GameScreen
{
    private readonly GameSession _session;
    private readonly Button _escapeButton = new("ESCAPE", Icons.Exit);
    private readonly Button _eatenButton = new("GET EATEN", Icons.Skull);
    private readonly Button _newMazeButton = new("NEW MAZE", Icons.Star);
    private float _time;
    private bool _gameOver;
    private Maze _maze;

    public GameplayScreen(GameSession session)
    {
        _session = session;
    }

    public override GameOrientation Orientation => GameOrientation.Landscape;

    public override void Load()
    {
        BuildMaze(_session.Seed);
    }

    public override void Layout()
    {
        _escapeButton.Bounds = new Rectangle((int)(Size.X / 2f) - 440, SafeArea.Bottom - 150, 420, 150);
        _escapeButton.Color = Palette.Button;
        _eatenButton.Bounds = new Rectangle((int)(Size.X / 2f) + 20, SafeArea.Bottom - 150, 420, 150);
        _eatenButton.Color = Palette.Danger;
        _newMazeButton.Bounds = new Rectangle(SafeArea.Right - 460, SafeArea.Top, 460, 140);
    }

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);

        bool pause = Input.BackPressed;
        if (pause && !_gameOver)
        {
            Pause();
            return;
        }

        _time += deltaSeconds;

        // Until the game has a maze to escape from and a monster to be eaten by, two buttons
        // let us try out both endings.
        if (_escapeButton.Update(Input, deltaSeconds))
            EndGame(GameOutcome.Escaped);
        else if (_eatenButton.Update(Input, deltaSeconds))
            EndGame(GameOutcome.Eaten);

        if (_newMazeButton.Update(Input, deltaSeconds))
        {
            // A new seed means a completely new maze.
            BuildMaze(Random.Shared.Next());
        }
    }

    public override void Draw(GameTime gameTime)
    {
        BeginSpriteBatch();
        DrawSimpleMap();
        _escapeButton.Draw(SpriteBatch, Assets);
        _eatenButton.Draw(SpriteBatch, Assets);
        _newMazeButton.Draw(SpriteBatch, Assets);
        SpriteBatch.End();
    }

    private void Pause()
    {
        Manager.Add(new PauseScreen(this));
    }

    private void EndGame(GameOutcome outcome)
    {
        if (_gameOver)
            return;

        _gameOver = true;
        _session.Outcome = outcome;

        GameScreen next = new GameOverScreen(_session);
        Manager.SwitchTo(next);
    }

    private void BuildMaze(int seed)
    {
        _maze = MazeGenerator.Generate(15, 15, seed, 0.15f);
    }

    /// <summary>A first look at the maze data: every wall drawn as a thin bar.</summary>
    private void DrawSimpleMap()
    {
        float cell = MathF.Floor(MathF.Min(SafeArea.Width / (float)_maze.Width,
            (SafeArea.Height - 360f) / _maze.Height));
        var origin = new Vector2(Size.X / 2f - cell * _maze.Width / 2f, SafeArea.Top + 140f);
        const float thickness = 6f;

        for (int y = 0; y < _maze.Height; y++)
        {
            for (int x = 0; x < _maze.Width; x++)
            {
                var point = new Point(x, y);
                Vector2 corner = origin + new Vector2(x, y) * cell;
                if (_maze.HasWall(point, Direction.North))
                    SpriteBatch.FillRectangle(corner, new Vector2(cell + thickness, thickness), Color.White);
                if (_maze.HasWall(point, Direction.West))
                    SpriteBatch.FillRectangle(corner, new Vector2(thickness, cell + thickness), Color.White);
                if (_maze.HasWall(point, Direction.South))
                    SpriteBatch.FillRectangle(corner + new Vector2(0f, cell), new Vector2(cell + thickness, thickness), Color.White);
                if (_maze.HasWall(point, Direction.East))
                    SpriteBatch.FillRectangle(corner + new Vector2(cell, 0f), new Vector2(thickness, cell + thickness), Color.White);
            }
        }

        Vector2 marker = new Vector2(cell * 0.5f);
        SpriteBatch.FillRectangle(origin + _maze.Start.ToVector2() * cell + marker * 0.5f, marker, Palette.Accent);
        SpriteBatch.FillRectangle(origin + _maze.Exit.ToVector2() * cell + marker * 0.5f, marker, Palette.Gold);

        string title = $"RANDOM MAZE  {_maze.Width} x {_maze.Height}   SEED {_maze.Seed}";
        Fonts.DrawShadowed(SpriteBatch, title, new Vector2(Size.X / 2f, SafeArea.Top + 60f), 60f, Palette.Text);
    }
}
