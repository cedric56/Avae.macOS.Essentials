using AVFoundation;

namespace Microsoft.Maui.Essentials.MediaPicker
{
	public class CameraPhotoView : NSViewController, IAVCapturePhotoCaptureDelegate
	{
		AVCaptureSession? session;
		AVCaptureDeviceInput? input;
		AVCaptureVideoPreviewLayer? previewLayer;
		AVCapturePhotoOutput? photoOutput;

		TaskCompletionSource<NSImage>? snapshotTcs;

		public override async void ViewDidLoad()
		{
			base.ViewDidLoad();

			var granted = await EnsureCameraAccess();
			if (granted)
			{
				View.WantsLayer = true;

				// 1. Get default camera
				var device = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);
				if (device == null)
					throw new Exception("No camera found");

				input = AVCaptureDeviceInput.FromDevice(device, out NSError error);
				if (error != null)
					throw new Exception(error.LocalizedDescription);

				// 2. Create session
				session = new AVCaptureSession
				{
					SessionPreset = AVCaptureSession.PresetPhoto
				};
				session.AddInput(input!);

				// 3. Setup preview
				previewLayer = new AVCaptureVideoPreviewLayer(session)
				{
					Frame = View.Bounds,
					VideoGravity = AVLayerVideoGravity.ResizeAspectFill
				};
				View.Layer = previewLayer;

				// 4. Setup photo output
				photoOutput = new AVCapturePhotoOutput();
				session.AddOutput(photoOutput);

				// 5. Start camera
				session.StartRunning();
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

				// Calculate centered position (assuming fixed size for label)
				nfloat labelWidth = 400;
				nfloat labelHeight = 24;
				var viewBounds = View.Bounds;

				label.Frame = new CoreGraphics.CGRect(
					(viewBounds.Width - labelWidth) / 2,
					(viewBounds.Height - labelHeight) / 2,
					labelWidth,
					labelHeight
				);

				View.AddSubview(label);
			}
		}

		private Task<bool> EnsureCameraAccess()
		{
			var tcs= new TaskCompletionSource<bool>();
			AVCaptureDevice.RequestAccessForMediaType(AVAuthorizationMediaType.Video, granted =>
			{
				tcs.SetResult(granted);
			});
			return tcs.Task;
		}

		public Task<NSImage> TakeSnapshot()
		{
			if (photoOutput is null)
				return Task.FromResult<NSImage>(null!);

			snapshotTcs = new TaskCompletionSource<NSImage>();

			var settings = AVCapturePhotoSettings.Create();
			settings.FlashMode = AVCaptureFlashMode.Off;

			photoOutput.CapturePhoto(settings, this);

			return snapshotTcs.Task;
		}

		[Export("captureOutput:didFinishProcessingPhoto:error:")]
		public void DidFinishProcessingPhoto(AVCapturePhotoOutput captureOutput, AVCapturePhoto photo, NSError? error)
		{
			if (error != null)
			{
				snapshotTcs?.TrySetException(new Exception(error.LocalizedDescription));
				return;
			}

			var data = photo.FileDataRepresentation;
			if (data != null)
			{
				var nsImage = new NSImage(data);
				snapshotTcs?.TrySetResult(nsImage);
			}
			else
			{
				snapshotTcs?.TrySetException(new Exception("No image data captured"));
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				session?.StopRunning();
				session?.Dispose();
				input?.Dispose();
				previewLayer?.Dispose();
				photoOutput?.Dispose();
			}
			base.Dispose(disposing);
		}
	}

}
