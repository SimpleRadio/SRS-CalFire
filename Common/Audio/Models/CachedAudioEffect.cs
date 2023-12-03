﻿using System;
using System.IO;
using Ciribob.FS3D.SimpleRadio.Standalone.Audio;
using Ciribob.FS3D.SimpleRadio.Standalone.Audio.Utility;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Providers;
using NAudio.Wave;
using NLog;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Models;

public class CachedAudioEffect
{
    private CachedAudioEffectProvider.CachedEffectsLoaderDelegate _loaderDelegate;
    public enum AudioEffectTypes
    {
        RADIO_TRANS_START = 0,
        RADIO_TRANS_END = 1,
        NATO_TONE = 4,
        VHF_NOISE = 8,
        HF_NOISE = 9,
        UHF_NOISE = 10,
        FM_NOISE = 11,
        INTERCOM_TRANS_START = 12,
        INTERCOM_TRANS_END = 13,
        AM_COLLISION = 14,
        AIRCRAFT_NOISE = 15,
        GROUND_NOISE = 16
    }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly WaveFormat RequiredFormat = new(Constants.OUTPUT_SAMPLE_RATE, 16, 1);

    public CachedAudioEffect(AudioEffectTypes audioEffect,
        CachedAudioEffectProvider.CachedEffectsLoaderDelegate cachedEffectsLoaderDelegate) : this(audioEffect, audioEffect + ".wav",
        AppDomain.CurrentDomain.BaseDirectory + $"{Path.DirectorySeparatorChar}AudioEffects{Path.DirectorySeparatorChar}" + audioEffect + ".wav", cachedEffectsLoaderDelegate)
    {
    }

    public CachedAudioEffect(AudioEffectTypes audioEffect, string fileName, string path, CachedAudioEffectProvider.CachedEffectsLoaderDelegate cachedEffectsLoaderDelegate)
    {
        _loaderDelegate = cachedEffectsLoaderDelegate;
        FileName = fileName;
        AudioEffectType = audioEffect;

        var file = path;

        AudioEffectFloat = null;

        try
        {
            if (_loaderDelegate != null)
            {
                using (Stream str = _loaderDelegate(fileName))
                {
                    //TODO check this isnt leaking memory - it should close the stream
                    using (var reader = new WaveFileReader(str))
                    {
                        //    Assert.AreEqual(16, reader.WaveFormat.BitsPerSample, "Only works with 16 bit audio");
                        if (reader.WaveFormat.BitsPerSample == RequiredFormat.BitsPerSample &&
                            reader.WaveFormat.SampleRate == reader.WaveFormat.SampleRate && reader.WaveFormat.Channels == 1)
                        {
                            var tmpBytes = new byte[reader.Length];
                            var read = reader.Read(tmpBytes, 0, tmpBytes.Length);
                            Logger.Info($"Read Effect {audioEffect} from Stream Successfully - Format {reader.WaveFormat}");

                            //convert to short  - 16 - then to float 32
                            var tmpShort = ConversionHelpers.ByteArrayToShortArray(tmpBytes);

                            //now to float
                            AudioEffectFloat = ConversionHelpers.ShortPCM16ArrayToFloat32Array(tmpShort);

                            Loaded = true;
                        }
                        else
                        {
                            Logger.Info(
                                $"Unable to read Effect {audioEffect} from Stream Successfully - {reader.WaveFormat} is not {RequiredFormat} !");
                        }
                    }
                }
            }
            else
            {
                if (File.Exists(file))
                    using (var reader = new WaveFileReader(file))
                    {
                        //    Assert.AreEqual(16, reader.WaveFormat.BitsPerSample, "Only works with 16 bit audio");
                        if (reader.WaveFormat.BitsPerSample == RequiredFormat.BitsPerSample &&
                            reader.WaveFormat.SampleRate == reader.WaveFormat.SampleRate && reader.WaveFormat.Channels == 1)
                        {
                            var tmpBytes = new byte[reader.Length];
                            var read = reader.Read(tmpBytes, 0, tmpBytes.Length);
                            Logger.Info($"Read Effect {audioEffect} from {file} Successfully - Format {reader.WaveFormat}");

                            //convert to short  - 16 - then to float 32
                            var tmpShort = ConversionHelpers.ByteArrayToShortArray(tmpBytes);

                            //now to float
                            AudioEffectFloat = ConversionHelpers.ShortPCM16ArrayToFloat32Array(tmpShort);

                            Loaded = true;
                        }
                        else
                        {
                            Logger.Info(
                                $"Unable to read Effect {audioEffect} from {file} Successfully - {reader.WaveFormat} is not {RequiredFormat} !");
                        }
                    }
                else
                    Logger.Info($"Unable to find file for effect {audioEffect} in AudioEffects{Path.DirectorySeparatorChar}{FileName} ");
            }
            
        }
        catch (Exception ex)
        {
            Logger.Error($"Unable to find file for effect {audioEffect} in AudioEffects{Path.DirectorySeparatorChar}{FileName} ", ex);
        }
    }

    /** Needed for list view ***/
    public string Text => FileName;

    /** Needed for list view ***/
    public object Value => this;

    public string FileName { get; }

    public bool Loaded { get; }

    public AudioEffectTypes AudioEffectType { get; }

    public float[] AudioEffectFloat { get; set; }

    /** Needed for list view ***/
    public override string ToString()
    {
        return Text;
    }
}