// @since 04
using System;
using Microsoft.Xna.Framework;
#if CH08
using Microsoft.Xna.Framework.Graphics;
#endif
#if CH13 && !CH14
using Microsoft.Xna.Framework.Input;
#endif
#if CH07
using MonsterMaze.Diagnostics;
#endif
#if CH29
using MonsterMaze.Effects;
#endif
using MonsterMaze.Gameplay;
#if CH10 && !CH13 || CH14
using MonsterMaze.Input;
#endif
#if CH05
using MonsterMaze.Mazes;
#endif
#if CH16
using MonsterMaze.Monster;
#endif
#if CH26
using MonsterMaze.Audio;
#endif
using MonsterMaze.Platform;
#if CH07
using MonsterMaze.Rendering;
#endif
using MonsterMaze.UI;

namespace MonsterMaze.Screens;

/// <summary>
/// The game itself, played in landscape. This screen owns the maze, the player and Rex, and
/// wires together every system that makes the game: rendering, AI, warnings, scoring, sound
/// and effects.
/// </summary>
public sealed class GameplayScreen : GameScreen
{
#if CH05 && !CH06
    // A small maze drawn by hand: '#' is wall, 'S' is the start and 'E' the exit cell.
    private static readonly string[] SampleMaze =
    {
        "#############",
        "#S    #     #",
        "##### # ### #",
        "#     #   # #",
        "# ####### # #",
        "# #     # # #",
        "# # ### # # #",
        "#   #     #E ",
        "#############"
    };

#endif
#if CH17 && !CH23
    private static readonly RexTuning Tuning = new();

#endif
    private readonly GameSession _session;
#if !CH13
    private readonly Button _escapeButton = new("ESCAPE", Icons.Exit);
    private readonly Button _eatenButton = new("GET EATEN", Icons.Skull);
#endif
#if CH06 && !CH09
    private readonly Button _newMazeButton = new("NEW MAZE", Icons.Star);
#endif
#if CH07
    private readonly MazeMapRenderer _map = new();
    private readonly DebugOverlay _debug = new();
#endif
#if CH08
    private readonly Camera _camera = new();
#endif
#if CH14
    private readonly TouchControls _controls = new();
#endif
#if CH20
    private readonly WarningSystem _warnings = new();
#endif
#if CH25
    private readonly Hud _hud = new();
#endif
#if CH30
    private readonly DangerEffects _danger = new();
#endif
#if CH33
    private readonly EatenSequence _eaten = new();
#endif
#if CH34
    private readonly EscapeSequence _escape = new();
#endif
    private float _time;
    private bool _gameOver;
#if CH05
    private Maze _maze;
#endif
#if CH07 && !CH17
    private DistanceMap _startDistances;
#endif
#if CH08 && !CH09
    private TestScene _testScene;
#endif
#if CH09
    private MazeRenderer _renderer;
#endif
#if CH10 && !CH13
    private Direction _lookDirection;
#endif
#if CH13
    private Player _player;
#endif
#if CH16
    private Rex _rex;
    private RexRenderer _rexRenderer;
#endif
#if CH17
    private DistanceMap _distances;
#endif
#if CH19
    private RexBrain _brain;
#endif
#if CH22
    private ScoreKeeper _score;
#endif
#if CH27
    private GameplayAudio _audio;
#endif
#if CH29
    private PostProcessor _post;
#endif
#if CH32
    private DustParticles _dust;
#endif
#if CH39
    private float _countdown;
    private int _countdownNumber;
#endif

    public GameplayScreen(GameSession session)
    {
        _session = session;
    }

    public override GameOrientation Orientation => GameOrientation.Landscape;
#if CH23

    private DifficultySettings Difficulty => _session.Settings;

    private RexTuning Tuning => _session.Settings.Rex;
#endif

#if CH05

