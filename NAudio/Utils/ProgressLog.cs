using System;
using System.Drawing;
using System.Windows.Forms;

namespace NAudio.Utils
{
    /// <summary>
    ///     A thread-safe Progress Log Control
    /// </summary>
    public partial class ProgressLog : UserControl
    {
        /// <summary>
        ///     Creates a new progress log control
        /// </summary>
        public ProgressLog()
        {
            InitializeComponent();
        }


        /// <summary>
        ///     The contents of the log as text
        /// </summary>
        public new string Text => richTextBoxLog.Text;

        /// <summary>
        ///     Log a message
        /// </summary>
        public void LogMessage(Color color, string message)
        {
            if (richTextBoxLog.InvokeRequired)
            {
                Invoke(new LogMessageDelegate(LogMessage), color, message);
            }
            else
            {
                richTextBoxLog.SelectionStart = richTextBoxLog.TextLength;
                richTextBoxLog.SelectionColor = color;
                richTextBoxLog.AppendText(message);
                richTextBoxLog.AppendText(Environment.NewLine);
            }
        }

        /// <summary>
        ///     Clear the log
        /// </summary>
        public void ClearLog()
        {
            if (richTextBoxLog.InvokeRequired)
                Invoke(new ClearLogDelegate(ClearLog), new object[] { });
            else
                richTextBoxLog.Clear();
        }


        private delegate void LogMessageDelegate(Color color, string message);

        private delegate void ClearLogDelegate();
    }
}