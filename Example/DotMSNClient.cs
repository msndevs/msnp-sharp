using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using MSNPSharp;
using MSNPSharp.Core;
using MSNPSharp.DataTransfer;

namespace MSNPSharpClient
{
	/// <summary>
	/// MSNPSharp Client example.
	/// </summary>
	public class ClientForm : System.Windows.Forms.Form
	{		
		// Create a Messenger object to use MSNPSharp.
		private	MSNPSharp.Messenger messenger = new Messenger();

		#region Form controls
		private System.Windows.Forms.Panel ListPanel;
		private System.Windows.Forms.Panel OwnerPanel;
		private System.Windows.Forms.Panel ContactPanel;
		private System.Windows.Forms.Splitter splitter1;
		private System.Windows.Forms.ListView ContactListView;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button loginButton;
		private System.Windows.Forms.TextBox accountTextBox;
		private System.Windows.Forms.TextBox passwordTextBox;
		private System.Windows.Forms.StatusBar statusBar;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.PropertyGrid propertyGrid;
		private System.Windows.Forms.Splitter splitter2;
		private System.Windows.Forms.PictureBox pictureBox;
		private System.Windows.Forms.PictureBox displayImageBox;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Button inviteButton;
		private System.Windows.Forms.Button filetransferButton;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;
		private System.Windows.Forms.Button MobileMessageButton;
        private System.Windows.Forms.Button changeDisplayButton;
        private OpenFileDialog openImageDialog;
        private Timer tmrKeepOnLine;
        private Button buttonOIM;
        private TextBox txtMSG;
        private IContainer components;
		#endregion
        

		public ClientForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			// You can set proxy settings here
			// for example: messenger.ConnectivitySettings.ProxyHost = "10.0.0.2";
			
			// by default this example will emulate the official microsoft windows messenger client
            messenger.Credentials.ClientID = "PROD0119GSJUC$18";
            messenger.Credentials.ClientCode = "ILTXC!4IXB5FB*PX";

            // uncomment this to enable verbose output for debugging
            Settings.TraceSwitch.Level = System.Diagnostics.TraceLevel.Verbose;
		
			// set the events that we will handle
			// remember that the nameserver is the server that sends contact lists, notifies you of contact status changes, etc.
			// a switchboard server handles the individual conversation sessions.
			messenger.NameserverProcessor.ConnectionEstablished += new EventHandler(NameserverProcessor_ConnectionEstablished);
			messenger.Nameserver.SignedIn += new EventHandler(Nameserver_SignedIn);
			messenger.Nameserver.SignedOff += new SignedOffEventHandler(Nameserver_SignedOff);
			messenger.NameserverProcessor.ConnectingException += new ProcessorExceptionEventHandler(NameserverProcessor_ConnectingException);			
			messenger.Nameserver.ExceptionOccurred += new HandlerExceptionEventHandler(Nameserver_ExceptionOccurred);					
			messenger.Nameserver.AuthenticationError += new HandlerExceptionEventHandler(Nameserver_AuthenticationError);
			messenger.Nameserver.ServerErrorReceived += new ErrorReceivedEventHandler(Nameserver_ServerErrorReceived);
			messenger.ConversationCreated += new ConversationCreatedEventHandler(messenger_ConversationCreated);			
			messenger.TransferInvitationReceived += new MSNSLPInvitationReceivedEventHandler(messenger_TransferInvitationReceived);
            messenger.Nameserver.OIMReceived += new OIMReceivedEventHandler(Nameserver_OIMReceived);
            messenger.Nameserver.PingAnswer += new PingAnswerEventHandler(Nameserver_PingAnswer);
        }

        void Nameserver_PingAnswer(object sender, PingAnswerEventArgs e)
        {
            nextPing = e.SecondsToWait;
        }

