using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Diagnostics;


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
        private Panel panel2;
        private RtfRichTextBox richTextHistory;
        private PictureBox displayOwner;
        private PictureBox displayUser;

        private Conversation _conversation = null;
        private ClientForm _clientform = null;
        private ToolStripDropDown emotionDropDown = new ToolStripDropDown();
        private List<string> _contacts = new List<string>(0);
        private Contact _firstInvitedContact;
        private OpenFileDialog openFileDialog;
        private ToolStrip tsMessage;
        private ToolStripComboBox cbMessageFontName;
        private ToolStripComboBox cbMessageFontSize;
        private ToolStripButton bMessageFontColor;
        private ToolStripSeparator tssMessageSeperator1;
        private ToolStripButton bMessageBold;
        private ToolStripButton bMessageItalic;
        private ToolStripButton bMessageUnderline;
        private ToolStripSeparator tssMessageSeperator2;
        private ToolStripButton bMessageSend;
        private ToolStripButton bMessageInsertEmoticon;
        private ToolStripButton bMessageSendNudge;
        private ToolStripButton bMessageSendCustomEmoticon;
        private ToolStripButton bMessageSendFiles;
        private ColorDialog dlgColor;
        

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

            if (contact == null && conversation.Contacts.Count > 0) // Received nudge :(
            {
                contact = conversation.Contacts[0];
            }

            _conversation = conversation;
            AddEvent();

            _clientform = clientform;
            _firstInvitedContact = contact;

            if (contact != null)
            {
                _conversation.Invite(contact);

                // request the image, if not already available
                if (contact.Status != PresenceStatus.Offline && contact.DisplayImage != null)
                {
                    if (contact.DisplayImage.Image == null)
                    {
                        try
                        {
                            if (contact.ClientType == ClientType.PassportMember)
                            {
                                // create a MSNSLPHandler. This handler takes care of the filetransfer protocol.
                                // The MSNSLPHandler makes use of the underlying P2P framework.					
                                MSNSLPHandler msnslpHandler = conversation.Messenger.GetMSNSLPHandler(contact.Mail);

                                // by sending an invitation a P2PTransferSession is automatically created.
                                // the session object takes care of the actual data transfer to the remote client,
                                // in contrast to the msnslpHandler object, which only deals with the protocol chatting.
                                P2PTransferSession session = msnslpHandler.SendInvitation(conversation.Messenger.Owner.Mail, contact.Mail, contact.DisplayImage);

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

                                session.ClientData = contact.DisplayImage;
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                    else
                        displayUser.Image = contact.DisplayImage.Image;
                }
            }            
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConversationForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.tsMessage = new System.Windows.Forms.ToolStrip();
            this.cbMessageFontName = new System.Windows.Forms.ToolStripComboBox();
            this.cbMessageFontSize = new System.Windows.Forms.ToolStripComboBox();
            this.bMessageFontColor = new System.Windows.Forms.ToolStripButton();
            this.tssMessageSeperator1 = new System.Windows.Forms.ToolStripSeparator();
            this.bMessageBold = new System.Windows.Forms.ToolStripButton();
            this.bMessageItalic = new System.Windows.Forms.ToolStripButton();
            this.bMessageUnderline = new System.Windows.Forms.ToolStripButton();
            this.tssMessageSeperator2 = new System.Windows.Forms.ToolStripSeparator();
            this.bMessageSend = new System.Windows.Forms.ToolStripButton();
            this.bMessageInsertEmoticon = new System.Windows.Forms.ToolStripButton();
            this.bMessageSendNudge = new System.Windows.Forms.ToolStripButton();
            this.bMessageSendCustomEmoticon = new System.Windows.Forms.ToolStripButton();
            this.bMessageSendFiles = new System.Windows.Forms.ToolStripButton();
            this.displayOwner = new System.Windows.Forms.PictureBox();
            this.inputTextBox = new System.Windows.Forms.TextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.displayUser = new System.Windows.Forms.PictureBox();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.dlgColor = new System.Windows.Forms.ColorDialog();
            this.richTextHistory = new MSNPSharpClient.RtfRichTextBox();
            this.panel1.SuspendLayout();
            this.tsMessage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.displayOwner)).BeginInit();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.displayUser)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.panel1.Controls.Add(this.tsMessage);
            this.panel1.Controls.Add(this.displayOwner);
            this.panel1.Controls.Add(this.inputTextBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 238);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(649, 110);
            this.panel1.TabIndex = 0;
            // 
            // tsMessage
            // 
            this.tsMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tsMessage.AutoSize = false;
            this.tsMessage.Dock = System.Windows.Forms.DockStyle.None;
            this.tsMessage.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsMessage.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cbMessageFontName,
            this.cbMessageFontSize,
            this.bMessageFontColor,
            this.tssMessageSeperator1,
            this.bMessageBold,
            this.bMessageItalic,
            this.bMessageUnderline,
            this.tssMessageSeperator2,
            this.bMessageSend,
            this.bMessageInsertEmoticon,
            this.bMessageSendNudge,
            this.bMessageSendCustomEmoticon,
            this.bMessageSendFiles});
            this.tsMessage.Location = new System.Drawing.Point(109, 3);
            this.tsMessage.Name = "tsMessage";
            this.tsMessage.Padding = new System.Windows.Forms.Padding(3, 0, 1, 0);
            this.tsMessage.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.tsMessage.Size = new System.Drawing.Size(537, 25);
            this.tsMessage.TabIndex = 8;
            // 
            // cbMessageFontName
            // 
            this.cbMessageFontName.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cbMessageFontName.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbMessageFontName.DropDownWidth = 200;
            this.cbMessageFontName.MaxDropDownItems = 12;
            this.cbMessageFontName.Name = "cbMessageFontName";
            this.cbMessageFontName.Size = new System.Drawing.Size(140, 25);
            this.cbMessageFontName.ToolTipText = "Font Name";
            this.cbMessageFontName.SelectedIndexChanged += new System.EventHandler(this.cbMessageFontName_SelectedIndexChanged);
            this.cbMessageFontName.Validating += new System.ComponentModel.CancelEventHandler(this.cbMessageFontName_Validating);
            this.cbMessageFontName.Validated += new System.EventHandler(this.cbMessageFontName_Validated);
            // 
            // cbMessageFontSize
            // 
            this.cbMessageFontSize.Items.AddRange(new object[] {
            "8",
            "9",
            "10",
            "11",
            "12",
            "14",
            "16",
            "18",
            "20",
            "22",
            "24",
            "26",
            "28",
            "36",
            "48",
            "72"});
            this.cbMessageFontSize.MaxDropDownItems = 12;
            this.cbMessageFontSize.Name = "cbMessageFontSize";
            this.cbMessageFontSize.Size = new System.Drawing.Size(75, 25);
            this.cbMessageFontSize.ToolTipText = "Font Size";
            this.cbMessageFontSize.SelectedIndexChanged += new System.EventHandler(this.cbMessageFontSize_SelectedIndexChanged);
            this.cbMessageFontSize.Validating += new System.ComponentModel.CancelEventHandler(this.cbMessageFontSize_Validating);
            this.cbMessageFontSize.Validated += new System.EventHandler(this.cbMessageFontSize_Validated);
            // 
            // bMessageFontColor
            // 
            this.bMessageFontColor.Image = ((System.Drawing.Image)(resources.GetObject("bMessageFontColor.Image")));
            this.bMessageFontColor.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bMessageFontColor.Name = "bMessageFontColor";
            this.bMessageFontColor.Size = new System.Drawing.Size(56, 22);
            this.bMessageFontColor.Text = "Color";
            this.bMessageFontColor.ToolTipText = "Font Color";
            this.bMessageFontColor.Click += new System.EventHandler(this.bMessageFontColor_Click);
            // 
            // tssMessageSeperator1
            // 
            this.tssMessageSeperator1.Name = "tssMessageSeperator1";
            this.tssMessageSeperator1.Size = new System.Drawing.Size(6, 25);
            // 
            // bMessageBold
            // 
            this.bMessageBold.CheckOnClick = true;
            this.bMessageBold.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.bMessageBold.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bMessageBold.Image = ((System.Drawing.Image)(resources.GetObject("bMessageBold.Image")));
            this.bMessageBold.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bMessageBold.Name = "bMessageBold";
            this.bMessageBold.Size = new System.Drawing.Size(23, 22);
            this.bMessageBold.Text = "B";
            this.bMessageBold.ToolTipText = "Bold";
            this.bMessageBold.CheckedChanged += new System.EventHandler(this.bMessageBoldItalicUnderline_CheckedChanged);
            // 
            // bMessageItalic
            // 
            this.bMessageItalic.CheckOnClick = true;
            this.bMessageItalic.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.bMessageItalic.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bMessageItalic.Image = ((System.Drawing.Image)(resources.GetObject("bMessageItalic.Image")));
            this.bMessageItalic.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bMessageItalic.Name = "bMessageItalic";
            this.bMessageItalic.Size = new System.Drawing.Size(23, 22);
            this.bMessageItalic.Text = "I";
            this.bMessageItalic.ToolTipText = "Italic";
            this.bMessageItalic.CheckedChanged += new System.EventHandler(this.bMessageBoldItalicUnderline_CheckedChanged);
            // 
            // bMessageUnderline
            // 
            this.bMessageUnderline.CheckOnClick = true;
            this.bMessageUnderline.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.bMessageUnderline.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bMessageUnderline.Image = ((System.Drawing.Image)(resources.GetObject("bMessageUnderline.Image")));
            this.bMessageUnderline.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bMessageUnderline.Name = "bMessageUnderline";
            this.bMessageUnderline.Size = new System.Drawing.Size(23, 22);
            this.bMessageUnderline.Text = "U";
            this.bMessageUnderline.ToolTipText = "Underline";
            this.bMessageUnderline.CheckedChanged += new System.EventHandler(this.bMessageBoldItalicUnderline_CheckedChanged);
            // 
            // tssMessageSeperator2
            // 
            this.tssMessageSeperator2.Name = "tssMessageSeperator2";
            this.tssMessageSeperator2.Size = new System.Drawing.Size(6, 25);
            // 
            // bMessageSend
            // 
            this.bMessageSend.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.bMessageSend.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.bMessageSend.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.bMessageSend.Image = ((System.Drawing.Image)(resources.GetObject("bMessageSend.Image")));
            this.bMessageSend.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bMessageSend.Name = "bMessageSend";
            this.bMessageSend.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
            this.bMessageSend.Size = new System.Drawing.Size(57, 22);
            this.bMessageSend.Text = "   &Send   ";
            this.bMessageSend.Click += new System.EventHandler(this.bMessageSend_Click);
            // 
            // bMessageInsertEmoticon
            // 
            this.bMessageInsertEmoticon.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bMessageInsertEmoticon.Image = global::MSNPSharpClient.Properties.Resources.smiley;
            this.bMessageInsertEmoticon.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.bMessageInsertEmoticon.ImageTransparentColor = System.Drawing.Color.White;
            this.bMessageInsertEmoticon.Name = "bMessageInsertEmoticon";
            this.bMessageInsertEmoticon.Size = new System.Drawing.Size(23, 22);
            this.bMessageInsertEmoticon.Text = "Insert an &emoticon";
            this.bMessageInsertEmoticon.Click += new System.EventHandler(this.bMessageInsertEmoticon_Click);
            // 
            // bMessageSendNudge
            // 
            this.bMessageSendNudge.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bMessageSendNudge.Image = global::MSNPSharpClient.Properties.Resources.nudge;
            this.bMessageSendNudge.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.bMessageSendNudge.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bMessageSendNudge.Name = "bMessageSendNudge";
            this.bMessageSendNudge.Size = new System.Drawing.Size(28, 22);
            this.bMessageSendNudge.Text = "Send a &nudge";
            this.bMessageSendNudge.Click += new System.EventHandler(this.bMessageSendNudge_Click);
            // 
            // bMessageSendCustomEmoticon
            // 
            this.bMessageSendCustomEmoticon.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bMessageSendCustomEmoticon.Image = ((System.Drawing.Image)(resources.GetObject("bMessageSendCustomEmoticon.Image")));
            this.bMessageSendCustomEmoticon.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bMessageSendCustomEmoticon.Name = "bMessageSendCustomEmoticon";
            this.bMessageSendCustomEmoticon.Size = new System.Drawing.Size(23, 22);
            this.bMessageSendCustomEmoticon.Text = "toolStripButton1";
            this.bMessageSendCustomEmoticon.ToolTipText = "Custom Emoticon";
            this.bMessageSendCustomEmoticon.Click += new System.EventHandler(this.bMessageSendCustomEmoticon_Click);
            // 
            // bMessageSendFiles
            // 
            this.bMessageSendFiles.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bMessageSendFiles.Image = global::MSNPSharpClient.Properties.Resources.file;
            this.bMessageSendFiles.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bMessageSendFiles.Name = "bMessageSendFiles";
            this.bMessageSendFiles.Size = new System.Drawing.Size(23, 22);
            this.bMessageSendFiles.Text = "toolStripButton2";
            this.bMessageSendFiles.ToolTipText = "Send Files";
            this.bMessageSendFiles.Click += new System.EventHandler(this.bMessageSendFiles_Click);
            // 
            // displayOwner
            // 
            this.displayOwner.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.displayOwner.BackColor = System.Drawing.Color.White;
            this.displayOwner.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.displayOwner.Location = new System.Drawing.Point(3, 3);
            this.displayOwner.Name = "displayOwner";
            this.displayOwner.Size = new System.Drawing.Size(100, 100);
            this.displayOwner.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.displayOwner.TabIndex = 0;
            this.displayOwner.TabStop = false;
            // 
            // inputTextBox
            // 
            this.inputTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.inputTextBox.Font = new System.Drawing.Font("Trebuchet MS", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.inputTextBox.Location = new System.Drawing.Point(109, 31);
            this.inputTextBox.Multiline = true;
            this.inputTextBox.Name = "inputTextBox";
            this.inputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.inputTextBox.Size = new System.Drawing.Size(537, 72);
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
            this.panel2.Size = new System.Drawing.Size(649, 238);
            this.panel2.TabIndex = 0;
            // 
            // displayUser
            // 
            this.displayUser.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.displayUser.BackColor = System.Drawing.Color.White;
            this.displayUser.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.displayUser.Location = new System.Drawing.Point(3, 132);
            this.displayUser.Name = "displayUser";
            this.displayUser.Size = new System.Drawing.Size(100, 100);
            this.displayUser.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.displayUser.TabIndex = 0;
            this.displayUser.TabStop = false;
            // 
            // openFileDialog
            // 
            this.openFileDialog.Multiselect = true;
            // 
            // richTextHistory
            // 
            this.richTextHistory.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextHistory.BackColor = System.Drawing.Color.Snow;
            this.richTextHistory.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextHistory.HiglightColor = MSNPSharpClient.RtfRichTextBox.RtfColor.White;
            this.richTextHistory.Location = new System.Drawing.Point(109, 3);
            this.richTextHistory.Name = "richTextHistory";
            this.richTextHistory.ReadOnly = true;
            this.richTextHistory.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.richTextHistory.Size = new System.Drawing.Size(540, 229);
            this.richTextHistory.TabIndex = 0;
            this.richTextHistory.TabStop = false;
            this.richTextHistory.Text = "";
            this.richTextHistory.TextColor = MSNPSharpClient.RtfRichTextBox.RtfColor.Black;
            // 
            // ConversationForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(649, 348);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "ConversationForm";
            this.Text = "Conversation - MSNPSharp";
            this.Load += new System.EventHandler(this.ConversationForm_Load);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.ConversationForm_Closing);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.tsMessage.ResumeLayout(false);
            this.tsMessage.PerformLayout();
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
            if (InvokeRequired)
            {
                Invoke(new EventHandler<MSNObjectDataTransferCompletedEventArgs>(Conversation_MSNObjectDataTransferCompleted), sender, e);
                return;
            }

            Emoticon emo = e.ClientData as Emoticon;
            if (emo != null)
            {
                MemoryStream ms = new MemoryStream();
                byte[] byt = new byte[e.ClientData.OpenStream().Length];
                e.ClientData.OpenStream().Seek(0, SeekOrigin.Begin);
                e.ClientData.OpenStream().Read(byt, 0, byt.Length);
                ms.Write(byt, 0, byt.Length);

                richTextHistory.Emotions[emo.Shortcut] = new Bitmap(Image.FromStream(ms));

                ms.Close();
            }

            while (richTextHistory.HasEmotion)
            {
                richTextHistory.InsertEmotion();
            }
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
                    bMessageSend.PerformClick();
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

            PrintText(e.Sender, e.Message);
        }

        private void PrintNudge(object sender, ContactEventArgs e)
        {
            DisplaySystemMessage(e.Contact.Name + " has sent a nudge!");
            PerformNudge();
        }

        public void DisplaySystemMessage(string systemMessage)
        {
            richTextHistory.SelectionColor = Color.Red;
            richTextHistory.SelectionFont = new Font("Verdana", 8f, FontStyle.Bold);
            richTextHistory.SelectionIndent = 30;
            richTextHistory.AppendText("* " + systemMessage + " *");
            richTextHistory.SelectionColor = Color.Black;
            richTextHistory.SelectionIndent = 0;
            richTextHistory.SelectionFont = new Font("Verdana", 8f);
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
                DisplaySystemMessage("Session was closed");
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
                DisplaySystemMessage(e.Contact.Name + " joined the conversation");
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
                DisplaySystemMessage(e.Contact.Name + " left the conversation");
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

        private void PrintText(Contact c, TextMessage message)
        {
            richTextHistory.SelectionColor = Color.Navy;
            richTextHistory.SelectionIndent = 0;
            richTextHistory.AppendText("[" + DateTime.Now.ToLongTimeString() + "]" + " ");
            richTextHistory.SelectionColor = c.Mail == _conversation.Messenger.Owner.Mail ? Color.Blue : Color.Black;
            richTextHistory.AppendText(c.Name + " <" + c.Mail + ">" + Environment.NewLine);
            richTextHistory.SelectionColor = message.Color;
            richTextHistory.SelectionIndent = 10;
            richTextHistory.AppendText(message.Text);
            richTextHistory.AppendText(Environment.NewLine);
            richTextHistory.ScrollToCaret();

            while (richTextHistory.HasEmotion)
            {
                richTextHistory.InsertEmotion();
            }
        }

        private Image CreateImageFromColor(Color color, Size buttonSize)
        {
            Bitmap bitmap = new Bitmap(buttonSize.Width, buttonSize.Height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(color);
            }
            return bitmap;
        }

        private void UpdateTextFonts()
        {
            FontStyle fontStyle = FontStyle.Regular;

            if (bMessageBold.Checked)
                fontStyle |= FontStyle.Bold;
            if (bMessageItalic.Checked)
                fontStyle |= FontStyle.Italic;
            if (bMessageUnderline.Checked)
                fontStyle |= FontStyle.Underline;

            Single fontSize = Single.Parse(cbMessageFontSize.Text);
            Font messageFont = null;

        CreateFont:
            try
            {
                messageFont = new Font(cbMessageFontName.Text, fontSize, fontStyle, GraphicsUnit.Point);
            }
            catch (ArgumentException)
            {
                fontStyle++;
                if (fontStyle <= (FontStyle.Strikeout | FontStyle.Underline | FontStyle.Italic | FontStyle.Bold))
                    goto CreateFont;
            }

            if (messageFont == null)
                return;

            bMessageBold.Checked = (fontStyle & FontStyle.Bold) != FontStyle.Regular;
            bMessageItalic.Checked = (fontStyle & FontStyle.Italic) != FontStyle.Regular;
            bMessageUnderline.Checked = (fontStyle & FontStyle.Underline) != FontStyle.Regular;

            richTextHistory.Font = new Font(richTextHistory.Font.FontFamily, messageFont.Size);
            inputTextBox.Font = messageFont;
        }

        private void ConversationForm_Load(object sender, EventArgs e)
        {
            Text = "Conversation with " + _firstInvitedContact.Mail + " - MSNPSharp";
            Icon = (Icon)((_firstInvitedContact.ClientType == ClientType.PassportMember) ? Properties.Resources.msn_ico : Properties.Resources.yahoo_ico);
            displayOwner.Image = _clientform.Messenger.Owner.DisplayImage.Image;

            lock (richTextHistory.Emotions)
            {
                richTextHistory.Emotions[":)"] = Properties.Resources.smiley;
                richTextHistory.Emotions[":d"] = Properties.Resources.biggrin;
                richTextHistory.Emotions[":("] = Properties.Resources.sad;
                richTextHistory.Emotions[";)"] = Properties.Resources.wink;
                richTextHistory.Emotions[":p"] = Properties.Resources.tongueout;
            }

            emotionDropDown.ImageScalingSize = new Size(15, 15);
            emotionDropDown.LayoutStyle = ToolStripLayoutStyle.Table;
            foreach (string str in richTextHistory.Emotions.Keys)
            {
                emotionDropDown.Items.Add(null, richTextHistory.Emotions[str], emotionDropDown_Click).ToolTipText = str;
            }
            ((TableLayoutSettings)emotionDropDown.LayoutSettings).ColumnCount = 3;


            foreach (FontFamily ff in FontFamily.Families)
            {
                cbMessageFontName.Items.Add(ff.Name);
            }
            cbMessageFontName.Text = inputTextBox.Font.Name;
            cbMessageFontSize.Text = inputTextBox.Font.Size.ToString();
            bMessageFontColor.Image = CreateImageFromColor(inputTextBox.ForeColor, inputTextBox.Size);

            cbMessageFontName.Tag = cbMessageFontName.Text;
            cbMessageFontSize.Tag = cbMessageFontSize.Text;

            UpdateTextFonts();

            inputTextBox.Select();
        }


        private void emotionDropDown_Click(object sender, EventArgs args)
        {
            ToolStripItem item = (ToolStripItem)sender;
            inputTextBox.AppendText(item.ToolTipText);
            inputTextBox.Focus();
        }

        private void PerformNudge()
        {
            Stopwatch stopwatch = new Stopwatch();
            
            Random rnd = new Random();
            int x = Left;
            int y = Top;

            stopwatch.Start();
            while (stopwatch.ElapsedMilliseconds < 500)
            {
                Left = rnd.Next(Location.X - 5, Location.X + 5);
                Top = rnd.Next(Location.Y - 1, Location.Y + 1);

                System.Threading.Thread.Sleep(10);
                Application.DoEvents();
            }
            stopwatch.Stop();

            Left = x;
            Top = y;
        }

        private void bMessageSend_Click(object sender, EventArgs e)
        {
            if (inputTextBox.Text.Length == 0)
                return;

            TextMessage message = new TextMessage(inputTextBox.Text);
            message.Font = inputTextBox.Font.Name;
            message.Color = inputTextBox.ForeColor;

            if (inputTextBox.Font.Bold)
                message.Decorations |= TextDecorations.Bold;
            if (inputTextBox.Font.Italic)
                message.Decorations |= TextDecorations.Italic;
            if (inputTextBox.Font.Underline)
                message.Decorations |= TextDecorations.Underline;

            PrintText(_conversation.Messenger.Owner, message);

            inputTextBox.Clear();
            inputTextBox.Focus();

            Conversation.SendTextMessage(message);
        }

        private void bMessageInsertEmoticon_Click(object sender, EventArgs e)
        {
            int x = Location.X + richTextHistory.Location.X + bMessageInsertEmoticon.Width;
            int y = Location.Y + richTextHistory.Height + emotionDropDown.Height + 20;
            emotionDropDown.Show(x, y);
            emotionDropDown.Focus();
        }

        private void bMessageSendNudge_Click(object sender, EventArgs e)
        {
            Conversation.SendNudge();
            DisplaySystemMessage("You send a nudge.");
            PerformNudge();
        }

        private void bMessageSendFiles_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                List<Contact> contacts = new List<Contact>();
                foreach (Contact contact in _conversation.Contacts)
                {
                    if (contact.Online && contact.ClientType == ClientType.PassportMember && contact.Mail != Conversation.Messenger.Owner.Mail)
                    {
                        contacts.Add(contact);
                    }
                }
                if (contacts.Count == 0 && _firstInvitedContact != null && _firstInvitedContact.ClientType == ClientType.PassportMember && _firstInvitedContact.Online)
                {
                    contacts.Add(_firstInvitedContact);
                }

                if (contacts.Count == 0)
                {
                    DisplaySystemMessage("All contacts are offline or this contact doesn't support receiving files.");
                    return;
                }

                try
                {
                    foreach (Contact contact in contacts)
                    {
                        foreach (string filename in openFileDialog.FileNames)
                        {
                            MSNSLPHandler msnslpHandler = Conversation.Messenger.GetMSNSLPHandler(contact.Mail);
                            FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                            P2PTransferSession session = msnslpHandler.SendInvitation(Conversation.Messenger.Owner.Mail, contact.Mail, Path.GetFileName(filename), fileStream);
                        }
                    }
                }
                catch (MSNPSharpException ex)
                {
                    MessageBox.Show(ex.Message);
                }



            }
        }

        private void bMessageSendCustomEmoticon_Click(object sender, EventArgs e)
        {
            MemoryStream mem = new MemoryStream();
            Properties.Resources.inner_emoticon.Save(mem, ImageFormat.Png);
            Emoticon emotest = new Emoticon(_clientform.Messenger.Owner.Mail, mem, "0", "test_em");
            MSNObjectCatalog.GetInstance().Add(emotest);
            List<Emoticon> emolist = new List<Emoticon>();
            emolist.Add(emotest);

            if (!richTextHistory.Emotions.ContainsKey(emotest.Shortcut))
            {
                richTextHistory.Emotions[emotest.Shortcut] = Properties.Resources.inner_emoticon;
            }

            try
            {
                Conversation.SendEmoticonDefinitions(emolist, EmoticonType.StaticEmoticon);
                TextMessage emotxt = new TextMessage("Hey, this is a custom emoticon: " + emotest.Shortcut);
                Conversation.SendTextMessage(emotxt);
                DisplaySystemMessage("You send a custom emoticon with text message: Hey, this is a custom emoticon: [test_em].");
            }
            catch (NotSupportedException)
            {
                DisplaySystemMessage("Cannot send custom emoticon in this conversation.");
            }
        }

        private void bMessageBoldItalicUnderline_CheckedChanged(object sender, EventArgs e)
        {
            UpdateTextFonts();
            inputTextBox.Select();
        }

        private void bMessageFontColor_Click(object sender, EventArgs e)
        {
            if (dlgColor.ShowDialog() == DialogResult.OK)
            {
                bMessageFontColor.Image = CreateImageFromColor(dlgColor.Color, bMessageFontColor.Image.Size);
                inputTextBox.ForeColor = dlgColor.Color;
            }
        }

        private void cbMessageFontName_SelectedIndexChanged(object sender, EventArgs e)
        {
            Validate();
        }

        private void cbMessageFontName_Validated(object sender, EventArgs e)
        {
            cbMessageFontName.Tag = cbMessageFontName.Text;
            UpdateTextFonts();
            inputTextBox.Select();
        }

        private void cbMessageFontName_Validating(object sender, CancelEventArgs e)
        {
            if (cbMessageFontName.FindStringExact(cbMessageFontName.Text) == -1)
            {
                cbMessageFontName.Text = cbMessageFontName.Tag.ToString();
            }

        }

        private void cbMessageFontSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            Validate();
        }

        private void cbMessageFontSize_Validated(object sender, EventArgs e)
        {
            cbMessageFontSize.Tag = cbMessageFontSize.Text;
            UpdateTextFonts();
            inputTextBox.Select();
        }

        private void cbMessageFontSize_Validating(object sender, CancelEventArgs e)
        {
            float fontSize = float.MinValue;
            float.TryParse(cbMessageFontSize.Text, out fontSize);

            if (fontSize < 8f || fontSize > 72f)
            {
                cbMessageFontSize.Text = cbMessageFontSize.Tag.ToString();
            }
        }
    }
};
