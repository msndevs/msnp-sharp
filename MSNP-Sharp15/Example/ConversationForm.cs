using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using MSNPSharp;
using System.Collections.Generic;
using System.IO;
using System.Drawing.Imaging;

namespace MSNPSharpClient
{
    /// <summary>
    /// Summary description for ConversationForm.
    /// </summary>
    public class ConversationForm : System.Windows.Forms.Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox inputTextBox;
        private System.Windows.Forms.Button sendButton;
        private Button sendnudgeButton;
        private Panel panel2;


        /// <summary>
        /// </summary>
        private Conversation _conversation = null;

        private ClientForm _clientform = null;
        private List<string> _contacts = new List<string>(0);

        private RichTextBox richTextHistory;
        private Button emotionTestButton;


        /// <summary>
        /// The conversation object which is associated with the form.
        /// </summary>
        public Conversation Conversation
        {
            get
            {
                return _conversation;
            }

        }

        public List<string> Contacts
        {
            get { return _contacts; }
        }

        protected ConversationForm()
        {
        }

        public ConversationForm(Conversation conversation, ClientForm clientform, Contact contact)
        {
            InitializeComponent();
            _conversation = conversation;
            AddEvent();

            if (contact != null)
            {
                _conversation.Invite(contact);
            }

            _clientform = clientform;



        }

        /// <summary>
        /// Attatch a new conversation to this chatting form
        /// </summary>
        /// <param name="convers"></param>
        public void AttachConversation(Conversation convers)
        {
            _conversation.End();
            _conversation = convers;   //Use the latest conversation, WLM just do the same.
            AddEvent();
        }

        public bool CanAttach(Conversation newconvers)
        {
            if ((_conversation.Type & ConversationType.Chat) == ConversationType.Chat)
            {
                if((newconvers.Type | ConversationType.Chat) == _conversation.Type)
                {
                    if (Conversation.Contacts.Count == newconvers.Contacts.Count)
                    {
                        foreach (Contact ct in newconvers.Contacts)
                        {
                            if (!Conversation.Contacts.Contains(ct))
                                return false;
                        }

                        return true;
                    }
                }
            }
            return false;
        }

