using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using NAudio.Wave;

namespace NAudio.Gui
{
    /// <summary>
    ///     Control for viewing waveforms
    /// </summary>
    public class WaveViewer : UserControl
    {
        private int bytesPerSample;

        /// <summary>
        ///     Required designer variable.
        /// </summary>
        private Container components;

        private int samplesPerPixel = 128;

        private WaveStream waveStream;

        /// <summary>
        ///     Creates a new WaveViewer control
        /// </summary>
        public WaveViewer()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();
            DoubleBuffered = true;
        }

        /// <summary>
        ///     sets the associated wavestream
        /// </summary>
        public WaveStream WaveStream
        {
            get => waveStream;
            set
            {
                waveStream = value;
                if (waveStream != null)
                    bytesPerSample = waveStream.WaveFormat.BitsPerSample / 8 * waveStream.WaveFormat.Channels;

                Invalidate();
            }
        }

        /// <summary>
        ///     The zoom level, in samples per pixel
        /// </summary>
        public int SamplesPerPixel
        {
            get => samplesPerPixel;
            set
            {
                samplesPerPixel = value;
                Invalidate();
            }
        }

        /// <summary>
        ///     Start position (currently in bytes)
        /// </summary>
        public long StartPosition { get; set; }

        /// <summary>
        ///     Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                if (components != null)
                    components.Dispose();

            base.Dispose(disposing);
        }

        /// <summary>
        ///     <see cref="Control.OnPaint" />
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (waveStream != null)
            {
                waveStream.Position = 0;
                int bytesRead;
                var waveData = new byte[samplesPerPixel * bytesPerSample];
                waveStream.Position = StartPosition + e.ClipRectangle.Left * bytesPerSample * samplesPerPixel;

                for (float x = e.ClipRectangle.X; x < e.ClipRectangle.Right; x += 1)
                {
                    short low = 0;
                    short high = 0;
                    bytesRead = waveStream.Read(waveData, 0, samplesPerPixel * bytesPerSample);
                    if (bytesRead == 0)
                        break;
                    for (var n = 0; n < bytesRead; n += 2)
                    {
                        var sample = BitConverter.ToInt16(waveData, n);
                        if (sample < low) low = sample;
                        if (sample > high) high = sample;
                    }

                    var lowPercent = ((float)low - short.MinValue) / ushort.MaxValue;
                    var highPercent = ((float)high - short.MinValue) / ushort.MaxValue;
                    e.Graphics.DrawLine(Pens.Black, x, Height * lowPercent, x, Height * highPercent);
                }
            }

            base.OnPaint(e);
        }


        #region Component Designer generated code

        /// <summary>
        ///     Required method for Designer support - do not modify
        ///     the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
        }

        #endregion
    }
}