using Android.App;
using Android.Content;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Nearby;
using Android.Gms.Nearby.Connection;
using Android.Widget;
using Android.OS;
using System.Collections.Generic;

namespace NearbySample
{
    public class DiscoverItem
    {
        public string Endpoint { get; set; }
        public string Name { get; set; }

        public override string ToString() => Name;
    }

    [Activity(Label = "NearbySample", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity,
        GoogleApiClient.IConnectionCallbacks,
        GoogleApiClient.IOnConnectionFailedListener,

        IOnAdvertiseCallback, IOnDiscoveryCallback
    {
        private GoogleApiClient googleApi;
        private TextView log;
        private ListView discoverList;
        private Button advertise;
        private Switch hostMode;
        private EditText name;
        private Button discover;
        private ArrayAdapter<DiscoverItem> discoverListAdapter;

        private bool IsAdvertising = false;
        private bool IsDiscovering = false;

        private void PrepareUI()
        {
            this.discover = FindViewById<Button>(Resource.Id.btnDiscover);
            this.advertise = FindViewById<Button>(Resource.Id.btnAdvertise);
            this.hostMode = FindViewById<Switch>(Resource.Id.hostMode);
            this.name = FindViewById<EditText>(Resource.Id.deviceName);
            name.Text = TryGetDeviceId();

            this.discoverList = FindViewById<ListView>(Resource.Id.discoverList);
            this.discoverListAdapter = new ArrayAdapter<DiscoverItem>(this, Android.Resource.Layout.SimpleListItem1, new List<DiscoverItem>());
            this.discoverList.Adapter = discoverListAdapter;
        }

        private void UpdateState()
        {
            this.discover.Enabled = this.googleApi.IsConnected && this.hostMode.Checked == false;
            this.discover.Visibility = this.hostMode.Checked ? Android.Views.ViewStates.Gone : Android.Views.ViewStates.Visible;
            this.discover.Text = IsDiscovering ? "Stop Discovering" : "Start Discovering";
                        
            this.advertise.Enabled = this.googleApi.IsConnected && this.hostMode.Checked == true;
            this.advertise.Visibility = !this.hostMode.Checked ? Android.Views.ViewStates.Gone : Android.Views.ViewStates.Visible;
            this.advertise.Text = IsAdvertising ? "Stop Advertising" : "Start Advertising";
        }

        private void HandleClicks()
        {
            this.hostMode.CheckedChange += delegate
            {
                this.UpdateState();
            };

            this.discoverList.ItemClick += async (object sender, AdapterView.ItemClickEventArgs e) =>
            {
                var item = this.discoverListAdapter.GetItem(e.Position);

                await NearbyClass.Connections.RequestConnectionAsync(this.googleApi,
                    this.name.Text, item.Endpoint, new OnAdvertiseCallback(this));
            };

            advertise.Click += delegate
            {
                if (IsAdvertising)
                {
                    NearbyClass.Connections.StopAdvertising(this.googleApi);
                    IsAdvertising = false;
                }
                else
                {
                    NearbyClass.Connections.StartAdvertising(googleApi, name.Text, PackageName,
                        new OnAdvertiseCallback(this),
                        new AdvertisingOptions(Strategy.P2pStar));
                    this.IsAdvertising = true;
                }
                this.UpdateState();
            };

            discover.Click += (sender, args) =>
            {
                if (IsDiscovering)
                {
                    NearbyClass.Connections.StopDiscovery(googleApi);
                    IsDiscovering = false;
                }
                else
                {
                    NearbyClass.Connections.StartDiscovery(googleApi, PackageName, new OnDiscoveryCallback(this),
                        new DiscoveryOptions(Strategy.P2pStar));

                    this.IsDiscovering = true;
                }

                this.UpdateState();
            };
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.googleApi = new GoogleApiClient.Builder(this)
                .AddConnectionCallbacks(this)
                .AddOnConnectionFailedListener(this)
                .AddApi(NearbyClass.CONNECTIONS_API)
                .Build();

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.main);

            this.PrepareUI();
            this.HandleClicks();
            this.UpdateState();
        }

        protected override void OnStart()
        {
            base.OnStart();
            Log("onStart");
            googleApi.Connect();
        }

        private const string TAG = "NEARBY";

        private void Log(string msg)
        {
            Android.Util.Log.Debug(TAG, msg);
            log?.Append("\n" + msg);
        }

        protected override void OnStop()
        {
            base.OnStop();
            Log("onStop");

            // Disconnect the Google API client and stop any ongoing discovery or advertising. When the
            // GoogleAPIClient is disconnected, any connected peers will get an onDisconnected callback.
            googleApi?.Disconnect();
        }

        private static string TryGetDeviceId()
        {
            try
            {
                return Android.Provider.Settings.Secure.GetString(Application.Context.ContentResolver,
                    Android.Provider.Settings.Secure.AndroidId);
            }
            catch
            {
                return string.Empty;
            }
        }

        void IOnAdvertiseCallback.OnConnectionInitiated(string endpointId, ConnectionInfo connectionInfo)
        {
            if (connectionInfo.IsIncomingConnection)
            {
                NearbyClass.Connections.AcceptConnection(googleApi, endpointId, null);
            } else
            {

            }
            Log("OnConnectionInitiated: " + endpointId);
        }

        void IOnAdvertiseCallback.OnConnectionResult(string endpointId, ConnectionResolution resolution)
        {
            Log("OnConnectionResult: " + endpointId);
            
        }

        void IOnAdvertiseCallback.OnDisconnected(string endpointId)
        {
            Log("OnDisconnected: " + endpointId);
        }

        public void OnEndpointFound(string endpointId, DiscoveredEndpointInfo info)
        {
            Log("OnEndpointFound: " + endpointId + ". " + info.EndpointName);

            this.discoverListAdapter.Add(new DiscoverItem
            {
                Endpoint = endpointId,
                Name = info.EndpointName
            });
        }

        public void OnEndpointLost(string endpointId)
        {
            Log("OnEndpointLost:" + endpointId);
        }

        public void OnConnected(Bundle connectionHint)
        {
            Log("On api connected");
            this.discover.Enabled = true;
            this.advertise.Enabled = true;
        }

        public void OnConnectionSuspended(int cause)
        {
            Log("On api suspended");
            this.discover.Enabled = false;
            this.advertise.Enabled = false;
        }

        public void OnConnectionFailed(ConnectionResult result)
        {
            Log("On api connection failed. " + result.ErrorMessage);
        }
    }

    public class OnDiscoveryCallback : EndpointDiscoveryCallback
    {
        private readonly IOnDiscoveryCallback _callback;

        public OnDiscoveryCallback(IOnDiscoveryCallback callback)
        {
            _callback = callback;
        }

        public override void OnEndpointFound(string endpointId, DiscoveredEndpointInfo info)
        {
            _callback.OnEndpointFound(endpointId, info);
        }

        public override void OnEndpointLost(string endpointId)
        {
            _callback.OnEndpointLost(endpointId);
        }
    }

    public class OnAdvertiseCallback : ConnectionLifecycleCallback
    {
        private readonly IOnAdvertiseCallback _callback;

        public OnAdvertiseCallback(IOnAdvertiseCallback callback)
        {
            _callback = callback;
        }

        public override void OnConnectionInitiated(string endpointId, ConnectionInfo connectionInfo)
        {
            _callback.OnConnectionInitiated(endpointId, connectionInfo);
        }

        public override void OnConnectionResult(string endpointId, ConnectionResolution resolution)
        {
            _callback.OnConnectionResult(endpointId, resolution);
        }

        public override void OnDisconnected(string endpointId)
        {
            _callback.OnDisconnected(endpointId);
        }
    }
}

