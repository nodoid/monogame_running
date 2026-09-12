using System;
using Foundation;
using Microsoft.Xna.Framework;
using MonsterMaze.Platform;
using UIKit;

namespace MonsterMaze.iOS;

/// <summary>
/// The iOS implementation of <see cref="IPlatformServices"/>.
/// </summary>
public sealed class IosPlatformServices : IPlatformServices
{
    private Game _game;

    /// <summary>Connects the services to the running game so we can reach its view controller.</summary>
    public void Attach(Game game)
    {
        _game = game;
    }

    public string Name => "iOS";

    private UIViewController Controller => _game?.Services.GetService(typeof(UIViewController)) as UIViewController;

    public void SetOrientation(GameOrientation orientation)
    {
        var controller = Controller;
        if (controller == null)
            return;

        if (OperatingSystem.IsIOSVersionAtLeast(16))
        {
            // Ask iOS to re-read the supported orientations (MonoGame reports them from
            // GraphicsDeviceManager.SupportedOrientations), then request the rotation.
            var mask = orientation == GameOrientation.Landscape
                ? UIInterfaceOrientationMask.Landscape
                : UIInterfaceOrientationMask.Portrait;
            controller.SetNeedsUpdateOfSupportedInterfaceOrientations();
            controller.View?.Window?.WindowScene?.RequestGeometryUpdate(
                new UIWindowSceneGeometryPreferencesIOS(mask), _ => { });
        }
        else
        {
            // Before iOS 16 the only way to force a rotation was to set the device orientation.
            var value = orientation == GameOrientation.Landscape
                ? UIInterfaceOrientation.LandscapeRight
                : UIInterfaceOrientation.Portrait;
            UIDevice.CurrentDevice.SetValueForKey(NSNumber.FromInt32((int)value), new NSString("orientation"));
#pragma warning disable CA1422 // Only used on iOS 15.
            UIViewController.AttemptRotationToDeviceOrientation();
#pragma warning restore CA1422
        }
    }

    public Insets GetSafeAreaInsets()
    {
        var view = Controller?.View;
        if (view == null)
            return Insets.Zero;

        // Ask the window rather than the game's view: the window's insets follow the rotation,
        // so in landscape the notch or Dynamic Island moves to the left or right edge.
        var insets = view.Window?.SafeAreaInsets ?? view.SafeAreaInsets;

        // UIKit measures in points; the back buffer is in pixels.
        double scale = view.ContentScaleFactor;
        return new Insets(
            (int)(insets.Left * scale), (int)(insets.Top * scale),
            (int)(insets.Right * scale), (int)(insets.Bottom * scale));
    }

    /// <summary>iPhones and iPads have no back button.</summary>
    public bool ConsumeBackRequest() => false;

    public void Vibrate(HapticStrength strength)
    {
        var style = strength switch
        {
            HapticStrength.Light => UIImpactFeedbackStyle.Light,
            HapticStrength.Medium => UIImpactFeedbackStyle.Medium,
            _ => UIImpactFeedbackStyle.Heavy
        };

        // iOS 17.5 ties feedback generators to a view; earlier versions create them directly.
        UIImpactFeedbackGenerator generator;
        if (OperatingSystem.IsIOSVersionAtLeast(17, 5) && Controller?.View is { } view)
            generator = UIImpactFeedbackGenerator.GetFeedbackGenerator(style, view);
        else
#pragma warning disable CA1422 // The only option before iOS 17.5.
            generator = new UIImpactFeedbackGenerator(style);
#pragma warning restore CA1422

        using (generator)
        {
            generator.Prepare();
            generator.ImpactOccurred();
        }
    }
}