    public override void Load()
    {
#if CH26
        Game.Audio.StopMusic(0.8f);
#endif
#if CH05 && !CH06
        _maze = Maze.FromText(SampleMaze);
#endif
#if CH06
        BuildMaze(_session.Seed);
#endif
#if CH08 && !CH09
        _testScene = new TestScene(GraphicsDevice);
#endif
#if CH09
        _renderer = new MazeRenderer(GraphicsDevice, Assets);
        _renderer.SetMaze(_maze);
#endif
#if CH12
        _renderer.Quality = QualityProfile.For(Game.Settings.Quality);
#endif
#if CH23
        _renderer.FogStart = Difficulty.FogStart;
        _renderer.FogEnd = Difficulty.FogEnd;
#endif
#if CH10 && !CH13
        _lookDirection = _maze.StartFacing;
#endif
#if CH13
        _player = new Player(_maze, _maze.Start, _maze.StartFacing);
#endif
#if CH17
        _player.EnteredCell += OnPlayerEnteredCell;
#endif
#if CH14
        _player.Bumped += OnPlayerBumped;
        _controls.Scheme = Game.Settings.Controls;
#endif
#if CH26
        _player.Footstep += OnPlayerFootstep;
#endif
#if CH16
        _rex = new Rex(ChooseRexCell(), Direction.South);
        _rexRenderer = new RexRenderer(GraphicsDevice, Assets);
#endif
#if CH17
        _distances = new DistanceMap(_maze);
        _distances.Build(_player.Cell);
#endif
#if CH19
        _brain = new RexBrain(_rex, _maze, Tuning, new Random(_session.Seed ^ 0x5EED));
#endif
#if CH23
        _warnings.FootstepRange = Difficulty.FootstepWarningRange;
        _warnings.ShowHuntingWarning = Difficulty.ShowHuntingWarning;
#endif
#if CH23
        _score = new ScoreKeeper(_maze, Difficulty.ScoreMultiplier);
#elif CH22
        _score = new ScoreKeeper(_maze, 1);
#endif
#if CH25
        _score.Awarded += OnScoreAwarded;
#endif
#if CH27
        _audio = new GameplayAudio(Game.Audio);
        _rex.Footstep += OnRexFootstep;
        _brain.StateChanged += OnRexStateChanged;
        _brain.Sniffed += OnRexSniffed;
#endif
#if CH29
        _post = new PostProcessor(GraphicsDevice, SpriteBatch, Assets, QualityProfile.For(Game.Settings.Quality));
#endif
#if CH30
        _danger.ReduceFlashing = Game.Settings.ReduceFlashing;
#endif
#if CH32
        _dust = new DustParticles(GraphicsDevice, Assets.ParticleSoft)
        {
            FloatingMotes = QualityProfile.For(Game.Settings.Quality).DustParticles
        };
#endif
#if CH39
        Game.Deactivated += OnGameDeactivated;
#endif
    }
#endif
#if CH09

    public override void Unload()
    {
        _renderer.Dispose();
#if CH16
        _rexRenderer.Dispose();
#endif
#if CH27
        _audio.Dispose();
#endif
#if CH29
        _post.Dispose();
#endif
#if CH32
        _dust.Dispose();
#endif
#if CH39
        Game.Deactivated -= OnGameDeactivated;
#endif
    }
#endif

    public override void Layout()
    {
#if !CH13
        _escapeButton.Bounds = new Rectangle((int)(Size.X / 2f) - 440, SafeArea.Bottom - 150, 420, 150);
        _escapeButton.Color = Palette.Button;
        _eatenButton.Bounds = new Rectangle((int)(Size.X / 2f) + 20, SafeArea.Bottom - 150, 420, 150);
        _eatenButton.Color = Palette.Danger;
#endif
#if CH06 && !CH09
        _newMazeButton.Bounds = new Rectangle(SafeArea.Right - 460, SafeArea.Top, 460, 140);
#endif
#if CH07
        FitMap();
#endif
#if CH14
        _controls.Layout(SafeArea);
#endif
#if CH25
        _hud.Layout(SafeArea, Size);
#endif
    }

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);
#if CH07
        _debug.Update(Input);
