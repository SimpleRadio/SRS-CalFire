namespace NAudio.FileFormats.SoundFont
{
    /// <summary>
    ///     Soundfont generator
    /// </summary>
    public class Generator
    {
        /// <summary>
        ///     Gets the generator type
        /// </summary>
        public GeneratorEnum GeneratorType { get; set; }

        /// <summary>
        ///     Generator amount as an unsigned short
        /// </summary>
        public ushort UInt16Amount { get; set; }

        /// <summary>
        ///     Generator amount as a signed short
        /// </summary>
        public short Int16Amount
        {
            get => (short)UInt16Amount;
            set => UInt16Amount = (ushort)value;
        }

        /// <summary>
        ///     Low byte amount
        /// </summary>
        public byte LowByteAmount
        {
            get => (byte)(UInt16Amount & 0x00FF);
            set
            {
                UInt16Amount &= 0xFF00;
                UInt16Amount += value;
            }
        }

        /// <summary>
        ///     High byte amount
        /// </summary>
        public byte HighByteAmount
        {
            get => (byte)((UInt16Amount & 0xFF00) >> 8);
            set
            {
                UInt16Amount &= 0x00FF;
                UInt16Amount += (ushort)(value << 8);
            }
        }

        /// <summary>
        ///     Instrument
        /// </summary>
        public Instrument Instrument { get; set; }

        /// <summary>
        ///     Sample Header
        /// </summary>
        public SampleHeader SampleHeader { get; set; }

        /// <summary>
        ///     <see cref="object.ToString" />
        /// </summary>
        public override string ToString()
        {
            if (GeneratorType == GeneratorEnum.Instrument)
                return string.Format("Generator Instrument {0}", Instrument.Name);
            if (GeneratorType == GeneratorEnum.SampleID)
                return string.Format("Generator SampleID {0}", SampleHeader);
            return string.Format("Generator {0} {1}", GeneratorType, UInt16Amount);
        }
    }
}