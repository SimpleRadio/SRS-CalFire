using System;
using NAudio.Wave.WaveFormats;
using NAudio.Wave.WaveOutputs;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    ///     Simple class that raises an event on every sample
    /// </summary>
    public class NotifyingSampleProvider : ISampleProvider, ISampleNotifier
    {
        private readonly int channels;

        // try not to give the garbage collector anything to deal with when playing live audio
        private readonly SampleEventArgs sampleArgs = new(0, 0);
        private readonly ISampleProvider source;

        /// <summary>
        ///     Initializes a new instance of NotifyingSampleProvider
        /// </summary>
        /// <param name="source">Source Sample Provider</param>
        public NotifyingSampleProvider(ISampleProvider source)
        {
            this.source = source;
            channels = WaveFormat.Channels;
        }

        /// <summary>
        ///     Sample notifier
        /// </summary>
        public event EventHandler<SampleEventArgs> Sample;

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
            if (Sample != null)
                for (var n = 0; n < samplesRead; n += channels)
                {
                    sampleArgs.Left = buffer[offset + n];
                    sampleArgs.Right = channels > 1 ? buffer[offset + n + 1] : sampleArgs.Left;
                    Sample(this, sampleArgs);
                }

            return samplesRead;
        }
    }
}