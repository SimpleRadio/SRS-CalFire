using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave.WaveFormats
{
    /// <summary>
    ///     MP3 WaveFormat, MPEGLAYER3WAVEFORMAT from mmreg.h
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
    public class Mp3WaveFormat : WaveFormat
    {
        private const short Mp3WaveFormatExtraBytes = 12; // MPEGLAYER3_WFX_EXTRA_BYTES

        /// <summary>
        ///     Block Size (nBlockSize)
        /// </summary>
        public ushort blockSize;

        /// <summary>
        ///     Codec Delay (nCodecDelay)
        /// </summary>
        public ushort codecDelay;

        /// <summary>
        ///     Padding flags (fdwFlags)
        /// </summary>
        public Mp3WaveFormatFlags flags;

        /// <summary>
        ///     Frames per block (nFramesPerBlock)
        /// </summary>
        public ushort framesPerBlock;

        /// <summary>
        ///     Wave format ID (wID)
        /// </summary>
        public Mp3WaveFormatId id;

        /// <summary>
        ///     Creates a new MP3 WaveFormat
        /// </summary>
        public Mp3WaveFormat(int sampleRate, int channels, int blockSize, int bitRate)
        {
            waveFormatTag = WaveFormatEncoding.MpegLayer3;
            this.channels = (short)channels;
            averageBytesPerSecond = bitRate / 8;
            bitsPerSample = 0; // must be zero
            blockAlign = 1; // must be 1
            this.sampleRate = sampleRate;

            extraSize = Mp3WaveFormatExtraBytes;
            id = Mp3WaveFormatId.Mpeg;
            flags = Mp3WaveFormatFlags.PaddingIso;
            this.blockSize = (ushort)blockSize;
            framesPerBlock = 1;
            codecDelay = 0;
        }
    }

    /// <summary>
    ///     Wave Format Padding Flags
    /// </summary>
    [Flags]
    public enum Mp3WaveFormatFlags
    {
        /// <summary>
        ///     MPEGLAYER3_FLAG_PADDING_ISO
        /// </summary>
        PaddingIso = 0,

        /// <summary>
        ///     MPEGLAYER3_FLAG_PADDING_ON
        /// </summary>
        PaddingOn = 1,

        /// <summary>
        ///     MPEGLAYER3_FLAG_PADDING_OFF
        /// </summary>
        PaddingOff = 2
    }

    /// <summary>
    ///     Wave Format ID
    /// </summary>
    public enum Mp3WaveFormatId : ushort
    {
        /// <summary>MPEGLAYER3_ID_UNKNOWN</summary>
        Unknown = 0,

        /// <summary>MPEGLAYER3_ID_MPEG</summary>
        Mpeg = 1,

        /// <summary>MPEGLAYER3_ID_CONSTANTFRAMESIZE</summary>
        ConstantFrameSize = 2
    }
}