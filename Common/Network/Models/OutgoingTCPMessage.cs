using System.Collections.Generic;
using System.Net.Sockets;
using Ciribob.SRS.Common.Network.Models;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Server.Network.Models
{
    public class OutgoingTCPMessage
    {
        public NetworkMessage NetworkMessage { get; set; }

        public List<Socket> SocketList { get; set; }
    }
}