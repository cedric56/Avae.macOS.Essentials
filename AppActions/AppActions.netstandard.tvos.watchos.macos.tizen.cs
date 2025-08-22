namespace Microsoft.Maui.ApplicationModel
{
	partial class AppActionsImplementation : IAppActions
	{
		public bool IsSupported =>
			true;

		private IEnumerable<AppAction>? _actions;

		public Task<IEnumerable<AppAction>> GetAsync() =>
			Task.FromResult(_actions ?? []);

		public Task SetAsync(IEnumerable<AppAction> actions)
		{
			_actions = actions;

            var oldDelegate = NSApplication.SharedApplication.Delegate;
            NSApplication.SharedApplication.Delegate = new ProxyAppDelegate(oldDelegate, actions);

            
			return Task.CompletedTask;
		}

		public event EventHandler<AppActionEventArgs> AppActionActivated;

        public class ProxyAppDelegate : NSApplicationDelegate
        {
            private readonly INSApplicationDelegate _inner;
            private readonly IEnumerable<AppAction> _actions;

            public ProxyAppDelegate(INSApplicationDelegate inner, IEnumerable<AppAction> actions)
            {
                _inner = inner;
                _actions = actions;
            }

            public override NSMenu ApplicationDockMenu(NSApplication sender)
            {
                var menu = new NSMenu();
                foreach (var action in _actions)
                {
                    var item = new NSMenuItem(action.Title, (s, e) =>
                    {
                        AppActionActivated?.Invoke(this, new AppActionEventArgs(action));
                    })
                    {
                        Subtitle = action.Subtitle,
                    };
                    if (action.Icon is not null)
                    {
                        item.Image = new NSImage(action.Icon);
                    };
                    menu.AddItem(item);
                }
                return menu;
            }

            // Forward other delegate calls if Avalonia needs them
            public override void DidFinishLaunching(NSNotification notification) =>
                _inner?.DidFinishLaunching(notification);

            public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender) =>
                _inner?.ApplicationShouldTerminateAfterLastWindowClosed(sender) ?? true;

            public event EventHandler<AppActionEventArgs> AppActionActivated;
        }
    }
}
