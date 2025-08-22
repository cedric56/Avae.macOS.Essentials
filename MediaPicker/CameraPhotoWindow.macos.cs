using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace Microsoft.Maui.Essentials.MediaPicker
{
    public class CameraPhotoWindow
    {
        TaskCompletionSource<FileResult>? tcs;
        CameraPhotoView? cameraView;
        NSImage? capturedImage;
        NSButton? captureButton;
        NSView? container;
        NSWindow? window;

        public Task<FileResult> ShowAsync()
        {
            tcs = new TaskCompletionSource<FileResult>();

            // Window setup
            window = new NSWindow(
                new CoreGraphics.CGRect(0, 0, 600, 400),
                NSWindowStyle.Closable | NSWindowStyle.Titled | NSWindowStyle.Resizable,
                NSBackingStore.Buffered,
                false)
            {
                Level = NSWindowLevel.Floating,
                Title = "Camera",
                StyleMask = NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Resizable
            };

            var parentWindow = WindowStateManager.Default.GetNSWindow();
            if (parentWindow != null)
                parentWindow.AddChildWindow(window, NSWindowOrderingMode.Above);

            window.Center();

            window.WillClose += (sender, e) =>
            {
                if (!tcs.Task.IsCompleted)
                    tcs.TrySetResult(null!);
            };

            // Container
            container = new NSView(new CoreGraphics.CGRect(0, 0, 600, 400));

            // Camera View
            cameraView = new CameraPhotoView();
            cameraView.View.Frame = new CoreGraphics.CGRect(0, 50, 600, 350);
            container.AddSubview(cameraView.View);

            // Capture Button
            captureButton = new NSButton
            {
                Title = "Capture",
                Frame = new CoreGraphics.CGRect(250, 10, 100, 30)
            };
            captureButton.Activated += async (s, e) =>
            {
                captureButton.Hidden = true; // hide after click
                capturedImage = await cameraView.TakeSnapshot();
                if (capturedImage is not null)
                    ShowPreview(capturedImage);
            };

            container.AddSubview(captureButton);
            window.ContentView = container;
            window.MakeKeyAndOrderFront(null);

            return tcs.Task;
        }

        public void ShowPreview(NSImage image)
        {
            // Remove camera
            cameraView!.View.RemoveFromSuperview();
            cameraView.Dispose();

            // Show captured image
            var imageView = new NSImageView(new CoreGraphics.CGRect(0, 50, 600, 350))
            {
                Image = image,
                ImageScaling = NSImageScale.AxesIndependently
            };
            container!.AddSubview(imageView);

            // Accept button
            var acceptButton = new NSButton
            {
                Title = "Accept",
                Frame = new CoreGraphics.CGRect(150, 10, 100, 30)
            };
            acceptButton.Activated += (s, e) =>
            {
                var path = SaveImageToTempFile(image);
                tcs!.TrySetResult(new FileResult(path));
                CloseWindow();
            };

            // Cancel button — return to live camera
            var cancelButton = new NSButton
            {
                Title = "Cancel",
                Frame = new CoreGraphics.CGRect(350, 10, 100, 30)
            };
            cancelButton.Activated += (s, e) =>
            {
                imageView.RemoveFromSuperview();
                acceptButton.RemoveFromSuperview();
                cancelButton.RemoveFromSuperview();
                ShowCamera();
            };

            container.AddSubview(acceptButton);
            container.AddSubview(cancelButton);
        }

        void ShowCamera()
        {
            // Restore live camera view
            cameraView = new CameraPhotoView();
            cameraView.View.Frame = new CoreGraphics.CGRect(0, 50, 600, 350);
            container!.AddSubview(cameraView.View);

            captureButton!.Hidden = false; // show capture again
            container.AddSubview(captureButton);
        }

        void CloseWindow()
        {
            window?.Close();
        }

        string SaveImageToTempFile(NSImage image)
        {
            var tmpPath = Path.Combine(Path.GetTempPath(), $"photo_{Guid.NewGuid()}.jpg");
            using var data = image.AsTiff();
            var rep = new NSBitmapImageRep(data!);
            var jpegData = rep.RepresentationUsingTypeProperties(NSBitmapImageFileType.Jpeg);
            jpegData.Save(NSUrl.CreateFileUrl(tmpPath), true);
            return tmpPath;
        }
    }
}
