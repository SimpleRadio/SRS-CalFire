using System.IO;
using System.Runtime.InteropServices;
using NAudio.Wave.WaveFormats;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    ///     Microsoft ADPCM
    ///     See http://icculus.org/SDL_sound/downloads/external_documentation/wavecomp.htm
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public class AdpcmWaveFormat : WaveFormat
    {
        // 7 pairs of coefficients

        private readonly short numCoeff;
        private readonly short samplesPerBlock;

        /// <summary>
        ///     Empty constructor needed for marshalling from a pointer
        /// </summary>
        private AdpcmWaveFormat() : this(8000, 1)
        {
        }

        /// <summary>
        ///     Microsoft ADPCM
        /// </summary>
        /// <param name="sampleRate">Sample Rate</param>
        /// <param name="channels">Channels</param>
        public AdpcmWaveFormat(int sampleRate, int channels) :
            base(sampleRate, 0, channels)
        {
            waveFormatTag = WaveFormatEncoding.Adpcm;

            // TODO: validate sampleRate, bitsPerSample
            extraSize = 32;
            switch (this.sampleRate)
            {
                case 8000:
                case 11025:
                    blockAlign = 256;
                    break;
                case 22050:
                    blockAlign = 512;
                    break;
                case 44100:
                default:
                    blockAlign = 1024;
                    break;
            }

            bitsPerSample = 4;
            samplesPerBlock = (short)((blockAlign - 7 * channels) * 8 / (bitsPerSample * channels) + 2);
            averageBytesPerSecond =
                SampleRate * blockAlign / samplesPerBlock;

            // samplesPerBlock = blockAlign - (7 * channels)) * (2 / channels) + 2;


            numCoeff = 7;
            Coefficients = new short[14]
            {
                256, 0, 512, -256, 0, 0, 192, 64, 240, 0, 460, -208, 392, -232
            };
        }

        /// <summary>
        ///     Samples per block
        /// </summary>
        public int SamplesPerBlock => samplesPerBlock;

        /// <summary>
        ///     Number of coefficients
        /// </summary>
        public int NumCoefficients => numCoeff;

        /// <summary>
        ///     Coefficients
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public short[] Coefficients { get; }

        /// <summary>
        ///     Serializes this wave format
        /// </summary>
        /// <param name="writer">Binary writer</param>
        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(samplesPerBlock);
            writer.Write(numCoeff);
            foreach (var coefficient in Coefficients) writer.Write(coefficient);
        }

        /// <summary>
        ///     String Description of this WaveFormat
        /// </summary>
        public override string ToString()
        {
            return string.Format("Microsoft ADPCM {0} Hz {1} channels {2} bits per sample {3} samples per block",
                SampleRate, channels, bitsPerSample, samplesPerBlock);
        }
    }
}