namespace NAudio.FileFormats.SoundFont
{
    /// <summary>
    ///     SoundFont instrument
    /// </summary>
    public class Instrument
    {
        internal ushort endInstrumentZoneIndex;
        internal ushort startInstrumentZoneIndex;

        /// <summary>
        ///     instrument name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Zones
        /// </summary>
        public Zone[] Zones { get; set; }

        /// <summary>
        ///     <see cref="object.ToString" />
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }
}