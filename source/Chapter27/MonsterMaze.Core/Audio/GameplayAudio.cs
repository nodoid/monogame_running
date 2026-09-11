using System;
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

    public GameplayAudio(AudioManager audio)
    {
        _audio = audio;
        _ambience = audio.CreateInstance(Sounds.AmbientLoop, looped: true);
        _ambience.Volume = 0f;
        AudioManager.TryPlay(_ambience);
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
        _audio.Play(sound, volume);
    }

    public void RexStateChanged(RexState state, Rex rex, Player player, int stepsAway)
    {
        switch (state)
        {
            case RexState.Charging:
                // A moment of silence, then the stinger and the roar, makes the spot hit harder.
                _audio.Play(Sounds.RexRoar);
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
        _audio.Play(Sounds.RexSnort, volume);
    }

    public void NearMiss() => _audio.Play(Sounds.NearMiss, 0.9f);

    /// <param name="danger">0 (safe) to 1 (about to be eaten).</param>
    /// <param name="stalking">True while Rex is following the player.</param>
    public void Update(float deltaSeconds, float danger, bool stalking, Rex rex, Player player, int stepsAway
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
                _audio.Play(Sounds.RexGrowl, 0.8f);
            }
        }

        _ambience.Volume = MathHelper.Clamp(0.55f * _audio.MusicVolume, 0f, 1f);
    }

    /// <summary>Silences the loops when the game ends; the endings bring their own sound.</summary>
    public void Stop()
    {
        _stopped = true;
        _ambience.Stop();
    }

    public void Dispose()
    {
        Stop();
        _ambience.Dispose();
    }
}
