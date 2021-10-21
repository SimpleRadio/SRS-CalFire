using System;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Input;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings.RadioChannels;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Utils;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.UI.Common.PresetChannels
{
    public class PresetChannelsViewModel
    {
        private IPresetChannelsStore _channelsStore;
        private int _radioId;

        public DelegateCommand DropDownClosedCommand { get; set; }


        private readonly object _presetChannelLock = new object();
        private ObservableCollection<PresetChannel> _presetChannels;

        public ObservableCollection<PresetChannel> PresetChannels
        {
            get => _presetChannels;
            set
            {
                _presetChannels = value;
                BindingOperations.EnableCollectionSynchronization(_presetChannels, _presetChannelLock);
            }
        }

        public int RadioId
        {
            private get { return _radioId; }
            set
            {
                _radioId = value;
                Reload();
            }
        }

        public PresetChannelsViewModel(IPresetChannelsStore channels, int radioId)
        {
            _radioId = radioId;
            _channelsStore = channels;
            ReloadCommand = new DelegateCommand(OnReload);
            DropDownClosedCommand = new DelegateCommand(DropDownClosed);
            PresetChannels = new ObservableCollection<PresetChannel>();
        }


        public ICommand ReloadCommand { get; }

        private void DropDownClosed(object args)
        {
            if (SelectedPresetChannel != null
                && SelectedPresetChannel.Value is double
                && (double)SelectedPresetChannel.Value > 0 && RadioId > 0)
                RadioHelper.SelectRadioChannel(SelectedPresetChannel, RadioId);
        }

        public PresetChannel SelectedPresetChannel { get; set; }

        public double Max { get; set; }
        public double Min { get; set; }

        public void Reload()
        {
            PresetChannels.Clear();

            var radios = ClientStateSingleton.Instance.PlayerUnitState.Radios;

            var radio = radios[_radioId];

            var i = 1;
            foreach (var channel in _channelsStore.LoadFromStore(radio.Name))
                if ((double)channel.Value < Max
                    && (double)channel.Value > Min)
                {
                    channel.Channel = i++;
                    PresetChannels.Add(channel);
                }
        }

        private void OnReload()
        {
            Reload();
        }

        public void Clear()
        {
            PresetChannels.Clear();
        }
    }
}