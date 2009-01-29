using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Security.Permissions;

namespace MSNPSharpClient
{
    
    public partial class TraceForm : Form
    {
        RichTextBoxTraceListener rtbTraceListener = null;
        public TraceForm()
        {
            InitializeComponent();
            rtbTraceListener = new RichTextBoxTraceListener(rtbTrace);
            Trace.Listeners.Add(rtbTraceListener);

            FormClosing += new FormClosingEventHandler(TraceForm_FormClosing);
        }

        void TraceForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Trace.Listeners.Remove(rtbTraceListener);
        }

        private void tsbClear_Click(object sender, EventArgs e)
        {
            rtbTrace.Clear();
        }

        private void tsbStop_Click(object sender, EventArgs e)
        {
            rtbTraceListener.Stop();
            tsbStart.Enabled = true;
            tsbStop.Enabled = false;
        }

        private void tsbStart_Click(object sender, EventArgs e)
        {
            rtbTraceListener.Resume();
            tsbStart.Enabled = false;
            tsbStop.Enabled = true;
        }
    }
    
    
    public class TraceWriter : TextWriter
    {
        private RichTextBox richTextBox = null;
        private delegate void WriteHandler(StringBuilder buffe, RichTextBox rtb);
        private int MaxBufferLen = 1024;
        private StringBuilder buffer = new StringBuilder();
        private DateTime lastInputTime = DateTime.Now;

        protected virtual void OutPut(StringBuilder buffer, RichTextBox rtb)
        {
            rtb.Text += buffer.ToString();
            rtb.Select(rtb.Text.Length, 0);
            rtb.ScrollToCaret();
        }

        public TraceWriter(RichTextBox outputRTB)
        {
            richTextBox = outputRTB;
        }

        public override void Write(char value)
        {
            if (richTextBox != null && buffer != null)
            {
                buffer.Append(value);
                if ((value == '\n' && buffer.Length >= MaxBufferLen) ||
                    buffer.Length == MaxBufferLen ||
                    (value == '\n' && (DateTime.Now - lastInputTime).Milliseconds >= 100))
                {
                    if (richTextBox.InvokeRequired)
                    {
                        StringBuilder copybuffer = new StringBuilder(buffer.ToString());
                        richTextBox.BeginInvoke(new WriteHandler(OutPut), new object[] { copybuffer, richTextBox });
                    }
                    else
                    {
                        OutPut(buffer, richTextBox);
                    }

                    buffer.Remove(0, buffer.Length);
                    lastInputTime = DateTime.Now;
                }
            }
        }

        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }
    }

    [HostProtection(SecurityAction.LinkDemand, Synchronization = true)]
    public class RichTextBoxTraceListener : TraceListener
    {
        // Fields
        private TraceWriter writer = null;
        private object syncObject = new object();
        private bool stop = false;

        // Methods
        public RichTextBoxTraceListener()
            : base()
        {
        }

        public RichTextBoxTraceListener(RichTextBox rtb)
            : base(string.Empty)
        {
            writer = new TraceWriter(rtb);
        }


        public override void Close()
        {
            if (!EnsureWriter()) return;

            if (this.writer != null)
            {
                this.writer.Close();
            }
            this.writer = null;
        }

        private bool EnsureWriter()
        {
            lock (syncObject)
            {
                if (writer == null || stop == true) return false;
                return true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Close();
            }
        }

        public override void Flush()
        {
            if (!EnsureWriter()) return;
            this.writer.Flush();
        }

        private static Encoding GetEncodingWithFallback(Encoding encoding)
        {
            Encoding encoding2 = (Encoding)encoding.Clone();
            encoding2.EncoderFallback = EncoderFallback.ReplacementFallback;
            encoding2.DecoderFallback = DecoderFallback.ReplacementFallback;
            return encoding2;
        }

        public override void Write(string message)
        {
            if (!EnsureWriter()) return;
            if (base.NeedIndent)
            {
                this.WriteIndent();
            }
            this.writer.Write(message);
        }

        public override void WriteLine(string message)
        {
            if (!EnsureWriter()) return;
            if (base.NeedIndent)
            {
                this.WriteIndent();
            }
            this.writer.WriteLine(message);
            base.NeedIndent = true;
        }

        public void Stop()
        {
            lock (syncObject)
            {
                stop = true;
            }
        }

        public void Resume()
        {
            lock (syncObject)
            {
                stop = false;
            }
        }

        // Properties
        public TraceWriter Writer
        {
            get
            {
                return this.writer;
            }
            set
            {
                this.writer = value;
            }
        }
    }

}