#endif

        bool pause = Input.BackPressed;
#if CH25
        pause |= _hud.PauseTapped(Input, deltaSeconds);
#endif
        if (pause && !_gameOver)
        {
            Pause();
            return;
        }
#if CH39

        // After a pause, count down before the maze comes back to life.
        if (_countdown > 0f)
        {
            UpdateCountdown(deltaSeconds);
            return;
        }
#endif

        _time += deltaSeconds;
#if CH33

        if (_eaten.IsActive)
        {
            UpdateEaten(deltaSeconds);
            return;
        }
#endif
#if CH34

        if (_escape.IsActive)
        {
            UpdateEscape(deltaSeconds);
            return;
        }
#endif
#if !CH13

        // Until the game has a maze to escape from and a monster to be eaten by, two buttons
        // let us try out both endings.
        if (_escapeButton.Update(Input, deltaSeconds))
            EndGame(GameOutcome.Escaped);
        else if (_eatenButton.Update(Input, deltaSeconds))
            EndGame(GameOutcome.Eaten);
#endif
#if CH06 && !CH09

        if (_newMazeButton.Update(Input, deltaSeconds))
        {
            // A new seed means a completely new maze.
            BuildMaze(Random.Shared.Next());
#if CH07
            FitMap();
#endif
        }
#endif
#if CH10 && !CH13

        // Tap the left or right of the screen to look around.
        foreach (Tap tap in Input.Taps)
            _lookDirection = tap.End.X < Size.X / 2f ? _lookDirection.TurnLeft() : _lookDirection.TurnRight();
#endif
#if CH13

        UpdatePlayer(deltaSeconds);
        if (_gameOver)
            return;
#endif
#if CH16
        UpdateRex(deltaSeconds);
#endif
#if CH20
        _warnings.Update(deltaSeconds, _brain, _rex, _player, _distances);
#endif
#if CH22
        _score.Update(deltaSeconds, _brain.State is RexState.Stalking or RexState.Charging, _distances[_rex.Cell]);
#endif
#if CH25
        _hud.Update(deltaSeconds, _score.Score);
#endif
#if CH27
        _audio.Update(deltaSeconds, _warnings.Danger, _brain.State == RexState.Stalking, _rex, _player,
            _distances[_rex.Cell]
#if CH28
            , WorldSpace.CellCentre(_maze.OutsideExit, WorldSpace.EyeHeight), _distances[_maze.Exit]
#endif
        );
#endif
#if CH30
        UpdateEffects(deltaSeconds, _warnings.Danger);
#endif
#if CH32
        _dust.Update(deltaSeconds, _player.Position, _time);
#endif
#if CH07
        UpdateDebugText();
