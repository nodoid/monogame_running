using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonsterMaze.Gameplay;
using MonsterMaze.Monster;

namespace MonsterMaze.Audio;

/// <summary>
/// All the sound of the maze: footsteps, Rex, the heartbeat, dripping water and the ambience.
/// The gameplay screen tells it what is happening and it decides what to play.
/// </summary>
public sealed class GameplayAudio : IDisposable
{
    private readonly AudioManager _audio;
    private readonly Random _random = new();
    private readonly SoundEffectInstance _ambience;
    private float _heartbeatTimer;
    private float _dripTimer = 3f;
    private float _growlTimer = 4f;
    private bool _stopped;
    private readonly SoundEffectInstance _tension;
    private readonly SoundEffectInstance _exitHum;
    private readonly AudioListener _listener = new();
    private readonly AudioEmitter _emitter = new();
    private readonly Dictionary<string, SoundEffectInstance[]> _pools = new();
    private readonly Dictionary<string, int> _poolNext = new();
    private float _ambienceLevel;
    private float _tensionLevel;
    private float _silence;

    public GameplayAudio(AudioManager audio)
    {
        _audio = audio;
        _ambience = audio.CreateInstance(Sounds.AmbientLoop, looped: true);
        _ambience.Volume = 0f;
        AudioManager.TryPlay(_ambience);
        _tension = audio.CreateInstance(Sounds.TensionLoop, looped: true);
        _tension.Volume = 0f;
        AudioManager.TryPlay(_tension);
        _exitHum = audio.CreateInstance(Sounds.ExitHum, looped: true);
        _exitHum.Volume = 0f;
        AudioManager.TryPlay(_exitHum);
    }

    public void PlayerFootstep() => _audio.PlayRandom(Sounds.Footsteps, 0.5f);

    public void WallBump() => _audio.Play(Sounds.WallBump, 0.8f);

    /// <param name="stepsAway">Walking distance from Rex to the player.</param>
    /// <param name="inSight">True if the player could see Rex down a straight corridor.</param>
    public void RexFootstep(Rex rex, Player player, int stepsAway, bool inSight)
    {
        // Louder when he's close. Heard through the walls, the step is the muffled recording.
        float volume = MathHelper.Clamp(1.1f - stepsAway / 14f, 0.08f, 1f);
        string sound = inSight || stepsAway <= 2 ? Sounds.RexStep : Sounds.RexStepFar;
        PlayAt(sound, volume, rex.Position, player);
    }

    public void RexStateChanged(RexState state, Rex rex, Player player, int stepsAway)
    {
        switch (state)
        {
            case RexState.Charging:
                // A moment of silence, then the stinger and the roar, makes the spot hit harder.
                _silence = 0.9f;
                PlayAt(Sounds.RexRoar, 1f, rex.Position, player);
                _audio.Play(Sounds.StingerSeen, 0.9f);
                break;

            case RexState.Hunting:
                RexSniff(rex, player, stepsAway);
                break;
        }
    }

    public void RexSniff(Rex rex, Player player, int stepsAway)
    {
        float volume = MathHelper.Clamp(1f - stepsAway / 12f, 0.1f, 0.9f);
        PlayAt(Sounds.RexSnort, volume, rex.Position, player);
    }

    public void NearMiss() => _audio.Play(Sounds.NearMiss, 0.9f);

