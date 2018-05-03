using Android.Gms.Nearby.Connection;

namespace NearbySample.Core
{
    internal class OnPayloadCallback : PayloadCallback
    {
        private readonly IPayloadCallback callback;

        public OnPayloadCallback(IPayloadCallback callback)
        {
            this.callback = callback;
        }

        public override void OnPayloadReceived(string endpointId, Payload payload)
        {
            callback.OnPayloadReceived(endpointId, payload);
        }

        public override void OnPayloadTransferUpdate(string endpointId, PayloadTransferUpdate update)
        {
            callback.OnPayloadTransferUpdate(endpointId, update);
        }
    }
}