#endif
    }

    public override void Draw(GameTime gameTime)
    {
#if CH08
        DrawWorld(gameTime);
#endif

        BeginSpriteBatch();
#if !CH05
        Fonts.DrawOutlined(SpriteBatch, "GAMEPLAY", new Vector2(Size.X / 2f, Size.Y * 0.35f), 160f, Palette.Accent,
            Color.Black, 6f);
        Fonts.DrawShadowed(SpriteBatch, "THIS SCREEN IS ALWAYS LANDSCAPE", new Vector2(Size.X / 2f, Size.Y * 0.5f), 56f,
            Palette.Text);
#endif
#if CH05 && !CH07
        DrawSimpleMap();
#endif
#if CH07
        DrawMap();
#endif
#if !CH13
        _escapeButton.Draw(SpriteBatch, Assets);
        _eatenButton.Draw(SpriteBatch, Assets);
#endif
#if CH06 && !CH09
        _newMazeButton.Draw(SpriteBatch, Assets);
#endif
#if CH14
        if (!_gameOver && !_player.Frozen)
            _controls.Draw(SpriteBatch, Assets, Size);
#endif
#if CH20 && !CH25
        Fonts.DrawOutlined(SpriteBatch, _warnings.Current.Text, new Vector2(Size.X / 2f, SafeArea.Top + 60f), 70f,
            _warnings.Current.Level == WarningLevel.Critical ? Palette.Danger : Palette.Gold, Color.Black, 4f);
#endif
#if CH22 && !CH25
        Fonts.DrawShadowed(SpriteBatch, $"SCORE {_score.Score:N0}", new Vector2(SafeArea.X, SafeArea.Top + 60f), 64f,
            Palette.Gold, TextAlign.Left);
#endif
#if CH25
        if (!_gameOver && !_player.Frozen)
            _hud.Draw(SpriteBatch, Assets, _warnings, Difficulty, Game.Settings, _time);
#endif
#if CH30
        _danger.DrawFlash(SpriteBatch, Size);
#endif
#if CH33
        _eaten.Draw(SpriteBatch, Assets, Size);
#endif
#if CH34
        _escape.Draw(SpriteBatch, Assets, Size);
#endif
#if CH39
        DrawCountdown();
#endif
#if CH07
        _debug.Draw(SpriteBatch, Fonts, SafeArea);
#endif
        SpriteBatch.End();
    }

    private void Pause()
    {
#if CH39
        _audio.Pause();
#endif
        Manager.Add(new PauseScreen(this));
    }
#if CH39

    /// <summary>Called by the pause menu. A short countdown gives the player time to get ready.</summary>
    public void Resume()
    {
        _audio.Resume();
        _countdown = 3f;
        _countdownNumber = 4;
    }

    /// <summary>Starts again with a brand-new random maze at the same difficulty.</summary>
    public void Restart()
    {
        Manager.SwitchTo(new GameplayScreen(GameSession.NewGame(_session.Difficulty)), TransitionStyle.Wipe);
    }

    /// <summary>Phone calls, notifications and app switches pause the game automatically.</summary>
    private void OnGameDeactivated(object sender, EventArgs e)
    {
        if (!_gameOver && Manager.TopScreen == this)
            Pause();
    }

    private void UpdateCountdown(float deltaSeconds)
    {
        _countdown -= deltaSeconds;
        int number = (int)MathF.Ceiling(_countdown);
        if (number == _countdownNumber)
            return;

        _countdownNumber = number;
        Game.Audio.Play(number > 0 ? Sounds.CountdownBeep : Sounds.CountdownGo);
    }

    private void DrawCountdown()
    {
        if (_countdown <= 0f)
            return;

        // Each number pops in large and shrinks.
        float fraction = _countdown - MathF.Floor(_countdown);
        float size = 260f + fraction * 120f;
        Fonts.DrawOutlined(SpriteBatch, MathF.Ceiling(_countdown).ToString("0"), Size / 2f, size,
            Palette.Gold * MathF.Min(1f, fraction * 3f), Color.Black, 8f);
    }
#endif

    private void EndGame(GameOutcome outcome)
    {
        if (_gameOver)
            return;

        _gameOver = true;
        _session.Outcome = outcome;
#if CH22
        _session.Score = _score.Score;
        _session.Seconds = _time;
        _session.Steps = _score.Steps;
        _session.CellsExplored = _score.CellsExplored;
        _session.NearMisses = _score.NearMisses;
#endif
#if CH27
        _audio.Stop();
#endif

#if CH34
        GameScreen next = outcome == GameOutcome.Eaten ? new EatenScreen(_session) : new EscapedScreen(_session);
#elif CH33
        GameScreen next = outcome == GameOutcome.Eaten ? new EatenScreen(_session) : new GameOverScreen(_session);
#else
        GameScreen next = new GameOverScreen(_session);
#endif
        Manager.SwitchTo(next);
    }
