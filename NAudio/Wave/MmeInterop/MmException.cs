using System;

namespace NAudio.Wave.MmeInterop
{
    /// <summary>
    ///     Summary description for MmException.
    /// </summary>
    public class MmException : Exception
    {
        private string function;

        /// <summary>
        ///     Creates a new MmException
        /// </summary>
        /// <param name="result">The result returned by the Windows API call</param>
        /// <param name="function">The name of the Windows API that failed</param>
        public MmException(MmResult result, string function)
            : base(ErrorMessage(result, function))
        {
            Result = result;
            this.function = function;
        }

        /// <summary>
        ///     Returns the Windows API result
        /// </summary>
        public MmResult Result { get; }


        private static string ErrorMessage(MmResult result, string function)
        {
            return string.Format("{0} calling {1}", result, function);
        }

        /// <summary>
        ///     Helper function to automatically raise an exception on failure
        /// </summary>
        /// <param name="result">The result of the API call</param>
        /// <param name="function">The API function name</param>
        public static void Try(MmResult result, string function)
        {
            if (result != MmResult.NoError)
                throw new MmException(result, function);
        }
    }
}