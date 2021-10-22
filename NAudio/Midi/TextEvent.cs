using System.IO;
using System.Text;
using NAudio.Utils;

namespace NAudio.Midi
{
    /// <summary>
    ///     Represents a MIDI text event
    /// </summary>
    public class TextEvent : MetaEvent
    {
        private byte[] data;

        /// <summary>
        ///     Reads a new text event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream</param>
        /// <param name="length">The data length</param>
        public TextEvent(BinaryReader br, int length)
        {
            data = br.ReadBytes(length);
        }

        /// <summary>
        ///     Creates a new TextEvent
        /// </summary>
        /// <param name="text">The text in this type</param>
        /// <param name="metaEventType">
        ///     MetaEvent type (must be one that is
        ///     associated with text data)
        /// </param>
        /// <param name="absoluteTime">Absolute time of this event</param>
        public TextEvent(string text, MetaEventType metaEventType, long absoluteTime)
            : base(metaEventType, text.Length, absoluteTime)
        {
            Text = text;
        }

        /// <summary>
        ///     The contents of this text event
        /// </summary>
        public string Text
        {
            get
            {
                Encoding byteEncoding = ByteEncoding.Instance;
                return byteEncoding.GetString(data);
            }
            set
            {
                Encoding byteEncoding = ByteEncoding.Instance;
                data = byteEncoding.GetBytes(value);
                metaDataLength = data.Length;
            }
        }

        /// <summary>
        ///     The raw contents of this text event
        /// </summary>
        public byte[] Data
        {
            get => data;
            set
            {
                data = value;
                metaDataLength = data.Length;
            }
        }

        /// <summary>
        ///     Creates a deep clone of this MIDI event.
        /// </summary>
        public override MidiEvent Clone()
        {
            return (TextEvent)MemberwiseClone();
        }

        /// <summary>
        ///     Describes this MIDI text event
        /// </summary>
        /// <returns>A string describing this event</returns>
        public override string ToString()
        {
            return string.Format("{0} {1}", base.ToString(), Text);
        }

        /// <summary>
        ///     Calls base class export first, then exports the data
        ///     specific to this event
        ///     <seealso cref="MidiEvent.Export">MidiEvent.Export</seealso>
        /// </summary>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write(data);
        }
    }
}