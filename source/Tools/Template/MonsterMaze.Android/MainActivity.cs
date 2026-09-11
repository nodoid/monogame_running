using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
#if CH04
using Android.Window;
#endif
using Microsoft.Xna.Framework;

namespace MonsterMaze.Android;

/// <summary>
/// The Android entry point. It creates the game, hands its rendering view to Android
/// and starts the game loop.
/// </summary>
[Activity(
    MainLauncher = true,
    Theme = "@style/Theme.Splash",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.SensorPortrait,
    // We rotate the screen ourselves, so Android must not restart the activity when it happens.
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout |
                           ConfigChanges.SmallestScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
public class MainActivity : AndroidGameActivity
{
    private MonsterMazeGame _game;
#if CH03
    private AndroidPlatformServices _platform;
#endif

    protected override void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);

#if CH03
        _platform = new AndroidPlatformServices(this);
        _game = new MonsterMazeGame(_platform);
#else
        _game = new MonsterMazeGame();
#endif
        var view = (View)_game.Services.GetService(typeof(View));
        SetContentView(view);
#if CH03

        // The window's decor view only exists once there is content, so hide the bars now.
        _platform.EnterImmersiveMode();
#endif
#if CH04

        // From Android 13 the back button and back gesture arrive through a callback. Handling it
        // stops Android closing the game, so the game can use back to pause or go up a menu.
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(0, new BackCallback(_platform));
#endif
        _game.Run();
    }
#if CH03

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);

        // Android shows the system bars again after dialogs and app switches, so hide them again.
        if (hasFocus)
            _platform?.EnterImmersiveMode();
    }
#endif
#if CH04

    /// <summary>Back on Android 12 and earlier.</summary>
#pragma warning disable CS0618, CS0672, CA1422 // Only used before Android 13.
    public override void OnBackPressed()
    {
        _platform.NotifyBackPressed();
    }
#pragma warning restore CS0618, CS0672, CA1422

    private sealed class BackCallback : Java.Lang.Object, IOnBackInvokedCallback
    {
        private readonly AndroidPlatformServices _platform;

        public BackCallback(AndroidPlatformServices platform)
        {
            _platform = platform;
        }

        public void OnBackInvoked() => _platform.NotifyBackPressed();
    }
#endif
}
