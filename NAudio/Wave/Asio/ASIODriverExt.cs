using System;

namespace NAudio.Wave.Asio
{
    /// <summary>
    ///     Callback used by the AsioDriverExt to get wave data
    /// </summary>
    public delegate void AsioFillBufferCallback(IntPtr[] inputChannels, IntPtr[] outputChannels);

    /// <summary>
    ///     AsioDriverExt is a simplified version of the AsioDriver. It provides an easier
    ///     way to access the capabilities of the Driver and implement the callbacks necessary
    ///     for feeding the driver.
    ///     Implementation inspired from Rob Philpot's with a managed C++ ASIO wrapper BlueWave.Interop.Asio
    ///     http://www.codeproject.com/KB/mcpp/Asio.Net.aspx
    ///     Contributor: Alexandre Mutel - email: alexandre_mutel at yahoo.fr
    /// </summary>
    public class AsioDriverExt
    {
        private AsioBufferInfo[] bufferInfos;
        private int bufferSize;
        private AsioCallbacks callbacks;
        private IntPtr[] currentInputBuffers;
        private IntPtr[] currentOutputBuffers;
        private int inputChannelOffset;
        private bool isOutputReadySupported;
        private int numberOfInputChannels;
        private int numberOfOutputChannels;
        private int outputChannelOffset;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AsioDriverExt" /> class based on an already
        ///     instantiated AsioDriver instance.
        /// </summary>
        /// <param name="driver">A AsioDriver already instantiated.</param>
        public AsioDriverExt(AsioDriver driver)
        {
            this.Driver = driver;

            if (!driver.Init(IntPtr.Zero)) throw new InvalidOperationException(driver.GetErrorMessage());

            callbacks = new AsioCallbacks();
            callbacks.pasioMessage = AsioMessageCallBack;
            callbacks.pbufferSwitch = BufferSwitchCallBack;
            callbacks.pbufferSwitchTimeInfo = BufferSwitchTimeInfoCallBack;
            callbacks.psampleRateDidChange = SampleRateDidChangeCallBack;

            BuildCapabilities();
        }

        /// <summary>
        ///     Gets the driver used.
        /// </summary>
        /// <value>The ASIOdriver.</value>
        public AsioDriver Driver { get; }

        /// <summary>
        ///     Gets or sets the fill buffer callback.
        /// </summary>
        /// <value>The fill buffer callback.</value>
        public AsioFillBufferCallback FillBufferCallback { get; set; }

        /// <summary>
        ///     Gets the capabilities of the AsioDriver.
        /// </summary>
        /// <value>The capabilities.</value>
        public AsioDriverCapability Capabilities { get; private set; }

        /// <summary>
        ///     Allows adjustment of which is the first output channel we write to
        /// </summary>
        /// <param name="outputChannelOffset">Output Channel offset</param>
        /// <param name="inputChannelOffset">Input Channel offset</param>
        public void SetChannelOffset(int outputChannelOffset, int inputChannelOffset)
        {
            if (outputChannelOffset + numberOfOutputChannels <= Capabilities.NbOutputChannels)
                this.outputChannelOffset = outputChannelOffset;
            else
                throw new ArgumentException("Invalid channel offset");

            if (inputChannelOffset + numberOfInputChannels <= Capabilities.NbInputChannels)
                this.inputChannelOffset = inputChannelOffset;
            else
                throw new ArgumentException("Invalid channel offset");
        }

        /// <summary>
        ///     Starts playing the buffers.
        /// </summary>
        public void Start()
        {
            Driver.Start();
        }

        /// <summary>
        ///     Stops playing the buffers.
        /// </summary>
        public void Stop()
        {
            Driver.Stop();
        }

        /// <summary>
        ///     Shows the control panel.
        /// </summary>
        public void ShowControlPanel()
        {
            Driver.ControlPanel();
        }

        /// <summary>
        ///     Releases this instance.
        /// </summary>
        public void ReleaseDriver()
        {
            try
            {
                Driver.DisposeBuffers();
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.ToString());
            }

            Driver.ReleaseComAsioDriver();
        }

        /// <summary>
        ///     Determines whether the specified sample rate is supported.
        /// </summary>
        /// <param name="sampleRate">The sample rate.</param>
        /// <returns>
        ///     <c>true</c> if [is sample rate supported]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsSampleRateSupported(double sampleRate)
        {
            return Driver.CanSampleRate(sampleRate);
        }

