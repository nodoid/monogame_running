// @since 04
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsterMaze.Input;
using MonsterMaze.Platform;
using MonsterMaze.UI;

namespace MonsterMaze.Screens;

/// <summary>
/// One screen of the game: the title, a menu, gameplay, and so on. The <see cref="ScreenManager"/>
/// keeps a stack of them. Only the top screen updates; screens underneath a pop-up keep drawing.
/// </summary>
public abstract class GameScreen
{
    public ScreenManager Manager { get; internal set; }

    public MonsterMazeGame Game => Manager.Game;
    public GameAssets Assets => Game.Assets;
    public FontSet Fonts => Game.Assets.Fonts;
    public SpriteBatch SpriteBatch => Game.SpriteBatch;
    public InputManager Input => Game.Input;
    public GraphicsDevice GraphicsDevice => Game.GraphicsDevice;

    /// <summary>The screen size in virtual units.</summary>
    public Vector2 Size => Game.Scaler.VirtualSize;

    /// <summary>The part of the screen clear of notches and rounded corners, in virtual units.</summary>
    public Rectangle SafeArea => Game.Scaler.SafeArea;

    /// <summary>Gameplay is landscape; everything else is portrait.</summary>
    public virtual GameOrientation Orientation => GameOrientation.Portrait;

    /// <summary>Pop-ups (such as the pause menu) draw on top of the screen below them.</summary>
    public virtual bool IsPopup => false;

    /// <summary>Called once when the screen is added.</summary>
    public virtual void Load()
    {
    }

    /// <summary>Called when the screen is removed.</summary>
    public virtual void Unload()
    {
    }

    /// <summary>Called whenever the screen size changes, and once before the first update.</summary>
    public virtual void Layout()
    {
    }

    public virtual void Update(GameTime gameTime)
    {
    }

    public abstract void Draw(GameTime gameTime);

    /// <summary>Removes this screen from the stack.</summary>
    public void Close() => Manager.Remove(this);

    /// <summary>Starts a SpriteBatch that draws in virtual units.</summary>
    protected void BeginSpriteBatch(BlendState blendState = null)
    {
        SpriteBatch.Begin(SpriteSortMode.Deferred, blendState ?? BlendState.AlphaBlend, SamplerState.LinearClamp,
            null, null, null, Game.Scaler.Transform);
    }

    /// <summary>Draws a background image scaled to cover the whole screen, cropping if it must.</summary>
    protected void DrawBackground(Texture2D texture, Color color)
    {
        float scale = System.MathF.Max(Size.X / texture.Width, Size.Y / texture.Height);
        var drawSize = new Vector2(texture.Width, texture.Height) * scale;
        SpriteBatch.Draw(texture, (Size - drawSize) / 2f, null, color, 0f, Vector2.Zero, scale,
            SpriteEffects.None, 0f);
    }

    /// <summary>A rectangle centred horizontally on the screen, with its middle at <paramref name="centreY"/>.</summary>
    protected Rectangle CentredRect(float centreY, int width, int height) =>
        new((int)(Size.X / 2f - width / 2f), (int)(centreY - height / 2f), width, height);

    protected static float Seconds(GameTime gameTime) => (float)gameTime.ElapsedGameTime.TotalSeconds;
}
