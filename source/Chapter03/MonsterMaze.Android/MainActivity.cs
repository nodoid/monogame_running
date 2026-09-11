using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
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
    private AndroidPlatformServices _platform;

    protected override void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);

        _platform = new AndroidPlatformServices(this);
        _game = new MonsterMazeGame(_platform);
        var view = (View)_game.Services.GetService(typeof(View));
        SetContentView(view);

        // The window's decor view only exists once there is content, so hide the bars now.
        _platform.EnterImmersiveMode();
        _game.Run();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);

        // Android shows the system bars again after dialogs and app switches, so hide them again.
        if (hasFocus)
            _platform?.EnterImmersiveMode();
    }
}
