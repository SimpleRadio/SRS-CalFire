namespace Ciribob.SRS.Common.Network.Models.EventMessages
{
    public class SRClientUpdateMessage
    {
        public SRClientBase SrClient { get; }
        public bool Connected { get; }

        public SRClientUpdateMessage(SRClientBase srClient, bool connected = true)
        {
            SrClient = srClient;
            Connected = connected;
        }
    }
}