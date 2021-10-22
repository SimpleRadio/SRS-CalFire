﻿using NAudio.Wave.WaveFormats;
using NAudio.Wave.WaveOutputs;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.Audio.Providers
{
    //From https://raw.githubusercontent.com/naudio/NAudio/master/NAudio/Wave/SampleProviders/VolumeSampleProvider.cs
    public class VolumeSampleProviderWithPeak : ISampleProvider
    {
        public delegate void SamplePeak(float peak);

        private readonly SamplePeak _samplePeak;
        private readonly ISampleProvider source;

        private int count;
        private float lastPeak;

        /// <summary>
        ///     Initializes a new instance of VolumeSampleProvider
        /// </summary>
        /// <param name="source">Source Sample Provider</param>
        public VolumeSampleProviderWithPeak(ISampleProvider source, SamplePeak samplePeak)
        {
            this.source = source;
            _samplePeak = samplePeak;
            Volume = 1.0f;
        }

        /// <summary>
        ///     Allows adjusting the volume, 1.0f = full volume
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        ///     WaveFormat
        /// </summary>
        public WaveFormat WaveFormat => source.WaveFormat;

        /// <summary>
        ///     Reads samples from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <param name="offset">Offset into sample buffer</param>
        /// <param name="sampleCount">Number of samples desired</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int sampleCount)
        {
            var samplesRead = source.Read(buffer, offset, sampleCount);

            for (var n = 0; n < sampleCount; n++)
            {
                var sample = buffer[offset + n];
                sample *= Volume;

                //stop over boosting
                if (sample > 1.0F)
                    sample = 1.0F;
                else if (sample < -1.0F) sample = -1.0F;

                buffer[offset + n] = sample;

                if (sample > lastPeak) lastPeak = sample;
            }

            if (count > 8)
            {
                _samplePeak(lastPeak);
                count = 0;
                lastPeak = 0;
            }
            else
            {
                count++;
            }


            return samplesRead;
        }
    }
}