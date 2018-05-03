using Android.Gms.Nearby.Connection;

namespace NearbySample.Core
{
    public class OnConnectionLifecycleCallback : ConnectionLifecycleCallback
    {
        private readonly IConnectionLifeCycleCallback callback;

        public OnConnectionLifecycleCallback(IConnectionLifeCycleCallback callback)
        {
            this.callback = callback;
        }

        public override void OnConnectionInitiated(string endpointId, ConnectionInfo connectionInfo)
        {
            callback.OnConnectionInitiated(endpointId, connectionInfo);
        }

        public override void OnConnectionResult(string endpointId, ConnectionResolution resolution)
        {
            callback.OnConnectionResult(endpointId, resolution);
        }

        public override void OnDisconnected(string endpointId)
        {
            callback.OnDisconnected(endpointId);
        }
    }
}