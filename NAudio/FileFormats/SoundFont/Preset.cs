namespace NAudio.FileFormats.SoundFont
{
    /// <summary>
    ///     A SoundFont Preset
    /// </summary>
    public class Preset
    {
        internal ushort endPresetZoneIndex;
        internal uint genre;
        internal uint library;
        internal uint morphology;
        internal ushort startPresetZoneIndex;

        /// <summary>
        ///     Preset name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Patch Number
        /// </summary>
        public ushort PatchNumber { get; set; }

        /// <summary>
        ///     Bank number
        /// </summary>
        public ushort Bank { get; set; }

        /// <summary>
        ///     Zones
        /// </summary>
        public Zone[] Zones { get; set; }

        /// <summary>
        ///     <see cref="object.ToString" />
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}-{1} {2}", Bank, PatchNumber, Name);
        }
    }
}