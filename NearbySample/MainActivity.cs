﻿using System;
using Android.App;
using Android.Content;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Nearby;
using Android.Gms.Nearby.Connection;
using Android.Widget;
using Android.OS;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Android;
using Android.Content.PM;
using Android.Gms.Tasks;
using Android.Views;
using NearbySample.Core;
using NearbySample.Models;
using Environment = Android.OS.Environment;
using Task = System.Threading.Tasks.Task;
using Uri = Android.Net.Uri;

namespace NearbySample
{
    public static class ArrayAdapterHelper
    {
        public static T Find<T>(this ArrayAdapter<T> adapter, Func<T, bool> predicat)
            where  T: class
        {
            for (int i = 0; i < adapter.Count; i++)
            {
                var item = adapter.GetItem(i);

                if (predicat(item))
                {
                    return item;
                }
            }

            return null;
        }
    }

    [Activity(Label = "NearbySample", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity,
        GoogleApiClient.IConnectionCallbacks,
        GoogleApiClient.IOnConnectionFailedListener,

        IConnectionLifeCycleCallback, IOnDiscoveryCallback, IPayloadCallback
    {
        private GoogleApiClient googleApi;
        private ListView discoverList;
        private Switch hostMode;
        private EditText name;
        private TextView logView;
        private ArrayAdapter<DiscoverItem> discoverListAdapter;

        private bool isAdvertising;
        private bool isDiscovering;
        private ListView connectedList;
        private ArrayAdapter<DiscoverItem> connectedListAdapter;
        private Button selectImage;

        private void PrepareUI()
        {
            this.hostMode = FindViewById<Switch>(Resource.Id.hostMode);
            this.name = FindViewById<EditText>(Resource.Id.deviceName);
            name.Text = TryGetDeviceId();

            this.discoverList = FindViewById<ListView>(Resource.Id.discoverList);
            this.discoverListAdapter = new ArrayAdapter<DiscoverItem>(this, Android.Resource.Layout.SimpleListItem1,
                new List<DiscoverItem>());
            this.discoverList.Adapter = discoverListAdapter;

            this.connectedList = FindViewById<ListView>(Resource.Id.connectedList);
            this.connectedListAdapter = new ArrayAdapter<DiscoverItem>(this, Android.Resource.Layout.SimpleListItem1,
                new List<DiscoverItem>());
            this.connectedList.Adapter = connectedListAdapter;

            this.selectImage = FindViewById<Button>(Resource.Id.btnImage);

            this.logView = FindViewById<TextView>(Resource.Id.logView);
            this.progress = FindViewById<ProgressBar>(Resource.Id.uploadProgress);
            this.progress.Visibility = ViewStates.Gone;
        }

        private async void UpdateState()
        {
            await this.apiConnected.Task;

            if (this.IsHostMode)
            {
                if (isDiscovering)
                {
                    Log("Stop discovery");
                    NearbyClass.Connections.StopDiscovery(googleApi);
                    isDiscovering = false;
                }

                if (!isAdvertising)
                {
                    Log("Start advertising...");
                    await NearbyClass.Connections.StartAdvertisingAsync(googleApi, name.Text, PackageName,
                        new OnConnectionLifecycleCallback(this),
                        new AdvertisingOptions(Strategy.P2pStar));
                    Log("Start advertising. Done");
                    this.isAdvertising = true;
                }

                selectImage.Visibility = ViewStates.Gone;
            }
            else
            {
                if (isAdvertising)
                {
                    Log("Stop advertising");
                    NearbyClass.Connections.StopAdvertising(googleApi);
                    isAdvertising = false;
                }

                if (!isDiscovering)
                {
                    Log("Start discovery...");

                    this.isDiscovering = true;
                    await NearbyClass.Connections.StartDiscoveryAsync(googleApi, PackageName,
                        new OnDiscoveryCallback(this),
                        new DiscoveryOptions(Strategy.P2pStar));

                    Log("Start discovery done");
                }

                selectImage.Visibility = ViewStates.Visible;
            }
        }

        private bool IsHostMode => this.hostMode.Checked;

        private void HandleClicks()
        {
            this.hostMode.CheckedChange += (sender, e) => this.UpdateState();

            this.discoverList.ItemClick += async (sender, e) =>
            {
                var item = this.discoverListAdapter.GetItem(e.Position);

                if (!IsHostMode)
                {
                    Log($"Request Connection to {item.Name}[{item.Endpoint}]");
                    await NearbyClass.Connections.RequestConnectionAsync(this.googleApi,
                        this.name.Text, item.Endpoint, new OnConnectionLifecycleCallback(this));
                }
            };

            this.connectedList.ItemClick += (sender, e) =>
            {
                var item = this.connectedListAdapter.GetItem(e.Position);
                NearbyClass.Connections.DisconnectFromEndpoint(this.googleApi, item.Endpoint);

                if (!IsHostMode)
                {
                    this.discoverListAdapter.Add(item);
                }

                this.connectedListAdapter.Remove(item);
            };

            this.selectImage.Click += async (sender, args) =>
            {
                if (CheckSelfPermission(Manifest.Permission.ReadExternalStorage) == Permission.Granted)
                {
                    var intent = new Intent(Intent.ActionGetContent);
                    intent.SetType("image/*");
                    StartActivityForResult(intent, ChooseFileResultCode);
                }
            };
        }


        private void RestoreGoogleApiConnectionIfNeeded()
        {
            if (!googleApi.IsConnected)
            {
                this.apiConnected = new TaskCompletionSource<bool>();
                this.googleApi.Connect();
            }
        }

        protected override async void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            RestoreGoogleApiConnectionIfNeeded();
            
            if (resultCode == Result.Ok)
            {
                if (requestCode == ChooseFileResultCode)
                {
                    var payload = CreatePayload(data.Data);

                    if (payload == null) return;
                    var connected = await this.apiConnected.Task;

                    if (connected)
                    {
                        for (int i = 0; i < this.connectedListAdapter.Count; i++)
                        {
                            var item = this.connectedListAdapter.GetItem(i);
                            {
                                Log($"Sending payload to {item.Name} [{item.Endpoint}]");
                                this.selectImage.Enabled = false;
                                await NearbyClass.Connections.SendPayload(this.googleApi, item.Endpoint, payload);
                                Log($"Done sending payload to {item.Name} [{item.Endpoint}]");
                            }
                        }
                    }
                }
            }
        }

        private long max = 0;

        private Payload CreatePayload(Uri uri)
        {
            try
            {
                using (var fd = ContentResolver.OpenFileDescriptor(uri, "r"))
                    this.max = fd.StatSize;

                var inputStream = ContentResolver.OpenInputStream(uri);
              
                return Payload.FromStream(inputStream);
            }
            catch (System.Exception e)
            {
                Log("Cannot read file: " + e.Message);
                return null;
            }
        }

        public const int ChooseFileResultCode = 1000;

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

            this.logView.Append("Debug logs:\n\n");

            this.UpdateState();
        }

        protected override void OnStart()
        {
            Log("onStart");
            if (!googleApi.IsConnected && !googleApi.IsConnecting)
            {
                RestoreGoogleApiConnectionIfNeeded();
            }

            base.OnStart();
        }

        private const string Tag = "NEARBY";

        private void Log(string msg)
        {
            Android.Util.Log.Debug(Tag, msg);
            this.logView.Append("\n" + msg);
        }

        protected override void OnStop()
        {
            //this.apiConnected = null;
            Log("onStop");

            // Disconnect the Google API client and stop any ongoing discovery or advertising. When the
            // GoogleAPIClient is disconnected, any connected peers will get an onDisconnected callback.
            //googleApi?.Disconnect();
            base.OnStop();
        }

        private static string TryGetDeviceId()
        {
            try
            {
                var model = Build.Model;
                var bluetoothName = Android.Provider.Settings.Secure.GetString(
                    Application.Context.ContentResolver, "bluetooth_name");

                return model.ToLower() != bluetoothName.ToLower()
                    ? $"{model} {bluetoothName}"
                    : bluetoothName;
            }
            catch
            {
                return string.Empty;
            }
        }

        void IConnectionLifeCycleCallback.OnConnectionInitiated(string endpointId, ConnectionInfo connectionInfo)
        {
            // At this moment we can ask user consent on connection. 
            // There is `connectionInfo.AuthenticationToken` that can be displayed to each user
            // to authenticate connection
            // in this sample we just accept connection by both sides without consent

            if (connectionInfo.IsIncomingConnection)
            {
                NearbyClass.Connections.AcceptConnection(googleApi, endpointId, new OnPayloadCallback(this));
                this.discoverListAdapter.Add(new DiscoverItem
                {
                    Endpoint = endpointId,
                    Name = connectionInfo.EndpointName
                });
            }
            else
            {
                NearbyClass.Connections.AcceptConnection(googleApi, endpointId, new OnPayloadCallback(this));
            }

            Log($"On Connection initiated {(connectionInfo.IsIncomingConnection ? "from" : "to")} {connectionInfo.EndpointName}[{endpointId}] Auth:{connectionInfo.AuthenticationToken}");
        }

        void IConnectionLifeCycleCallback.OnConnectionResult(string endpointId, ConnectionResolution resolution)
        {
            if (resolution.Status.IsSuccess)
            {
                var item = this.discoverListAdapter.Find(d => d.Endpoint == endpointId);
                if (item != null)
                {
                    Log($"Connected to {item.Name} [{item.Endpoint}]");

                    this.connectedListAdapter.Add(item);
                    this.discoverListAdapter.Remove(item);
                }
            }
            else
            {
                Log("OnConnectionResult: " + endpointId + " failed " + resolution.Status.StatusMessage);
            }
        }
        
        void IConnectionLifeCycleCallback.OnDisconnected(string endpointId)
        {
            var connectedItem = this.connectedListAdapter.Find(d => d.Endpoint == endpointId);
            if (connectedItem != null)
            {
                this.connectedListAdapter.Remove(connectedItem);

                if(IsHostMode == false)
                    this.discoverListAdapter.Add(connectedItem);
            }

            Log("OnDisconnected: " + endpointId);
        }

        public void OnEndpointFound(string endpointId, DiscoveredEndpointInfo info)
        {
            Log("OnEndpointFound: " + endpointId + ". " + info.EndpointName);

            var existing = this.discoverListAdapter.Find(d => d.Endpoint == endpointId);

            if (existing == null)
                this.discoverListAdapter.Add(new DiscoverItem
                {
                    Endpoint = endpointId,
                    Name = info.EndpointName
                });
        }

        void IOnDiscoveryCallback.OnEndpointLost(string endpointId)
        {
            Log("OnEndpointLost:" + endpointId);

            var item = this.discoverListAdapter.Find(d => d.Endpoint == endpointId);

            if (item != null)
                this.discoverListAdapter.Remove(item);

            var connected = this.connectedListAdapter.Find(d => d.Endpoint == endpointId);
            if (connected != null)
            {
                this.connectedListAdapter.Remove(connected);
            }
        }

        private TaskCompletionSource<bool> apiConnected = new TaskCompletionSource<bool>();
        private ProgressBar progress;

        void GoogleApiClient.IConnectionCallbacks.OnConnected(Bundle connectionHint)
        {
            Log("On api connected");
            apiConnected.SetResult(true);
            UpdateState();
        }

        void GoogleApiClient.IConnectionCallbacks.OnConnectionSuspended(int cause)
        {
            Log("On api suspended");
        }

        public void OnConnectionFailed(ConnectionResult result)
        {
            Log("On api connection failed. " + result.ErrorMessage);
        }

        void IPayloadCallback.OnPayloadReceived(string endpointId, Payload payload)
        {
            if (payload.PayloadType == Payload.Type.Bytes)
            {
                var bytes = payload.AsBytes();

                Log($"OnPayloadReceived: {endpointId} - {payload.Id} {payload.PayloadType} {bytes?.LongLength ?? -1}");
            }

            if (payload.PayloadType == Payload.Type.Stream)
            {
                Log($"OnPayloadReceived: {endpointId} - {payload.Id} Stream ");
                var stream = payload.AsStream();

                var path = $"{Environment.ExternalStorageDirectory}/{PackageName}/file.jpg";
                var f = new Java.IO.File(path);
                var dirs = new Java.IO.File(f.Parent);
                if (!dirs.Exists())
                    dirs.Mkdirs();

                f.CreateNewFile();
                Log("OnPayloadReceived: Reading stream, writing to " + path);
                using (var result = new FileStream(f.ToString(), FileMode.OpenOrCreate))
                {
                    stream.AsInputStream().CopyTo(result);
                }

                var intent = new Intent();
                intent.SetAction(Intent.ActionView);
                intent.SetDataAndType(Uri.Parse("file://" + path), "image/*");
                StartActivity(intent);
            }
        }

        void IPayloadCallback.OnPayloadTransferUpdate(string endpointId, PayloadTransferUpdate update)
        {
            switch (update.TransferStatus)
            {
                case PayloadTransferUpdate.Status.InProgress:
                    this.selectImage.Enabled = false;
                    this.progress.Visibility = ViewStates.Visible;
                    this.progress.Max = 10000;
  
                    this.progress.Progress = (int) ((double)update.BytesTransferred / (double)max * 10000);
                    break;
                case PayloadTransferUpdate.Status.Failure:
                    Log($"OnPayloadTransferUpdate: {endpointId} Failure - {update.TransferStatus} {update.BytesTransferred} of {update.TotalBytes}");
                    this.selectImage.Enabled = true;
                    this.progress.Visibility = ViewStates.Gone;
                    break;
                case PayloadTransferUpdate.Status.Success:
                    this.selectImage.Enabled = true;
                    this.progress.Visibility = ViewStates.Gone;
                    Log($"OnPayloadTransferUpdate: {endpointId} Succes - {update.TransferStatus} {update.BytesTransferred} of {update.TotalBytes}");
                     break;
            }
        }
    }
}

