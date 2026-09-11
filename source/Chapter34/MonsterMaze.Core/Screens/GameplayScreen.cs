using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.Diagnostics;
using MonsterMaze.Effects;
using MonsterMaze.Gameplay;
using MonsterMaze.Input;
using MonsterMaze.Mazes;
using MonsterMaze.Monster;
using MonsterMaze.Audio;
using MonsterMaze.Platform;
using MonsterMaze.Rendering;
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
    private readonly MazeMapRenderer _map = new();
    private readonly DebugOverlay _debug = new();
    private readonly Camera _camera = new();
    private readonly TouchControls _controls = new();
    private readonly WarningSystem _warnings = new();
    private readonly Hud _hud = new();
    private readonly DangerEffects _danger = new();
    private readonly EatenSequence _eaten = new();
    private readonly EscapeSequence _escape = new();
    private float _time;
    private bool _gameOver;
    private Maze _maze;
    private MazeRenderer _renderer;
    private Player _player;
    private Rex _rex;
    private RexRenderer _rexRenderer;
    private DistanceMap _distances;
    private RexBrain _brain;
    private ScoreKeeper _score;
    private GameplayAudio _audio;
    private PostProcessor _post;
    private DustParticles _dust;

    public GameplayScreen(GameSession session)
    {
        _session = session;
    }

    public override GameOrientation Orientation => GameOrientation.Landscape;

    private DifficultySettings Difficulty => _session.Settings;

    private RexTuning Tuning => _session.Settings.Rex;

    public override void Load()
    {
        Game.Audio.StopMusic(0.8f);
        BuildMaze(_session.Seed);
        _renderer = new MazeRenderer(GraphicsDevice, Assets);
        _renderer.SetMaze(_maze);
        _renderer.Quality = QualityProfile.For(Game.Settings.Quality);
        _renderer.FogStart = Difficulty.FogStart;
        _renderer.FogEnd = Difficulty.FogEnd;
        _player = new Player(_maze, _maze.Start, _maze.StartFacing);
        _player.EnteredCell += OnPlayerEnteredCell;
        _player.Bumped += OnPlayerBumped;
        _controls.Scheme = Game.Settings.Controls;
        _player.Footstep += OnPlayerFootstep;
        _rex = new Rex(ChooseRexCell(), Direction.South);
        _rexRenderer = new RexRenderer(GraphicsDevice, Assets);
        _distances = new DistanceMap(_maze);
        _distances.Build(_player.Cell);
        _brain = new RexBrain(_rex, _maze, Tuning, new Random(_session.Seed ^ 0x5EED));
        _warnings.FootstepRange = Difficulty.FootstepWarningRange;
        _warnings.ShowHuntingWarning = Difficulty.ShowHuntingWarning;
        _score = new ScoreKeeper(_maze, Difficulty.ScoreMultiplier);
        _score.Awarded += OnScoreAwarded;
        _audio = new GameplayAudio(Game.Audio);
        _rex.Footstep += OnRexFootstep;
        _brain.StateChanged += OnRexStateChanged;
        _brain.Sniffed += OnRexSniffed;
        _post = new PostProcessor(GraphicsDevice, SpriteBatch, Assets, QualityProfile.For(Game.Settings.Quality));
        _danger.ReduceFlashing = Game.Settings.ReduceFlashing;
        _dust = new DustParticles(GraphicsDevice, Assets.ParticleSoft)
        {
            FloatingMotes = QualityProfile.For(Game.Settings.Quality).DustParticles
        };
    }

    public override void Unload()
    {
        _renderer.Dispose();
        _rexRenderer.Dispose();
        _audio.Dispose();
        _post.Dispose();
        _dust.Dispose();
    }

    public override void Layout()
    {
        FitMap();
        _controls.Layout(SafeArea);
        _hud.Layout(SafeArea, Size);
    }

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);
        _debug.Update(Input);

        bool pause = Input.BackPressed;
        pause |= _hud.PauseTapped(Input, deltaSeconds);
        if (pause && !_gameOver)
        {
            Pause();
            return;
        }

        _time += deltaSeconds;

        if (_eaten.IsActive)
        {
            UpdateEaten(deltaSeconds);
            return;
        }

        if (_escape.IsActive)
        {
            UpdateEscape(deltaSeconds);
            return;
        }

        UpdatePlayer(deltaSeconds);
        if (_gameOver)
            return;
        UpdateRex(deltaSeconds);
        _warnings.Update(deltaSeconds, _brain, _rex, _player, _distances);
        _score.Update(deltaSeconds, _brain.State is RexState.Stalking or RexState.Charging, _distances[_rex.Cell]);
        _hud.Update(deltaSeconds, _score.Score);
        _audio.Update(deltaSeconds, _warnings.Danger, _brain.State == RexState.Stalking, _rex, _player,
            _distances[_rex.Cell]
            , WorldSpace.CellCentre(_maze.OutsideExit, WorldSpace.EyeHeight), _distances[_maze.Exit]
        );
        UpdateEffects(deltaSeconds, _warnings.Danger);
        _dust.Update(deltaSeconds, _player.Position, _time);
        UpdateDebugText();
    }

    public override void Draw(GameTime gameTime)
    {
        DrawWorld(gameTime);

        BeginSpriteBatch();
        DrawMap();
        if (!_gameOver && !_player.Frozen)
            _controls.Draw(SpriteBatch, Assets, Size);
        if (!_gameOver && !_player.Frozen)
            _hud.Draw(SpriteBatch, Assets, _warnings, Difficulty, Game.Settings, _time);
        _danger.DrawFlash(SpriteBatch, Size);
        _eaten.Draw(SpriteBatch, Assets, Size);
        _escape.Draw(SpriteBatch, Assets, Size);
        _debug.Draw(SpriteBatch, Fonts, SafeArea);
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
        _session.Score = _score.Score;
        _session.Seconds = _time;
        _session.Steps = _score.Steps;
        _session.CellsExplored = _score.CellsExplored;
        _session.NearMisses = _score.NearMisses;
        _audio.Stop();

        GameScreen next = outcome == GameOutcome.Eaten ? new EatenScreen(_session) : new EscapedScreen(_session);
        Manager.SwitchTo(next);
    }

    private void BuildMaze(int seed)
    {
        _maze = MazeGenerator.Generate(Difficulty.MazeWidth, Difficulty.MazeHeight, seed, Difficulty.BraidChance);
    }

    private void FitMap()
    {
        // Once there is a 3D view, the map becomes a small developer's overlay.
        int size = (int)(Size.Y * 0.45f);
        _map.Fit(_maze, new Rectangle(SafeArea.Right - size, SafeArea.Top + 170, size, size));
    }

    private void DrawMap()
    {
        if (!_debug.Visible)
            return;
        _map.DrawBackground(SpriteBatch, _maze, Color.Black * 0.75f);
        _map.DrawDistances(SpriteBatch, _distances, 0.75f);
        _map.DrawWalls(SpriteBatch, _maze, Color.White, MathF.Max(2f, _map.CellSize * 0.12f));
        _map.DrawMarker(SpriteBatch, _maze.Start.ToVector2(), Palette.Accent);
        _map.DrawMarker(SpriteBatch, _maze.Exit.ToVector2(), Palette.Gold);
        _map.DrawArrow(SpriteBatch, WorldSpace.ToCellCoordinates(_player.Position), _player.Yaw, Color.White);
        _map.DrawMarker(SpriteBatch, WorldSpace.ToCellCoordinates(_rex.Position), Palette.Danger, 0.7f);
    }

    /// <summary>Fills the developer overlay (three-finger tap or M) with facts about the game.</summary>
    private void UpdateDebugText()
    {
        _debug.Clear();
        if (!_debug.Visible)
            return;

        _debug.Add($"SEED {_maze.Seed}  SIZE {_maze.Width}x{_maze.Height}");
        _debug.Add($"START {_maze.Start}  EXIT {_maze.Exit} {_maze.ExitSide}");
        _debug.Add($"QUALITY {Game.Settings.Quality}  TRIANGLES {_renderer.Mesh.TriangleCount}");
        _debug.Add($"PLAYER {_player.Cell} FACING {_player.Facing}  STEPS {_player.StepsTaken}");
        _debug.Add($"REX {_rex.Cell}  {_distances[_rex.Cell]} STEPS AWAY");
        _debug.Add($"REX STATE {_brain.State}  SEES {_brain.CanSeePlayer}  HEARS {_brain.CanHearPlayer}");
        _debug.Add($"DANGER {_warnings.Danger:0.00}");
    }

    private void DrawWorld(GameTime gameTime)
    {
        float seconds = (float)gameTime.TotalGameTime.TotalSeconds;

        // Draw the 3D world into an off-screen target, ready for post-processing.
        _post.BeginScene();
        GraphicsDevice.Clear(_renderer.FogColor);
        float aspectRatio = GraphicsDevice.Viewport.AspectRatio;

        PositionCamera(aspectRatio);
        _renderer.Draw(_camera, seconds);
        _rexRenderer.Draw(_rex, _camera, _renderer);
        _dust.Draw(_camera, _renderer);

        _post.EndScene();
    }

    private void PositionCamera(float aspectRatio)
    {
        _camera.FitFieldOfView(aspectRatio);
        _camera.FarPlane = _renderer.FogEnd + 2f;
        _camera.Position = _player.Position;
        _camera.Yaw = _player.Yaw;
        _camera.Position += _danger.ShakeOffset;
        _camera.Roll = _danger.ShakeRoll;

        if (_eaten.IsActive)
        {
            // Snap round to face Rex as he lunges.
            Vector3 toRex = _rex.Position - _player.Position;
            float yawToRex = MathF.Atan2(toRex.X, -toRex.Z);
            _camera.Yaw += MathHelper.WrapAngle(yawToRex - _camera.Yaw) * _eaten.TurnAmount;
            _camera.Pitch = 0.18f * _eaten.TurnAmount;
        }

        if (_escape.IsActive)
        {
            // Keep walking out into the light.
            _camera.Position += new Vector3(MathF.Sin(_player.Yaw), 0f, -MathF.Cos(_player.Yaw)) * _escape.WalkDistance;
        }
        _camera.Update();
    }

    private void UpdatePlayer(float deltaSeconds)
    {
        _player.Queue(_controls.Poll(Input, deltaSeconds));
        _player.Update(deltaSeconds);

        if (_player.HasEscaped && !_gameOver)
        {
            StartEscape();
        }
    }

    private void OnPlayerEnteredCell(Player player)
    {
        // Sound travels along corridors, so re-measure walking distances from the player's new cell.
        if (_maze.InBounds(player.Cell))
            _distances.Build(player.Cell);
        _score.OnStep(player.Cell);
    }

    private void OnPlayerBumped(Player player)
    {
        if (Game.Settings.Haptics)
            Game.Platform.Vibrate(HapticStrength.Light);
        _audio.WallBump();
    }

    private void OnPlayerFootstep(Player player)
    {
        _audio.PlayerFootstep();
    }

    private Point ChooseRexCell()
    {
        return RexSpawner.ChooseCell(_maze, Tuning, new Random(_session.Seed ^ 0x7EE5));
    }

    private void UpdateRex(float deltaSeconds)
    {
        _brain.Update(deltaSeconds, _player, _distances);
        _rex.Update(deltaSeconds);
        CheckCaught();
    }

    private void CheckCaught()
    {
        if (_gameOver)
            return;

        // Compare positions on the floor: if Rex and the player overlap, the game is up.
        Vector3 gap = _rex.Position - _player.Position;
        gap.Y = 0f;
        if (gap.Length() > 1.1f)
            return;

        StartEaten();
    }

    private void OnScoreAwarded(int points, string reason)
    {
        _hud.ShowAward(points, reason);
        _audio.NearMiss();
    }

    private void OnRexFootstep(Rex rex)
    {
        int steps = _distances[rex.Cell];
        bool inSight = RexSenses.CanSee(_maze, _player.Cell, rex.Cell, 12);
        _audio.RexFootstep(rex, _player, steps, inSight);

        // The closer he is, the harder the ground shakes.
        float strength = MathHelper.Clamp(1f - steps / 8f, 0f, 1f);
        _danger.Shake(strength * 0.7f);
        if (steps <= 3 && Game.Settings.Haptics)
            Game.Platform.Vibrate(HapticStrength.Medium);
        if (steps <= 5)
            _dust.Burst(new Vector3(_player.Position.X, 0f, _player.Position.Z), (int)(strength * 24f));
    }

    private void OnRexStateChanged(RexState state)
    {
        _audio.RexStateChanged(state, _rex, _player, _distances[_rex.Cell]);

        if (state == RexState.Charging)
        {
            _danger.Flash(new Color(255, 70, 40), 0.45f);
            _danger.Shake(0.6f);
            if (Game.Settings.Haptics)
                Game.Platform.Vibrate(HapticStrength.Heavy);
        }
    }

    private void OnRexSniffed() => _audio.RexSniff(_rex, _player, _distances[_rex.Cell]);

    private void UpdateEffects(float deltaSeconds, float danger)
    {
        _danger.Update(deltaSeconds, danger, _brain.State == RexState.Charging, _post
            , Game.Settings.Brightness
        );
    }

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
}
