﻿using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Wave.WaveFormats;

namespace Ciribob.FS3D.SimpleRadio.Standalone.Client.Audio.Providers
{
    public class RadioAudioProvider : AudioProvider
    {
        public RadioAudioProvider(int sampleRate)
        {
            BufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(sampleRate, 16, 2));

            BufferedWaveProvider.ReadFully = false;
            BufferedWaveProvider.DiscardOnBufferOverflow = true;

            var pcm = new Pcm16BitToSampleProvider(BufferedWaveProvider);

            VolumeSampleProvider = new VolumeSampleProvider(pcm);
        }

        public VolumeSampleProvider VolumeSampleProvider { get; }
        private BufferedWaveProvider BufferedWaveProvider { get; }

        public void AddAudioSamples(byte[] pcmAudio, int radioId, bool isStereo = false)
        {
            if (isStereo)
            {
                BufferedWaveProvider.AddSamples(pcmAudio, 0, pcmAudio.Length);
            }
            else
            {
                var seperatedAudio = SeperateAudio(pcmAudio, radioId);
                BufferedWaveProvider.AddSamples(seperatedAudio, 0, seperatedAudio.Length);
            }
        }
    }
}