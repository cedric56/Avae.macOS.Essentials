using Avalonia;
using Avalonia.Controls;

namespace Microsoft.Maui.ApplicationModel
{
    /// <summary>
    /// A static class that contains platform-specific helper methods.
    /// </summary>
    public static class Platform
    {
        public static AppBuilder UseMauiEssentials(this AppBuilder builder)
        {
            NSApplication.CheckForIllegalCrossThreadCalls = false;

            Window.GotFocusEvent.AddClassHandler(typeof(Window), (sender, args) =>
            {
                var window = (Window)sender!;
                OnActivated(window);
            });
            Window.WindowOpenedEvent.AddClassHandler(typeof(Window), (sender, args) =>
            {
                var window = (Window)sender!;
                if (!_windows.Contains(window))
                {
                    _windows.Add(window);
                    OnActivated(window);
                }
            });
            Window.WindowClosedEvent.AddClassHandler(typeof(Window), (sender, _) =>
            {
                var window = (Window)sender!;
                _windows.Remove(window);
                if (_windows.Count > 0)
                    OnActivated(_windows.Last());
            });

            return builder;
        }

        static List<Window> _windows = new List<Window>();

        /// <inheritdoc cref="IWindowStateManager.OnActivated(UI.Xaml.Window, UI.Xaml.WindowActivatedEventArgs)"/>
        public static void OnActivated(Window window) =>
            WindowStateManager.Default.OnActivated(window);
    }
}
