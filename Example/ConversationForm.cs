using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using MSNPSharp;
using System.Collections.Generic;

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
        private List<string> _leftusers = new List<string>(0);
        private List<TextMessage> _messagequene = new List<TextMessage>(0);
        private List<object> _nudgequene = new List<object>(0);
        private ClientForm _clientform = null;
        private Dictionary<string, bool> _contactStatus = new Dictionary<string, bool>(0);
        private RichTextBox richTextHistory;
        private bool _isChatForm = false;

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

        protected ConversationForm()
        {
        }

        public ConversationForm(Conversation conversation, ClientForm clientform, string account)
        {
            if (conversation != null)
            {
                _contactStatus.Add(account, false);
                _conversation = conversation;
                AddEvent();
            }
            else
            {
                //Create by local user
                _contactStatus.Add(account, true);
                _leftusers.Add(account);
            }
            _clientform = clientform;

            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

        }

        /// <summary>
        /// Attatch a new conversation to this chatting form
        /// </summary>
        /// <param name="convers"></param>
        public void AttachConversation(Conversation convers)
        {
            if (convers == null)
                throw new NullReferenceException();
 
            if (Conversation != null)
            {
                Conversation.Switchboard.Close();
                RemoveEvent();
            }

            _conversation = convers;
            AddEvent();
        }

        public int CanAttach(string account)
        {
            if (_isChatForm)
            {
                if (_contactStatus.ContainsKey(account.ToLowerInvariant()))
                {
                    // If the remote contact is still in the conversation, return false.
                    // If the remote contact has left, return true.
                    if (!_contactStatus[account.ToLowerInvariant()])
                        return 1;
                    else
                        return 0;
                }
            }
            return -1;
        }

        void Switchboard_NudgeReceived(object sender, ContactEventArgs e)
        {
            if (Visible == false)
            {
                Invoke(new MakeVisibleDelegate(MakeVisible));
            }

            Invoke(new PrintNudgeDelegate(PrintNudge), e);
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
            _isChatForm = true;
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
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.panel1.Controls.Add(this.sendnudgeButton);
            this.panel1.Controls.Add(this.sendButton);
            this.panel1.Controls.Add(this.inputTextBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 262);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(555, 72);
            this.panel1.TabIndex = 1;
            // 
            // sendnudgeButton
            // 
            this.sendnudgeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.sendnudgeButton.Location = new System.Drawing.Point(470, 38);
            this.sendnudgeButton.Name = "sendnudgeButton";
            this.sendnudgeButton.Size = new System.Drawing.Size(75, 23);
            this.sendnudgeButton.TabIndex = 2;
            this.sendnudgeButton.Text = "Send &Nudge";
            this.sendnudgeButton.Click += new System.EventHandler(this.sendnudgeButton_Click);
            // 
            // sendButton
            // 
            this.sendButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.sendButton.Location = new System.Drawing.Point(470, 8);
            this.sendButton.Name = "sendButton";
            this.sendButton.Size = new System.Drawing.Size(75, 24);
            this.sendButton.TabIndex = 1;
            this.sendButton.Text = "&Send";
            this.sendButton.Click += new System.EventHandler(this.sendButton_Click);
            // 
            // inputTextBox
            // 
            this.inputTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.inputTextBox.Location = new System.Drawing.Point(8, 8);
            this.inputTextBox.Multiline = true;
            this.inputTextBox.Name = "inputTextBox";
            this.inputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.inputTextBox.Size = new System.Drawing.Size(437, 56);
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
            this.panel2.Size = new System.Drawing.Size(555, 262);
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
            this.richTextHistory.Size = new System.Drawing.Size(555, 262);
            this.richTextHistory.TabIndex = 1;
            this.richTextHistory.TabStop = false;
            this.richTextHistory.Text = "";
            // 
            // ConversationForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(555, 334);
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

        private bool _typingMessageSended = false;

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
            _typingMessageSended = false;


            richTextHistory.SelectionColor = Color.Gray;
            richTextHistory.AppendText("You:" + Environment.NewLine);

            richTextHistory.SelectionColor = message.Color;
            richTextHistory.AppendText(message.Text);
            richTextHistory.AppendText(Environment.NewLine);
            richTextHistory.ScrollToCaret();

            inputTextBox.Clear();
            inputTextBox.Focus();

            //All contacts left, recreate the conversation
            if (ReInvite())
            {
                _messagequene.Add(message);
                return;
            }

            //// if there is no switchboard available, request a new switchboard session
            //if(Conversation.SwitchboardProcessor.Connected == false)
            //{
            //    Conversation.Messenger.Nameserver.RequestSwitchboard(Conversation.Switchboard, this);
            //    _messagequene.Add(message);
            //    return;
            //}

            //// note: you can add some code here to catch the event where the remote contact lefts due to being idle too long
            //// in that case Conversation.Switchboard.Contacts.Count equals 0.
            //if (Conversation.Switchboard.Contacts.Count == 0)
            //{
            //    foreach (string account in _leftusers)
            //    {
            //        _conversation.Invite(account);
            //    }
            //    _leftusers.Clear();
            //    _messagequene.Add(message);
            //    return;
            //}

            Conversation.Switchboard.SendTextMessage(message);

        }

        private void RemoveEvent()
        {
            if (Conversation != null)
            {
                Conversation.Switchboard.TextMessageReceived -= Switchboard_TextMessageReceived;
                Conversation.Switchboard.SessionClosed -= Switchboard_SessionClosed;
                Conversation.Switchboard.ContactJoined -= Switchboard_ContactJoined;
                Conversation.Switchboard.ContactLeft -= Switchboard_ContactLeft;
                Conversation.Switchboard.NudgeReceived -= Switchboard_NudgeReceived;
                Conversation.Switchboard.AllContactsLeft -= Switchboard_AllContactsLeft;
            }
        }

        private void AddEvent()
        {
            if (Conversation != null)
            {
                Conversation.Switchboard.TextMessageReceived += new TextMessageReceivedEventHandler(Switchboard_TextMessageReceived);
                Conversation.Switchboard.SessionClosed += new SBChangedEventHandler(Switchboard_SessionClosed);
                Conversation.Switchboard.ContactJoined += new ContactChangedEventHandler(Switchboard_ContactJoined);
                Conversation.Switchboard.ContactLeft += new ContactChangedEventHandler(Switchboard_ContactLeft);
                Conversation.Switchboard.NudgeReceived += new ContactChangedEventHandler(Switchboard_NudgeReceived);
                Conversation.Switchboard.AllContactsLeft += new SBChangedEventHandler(Switchboard_AllContactsLeft);
            }
        }

        private bool ReInvite()
        {
            if (_conversation == null || !Conversation.Switchboard.IsSessionEstablished)
            {
                RemoveEvent();
                _conversation = _clientform.Messenger.CreateConversation();

                AddEvent();
                foreach (string account in _leftusers)
                {
                    _conversation.Invite(account, ClientType.PassportMember);
                }
                _leftusers.Clear();
                return true;
            }
            return false;
        }

        private void sendButton_Click(object sender, System.EventArgs e)
        {
            SendInput();
        }

        private void inputTextBox_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (Conversation == null || !Conversation.Switchboard.IsSessionEstablished) //DONOT call ReInvite here!
                return;

            if (_typingMessageSended == false)
            {
                Conversation.Switchboard.SendTypingMessage();
                _typingMessageSended = true;
            }

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

        private delegate void MakeVisibleDelegate();
        private void MakeVisible()
        {
            Show();
        }

        private delegate void PrintTextDelegate(TextMessageEventArgs e);
        private void PrintText(TextMessageEventArgs e)
        {
            richTextHistory.SelectionColor = Color.Gray;
            richTextHistory.AppendText(e.Sender.Name + ":" + Environment.NewLine);

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

        private delegate void PrintNudgeDelegate(ContactEventArgs e);
        private void PrintNudge(ContactEventArgs e)
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
                this.Invoke(new MakeVisibleDelegate(MakeVisible));
            }
            Invoke(new PrintTextDelegate(PrintText), e);
        }

        private void Switchboard_SessionClosed(object sender, EventArgs e)
        {
            if (!richTextHistory.InvokeRequired)
            {
                DisplaySystemMessage("* Session was closed");
            }
            else
            {
                richTextHistory.Invoke(new SBChangedEventHandler(Switchboard_SessionClosed), sender, e);
            }
        }

        #region These three functions causes reinvite
        private delegate void Delegate_Switchboard_ContactJoined(object sender, ContactEventArgs e);

        private void Switchboard_ContactJoined(object sender, ContactEventArgs e)
        {
            if (richTextHistory.InvokeRequired)
            {
                richTextHistory.Invoke(new Delegate_Switchboard_ContactJoined(Switchboard_ContactJoined), sender, e);
            }
            else
            {
                DisplaySystemMessage("* " + e.Contact.Name + " joined the conversation");

                _contactStatus[e.Contact.Mail.ToLowerInvariant()] = true;

                //Send all messages and nudges
                if (_messagequene.Count > 0)
                {
                    for (int i = 0; i < _messagequene.Count; i++)
                    {
                        _conversation.Switchboard.SendTextMessage(_messagequene[i]);
                    }
                    _messagequene.Clear();
                }

                if (_nudgequene.Count > 0)
                {
                    foreach (object ob in _nudgequene)
                    {
                        _conversation.Switchboard.SendNudge();
                    }
                    _nudgequene.Clear();
                }
            }
        }

        private void Switchboard_ContactLeft(object sender, ContactEventArgs e)
        {
            if (richTextHistory.InvokeRequired)
            {
                richTextHistory.Invoke(new ContactChangedEventHandler(Switchboard_ContactLeft), sender, e);
            }
            else
            {
                DisplaySystemMessage("* " + e.Contact.Name + " left the conversation");

                if (!_leftusers.Contains(e.Contact.Mail))
                    _leftusers.Add(e.Contact.Mail);

                _contactStatus[e.Contact.Mail.ToLowerInvariant()] = false;
            }
        }

        void Switchboard_AllContactsLeft(object sender, EventArgs e)
        {
            RemoveEvent();
            Conversation.Switchboard.Close();
        }


        #endregion
        private void ConversationForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Remember to close!
            if (Conversation != null)
            {
                Conversation.Switchboard.Close();
                RemoveEvent();
            }
            //_clientform.Dicconversation.Remove(Conversation);
            _clientform.ConversationForms.Remove(this);
        }

        private void sendnudgeButton_Click(object sender, EventArgs e)
        {
            if (ReInvite())
            {
                _nudgequene.Add(new object());
                return;
            }

            Conversation.Switchboard.SendNudge();

            DisplaySystemMessage("* You send a nudge.");
        }

        
    }
}
