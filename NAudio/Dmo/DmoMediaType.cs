using System;
using System.Runtime.InteropServices;
using NAudio.Wave.WaveFormats;

namespace NAudio.Dmo
{
    /// <summary>
    ///     http://msdn.microsoft.com/en-us/library/aa929922.aspx
    ///     DMO_MEDIA_TYPE
    /// </summary>
    public struct DmoMediaType
    {
        private Guid subtype;
        private bool bTemporalCompression;
        private IntPtr pUnk; // not used
        private int cbFormat;
        private IntPtr pbFormat;

        /// <summary>
        ///     Major type
        /// </summary>
        public Guid MajorType { get; private set; }

        /// <summary>
        ///     Major type name
        /// </summary>
        public string MajorTypeName => MediaTypes.GetMediaTypeName(MajorType);

        /// <summary>
        ///     Subtype
        /// </summary>
        public Guid SubType => subtype;

        /// <summary>
        ///     Subtype name
        /// </summary>
        public string SubTypeName
        {
            get
            {
                if (MajorType == MediaTypes.MEDIATYPE_Audio) return AudioMediaSubtypes.GetAudioSubtypeName(subtype);

                return subtype.ToString();
            }
        }

        /// <summary>
        ///     Fixed size samples
        /// </summary>
        public bool FixedSizeSamples { get; private set; }

        /// <summary>
        ///     Sample size
        /// </summary>
        public int SampleSize { get; }

        /// <summary>
        ///     Format type
        /// </summary>
        public Guid FormatType { get; private set; }

        /// <summary>
        ///     Format type name
        /// </summary>
        public string FormatTypeName
        {
            get
            {
                if (FormatType == DmoMediaTypeGuids.FORMAT_None) return "None";

                if (FormatType == Guid.Empty) return "Null";

                if (FormatType == DmoMediaTypeGuids.FORMAT_WaveFormatEx) return "WaveFormatEx";

                return FormatType.ToString();
            }
        }

        /// <summary>
        ///     Gets the structure as a Wave format (if it is one)
        /// </summary>
        public WaveFormat GetWaveFormat()
        {
            if (FormatType == DmoMediaTypeGuids.FORMAT_WaveFormatEx) return WaveFormat.MarshalFromPtr(pbFormat);

            throw new InvalidOperationException("Not a WaveFormat type");
        }

        /// <summary>
        ///     Sets this object up to point to a wave format
        /// </summary>
        /// <param name="waveFormat">Wave format structure</param>
        public void SetWaveFormat(WaveFormat waveFormat)
        {
            MajorType = MediaTypes.MEDIATYPE_Audio;

            var wfe = waveFormat as WaveFormatExtensible;
            if (wfe != null)
                subtype = wfe.SubFormat;
            else
                switch (waveFormat.Encoding)
                {
                    case WaveFormatEncoding.Pcm:
                        subtype = AudioMediaSubtypes.MEDIASUBTYPE_PCM;
                        break;
                    case WaveFormatEncoding.IeeeFloat:
                        subtype = AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT;
                        break;
                    case WaveFormatEncoding.MpegLayer3:
                        subtype = AudioMediaSubtypes.WMMEDIASUBTYPE_MP3;
                        break;
                    default:
                        throw new ArgumentException($"Not a supported encoding {waveFormat.Encoding}");
                }

            FixedSizeSamples = SubType == AudioMediaSubtypes.MEDIASUBTYPE_PCM ||
                               SubType == AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT;
            FormatType = DmoMediaTypeGuids.FORMAT_WaveFormatEx;
            if (cbFormat < Marshal.SizeOf(waveFormat))
                throw new InvalidOperationException("Not enough memory assigned for a WaveFormat structure");
            //Debug.Assert(cbFormat >= ,"Not enough space");
            Marshal.StructureToPtr(waveFormat, pbFormat, false);
        }
    }
}