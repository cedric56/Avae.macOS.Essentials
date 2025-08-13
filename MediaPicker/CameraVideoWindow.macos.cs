using AVFoundation;
using AVKit;
using CoreMedia;
using Microsoft.Maui.Storage;

namespace Microsoft.Maui.Essentials.MediaPicker
{
    public class CameraVideoWindow
    {
        TaskCompletionSource<FileResult>? tcs;
        CameraVideoView? videoView; // This is your AVCaptureMovieFileOutput wrapper
        string? recordedFilePath;

        NSButton? startButton;
        NSButton? stopButton;

        public Task<FileResult> ShowAsync()
        {
            tcs = new TaskCompletionSource<FileResult>();

            videoView = new CameraVideoView();
            videoView.View.Frame = new CoreGraphics.CGRect(0, 60, 600, 400);

            var window = new NSWindow(
                new CoreGraphics.CGRect(0, 0, 600, 400),
                NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Titled,
                NSBackingStore.Buffered,
                false)
            {
                Title = "Camera",
                Level = NSWindowLevel.PopUpMenu,
                StyleMask = NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Resizable
            };

            var parentWindow = NSApplication.SharedApplication.MainWindow;
            if (parentWindow != null)
                parentWindow.AddChildWindow(window, NSWindowOrderingMode.Above);

            window.Level = NSWindowLevel.Floating;
            window.Center();

            window.WillClose += (sender, e) =>
            {
                if (!tcs.Task.IsCompleted)
                    tcs.SetResult(null!);
            };

            var container = new NSView(new CoreGraphics.CGRect(0, 0, 800, 600));
            container.AddSubview(videoView.View);

            startButton = new NSButton { Title = "Start", Frame = new CoreGraphics.CGRect(20, 20, 100, 30) };
            stopButton = new NSButton { Title = "Stop", Frame = new CoreGraphics.CGRect(140, 20, 100, 30) };

            startButton.Activated += async (s, e) =>
            {
                recordedFilePath = await videoView.StartRecordingAsync();
                startButton.Hidden = true;
                stopButton.Hidden = false;
            };

            stopButton.Activated += async (s, e) =>
            {
                if (await videoView.StopRecording())
                {
                    startButton.Hidden = true;
                    stopButton.Hidden = true;
                    ShowPlayback(container, recordedFilePath!);
                }
            };
            stopButton.Hidden = true; // Initially hidden
            startButton.TranslatesAutoresizingMaskIntoConstraints = false;
            stopButton.TranslatesAutoresizingMaskIntoConstraints = false;

            container.AddSubview(startButton);
            container.AddSubview(stopButton);

            var centerXStart = NSLayoutConstraint.Create(
                startButton, NSLayoutAttribute.CenterX,
                NSLayoutRelation.Equal,
                container, NSLayoutAttribute.CenterX,
                1f, 0f);

            var bottomStart = NSLayoutConstraint.Create(
                startButton, NSLayoutAttribute.Bottom,
                NSLayoutRelation.Equal,
                container, NSLayoutAttribute.Bottom,
                1f, -20f);

            var centerXStop = NSLayoutConstraint.Create(
                stopButton, NSLayoutAttribute.CenterX,
                NSLayoutRelation.Equal,
                container, NSLayoutAttribute.CenterX,
                1f, 0f);

            var bottomStop = NSLayoutConstraint.Create(
                stopButton, NSLayoutAttribute.Bottom,
                NSLayoutRelation.Equal,
                container, NSLayoutAttribute.Bottom,
                1f, -20f);

            container.AddConstraints(new[] { centerXStart, bottomStart, centerXStop, bottomStop });

            window.ContentView = container;
            window.MakeKeyAndOrderFront(null);

            return tcs.Task;
        }

        void ShowPlayback(NSView container, string videoPath)
        {
            videoView!.View.RemoveFromSuperview();
            videoView.Dispose();

            var player = new AVPlayer(NSUrl.FromFilename(videoPath));
            var playerView = new AVPlayerView(new CoreGraphics.CGRect(0, 100, 800, 460))
            {
                Player = player,
                ControlsStyle = AVPlayerViewControlsStyle.None
            };
            container.AddSubview(playerView);

            var playButton = new NSButton { Title = "Play", Frame = new CoreGraphics.CGRect(20, 60, 100, 30) };
            playButton.Activated += (s, e) =>
            {
                if (player.Rate == 0)
                {
                    player.Play();
                    playButton.Title = "Pause";
                }
                else
                {
                    player.Pause();
                    playButton.Title = "Play";
                }
            };
            container.AddSubview(playButton);

            var slider = new NSSlider(new CoreGraphics.CGRect(140, 65, 500, 20))
            {
                MinValue = 0,
                MaxValue = player.CurrentItem!.Asset.Duration.Seconds,
                DoubleValue = 0
            };
            slider.Activated += (s, e) =>
            {
                var newTime = CMTime.FromSeconds(slider.DoubleValue, 600);
                player.Seek(newTime);
            };
            container.AddSubview(slider);

            player.AddPeriodicTimeObserver(CMTime.FromSeconds(0.5, 600), null, time =>
            {
                slider.DoubleValue = time.Seconds;
            });

            var acceptButton = new NSButton { Title = "Accept", Frame = new CoreGraphics.CGRect(660, 20, 100, 30) };
            acceptButton.Activated += (s, e) =>
            {
                tcs!.TrySetResult(new FileResult(videoPath));
                container.Window?.Close();
            };
            container.AddSubview(acceptButton);

            var cancelButton = new NSButton { Title = "Cancel", Frame = new CoreGraphics.CGRect(540, 20, 100, 30) };
            cancelButton.Activated += (s, e) =>
            {
                File.Delete(videoPath);
                // Return to live camera with Start button only
                container.Subviews.ToList().ForEach(v => v.RemoveFromSuperview());
                videoView = new CameraVideoView();
                videoView.View.Frame = new CoreGraphics.CGRect(0, 60, 800, 520);
                container.AddSubview(videoView.View);
                container.AddSubview(startButton!);
                container.AddSubview(stopButton!);
                startButton!.Hidden = false;
                stopButton!.Hidden = true;
            };
            container.AddSubview(cancelButton);
        }
    }
}
