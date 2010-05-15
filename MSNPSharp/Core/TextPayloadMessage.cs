using System;
using System.Collections.Generic;
using System.Text;

namespace MSNPSharp.Core
{
    public class TextPayloadMessage : NetworkMessage
    {
        private string text = string.Empty;

        /// <summary>
        /// The payload text.
        /// </summary>
        public string Text
        {
            get 
            { 
                return text;
            }

            private set
            {
                text = value;
                InnerBody = Encoding.GetBytes(Text);
            }
        }

        private Encoding encoding = Encoding.UTF8;

        /// <summary>
        /// The encoding used when parsing the payload text.
        /// </summary>
        public Encoding Encoding
        {
            get 
            { 
                return encoding; 
            }

            private set
            {
                encoding = value;
                InnerBody = Encoding.GetBytes(Text);
            }
        }

        public TextPayloadMessage(string txt)
        {
            Text = txt;

        }

        public TextPayloadMessage(string txt, Encoding encode)
        {
            Text = txt;
            Encoding = encode;
        }

        public override byte[] GetBytes()
        {
            return Encoding.GetBytes(Text);
        }

        public override void ParseBytes(byte[] data)
        {
            Text = Encoding.GetString(data);
        }

        public override void PrepareMessage()
        {
            InnerBody = Encoding.GetBytes(Text);
        }

        public override string ToString()
        {
            return Text.Replace("\r", "\\r").Replace("\n", "\\n\n");
        }
    }
}