        void Nameserver_OIMReceived(object sender, OIMReceivedEventArgs e)
        {
            if (MessageBox.Show(e.TimeReceived + ": " + e.Message + "\r\n\r\n\r\nSave message?", "Offline Message from " + e.Email, MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
            {
                e.DeleteMessage = false;
            }
        }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClientForm));
            this.ListPanel = new System.Windows.Forms.Panel();
            this.ContactListView = new System.Windows.Forms.ListView();
            this.OwnerPanel = new System.Windows.Forms.Panel();
            this.changeDisplayButton = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.statusBar = new System.Windows.Forms.StatusBar();
            this.loginButton = new System.Windows.Forms.Button();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.accountTextBox = new System.Windows.Forms.TextBox();
            this.ContactPanel = new System.Windows.Forms.Panel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.MobileMessageButton = new System.Windows.Forms.Button();
            this.filetransferButton = new System.Windows.Forms.Button();
            this.inviteButton = new System.Windows.Forms.Button();
            this.displayImageBox = new System.Windows.Forms.PictureBox();
            this.splitter2 = new System.Windows.Forms.Splitter();
            this.propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.openImageDialog = new System.Windows.Forms.OpenFileDialog();
            this.tmrKeepOnLine = new System.Windows.Forms.Timer(this.components);
            this.txtMSG = new System.Windows.Forms.TextBox();
            this.buttonOIM = new System.Windows.Forms.Button();
            this.ListPanel.SuspendLayout();
            this.OwnerPanel.SuspendLayout();
            this.ContactPanel.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.displayImageBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // ListPanel
            // 
            this.ListPanel.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ListPanel.Controls.Add(this.ContactListView);
            this.ListPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListPanel.Location = new System.Drawing.Point(0, 96);
            this.ListPanel.Name = "ListPanel";
            this.ListPanel.Size = new System.Drawing.Size(406, 400);
            this.ListPanel.TabIndex = 0;
            // 
            // ContactListView
            // 
            this.ContactListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ContactListView.Location = new System.Drawing.Point(0, 0);
            this.ContactListView.MultiSelect = false;
            this.ContactListView.Name = "ContactListView";
            this.ContactListView.Size = new System.Drawing.Size(406, 400);
            this.ContactListView.TabIndex = 0;
            this.ContactListView.UseCompatibleStateImageBehavior = false;
            this.ContactListView.View = System.Windows.Forms.View.List;
            this.ContactListView.ItemActivate += new System.EventHandler(this.ContactListView_ItemActivate);
            // 
            // OwnerPanel
            // 
            this.OwnerPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.OwnerPanel.Controls.Add(this.changeDisplayButton);
            this.OwnerPanel.Controls.Add(this.button1);
            this.OwnerPanel.Controls.Add(this.statusBar);
            this.OwnerPanel.Controls.Add(this.loginButton);
            this.OwnerPanel.Controls.Add(this.passwordTextBox);
            this.OwnerPanel.Controls.Add(this.label2);
            this.OwnerPanel.Controls.Add(this.label1);
            this.OwnerPanel.Controls.Add(this.accountTextBox);
            this.OwnerPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.OwnerPanel.Location = new System.Drawing.Point(0, 496);
            this.OwnerPanel.Name = "OwnerPanel";
            this.OwnerPanel.Size = new System.Drawing.Size(638, 104);
            this.OwnerPanel.TabIndex = 1;
            // 
            // changeDisplayButton
            // 
            this.changeDisplayButton.Location = new System.Drawing.Point(264, 40);
            this.changeDisplayButton.Name = "changeDisplayButton";
            this.changeDisplayButton.Size = new System.Drawing.Size(163, 23);
            this.changeDisplayButton.TabIndex = 7;
            this.changeDisplayButton.Text = "Display image";
            this.changeDisplayButton.Click += new System.EventHandler(this.changeDisplayButton_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(352, 16);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 6;
            this.button1.Text = "Sign off >";
            this.button1.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // statusBar
            // 
            this.statusBar.Location = new System.Drawing.Point(0, 82);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(638, 22);
            this.statusBar.TabIndex = 5;
            // 
            // loginButton
            // 
            this.loginButton.Location = new System.Drawing.Point(264, 16);
            this.loginButton.Name = "loginButton";
            this.loginButton.Size = new System.Drawing.Size(75, 23);
            this.loginButton.TabIndex = 4;
            this.loginButton.Text = "> Sign in";
            this.loginButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.Location = new System.Drawing.Point(64, 40);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.PasswordChar = '*';
            this.passwordTextBox.Size = new System.Drawing.Size(184, 20);
            this.passwordTextBox.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(8, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 23);
            this.label2.TabIndex = 2;
            this.label2.Text = "Password";
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(8, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "Account";
            // 
            // accountTextBox
            // 
            this.accountTextBox.Location = new System.Drawing.Point(64, 16);
            this.accountTextBox.Name = "accountTextBox";
            this.accountTextBox.Size = new System.Drawing.Size(184, 20);
            this.accountTextBox.TabIndex = 0;
            // 
            // ContactPanel
            // 
            this.ContactPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(128)))));
            this.ContactPanel.Controls.Add(this.panel1);
            this.ContactPanel.Controls.Add(this.splitter2);
            this.ContactPanel.Controls.Add(this.propertyGrid);
            this.ContactPanel.Dock = System.Windows.Forms.DockStyle.Right;
            this.ContactPanel.Location = new System.Drawing.Point(406, 96);
            this.ContactPanel.Name = "ContactPanel";
            this.ContactPanel.Size = new System.Drawing.Size(232, 400);
            this.ContactPanel.TabIndex = 2;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.panel1.Controls.Add(this.buttonOIM);
            this.panel1.Controls.Add(this.txtMSG);
            this.panel1.Controls.Add(this.MobileMessageButton);
            this.panel1.Controls.Add(this.filetransferButton);
            this.panel1.Controls.Add(this.inviteButton);
            this.panel1.Controls.Add(this.displayImageBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 243);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(232, 157);
            this.panel1.TabIndex = 2;
            // 
            // MobileMessageButton
            // 
            this.MobileMessageButton.Location = new System.Drawing.Point(52, 128);
            this.MobileMessageButton.Name = "MobileMessageButton";
            this.MobileMessageButton.Size = new System.Drawing.Size(75, 23);
            this.MobileMessageButton.TabIndex = 5;
            this.MobileMessageButton.Text = "Send mobile";
            this.MobileMessageButton.Click += new System.EventHandler(this.MobileMessageButton_Click);
            // 
            // filetransferButton
            // 
            this.filetransferButton.Location = new System.Drawing.Point(120, 40);
            this.filetransferButton.Name = "filetransferButton";
            this.filetransferButton.Size = new System.Drawing.Size(75, 23);
            this.filetransferButton.TabIndex = 4;
            this.filetransferButton.Text = "Send file";
            this.filetransferButton.Click += new System.EventHandler(this.filetransferButton_Click);
            // 
            // inviteButton
            // 
            this.inviteButton.Location = new System.Drawing.Point(120, 8);
            this.inviteButton.Name = "inviteButton";
            this.inviteButton.Size = new System.Drawing.Size(75, 23);
            this.inviteButton.TabIndex = 3;
            this.inviteButton.Text = "Chat";
            this.inviteButton.Click += new System.EventHandler(this.inviteButton_Click);
            // 
            // displayImageBox
            // 
            this.displayImageBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.displayImageBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.displayImageBox.Location = new System.Drawing.Point(24, 8);
            this.displayImageBox.Name = "displayImageBox";
            this.displayImageBox.Size = new System.Drawing.Size(80, 89);
            this.displayImageBox.TabIndex = 2;
            this.displayImageBox.TabStop = false;
            // 
            // splitter2
            // 
            this.splitter2.BackColor = System.Drawing.SystemColors.Control;
            this.splitter2.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter2.Location = new System.Drawing.Point(0, 240);
            this.splitter2.Name = "splitter2";
            this.splitter2.Size = new System.Drawing.Size(232, 3);
            this.splitter2.TabIndex = 1;
            this.splitter2.TabStop = false;
            // 
            // propertyGrid
            // 
            this.propertyGrid.BackColor = System.Drawing.SystemColors.Control;
            this.propertyGrid.CommandsBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(128)))));
            this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Top;
            this.propertyGrid.LineColor = System.Drawing.SystemColors.ScrollBar;
            this.propertyGrid.Location = new System.Drawing.Point(0, 0);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.Size = new System.Drawing.Size(232, 240);
            this.propertyGrid.TabIndex = 0;
            // 
            // pictureBox
            // 
            this.pictureBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.pictureBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.pictureBox.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox.Image")));
            this.pictureBox.Location = new System.Drawing.Point(0, 0);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(638, 96);
            this.pictureBox.TabIndex = 5;
            this.pictureBox.TabStop = false;
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Right;
            this.splitter1.Location = new System.Drawing.Point(403, 96);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(3, 400);
            this.splitter1.TabIndex = 3;
            this.splitter1.TabStop = false;
            // 
            // openFileDialog
            // 
            this.openFileDialog.Multiselect = true;
            // 
            // openImageDialog
            // 
            this.openImageDialog.Filter = "PNG Images|*.png";
            this.openImageDialog.Multiselect = true;
            this.openImageDialog.Title = "Select display image";
            // 
            // tmrKeepOnLine
            // 
            this.tmrKeepOnLine.Interval = 1000;
            this.tmrKeepOnLine.Tick += new System.EventHandler(this.tmrKeepOnLine_Tick);
            // 
            // txtMSG
            // 
            this.txtMSG.Location = new System.Drawing.Point(24, 103);
            this.txtMSG.Name = "txtMSG";
            this.txtMSG.Size = new System.Drawing.Size(184, 20);
            this.txtMSG.TabIndex = 6;
            this.txtMSG.Text = "MSNPSharp Message";
            // 
            // buttonOIM
            // 
            this.buttonOIM.Location = new System.Drawing.Point(133, 128);
            this.buttonOIM.Name = "buttonOIM";
            this.buttonOIM.Size = new System.Drawing.Size(75, 23);
            this.buttonOIM.TabIndex = 7;
            this.buttonOIM.Text = "Send OIM";
            this.buttonOIM.Click += new System.EventHandler(this.buttonOIM_Click);
            // 
            // ClientForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(638, 600);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.ListPanel);
            this.Controls.Add(this.ContactPanel);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.OwnerPanel);
            this.Name = "ClientForm";
            this.Text = "MSNPSharp Example Client";
            this.ListPanel.ResumeLayout(false);
            this.OwnerPanel.ResumeLayout(false);
            this.OwnerPanel.PerformLayout();
            this.ContactPanel.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.displayImageBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new ClientForm());
		}

        /// <summary>
        /// A delegate passed to Invoke in order to create the conversation form in the thread of the main form.
        /// </summary>
        private delegate void SetStatusDelegate(string status);

        private void SetStatusSynchronized(string status)
        {
            statusBar.Text = status;
        }

		private void SetStatus(string status)
		{
            this.Invoke(new SetStatusDelegate(SetStatusSynchronized), new object[] { status });            
		}

		/// <summary>
		/// Sign into the messenger network. Disconnect first if a connection has already been established.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button1_Click(object sender, System.EventArgs e)
		{			
			if(messenger.Connected)
			{
				SetStatus("Disconnecting from server");
				messenger.Disconnect();
			}

			// set the credentials, this is ofcourse something every MSNPSharp program will need to
			// implement.
			messenger.Credentials.Account = accountTextBox.Text;
			messenger.Credentials.Password = passwordTextBox.Text;

			// inform the user what is happening and try to connecto to the messenger network.			
			SetStatus("Connecting to server");
			messenger.Connect();

			// note that Messenger.Connect() will run in a seperate thread and return immediately.
			// it will fire events that informs you about the status of the connection attempt. 
			// these events are registered in the constructor.
		}

		private void NameserverProcessor_ConnectionEstablished(object sender, EventArgs e)
		{
			SetStatus("Connected to server");
		}

		private void Nameserver_SignedIn(object sender, EventArgs e)
		{
			SetStatus("Signed into the messenger network as " + messenger.Owner.Name );            

            // set our presence status
			messenger.Owner.Status = PresenceStatus.Online;
            
            Invoke(new UpdateContactlistDelegate(UpdateContactlist));
		}

		private void Nameserver_SignedOff(object sender, SignedOffEventArgs e)
		{
			SetStatus("Signed off from the messenger network");
            tmrKeepOnLine.Enabled = false;
		}

		private void Nameserver_ExceptionOccurred(object sender, ExceptionEventArgs e)
		{
			// ignore the unauthorized exception, since we're handling that error in another method.
			if(e.Exception is UnauthorizedException)
				return;

			MessageBox.Show(e.Exception.ToString(), "Nameserver exception");
		}

		private void NameserverProcessor_ConnectingException(object sender, ExceptionEventArgs e)
		{
			MessageBox.Show(e.Exception.ToString(), "Connecting exception");
			SetStatus("Connecting failed");
		}

		private void Nameserver_AuthenticationError(object sender, ExceptionEventArgs e)
		{
			MessageBox.Show("Authentication failed, check your account or password.", "Authentication failed");
			SetStatus("Authentication failed");
		}

		/// <summary>
		/// Sign off from the messenger network by disconnecting.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button1_Click_1(object sender, System.EventArgs e)
		{
			if(messenger.Connected)
				messenger.Disconnect();
		}

        /// <summary>
        /// Used to invoke UpdateContactlist
        /// </summary>
        private delegate void UpdateContactlistDelegate();

		/// <summary>
		/// Updates the listview.
		/// </summary>
		private void UpdateContactlist()
		{
			if(messenger.Connected == false)
				return;
			
			ContactListView.SuspendLayout();			
			ContactListView.Items.Clear();			

			foreach(Contact contact in messenger.ContactList.All)
			{
				ListViewItem item = new ListViewItem();
				item.Text = contact.Name;				
				item.Tag = contact;				
				ContactListView.Items.Add(item);
			}						
			
			ContactListView.ResumeLayout();
            tmrKeepOnLine.Enabled = true;
		}

		/// <summary>
		/// Shows the properties of the selected contact and downloads the display image.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ContactListView_ItemActivate(object sender, System.EventArgs e)
		{
			if(ContactListView.SelectedItems.Count == 0)
				return;

			Contact selectedContact = (Contact)ContactListView.SelectedItems[0].Tag;
			this.propertyGrid.SelectedObject = selectedContact;

			// request the image, if not already available
			if(selectedContact.Status != PresenceStatus.Offline && selectedContact.DisplayImage != null)
			{
				if(selectedContact.DisplayImage.Image == null)
				{
                    try
                    {
                        // create a MSNSLPHandler. This handler takes care of the filetransfer protocol.
                        // The MSNSLPHandler makes use of the underlying P2P framework.					
                        MSNSLPHandler msnslpHandler = messenger.GetMSNSLPHandler(selectedContact.Mail);

                        // by sending an invitation a P2PTransferSession is automatically created.
                        // the session object takes care of the actual data transfer to the remote client,
                        // in contrast to the msnslpHandler object, which only deals with the protocol chatting.
                        P2PTransferSession session = msnslpHandler.SendInvitation(messenger.Owner.Mail, selectedContact.Mail, selectedContact.DisplayImage);

                        // as usual, via events we want to be notified when a transfer is finished.
                        // ofcourse, optionally, you can also catch abort and error events.
                        session.TransferFinished += new EventHandler(session_TransferFinished);
                        session.ClientData = selectedContact.DisplayImage;
                    }
                    catch (Exception)
                    {
                    }
				}
				else
					displayImageBox.Image = selectedContact.DisplayImage.Image;
			}
		}

		/// <summary>
		/// Notifies the user of errors which are send by the MSN server.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Nameserver_ServerErrorReceived(object sender, MSNErrorEventArgs e)
		{
			// when the MSN server sends an error code we want to be notified.
			MessageBox.Show(e.MSNError.ToString(), "Server error received");
			SetStatus("Server error received");
		}

		/// <summary>
		/// Displays the retrieved image in the image box on the form.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void session_TransferFinished(object sender, EventArgs e)
		{
			P2PTransferSession session = (P2PTransferSession)sender;
			DisplayImage image = (DisplayImage)session.ClientData;			
			image.RetrieveImage();

			if(image.Image != null)
				displayImageBox.Image = image.Image;
		}

		/// <summary>
		/// Creates a conversation and invites the selected contact.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void inviteButton_Click(object sender, System.EventArgs e)
		{
			if(ContactListView.SelectedItems.Count == 0)
				return;

			Contact selectedContact = (Contact)ContactListView.SelectedItems[0].Tag;
			if(selectedContact != null && selectedContact.Online == true)
			{
				Conversation conversation = messenger.CreateConversation();
                conversation.Invite(selectedContact);
			    ConversationForm form = CreateConversationForm(conversation);
						
				
				form.Show();
			}
		}

		/// <summary>
		/// A delegate passed to Invoke in order to create the conversation form in the thread of the main form.
		/// </summary>
		private delegate ConversationForm CreateConversationDelegate(Conversation conversation);

		private ConversationForm CreateConversationForm(Conversation conversation)
		{
			// create a new conversation. However do not show the window untill a message is received.
			// for example, a conversation will be created when the remote client sends wants to send
			// you a file. You don't want to show the conversation form in that case.
			ConversationForm conversationForm = new ConversationForm(conversation);
			// do this to create the window handle. Otherwise we are not able to call Invoke() on the
			// conversation form later.
			conversationForm.Handle.ToInt32();					

			return conversationForm;
		}

		private void messenger_ConversationCreated(object sender, ConversationCreatedEventArgs e)
		{
			// check if the request is initiated remote or by this object
			// if it is initiated remote then we have to create a conversation form. Otherwise the 
			// form is already created and we don't need to create another one.
			if(e.Initiator == null)
			{				
				// use the invoke method to create the form in the main thread
				this.Invoke(new CreateConversationDelegate(CreateConversationForm), new object[] { e.Conversation });
			}
		}

		/// <summary>
		/// Sends a file to the selected remote contact.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void filetransferButton_Click(object sender, System.EventArgs e)
		{
			if(ContactListView.SelectedItems.Count == 0)
				return;

			Contact selectedContact = (Contact)ContactListView.SelectedItems[0].Tag;
			this.propertyGrid.SelectedObject = selectedContact;
			// open a dialog box to select the file
			if(selectedContact.Online && openFileDialog.ShowDialog() == DialogResult.OK)
			{
                try
                {
                    foreach (string filename in openFileDialog.FileNames)
                    {
                        MSNSLPHandler msnslpHandler = messenger.GetMSNSLPHandler(selectedContact.Mail);
                        FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                        P2PTransferSession session = msnslpHandler.SendInvitation(messenger.Owner.Mail, selectedContact.Mail, Path.GetFileName(filename), fileStream);
                    }
                }
                catch (MSNPSharpException ex)
                {
                    MessageBox.Show(ex.Message);
                }
			}
		}

        private delegate DialogResult ShowFileDialogDelegate(FileDialog dialog);

        private DialogResult ShowFileDialog(FileDialog dialog)
        {
            return dialog.ShowDialog();
        }

		/// <summary>
		/// Asks the user to accept or deny the incoming filetransfer invitation.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void messenger_TransferInvitationReceived(object sender, MSNSLPInvitationEventArgs e)
		{			
			if(MessageBox.Show(
				messenger.ContactList[e.TransferProperties.RemoteContact].Name + 
				" wants to send you a file.\r\nFilename: " + 
				e.Filename + "\r\nLength (bytes): " + e.FileSize, 
				"Filetransfer invitation",
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				// by setting the Accept property in the EventArgs to true we give the transfer a green light				
				saveFileDialog.FileName = e.Filename;
                if((DialogResult)Invoke(new ShowFileDialogDelegate(ShowFileDialog), new object[] { saveFileDialog }) == DialogResult.OK)				
				{
					e.TransferSession.DataStream = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write);
					//e.Handler.AcceptTransfer (e);
                    e.Accept = true;
				}
			}
		}

		private void MobileMessageButton_Click(object sender, System.EventArgs e)
		{
			if(ContactListView.SelectedItems.Count == 0)
				return;

			Contact selectedContact = (Contact)ContactListView.SelectedItems[0].Tag;
			this.propertyGrid.SelectedObject = selectedContact;

			// open a dialog box to select the file
			if(selectedContact.Online == false && selectedContact.MobileAccess == true)
			{										
				messenger.Nameserver.SendMobileMessage(selectedContact, txtMSG.Text);
			}
			else
				MessageBox.Show("This contact is not able to receive mobile messages");
		}

        /// <summary>
        /// Change the current owner's display image.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void changeDisplayButton_Click(object sender, EventArgs e)
        {
            messenger.Owner.DisplayImage.FileLocation = @"C:\icon.png";
            //messenger.Owner.BroadcastDisplayImage();

            if (openImageDialog.ShowDialog() == DialogResult.OK)
            {
                Image fileImage = Image.FromFile(openImageDialog.FileName);
                DisplayImage displayImage = new DisplayImage();
                displayImage.Image = fileImage;
                messenger.Owner.DisplayImage = displayImage;
            }
        }

        private int nextPing = 50;
        private void tmrKeepOnLine_Tick(object sender, EventArgs e)
        {
            if (nextPing > 0)
                nextPing--;
            if (nextPing == 0)
                messenger.Nameserver.SendPing();
        }

        private void buttonOIM_Click(object sender, EventArgs e)
        {
            if (ContactListView.SelectedItems.Count == 0)
                return;

            Contact selectedContact = (Contact)ContactListView.SelectedItems[0].Tag;
            this.propertyGrid.SelectedObject = selectedContact;

            messenger.Nameserver.SendOIMMessage(selectedContact.Mail, txtMSG.Text);
        }
	}
}