#if CH06

    private void BuildMaze(int seed)
    {
#if CH23
        _maze = MazeGenerator.Generate(Difficulty.MazeWidth, Difficulty.MazeHeight, seed, Difficulty.BraidChance);
#else
        _maze = MazeGenerator.Generate(15, 15, seed, 0.15f);
#endif
#if CH07 && !CH17
        _startDistances = new DistanceMap(_maze);
        _startDistances.Build(_maze.Start);
#endif
    }
#endif
#if CH05 && !CH07

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

#if CH06
        string title = $"RANDOM MAZE  {_maze.Width} x {_maze.Height}   SEED {_maze.Seed}";
#else
        string title = $"A HAND-MADE MAZE  {_maze.Width} x {_maze.Height}";
#endif
        Fonts.DrawShadowed(SpriteBatch, title, new Vector2(Size.X / 2f, SafeArea.Top + 60f), 60f, Palette.Text);
    }
#endif
#if CH07

    private void FitMap()
    {
#if CH08
        // Once there is a 3D view, the map becomes a small developer's overlay.
        int size = (int)(Size.Y * 0.45f);
        _map.Fit(_maze, new Rectangle(SafeArea.Right - size, SafeArea.Top + 170, size, size));
#else
        _map.Fit(_maze, new Rectangle(SafeArea.X, SafeArea.Top + 120, SafeArea.Width, SafeArea.Height - 300));
#endif
    }

    private void DrawMap()
    {
#if CH08
        if (!_debug.Visible)
            return;
#endif
        _map.DrawBackground(SpriteBatch, _maze, Color.Black * 0.75f);
#if CH17
        _map.DrawDistances(SpriteBatch, _distances, 0.75f);
#else
        _map.DrawDistances(SpriteBatch, _startDistances, 0.75f);
#endif
        _map.DrawWalls(SpriteBatch, _maze, Color.White, MathF.Max(2f, _map.CellSize * 0.12f));
        _map.DrawMarker(SpriteBatch, _maze.Start.ToVector2(), Palette.Accent);
        _map.DrawMarker(SpriteBatch, _maze.Exit.ToVector2(), Palette.Gold);
#if CH10 && !CH13
        _map.DrawArrow(SpriteBatch, _maze.Start.ToVector2(), _lookDirection.ToYaw(), Color.White);
#endif
#if CH13
        _map.DrawArrow(SpriteBatch, WorldSpace.ToCellCoordinates(_player.Position), _player.Yaw, Color.White);
#endif
#if CH16
        _map.DrawMarker(SpriteBatch, WorldSpace.ToCellCoordinates(_rex.Position), Palette.Danger, 0.7f);
#endif
#if !CH08
        Fonts.DrawShadowed(SpriteBatch, $"SEED {_maze.Seed}   {_maze.Width} x {_maze.Height}   LONGEST WALK {_startDistances.MaxDistance}",
            new Vector2(Size.X / 2f, SafeArea.Top + 60f), 52f, Palette.Text);
#endif
    }

    /// <summary>Fills the developer overlay (three-finger tap or M) with facts about the game.</summary>
    private void UpdateDebugText()
    {
        _debug.Clear();
        if (!_debug.Visible)
            return;

        _debug.Add($"SEED {_maze.Seed}  SIZE {_maze.Width}x{_maze.Height}");
        _debug.Add($"START {_maze.Start}  EXIT {_maze.Exit} {_maze.ExitSide}");
#if CH12
        _debug.Add($"QUALITY {Game.Settings.Quality}  TRIANGLES {_renderer.Mesh.TriangleCount}");
#endif
#if CH13
        _debug.Add($"PLAYER {_player.Cell} FACING {_player.Facing}  STEPS {_player.StepsTaken}");
#endif
#if CH17
        _debug.Add($"REX {_rex.Cell}  {_distances[_rex.Cell]} STEPS AWAY");
#endif
#if CH19
        _debug.Add($"REX STATE {_brain.State}  SEES {_brain.CanSeePlayer}  HEARS {_brain.CanHearPlayer}");
#elif CH17
        _debug.Add($"SEES {RexSenses.CanSee(_maze, _rex.Cell, _player.Cell, Tuning.SightRange)}  " +
                   $"HEARS {RexSenses.CanHear(_distances, _rex.Cell, Tuning.HearingRange)}");
#endif
#if CH20
        _debug.Add($"DANGER {_warnings.Danger:0.00}");
#endif
    }
