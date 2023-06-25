using System.Collections.Generic;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Common.Settings.Setting;

public enum ServerSettingsKeys
{
    SERVER_PORT = 0,
    LOS_ENABLED = 1, // not used
    DISTANCE_ENABLED = 2,
    IRL_RADIO_TX = 3,
    IRL_RADIO_RX_INTERFERENCE = 4, // not used
    IRL_RADIO_STATIC = 5, // Not used
    TEST_FREQUENCIES = 6,
    SHOW_TUNED_COUNT = 7,
    SHOW_TRANSMITTER_NAME = 8, // not used
    RETRANSMISSION_NODE_LIMIT = 9, // not used
    SERVER_RECORDING = 10,
    SERVER_RECORDING_FREQUENCIES = 11
}

public class DefaultServerSettings
{
    public static readonly Dictionary<string, string> Defaults = new()
    {
        { ServerSettingsKeys.DISTANCE_ENABLED.ToString(), "false" },
        { ServerSettingsKeys.IRL_RADIO_RX_INTERFERENCE.ToString(), "false" },
        { ServerSettingsKeys.IRL_RADIO_STATIC.ToString(), "false" },
        { ServerSettingsKeys.IRL_RADIO_TX.ToString(), "true" },
        { ServerSettingsKeys.LOS_ENABLED.ToString(), "false" },
        { ServerSettingsKeys.SERVER_PORT.ToString(), "5002" },
        { ServerSettingsKeys.SERVER_RECORDING.ToString(), "false" },
        { ServerSettingsKeys.TEST_FREQUENCIES.ToString(), "247.2,120.3" },
        { ServerSettingsKeys.SHOW_TUNED_COUNT.ToString(), "true" },
        { ServerSettingsKeys.SHOW_TRANSMITTER_NAME.ToString(), "false" },
        { ServerSettingsKeys.RETRANSMISSION_NODE_LIMIT.ToString(), "0" },
        { ServerSettingsKeys.SERVER_RECORDING_FREQUENCIES.ToString(), "250.1,249.1" }
    };
}