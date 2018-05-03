using Android.Gms.Nearby.Connection;

namespace NearbySample.Core
{
    public class OnDiscoveryCallback : EndpointDiscoveryCallback
    {
        private readonly IOnDiscoveryCallback callback;

        public OnDiscoveryCallback(IOnDiscoveryCallback callback)
        {
            this.callback = callback;
        }

        public override void OnEndpointFound(string endpointId, DiscoveredEndpointInfo info)
        {
            callback.OnEndpointFound(endpointId, info);
        }

        public override void OnEndpointLost(string endpointId)
        {
            callback.OnEndpointLost(endpointId);
        }
    }
}