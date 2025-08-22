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

			var dock = NSApplication.SharedApplication.ApplicationDockMenu;
			if (dock is not null)
			{
				var menu = dock.Invoke(NSApplication.SharedApplication);

				foreach (var action in actions)
				{
					var item = new NSMenuItem(action.Title, (sender, e) =>
					{
						AppActionActivated?.Invoke(this, new AppActionEventArgs(action));
					})
					{
						Subtitle = action.Subtitle,
					};
					if (action.Icon is not null)
					{
						item.Image = new NSImage(action.Icon);
					}
					menu.AddItem(item);
				}
			}
			return Task.CompletedTask;
		}

		public event EventHandler<AppActionEventArgs> AppActionActivated;
	}
}
