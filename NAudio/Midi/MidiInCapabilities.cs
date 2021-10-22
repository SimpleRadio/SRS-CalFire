using System.Runtime.InteropServices;
using NAudio.Wave.MmeInterop;

namespace NAudio.Midi
{
    /// <summary>
    ///     MIDI In Device Capabilities
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MidiInCapabilities
    {
        /// <summary>
        ///     wMid
        /// </summary>
        private readonly ushort manufacturerId;

        /// <summary>
        ///     wPid
        /// </summary>
        private readonly ushort productId;

        /// <summary>
        ///     vDriverVersion
        /// </summary>
        private readonly uint driverVersion;

        /// <summary>
        ///     Product Name
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxProductNameLength)]
        private readonly string productName;

        /// <summary>
        ///     Support - Reserved
        /// </summary>
        private readonly int support;

        private const int MaxProductNameLength = 32;

        /// <summary>
        ///     Gets the manufacturer of this device
        /// </summary>
        public Manufacturers Manufacturer => (Manufacturers)manufacturerId;

        /// <summary>
        ///     Gets the product identifier (manufacturer specific)
        /// </summary>
        public int ProductId => productId;

        /// <summary>
        ///     Gets the product name
        /// </summary>
        public string ProductName => productName;
    }
}