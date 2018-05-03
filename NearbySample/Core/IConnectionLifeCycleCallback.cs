using Android.Gms.Nearby.Connection;

namespace NearbySample.Core
{
    public interface IConnectionLifeCycleCallback
    {
        void OnConnectionInitiated(string endpointId, ConnectionInfo connectionInfo);
        void OnConnectionResult(string endpointId, ConnectionResolution resolution);
        void OnDisconnected(string endpointId);
    }
}