    /// <param name="danger">0 (safe) to 1 (about to be eaten).</param>
    /// <param name="stalking">True while Rex is following the player.</param>
    public void Update(float deltaSeconds, float danger, bool stalking, Rex rex, Player player, int stepsAway
        , Vector3 exitPosition, int stepsToExit
    )
    {
        if (_stopped)
            return;

        // The heartbeat starts once Rex is a real threat and speeds up as he closes in.
        if (danger > 0.25f)
        {
            _heartbeatTimer -= deltaSeconds;
            if (_heartbeatTimer <= 0f)
            {
                _audio.Play(Sounds.Heartbeat, MathHelper.Lerp(0.3f, 1f, danger));
                _heartbeatTimer = MathHelper.Lerp(1.1f, 0.36f, danger);
            }
        }

        // Water drips somewhere in the dark, from a random direction.
        _dripTimer -= deltaSeconds;
        if (_dripTimer <= 0f)
        {
            _dripTimer = 3f + (float)_random.NextDouble() * 6f;
            float pan = (float)_random.NextDouble() * 2f - 1f;
            _audio.PlayRandom(Sounds.Drips, 0.35f + (float)_random.NextDouble() * 0.3f, 0.15f, pan);
        }

        // A low growl now and then while he stalks nearby.
        if (stalking && stepsAway <= 6)
        {
            _growlTimer -= deltaSeconds;
            if (_growlTimer <= 0f)
            {
                _growlTimer = 4f + (float)_random.NextDouble() * 4f;
                PlayAt(Sounds.RexGrowl, 0.8f, rex.Position, player);
            }
        }

        // The music follows the danger: ambience fades down as the tension loop swells up.
        _silence = MathF.Max(0f, _silence - deltaSeconds);
        float silence = _silence > 0f ? 0f : 1f;
        float tensionTarget = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((danger - 0.2f) / 0.6f, 0f, 1f));
        _tensionLevel = MathHelper.Lerp(_tensionLevel, tensionTarget * silence, 1f - MathF.Exp(-deltaSeconds * 2f));
        _ambienceLevel = MathHelper.Lerp(_ambienceLevel, (0.6f - 0.3f * danger) * silence, 1f - MathF.Exp(-deltaSeconds * 1.5f));
        _tension.Volume = MathHelper.Clamp(_tensionLevel * 0.85f * _audio.MusicVolume, 0f, 1f);
        _ambience.Volume = MathHelper.Clamp(_ambienceLevel * _audio.MusicVolume, 0f, 1f);

        // The exit hums with light: follow the sound and it leads you out.
        float hum = MathHelper.Clamp(1f - stepsToExit / 6f, 0f, 1f);
        _exitHum.Volume = MathHelper.Clamp(hum * hum * 0.7f * _audio.SoundVolume, 0f, 1f);
        if (hum > 0f && _exitHum.State == SoundState.Playing)
            Position(_exitHum, exitPosition, player);
    }

    public void Pause()
    {
        _ambience.Pause();
        _tension.Pause();
        _exitHum.Pause();
    }

    public void Resume()
    {
        _ambience.Resume();
        _tension.Resume();
        _exitHum.Resume();
    }

    /// <summary>Silences the loops when the game ends; the endings bring their own sound.</summary>
    public void Stop()
    {
        _stopped = true;
        _ambience.Stop();
        _tension.Stop();
        _exitHum.Stop();
    }

    public void Dispose()
    {
        Stop();
        _ambience.Dispose();
        _tension.Dispose();
        _exitHum.Dispose();
        foreach (SoundEffectInstance[] pool in _pools.Values)
        {
            foreach (SoundEffectInstance instance in pool)
                instance.Dispose();
        }
    }

    /// <summary>
    /// Plays a sound so it seems to come from a point in the maze. Phones only have so many
    /// hardware voices, so each 3D sound reuses a small pool of instances.
    /// </summary>
    private void PlayAt(string sound, float volume, Vector3 source, Player player)
    {
        if (!_pools.TryGetValue(sound, out SoundEffectInstance[] pool))
        {
            pool = new SoundEffectInstance[3];
            for (int i = 0; i < pool.Length; i++)
                pool[i] = _audio.CreateInstance(sound);
            _pools[sound] = pool;
            _poolNext[sound] = 0;
        }

        int next = _poolNext[sound];
        _poolNext[sound] = (next + 1) % pool.Length;

        SoundEffectInstance instance = pool[next];
        instance.Stop();
        instance.Volume = MathHelper.Clamp(volume * _audio.SoundVolume, 0f, 1f);
        if (AudioManager.TryPlay(instance))
            Position(instance, source, player);
    }

    /// <summary>
    /// Points a sound at a position using AudioListener and AudioEmitter. We place the emitter one
    /// unit away in the right direction, so OpenAL only decides left and right. How loud it is we
    /// decide ourselves, from the walking distance through the maze rather than a straight line.
    /// </summary>
    private void Position(SoundEffectInstance instance, Vector3 source, Player player)
    {
        var forward = new Vector3(MathF.Sin(player.Yaw), 0f, -MathF.Cos(player.Yaw));
        _listener.Position = player.Position;
        _listener.Forward = forward;
        _listener.Up = Vector3.Up;

        Vector3 direction = source - player.Position;
        direction.Y = 0f;
        direction = direction.LengthSquared() < 0.0001f ? forward : Vector3.Normalize(direction);
        _emitter.Position = player.Position + direction;

        instance.Apply3D(_listener, _emitter);
    }
}