        /// <summary>
        ///     Sets the sample rate.
        /// </summary>
        /// <param name="sampleRate">The sample rate.</param>
        public void SetSampleRate(double sampleRate)
        {
            Driver.SetSampleRate(sampleRate);
            // Update Capabilities
            BuildCapabilities();
        }

        /// <summary>
        ///     Creates the buffers for playing.
        /// </summary>
        /// <param name="numberOfOutputChannels">The number of outputs channels.</param>
        /// <param name="numberOfInputChannels">The number of input channel.</param>
        /// <param name="useMaxBufferSize">if set to <c>true</c> [use max buffer size] else use Prefered size</param>
        public int CreateBuffers(int numberOfOutputChannels, int numberOfInputChannels, bool useMaxBufferSize)
        {
            if (numberOfOutputChannels < 0 || numberOfOutputChannels > Capabilities.NbOutputChannels)
                throw new ArgumentException(
                    $"Invalid number of channels {numberOfOutputChannels}, must be in the range [0,{Capabilities.NbOutputChannels}]");

            if (numberOfInputChannels < 0 || numberOfInputChannels > Capabilities.NbInputChannels)
                throw new ArgumentException("numberOfInputChannels",
                    $"Invalid number of input channels {numberOfInputChannels}, must be in the range [0,{Capabilities.NbInputChannels}]");

            // each channel needs a buffer info
            this.numberOfOutputChannels = numberOfOutputChannels;
            this.numberOfInputChannels = numberOfInputChannels;
            // Ask for maximum of output channels even if we use only the nbOutputChannelsArg
            var nbTotalChannels = Capabilities.NbInputChannels + Capabilities.NbOutputChannels;
            bufferInfos = new AsioBufferInfo[nbTotalChannels];
            currentOutputBuffers = new IntPtr[numberOfOutputChannels];
            currentInputBuffers = new IntPtr[numberOfInputChannels];

            // and do the same for output channels
            // ONLY work on output channels (just put isInput = true for InputChannel)
            var totalIndex = 0;
            for (var index = 0; index < Capabilities.NbInputChannels; index++, totalIndex++)
            {
                bufferInfos[totalIndex].isInput = true;
                bufferInfos[totalIndex].channelNum = index;
                bufferInfos[totalIndex].pBuffer0 = IntPtr.Zero;
                bufferInfos[totalIndex].pBuffer1 = IntPtr.Zero;
            }

            for (var index = 0; index < Capabilities.NbOutputChannels; index++, totalIndex++)
            {
                bufferInfos[totalIndex].isInput = false;
                bufferInfos[totalIndex].channelNum = index;
                bufferInfos[totalIndex].pBuffer0 = IntPtr.Zero;
                bufferInfos[totalIndex].pBuffer1 = IntPtr.Zero;
            }

            if (useMaxBufferSize)
                // use the drivers maximum buffer size
                bufferSize = Capabilities.BufferMaxSize;
            else
                // use the drivers preferred buffer size
                bufferSize = Capabilities.BufferPreferredSize;

            unsafe
            {
                fixed (AsioBufferInfo* infos = &bufferInfos[0])
                {
                    var pOutputBufferInfos = new IntPtr(infos);

                    // Create the ASIO Buffers with the callbacks
                    Driver.CreateBuffers(pOutputBufferInfos, nbTotalChannels, bufferSize, ref callbacks);
                }
            }

            // Check if outputReady is supported
            isOutputReadySupported = Driver.OutputReady() == AsioError.ASE_OK;
            return bufferSize;
        }

        /// <summary>
        ///     Builds the capabilities internally.
        /// </summary>
        private void BuildCapabilities()
        {
            Capabilities = new AsioDriverCapability();

            Capabilities.DriverName = Driver.GetDriverName();

            // Get nb Input/Output channels
            Driver.GetChannels(out Capabilities.NbInputChannels, out Capabilities.NbOutputChannels);

            Capabilities.InputChannelInfos = new AsioChannelInfo[Capabilities.NbInputChannels];
            Capabilities.OutputChannelInfos = new AsioChannelInfo[Capabilities.NbOutputChannels];

            // Get ChannelInfo for Inputs
            for (var i = 0; i < Capabilities.NbInputChannels; i++)
                Capabilities.InputChannelInfos[i] = Driver.GetChannelInfo(i, true);

            // Get ChannelInfo for Output
            for (var i = 0; i < Capabilities.NbOutputChannels; i++)
                Capabilities.OutputChannelInfos[i] = Driver.GetChannelInfo(i, false);

            // Get the current SampleRate
            Capabilities.SampleRate = Driver.GetSampleRate();

            var error = Driver.GetLatencies(out Capabilities.InputLatency, out Capabilities.OutputLatency);
            // focusrite scarlett 2i4 returns ASE_NotPresent here

            if (error != AsioError.ASE_OK && error != AsioError.ASE_NotPresent)
            {
                var ex = new AsioException("ASIOgetLatencies");
                ex.Error = error;
                throw ex;
            }

            // Get BufferSize
            Driver.GetBufferSize(out Capabilities.BufferMinSize, out Capabilities.BufferMaxSize,
                out Capabilities.BufferPreferredSize, out Capabilities.BufferGranularity);
        }

