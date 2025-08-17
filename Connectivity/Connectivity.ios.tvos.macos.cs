namespace Microsoft.Maui.Networking
{
	partial class ConnectivityImplementation : IConnectivity
	{
		static ReachabilityListener listener;

		void StartListeners()
		{
			listener = new ReachabilityListener();
			listener.ReachabilityChanged += OnConnectivityChanged;
		}

		void StopListeners()
		{
			if (listener == null)
				return;

			listener.ReachabilityChanged -= OnConnectivityChanged;
			listener.Dispose();
			listener = null!;
		}

		public NetworkAccess NetworkAccess
		{
			get
			{
				var restricted = false;
				var internetStatus = Reachability.InternetConnectionStatus();
				if ((internetStatus == NetworkStatus.ReachableViaCarrierDataNetwork && !restricted) || internetStatus == NetworkStatus.ReachableViaWiFiNetwork)
					return NetworkAccess.Internet;

				var remoteHostStatus = Reachability.RemoteHostStatus();
				if ((remoteHostStatus == NetworkStatus.ReachableViaCarrierDataNetwork && !restricted) || remoteHostStatus == NetworkStatus.ReachableViaWiFiNetwork)
					return NetworkAccess.Internet;

				return NetworkAccess.None;
			}
		}

		public IEnumerable<ConnectionProfile> ConnectionProfiles
		{
			get
			{
				var statuses = Reachability.GetActiveConnectionType();
				foreach (var status in statuses)
				{
					switch (status)
					{
						case NetworkStatus.ReachableViaCarrierDataNetwork:
							yield return ConnectionProfile.Cellular;
							break;
						case NetworkStatus.ReachableViaWiFiNetwork:
							yield return ConnectionProfile.WiFi;
							break;
						default:
							yield return ConnectionProfile.Unknown;
							break;
					}
				}
			}
		}
	}
}
