namespace Ciribob.FS3D.SimpleRadio.Standalone.Mobile.Models.RadioChannels;

public interface IPresetChannelsStore
{
    IEnumerable<PresetChannel> LoadFromStore(string radioName);
}