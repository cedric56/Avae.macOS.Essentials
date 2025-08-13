#nullable enable
namespace Microsoft.Maui.ApplicationModel
{
	partial class BrowserImplementation : IBrowser
	{
		static Task<bool> OpenAsync(Uri uri, BrowserLaunchOptions options) =>
			Task.FromResult(NSWorkspace.SharedWorkspace.OpenUrl(new NSUrl(uri.AbsoluteUri)));

        Task<bool> IBrowser.OpenAsync(Uri uri, BrowserLaunchOptions options)
        {
            return Task.FromResult(NSWorkspace.SharedWorkspace.OpenUrl(new NSUrl(uri.AbsoluteUri)));
        }
    }
}
