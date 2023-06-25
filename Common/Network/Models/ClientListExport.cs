using System.Collections.Generic;

namespace Ciribob.SRS.Common.Network.Models;

public struct ClientListExport
{
    public ICollection<SRClientBase> Clients { get; set; }

    public string ServerVersion { get; set; }
}