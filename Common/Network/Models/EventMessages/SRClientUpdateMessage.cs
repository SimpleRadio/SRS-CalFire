namespace Ciribob.SRS.Common.Network.Models.EventMessages
{
    public class SRClientUpdateMessage
    {
        public SRClient SrClient { get; }
        public bool Connected { get; }

        public SRClientUpdateMessage(SRClient srClient, bool connected = true)
        {
            SrClient = srClient;
            Connected = connected;
        }
    }
}