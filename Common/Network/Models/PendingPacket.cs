using System.Net;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Server.Network.Models;

public class PendingPacket
{
    public IPEndPoint ReceivedFrom { get; set; }
    public byte[] RawBytes { get; set; }
}