#endif
#if CH08

    private void DrawWorld(GameTime gameTime)
    {
        float seconds = (float)gameTime.TotalGameTime.TotalSeconds;
#if CH29

        // Draw the 3D world into an off-screen target, ready for post-processing.
        _post.BeginScene();
#endif
#if CH11
        GraphicsDevice.Clear(_renderer.FogColor);
#else
        GraphicsDevice.Clear(new Color(12, 8, 20));
#endif
        float aspectRatio = GraphicsDevice.Viewport.AspectRatio;
#if CH08 && !CH09

        _camera.AspectRatio = aspectRatio;
        _camera.LookAt(new Vector3(0f, 2f, 7f), Vector3.Zero);
        _testScene.Draw(_camera, seconds);
#endif
#if CH09 && !CH10

        // Circle slowly above the maze to admire it.
        var centre = new Vector3(_maze.Width, 0f, _maze.Height) * WorldSpace.CellSize / 2f;
        float radius = MathF.Max(_maze.Width, _maze.Height) * WorldSpace.CellSize * 0.75f;
        _camera.AspectRatio = aspectRatio;
        _camera.FarPlane = radius * 4f;
        _camera.LookAt(centre + new Vector3(MathF.Sin(_time * 0.25f) * radius, radius, MathF.Cos(_time * 0.25f) * radius),
            centre);
        _renderer.Draw(_camera, seconds);
#endif
#if CH10

        PositionCamera(aspectRatio);
        _renderer.Draw(_camera, seconds);
#endif
#if CH16
        _rexRenderer.Draw(_rex, _camera, _renderer);
#endif
#if CH32
        _dust.Draw(_camera, _renderer);
#endif
#if CH29

        _post.EndScene();
#endif
    }
#endif
#if CH10

    private void PositionCamera(float aspectRatio)
    {
        _camera.FitFieldOfView(aspectRatio);
#if CH11
        _camera.FarPlane = _renderer.FogEnd + 2f;
#endif
#if CH13
        _camera.Position = _player.Position;
        _camera.Yaw = _player.Yaw;
#else
        _camera.Position = WorldSpace.CellCentre(_maze.Start, WorldSpace.EyeHeight);
        _camera.Yaw = _lookDirection.ToYaw();
#endif
#if CH30
        _camera.Position += _danger.ShakeOffset;
        _camera.Roll = _danger.ShakeRoll;
#endif
#if CH33

        if (_eaten.IsActive)
        {
            // Snap round to face Rex as he lunges.
            Vector3 toRex = _rex.Position - _player.Position;
            float yawToRex = MathF.Atan2(toRex.X, -toRex.Z);
            _camera.Yaw += MathHelper.WrapAngle(yawToRex - _camera.Yaw) * _eaten.TurnAmount;
            _camera.Pitch = 0.18f * _eaten.TurnAmount;
        }
#endif
#if CH34

        if (_escape.IsActive)
        {
            // Keep walking out into the light.
            _camera.Position += new Vector3(MathF.Sin(_player.Yaw), 0f, -MathF.Cos(_player.Yaw)) * _escape.WalkDistance;
        }
#endif
        _camera.Update();
    }
#endif
#if CH13

    private void UpdatePlayer(float deltaSeconds)
    {
#if CH14
        _player.Queue(_controls.Poll(Input, deltaSeconds));
#else
        _player.Queue(PollTapZones());
#endif
        _player.Update(deltaSeconds);
#if CH15

        if (_player.HasEscaped && !_gameOver)
        {
#if CH34
            StartEscape();
#else
#if CH22
            _score.AwardEscape(_time);
#endif
            EndGame(GameOutcome.Escaped);
#endif
        }
#endif
    }

