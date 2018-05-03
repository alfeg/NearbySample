using Android.Gms.Nearby.Connection;

namespace NearbySample.Core
{
    internal interface IPayloadCallback
    {
        void OnPayloadReceived(string endpointId, Payload payload);
        void OnPayloadTransferUpdate(string endpointId, PayloadTransferUpdate update);
    }
}