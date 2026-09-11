using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonsterMaze.Data;
using MonsterMaze.Input;
using MonsterMaze.Rendering;
using MonsterMaze.UI;

namespace MonsterMaze.Screens;

/// <summary>The player's settings. Changes apply straight away and are saved on the way out.</summary>
public sealed class OptionsScreen : GameScreen
{
    private readonly List<Option> _options = new();
    private readonly Button _back = new("BACK", Icons.Back);

    private Settings Settings => Game.Settings;

    public override void Load()
    {
        _options.Add(Option.Slider("SOUND", () => Percent(Settings.SoundVolume),
            step => Settings.SoundVolume = Math.Clamp(Settings.SoundVolume + step * 0.1f, 0f, 1f)));
        _options.Add(Option.Slider("MUSIC", () => Percent(Settings.MusicVolume),
            step => Settings.MusicVolume = Math.Clamp(Settings.MusicVolume + step * 0.1f, 0f, 1f)));
        _options.Add(Option.Toggle("CONTROLS", () => Settings.Controls == ControlScheme.Swipe ? "SWIPE" : "BUTTONS",
            () => Settings.Controls = Settings.Controls == ControlScheme.Swipe ? ControlScheme.Buttons : ControlScheme.Swipe));
        _options.Add(Option.Toggle("GRAPHICS", () => Settings.Quality.ToString().ToUpperInvariant(), () =>
        {
            Settings.Quality = (GraphicsQuality)(((int)Settings.Quality + 1) % 3);
            Settings.QualityChosen = true;
        }));
        _options.Add(Option.Slider("BRIGHTNESS", () => ((int)MathF.Round(Settings.Brightness * 100f)).ToString("+0;-0;0"),
            step => Settings.Brightness = Math.Clamp(Settings.Brightness + step * 0.05f, -0.1f, 0.2f)));
        _options.Add(Option.Toggle("REDUCE FLASHING", () => OnOff(Settings.ReduceFlashing),
            () => Settings.ReduceFlashing = !Settings.ReduceFlashing));
        _options.Add(Option.Toggle("VIBRATION", () => OnOff(Settings.Haptics), () => Settings.Haptics = !Settings.Haptics));
        _options.Add(Option.Toggle("BATTERY SAVER", () => OnOff(Settings.BatterySaver), () =>
        {
            Settings.BatterySaver = !Settings.BatterySaver;
            Game.ApplyFrameRate();
        }));
        _options.Add(Option.Toggle("SHOW FPS", () => OnOff(Settings.ShowFps), () => Settings.ShowFps = !Settings.ShowFps));
    }

    public override void Layout()
    {
        float top = SafeArea.Top + 260f;
        float available = SafeArea.Bottom - 200f - top;
        float spacing = MathF.Min(160f, available / _options.Count);
        int height = (int)MathF.Min(130f, spacing - 16f);
        int width = Math.Min(SafeArea.Width, 1000);
        float left = Size.X / 2f - width / 2f;

        for (int i = 0; i < _options.Count; i++)
            _options[i].Layout(new Rectangle((int)left, (int)(top + i * spacing), width, height));

        _back.Bounds = CentredRect(SafeArea.Bottom - 80f, 520, 140);
        _back.Color = Palette.ButtonSecondary;
    }

    public override void Update(GameTime gameTime)
    {
        float deltaSeconds = Seconds(gameTime);
        foreach (Option option in _options)
            option.Update(Input, deltaSeconds);

        if (_back.Update(Input, deltaSeconds) || Input.BackPressed)
        {
            Game.Save();
            Manager.SwitchTo(new MainMenuScreen());
        }
    }

    public override void Draw(GameTime gameTime)
    {
        BeginSpriteBatch();
        DrawBackground(Assets.MenuBackground, new Color(100, 100, 100));
        Fonts.DrawOutlined(SpriteBatch, "OPTIONS", new Vector2(Size.X / 2f, SafeArea.Top + 130f), 130f, Palette.Gold,
            Color.Black, 6f);

        foreach (Option option in _options)
            option.Draw(SpriteBatch, Assets);
        _back.Draw(SpriteBatch, Assets);
        SpriteBatch.End();
    }

    private static string Percent(float value) => $"{(int)MathF.Round(value * 100f)}%";

    private static string OnOff(bool value) => value ? "ON" : "OFF";

    /// <summary>One row: a label, the current value, and either -/+ buttons or a toggle.</summary>
    private sealed class Option
    {
        private readonly string _label;
        private readonly Func<string> _value;
        private readonly Action<int> _step;
        private readonly Action _toggle;
        private readonly Button _minus = new("-") { Color = Palette.ButtonSecondary };
        private readonly Button _plus = new("+") { Color = Palette.ButtonSecondary };
        private readonly Button _switch = new("") { Color = Palette.Button };
        private Rectangle _bounds;

        private Option(string label, Func<string> value, Action<int> step, Action toggle)
        {
            _label = label;
            _value = value;
            _step = step;
            _toggle = toggle;
        }

        public static Option Slider(string label, Func<string> value, Action<int> step) => new(label, value, step, null);

        public static Option Toggle(string label, Func<string> value, Action toggle) => new(label, value, null, toggle);

        public void Layout(Rectangle bounds)
        {
            _bounds = bounds;
            int size = bounds.Height;
            _plus.Bounds = new Rectangle(bounds.Right - size, bounds.Y, size, size);
            _minus.Bounds = new Rectangle(bounds.Right - size * 3 - 20, bounds.Y, size, size);
            _switch.Bounds = new Rectangle(bounds.Right - 380, bounds.Y, 380, size);
            _minus.TextSize = _plus.TextSize = size * 0.6f;
            _switch.TextSize = size * 0.4f;
        }

        public void Update(InputManager input, float deltaSeconds)
        {
            if (_toggle != null)
            {
                if (_switch.Update(input, deltaSeconds))
                    _toggle();
                return;
            }

            if (_minus.Update(input, deltaSeconds))
                _step(-1);
            if (_plus.Update(input, deltaSeconds))
                _step(1);
        }

        public void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, GameAssets assets)
        {
            float middle = _bounds.Center.Y;
            assets.Fonts.DrawShadowed(spriteBatch, _label, new Vector2(_bounds.X, middle), _bounds.Height * 0.42f,
                Palette.Text, TextAlign.Left);

            if (_toggle != null)
            {
                _switch.Text = _value();
                _switch.Draw(spriteBatch, assets);
                return;
            }

            _minus.Draw(spriteBatch, assets);
            _plus.Draw(spriteBatch, assets);
            float valueX = (_minus.Bounds.Right + _plus.Bounds.Left) / 2f;
            assets.Fonts.DrawShadowed(spriteBatch, _value(), new Vector2(valueX, middle), _bounds.Height * 0.42f,
                Palette.Gold);
        }
    }
}