#endif
#if CH17

    private void OnPlayerEnteredCell(Player player)
    {
        // Sound travels along corridors, so re-measure walking distances from the player's new cell.
        if (_maze.InBounds(player.Cell))
            _distances.Build(player.Cell);
#if CH22
        _score.OnStep(player.Cell);
#endif
    }
#endif
#if CH13 && !CH14

    /// <summary>Simple tap zones: left third turns left, right third turns right, the middle walks.</summary>
    private PlayerAction PollTapZones()
    {
        if (Input.IsKeyDown(Keys.Up))
            return PlayerAction.Forward;
        if (Input.IsKeyDown(Keys.Down))
            return PlayerAction.Back;
        if (Input.IsKeyDown(Keys.Left))
            return PlayerAction.TurnLeft;
        if (Input.IsKeyDown(Keys.Right))
            return PlayerAction.TurnRight;

        foreach (var tap in Input.Taps)
        {
            float x = tap.End.X / Size.X;
            if (x < 0.3f)
                return PlayerAction.TurnLeft;
            if (x > 0.7f)
                return PlayerAction.TurnRight;
            return tap.End.Y > Size.Y * 0.75f ? PlayerAction.Back : PlayerAction.Forward;
        }

        return PlayerAction.None;
    }
#endif
#if CH14

    private void OnPlayerBumped(Player player)
    {
        if (Game.Settings.Haptics)
            Game.Platform.Vibrate(HapticStrength.Light);
#if CH27
        _audio.WallBump();
#endif
    }
#endif
#if CH26

    private void OnPlayerFootstep(Player player)
    {
#if CH27
        _audio.PlayerFootstep();
#else
        Game.Audio.PlayRandom(Sounds.Footsteps, 0.5f);
#endif
    }
#endif
#if CH16

    private Point ChooseRexCell()
    {
#if CH21
        return RexSpawner.ChooseCell(_maze, Tuning, new Random(_session.Seed ^ 0x7EE5));
#else
        // For now, Rex waits in the dead end farthest from the start.
        var distances = new DistanceMap(_maze);
        distances.Build(_maze.Start);
        return distances.FindFarthest(_maze.IsDeadEnd);
#endif
    }

    private void UpdateRex(float deltaSeconds)
    {
#if CH17 && !CH18
        // Rex can't move yet, but he roars when he sees the player.
        bool seesPlayer = RexSenses.CanSee(_maze, _rex.Cell, _player.Cell, Tuning.SightRange);
        _rex.Play(seesPlayer ? RexAnimation.Roar : RexAnimation.Idle);
#endif
#if CH18 && !CH19
        // For now Rex always knows where the player is and walks the shortest path towards them.
        if (!_rex.IsMoving)
        {
            if (_distances.TryStepTowardOrigin(_rex.Cell, out Direction step))
                _rex.MoveTo(_rex.Cell + step.ToOffset(), Tuning.StalkStepSeconds);
            else
                _rex.Play(RexAnimation.Idle);
        }
#endif
#if CH19
        _brain.Update(deltaSeconds, _player, _distances);
#endif
        _rex.Update(deltaSeconds);
#if CH18
        CheckCaught();
#endif
    }
#endif
#if CH18

    private void CheckCaught()
    {
        if (_gameOver)
            return;

        // Compare positions on the floor: if Rex and the player overlap, the game is up.
        Vector3 gap = _rex.Position - _player.Position;
        gap.Y = 0f;
        if (gap.Length() > 1.1f)
            return;

#if CH33
        StartEaten();
#else
#if CH19
        _brain.Attack();
#endif
        EndGame(GameOutcome.Eaten);
#endif
    }
#endif
#if CH25

    private void OnScoreAwarded(int points, string reason)
    {
        _hud.ShowAward(points, reason);
#if CH27
        _audio.NearMiss();
#endif
    }
