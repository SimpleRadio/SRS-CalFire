using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Ciribob.SRS.Common.Setting;
using NLog;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings
{
    public class SyncedServerSettings
    {
        private static SyncedServerSettings instance;
        private static readonly object _lock = new();
        private static readonly Dictionary<string, string> defaults = DefaultServerSettings.Defaults;

        private readonly ConcurrentDictionary<string, string> _settings;
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public SyncedServerSettings()
        {
            _settings = new ConcurrentDictionary<string, string>();
        }

        public List<double> GlobalFrequencies { get; set; } = new();

        // Node Limit of 0 means no retransmission
        public int RetransmitNodeLimit { get; set; }

        public static SyncedServerSettings Instance
        {
            get
            {
                lock (_lock)
                {
                    if (instance == null) instance = new SyncedServerSettings();
                }

                return instance;
            }
        }

        public string GetSetting(ServerSettingsKeys key)
        {
            var setting = key.ToString();

            return _settings.GetOrAdd(setting, defaults.ContainsKey(setting) ? defaults[setting] : "");
        }

        public bool GetSettingAsBool(ServerSettingsKeys key)
        {
            return Convert.ToBoolean(GetSetting(key));
        }

        public void Decode(Dictionary<string, string> encoded)
        {
            foreach (var kvp in encoded)
            {
                _settings.AddOrUpdate(kvp.Key, kvp.Value, (key, oldVal) => kvp.Value);

                if (kvp.Key.Equals(ServerSettingsKeys.RETRANSMISSION_NODE_LIMIT.ToString()))
                {
                    if (!int.TryParse(kvp.Value, out var nodeLimit))
                        nodeLimit = 0;
                    else
                        RetransmitNodeLimit = nodeLimit;
                }
            }
        }
    }
}