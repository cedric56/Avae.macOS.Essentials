using AVFoundation;

namespace Microsoft.Maui.Essentials.MediaPicker
{    
    public class CameraVideoView : NSViewController, IAVCaptureFileOutputRecordingDelegate
    {
        AVCaptureSession? session;
        AVCaptureDeviceInput? videoInput;
        AVCaptureDeviceInput? audioInput;
        AVCaptureMovieFileOutput? movieOutput;
        AVCaptureVideoPreviewLayer? previewLayer;

        TaskCompletionSource<string>? recordingTcs;

        public override async void ViewDidLoad()
        {
            base.ViewDidLoad();

            var granted = await EnsureCameraAccess();
            if (granted)
            {
                View.WantsLayer = true;

                // 1. Camera input
                var videoDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);
                if (videoDevice is not null)
                {
                    videoInput = AVCaptureDeviceInput.FromDevice(videoDevice, out NSError error);
                    if (error != null) throw new Exception(error.LocalizedDescription);

                    // 2. Audio input (optional, ignore if unavailable)
                    var audioDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Audio);
                    audioInput = AVCaptureDeviceInput.FromDevice(audioDevice!, out _);

                    // 3. Session
                    session = new AVCaptureSession
                    {
                        SessionPreset = AVCaptureSession.PresetHigh
                    };
                    session.AddInput(videoInput!);
                    if (audioInput != null)
                        session.AddInput(audioInput);

                    // 4. Preview
                    previewLayer = new AVCaptureVideoPreviewLayer(session)
                    {
                        Frame = View.Bounds,
                        VideoGravity = AVLayerVideoGravity.ResizeAspectFill
                    };
                    View.Layer = previewLayer;

                    // 5. Output
                    movieOutput = new AVCaptureMovieFileOutput();
                    session.AddOutput(movieOutput);

                    // 6. Start
                    session.StartRunning();
                }
            }
            else
            {
                var message = "Camera access not granted. Enable it in System Settings.";
                var label = new NSTextField
                {
                    StringValue = message,
                    Editable = false,
                    Bordered = false,
                    BackgroundColor = NSColor.Clear,
                    Alignment = NSTextAlignment.Center,
                    Font = NSFont.SystemFontOfSize(14)!
                };

                label.TranslatesAutoresizingMaskIntoConstraints = false;
                View.AddSubview(label);

                // Set fixed width & height
                View.AddConstraint(NSLayoutConstraint.Create(
                    label, NSLayoutAttribute.Width,
                    NSLayoutRelation.Equal,
                    null, NSLayoutAttribute.NoAttribute,
                    1, 400));

                View.AddConstraint(NSLayoutConstraint.Create(
                    label, NSLayoutAttribute.Height,
                    NSLayoutRelation.Equal,
                    null, NSLayoutAttribute.NoAttribute,
                    1, 24));

                // Center horizontally
                View.AddConstraint(NSLayoutConstraint.Create(
                    label, NSLayoutAttribute.CenterX,
                    NSLayoutRelation.Equal,
                    View, NSLayoutAttribute.CenterX,
                    1, 0));

                // Center vertically
                View.AddConstraint(NSLayoutConstraint.Create(
                    label, NSLayoutAttribute.CenterY,
                    NSLayoutRelation.Equal,
                    View, NSLayoutAttribute.CenterY,
                    1, 0));
            }
        }

        private Task<bool> EnsureCameraAccess()
        {
            var tcs = new TaskCompletionSource<bool>();
            AVCaptureDevice.RequestAccessForMediaType(AVAuthorizationMediaType.Video, granted =>
            {
                tcs.SetResult(granted);
            });
            return tcs.Task;
        }

        public Task<string> StartRecordingAsync()
        {
            if (movieOutput is null)
                return Task.FromResult<string>(null!);

            recordingTcs = new TaskCompletionSource<string>();

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mov");
            var fileUrl = NSUrl.FromFilename(tempPath);

            movieOutput.StartRecordingToOutputFile(fileUrl, new RecordingDelegate(recordingTcs));

            return recordingTcs.Task;
        }

        public Task<bool> StopRecording()
        {
            if (movieOutput is not null)
            {
                movieOutput.StopRecording();
                return Task.FromResult<bool>(true);
            }

            return Task.FromResult<bool>(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                session?.StopRunning();
                session?.Dispose();
                videoInput?.Dispose();
                audioInput?.Dispose();
                previewLayer?.Dispose();
                movieOutput?.Dispose();
            }
            base.Dispose(disposing);
        }

        class RecordingDelegate : AVCaptureFileOutputRecordingDelegate
        {
            readonly TaskCompletionSource<string>? startTcs;
            public event Action<bool>? RecordingStopped;
            string? filePath;

            public RecordingDelegate(TaskCompletionSource<string> startTcs)
            {
                this.startTcs = startTcs;
            }

            public override void DidStartRecording(AVCaptureFileOutput captureOutput, NSUrl outputFileUrl, NSObject[] connections)
            {
                filePath = outputFileUrl.Path;
                startTcs!.TrySetResult(filePath!);
            }

            public override void FinishedRecording(AVCaptureFileOutput captureOutput, NSUrl outputFileUrl, NSObject[] connections, NSError? error)
            {
                RecordingStopped?.Invoke(error == null);
            }
        }
    }
}
