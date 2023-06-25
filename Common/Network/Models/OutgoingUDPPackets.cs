using System.Collections.Generic;
using System.Net;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Server.Network.Models;

public class OutgoingUDPPackets
{
    public List<IPEndPoint> OutgoingEndPoints { get; set; }
    public byte[] ReceivedPacket { get; set; }
}