using System;
using System.Collections.Generic;
using Ciribob.FS3D.SimpleRadio.Standalone.Audio;
using Ciribob.FS3D.SimpleRadio.Standalone.Client.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Models;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Settings;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Settings.Setting;
using Ciribob.SRS.Common.Network.Models;
using Ciribob.SRS.Common.Network.Singletons;
using MathNet.Filtering;
using NAudio.Dsp;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Providers
{
    public class ClientEffectsPipeline
    {
        private readonly Random _random = new Random();

        private OnlineFilter[] _filters;

        private readonly BiQuadFilter _highPassFilter;
        private readonly BiQuadFilter _lowPassFilter;

        private static readonly double HQ_RESET_CHANCE = 0.8;

        private int hqTonePosition = 0;
        private int natoPosition = 0;
        private int fmNoisePosition = 0;
        private int vhfNoisePosition = 0;
        private int uhfNoisePosition = 0;
        private int hfNoisePosition = 0;

        private readonly CachedAudioEffectProvider effectProvider = CachedAudioEffectProvider.Instance;

        private bool natoToneEnabled;
        private bool radioEffectsEnabled;
        private bool clippingEnabled;
        private float natoToneVolume;

        private float fmVol;
        private float hfVol;
        private float uhfVol;
        private float vhfVol;
        
        private float groundNoiseVol;
        private float aircraftNoiseVol;

        private int groundNoisePosition = 0;
        private int aircraftNoisePosition = 0;

        private long lastRefresh = 0; //last refresh of settings

        private readonly Settings.ProfileSettingsStore profileSettings;

        private bool radioEffects;
        private bool radioBackgroundNoiseEffect;

        private CachedAudioEffect amCollisionEffect;
        private int amEffectPosition = 0;
        private float amCollisionVol = 1.0f;

        private bool irlRadioRXInterference = false;

        private readonly SyncedServerSettings serverSettings;


        public ClientEffectsPipeline()
        {
            profileSettings = GlobalSettingsStore.Instance.ProfileSettingsStore;
            serverSettings =  SyncedServerSettings.Instance;

            _filters = new OnlineFilter[2];
            _filters[0] =
                OnlineFilter.CreateBandpass(ImpulseResponse.Finite, Constants.OUTPUT_SAMPLE_RATE, 560, 3900);
            _filters[1] =
                OnlineFilter.CreateBandpass(ImpulseResponse.Finite, Constants.OUTPUT_SAMPLE_RATE, 100, 4500);

            _highPassFilter = BiQuadFilter.HighPassFilter(Constants.OUTPUT_SAMPLE_RATE, 520, 0.97f);
            _lowPassFilter = BiQuadFilter.LowPassFilter(Constants.OUTPUT_SAMPLE_RATE, 4130, 2.0f);
            RefreshSettings();

            amCollisionEffect = CachedAudioEffectProvider.Instance.AMCollision;
           
        }

        private void RefreshSettings()
        {
            //only get settings every 3 seconds - and cache them - issues with performance
            long now = DateTime.Now.Ticks;

            if (TimeSpan.FromTicks(now - lastRefresh).TotalSeconds > 3) //3 seconds since last refresh
            {
                lastRefresh = now;

                natoToneEnabled = profileSettings.GetClientSettingBool(ProfileSettingsKeys.NATOTone);
                radioEffectsEnabled = profileSettings.GetClientSettingBool(ProfileSettingsKeys.RadioEffects);
                clippingEnabled = profileSettings.GetClientSettingBool(ProfileSettingsKeys.RadioEffectsClipping);
                natoToneVolume = profileSettings.GetClientSettingFloat(ProfileSettingsKeys.NATOToneVolume);
                amCollisionVol = profileSettings.GetClientSettingFloat(ProfileSettingsKeys.AMCollisionVolume);

                fmVol = profileSettings.GetClientSettingFloat(ProfileSettingsKeys.FMNoiseVolume);
                hfVol = profileSettings.GetClientSettingFloat(ProfileSettingsKeys.HFNoiseVolume);
                uhfVol = profileSettings.GetClientSettingFloat(ProfileSettingsKeys.UHFNoiseVolume);
                vhfVol = profileSettings.GetClientSettingFloat(ProfileSettingsKeys.VHFNoiseVolume);

                radioEffects = profileSettings.GetClientSettingBool(ProfileSettingsKeys.RadioEffects);

                radioBackgroundNoiseEffect = profileSettings.GetClientSettingBool(ProfileSettingsKeys.RadioBackgroundNoiseEffect) ;

                irlRadioRXInterference = serverSettings.GetSettingAsBool(ServerSettingsKeys.IRL_RADIO_RX_INTERFERENCE);

                aircraftNoiseVol = profileSettings.GetClientSettingFloat(ProfileSettingsKeys.AircraftNoiseVolume);
                groundNoiseVol = profileSettings.GetClientSettingFloat(ProfileSettingsKeys.GroundNoiseVolume);
                
                
            }
        }

        public float[] ProcessClientTransmissions(float[] tempBuffer, List<DeJitteredTransmission> transmissions, out int clientTransmissionLength)
        {
            RefreshSettings();
            DeJitteredTransmission lastTransmission = transmissions[0];

            clientTransmissionLength = 0;
            foreach (var transmission in transmissions)
            {
                for (int i = 0; i < transmission.PCMAudioLength; i++)
                {
                    tempBuffer[i] += transmission.PCMMonoAudio[i];
                }

                clientTransmissionLength = Math.Max(clientTransmissionLength, transmission.PCMAudioLength);
            }
            
            //only process if AM effect doesnt apply
            tempBuffer = ProcessClientAudioSamples(tempBuffer, clientTransmissionLength, 0, lastTransmission);
            
            return tempBuffer;
        }

        public float[] ProcessClientAudioSamples(float[] buffer, int count, int offset, DeJitteredTransmission transmission)
        {
            if (transmission.Modulation == Modulation.MIDS
                || transmission.Modulation == Modulation.SATCOM
                || transmission.Modulation == Modulation.INTERCOM)
            {
                if (radioEffects)
                {
                    AddRadioEffectIntercom(buffer, count, offset, transmission.Modulation);
                }
            }
            else
            {
                AddRadioEffect(buffer, count, offset, transmission.Modulation, transmission.Frequency, transmission.UnitType);
            }
            
            //final adjust
            AdjustVolume(buffer, count, offset, transmission.Volume);
            
            return buffer;
        }

        private void AdjustVolume(float[] buffer, int count, int offset, float volume)
        {
            int outputIndex = offset;
            while (outputIndex < offset + count)
            {
                buffer[outputIndex] *= volume;

                outputIndex++;
            }
        }

        private void AddRadioEffectIntercom(float[] buffer, int count, int offset,Modulation modulation)
        {
            int outputIndex = offset;
            while (outputIndex < offset + count)
            {
                var audio = _highPassFilter.Transform(buffer[outputIndex]);

                audio = _highPassFilter.Transform(audio);

                if (float.IsNaN(audio))
                    audio = _lowPassFilter.Transform(buffer[outputIndex]);
                else
                    audio = _lowPassFilter.Transform(audio);

                if (!float.IsNaN(audio))
                {
                    // clip
                    if (audio > 1.0f)
                        audio = 1.0f;
                    if (audio < -1.0f)
                        audio = -1.0f;

                    buffer[outputIndex] = audio;
                }

                outputIndex++;
            }
        }


        private void AddRadioEffect(float[] buffer, int count, int offset, Modulation modulation, double freq,
            string transmissionUnitType)
        {
            int outputIndex = offset;
             
            while (outputIndex < offset + count)
            {
                var audio = (double) buffer[outputIndex];

                if (radioEffectsEnabled)
                {
                    if (clippingEnabled)
                    {
                        if (audio > RadioFilter.CLIPPING_MAX)
                        {
                            audio = RadioFilter.CLIPPING_MAX;
                        }
                        else if (audio < RadioFilter.CLIPPING_MIN)
                        {
                            audio = RadioFilter.CLIPPING_MIN;
                        }
                    }

                    //high and low pass filter
                    for (int j = 0; j < _filters.Length; j++)
                    {
                        var filter = _filters[j];
                        audio = filter.ProcessSample(audio);
                        if (double.IsNaN(audio))
                        {
                            audio = (double)buffer[outputIndex];
                        }

                        audio *= RadioFilter.BOOST;
                    }
                }

                if (radioBackgroundNoiseEffect)
                {
                    if (effectProvider.AircraftNoise.Loaded && transmissionUnitType == PlayerUnitStateBase.TYPE_AIRCRAFT)
                    {
                        var noise = effectProvider.AircraftNoise.AudioEffectFloat;
                        audio += ((noise[aircraftNoisePosition]) * aircraftNoiseVol);
                        aircraftNoisePosition++;

                        if (aircraftNoisePosition == noise.Length)
                        {
                            aircraftNoisePosition = 0;
                        }
                    }
                    else if (effectProvider.GroundNoise.Loaded)
                    {
                        var noise = effectProvider.GroundNoise.AudioEffectFloat;
                        audio += ((noise[groundNoisePosition]) * groundNoiseVol);
                        groundNoisePosition++;

                        if (groundNoisePosition == noise.Length)
                        {
                            groundNoisePosition = 0;
                        }
                    }
                }

                if (modulation == Modulation.FM
                    && effectProvider.NATOTone.Loaded
                    && natoToneEnabled)
                {
                    var natoTone = effectProvider.NATOTone.AudioEffectFloat;
                    audio += ((natoTone[natoPosition]) * natoToneVolume);
                    natoPosition++;

                    if (natoPosition == natoTone.Length)
                    {
                        natoPosition = 0;
                    }
                }

                audio = AddRadioBackgroundNoiseEffect(audio, modulation,freq);

                // clip
                if (audio > 1.0f)
                    audio = 1.0f;
                if (audio < -1.0f)
                    audio = -1.0f;

                buffer[outputIndex] = (float) audio;

                outputIndex++;
            }
        }

        private double AddRadioBackgroundNoiseEffect(double audio, Modulation modulation, double freq)
        {
            if (radioBackgroundNoiseEffect)
            {
                if (modulation == Modulation.HAVEQUICK || modulation == Modulation.AM)
                {
                    //mix in based on frequency
                    if (freq >= 200d * 1000000)
                    {
                        if (effectProvider.UHFNoise.Loaded)
                        {
                            var noise = effectProvider.UHFNoise.AudioEffectFloat;
                            //UHF Band?
                            audio += ((noise[uhfNoisePosition]) * uhfVol);
                            uhfNoisePosition++;

                            if (uhfNoisePosition == noise.Length)
                            {
                                uhfNoisePosition = 0;
                            }
                        }
                    }
                    else if (freq > 80d * 1000000)
                    {
                        if (effectProvider.VHFNoise.Loaded)
                        {
                            //VHF Band? - Very rough
                            var noise = effectProvider.VHFNoise.AudioEffectFloat;
                            audio += ((noise[vhfNoisePosition]) * vhfVol);
                            vhfNoisePosition++;

                            if (vhfNoisePosition == noise.Length)
                            {
                                vhfNoisePosition = 0;
                            }
                        }
                    }
                    else
                    {
                        if (effectProvider.HFNoise.Loaded)
                        {
                            //HF!
                            var noise = effectProvider.HFNoise.AudioEffectFloat;
                            audio += ((noise[hfNoisePosition]) * hfVol);
                            hfNoisePosition++;

                            if (hfNoisePosition == noise.Length)
                            {
                                hfNoisePosition = 0;
                            }
                        }
                    }
                }
                else if (modulation == Modulation.FM)
                {
                    if (effectProvider.FMNoise.Loaded)
                    {
                        //FM picks up most of the 20-60 ish range + has a different effect
                        //HF!
                        var noise = effectProvider.FMNoise.AudioEffectFloat;
                        //UHF Band?
                        audio += ((noise[fmNoisePosition]) * fmVol);
                        fmNoisePosition++;

                        if (fmNoisePosition == noise.Length)
                        {
                            fmNoisePosition = 0;
                        }
                    }
                }
            }

            return audio;
        }
    }
}
