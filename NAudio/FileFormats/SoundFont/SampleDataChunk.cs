using System.IO;

namespace NAudio.FileFormats.SoundFont
{
    internal class SampleDataChunk
    {
        public SampleDataChunk(RiffChunk chunk)
        {
            var header = chunk.ReadChunkID();
            if (header != "sdta")
                throw new InvalidDataException(string.Format("Not a sample data chunk ({0})", header));

            SampleData = chunk.GetData();
        }

        public byte[] SampleData { get; }
    }
} // end of namespace