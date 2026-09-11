// @since 26
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using MonsterMaze.Data;
using MonsterMaze.UI;

namespace MonsterMaze.Audio;

/// <summary>
/// Loads every sound once and plays them at the player's chosen volumes. Music is played as a
/// looping SoundEffectInstance rather than a Song: sound effects loop without a gap on every
/// platform and several can play at once, which lets us layer music later on.
/// </summary>
public sealed class AudioManager
{
    private readonly Settings _settings;
    private readonly Dictionary<string, SoundEffect> _sounds = new();
    private readonly Random _random = new();

    private SoundEffectInstance _music;
    private SoundEffectInstance _fadingMusic;
    private string _musicName;
    private float _musicLevel;
    private float _fadingLevel;
    private float _fadeSpeed = 1f;
    private bool _stopping;

    public AudioManager(ContentManager content, Settings settings)
    {
        _settings = settings;
        foreach (string name in Sounds.All())
            _sounds[name] = content.Load<SoundEffect>("Audio/" + name);

        // Every button in the game clicks when tapped.
        Button.AnyTapped += () => Play(Sounds.UiSelect, 0.8f);
    }

    public float SoundVolume => _settings.SoundVolume;
    public float MusicVolume => _settings.MusicVolume;

    public SoundEffect Get(string name) => _sounds[name];

    /// <summary>Plays a sound once. Volume is scaled by the player's sound volume setting.</summary>
    public void Play(string name, float volume = 1f, float pitch = 0f, float pan = 0f)
    {
        float finalVolume = MathHelper.Clamp(volume * SoundVolume, 0f, 1f);
        if (finalVolume <= 0.001f)
            return;

        try
        {
            _sounds[name].Play(finalVolume, MathHelper.Clamp(pitch, -1f, 1f), MathHelper.Clamp(pan, -1f, 1f));
        }
        catch (Exception)
        {
            // No audio device, or every voice busy: carry on silently rather than crash.
        }
    }

    /// <summary>
    /// Starts an instance playing. Sound must never crash the game: a phone may have no free
    /// voices, or its audio may be unavailable (for example during a phone call).
    /// </summary>
    public static bool TryPlay(SoundEffectInstance instance)
    {
        try
        {
            instance.Play();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Plays one of several variations with a slightly random pitch, so repeats sound natural.</summary>
    public void PlayRandom(string[] names, float volume = 1f, float pitchVariation = 0.08f, float pan = 0f)
    {
        float pitch = ((float)_random.NextDouble() * 2f - 1f) * pitchVariation;
        Play(names[_random.Next(names.Length)], volume, pitch, pan);
    }

    /// <summary>Creates an instance of a sound, for loops or sounds we need to control while playing.</summary>
    public SoundEffectInstance CreateInstance(string name, bool looped = false)
    {
        SoundEffectInstance instance = _sounds[name].CreateInstance();
        instance.IsLooped = looped;
        return instance;
    }

    /// <summary>Cross-fades to a new music track.</summary>
    public void PlayMusic(string name, float fadeSeconds = 1f)
    {
        if (_musicName == name && _music != null && !_stopping)
            return;

        StopFadingMusic();
        _fadingMusic = _music;
        _fadingLevel = _musicLevel;

        _musicName = name;
        _music = CreateInstance(name, looped: true);
        _musicLevel = 0f;
        _music.Volume = 0f;
        TryPlay(_music);
        _fadeSpeed = 1f / MathF.Max(0.01f, fadeSeconds);
        _stopping = false;
    }

    public void StopMusic(float fadeSeconds = 1f)
    {
        _stopping = true;
        _fadeSpeed = 1f / MathF.Max(0.01f, fadeSeconds);
    }
#if CH39

    /// <summary>Pauses the music, for example when the game is paused or sent to the background.</summary>
    public void PauseMusic()
    {
        _music?.Pause();
        _fadingMusic?.Pause();
    }

    public void ResumeMusic()
    {
        if (_music?.State == SoundState.Paused)
            _music.Resume();
        if (_fadingMusic?.State == SoundState.Paused)
            _fadingMusic.Resume();
    }
#endif

    public void Update(float deltaSeconds)
    {
        if (_music != null)
        {
            float target = _stopping ? 0f : 1f;
            _musicLevel = MathHelper.Clamp(_musicLevel + MathF.Sign(target - _musicLevel) * _fadeSpeed * deltaSeconds, 0f, 1f);
            _music.Volume = _musicLevel * MusicVolume;

            if (_stopping && _musicLevel <= 0f)
            {
                _music.Stop();
                _music.Dispose();
                _music = null;
                _musicName = null;
            }
        }

        if (_fadingMusic != null)
        {
            _fadingLevel -= _fadeSpeed * deltaSeconds;
            if (_fadingLevel <= 0f)
                StopFadingMusic();
            else
                _fadingMusic.Volume = _fadingLevel * MusicVolume;
        }
    }

    private void StopFadingMusic()
    {
        if (_fadingMusic == null)
            return;
        _fadingMusic.Stop();
        _fadingMusic.Dispose();
        _fadingMusic = null;
    }
}
