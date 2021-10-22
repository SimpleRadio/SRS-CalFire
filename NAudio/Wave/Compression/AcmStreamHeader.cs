using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NAudio.Wave.MmeInterop;

namespace NAudio.Wave.Compression
{
    internal class AcmStreamHeader : IDisposable
    {
        private bool firstTime;
        private GCHandle hDestBuffer;
        private GCHandle hSourceBuffer;
        private readonly IntPtr streamHandle;
        private readonly AcmStreamHeaderStruct streamHeader;

        public AcmStreamHeader(IntPtr streamHandle, int sourceBufferLength, int destBufferLength)
        {
            streamHeader = new AcmStreamHeaderStruct();
            SourceBuffer = new byte[sourceBufferLength];
            hSourceBuffer = GCHandle.Alloc(SourceBuffer, GCHandleType.Pinned);

            DestBuffer = new byte[destBufferLength];
            hDestBuffer = GCHandle.Alloc(DestBuffer, GCHandleType.Pinned);

            this.streamHandle = streamHandle;
            firstTime = true;
            //Prepare();
        }

        public byte[] SourceBuffer { get; private set; }

        public byte[] DestBuffer { get; private set; }

        private void Prepare()
        {
            streamHeader.cbStruct = Marshal.SizeOf(streamHeader);
            streamHeader.sourceBufferLength = SourceBuffer.Length;
            streamHeader.sourceBufferPointer = hSourceBuffer.AddrOfPinnedObject();
            streamHeader.destBufferLength = DestBuffer.Length;
            streamHeader.destBufferPointer = hDestBuffer.AddrOfPinnedObject();
            MmException.Try(AcmInterop.acmStreamPrepareHeader(streamHandle, streamHeader, 0), "acmStreamPrepareHeader");
        }

        private void Unprepare()
        {
            streamHeader.sourceBufferLength = SourceBuffer.Length;
            streamHeader.sourceBufferPointer = hSourceBuffer.AddrOfPinnedObject();
            streamHeader.destBufferLength = DestBuffer.Length;
            streamHeader.destBufferPointer = hDestBuffer.AddrOfPinnedObject();

            var result = AcmInterop.acmStreamUnprepareHeader(streamHandle, streamHeader, 0);
            if (result != MmResult.NoError)
                //if (result == MmResult.AcmHeaderUnprepared)
                throw new MmException(result, "acmStreamUnprepareHeader");
        }

        public void Reposition()
        {
            firstTime = true;
        }

        public int Convert(int bytesToConvert, out int sourceBytesConverted)
        {
            Prepare();
            try
            {
                streamHeader.sourceBufferLength = bytesToConvert;
                streamHeader.sourceBufferLengthUsed = bytesToConvert;
                var flags = firstTime
                    ? AcmStreamConvertFlags.Start | AcmStreamConvertFlags.BlockAlign
                    : AcmStreamConvertFlags.BlockAlign;
                MmException.Try(AcmInterop.acmStreamConvert(streamHandle, streamHeader, flags), "acmStreamConvert");
                firstTime = false;
                Debug.Assert(streamHeader.destBufferLength == DestBuffer.Length,
                    "Codecs should not change dest buffer length");
                sourceBytesConverted = streamHeader.sourceBufferLengthUsed;
            }
            finally
            {
                Unprepare();
            }

            return streamHeader.destBufferLengthUsed;
        }

        #region IDisposable Members

        private bool disposed;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                //Unprepare();
                SourceBuffer = null;
                DestBuffer = null;
                hSourceBuffer.Free();
                hDestBuffer.Free();
            }

            disposed = true;
        }

        ~AcmStreamHeader()
        {
            Debug.Assert(false, "AcmStreamHeader dispose was not called");
            Dispose(false);
        }

        #endregion
    }
}