using Android.Gms.Nearby.Connection;

namespace NearbySample
{
    public interface IOnAdvertiseCallback
    {
        void OnConnectionInitiated(string endpointId, ConnectionInfo connectionInfo);
        void OnConnectionResult(string endpointId, ConnectionResolution resolution);
        void OnDisconnected(string endpointId);
        void OnEndpointFound(string endpointId, DiscoveredEndpointInfo info);
    }
}