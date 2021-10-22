﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.WaveFormats;

namespace NAudio.FileFormats.Wav
{
    internal class WaveFileChunkReader
    {
        private readonly bool storeAllChunks;
        private readonly bool strictMode;
        private bool isRf64;
        private long riffSize;

        public WaveFileChunkReader()
        {
            storeAllChunks = true;
            strictMode = false;
        }

        /// <summary>
        ///     WaveFormat
        /// </summary>
        public WaveFormat WaveFormat { get; private set; }

        /// <summary>
        ///     Data Chunk Position
        /// </summary>
        public long DataChunkPosition { get; private set; }

        /// <summary>
        ///     Data Chunk Length
        /// </summary>
        public long DataChunkLength { get; private set; }

        /// <summary>
        ///     Riff Chunks
        /// </summary>
        public List<RiffChunk> RiffChunks { get; private set; }

        public void ReadWaveHeader(Stream stream)
        {
            DataChunkPosition = -1;
            WaveFormat = null;
            RiffChunks = new List<RiffChunk>();
            DataChunkLength = 0;

            var br = new BinaryReader(stream);
            ReadRiffHeader(br);
            riffSize = br.ReadUInt32(); // read the file size (minus 8 bytes)

            if (br.ReadInt32() != ChunkIdentifier.ChunkIdentifierToInt32("WAVE"))
                throw new FormatException("Not a WAVE file - no WAVE header");

            if (isRf64) ReadDs64Chunk(br);

            var dataChunkId = ChunkIdentifier.ChunkIdentifierToInt32("data");
            var formatChunkId = ChunkIdentifier.ChunkIdentifierToInt32("fmt ");

            // sometimes a file has more data than is specified after the RIFF header
            var stopPosition = Math.Min(riffSize + 8, stream.Length);

            // this -8 is so we can be sure that there are at least 8 bytes for a chunk id and length
            while (stream.Position <= stopPosition - 8)
            {
                var chunkIdentifier = br.ReadInt32();
                var chunkLength = br.ReadUInt32();
                if (chunkIdentifier == dataChunkId)
                {
                    DataChunkPosition = stream.Position;
                    if (!isRf64) // we already know the dataChunkLength if this is an RF64 file
                        DataChunkLength = chunkLength;

                    stream.Position += chunkLength;
                }
                else if (chunkIdentifier == formatChunkId)
                {
                    if (chunkLength > int.MaxValue)
                        throw new InvalidDataException(string.Format("Format chunk length must be between 0 and {0}.",
                            int.MaxValue));
                    WaveFormat = WaveFormat.FromFormatChunk(br, (int)chunkLength);
                }
                else
                {
                    // check for invalid chunk length
                    if (chunkLength > stream.Length - stream.Position)
                    {
                        if (strictMode)
                            Debug.Assert(false, string.Format("Invalid chunk length {0}, pos: {1}. length: {2}",
                                chunkLength, stream.Position, stream.Length));

                        // an exception will be thrown further down if we haven't got a format and data chunk yet,
                        // otherwise we will tolerate this file despite it having corrupt data at the end
                        break;
                    }

                    if (storeAllChunks)
                    {
                        if (chunkLength > int.MaxValue)
                            throw new InvalidDataException(
                                string.Format("RiffChunk chunk length must be between 0 and {0}.", int.MaxValue));
                        RiffChunks.Add(GetRiffChunk(stream, chunkIdentifier, (int)chunkLength));
                    }

                    stream.Position += chunkLength;
                }
            }

            if (WaveFormat == null) throw new FormatException("Invalid WAV file - No fmt chunk found");

            if (DataChunkPosition == -1) throw new FormatException("Invalid WAV file - No data chunk found");
        }

        /// <summary>
        ///     http://tech.ebu.ch/docs/tech/tech3306-2009.pdf
        /// </summary>
        private void ReadDs64Chunk(BinaryReader reader)
        {
            var ds64ChunkId = ChunkIdentifier.ChunkIdentifierToInt32("ds64");
            var chunkId = reader.ReadInt32();
            if (chunkId != ds64ChunkId) throw new FormatException("Invalid RF64 WAV file - No ds64 chunk found");

            var chunkSize = reader.ReadInt32();
            riffSize = reader.ReadInt64();
            DataChunkLength = reader.ReadInt64();
            var sampleCount = reader.ReadInt64(); // replaces the value in the fact chunk
            reader.ReadBytes(chunkSize - 24); // get to the end of this chunk (should parse extra stuff later)
        }

        private static RiffChunk GetRiffChunk(Stream stream, int chunkIdentifier, int chunkLength)
        {
            return new RiffChunk(chunkIdentifier, chunkLength, stream.Position);
        }

        private void ReadRiffHeader(BinaryReader br)
        {
            var header = br.ReadInt32();
            if (header == ChunkIdentifier.ChunkIdentifierToInt32("RF64"))
                isRf64 = true;
            else if (header != ChunkIdentifier.ChunkIdentifierToInt32("RIFF"))
                throw new FormatException("Not a WAVE file - no RIFF header");
        }
    }
}