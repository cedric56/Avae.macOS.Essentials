namespace Microsoft.Maui.ApplicationModel
{
	class AppInfoImplementation : IAppInfo
	{

		public AppPackagingModel PackagingModel => AppPackagingModel.Packaged;

		public string PackageName => GetBundleValue("CFBundleIdentifier");

		public string Name => GetBundleValue("CFBundleDisplayName") ?? GetBundleValue("CFBundleName");

		public Version Version => Utils.ParseVersion(VersionString);

		public string VersionString => GetBundleValue("CFBundleShortVersionString");

		public string BuildString => GetBundleValue("CFBundleVersion");

		string GetBundleValue(string key)
			=> NSBundle.MainBundle.ObjectForInfoDictionary(key)?.ToString();


		public void ShowSettingsUI()
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				var prefsApp = ScriptingBridge.SBApplication.GetApplication("com.apple.systempreferences");
				prefsApp.SendMode = ScriptingBridge.AESendMode.NoReply;
				prefsApp.Activate();
			});
		}

		public AppTheme RequestedTheme
		{
			get
			{
				if (OperatingSystem.IsMacOSVersionAtLeast(10, 14))
				{
					var app = NSAppearance.CurrentAppearance?.FindBestMatch(new string[]
					{
						NSAppearance.NameAqua,
						NSAppearance.NameDarkAqua
					});

					if (string.IsNullOrEmpty(app))
						return AppTheme.Unspecified;

					if (app == NSAppearance.NameDarkAqua)
						return AppTheme.Dark;
				}
				return AppTheme.Light;
			}
		}

		public LayoutDirection RequestedLayoutDirection
		{
			get
			{				
				return (IsDeviceUILayoutDirectionRightToLeft) ?
					LayoutDirection.RightToLeft : LayoutDirection.LeftToRight;
			}
		}

		public bool IsDeviceUILayoutDirectionRightToLeft => 
			NSApplication.SharedApplication.UserInterfaceLayoutDirection == NSUserInterfaceLayoutDirection.RightToLeft;

		internal static bool VerifyHasUrlScheme(string scheme)
		{
			var cleansed = scheme.Replace("://", string.Empty, StringComparison.Ordinal);
			var schemes = GetCFBundleURLSchemes().ToList();
			return schemes.Any(x => x != null && x.Equals(cleansed, StringComparison.OrdinalIgnoreCase));
		}

		internal static IEnumerable<string> GetCFBundleURLSchemes()
		{
			var schemes = new List<string>();

			NSObject nsobj = null;
			if (!NSBundle.MainBundle.InfoDictionary.TryGetValue((NSString)"CFBundleURLTypes", out nsobj))
				return schemes;

			var array = nsobj as NSArray;

			if (array == null)
				return schemes;

			for (nuint i = 0; i < array.Count; i++)
			{
				var d = array.GetItem<NSDictionary>(i);
				if (d == null || !d.Any())
					continue;

				if (!d.TryGetValue((NSString)"CFBundleURLSchemes", out nsobj))
					continue;

				var a = nsobj as NSArray;
				var urls = ConvertToIEnumerable<NSString>(a).Select(x => x.ToString()).ToArray();
				foreach (var url in urls)
					schemes.Add(url);
			}

			return schemes;
		}

		static IEnumerable<T> ConvertToIEnumerable<T>(NSArray array)
			where T : class, ObjCRuntime.INativeObject
		{
			for (nuint i = 0; i < array.Count; i++)
				yield return array.GetItem<T>(i);
		}
	}
}
