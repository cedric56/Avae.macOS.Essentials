using AuthenticationServices;
using Microsoft.Maui.ApplicationModel;
using System.Net;
using System.Text;

namespace Microsoft.Maui.Authentication
{
	partial class WebAuthenticatorImplementation : IWebAuthenticator, IPlatformWebAuthenticatorCallback
	{
		const int asWebAuthenticationSessionErrorCodeCanceledLogin = 1;
		const string asWebAuthenticationSessionErrorDomain = "com.apple.AuthenticationServices.WebAuthenticationSession";

		readonly CallBackHelper callbackHelper = new CallBackHelper();

		TaskCompletionSource<WebAuthenticatorResult> tcsResponse;
		Uri redirectUri;

		ASWebAuthenticationSession was;

		public WebAuthenticatorImplementation()
		{
			callbackHelper.Register(this);
		}

		public async Task<WebAuthenticatorResult> AuthenticateAsync(WebAuthenticatorOptions webAuthenticatorOptions)
		{
			var url = webAuthenticatorOptions?.Url;
			var callbackUrl = webAuthenticatorOptions?.CallbackUrl;

			if(callbackUrl.Scheme != "http" && callbackUrl.Scheme != "https")
			{
				if (!AppInfoImplementation.VerifyHasUrlScheme(callbackUrl.Scheme))
					throw new InvalidOperationException("You must register your URL Scheme handler in your app's Info.plist!");
			}

            // Cancel any previous task that's still pending
            if (tcsResponse?.Task != null && !tcsResponse.Task.IsCompleted)
				tcsResponse.TrySetCanceled();

			tcsResponse = new TaskCompletionSource<WebAuthenticatorResult>();
			redirectUri = callbackUrl;
			var scheme = redirectUri.Scheme;

			if (OperatingSystem.IsMacOSVersionAtLeast(10, 15))
			{
                if (callbackUrl.Scheme == "http" || callbackUrl.Scheme == "https")
                {
                    return await ProcessHttpScheme(webAuthenticatorOptions);
                }

                void AuthSessionCallback(NSUrl cbUrl, NSError error)
				{
					if (error == null)
						OpenUrlCallback(cbUrl);
					else if (error.Domain == asWebAuthenticationSessionErrorDomain && error.Code == asWebAuthenticationSessionErrorCodeCanceledLogin)
						tcsResponse.TrySetCanceled();
					else
						tcsResponse.TrySetException(new NSErrorException(error));

					was = null;
				}

				was = new ASWebAuthenticationSession(WebUtils.GetNativeUrl(url), scheme, AuthSessionCallback);

				using (was)
				{
					var ctx = new ContextProvider(PlatformUtils.GetCurrentWindow());
					was.PresentationContextProvider = ctx;
					was.PrefersEphemeralWebBrowserSession = webAuthenticatorOptions?.PrefersEphemeralWebBrowserSession ?? false;

					was.Start();
					return await tcsResponse.Task;
				}
			}
			if (callbackUrl.Scheme == "http" || callbackUrl.Scheme == "https")
			{
				return await ProcessHttpScheme(webAuthenticatorOptions);
			}
			else
			{
				var opened = NSWorkspace.SharedWorkspace.OpenUrl(url);
				if (!opened)
					tcsResponse.TrySetException(new Exception("Error opening Safari"));

				return await tcsResponse.Task;
			}
		}

        async static Task<WebAuthenticatorResult> ProcessHttpScheme(WebAuthenticatorOptions webAuthenticatorOptions)
        {
            using var listener = new HttpListener();

            listener.Prefixes.Add(webAuthenticatorOptions.CallbackUrl.OriginalString);
            listener.Start();

            await Launcher.OpenAsync(webAuthenticatorOptions.Url);

            var cancelToken = new CancellationTokenSource();
            var context = await listener.GetContextAsync().WaitAsync(TimeSpan.FromMinutes(1), cancelToken.Token);

            var response = context.Response;
            string responseString = "<html><head><style>h1{color:green;font-size:20px;}</style></head><body><h1>You can now close this window.</h1></body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            await responseOutput.WriteAsync(buffer, 0, buffer.Length);
            responseOutput.Close();
            listener.Stop();

            if (webAuthenticatorOptions.ResponseDecoder is not null)
            {
                var dictionary = webAuthenticatorOptions.ResponseDecoder.DecodeResponse(context.Request.Url);
                return new WebAuthenticatorResult(dictionary);
            }

            return new WebAuthenticatorResult(context.Request.Url);

        }

        public bool OpenUrlCallback(Uri uri)
		{
			// If we aren't waiting on a task, don't handle the url
			if (tcsResponse?.Task?.IsCompleted ?? true)
				return false;

			try
			{
				// If we can't handle the url, don't
				if (!WebUtils.CanHandleCallback(redirectUri, uri))
					return false;

				tcsResponse.TrySetResult(new WebAuthenticatorResult(uri));
				return true;
			}
			catch (Exception ex)
			{
				tcsResponse.TrySetException(ex);
				return false;
			}
		}

		class ContextProvider : NSObject, IASWebAuthenticationPresentationContextProviding
		{
			public ContextProvider(NSWindow window) =>
				Window = window;

			public NSWindow Window { get; }

			public NSWindow GetPresentationAnchor(ASWebAuthenticationSession session)
				=> Window;
		}

		class CallBackHelper : NSObject
		{
			WebAuthenticatorImplementation implementation;

			public void Register(WebAuthenticatorImplementation implementation)
			{
				this.implementation = implementation;

				NSAppleEventManager.SharedAppleEventManager.SetEventHandler(
					this,
					new ObjCRuntime.Selector("handleAppleEvent:withReplyEvent:"),
					AEEventClass.Internet,
					AEEventID.GetUrl);
			}

			[Export("handleAppleEvent:withReplyEvent:")]
			public void HandleAppleEvent(NSAppleEventDescriptor evt, NSAppleEventDescriptor replyEvt)
			{
				var url = evt.ParamDescriptorForKeyword(DirectObject).StringValue;
				var uri = new Uri(url);
				implementation.OpenUrlCallback(WebUtils.GetNativeUrl(uri));
			}

			static uint GetDescriptor(string s) =>
				(uint)(s[0] << 24 | s[1] << 16 | s[2] << 8 | s[3]);

			static uint DirectObject => GetDescriptor("----");
		}
	}
}
