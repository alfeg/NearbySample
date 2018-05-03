using Android.Gms.Nearby.Connection;

namespace NearbySample.Core
{
    public interface IOnDiscoveryCallback
    {
        void OnEndpointFound(string endpointId, DiscoveredEndpointInfo info);
        void OnEndpointLost(string endpointId);
    }
}