#nullable enable
using CoreLocation;
using Microsoft.Maui.ApplicationModel;
using System.Threading;

namespace Microsoft.Maui.Devices.Sensors
{
    partial class GeolocationImplementation : IGeolocation
    {
        CLLocationManager? listeningManager;

        /// <summary>
        /// Indicates if currently listening to location updates while the app is in foreground.
        /// </summary>
        public bool IsListeningForeground { get => listeningManager != null; }

        public Task<Location?> GetLastKnownLocationAsync()
        {
            if (!CLLocationManager.LocationServicesEnabled)
                throw new FeatureNotEnabledException("Location services are not enabled on device.");

            var manager = new CLLocationManager();
            var location = manager.Location;

            var reducedAccuracy = false;
            return Task.FromResult(location?.ToLocation(reducedAccuracy));
        }

        public async Task<Location?> GetLocationAsync(GeolocationRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (!CLLocationManager.LocationServicesEnabled)
                throw new FeatureNotEnabledException("Location services are not enabled on device.");

            // the location manager requires an active run loop
            // so just use the main loop
            var manager = MainThread.InvokeOnMainThread(() => new CLLocationManager());

            var tcs = new TaskCompletionSource<CLLocation?>(manager);

            var listener = new SingleLocationListener();
            listener.LocationHandler += HandleLocation;
            listener.ErrorHandler += HandleError;
            cancellationToken = Utils.TimeoutToken(cancellationToken, request.Timeout);
            cancellationToken.Register(Cancel);

            manager.DesiredAccuracy = request.PlatformDesiredAccuracy;
            manager.DistanceFilter = 0;
            manager.Delegate = listener;
            manager.RequestAlwaysAuthorization();

            var reducedAccuracy = false;

            var clLocation = await tcs.Task;

            return clLocation?.ToLocation(reducedAccuracy);

            void HandleLocation(CLLocation location)
            {
                manager.StopUpdatingLocation();
                tcs.TrySetResult(location);
            }

            void HandleError(GeolocationError error)
            {
                tcs.TrySetResult(null!);
                OnLocationError(error);
            }

            void Cancel()
            {
                manager.StopUpdatingLocation();
                tcs.TrySetResult(null);
            }
        }

        /// <summary>
        /// Starts listening to location updates using the <see cref="Geolocation.LocationChanged"/>
        /// event or the <see cref="Geolocation.ListeningFailed"/> event. Events may only sent when
        /// the app is in the foreground. Requests <see cref="Permissions.LocationWhenInUse"/>
        /// from the user.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
        /// <exception cref="FeatureNotSupportedException">Thrown if listening is not supported on this platform.</exception>
        /// <exception cref="InvalidOperationException">Thrown if already listening and <see cref="IsListeningForeground"/> returns <see langword="true"/>.</exception>
        /// <param name="request">The listening request parameters to use.</param>
        /// <returns><see langword="true"/> when listening was started, or <see langword="false"/> when listening couldn't be started.</returns>
        public Task<bool> StartListeningForegroundAsync(GeolocationListeningRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (IsListeningForeground)
                throw new InvalidOperationException("Already listening to location changes.");

            if (!CLLocationManager.LocationServicesEnabled)
                throw new FeatureNotEnabledException("Location services are not enabled on device.");

            // the location manager requires an active run loop
            // so just use the main loop
            listeningManager = MainThread.InvokeOnMainThread(() => new CLLocationManager());

            var reducedAccuracy = false;

            var listener = new ContinuousLocationListener();
            listener.LocationHandler += HandleLocation;
            listener.ErrorHandler += HandleError;

            listeningManager.DesiredAccuracy = request.PlatformDesiredAccuracy;
            listeningManager.Delegate = listener;
            listeningManager.RequestAlwaysAuthorization();


            return Task.FromResult(true);

            void HandleLocation(CLLocation clLocation)
            {
                OnLocationChanged(clLocation.ToLocation(reducedAccuracy));
            }

            void HandleError(GeolocationError error)
            {
                StopListeningForeground();
                OnLocationError(error);
            }
        }

