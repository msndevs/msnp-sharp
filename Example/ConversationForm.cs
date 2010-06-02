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
    using System.Threading;
    using MSNPSharp.Core;
using MSNPSharp.Utilities;

    /// <summary>
    /// Summary description for ConversationForm.
    /// </summary>
    public class ConversationForm : System.Windows.Forms.Form
    {
        #region Windows Form Designer generated code

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

        private ToolStripDropDown emotionDropDown = new ToolStripDropDown();
        private ToolStripDropDown onlineUsersDropDown = new ToolStripDropDown();

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
        private Button btnSendFiles;
        private Button btnInviteUsers;
        private Button btnCustomEmoticon;
        private Button btnActivityTest;
        private ColorDialog dlgColor;


        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConversationForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.tsMessage = new System.Windows.Forms.ToolStrip();
            this.bMessageInsertEmoticon = new System.Windows.Forms.ToolStripButton();
            this.bMessageSendNudge = new System.Windows.Forms.ToolStripButton();
            this.tssMessageSeperator1 = new System.Windows.Forms.ToolStripSeparator();
            this.bMessageFontColor = new System.Windows.Forms.ToolStripButton();
            this.bMessageBold = new System.Windows.Forms.ToolStripButton();
            this.bMessageItalic = new System.Windows.Forms.ToolStripButton();
            this.bMessageUnderline = new System.Windows.Forms.ToolStripButton();
            this.cbMessageFontName = new System.Windows.Forms.ToolStripComboBox();
            this.cbMessageFontSize = new System.Windows.Forms.ToolStripComboBox();
            this.tssMessageSeperator2 = new System.Windows.Forms.ToolStripSeparator();
            this.bMessageSend = new System.Windows.Forms.ToolStripButton();
            this.displayOwner = new System.Windows.Forms.PictureBox();
            this.inputTextBox = new System.Windows.Forms.TextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnActivityTest = new System.Windows.Forms.Button();
            this.btnCustomEmoticon = new System.Windows.Forms.Button();
            this.btnInviteUsers = new System.Windows.Forms.Button();
            this.btnSendFiles = new System.Windows.Forms.Button();
            this.displayUser = new System.Windows.Forms.PictureBox();
            this.richTextHistory = new MSNPSharpClient.RtfRichTextBox();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.dlgColor = new System.Windows.Forms.ColorDialog();
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
            this.panel1.Location = new System.Drawing.Point(0, 272);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(663, 111);
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
            this.bMessageInsertEmoticon,
            this.bMessageSendNudge,
            this.tssMessageSeperator1,
            this.bMessageFontColor,
            this.bMessageBold,
            this.bMessageItalic,
            this.bMessageUnderline,
            this.cbMessageFontName,
            this.cbMessageFontSize,
            this.tssMessageSeperator2,
            this.bMessageSend});
            this.tsMessage.Location = new System.Drawing.Point(109, 3);
            this.tsMessage.Name = "tsMessage";
            this.tsMessage.Padding = new System.Windows.Forms.Padding(3, 0, 1, 0);
            this.tsMessage.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.tsMessage.Size = new System.Drawing.Size(550, 25);
            this.tsMessage.TabIndex = 8;
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
            // tssMessageSeperator1
            // 
            this.tssMessageSeperator1.Name = "tssMessageSeperator1";
            this.tssMessageSeperator1.Size = new System.Drawing.Size(6, 25);
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
            this.inputTextBox.Size = new System.Drawing.Size(550, 72);
            this.inputTextBox.TabIndex = 1;
            this.inputTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.inputTextBox_KeyDown);
            this.inputTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.inputTextBox_KeyPress);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btnActivityTest);
            this.panel2.Controls.Add(this.btnCustomEmoticon);
            this.panel2.Controls.Add(this.btnInviteUsers);
            this.panel2.Controls.Add(this.btnSendFiles);
            this.panel2.Controls.Add(this.displayUser);
            this.panel2.Controls.Add(this.richTextHistory);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(663, 272);
            this.panel2.TabIndex = 0;
            // 
            // btnActivityTest
            // 
            this.btnActivityTest.Location = new System.Drawing.Point(3, 98);
            this.btnActivityTest.Name = "btnActivityTest";
            this.btnActivityTest.Size = new System.Drawing.Size(100, 24);
            this.btnActivityTest.TabIndex = 4;
            this.btnActivityTest.Text = "Activity Test";
            this.btnActivityTest.UseVisualStyleBackColor = true;
            this.btnActivityTest.Click += new System.EventHandler(this.btnActivityTest_Click);
            // 
            // btnCustomEmoticon
            // 
            this.btnCustomEmoticon.Location = new System.Drawing.Point(3, 41);
            this.btnCustomEmoticon.Name = "btnCustomEmoticon";
            this.btnCustomEmoticon.Size = new System.Drawing.Size(100, 23);
            this.btnCustomEmoticon.TabIndex = 3;
            this.btnCustomEmoticon.Text = "Custom Emoticon";
            this.btnCustomEmoticon.UseVisualStyleBackColor = true;
            this.btnCustomEmoticon.Click += new System.EventHandler(this.bMessageSendCustomEmoticon_Click);
            // 
            // btnInviteUsers
            // 
            this.btnInviteUsers.Location = new System.Drawing.Point(3, 70);
            this.btnInviteUsers.Name = "btnInviteUsers";
            this.btnInviteUsers.Size = new System.Drawing.Size(100, 23);
            this.btnInviteUsers.TabIndex = 2;
            this.btnInviteUsers.Text = "Invite Users";
            this.btnInviteUsers.UseVisualStyleBackColor = true;
            this.btnInviteUsers.Click += new System.EventHandler(this.btnInviteUsers_Click);
            // 
            // btnSendFiles
            // 
            this.btnSendFiles.Location = new System.Drawing.Point(3, 12);
            this.btnSendFiles.Name = "btnSendFiles";
            this.btnSendFiles.Size = new System.Drawing.Size(100, 23);
            this.btnSendFiles.TabIndex = 1;
            this.btnSendFiles.Text = "Send Files";
            this.btnSendFiles.UseVisualStyleBackColor = true;
            this.btnSendFiles.Click += new System.EventHandler(this.bMessageSendFiles_Click);
            // 
            // displayUser
            // 
            this.displayUser.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.displayUser.BackColor = System.Drawing.Color.White;
            this.displayUser.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.displayUser.Location = new System.Drawing.Point(3, 166);
            this.displayUser.Name = "displayUser";
            this.displayUser.Size = new System.Drawing.Size(100, 101);
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
            this.richTextHistory.HiglightColor = MSNPSharpClient.RtfRichTextBox.RtfColor.White;
            this.richTextHistory.Location = new System.Drawing.Point(109, 3);
            this.richTextHistory.Name = "richTextHistory";
            this.richTextHistory.ReadOnly = true;
            this.richTextHistory.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.richTextHistory.Size = new System.Drawing.Size(554, 264);
            this.richTextHistory.TabIndex = 0;
            this.richTextHistory.TabStop = false;
            this.richTextHistory.Text = "";
            this.richTextHistory.TextColor = MSNPSharpClient.RtfRichTextBox.RtfColor.Black;
            // 
            // openFileDialog
            // 
            this.openFileDialog.Multiselect = true;
            // 
            // ConversationForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(663, 383);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "ConversationForm";
            this.Text = "Conversation - MSNPSharp";
            this.Load += new System.EventHandler(this.ConversationForm_Load);
            this.Shown += new System.EventHandler(this.ConversationForm_Shown);
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

        private Messenger _messenger = null;
        private Contact _firstInvitedContact = null;
        private Guid activeconversationID = Guid.Empty;

        bool isYIM = false;

        /// <summary>
        /// The conversation object which is associated with the form.
        /// </summary>
        public Conversation ActiveConversation
        {
            get
            {
                return _messenger.MessageManager.GetConversation(activeconversationID);
            }

        }

        public Contact RemoteOwner
        {
            get
            {
                return _firstInvitedContact;
            }
        }

        public Guid ConversationID
        {
            get
            {
                return activeconversationID;
            }

            private set
            {
                activeconversationID = value;
            }
        }

        protected ConversationForm()
        {
        }

        /// <summary>
        /// For sending and receiving YIM messages.
        /// </summary>
        /// <param name="messenger"></param>
        /// <param name="contact"></param>
        public ConversationForm(Messenger messenger, Contact contact, Guid convId)
        {
            InitializeComponent();

            _firstInvitedContact = contact;
            _messenger = messenger;
            activeconversationID = convId;

            isYIM = (contact.IsMessengerUser && contact.ClientType == ClientType.EmailMember);

            if (isYIM)
            {
                btnActivityTest.Enabled = false;
                btnCustomEmoticon.Enabled = false;
                btnInviteUsers.Enabled = false;
                btnSendFiles.Enabled = false;
            }
        }

        public void OnMessageReceived(object sender, MessageArrivedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<MessageArrivedEventArgs>(OnMessageReceived), new object[] { sender, e });
            }
            else
            {
                switch (e.MessageType)
                {
                    case MessageType.Nudge:
                        MakeVisible(sender, e);
                        PrintNudge(e.Sender);
                        break;
                    case MessageType.TextMessage:
                        MakeVisible(sender, e);
                        PrintText(e.Sender, (e as TextMessageArrivedEventArgs).TextMessage);
                        break;
                    case MessageType.Emoticon:
                        {
                            Emoticon emo = (e as EmoticonArrivedEventArgs).Emoticon;
                            if (emo != null)
                            {
                                MemoryStream ms = new MemoryStream();
                                byte[] byt = new byte[emo.OpenStream().Length];
                                emo.OpenStream().Seek(0, SeekOrigin.Begin);
                                emo.OpenStream().Read(byt, 0, byt.Length);
                                ms.Write(byt, 0, byt.Length);

                                richTextHistory.Emotions[emo.Shortcut] = new Bitmap(Image.FromStream(ms));

                                ms.Close();
                            }

                            while (richTextHistory.HasEmotion)
                            {
                                richTextHistory.InsertEmotion();
                            }
                        }
                        break;
                }
            }
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


        private void inputTextBox_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (!isYIM)
                if (ConversationID == Guid.Empty && e.KeyCode != Keys.Return)
                    return;

            if ((e.KeyCode == Keys.Return) && (e.Alt || e.Control || e.Shift))
            {
                return;
            }

            try
            {
                if (!isYIM)
                    _messenger.MessageManager.SendTyping(ConversationID);
                else
                    _messenger.MessageManager.SendTyping(_firstInvitedContact);
            }
            catch (Exception)
            {
            }

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

        private void PrintNudge(Contact sender)
        {
            DisplaySystemMessage(sender.Name + " has sent a nudge!");
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


        private void ConversationForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _firstInvitedContact.DisplayImageChanged -= Contact_DisplayImageChanged;
            _firstInvitedContact.DisplayImageContextChanged -= Contact_DisplayImageConextChanged;
        }

        private void PrintText(Contact c, TextMessage message)
        {
            richTextHistory.SelectionColor = Color.Navy;
            richTextHistory.SelectionIndent = 0;
            richTextHistory.AppendText("[" + DateTime.Now.ToLongTimeString() + "]" + " ");
            richTextHistory.SelectionColor = c.Mail == _messenger.ContactList.Owner.Mail ? Color.Blue : Color.Black;
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
            displayOwner.Image = _messenger.ContactList.Owner.DisplayImage.Image;

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

            onlineUsersDropDown.LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;

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

        private void ConversationForm_Shown(object sender, EventArgs e)
        {
            if (!isYIM)
                displayUser.Image = _firstInvitedContact.DisplayImage.Image;
            else
                displayUser.Image = Properties.Resources.YahooMessenger_logo.Clone() as Image;

            _firstInvitedContact.DisplayImageChanged += new EventHandler<DisplayImageChangedEventArgs>(Contact_DisplayImageChanged);
            _firstInvitedContact.DisplayImageContextChanged += new EventHandler<DisplayImageChangedEventArgs>(Contact_DisplayImageConextChanged);

            // request the image, if not already available
            if (_firstInvitedContact.Status != PresenceStatus.Offline)
            {
                if (_firstInvitedContact.DisplayImage.IsDefaultImage && isYIM == false)
                {
                    try
                    {
                        RequestDisplayImage(_firstInvitedContact, null);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex.Message + "\r\n StackTrace: " + ex.StackTrace);
                    }
                }


            }
        }

        private void RequestDisplayImage(Contact remoteContact, DisplayImage updateImage)
        {
            if (remoteContact.ClientType == ClientType.PassportMember)
            {
                // create a MSNSLPHandler. This handler takes care of the filetransfer protocol.
                // The MSNSLPHandler makes use of the underlying P2P framework.
                MSNSLPHandler msnslpHandler = _messenger.GetMSNSLPHandler(remoteContact);

                if (updateImage == null)
                    updateImage = remoteContact.DisplayImage;

                // by sending an invitation a P2PTransferSession is automatically created.
                // the session object takes care of the actual data transfer to the remote client,
                // in contrast to the msnslpHandler object, which only deals with the protocol chatting.
                P2PTransferSession session = msnslpHandler.SendInvitation(_messenger.ContactList.Owner, remoteContact, updateImage);

            }
        }

        private void Contact_DisplayImageConextChanged(object sender, DisplayImageChangedEventArgs e)
        {
            try
            {
                RequestDisplayImage(_firstInvitedContact, e.NewDisplayImage);
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex.Message + "\r\n StackTrace: " + ex.StackTrace);
            }
        }

        private void Contact_DisplayImageChanged(object sender, DisplayImageChangedEventArgs e)
        {
            if (displayUser.InvokeRequired)
            {
                Invoke(new EventHandler<DisplayImageChangedEventArgs>(Contact_DisplayImageChanged), new object[] { sender, e });
            }
            else
            {
                displayUser.Image = e.NewDisplayImage.Image;
            }
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

            inputTextBox.Clear();
            inputTextBox.Focus();

            if (ConversationID != Guid.Empty)
            {
                ConversationID = _messenger.MessageManager.SendTextMessage(ConversationID, message);
                PrintText(_messenger.ContactList.Owner, message);
            }
            else
            {
                ConversationID = _messenger.MessageManager.SendTextMessage(_firstInvitedContact, message);

                if (!isYIM)
                    PrintText(_messenger.ContactList.Owner, new TextMessage(message.Text + "\r\n(Send as Offline message.)"));
                else
                    PrintText(_messenger.ContactList.Owner, message);
            }
            
        }

        private void bMessageInsertEmoticon_Click(object sender, EventArgs e)
        {
            int x = Location.X + richTextHistory.Location.X + bMessageInsertEmoticon.Width;
            int y = Location.Y + richTextHistory.Height + emotionDropDown.Height + 20;
            emotionDropDown.Show(x, y);
            emotionDropDown.Focus();
        }

        private void btnInviteUsers_Click(object sender, EventArgs e)
        {
            int x = Location.X + 10 + btnInviteUsers.Width;
            int y = Location.Y + 10 + btnInviteUsers.Height + 20;

            onlineUsersDropDown.Items.Clear();
            foreach (Contact c in _messenger.ContactList.Forward)
            {
                if (c.Online && c.ClientType == ClientType.PassportMember)
                {
                    onlineUsersDropDown.Items.Add(c.Mail, null, onlineUsersDropDown_Click).ToolTipText = c.Mail;
                }
            }

            onlineUsersDropDown.Show(x, y);
            onlineUsersDropDown.Focus();
        }

        private void onlineUsersDropDown_Click(object sender, EventArgs args)
        {
            ToolStripItem item = (ToolStripItem)sender;
            ActiveConversation.Invite(item.ToolTipText, ClientType.PassportMember);
        }

        private void bMessageSendNudge_Click(object sender, EventArgs e)
        {
            if (ConversationID != Guid.Empty)
            {
                ConversationID = _messenger.MessageManager.SendNudge(ConversationID);
                DisplaySystemMessage("You send a nudge.");
            }
            else
            {
                if (!isYIM)
                    DisplaySystemMessage("Remote contact not online.");
                else
                {
                    ConversationID = _messenger.MessageManager.SendNudge(_firstInvitedContact);
                    DisplaySystemMessage("You send a nudge.");
                }
            }
        }

        private void bMessageSendFiles_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                List<Contact> contacts = new List<Contact>();
                foreach (Contact contact in ActiveConversation.Contacts)
                {
                    if (contact.Online && contact.ClientType == ClientType.PassportMember && 
                        contact.Mail != _messenger.ContactList.Owner.Mail)
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
                            MSNSLPHandler msnslpHandler = _messenger.GetMSNSLPHandler(contact);
                            FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                            P2PTransferSession session = msnslpHandler.SendInvitation(_messenger.ContactList.Owner, contact, Path.GetFileName(filename), fileStream);
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
            Emoticon emotest = new Emoticon(_messenger.ContactList.Owner.Mail, mem, "0", "test_em");
            MSNObjectCatalog.GetInstance().Add(emotest);
            List<Emoticon> emolist = new List<Emoticon>();
            emolist.Add(emotest);

            if (!richTextHistory.Emotions.ContainsKey(emotest.Shortcut))
            {
                richTextHistory.Emotions[emotest.Shortcut] = Properties.Resources.inner_emoticon;
            }

            if (ConversationID != Guid.Empty)
            {
                ConversationID = _messenger.MessageManager.SendEmoticonDefinitions(ConversationID, emolist, EmoticonType.StaticEmoticon);
                TextMessage emotxt = new TextMessage("Hey, this is a custom emoticon: " + emotest.Shortcut);
                ConversationID = _messenger.MessageManager.SendTextMessage(ConversationID, emotxt);
                DisplaySystemMessage("You send a custom emoticon with text message: Hey, this is a custom emoticon: [test_em].");
            }
            else
            {
                if (!isYIM)
                    DisplaySystemMessage("Remote contact not online, emoticon will not be sent.");
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

        private void MSNSLPHandler_TransferSessionClosed(object sender, P2PTransferSessionEventArgs e)
        {
            if (!richTextHistory.InvokeRequired)
            {
                DisplaySystemMessage("Activity session closed.");
            }
            else
            {
                richTextHistory.Invoke(new EventHandler<P2PTransferSessionEventArgs>(MSNSLPHandler_TransferSessionClosed), new object[] { sender,e });
            }
        }

        private void btnActivityTest_Click(object sender, EventArgs e)
        {
            String activityID = "7"; //"20521364";        //The activityID of Music Mix activity.
            String activityName = "Activity Test";     //Th name of acticvity
            MSNSLPHandler msnslpHandler = _messenger.GetMSNSLPHandler(_firstInvitedContact);
            P2PTransferSession session = msnslpHandler.SendInvitation(_messenger.ContactList.Owner, _firstInvitedContact, activityID, activityName, @"http://code.google.com/p/msnp-sharp");

            msnslpHandler.TransferSessionClosed += delegate(object s, P2PTransferSessionEventArgs ea)
            {
                if (ea.TransferSession == session)
                    MSNSLPHandler_TransferSessionClosed(s, ea);
            };
        }
    }
};
