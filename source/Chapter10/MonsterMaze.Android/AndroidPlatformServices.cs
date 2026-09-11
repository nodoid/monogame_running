using System;
using System.Threading;
using Android.App;
using Android.Content.PM;
using Android.Views;
using MonsterMaze.Platform;

namespace MonsterMaze.Android;

/// <summary>
/// The Android implementation of <see cref="IPlatformServices"/>.
/// </summary>
public sealed class AndroidPlatformServices : IPlatformServices
{
    private readonly Activity _activity;
    private int _backRequests;

    public AndroidPlatformServices(Activity activity)
    {
        _activity = activity;
    }

    public string Name => "Android";

    public void SetOrientation(GameOrientation orientation)
    {
        // MonoGame does not rotate an Android activity for us, so ask Android directly.
        // The "sensor" variants still let the player flip the phone the other way up.
        var wanted = orientation == GameOrientation.Landscape
            ? ScreenOrientation.SensorLandscape
            : ScreenOrientation.SensorPortrait;

        _activity.RunOnUiThread(() =>
        {
            if (_activity.RequestedOrientation != wanted)
                _activity.RequestedOrientation = wanted;
        });
    }

    public Insets GetSafeAreaInsets()
    {
        // Phones only report camera cut-outs from Android 9.
        if (!OperatingSystem.IsAndroidVersionAtLeast(28))
            return Insets.Zero;

        try
        {
            var cutout = _activity.Window?.DecorView?.RootWindowInsets?.DisplayCutout;
            if (cutout == null)
                return Insets.Zero;

            return new Insets(cutout.SafeInsetLeft, cutout.SafeInsetTop, cutout.SafeInsetRight, cutout.SafeInsetBottom);
        }
        catch (Exception)
        {
            // Insets are only a nicety; never let them crash the game.
            return Insets.Zero;
        }
    }

    /// <summary>Called by the activity when the back button or back gesture is used.</summary>
    public void NotifyBackPressed() => Interlocked.Increment(ref _backRequests);

    public bool ConsumeBackRequest() => Interlocked.Exchange(ref _backRequests, 0) > 0;

    /// <summary>
    /// Hides the status and navigation bars and lets the game draw behind any camera cut-out.
    /// </summary>
    public void EnterImmersiveMode()
    {
        var window = _activity.Window;
        if (window == null)
            return;

        if (OperatingSystem.IsAndroidVersionAtLeast(28))
            window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            // From Android 15 apps are always drawn edge to edge, so this is only needed before then.
            if (!OperatingSystem.IsAndroidVersionAtLeast(35))
                window.SetDecorFitsSystemWindows(false);

            var controller = window.InsetsController;
            if (controller != null)
            {
                controller.Hide(WindowInsets.Type.SystemBars());
                controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
            }
        }
        else
        {
#pragma warning disable CS0618, CA1422 // The older API is the only option before Android 11.
            window.DecorView.SystemUiFlags =
                SystemUiFlags.ImmersiveSticky | SystemUiFlags.Fullscreen | SystemUiFlags.HideNavigation |
                SystemUiFlags.LayoutFullscreen | SystemUiFlags.LayoutHideNavigation | SystemUiFlags.LayoutStable;
#pragma warning restore CS0618, CA1422
        }
    }
}
