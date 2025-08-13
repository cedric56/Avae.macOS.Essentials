namespace Microsoft.Maui.ApplicationModel
{
	partial class AppActionsImplementation : IAppActions
	{
		public bool IsSupported =>
			false;

		public Task<IEnumerable<AppAction>> GetAsync() =>
			throw ExceptionUtils.NotSupportedOrImplementedException;

		public Task SetAsync(IEnumerable<AppAction> actions) =>
			throw ExceptionUtils.NotSupportedOrImplementedException;

#pragma warning disable CS0067 // The event is never used
		public event EventHandler<AppActionEventArgs> AppActionActivated;
#pragma warning restore CS0067 // The event is never used
	}
}
