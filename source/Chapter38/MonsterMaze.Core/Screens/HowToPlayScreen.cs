using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.UI;

namespace MonsterMaze.Screens;

/// <summary>The rules of the game in four short sections.</summary>
public sealed class HowToPlayScreen : GameScreen
{
    private static readonly (int Icon, string Heading, string Text, Color Color)[] Sections =
    {
        (Icons.Exit, "FIND THE EXIT",
            "Somewhere on the edge of the maze is the way out. Follow the warm light and the humming sound.",
            Palette.Accent),
        (Icons.Skull, "AVOID REX",
            "Rex hunts by sound and charges when he sees you. Watch the warnings at the top of the screen and keep moving.",
            Palette.Danger),
        (Icons.TurnRight, "CONTROLS",
            "Swipe left or right to turn. Swipe up, tap or hold to walk. Swipe down to step back. Prefer buttons? Change it in Options.",
            Palette.Gold),
        (Icons.Star, "SCORING",
            "Points for every step and every new corridor, bonuses for near misses, and a big bonus for escaping quickly.",
            new Color(120, 200, 255))
    };

    private readonly List<string>[] _lines = new List<string>[Sections.Length];
    private readonly Button _back = new("BACK", Icons.Back);

    public override void Layout()
    {
        float width = Math.Min(SafeArea.Width, 1000) - 170f;
        for (int i = 0; i < Sections.Length; i++)
            _lines[i] = Fonts.Wrap(Sections[i].Text, 44f, width);

        _back.Bounds = CentredRect(SafeArea.Bottom - 80f, 520, 140);
        _back.Color = Palette.ButtonSecondary;
    }

    public override void Update(GameTime gameTime)
    {
        if (_back.Update(Input, Seconds(gameTime)) || Input.BackPressed)
            Manager.SwitchTo(new MainMenuScreen());
    }

    public override void Draw(GameTime gameTime)
    {
        BeginSpriteBatch();
        DrawBackground(Assets.MenuBackground, new Color(90, 90, 90));
        Fonts.DrawOutlined(SpriteBatch, "HOW TO PLAY", new Vector2(Size.X / 2f, SafeArea.Top + 130f), 120f, Palette.Gold,
            Color.Black, 6f);

        float left = Size.X / 2f - Math.Min(SafeArea.Width, 1000) / 2f;
        float y = SafeArea.Top + 290f;
        float spacing = (SafeArea.Bottom - 200f - y) / Sections.Length;
        for (int i = 0; i < Sections.Length; i++)
        {
            var section = Sections[i];
            SpriteBatch.Draw(Assets.Icons, new Vector2(left + 60f, y + 40f), Icons.Source(section.Icon), section.Color, 0f,
                new Vector2(Icons.CellSize / 2f), 0.85f, SpriteEffects.None, 0f);
            Fonts.DrawShadowed(SpriteBatch, section.Heading, new Vector2(left + 150f, y + 40f), 64f, section.Color,
                TextAlign.Left);

            float lineY = y + 110f;
            foreach (string line in _lines[i])
            {
                Fonts.Draw(SpriteBatch, line, new Vector2(left + 150f, lineY), 44f, Palette.Text, TextAlign.Left);
                lineY += 54f;
            }

            y += spacing;
        }

        _back.Draw(SpriteBatch, Assets);
        SpriteBatch.End();
    }
}
