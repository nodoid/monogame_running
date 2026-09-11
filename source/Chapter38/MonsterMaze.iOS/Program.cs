using Foundation;
using UIKit;
using AVFoundation;

namespace MonsterMaze.iOS;

/// <summary>
/// The iOS entry point. iOS calls <see cref="FinishedLaunching"/> once the app is ready,
/// and we create and run the game from there.
/// </summary>
[Register("AppDelegate")]
internal class Program : UIApplicationDelegate
{
    private static MonsterMazeGame _game;

    public override void FinishedLaunching(UIApplication app)
    {
        // "Ambient" audio respects the silent switch and mixes with the player's own music,
        // which is what people expect from a game.
        AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Ambient);
        var platform = new IosPlatformServices();
        _game = new MonsterMazeGame(platform);
        platform.Attach(_game);
        _game.Run();
    }

    private static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(Program));
    }
}
