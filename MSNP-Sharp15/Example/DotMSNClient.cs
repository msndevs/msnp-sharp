namespace MSNPSharpClient
{
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

    /// <summary>
    /// MSNPSharp Client example.
    /// </summary>
    public class ClientForm : System.Windows.Forms.Form
    {
        // Create a Messenger object to use MSNPSharp.
        private Messenger messenger = new Messenger();

        #region Form controls
        private Panel ListPanel;
        private Panel ContactPanel;
        private PictureBox pictureBox;
        private PictureBox displayImageBox;
        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;
        private Button changeDisplayButton;
        private OpenFileDialog openImageDialog;
        private Timer tmrKeepOnLine;
        private TreeView treeViewFavoriteList;
        private ImageList ImageList1;
        private ContextMenuStrip userMenuStrip;
        private ToolStripMenuItem sendIMMenuItem;
        private ToolStripMenuItem blockMenuItem;
        private ToolStripSeparator toolStripMenuItem2;
        private ToolStripMenuItem unblockMenuItem;
        private ToolStripMenuItem sendOIMMenuItem;
        private ToolStripSeparator toolStripMenuItem3;
        private ToolStripMenuItem sendFileMenuItem;
        private ToolStripMenuItem sendMIMMenuItem;
        private StatusBar statusBar;
        private Panel OwnerPanel;
        private Splitter splitter2;
        private Panel panel1;
        private Label label1;
        private Button button1;
        private TextBox accountTextBox;
        private Label label2;
        private Button loginButton;
        private TextBox passwordTextBox;
        private PropertyGrid propertyGrid;
        private Panel SortPanel;
        private Panel treeViewPanel;
        private Button btnSortBy;
        private ContextMenuStrip sortContextMenu;
        private ToolStripMenuItem toolStripSortByStatus;
        private ToolStripMenuItem toolStripSortBygroup;
        private ToolStripMenuItem toolStripDeleteGroup;
        private ContextMenuStrip groupContextMenu;
        private TextBox txtAddNew;
        private Button btnAddNew;
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

            messenger.Nameserver.ContactOnline += new ContactChangedEventHandler(Nameserver_ContactOnline);
            messenger.Nameserver.ContactOffline += new ContactChangedEventHandler(Nameserver_ContactOffline);

            messenger.Nameserver.ReverseAdded += new ContactChangedEventHandler(Nameserver_ReverseAdded);

            treeViewFavoriteList.TreeViewNodeSorter = StatusSorter.Default;

            if (toolStripSortByStatus.Checked)
                SortByStatus();
            else
                SortByGroup();
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
            this.treeViewPanel = new System.Windows.Forms.Panel();
            this.treeViewFavoriteList = new System.Windows.Forms.TreeView();
            this.SortPanel = new System.Windows.Forms.Panel();
            this.txtAddNew = new System.Windows.Forms.TextBox();
            this.btnAddNew = new System.Windows.Forms.Button();
            this.btnSortBy = new System.Windows.Forms.Button();
            this.userMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.sendIMMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sendOIMMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sendMIMMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.sendFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.blockMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.unblockMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeDisplayButton = new System.Windows.Forms.Button();
            this.ContactPanel = new System.Windows.Forms.Panel();
            this.splitter2 = new System.Windows.Forms.Splitter();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.accountTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.loginButton = new System.Windows.Forms.Button();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.displayImageBox = new System.Windows.Forms.PictureBox();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.openImageDialog = new System.Windows.Forms.OpenFileDialog();
            this.tmrKeepOnLine = new System.Windows.Forms.Timer(this.components);
            this.ImageList1 = new System.Windows.Forms.ImageList(this.components);
            this.statusBar = new System.Windows.Forms.StatusBar();
            this.OwnerPanel = new System.Windows.Forms.Panel();
            this.sortContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripSortByStatus = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSortBygroup = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripDeleteGroup = new System.Windows.Forms.ToolStripMenuItem();
            this.groupContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ListPanel.SuspendLayout();
            this.treeViewPanel.SuspendLayout();
            this.SortPanel.SuspendLayout();
            this.userMenuStrip.SuspendLayout();
            this.ContactPanel.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.displayImageBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.OwnerPanel.SuspendLayout();
            this.sortContextMenu.SuspendLayout();
            this.groupContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // ListPanel
            // 
            this.ListPanel.BackColor = System.Drawing.SystemColors.Window;
            this.ListPanel.Controls.Add(this.treeViewPanel);
            this.ListPanel.Controls.Add(this.SortPanel);
            this.ListPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListPanel.Location = new System.Drawing.Point(287, 95);
            this.ListPanel.Name = "ListPanel";
            this.ListPanel.Size = new System.Drawing.Size(280, 563);
            this.ListPanel.TabIndex = 0;
            // 
            // treeViewPanel
            // 
            this.treeViewPanel.Controls.Add(this.treeViewFavoriteList);
            this.treeViewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewPanel.Location = new System.Drawing.Point(0, 29);
            this.treeViewPanel.Name = "treeViewPanel";
            this.treeViewPanel.Size = new System.Drawing.Size(280, 534);
            this.treeViewPanel.TabIndex = 2;
            // 
            // treeViewFavoriteList
            // 
            this.treeViewFavoriteList.BackColor = System.Drawing.SystemColors.Info;
            this.treeViewFavoriteList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.treeViewFavoriteList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewFavoriteList.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.treeViewFavoriteList.FullRowSelect = true;
            this.treeViewFavoriteList.HideSelection = false;
            this.treeViewFavoriteList.Indent = 20;
            this.treeViewFavoriteList.ItemHeight = 20;
            this.treeViewFavoriteList.Location = new System.Drawing.Point(0, 0);
            this.treeViewFavoriteList.Name = "treeViewFavoriteList";
            this.treeViewFavoriteList.ShowLines = false;
            this.treeViewFavoriteList.ShowPlusMinus = false;
            this.treeViewFavoriteList.ShowRootLines = false;
            this.treeViewFavoriteList.Size = new System.Drawing.Size(280, 534);
            this.treeViewFavoriteList.TabIndex = 0;
            this.treeViewFavoriteList.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseDoubleClick);
            this.treeViewFavoriteList.DragDrop += new System.Windows.Forms.DragEventHandler(this.treeViewFavoriteList_DragDrop);
            this.treeViewFavoriteList.DragEnter += new System.Windows.Forms.DragEventHandler(this.treeViewFavoriteList_DragEnter);
            this.treeViewFavoriteList.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
            this.treeViewFavoriteList.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.treeViewFavoriteList_ItemDrag);
            this.treeViewFavoriteList.DragOver += new System.Windows.Forms.DragEventHandler(this.treeViewFavoriteList_DragOver);
            // 
            // SortPanel
            // 
            this.SortPanel.BackColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.SortPanel.Controls.Add(this.txtAddNew);
            this.SortPanel.Controls.Add(this.btnAddNew);
            this.SortPanel.Controls.Add(this.btnSortBy);
            this.SortPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.SortPanel.Location = new System.Drawing.Point(0, 0);
            this.SortPanel.Name = "SortPanel";
            this.SortPanel.Size = new System.Drawing.Size(280, 29);
            this.SortPanel.TabIndex = 1;
            // 
            // txtAddNew
            // 
            this.txtAddNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAddNew.Location = new System.Drawing.Point(0, 5);
            this.txtAddNew.Name = "txtAddNew";
            this.txtAddNew.Size = new System.Drawing.Size(187, 21);
            this.txtAddNew.TabIndex = 8;
            // 
            // btnAddNew
            // 
            this.btnAddNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddNew.Location = new System.Drawing.Point(194, 2);
            this.btnAddNew.Name = "btnAddNew";
            this.btnAddNew.Size = new System.Drawing.Size(71, 25);
            this.btnAddNew.TabIndex = 7;
            this.btnAddNew.Text = "add new";
            this.btnAddNew.UseVisualStyleBackColor = true;
            this.btnAddNew.Click += new System.EventHandler(this.btnAddNew_Click);
            // 
            // btnSortBy
            // 
            this.btnSortBy.BackColor = System.Drawing.SystemColors.Control;
            this.btnSortBy.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.btnSortBy.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnSortBy.Location = new System.Drawing.Point(10, 3);
            this.btnSortBy.Name = "btnSortBy";
            this.btnSortBy.Size = new System.Drawing.Size(45, 23);
            this.btnSortBy.TabIndex = 1;
            this.btnSortBy.Text = "sort";
            this.btnSortBy.UseVisualStyleBackColor = true;
            this.btnSortBy.Click += new System.EventHandler(this.btnSortBy_Click);
            // 
            // userMenuStrip
            // 
            this.userMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sendIMMenuItem,
            this.sendOIMMenuItem,
            this.sendMIMMenuItem,
            this.toolStripMenuItem3,
            this.sendFileMenuItem,
            this.toolStripMenuItem2,
            this.blockMenuItem,
            this.unblockMenuItem});
            this.userMenuStrip.Name = "contextMenuStrip1";
            this.userMenuStrip.Size = new System.Drawing.Size(201, 148);
            // 
            // sendIMMenuItem
            // 
            this.sendIMMenuItem.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.sendIMMenuItem.Name = "sendIMMenuItem";
            this.sendIMMenuItem.Size = new System.Drawing.Size(200, 22);
            this.sendIMMenuItem.Text = "Send Instant Message";
            this.sendIMMenuItem.Click += new System.EventHandler(this.sendMessageToolStripMenuItem_Click);
            // 
            // sendOIMMenuItem
            // 
            this.sendOIMMenuItem.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.sendOIMMenuItem.Name = "sendOIMMenuItem";
            this.sendOIMMenuItem.Size = new System.Drawing.Size(200, 22);
            this.sendOIMMenuItem.Text = "Send Offline Message";
            this.sendOIMMenuItem.Click += new System.EventHandler(this.sendOfflineMessageToolStripMenuItem_Click);
            // 
            // sendMIMMenuItem
            // 
            this.sendMIMMenuItem.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.sendMIMMenuItem.Name = "sendMIMMenuItem";
            this.sendMIMMenuItem.Size = new System.Drawing.Size(200, 22);
            this.sendMIMMenuItem.Text = "Send Mobile Message";
            this.sendMIMMenuItem.Click += new System.EventHandler(this.sendMIMMenuItem_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(197, 6);
            // 
            // sendFileMenuItem
            // 
            this.sendFileMenuItem.Name = "sendFileMenuItem";
            this.sendFileMenuItem.Size = new System.Drawing.Size(200, 22);
            this.sendFileMenuItem.Text = "Send File";
            this.sendFileMenuItem.Click += new System.EventHandler(this.sendFileMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(197, 6);
            // 
            // blockMenuItem
            // 
            this.blockMenuItem.Name = "blockMenuItem";
            this.blockMenuItem.Size = new System.Drawing.Size(200, 22);
            this.blockMenuItem.Text = "Block";
            this.blockMenuItem.Click += new System.EventHandler(this.blockToolStripMenuItem_Click);
            // 
            // unblockMenuItem
            // 
            this.unblockMenuItem.Name = "unblockMenuItem";
            this.unblockMenuItem.Size = new System.Drawing.Size(200, 22);
            this.unblockMenuItem.Text = "Unblock";
            this.unblockMenuItem.Click += new System.EventHandler(this.unblockMenuItem_Click);
            // 
            // changeDisplayButton
            // 
            this.changeDisplayButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.changeDisplayButton.BackColor = System.Drawing.SystemColors.Control;
            this.changeDisplayButton.Location = new System.Drawing.Point(351, 64);
            this.changeDisplayButton.Name = "changeDisplayButton";
            this.changeDisplayButton.Size = new System.Drawing.Size(102, 24);
            this.changeDisplayButton.TabIndex = 7;
            this.changeDisplayButton.Text = "Display image";
            this.changeDisplayButton.UseVisualStyleBackColor = true;
            this.changeDisplayButton.Click += new System.EventHandler(this.changeDisplayButton_Click);
            // 
            // ContactPanel
            // 
            this.ContactPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(128)))));
            this.ContactPanel.Controls.Add(this.splitter2);
            this.ContactPanel.Controls.Add(this.panel1);
            this.ContactPanel.Controls.Add(this.propertyGrid);
            this.ContactPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.ContactPanel.Location = new System.Drawing.Point(0, 95);
            this.ContactPanel.Name = "ContactPanel";
            this.ContactPanel.Size = new System.Drawing.Size(287, 563);
            this.ContactPanel.TabIndex = 2;
            // 
            // splitter2
            // 
            this.splitter2.BackColor = System.Drawing.SystemColors.Control;
            this.splitter2.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter2.Location = new System.Drawing.Point(0, 461);
            this.splitter2.Name = "splitter2";
            this.splitter2.Size = new System.Drawing.Size(287, 11);
            this.splitter2.TabIndex = 6;
            this.splitter2.TabStop = false;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.accountTextBox);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.loginButton);
            this.panel1.Controls.Add(this.passwordTextBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 461);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(287, 102);
            this.panel1.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(7, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Account";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(174, 66);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(90, 24);
            this.button1.TabIndex = 6;
            this.button1.Text = "Sign off >";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // accountTextBox
            // 
            this.accountTextBox.Location = new System.Drawing.Point(77, 13);
            this.accountTextBox.Name = "accountTextBox";
            this.accountTextBox.Size = new System.Drawing.Size(187, 21);
            this.accountTextBox.TabIndex = 0;
            this.accountTextBox.Text = "msnpsharp@live.cn";
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(7, 38);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 24);
            this.label2.TabIndex = 2;
            this.label2.Text = "Password";
            // 
            // loginButton
            // 
            this.loginButton.Location = new System.Drawing.Point(77, 66);
            this.loginButton.Name = "loginButton";
            this.loginButton.Size = new System.Drawing.Size(90, 24);
            this.loginButton.TabIndex = 4;
            this.loginButton.Text = "> Sign in";
            this.loginButton.UseVisualStyleBackColor = true;
            this.loginButton.Click += new System.EventHandler(this.loginButton_Click);
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.Location = new System.Drawing.Point(77, 41);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.PasswordChar = '*';
            this.passwordTextBox.Size = new System.Drawing.Size(187, 21);
            this.passwordTextBox.TabIndex = 3;
            this.passwordTextBox.Text = "123456";
            // 
            // propertyGrid
            // 
            this.propertyGrid.BackColor = System.Drawing.SystemColors.Control;
            this.propertyGrid.CommandsBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(128)))));
            this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Top;
            this.propertyGrid.LineColor = System.Drawing.SystemColors.ScrollBar;
            this.propertyGrid.Location = new System.Drawing.Point(0, 0);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.Size = new System.Drawing.Size(287, 461);
            this.propertyGrid.TabIndex = 4;
            // 
            // displayImageBox
            // 
            this.displayImageBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.displayImageBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.displayImageBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.displayImageBox.Location = new System.Drawing.Point(462, 6);
            this.displayImageBox.Name = "displayImageBox";
            this.displayImageBox.Size = new System.Drawing.Size(96, 82);
            this.displayImageBox.TabIndex = 2;
            this.displayImageBox.TabStop = false;
            // 
            // pictureBox
            // 
            this.pictureBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.pictureBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.pictureBox.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox.Image")));
            this.pictureBox.Location = new System.Drawing.Point(0, 0);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(567, 95);
            this.pictureBox.TabIndex = 5;
            this.pictureBox.TabStop = false;
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
            // ImageList1
            // 
            this.ImageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.ImageList1.ImageSize = new System.Drawing.Size(16, 16);
            this.ImageList1.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // statusBar
            // 
            this.statusBar.Location = new System.Drawing.Point(0, 2);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(567, 24);
            this.statusBar.TabIndex = 5;
            // 
            // OwnerPanel
            // 
            this.OwnerPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.OwnerPanel.Controls.Add(this.statusBar);
            this.OwnerPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.OwnerPanel.Location = new System.Drawing.Point(0, 658);
            this.OwnerPanel.Name = "OwnerPanel";
            this.OwnerPanel.Size = new System.Drawing.Size(567, 26);
            this.OwnerPanel.TabIndex = 1;
            // 
            // sortContextMenu
            // 
            this.sortContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSortByStatus,
            this.toolStripSortBygroup});
            this.sortContextMenu.Name = "sortContextMenu";
            this.sortContextMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.sortContextMenu.ShowCheckMargin = true;
            this.sortContextMenu.ShowImageMargin = false;
            this.sortContextMenu.Size = new System.Drawing.Size(136, 48);
            // 
            // toolStripSortByStatus
            // 
            this.toolStripSortByStatus.Checked = true;
            this.toolStripSortByStatus.CheckOnClick = true;
            this.toolStripSortByStatus.CheckState = System.Windows.Forms.CheckState.Checked;
            this.toolStripSortByStatus.Name = "toolStripSortByStatus";
            this.toolStripSortByStatus.ShowShortcutKeys = false;
            this.toolStripSortByStatus.Size = new System.Drawing.Size(135, 22);
            this.toolStripSortByStatus.Text = "Sort by status";
            this.toolStripSortByStatus.Click += new System.EventHandler(this.toolStripSortByStatus_Click);
            // 
            // toolStripSortBygroup
            // 
            this.toolStripSortBygroup.CheckOnClick = true;
            this.toolStripSortBygroup.Name = "toolStripSortBygroup";
            this.toolStripSortBygroup.ShowShortcutKeys = false;
            this.toolStripSortBygroup.Size = new System.Drawing.Size(135, 22);
            this.toolStripSortBygroup.Text = "Sort by group";
            this.toolStripSortBygroup.Click += new System.EventHandler(this.toolStripSortBygroup_Click);
            // 
            // toolStripDeleteGroup
            // 
            this.toolStripDeleteGroup.Name = "toolStripDeleteGroup";
            this.toolStripDeleteGroup.Size = new System.Drawing.Size(136, 22);
            this.toolStripDeleteGroup.Text = "Delete group";
            this.toolStripDeleteGroup.Click += new System.EventHandler(this.toolStripDeleteGroup_Click);
            // 
            // groupContextMenu
            // 
            this.groupContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripDeleteGroup});
            this.groupContextMenu.Name = "sortContextMenu";
            this.groupContextMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.groupContextMenu.ShowCheckMargin = true;
            this.groupContextMenu.ShowImageMargin = false;
            this.groupContextMenu.Size = new System.Drawing.Size(137, 26);
            // 
            // ClientForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 14);
            this.ClientSize = new System.Drawing.Size(567, 684);
            this.Controls.Add(this.ListPanel);
            this.Controls.Add(this.displayImageBox);
            this.Controls.Add(this.ContactPanel);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.OwnerPanel);
            this.Controls.Add(this.changeDisplayButton);
            this.Name = "ClientForm";
            this.Text = "MSNPSharp Example Client";
            this.ListPanel.ResumeLayout(false);
            this.treeViewPanel.ResumeLayout(false);
            this.SortPanel.ResumeLayout(false);
            this.SortPanel.PerformLayout();
            this.userMenuStrip.ResumeLayout(false);
            this.ContactPanel.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.displayImageBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.OwnerPanel.ResumeLayout(false);
            this.sortContextMenu.ResumeLayout(false);
            this.groupContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ClientForm());
        }

        void Nameserver_ContactOnline(object sender, ContactEventArgs e)
        {
            Invoke(new ContactChangedEventHandler(ContactOnlineOfline), sender, e);
        }
        void Nameserver_ContactOffline(object sender, ContactEventArgs e)
        {
            Invoke(new ContactChangedEventHandler(ContactOnlineOfline),sender, e);
        }

        void ContactOnlineOfline(object sender, ContactEventArgs e)
        {
            if (toolStripSortByStatus.Checked)
                SortByStatus();
            else
                SortByGroup();
        }

        void Nameserver_PingAnswer(object sender, PingAnswerEventArgs e)
        {
            nextPing = e.SecondsToWait;
        }

        void Nameserver_OIMReceived(object sender, OIMReceivedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new OIMReceivedEventHandler(Nameserver_OIMReceived), sender, e);
                return;
            }

            if (DialogResult.Yes == MessageBox.Show(e.ReceivedTime + ":\r\n" + e.Message + "\r\n\r\n\r\nClick yes, if you want to receive this message next time you login.", "Offline Message from " + e.Email, MessageBoxButtons.YesNoCancel))
            {
                e.IsRead = false;
            }
        }

        void Nameserver_ReverseAdded(object sender, ContactEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new ContactChangedEventHandler(Nameserver_ReverseAdded), sender, e);
                return;
            }

            Contact contact = e.Contact;
            ReverseAddedForm form = new ReverseAddedForm(contact);
            form.FormClosed += delegate(object f, FormClosedEventArgs fce)
            {
                form = f as ReverseAddedForm;
                if (DialogResult.OK == form.DialogResult)
                {
                    if (form.AddToContactList)
                    {
                        contact.NSMessageHandler.ContactService.AddNewContact(contact.Mail);
                        if (form.Blocked)
                        {
                            contact.Blocked = true;
                        }
                        contact.OnPendingList = false;
                    }
                    /*
                else if (form.Blocked)
                {
                    contact.Blocked = true;
                    contact.OnPendingList = false;
                }
                else
                {
                    contact.OnAllowedList = true;
                    contact.OnPendingList = false;
                }
                * */


                }
                return;
            };
            form.Show(this);
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
        private void loginButton_Click(object sender, System.EventArgs e)
        {
            if (messenger.Connected)
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
            SetStatus("Signed into the messenger network as " + messenger.Owner.Name);

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
            if (e.Exception is UnauthorizedException)
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
        private void button1_Click(object sender, System.EventArgs e)
        {
            if (messenger.Connected)
                messenger.Disconnect();

            if (toolStripSortByStatus.Checked)
                SortByStatus();
            else
                SortByGroup();
        }

        /// <summary>
        /// Used to invoke UpdateContactlist
        /// </summary>
        private delegate void UpdateContactlistDelegate();

        /// <summary>
        /// Updates the treeView.
        /// </summary>
        private void UpdateContactlist()
        {
            if (messenger.Connected == false)
                return;

            if (toolStripSortByStatus.Checked)
                SortByStatus();
            else
                SortByGroup();

            tmrKeepOnLine.Enabled = true;
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

            if (image.Image != null)
                displayImageBox.Image = image.Image;
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
            if (e.Initiator == null)
            {
                // use the invoke method to create the form in the main thread
                this.Invoke(new CreateConversationDelegate(CreateConversationForm), new object[] { e.Conversation });
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
            if (MessageBox.Show(
                messenger.ContactList[e.TransferProperties.RemoteContact].Name +
                " wants to send you a file.\r\nFilename: " +
                e.Filename + "\r\nLength (bytes): " + e.FileSize,
                "Filetransfer invitation",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // by setting the Accept property in the EventArgs to true we give the transfer a green light				
                saveFileDialog.FileName = e.Filename;
                if ((DialogResult)Invoke(new ShowFileDialogDelegate(ShowFileDialog), new object[] { saveFileDialog }) == DialogResult.OK)
                {
                    e.TransferSession.DataStream = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write);
                    //e.Handler.AcceptTransfer (e);
                    e.Accept = true;
                }
            }
        }


        /// <summary>
        /// Change the current owner's display image.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void changeDisplayButton_Click(object sender, EventArgs e)
        {
            //messenger.Owner.DisplayImage.FileLocation = @"C:\icon.png";
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

        private static Font PARENT_NODE_FONT = new Font("Tahoma", 8f, FontStyle.Bold);
        private static Font USER_NODE_FONT = new Font("Tahoma", 8f);
        private static Font USER_NODE_FONT_BANNED = new Font("Tahoma", 8f, FontStyle.Strikeout);
        public class StatusSorter : IComparer
        {
            public static StatusSorter Default = new StatusSorter();
            private StatusSorter()
            {
            }
            public int Compare(object x, object y)
            {
                TreeNode node = x as TreeNode;
                TreeNode node2 = y as TreeNode;

                if (node.Tag is string && node2.Tag is string)
                {
                    // Online (0), Offline (1)
                    return Convert.ToInt32(node.Tag) - Convert.ToInt32(node2.Tag);
                }
                else if (node.Tag is Contact && node2.Tag is Contact)
                {
                    if (((Contact)node.Tag).Status == ((Contact)node2.Tag).Status)
                    {
                        return string.Compare(((Contact)node.Tag).Name, ((Contact)node2.Tag).Name, StringComparison.CurrentCultureIgnoreCase);
                    }
                    if (((Contact)node.Tag).Online)
                        return 1;
                    else if (((Contact)node2.Tag).Online)
                        return -1;
                }
                else if (node.Tag is ContactGroup && node2.Tag is ContactGroup)
                {
                    return string.Compare(((ContactGroup)node.Tag).Name, ((ContactGroup)node2.Tag).Name, StringComparison.CurrentCultureIgnoreCase);
                }
                return 0;
            }
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if ((e.Button == MouseButtons.Left) && (e.Node.Level != 0))
            {
                Contact selectedContact = (Contact)e.Node.Tag;
                propertyGrid.SelectedObject = selectedContact;
                if (selectedContact.Online)
                {
                    sendMessageToolStripMenuItem_Click(this, EventArgs.Empty);
                }
            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (e.Node.Level == 0)
                {
                    if (e.Node.IsExpanded || (e.Node.ImageIndex == 1))
                    {
                        e.Node.Collapse();
                        e.Node.ImageIndex = 0;
                        e.Node.SelectedImageIndex = 0;
                    }
                    else
                    {
                        e.Node.Expand();
                        e.Node.ImageIndex = 1;
                        e.Node.SelectedImageIndex = 1;
                    }
                    if (e.Node.Tag is ContactGroup)
                    {
                        propertyGrid.SelectedObject = (ContactGroup)e.Node.Tag;
                    }
                }
                else
                {
                    Contact selectedContact = (Contact)e.Node.Tag;

                    propertyGrid.SelectedObject = selectedContact;

                    // request the image, if not already available
                    if (selectedContact.Status != PresenceStatus.Offline && selectedContact.DisplayImage != null)
                    {
                        if (selectedContact.DisplayImage.Image == null)
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
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (e.Node.Tag is Contact && e.Node.Level > 0)
                {
                    treeViewFavoriteList.SelectedNode = e.Node;
                    Contact contact = (Contact)treeViewFavoriteList.SelectedNode.Tag;

                    if (contact.Blocked)
                    {
                        blockMenuItem.Visible = false;
                        unblockMenuItem.Visible = true;
                    }
                    else
                    {
                        blockMenuItem.Visible = true;
                        unblockMenuItem.Visible = false;
                    }

                    if (contact.Online)
                    {
                        sendIMMenuItem.Visible = true;
                        sendOIMMenuItem.Visible = false;

                        sendFileMenuItem.Visible = true;
                        toolStripMenuItem2.Visible = true;
                    }
                    else
                    {
                        sendIMMenuItem.Visible = false;
                        sendOIMMenuItem.Visible = true;

                        sendFileMenuItem.Visible = false;
                        toolStripMenuItem2.Visible = false;
                    }

                    Point point = treeViewFavoriteList.PointToScreen(new Point(e.X, e.Y));
                    userMenuStrip.Show(point.X - userMenuStrip.Width, point.Y);
                }
                else if (e.Node.Tag is ContactGroup)
                {
                    treeViewFavoriteList.SelectedNode = e.Node;
                    ContactGroup contact = (ContactGroup)treeViewFavoriteList.SelectedNode.Tag;

                    Point point = treeViewFavoriteList.PointToScreen(new Point(e.X, e.Y));
                    groupContextMenu.Show(point.X - groupContextMenu.Width, point.Y);
                }
            }
        }

        private void blockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ((Contact)treeViewFavoriteList.SelectedNode.Tag).Blocked = true;
            treeViewFavoriteList.SelectedNode.NodeFont = USER_NODE_FONT_BANNED;
        }

        private void unblockMenuItem_Click(object sender, EventArgs e)
        {
            ((Contact)treeViewFavoriteList.SelectedNode.Tag).Blocked = false;
            treeViewFavoriteList.SelectedNode.NodeFont = USER_NODE_FONT;
        }

        private void sendMessageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Conversation conversation = messenger.CreateConversation();
            conversation.Invite(((Contact)treeViewFavoriteList.SelectedNode.Tag));
            ConversationForm form = CreateConversationForm(conversation);
            form.Show();
        }

        private void sendOfflineMessageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Contact selectedContact = (Contact)treeViewFavoriteList.SelectedNode.Tag;
            this.propertyGrid.SelectedObject = selectedContact;

            messenger.Nameserver.OIMService.SendOIMMessage(selectedContact.Mail, "MSNP message");
        }

        private void sendFileMenuItem_Click(object sender, EventArgs e)
        {
            Contact selectedContact = (Contact)treeViewFavoriteList.SelectedNode.Tag;
            this.propertyGrid.SelectedObject = selectedContact;

            // open a dialog box to select the file
            if (selectedContact.Online && openFileDialog.ShowDialog() == DialogResult.OK)
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

        private void sendMIMMenuItem_Click(object sender, EventArgs e)
        {
            Contact selectedContact = (Contact)treeViewFavoriteList.SelectedNode.Tag;
            this.propertyGrid.SelectedObject = selectedContact;

            // open a dialog box to select the file
            if (selectedContact.Online == false && selectedContact.MobileAccess == true)
            {
                messenger.Nameserver.SendMobileMessage(selectedContact, "MSNP mobile message");
            }
            else
                MessageBox.Show("This contact is not able to receive mobile messages");
        }


        private void btnSortBy_Click(object sender, EventArgs e)
        {
            int x = ((base.Location.X + ListPanel.Location.X) + treeViewFavoriteList.Location.X) + 5;
            int y = ((base.Location.Y + ListPanel.Location.Y) + treeViewFavoriteList.Location.Y) + (3 * btnSortBy.Height);
            sortContextMenu.Show(x, y);
            sortContextMenu.Focus();
        }

        private void toolStripSortByStatus_Click(object sender, EventArgs e)
        {
            if (this.toolStripSortByStatus.Checked)
            {
                SortByStatus();
            }
            else
            {
                this.toolStripSortByStatus.Checked = true;
            }
        }

        private void SortByStatus()
        {
            this.treeViewFavoriteList.BeginUpdate();
            this.toolStripSortBygroup.Checked = false;
            this.treeViewFavoriteList.Nodes.Clear();

            TreeNode onlinenode = treeViewFavoriteList.Nodes.Add("0", "Online", 0, 0);
            onlinenode.NodeFont = PARENT_NODE_FONT;
            onlinenode.Tag = "0";
            TreeNode offlinenode = treeViewFavoriteList.Nodes.Add("1", "Offline", 0, 0);
            offlinenode.NodeFont = PARENT_NODE_FONT;
            offlinenode.Tag = "1";

            foreach (Contact contact in messenger.ContactList.All)
            {
                TreeNode newnode = contact.Online ? onlinenode.Nodes.Add(contact.Mail, contact.Name) : offlinenode.Nodes.Add(contact.Mail, contact.Name);
                newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                newnode.Tag = contact;
            }

            onlinenode.Text = "Online (" + onlinenode.Nodes.Count.ToString() + ")";
            offlinenode.Text = "Offline (" + offlinenode.Nodes.Count.ToString() + ")";

            treeViewFavoriteList.Sort();
            treeViewFavoriteList.EndUpdate();
            treeViewFavoriteList.AllowDrop = false;
        }

        private void toolStripSortBygroup_Click(object sender, EventArgs e)
        {
            if (this.toolStripSortBygroup.Checked)
            {
                SortByGroup();
            }
            else
            {
                this.toolStripSortBygroup.Checked = true;
            }
        }

        private void SortByGroup()
        {
            this.treeViewFavoriteList.BeginUpdate();
            this.toolStripSortByStatus.Checked = false;
            this.treeViewFavoriteList.Nodes.Clear();

            foreach (ContactGroup group in this.messenger.ContactGroups)
            {
                TreeNode node = treeViewFavoriteList.Nodes.Add(group.Guid, "0", 0, 0);
                node.ImageIndex = 0;
                node.NodeFont = PARENT_NODE_FONT;
                node.Tag = group;
            }

            TreeNode common = treeViewFavoriteList.Nodes.Add("0", "0", 0, 0);
            common.ImageIndex = 0;
            common.NodeFont = PARENT_NODE_FONT;
            common.Tag = String.Empty;

            foreach (Contact contact in messenger.ContactList.All)
            {
                if (contact.ContactGroups.Count == 0)
                {
                    TreeNode newnode = common.Nodes.Add(contact.Mail, contact.Name);
                    newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                    newnode.Tag = contact;
                    if (contact.Online)
                        common.Text = (Convert.ToInt32(common.Text) + 1).ToString();
                }
                else
                {
                    foreach (ContactGroup group in contact.ContactGroups)
                    {
                        TreeNode found = treeViewFavoriteList.Nodes.Find(group.Guid, false)[0];
                        TreeNode newnode = found.Nodes.Add(contact.Mail, contact.Name);
                        newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                        newnode.Tag = contact;
                        if (contact.Online)
                            found.Text = (Convert.ToInt32(found.Text) + 1).ToString();
                    }
                }
            }

            foreach (TreeNode nodeGroup in treeViewFavoriteList.Nodes)
            {
                if (nodeGroup.Tag is ContactGroup)
                {
                    nodeGroup.Text = ((ContactGroup)nodeGroup.Tag).Name + "(" + nodeGroup.Text + "/" + nodeGroup.Nodes.Count + ")";
                }
            }

            common.Text = "No group (" + common.Text + "/" + common.Nodes.Count + ")";

            treeViewFavoriteList.Sort();
            treeViewFavoriteList.EndUpdate();
            treeViewFavoriteList.AllowDrop = true;

        }

        private void treeViewFavoriteList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void treeViewFavoriteList_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if ((e.Item is TreeNode) && (((TreeNode)e.Item).Level > 0))
            {
                base.DoDragDrop(e.Item, DragDropEffects.Move);
            }
        }

        private void treeViewFavoriteList_DragOver(object sender, DragEventArgs e)
        {
            Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
            TreeNode nodeAt = ((TreeView)sender).GetNodeAt(pt);
            TreeNode data = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");
            if (((data.Level == 0) || (data.Parent == nodeAt)) || (nodeAt.Parent == data.Parent))
            {
                e.Effect = DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void treeViewFavoriteList_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                TreeNode contactNode = (TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");
                if (contactNode.Level != 0)
                {
                    Point pt = ((TreeView)sender).PointToClient(new Point(e.X, e.Y));
                    TreeNode newgroupNode = (((TreeView)sender).GetNodeAt(pt).Level == 0) ? ((TreeView)sender).GetNodeAt(pt) : ((TreeView)sender).GetNodeAt(pt).Parent;
                    TreeNode oldgroupNode = contactNode.Parent;
                    Contact contact = (Contact)contactNode.Tag;
                    bool flag = true;
                    try
                    {
                        if (newgroupNode.Tag is ContactGroup)
                        {
                            contact.NSMessageHandler.ContactService.AddContactToGroup(contact, (ContactGroup)newgroupNode.Tag);
                        }
                        if (oldgroupNode.Tag is ContactGroup)
                        {
                            contact.NSMessageHandler.ContactService.RemoveContactFromGroup(contact, (ContactGroup)oldgroupNode.Tag);
                        }
                    }
                    catch (Exception)
                    {
                        flag = false;
                    }

                    if (flag)
                    {
                        treeViewFavoriteList.BeginUpdate();
                        TreeNode node3 = (TreeNode)contactNode.Clone();
                        newgroupNode.Nodes.Add(node3);
                        contactNode.Remove();
                        treeViewFavoriteList.EndUpdate();

                        newgroupNode.Text = newgroupNode.Text.Split(new char[] { '/' })[0] + "/" + newgroupNode.Nodes.Count + ")";
                        oldgroupNode.Text = oldgroupNode.Text.Split(new char[] { '/' })[0] + "/" + oldgroupNode.Nodes.Count + ")";
                    }
                }
            }
        }

        private void toolStripDeleteGroup_Click(object sender, EventArgs e)
        {
            ContactGroup selectedGroup = (ContactGroup)treeViewFavoriteList.SelectedNode.Tag;
            this.propertyGrid.SelectedObject = selectedGroup;

            messenger.ContactGroups.Remove(selectedGroup);

            System.Threading.Thread.Sleep(500);
            Application.DoEvents();
            System.Threading.Thread.Sleep(500);

            SortByGroup();            
        }

        private void btnAddNew_Click(object sender, EventArgs e)
        {
            messenger.Nameserver.ContactService.AddNewContact(txtAddNew.Text);
        }

    }
}