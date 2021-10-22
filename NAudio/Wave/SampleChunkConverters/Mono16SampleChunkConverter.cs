﻿using NAudio.Utils;
using NAudio.Wave.WaveFormats;
using NAudio.Wave.WaveOutputs;

namespace NAudio.Wave.SampleChunkConverters
{
    internal class Mono16SampleChunkConverter : ISampleChunkConverter
    {
        private byte[] sourceBuffer;
        private int sourceSample;
        private int sourceSamples;
        private WaveBuffer sourceWaveBuffer;

        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.Pcm &&
                   waveFormat.BitsPerSample == 16 &&
                   waveFormat.Channels == 1;
        }

        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            var sourceBytesRequired = samplePairsRequired * 2;
            sourceSample = 0;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceWaveBuffer = new WaveBuffer(sourceBuffer);
            sourceSamples = source.Read(sourceBuffer, 0, sourceBytesRequired) / 2;
        }

        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (sourceSample < sourceSamples)
            {
                sampleLeft = sourceWaveBuffer.ShortBuffer[sourceSample++] / 32768.0f;
                sampleRight = sampleLeft;
                return true;
            }

            sampleLeft = 0.0f;
            sampleRight = 0.0f;
            return false;
        }
    }
}