        void Switchboard_NudgeReceived(object sender, ContactEventArgs e)
        {
            if (Visible == false)
            {
                Invoke(new EventHandler<EventArgs>(MakeVisible), sender, e);
            }

            Invoke(new EventHandler<ContactEventArgs>(PrintNudge), sender, e);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.sendnudgeButton = new System.Windows.Forms.Button();
            this.sendButton = new System.Windows.Forms.Button();
            this.inputTextBox = new System.Windows.Forms.TextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.richTextHistory = new System.Windows.Forms.RichTextBox();
            this.emotionTestButton = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.panel1.Controls.Add(this.emotionTestButton);
            this.panel1.Controls.Add(this.sendnudgeButton);
            this.panel1.Controls.Add(this.sendButton);
            this.panel1.Controls.Add(this.inputTextBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 289);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(573, 106);
            this.panel1.TabIndex = 1;
            // 
            // sendnudgeButton
            // 
            this.sendnudgeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.sendnudgeButton.Location = new System.Drawing.Point(471, 41);
            this.sendnudgeButton.Name = "sendnudgeButton";
            this.sendnudgeButton.Size = new System.Drawing.Size(90, 25);
            this.sendnudgeButton.TabIndex = 2;
            this.sendnudgeButton.Text = "Send &Nudge";
            this.sendnudgeButton.Click += new System.EventHandler(this.SendNudge);
            // 
            // sendButton
            // 
            this.sendButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.sendButton.Location = new System.Drawing.Point(471, 9);
            this.sendButton.Name = "sendButton";
            this.sendButton.Size = new System.Drawing.Size(90, 25);
            this.sendButton.TabIndex = 1;
            this.sendButton.Text = "&Send";
            this.sendButton.Click += new System.EventHandler(this.sendButton_Click);
            // 
            // inputTextBox
            // 
            this.inputTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.inputTextBox.Location = new System.Drawing.Point(10, 9);
            this.inputTextBox.Multiline = true;
            this.inputTextBox.Name = "inputTextBox";
            this.inputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.inputTextBox.Size = new System.Drawing.Size(431, 88);
            this.inputTextBox.TabIndex = 0;
            this.inputTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.inputTextBox_KeyDown);
            this.inputTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.inputTextBox_KeyPress);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.richTextHistory);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(573, 289);
            this.panel2.TabIndex = 2;
            // 
            // richTextHistory
            // 
            this.richTextHistory.BackColor = System.Drawing.Color.White;
            this.richTextHistory.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextHistory.Location = new System.Drawing.Point(0, 0);
            this.richTextHistory.Name = "richTextHistory";
            this.richTextHistory.ReadOnly = true;
            this.richTextHistory.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.richTextHistory.Size = new System.Drawing.Size(573, 289);
            this.richTextHistory.TabIndex = 1;
            this.richTextHistory.TabStop = false;
            this.richTextHistory.Text = "";
            // 
            // emotionTestButton
            // 
            this.emotionTestButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.emotionTestButton.Location = new System.Drawing.Point(471, 72);
            this.emotionTestButton.Name = "emotionTestButton";
            this.emotionTestButton.Size = new System.Drawing.Size(90, 25);
            this.emotionTestButton.TabIndex = 3;
            this.emotionTestButton.Text = "&Emotion Test";
            this.emotionTestButton.Click += new System.EventHandler(this.SendEmoticonTest);
            // 
            // ConversationForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 14);
            this.ClientSize = new System.Drawing.Size(573, 395);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "ConversationForm";
            this.Text = "Conversation - MSNPSharp";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.ConversationForm_Closing);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private void RemoveEvent()
        {
            if (Conversation != null)
            {
                Conversation.TextMessageReceived -= Switchboard_TextMessageReceived;
                Conversation.SessionClosed -= Switchboard_SessionClosed;
                Conversation.ContactJoined -= Switchboard_ContactJoined;
                Conversation.ContactLeft -= Switchboard_ContactLeft;
                Conversation.NudgeReceived -= Switchboard_NudgeReceived;


                Conversation.MSNObjectDataTransferCompleted -= Conversation_MSNObjectDataTransferCompleted;
            }
        }

        private void AddEvent()
        {
            Conversation.TextMessageReceived += new EventHandler<TextMessageEventArgs>(Switchboard_TextMessageReceived);
            Conversation.SessionClosed += new EventHandler<EventArgs>(Switchboard_SessionClosed);
            Conversation.ContactJoined += new EventHandler<ContactEventArgs>(Switchboard_ContactJoined);
            Conversation.ContactLeft += new EventHandler<ContactEventArgs>(Switchboard_ContactLeft);
            Conversation.NudgeReceived += new EventHandler<ContactEventArgs>(Switchboard_NudgeReceived);

            Conversation.MSNObjectDataTransferCompleted += new EventHandler<MSNObjectDataTransferCompletedEventArgs>(Conversation_MSNObjectDataTransferCompleted);
            //Conversation.ConversationEnded += new EventHandler<ConversationEndEventArgs>(Conversation_ConversationEnded);

        }

        void Conversation_MSNObjectDataTransferCompleted(object sender, MSNObjectDataTransferCompletedEventArgs e)
        {
            //This is just an example to tell you how to get the emoticon data.
            FileStream fs = new FileStream("emoticon_rcv_example.png", FileMode.OpenOrCreate);
            byte[] byt = new byte[e.ClientData.OpenStream().Length];
            e.ClientData.OpenStream().Seek(0, SeekOrigin.Begin);
            e.ClientData.OpenStream().Read(byt, 0, byt.Length);
            fs.Write(byt, 0, byt.Length);
            fs.Close();
        }


        private void sendButton_Click(object sender, System.EventArgs e)
        {
            SendInput();
        }

        private void inputTextBox_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (Conversation.Expired)
                return;

            Conversation.SendTypingMessage();

            if ((e.KeyCode == Keys.Return) && (e.Alt || e.Control || e.Shift))
            {
                return;
            }

            if (e.KeyCode == Keys.Return)
            {
                if (!inputTextBox.Text.Equals(String.Empty))
                {
                    SendInput();
                }
                e.Handled = true;
            }
        }

        private void inputTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\x001b')
            {
                Close();
            }
            else if ((e.KeyChar == '\r') && inputTextBox.Text.Equals(string.Empty))
            {
                e.Handled = true;
            }
        }

        private void MakeVisible(object sender, EventArgs e)
        {
            Show();
        }

        private void PrintText(object sender, TextMessageEventArgs e)
        {
            richTextHistory.SelectionColor = Color.Gray;
            richTextHistory.AppendText(e.Sender.Name + "(" + e.Sender.Mail + "):" + Environment.NewLine);

            TextDecorations td = e.Message.Decorations;
            FontStyle fs = FontStyle.Regular;
            if ((td & TextDecorations.Bold) == TextDecorations.Bold)
                fs |= FontStyle.Bold;
            if ((td & TextDecorations.Italic) == TextDecorations.Italic)
                fs |= FontStyle.Italic;
            if ((td & TextDecorations.Underline) == TextDecorations.Underline)
                fs |= FontStyle.Underline;
            if ((td & TextDecorations.Strike) == TextDecorations.Strike)
                fs |= FontStyle.Strikeout;

            richTextHistory.SelectionColor = e.Message.Color;
            richTextHistory.SelectionFont = new Font(e.Message.Font, 8f, fs);
            richTextHistory.AppendText(e.Message.Text);
            richTextHistory.AppendText(Environment.NewLine);
            richTextHistory.ScrollToCaret();
        }

        private void PrintNudge(object sender, ContactEventArgs e)
        {
            DisplaySystemMessage("* " + e.Contact.Name + " has sent a nudge!");

        }



        public void DisplaySystemMessage(string systemMessage)
        {
            richTextHistory.SelectionColor = Color.Red;
            richTextHistory.SelectionFont = new Font("Verdana", 8f, FontStyle.Bold);
            richTextHistory.AppendText(systemMessage);
            richTextHistory.SelectionColor = Color.Black;
            richTextHistory.SelectionFont = new Font("Verdana", 9f);
            richTextHistory.AppendText(Environment.NewLine);
        }


        private void Switchboard_TextMessageReceived(object sender, TextMessageEventArgs e)
        {
            if (Visible == false)
            {
                this.Invoke(new EventHandler<EventArgs>(MakeVisible), sender, e);
            }
            Invoke(new EventHandler<TextMessageEventArgs>(PrintText), sender, e);
        }

        private void Switchboard_SessionClosed(object sender, EventArgs e)
        {
            if (!richTextHistory.InvokeRequired)
            {
                DisplaySystemMessage("* Session was closed");
                //sendButton.Enabled = false;
                //sendnudgeButton.Enabled = false;
                //emotionTestButton.Enabled = false;
                //inputTextBox.Enabled = false;
            }
            else
            {
                richTextHistory.Invoke(new EventHandler<EventArgs>(Switchboard_SessionClosed), sender, e);
            }
        }

        #region These three functions causes reinvite

        private void Switchboard_ContactJoined(object sender, ContactEventArgs e)
        {
            if (richTextHistory.InvokeRequired)
            {
                richTextHistory.Invoke(new EventHandler<ContactEventArgs>(Switchboard_ContactJoined), sender, e);
            }
            else
            {
                DisplaySystemMessage("* " + e.Contact.Name + " joined the conversation");

            }
        }

        private void Switchboard_ContactLeft(object sender, ContactEventArgs e)
        {
            if (richTextHistory.InvokeRequired)
            {
                richTextHistory.Invoke(new EventHandler<ContactEventArgs>(Switchboard_ContactLeft), sender, e);
            }
            else
            {
                DisplaySystemMessage("* " + e.Contact.Name + " left the conversation");
            }
        }


        #endregion

        private void ConversationForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Remember to close!
            RemoveEvent();  //Remove event handlers first, then close the conversation.
            if (!Conversation.Ended)
            {
                Conversation.End();
            }

            _clientform.ConversationForms.Remove(this);
        }

        private void SendInput()
        {
            // check whether there is input
            if (inputTextBox.Text.Length == 0)
                return;

            /* You can optionally change the message's font, charset, color here.
             * For example:
             * message.Color = Color.Red;
             * message.Decorations = TextDecorations.Bold;
            */
            TextMessage message = new TextMessage(inputTextBox.Text);
            message.Font = "Trebuchet MS";
            message.Color = Color.Brown;
            message.Decorations = TextDecorations.Bold;


            richTextHistory.SelectionColor = Color.Gray;
            richTextHistory.AppendText("You:" + Environment.NewLine);

            richTextHistory.SelectionColor = message.Color;
            richTextHistory.AppendText(message.Text);
            richTextHistory.AppendText(Environment.NewLine);
            richTextHistory.ScrollToCaret();

            inputTextBox.Clear();
            inputTextBox.Focus();

            //All contacts left, recreate the conversation
            Conversation.SendTextMessage(message);

        }

        private void SendNudge(object sender, EventArgs e)
        {
            Conversation.SendNudge();
            DisplaySystemMessage("* You send a nudge.");


        }

        private void SendEmoticonTest(object sender, EventArgs e)
        {
            MemoryStream mem = new MemoryStream();
            Properties.Resources.inner_emoticon.Save(mem, ImageFormat.Png);
            Emoticon emotest = new Emoticon(_clientform.Messenger.Owner.Mail, mem, "0", "test_emoicon");
            MSNObjectCatalog.GetInstance().Add(emotest);
            ArrayList emolist = new ArrayList();
            emolist.Add(emotest);

            try
            {
                Conversation.SendEmoticonDefinitions(emolist, EmoticonType.StaticEmoticon);
                TextMessage emotxt = new TextMessage("Hey, this is a custom emoticon: " + emotest.Shortcut);
                Conversation.SendTextMessage(emotxt);
                DisplaySystemMessage("* You send a custom emoticon with text message: Hey, this is a custom emoticon: [Emoticon].");
            }
            catch (NotSupportedException)
            {
                DisplaySystemMessage("* Cannot send custom emoticon in this conversation.");
            }

        }

        
    }
}
