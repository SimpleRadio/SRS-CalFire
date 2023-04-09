using System;
using System.Collections.Generic;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Models;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Providers;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Audio.Utility;
using Ciribob.FS3D.SimpleRadio.Standalone.Common.Network.Client;
using NAudio.Utils;
using NAudio.Wave;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Server.Audio;

//mixes down a single clients audio to a single stream for output
public class ClientRecordedAudioProvider : ClientAudioProvider, ISampleProvider
{
    private ClientEffectsPipeline pipeline = new();
    private List<DeJitteredTransmission> _mainAudio = new();
    private float[] mixBuffer;

    public ClientRecordedAudioProvider(WaveFormat waveFormat, string guid) : base(false)
    {
        if (waveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
        {
            throw new ArgumentException("Mixer wave format must be IEEE float");
        }

        WaveFormat = waveFormat;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        _mainAudio.Clear();
        int primarySamples = 0;

        mixBuffer = BufferHelpers.Ensure(mixBuffer, count);
        ClearArray(mixBuffer);

        lock (JitterBufferProviderInterface)
        {
            
            int index = JitterBufferProviderInterface.Length - 1;
            while (index >= 0)
            {
                var source = JitterBufferProviderInterface[index];
                
                var transmission = source.Read(count);

                if (transmission.PCMAudioLength > 0)
                {
                    _mainAudio.Add(transmission);
                }
                index--;
            }
        }
        
        //merge all the audio for the client
        if (_mainAudio.Count > 0)
        {
            mixBuffer = pipeline.ProcessClientTransmissions(mixBuffer, _mainAudio, out primarySamples);
        }
        
        //Mix into the buffer
        for (int i = 0; i < primarySamples; i++)
        {
            buffer[i + offset] += mixBuffer[i];
        }

        //dont return full set
        return primarySamples;
    }

    public WaveFormat WaveFormat { get; }
    
    public float[] ClearArray(float[] buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = 0;
        }

        return buffer;
    }
    
    private int EnsureFullBuffer(float[] buffer, int samplesCount, int offset, int count)
    {
        // ensure we return a full buffer of STEREO
        if (samplesCount < count)
        {
            int outputIndex = offset + samplesCount;
            while (outputIndex < offset + count)
            {
                buffer[outputIndex++] = 0;
            }
    
            samplesCount = count;
        }
    
        //Should be impossible - ensures audio doesnt crash if its not
        if (samplesCount > count)
        {
            samplesCount = count;
        }
    
        return samplesCount;
    }
}