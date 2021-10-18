using System.Net;
using System.Net.Sockets;

namespace Ciribob.SRS.Server.Network
{
    public class PendingPacket
    {
        public IPEndPoint ReceivedFrom { get; set; }
        public byte[] RawBytes { get; set; }
    }
}