        /// <summary>
        ///     Callback called by the AsioDriver on fill buffer demand. Redirect call to external callback.
        /// </summary>
        /// <param name="doubleBufferIndex">Index of the double buffer.</param>
        /// <param name="directProcess">if set to <c>true</c> [direct process].</param>
        private void BufferSwitchCallBack(int doubleBufferIndex, bool directProcess)
        {
            for (var i = 0; i < numberOfInputChannels; i++)
                currentInputBuffers[i] = bufferInfos[i + inputChannelOffset].Buffer(doubleBufferIndex);

            for (var i = 0; i < numberOfOutputChannels; i++)
                currentOutputBuffers[i] = bufferInfos[i + outputChannelOffset + Capabilities.NbInputChannels]
                    .Buffer(doubleBufferIndex);

            FillBufferCallback?.Invoke(currentInputBuffers, currentOutputBuffers);

            if (isOutputReadySupported) Driver.OutputReady();
        }

        /// <summary>
        ///     Callback called by the AsioDriver on event "Samples rate changed".
        /// </summary>
        /// <param name="sRate">The sample rate.</param>
        private void SampleRateDidChangeCallBack(double sRate)
        {
            // Check when this is called?
            Capabilities.SampleRate = sRate;
        }

        /// <summary>
        ///     Asio message call back.
        /// </summary>
        /// <param name="selector">The selector.</param>
        /// <param name="value">The value.</param>
        /// <param name="message">The message.</param>
        /// <param name="opt">The opt.</param>
        /// <returns></returns>
        private int AsioMessageCallBack(AsioMessageSelector selector, int value, IntPtr message, IntPtr opt)
        {
            // Check when this is called?
            switch (selector)
            {
                case AsioMessageSelector.kAsioSelectorSupported:
                    var subValue =
                        (AsioMessageSelector)Enum.ToObject(typeof(AsioMessageSelector), value);
                    switch (subValue)
                    {
                        case AsioMessageSelector.kAsioEngineVersion:
                            return 1;
                        case AsioMessageSelector.kAsioResetRequest:
                            return 0;
                        case AsioMessageSelector.kAsioBufferSizeChange:
                            return 0;
                        case AsioMessageSelector.kAsioResyncRequest:
                            return 0;
                        case AsioMessageSelector.kAsioLatenciesChanged:
                            return 0;
                        case AsioMessageSelector.kAsioSupportsTimeInfo:
//                            return 1; DON'T SUPPORT FOR NOW. NEED MORE TESTING.
                            return 0;
                        case AsioMessageSelector.kAsioSupportsTimeCode:
//                            return 1; DON'T SUPPORT FOR NOW. NEED MORE TESTING.
                            return 0;
                    }

                    break;
                case AsioMessageSelector.kAsioEngineVersion:
                    return 2;
                case AsioMessageSelector.kAsioResetRequest:
                    return 1;
                case AsioMessageSelector.kAsioBufferSizeChange:
                    return 0;
                case AsioMessageSelector.kAsioResyncRequest:
                    return 0;
                case AsioMessageSelector.kAsioLatenciesChanged:
                    return 0;
                case AsioMessageSelector.kAsioSupportsTimeInfo:
                    return 0;
                case AsioMessageSelector.kAsioSupportsTimeCode:
                    return 0;
            }

            return 0;
        }

        /// <summary>
        ///     Buffers switch time info call back.
        /// </summary>
        /// <param name="asioTimeParam">The asio time param.</param>
        /// <param name="doubleBufferIndex">Index of the double buffer.</param>
        /// <param name="directProcess">if set to <c>true</c> [direct process].</param>
        /// <returns></returns>
        private IntPtr BufferSwitchTimeInfoCallBack(IntPtr asioTimeParam, int doubleBufferIndex, bool directProcess)
        {
            // Check when this is called?
            return IntPtr.Zero;
        }
    }
}