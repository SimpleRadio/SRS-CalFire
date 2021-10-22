using System;
using System.IO;
using System.Text;

namespace NAudio.FileFormats.SoundFont
{
    /// <summary>
    ///     Instrument Builder
    /// </summary>
    internal class InstrumentBuilder : StructureBuilder<Instrument>
    {
        private Instrument lastInstrument;

        public override int Length => 22;

        public Instrument[] Instruments => data.ToArray();

        public override Instrument Read(BinaryReader br)
        {
            var i = new Instrument();
            var s = Encoding.UTF8.GetString(br.ReadBytes(20), 0, 20);
            if (s.IndexOf('\0') >= 0) s = s.Substring(0, s.IndexOf('\0'));

            i.Name = s;
            i.startInstrumentZoneIndex = br.ReadUInt16();
            if (lastInstrument != null)
                lastInstrument.endInstrumentZoneIndex = (ushort)(i.startInstrumentZoneIndex - 1);

            data.Add(i);
            lastInstrument = i;
            return i;
        }

        public override void Write(BinaryWriter bw, Instrument instrument)
        {
        }

        public void LoadZones(Zone[] zones)
        {
            // don't do the last preset, which is simply EOP
            for (var instrument = 0; instrument < data.Count - 1; instrument++)
            {
                var i = data[instrument];
                i.Zones = new Zone[i.endInstrumentZoneIndex - i.startInstrumentZoneIndex + 1];
                Array.Copy(zones, i.startInstrumentZoneIndex, i.Zones, 0, i.Zones.Length);
            }

            // we can get rid of the EOP record now
            data.RemoveAt(data.Count - 1);
        }
    }
}