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
		private System.Windows.Forms.TextBox conversationTextBox;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.TextBox inputTextBox;
		private System.Windows.Forms.Button sendButton;
        private Button sendnudgeButton;
        private Panel panel2;


		/// <summary>
		/// </summary>
		private Conversation _conversation;
        private List<string> _leftusers = new List<string>(0);
        private List<TextMessage> _messagequene = new List<TextMessage>(0);
        private List<object> _nudgequene = new List<object>(0);
        private ClientForm _clientform = null;
        private Dictionary<string, bool> _contactStatus = new Dictionary<string, bool>(0);
        private bool _isChatForm = false;

		/// <summary>
		/// The conversation object which is associated with the form.
		/// </summary>
		public Conversation Conversation
		{
			get { return _conversation; }
		}

        protected ConversationForm()
        {
        }

		public ConversationForm(Conversation conversation,ClientForm clientform)
		{
			_conversation = conversation;
            _clientform = clientform;

			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

            AddEvent();
		}

        /// <summary>
        /// Attatch a new conversation to this chatting form
        /// </summary>
        /// <param name="convers"></param>
        public void AttachConversation(Conversation convers)
        {
            Conversation.Switchboard.Close();
            RemoveEvent();

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
            MessageBox.Show("!!!!!");
        }		

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
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
            this.conversationTextBox = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.sendnudgeButton = new System.Windows.Forms.Button();
            this.sendButton = new System.Windows.Forms.Button();
            this.inputTextBox = new System.Windows.Forms.TextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // conversationTextBox
            // 
            this.conversationTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.conversationTextBox.Location = new System.Drawing.Point(0, 0);
            this.conversationTextBox.Multiline = true;
            this.conversationTextBox.Name = "conversationTextBox";
            this.conversationTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.conversationTextBox.Size = new System.Drawing.Size(555, 256);
            this.conversationTextBox.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.panel1.Controls.Add(this.sendnudgeButton);
            this.panel1.Controls.Add(this.sendButton);
            this.panel1.Controls.Add(this.inputTextBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 256);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(555, 78);
            this.panel1.TabIndex = 1;
            // 
            // sendnudgeButton
            // 
            this.sendnudgeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.sendnudgeButton.Location = new System.Drawing.Point(452, 41);
            this.sendnudgeButton.Name = "sendnudgeButton";
            this.sendnudgeButton.Size = new System.Drawing.Size(90, 25);
            this.sendnudgeButton.TabIndex = 2;
            this.sendnudgeButton.Text = "Send Nudge";
            this.sendnudgeButton.Click += new System.EventHandler(this.sendnudgeButton_Click);
            // 
            // sendButton
            // 
            this.sendButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.sendButton.Location = new System.Drawing.Point(452, 9);
            this.sendButton.Name = "sendButton";
            this.sendButton.Size = new System.Drawing.Size(90, 25);
            this.sendButton.TabIndex = 1;
            this.sendButton.Text = "Send";
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
            this.inputTextBox.Size = new System.Drawing.Size(412, 60);
            this.inputTextBox.TabIndex = 0;
            this.inputTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.inputTextBox_KeyDown);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.conversationTextBox);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(555, 256);
            this.panel2.TabIndex = 2;
            // 
            // ConversationForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 14);
            this.ClientSize = new System.Drawing.Size(555, 334);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "ConversationForm";
            this.Text = "Conversation - MSNPSharp";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.ConversationForm_Closing);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

		}
		#endregion

		private bool _typingMessageSended = false;

		private void SendInput()
		{			
			// check whether there is input
			if(inputTextBox.Text.Length == 0) return;

            /* You can optionally change the message's font, charset, color here.
             * For example:
             * message.Color = Color.Red;
             * message.Decorations = TextDecorations.Bold;
            */
            TextMessage message = new TextMessage(inputTextBox.Text);
            _typingMessageSended = false;
            inputTextBox.Text = String.Empty;
            conversationTextBox.Text += "You say: " + message.Text + "\r\n";

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
            Conversation.Switchboard.TextMessageReceived        -= Switchboard_TextMessageReceived;
            Conversation.Switchboard.SessionClosed              -= Switchboard_SessionClosed;
            Conversation.Switchboard.ContactJoined              -= Switchboard_ContactJoined;
            Conversation.Switchboard.ContactLeft                -= Switchboard_ContactLeft;
            Conversation.Switchboard.NudgeReceived              -= Switchboard_NudgeReceived;
            Conversation.Switchboard.AllContactsLeft            -= Switchboard_AllContactsLeft;
        }

        private void AddEvent()
        {
            Conversation.Switchboard.TextMessageReceived    += new TextMessageReceivedEventHandler(Switchboard_TextMessageReceived);
            Conversation.Switchboard.SessionClosed          += new SBChangedEventHandler(Switchboard_SessionClosed);
            Conversation.Switchboard.ContactJoined          += new ContactChangedEventHandler(Switchboard_ContactJoined);
            Conversation.Switchboard.ContactLeft            += new ContactChangedEventHandler(Switchboard_ContactLeft);
            Conversation.Switchboard.NudgeReceived          += new ContactChangedEventHandler(Switchboard_NudgeReceived);
            Conversation.Switchboard.AllContactsLeft        += new SBChangedEventHandler(Switchboard_AllContactsLeft);
        }

        private bool ReInvite()
        {
            if (!Conversation.Switchboard.IsSessionEstablished)
            {
                _clientform.Dicconversation.Remove(_conversation);
                RemoveEvent();
                _conversation = _conversation.Messenger.CreateConversation();
                _clientform.Dicconversation.Add(_conversation, this);
                AddEvent();
                foreach (string account in _leftusers)
                {
                    _conversation.Invite(account);
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
            if (!Conversation.Switchboard.IsSessionEstablished) //DONOT call ReInvite here!
                return;

			if(_typingMessageSended == false)
			{
				Conversation.Switchboard.SendTypingMessage();
				_typingMessageSended = true;
			}
			if(e.KeyCode == Keys.Enter)
				SendInput();
		}

		private delegate void MakeVisibleDelegate();
		private void MakeVisible()
		{
			Show();
		}

        private delegate void PrintTextDelegate(string name, string text);
        private void PrintText(string name, string text)
        {
            conversationTextBox.Text += name + " says: " + text + "\r\n";
        }

		private void Switchboard_TextMessageReceived(object sender, TextMessageEventArgs e)
		{			
			// use Invoke() because this method is not run in the form's thread, but in the MSNPSharp (worker)thread
            if(Visible == false)
			{
				this.Invoke(new MakeVisibleDelegate(MakeVisible));
			}
            Invoke(new PrintTextDelegate(PrintText), new object[] { e.Sender.Name, e.Message.Text });            	
		}

		private void Switchboard_SessionClosed(object sender, EventArgs e)
		{
            if (!this.InvokeRequired)
            {
                conversationTextBox.Text += "* Session was closed\r\n";
            }
            else
            {
                this.Invoke(new SBChangedEventHandler(Switchboard_SessionClosed),
                    new object[] { sender, e });
            }
			
		}

        #region These three functions causes reinvite
        private delegate void Delegate_Switchboard_ContactJoined(object sender, ContactEventArgs e);

        private void Switchboard_ContactJoined(object sender, ContactEventArgs e)
        {
            if (conversationTextBox.InvokeRequired)
            {
                Delegate_Switchboard_ContactJoined d = new Delegate_Switchboard_ContactJoined(Switchboard_ContactJoined);
                object[] args ={ 0, 0 };
                args[0] = sender;
                args[1] = e;
                conversationTextBox.Invoke(d, args);
            }
            else
            {
                conversationTextBox.Text += "* " + e.Contact.Name + " joined the conversation\r\n";
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
            if (conversationTextBox.InvokeRequired)
            {
                conversationTextBox.Invoke(new ContactChangedEventHandler(Switchboard_ContactLeft),
                    new object[] { sender, e });
            }
            else
            {
                conversationTextBox.Text += "* " + e.Contact.Name + " left the conversation\r\n";
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
			Conversation.Switchboard.Close();
            RemoveEvent();
            _clientform.Dicconversation.Remove(Conversation);
		}

        private void sendnudgeButton_Click(object sender, EventArgs e)
        {
            if (ReInvite())
            {
                _nudgequene.Add(new object());
                return;
            }

            //if (Conversation.SwitchboardProcessor.Connected == false)
            //{
            //    Conversation.Messenger.Nameserver.RequestSwitchboard(Conversation.Switchboard, this);
            //}

            Conversation.Switchboard.SendNudge();
            conversationTextBox.Text += "You send a nudge.\r\n";
        }
	}
}
