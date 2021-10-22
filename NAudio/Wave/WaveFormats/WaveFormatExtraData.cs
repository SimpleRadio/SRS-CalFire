using System.IO;
using System.Runtime.InteropServices;
using NAudio.Wave.WaveFormats;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    ///     This class used for marshalling from unmanaged code
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
    public class WaveFormatExtraData : WaveFormat
    {
        // try with 100 bytes for now, increase if necessary

        /// <summary>
        ///     parameterless constructor for marshalling
        /// </summary>
        internal WaveFormatExtraData()
        {
        }

        /// <summary>
        ///     Reads this structure from a BinaryReader
        /// </summary>
        public WaveFormatExtraData(BinaryReader reader)
            : base(reader)
        {
            ReadExtraData(reader);
        }

        /// <summary>
        ///     Allows the extra data to be read
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public byte[] ExtraData { get; } = new byte[100];

        internal void ReadExtraData(BinaryReader reader)
        {
            if (extraSize > 0) reader.Read(ExtraData, 0, extraSize);
        }

        /// <summary>
        ///     Writes this structure to a BinaryWriter
        /// </summary>
        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            if (extraSize > 0) writer.Write(ExtraData, 0, extraSize);
        }
    }
}