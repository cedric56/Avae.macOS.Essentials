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

            var proxy = new ProxyAppDelegate(actions);
            proxy.AppActionActivated += (sender, e) =>
            {
                AppActionActivated?.Invoke(this, e);
            };
            NSApplication.SharedApplication.Delegate = proxy;
            
			return Task.CompletedTask;
		}

		public event EventHandler<AppActionEventArgs> AppActionActivated;

        public class ProxyAppDelegate : NSApplicationDelegate
        {
            private readonly IEnumerable<AppAction> _actions;

            public ProxyAppDelegate(IEnumerable<AppAction> actions)
            {
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

            public event EventHandler<AppActionEventArgs> AppActionActivated;
        }
    }
}
