using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using MSNPSharp;

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

		/// <summary>
		/// </summary>
		private Conversation _conversation;

		/// <summary>
		/// The conversation object which is associated with the form.
		/// </summary>
		public Conversation Conversation
		{
			get { return _conversation; }
		}

		public ConversationForm(Conversation conversation)
		{
			_conversation = conversation;

			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			Conversation.Switchboard.TextMessageReceived += new TextMessageReceivedEventHandler(Switchboard_TextMessageReceived);
			Conversation.Switchboard.SessionClosed += new SBChangedEventHandler(Switchboard_SessionClosed);
			Conversation.Switchboard.ContactJoined += new ContactChangedEventHandler(Switchboard_ContactJoined);
			Conversation.Switchboard.ContactLeft   += new ContactChangedEventHandler(Switchboard_ContactLeft);			
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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.conversationTextBox = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.sendButton = new System.Windows.Forms.Button();
            this.inputTextBox = new System.Windows.Forms.TextBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // conversationTextBox
            // 
            this.conversationTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.conversationTextBox.Location = new System.Drawing.Point(0, 0);
            this.conversationTextBox.Multiline = true;
            this.conversationTextBox.Name = "conversationTextBox";
            this.conversationTextBox.Size = new System.Drawing.Size(464, 334);
            this.conversationTextBox.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.panel1.Controls.Add(this.sendButton);
            this.panel1.Controls.Add(this.inputTextBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 262);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(464, 72);
            this.panel1.TabIndex = 1;
            // 
            // sendButton
            // 
            this.sendButton.Location = new System.Drawing.Point(376, 24);
            this.sendButton.Name = "sendButton";
            this.sendButton.Size = new System.Drawing.Size(75, 23);
            this.sendButton.TabIndex = 1;
            this.sendButton.Text = "Send";
            this.sendButton.Click += new System.EventHandler(this.sendButton_Click);
            // 
            // inputTextBox
            // 
            this.inputTextBox.Location = new System.Drawing.Point(8, 8);
            this.inputTextBox.Multiline = true;
            this.inputTextBox.Name = "inputTextBox";
            this.inputTextBox.Size = new System.Drawing.Size(344, 56);
            this.inputTextBox.TabIndex = 0;
            this.inputTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.inputTextBox_KeyDown);
            // 
            // ConversationForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(464, 334);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.conversationTextBox);
            this.Name = "ConversationForm";
            this.Text = "Conversation - MSNPSharp";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.ConversationForm_Closing);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		private bool _typingMessageSended = false;

		private void SendInput()
		{			
			// check whether there is input
			if(inputTextBox.Text.Length == 0) return;

			// if there is no switchboard available, request a new switchboard session
			if(Conversation.SwitchboardProcessor.Connected == false)
			{
				Conversation.Messenger.Nameserver.RequestSwitchboard(Conversation.Switchboard, this);
			}

            // note: you can add some code here to catch the event where the remote contact lefts due to being idle too long
            // in that case Conversation.Switchboard.Contacts.Count equals 0.            

			TextMessage message = new TextMessage(inputTextBox.Text);

			/* You can optionally change the message's font, charset, color here.
			 * For example:
			 * message.Color = Color.Red;
			 * message.Decorations = TextDecorations.Bold;
			 */
			
			Conversation.Switchboard.SendTextMessage(message);
			_typingMessageSended = false;
			inputTextBox.Text = "";
			conversationTextBox.Text += "You say: " + message.Text + "\r\n";
		}


		private void sendButton_Click(object sender, System.EventArgs e)
		{
			SendInput();
		}

		private void inputTextBox_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
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
			conversationTextBox.Text += "* Session was closed\r\n";
		}

		private void Switchboard_ContactJoined(object sender, ContactEventArgs e)
		{
			conversationTextBox.Text += "* " + e.Contact.Name + " joined the conversation\r\n";
		}

		private void Switchboard_ContactLeft(object sender, ContactEventArgs e)
		{
			conversationTextBox.Text += "* " + e.Contact.Name + " left the conversation\r\n";
		}

		private void ConversationForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Conversation.Switchboard.Close();
		}
	}
}
