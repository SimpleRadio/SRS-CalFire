using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Annotations;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings.RadioChannels;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Utils;
using Ciribob.SRS.Common.Helpers;
using Ciribob.SRS.Common.Network.Models;
using NLog;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.Singletons.Models
{
    public class Radio: PropertyChangedBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private string _name = "";
        public RadioConfig Config { get; set; } = new();

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                ReloadChannels();
            }
        }

        public double Freq { get; set; } = 1;
        public Modulation Modulation { get; set; } = Modulation.DISABLED;

        public bool Encrypted { get; set; } = false;
        public byte EncKey { get; set; } = 0;

        public double SecFreq { get; set; } = 1;

        //should the radio restransmit?
        public bool Retransmit { get; set; }
        public float Volume { get; set; } = 1.0f;
        public int CurrentChannel { get; set; } = -1;
        public bool SimultaneousTransmission { get; set; }

        //Channels
        public ObservableCollection<PresetChannel> PresetChannels { get; } = new ObservableCollection<PresetChannel>();

        /**
         * Used to determine if we should send an update to the server or not
         * We only need to do that if something that would stop us Receiving happens which
         * is frequencies and modulation
         */
        public bool Available()
        {
            return Modulation != Modulation.DISABLED;
        }

        public Radio()
        {
            ReloadChannels();
        }

        public void ReloadChannels()
        {
            PresetChannels.Clear();
            if (Name.Length == 0 || !Available())
            {
                return;
            }
            foreach (var presetChannel in new FilePresetChannelsStore().LoadFromStore(Name))
            {
                var freq = (double)presetChannel.Value;
                if (freq < Config.MaxFrequency && freq > Config.MinimumFrequency)
                {
                    PresetChannels.Add(presetChannel);
                    Logger.Info($"Added {presetChannel.Text} for radio {Name} with frequency {freq}");
                }
                else
                {
                    Logger.Error($"Unable to add {presetChannel.Text} for radio {Name} with frequency {freq} - outside of radio range");
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var compare = (Radio)obj;

            if (!Name.Equals(compare.Name)) return false;
            if (!RadioBase.FreqCloseEnough(Freq, compare.Freq)) return false;
            if (Modulation != compare.Modulation) return false;
            if (Encrypted != compare.Encrypted) return false;
            if (EncKey != compare.EncKey) return false;
            if (Retransmit != compare.Retransmit) return false;
            if (!RadioBase.FreqCloseEnough(SecFreq, compare.SecFreq)) return false;

            if (Config != null && compare.Config == null) return false;
            if (Config == null && compare.Config != null) return false;

            return Config.Equals(compare.Config);
        }

        public RadioBase RadioBase
        {
            get
            {
                return new RadioBase()
                {
                    Encrypted = Encrypted,
                    EncKey = EncKey,
                    Modulation = Modulation,
                    Freq = Freq,
                    SecFreq = SecFreq
                };
            }
        }
    }
}