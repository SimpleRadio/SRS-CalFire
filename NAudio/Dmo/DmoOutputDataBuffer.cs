using System;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    /// <summary>
    ///     DMO Output Data Buffer
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct DmoOutputDataBuffer : IDisposable
    {
        /// <summary>
        ///     Creates a new DMO Output Data Buffer structure
        /// </summary>
        /// <param name="maxBufferSize">Maximum buffer size</param>
        public DmoOutputDataBuffer(int maxBufferSize)
        {
            MediaBuffer = new MediaBuffer(maxBufferSize);
            StatusFlags = DmoOutputDataBufferFlags.None;
            Timestamp = 0;
            Duration = 0;
        }

        /// <summary>
        ///     Dispose
        /// </summary>
        public void Dispose()
        {
            if (MediaBuffer != null)
            {
                ((MediaBuffer)MediaBuffer).Dispose();
                MediaBuffer = null;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        ///     Media Buffer
        /// </summary>
        [field: MarshalAs(UnmanagedType.Interface)]
        public IMediaBuffer MediaBuffer { get; internal set; }

        /// <summary>
        ///     Length of data in buffer
        /// </summary>
        public int Length => ((MediaBuffer)MediaBuffer).Length;

        /// <summary>
        ///     Status Flags
        /// </summary>
        public DmoOutputDataBufferFlags StatusFlags { get; set; }

        /// <summary>
        ///     Timestamp
        /// </summary>
        public long Timestamp { get; internal set; }

        /// <summary>
        ///     Duration
        /// </summary>
        public long Duration { get; internal set; }

        /// <summary>
        ///     Retrives the data in this buffer
        /// </summary>
        /// <param name="data">Buffer to receive data</param>
        /// <param name="offset">Offset into buffer</param>
        public void RetrieveData(byte[] data, int offset)
        {
            ((MediaBuffer)MediaBuffer).RetrieveData(data, offset);
        }

        /// <summary>
        ///     Is more data available
        ///     If true, ProcessOuput should be called again
        /// </summary>
        public bool MoreDataAvailable =>
            (StatusFlags & DmoOutputDataBufferFlags.Incomplete) == DmoOutputDataBufferFlags.Incomplete;
    }
}