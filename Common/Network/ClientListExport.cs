using System.Collections.Generic;

namespace Ciribob.SRS.Common.Network
{
    public struct ClientListExport
    {
        public ICollection<SRClient> Clients { get; set; }

        public string ServerVersion { get; set; }
    }
}
