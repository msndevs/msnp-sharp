using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace MSNPSharpClient
{
    using MSNPSharp;
    using MSNPSharp.DataTransfer;

    /// <summary>
    /// Summary description for ConversationForm.
    /// </summary>
    public class ConversationForm : System.Windows.Forms.Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private Container components = null;
        private Panel panel1;
        private TextBox inputTextBox;
        private Button sendButton;
        private Button sendnudgeButton;
        private Panel panel2;
        private RichTextBox richTextHistory;
        private PictureBox displayOwner;
        private PictureBox displayUser;
        private Button emotionTestButton;

        private Conversation _conversation = null;
        private ClientForm _clientform = null;
        private List<string> _contacts = new List<string>(0);

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
            get
            {
                return _contacts;
            }
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
                if ((newconvers.Type | ConversationType.Chat) == _conversation.Type)
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
            this.displayOwner = new System.Windows.Forms.PictureBox();
            this.emotionTestButton = new System.Windows.Forms.Button();
            this.sendnudgeButton = new System.Windows.Forms.Button();
            this.sendButton = new System.Windows.Forms.Button();
            this.inputTextBox = new System.Windows.Forms.TextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.displayUser = new System.Windows.Forms.PictureBox();
            this.richTextHistory = new System.Windows.Forms.RichTextBox();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.displayOwner)).BeginInit();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.displayUser)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.panel1.Controls.Add(this.displayOwner);
            this.panel1.Controls.Add(this.emotionTestButton);
            this.panel1.Controls.Add(this.sendnudgeButton);
            this.panel1.Controls.Add(this.sendButton);
            this.panel1.Controls.Add(this.inputTextBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 287);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(573, 108);
            this.panel1.TabIndex = 0;
            // 
            // displayOwner
            // 
            this.displayOwner.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.displayOwner.BackColor = System.Drawing.Color.White;
            this.displayOwner.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.displayOwner.Location = new System.Drawing.Point(3, 5);
            this.displayOwner.Name = "displayOwner";
            this.displayOwner.Size = new System.Drawing.Size(100, 100);
            this.displayOwner.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.displayOwner.TabIndex = 0;
            this.displayOwner.TabStop = false;
            // 
            // emotionTestButton
            // 
            this.emotionTestButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.emotionTestButton.Location = new System.Drawing.Point(486, 82);
            this.emotionTestButton.Name = "emotionTestButton";
            this.emotionTestButton.Size = new System.Drawing.Size(84, 23);
            this.emotionTestButton.TabIndex = 3;
            this.emotionTestButton.Text = "&Emotion Test";
            this.emotionTestButton.Click += new System.EventHandler(this.SendEmoticonTest);
            // 
            // sendnudgeButton
            // 
            this.sendnudgeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.sendnudgeButton.Location = new System.Drawing.Point(486, 53);
            this.sendnudgeButton.Name = "sendnudgeButton";
            this.sendnudgeButton.Size = new System.Drawing.Size(84, 23);
            this.sendnudgeButton.TabIndex = 4;
            this.sendnudgeButton.Text = "Send &Nudge";
            this.sendnudgeButton.Click += new System.EventHandler(this.SendNudge);
            // 
            // sendButton
            // 
            this.sendButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.sendButton.Location = new System.Drawing.Point(486, 6);
            this.sendButton.Name = "sendButton";
            this.sendButton.Size = new System.Drawing.Size(84, 41);
            this.sendButton.TabIndex = 2;
            this.sendButton.Text = "&Send";
            this.sendButton.Click += new System.EventHandler(this.sendButton_Click);
            // 
            // inputTextBox
            // 
            this.inputTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.inputTextBox.Location = new System.Drawing.Point(109, 3);
            this.inputTextBox.Multiline = true;
            this.inputTextBox.Name = "inputTextBox";
            this.inputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.inputTextBox.Size = new System.Drawing.Size(371, 102);
            this.inputTextBox.TabIndex = 1;
            this.inputTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.inputTextBox_KeyDown);
            this.inputTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.inputTextBox_KeyPress);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.displayUser);
            this.panel2.Controls.Add(this.richTextHistory);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(573, 287);
            this.panel2.TabIndex = 0;
            // 
            // displayUser
            // 
            this.displayUser.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.displayUser.BackColor = System.Drawing.Color.White;
            this.displayUser.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.displayUser.Location = new System.Drawing.Point(3, 181);
            this.displayUser.Name = "displayUser";
            this.displayUser.Size = new System.Drawing.Size(100, 100);
            this.displayUser.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.displayUser.TabIndex = 0;
            this.displayUser.TabStop = false;
            // 
            // richTextHistory
            // 
            this.richTextHistory.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextHistory.BackColor = System.Drawing.Color.Snow;
            this.richTextHistory.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextHistory.Location = new System.Drawing.Point(109, 3);
            this.richTextHistory.Name = "richTextHistory";
            this.richTextHistory.ReadOnly = true;
            this.richTextHistory.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.richTextHistory.Size = new System.Drawing.Size(464, 278);
            this.richTextHistory.TabIndex = 0;
            this.richTextHistory.TabStop = false;
            this.richTextHistory.Text = "";
            // 
            // ConversationForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(573, 395);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "ConversationForm";
            this.Text = "Conversation - MSNPSharp";
            this.Load += new System.EventHandler(this.ConversationForm_Load);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.ConversationForm_Closing);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.displayOwner)).EndInit();
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.displayUser)).EndInit();
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

            if ((e.KeyCode == Keys.Return) && (e.Alt || e.Control || e.Shift))
            {
                return;
            }

            Conversation.SendTypingMessage();

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

            richTextHistory.ScrollToCaret();
        }


        private void Switchboard_TextMessageReceived(object sender, TextMessageEventArgs e)
        {
            if (!Visible)
            {
                Invoke(new EventHandler<EventArgs>(MakeVisible), sender, e);
            }

            Invoke(new EventHandler<TextMessageEventArgs>(PrintText), sender, e);
        }

        private void Switchboard_SessionClosed(object sender, EventArgs e)
        {
            if (!richTextHistory.InvokeRequired)
            {
                DisplaySystemMessage("* Session was closed");
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

                // request the image, if not already available
                if (e.Contact.Status != PresenceStatus.Offline && e.Contact.DisplayImage != null)
                {
                    if (e.Contact.DisplayImage.Image == null)
                    {
                        try
                        {
                            if (e.Contact.ClientType == ClientType.PassportMember)
                            {
                                // create a MSNSLPHandler. This handler takes care of the filetransfer protocol.
                                // The MSNSLPHandler makes use of the underlying P2P framework.					
                                MSNSLPHandler msnslpHandler = _conversation.Messenger.GetMSNSLPHandler(e.Contact.Mail);

                                // by sending an invitation a P2PTransferSession is automatically created.
                                // the session object takes care of the actual data transfer to the remote client,
                                // in contrast to the msnslpHandler object, which only deals with the protocol chatting.
                                P2PTransferSession session = msnslpHandler.SendInvitation(_conversation.Messenger.Owner.Mail, e.Contact.Mail, e.Contact.DisplayImage);

                                // as usual, via events we want to be notified when a transfer is finished.
                                // ofcourse, optionally, you can also catch abort and error events.
                                session.TransferFinished += delegate(object s, EventArgs ea)
                                {
                                    P2PTransferSession sess = (P2PTransferSession)s;
                                    DisplayImage image = (DisplayImage)sess.ClientData;
                                    image.RetrieveImage();

                                    if (image.Image != null)
                                        displayUser.Image = image.Image;
                                };

                                session.ClientData = e.Contact.DisplayImage;
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                    else
                        displayUser.Image = e.Contact.DisplayImage.Image;
                }

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
            Emoticon emotest = new Emoticon(_clientform.Messenger.Owner.Mail, mem, "0", "test_em");
            MSNObjectCatalog.GetInstance().Add(emotest);
            List<Emoticon> emolist = new List<Emoticon>();
            emolist.Add(emotest);

            try
            {
                Conversation.SendEmoticonDefinitions(emolist, EmoticonType.StaticEmoticon);
                TextMessage emotxt = new TextMessage("Hey, this is a custom emoticon: " + emotest.Shortcut);
                Conversation.SendTextMessage(emotxt);
                DisplaySystemMessage("* You send a custom emoticon with text message: Hey, this is a custom emoticon: [test_em].");
            }
            catch (NotSupportedException)
            {
                DisplaySystemMessage("* Cannot send custom emoticon in this conversation.");
            }

        }

        private void ConversationForm_Load(object sender, EventArgs e)
        {
            displayOwner.Image = _clientform.Messenger.Owner.DisplayImage.Image;
        }

    }
};
