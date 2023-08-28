using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Common.Network.Models;

public class PresetChannels
{
    public Dictionary<string, List<PresetChannel>> RadioPresets = new();

    public static string NormaliseRadioName(string name)
    {
        //only allow alphanumeric, remove all spaces etc
        return Regex.Replace(name, "[^a-zA-Z0-9]", "").ToLower();
    }

    public List<PresetChannel> GetRadioPresets(string name)
    {
        List<PresetChannel> presetChannels;
        if (RadioPresets.TryGetValue(NormaliseRadioName(name), out presetChannels))
        {
            return presetChannels;
        }
        else
        {
            return new List<PresetChannel>();
        }
    }

    public void AddRadioPreset(string radioName, PresetChannel presetChannel)
    {
        radioName = NormaliseRadioName(radioName);

        List<PresetChannel> presets;
        if (!RadioPresets.TryGetValue(radioName, out presets))
        {
            presets = new List<PresetChannel>();
        }
        
        presets.Add(presetChannel);
        
        RadioPresets.Add(radioName,presets);
    }

    public static PresetChannels AddRadioPresets(string[] lines)
    {
        PresetChannels presetChannels = new PresetChannels();
        
        const double MHz = 1000000;
        if (lines?.Length > 0)
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Length > 0)
                    try
                    {
                        var radioName = "";
                        var frequencyText = "";
                        string channelName = null;
                        //spilt on | 
                        if (line.Contains('|'))
                        {
                            var spilt = line.Split('|');

                            if (spilt.Length == 2)
                            {
                                frequencyText = spilt[1].Trim();
                                radioName = spilt[0].Trim();
                                
                            }else if (spilt.Length == 3)
                            {
                                frequencyText = spilt[2].Trim();
                                channelName = spilt[1].Trim();
                                radioName = spilt[0].Trim();
                            }

                            double frequency;
                            if (double.TryParse(frequencyText, CultureInfo.InvariantCulture, out frequency))
                            {
                                presetChannels.AddRadioPreset(radioName, new PresetChannel()
                                {
                                    ChannelName = channelName ?? frequencyText, //use channel name if not null,
                                    PresetFrequency = frequency * MHz
                                });
                            }
                        }
                        else
                        {
                            //invalid
                        }
                    }
                    catch (Exception)
                    {
                        //ignore for now
                    }
            }

        return presetChannels;
    }
}

public class PresetChannel
{
    public string ChannelName { get; set; } = "";
    public double PresetFrequency { get; set; }
}