        /// <summary>
        /// Stop listening for location updates when the app is in the foreground.
        /// Has no effect when not listening and <see cref="Geolocation.IsListeningForeground"/>
        /// is currently <see langword="false"/>.
        /// </summary>
        public void StopListeningForeground()
        {
            if (!IsListeningForeground ||
                listeningManager is null)
                return;

            listeningManager.StopUpdatingLocation();

            if (listeningManager.Delegate is ContinuousLocationListener listener)
            {
                listener.LocationHandler = null;
                listener.ErrorHandler = null;
            }

            listeningManager.WeakDelegate = null;

            listeningManager = null;
        }
    }

    class SingleLocationListener : CLLocationManagerDelegate
    {
        bool wasRaised = false;

        internal Action<CLLocation>? LocationHandler { get; set; }
        internal Action<GeolocationError>? ErrorHandler { get; set; }

        /// <inheritdoc/>
        public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        {
            if (wasRaised)
                return;

            wasRaised = true;

            var location = locations?.LastOrDefault();
            LocationHandler?.Invoke(location);
        }

        public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
        {
            Console.WriteLine($"Continuous AuthorizationChanged: {status}");
            switch (status)
            {
                case CLAuthorizationStatus.AuthorizedWhenInUse:
                case CLAuthorizationStatus.AuthorizedAlways:
                    Console.WriteLine("Starting continuous location updates after authorization.");
                    manager.RequestLocation();
                    break;
                case CLAuthorizationStatus.NotDetermined:
                    Console.WriteLine("Authorization still pending, requesting again.");
                    manager.RequestAlwaysAuthorization();
                    break;
                case CLAuthorizationStatus.Denied:
                case CLAuthorizationStatus.Restricted:
                    Console.WriteLine("Continuous location access denied or restricted.");
                    ErrorHandler?.Invoke(GeolocationError.Unauthorized);
                    break;
            }
        }

        public override void Failed(CLLocationManager manager, NSError error)
        {
            ErrorHandler?.Invoke(GeolocationError.PositionUnavailable);
        }

        /// <inheritdoc/>
        public override bool ShouldDisplayHeadingCalibration(CLLocationManager manager) => false;
    }

    class ContinuousLocationListener : CLLocationManagerDelegate
    {
        internal Action<CLLocation>? LocationHandler { get; set; }

        internal Action<GeolocationError>? ErrorHandler { get; set; }

        public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
        {
            Console.WriteLine($"Continuous AuthorizationChanged: {status}");
            switch (status)
            {
                case CLAuthorizationStatus.AuthorizedWhenInUse:
                case CLAuthorizationStatus.AuthorizedAlways:
                    Console.WriteLine("Starting continuous location updates after authorization.");
                    manager.StartUpdatingLocation();
                    break;
                case CLAuthorizationStatus.NotDetermined:
                    Console.WriteLine("Authorization still pending, requesting again.");
                    manager.RequestAlwaysAuthorization();
                    break;
                case CLAuthorizationStatus.Denied:
                case CLAuthorizationStatus.Restricted:
                    Console.WriteLine("Continuous location access denied or restricted.");
                    ErrorHandler?.Invoke(GeolocationError.Unauthorized);
                    break;
            }
        }

        /// <inheritdoc/>
        public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        {
            var location = locations?.LastOrDefault();
            LocationHandler?.Invoke(location);
        }

        /// <inheritdoc/>
        public override void Failed(CLLocationManager manager, NSError error)
        {
            if ((CLError)error.Code == CLError.Network)
                ErrorHandler?.Invoke(GeolocationError.PositionUnavailable);
        }

        /// <inheritdoc/>
        public override bool ShouldDisplayHeadingCalibration(CLLocationManager manager) => false;
    }
}