using System.Collections.Generic;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Settings.Setting;
using Ciribob.SRS.Common.Helpers;
using Newtonsoft.Json;

namespace Ciribob.SRS.Common.Network.Models;

public class NetworkMessage
{
    public enum MessageType
    {
        PING,
        SYNC,
        FULL_UPDATE, //entire state - all currently known fields
        PARTIAL_UPDATE, // everything other than radios
        SERVER_SETTINGS,
        CLIENT_DISCONNECT, // Client disconnected
        VERSION_MISMATCH,
        EXTERNAL_AWACS_MODE_PASSWORD, // Received server side to "authenticate"/pick side for external AWACS mode
        EXTERNAL_AWACS_MODE_DISCONNECT // Received server side on "voluntary" disconnect by the client (without closing the server connection)
    }

    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        ContractResolver = new JsonNetworkPropertiesResolver(), // strip out things not required for the TCP sync
        NullValueHandling = NullValueHandling.Ignore // same some network bandwidth
    };

    public SRClientBase Client { get; set; }

    public MessageType MsgType { get; set; }

    public List<SRClientBase> Clients { get; set; }

    public Dictionary<string, string> ServerSettings { get; set; }

    public string ExternalAWACSModePassword { get; set; }

    public string Version { get; set; }

    public string Encode()
    {
        Version = UpdaterChecker.VERSION;
        return JsonConvert.SerializeObject(this, JsonSerializerSettings) + "\n";
    }
}