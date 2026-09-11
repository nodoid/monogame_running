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

    protected override void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);

        _game = new MonsterMazeGame();
        var view = (View)_game.Services.GetService(typeof(View));
        SetContentView(view);
        _game.Run();
    }
}