#endif
#if CH27

    private void OnRexFootstep(Rex rex)
    {
        int steps = _distances[rex.Cell];
        bool inSight = RexSenses.CanSee(_maze, _player.Cell, rex.Cell, 12);
        _audio.RexFootstep(rex, _player, steps, inSight);
#if CH30

        // The closer he is, the harder the ground shakes.
        float strength = MathHelper.Clamp(1f - steps / 8f, 0f, 1f);
        _danger.Shake(strength * 0.7f);
        if (steps <= 3 && Game.Settings.Haptics)
            Game.Platform.Vibrate(HapticStrength.Medium);
#endif
#if CH32
        if (steps <= 5)
            _dust.Burst(new Vector3(_player.Position.X, 0f, _player.Position.Z), (int)(strength * 24f));
#endif
    }

    private void OnRexStateChanged(RexState state)
    {
        _audio.RexStateChanged(state, _rex, _player, _distances[_rex.Cell]);
#if CH30

        if (state == RexState.Charging)
        {
            _danger.Flash(new Color(255, 70, 40), 0.45f);
            _danger.Shake(0.6f);
            if (Game.Settings.Haptics)
                Game.Platform.Vibrate(HapticStrength.Heavy);
        }
#endif
    }

    private void OnRexSniffed() => _audio.RexSniff(_rex, _player, _distances[_rex.Cell]);
#endif
#if CH30

    private void UpdateEffects(float deltaSeconds, float danger)
    {
        _danger.Update(deltaSeconds, danger, _brain.State == RexState.Charging, _post
#if CH31
            , Game.Settings.Brightness
#endif
        );
    }
#endif
#if CH33

    private void StartEaten()
    {
        _eaten.Start();
        _brain.Attack();
        _player.Frozen = true;
        _audio.Stop();
        Game.Audio.Play(Sounds.RexRoar);
        _danger.Shake(1f);
        _danger.Flash(new Color(200, 0, 0), 0.5f);
        if (Game.Settings.Haptics)
            Game.Platform.Vibrate(HapticStrength.Heavy);
    }

    private void UpdateEaten(float deltaSeconds)
    {
        float before = _eaten.Time;
        _eaten.Update(deltaSeconds);

        // The jaws snap shut.
        if (before < EatenSequence.ChompTime && _eaten.Time >= EatenSequence.ChompTime)
        {
            Game.Audio.Play(Sounds.Chomp);
            _danger.Shake(1f);
            _danger.Flash(new Color(140, 0, 0), 0.8f);
            if (Game.Settings.Haptics)
                Game.Platform.Vibrate(HapticStrength.Heavy);
        }

        _rex.Update(deltaSeconds);
        UpdateEffects(deltaSeconds, 1f);
        _dust.Update(deltaSeconds, _player.Position, _time);

        if (_eaten.IsFinished)
            EndGame(GameOutcome.Eaten);
    }
#endif
#if CH34

    private void StartEscape()
    {
        (_session.EscapeBonus, _session.TimeBonus) = _score.AwardEscape(_time);
        _escape.Start();
        _player.Frozen = true;
        _audio.Stop();
        Game.Audio.Play(Sounds.EscapeFanfare);
        if (Game.Settings.Haptics)
            Game.Platform.Vibrate(HapticStrength.Medium);
    }

    private void UpdateEscape(float deltaSeconds)
    {
        _escape.Update(deltaSeconds);

        // Light floods in: turn up the exposure and the glow, and let the danger fade away.
        _renderer.Brightness = _escape.Exposure;
        _post.BloomIntensity = 0.9f + (_escape.Exposure - 1f) * 0.8f;
        _post.BloomThreshold = MathHelper.Lerp(0.72f, 0.35f, _escape.WhiteOut);
        UpdateEffects(deltaSeconds, 0f);
        _dust.Update(deltaSeconds, _player.Position, _time);

        if (_escape.IsFinished)
            EndGame(GameOutcome.Escaped);
    }
#endif
}
