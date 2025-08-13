using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Essentials.MediaPicker;
using Microsoft.Maui.Storage;

namespace Microsoft.Maui.Media
{
	partial class MediaPickerImplementation : IMediaPicker
	{
        public bool IsCaptureSupported => true;

        public Task<FileResult> PickPhotoAsync(MediaPickerOptions? options = null)
        {
            return FilePicker.PickAsync(new PickOptions
            {
                 PickerTitle = options?.Title,
                FileTypes = FilePickerFileType.Images
            });
        }

        public Task<FileResult> CapturePhotoAsync(MediaPickerOptions? options = null)
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var photo = new CameraPhotoWindow();
                return await photo.ShowAsync();
            });
        }

        public Task<FileResult> PickVideoAsync(MediaPickerOptions? options = null)
        {
            return FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = options?.Title,
                FileTypes = FilePickerFileType.Videos
            });
        }

        public Task<FileResult> CaptureVideoAsync(MediaPickerOptions? options = null)
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var photo = new CameraVideoWindow();
                return await photo.ShowAsync();
            });
        }
    }
}
