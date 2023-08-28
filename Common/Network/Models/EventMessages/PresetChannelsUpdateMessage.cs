using Ciribob.FS3D.SimpleRadio.Standalone.Common.Network.Models;

namespace Ciribob.SRS.Common.Network.Models.EventMessages;

public class PresetChannelsUpdateMessage
{
    //TODO handle this in the client singleton on the mobile app and desktop app
    //copy the preset channels to the radios which will trigger the change on the UI
    public PresetChannels PresetChannels { get; }

    public PresetChannelsUpdateMessage(PresetChannels presetChannels)
    {
        PresetChannels = presetChannels;
    }
}