using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NAudio.Gui
{
    /// <summary>
    ///     Summary description for Fader.
    /// </summary>
    public class Fader : Control
    {
        private readonly int SliderHeight = 30;
        private readonly int SliderWidth = 15;

        /// <summary>
        ///     Required designer variable.
        /// </summary>
        private Container components;

        private bool dragging;
        private int dragY;
        private float percent;
        private Rectangle sliderRectangle;

        /// <summary>
        ///     Creates a new Fader control
        /// </summary>
        public Fader()
        {
            // Required for Windows.Forms Class Composition Designer support
            InitializeComponent();

            SetStyle(ControlStyles.DoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
        }


        /// <summary>
        ///     Minimum value of this fader
        /// </summary>
        public int Minimum { get; set; }

        /// <summary>
        ///     Maximum value of this fader
        /// </summary>
        public int Maximum { get; set; }

        /// <summary>
        ///     Current value of this fader
        /// </summary>
        public int Value
        {
            get => (int)(percent * (Maximum - Minimum)) + Minimum;
            set => percent = (float)(value - Minimum) / (Maximum - Minimum);
        }

        /// <summary>
        ///     Fader orientation
        /// </summary>
        public Orientation Orientation { get; set; }

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

        private void DrawSlider(Graphics g)
        {
            Brush block = new SolidBrush(Color.White);
            var centreLine = new Pen(Color.Black);
            sliderRectangle.X = (Width - SliderWidth) / 2;
            sliderRectangle.Width = SliderWidth;
            sliderRectangle.Y = (int)((Height - SliderHeight) * percent);
            sliderRectangle.Height = SliderHeight;

            g.FillRectangle(block, sliderRectangle);
            g.DrawLine(centreLine, sliderRectangle.Left, sliderRectangle.Top + sliderRectangle.Height / 2,
                sliderRectangle.Right, sliderRectangle.Top + sliderRectangle.Height / 2);
            block.Dispose();
            centreLine.Dispose();

            /*sliderRectangle.X = (this.Width - SliderWidth) / 2;
            sliderRectangle.Width = SliderWidth;
            sliderRectangle.Y = (int)((this.Height - SliderHeight) * percent);
            sliderRectangle.Height = SliderHeight;
            g.DrawImage(Images.Fader1,sliderRectangle);*/
        }


        /// <summary>
        ///     <see cref="Control.OnPaint" />
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (Orientation == Orientation.Vertical)
            {
                Brush groove = new SolidBrush(Color.Black);
                g.FillRectangle(groove, Width / 2, SliderHeight / 2, 2, Height - SliderHeight);
                groove.Dispose();
                DrawSlider(g);
            }

            base.OnPaint(e);
        }

        /// <summary>
        ///     <see cref="Control.OnMouseDown" />
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (sliderRectangle.Contains(e.X, e.Y))
            {
                dragging = true;
                dragY = e.Y - sliderRectangle.Y;
            }

            // TODO: are we over the fader
            base.OnMouseDown(e);
        }

        /// <summary>
        ///     <see cref="Control.OnMouseMove" />
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                var sliderTop = e.Y - dragY;
                if (sliderTop < 0)
                    percent = 0;
                else if (sliderTop > Height - SliderHeight)
                    percent = 1;
                else
                    percent = sliderTop / (float)(Height - SliderHeight);

                Invalidate();
            }

            base.OnMouseMove(e);
        }

        /// <summary>
        ///     <see cref="Control.OnMouseUp" />
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            base.OnMouseUp(e);
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