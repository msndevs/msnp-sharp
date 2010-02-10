using System.Windows.Forms;

namespace MSNPSharpClient
{
    partial class ClientForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
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
            this.ListPanel = new System.Windows.Forms.Panel();
            this.SortPanel = new System.Windows.Forms.Panel();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnAddNew = new System.Windows.Forms.Button();
            this.btnSortBy = new System.Windows.Forms.Button();
            this.treeViewPanel = new System.Windows.Forms.Panel();
            this.treeViewFavoriteList = new System.Windows.Forms.TreeView();
            this.ImageList1 = new System.Windows.Forms.ImageList(this.components);
            this.treeViewFilterList = new System.Windows.Forms.TreeView();
            this.comboPlaces = new System.Windows.Forms.ComboBox();
            this.userMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.sendIMMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sendOIMMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sendMIMMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.importContactsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.createCircleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.blockMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.unblockMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ContactPanel = new System.Windows.Forms.Panel();
            this.propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.panel1 = new System.Windows.Forms.Panel();
            this.displayImageBox = new System.Windows.Forms.PictureBox();
            this.cbRobotMode = new System.Windows.Forms.CheckBox();
            this.comboProtocol = new System.Windows.Forms.ComboBox();
            this.accountTextBox = new System.Windows.Forms.TextBox();
            this.loginButton = new System.Windows.Forms.Button();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.comboStatus = new System.Windows.Forms.ComboBox();
            this.pnlNameAndPM = new System.Windows.Forms.Panel();
            this.btnSetMusic = new System.Windows.Forms.Button();
            this.lblPM = new System.Windows.Forms.TextBox();
            this.lblName = new System.Windows.Forms.TextBox();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.openImageDialog = new System.Windows.Forms.OpenFileDialog();
            this.tmrKeepOnLine = new System.Windows.Forms.Timer(this.components);
            this.tmrNews = new System.Windows.Forms.Timer(this.components);
            this.statusBar = new System.Windows.Forms.StatusBar();
            this.OwnerPanel = new System.Windows.Forms.Panel();
            this.sortContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripSortByStatus = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSortBygroup = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripDeleteGroup = new System.Windows.Forms.ToolStripMenuItem();
            this.groupContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.toolTipChangePhoto = new System.Windows.Forms.ToolTip(this.components);
            this.pbNewsPicture = new System.Windows.Forms.PictureBox();
            this.WhatsUpPanel = new System.Windows.Forms.Panel();
            this.lblNewsLink = new System.Windows.Forms.LinkLabel();
            this.lblNews = new System.Windows.Forms.Label();
            this.cmdNext = new System.Windows.Forms.Button();
            this.cmdPrev = new System.Windows.Forms.Button();
            this.lblWhatsup = new System.Windows.Forms.Label();
            this.ListPanel.SuspendLayout();
            this.SortPanel.SuspendLayout();
            this.treeViewPanel.SuspendLayout();
            this.userMenuStrip.SuspendLayout();
            this.ContactPanel.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.displayImageBox)).BeginInit();
            this.pnlNameAndPM.SuspendLayout();
            this.OwnerPanel.SuspendLayout();
            this.sortContextMenu.SuspendLayout();
            this.groupContextMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbNewsPicture)).BeginInit();
            this.WhatsUpPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // ListPanel
            // 
            this.ListPanel.BackColor = System.Drawing.SystemColors.Control;
            this.ListPanel.Controls.Add(this.SortPanel);
            this.ListPanel.Controls.Add(this.treeViewPanel);
            this.ListPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListPanel.Location = new System.Drawing.Point(239, 88);
            this.ListPanel.Name = "ListPanel";
            this.ListPanel.Size = new System.Drawing.Size(300, 427);
            this.ListPanel.TabIndex = 0;
            // 
            // SortPanel
            // 
            this.SortPanel.BackColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.SortPanel.Controls.Add(this.txtSearch);
            this.SortPanel.Controls.Add(this.btnAddNew);
            this.SortPanel.Controls.Add(this.btnSortBy);
            this.SortPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.SortPanel.Location = new System.Drawing.Point(0, 0);
            this.SortPanel.Name = "SortPanel";
            this.SortPanel.Size = new System.Drawing.Size(300, 27);
            this.SortPanel.TabIndex = 1;
            // 
            // txtSearch
            // 
            this.txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSearch.ForeColor = System.Drawing.SystemColors.ScrollBar;
            this.txtSearch.Location = new System.Drawing.Point(6, 2);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(199, 20);
            this.txtSearch.TabIndex = 9;
            this.txtSearch.Text = "Search contacts";
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            this.txtSearch.Leave += new System.EventHandler(this.txtSearch_Leave);
            this.txtSearch.Enter += new System.EventHandler(this.txtSearch_Enter);
            // 
            // btnAddNew
            // 
            this.btnAddNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddNew.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnAddNew.Location = new System.Drawing.Point(254, 1);
            this.btnAddNew.Name = "btnAddNew";
            this.btnAddNew.Size = new System.Drawing.Size(36, 21);
            this.btnAddNew.TabIndex = 7;
            this.btnAddNew.Text = "+";
            this.toolTipChangePhoto.SetToolTip(this.btnAddNew, "Add new contact");
            this.btnAddNew.UseVisualStyleBackColor = true;
            this.btnAddNew.Click += new System.EventHandler(this.btnAddNew_Click);
            // 
            // btnSortBy
            // 
            this.btnSortBy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSortBy.BackColor = System.Drawing.SystemColors.Control;
            this.btnSortBy.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.btnSortBy.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnSortBy.Location = new System.Drawing.Point(210, 1);
            this.btnSortBy.Name = "btnSortBy";
            this.btnSortBy.Size = new System.Drawing.Size(38, 21);
            this.btnSortBy.TabIndex = 0;
            this.btnSortBy.Text = "sort";
            this.btnSortBy.UseVisualStyleBackColor = true;
            this.btnSortBy.Click += new System.EventHandler(this.btnSortBy_Click);
            // 
            // treeViewPanel
            // 
            this.treeViewPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.treeViewPanel.Controls.Add(this.treeViewFavoriteList);
            this.treeViewPanel.Controls.Add(this.treeViewFilterList);
            this.treeViewPanel.Location = new System.Drawing.Point(0, 27);
            this.treeViewPanel.Name = "treeViewPanel";
            this.treeViewPanel.Size = new System.Drawing.Size(300, 400);
            this.treeViewPanel.TabIndex = 2;
            // 
            // treeViewFavoriteList
            // 
            this.treeViewFavoriteList.AllowDrop = true;
            this.treeViewFavoriteList.BackColor = System.Drawing.SystemColors.Info;
            this.treeViewFavoriteList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.treeViewFavoriteList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewFavoriteList.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.treeViewFavoriteList.FullRowSelect = true;
            this.treeViewFavoriteList.HideSelection = false;
            this.treeViewFavoriteList.ImageIndex = 0;
            this.treeViewFavoriteList.ImageList = this.ImageList1;
            this.treeViewFavoriteList.Indent = 15;
            this.treeViewFavoriteList.ItemHeight = 20;
            this.treeViewFavoriteList.Location = new System.Drawing.Point(0, 0);
            this.treeViewFavoriteList.Name = "treeViewFavoriteList";
            this.treeViewFavoriteList.SelectedImageIndex = 0;
            this.treeViewFavoriteList.ShowLines = false;
            this.treeViewFavoriteList.ShowPlusMinus = false;
            this.treeViewFavoriteList.ShowRootLines = false;
            this.treeViewFavoriteList.Size = new System.Drawing.Size(300, 400);
            this.treeViewFavoriteList.TabIndex = 0;
            this.treeViewFavoriteList.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseDoubleClick);
            this.treeViewFavoriteList.DragDrop += new System.Windows.Forms.DragEventHandler(this.treeViewFavoriteList_DragDrop);
            this.treeViewFavoriteList.DragEnter += new System.Windows.Forms.DragEventHandler(this.treeViewFavoriteList_DragEnter);
            this.treeViewFavoriteList.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
            this.treeViewFavoriteList.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.treeViewFavoriteList_ItemDrag);
            this.treeViewFavoriteList.DragOver += new System.Windows.Forms.DragEventHandler(this.treeViewFavoriteList_DragOver);
            // 
            // ImageList1
            // 
            this.ImageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.ImageList1.ImageSize = new System.Drawing.Size(10, 10);
            this.ImageList1.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // treeViewFilterList
            // 
            this.treeViewFilterList.BackColor = System.Drawing.SystemColors.Info;
            this.treeViewFilterList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.treeViewFilterList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewFilterList.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.treeViewFilterList.FullRowSelect = true;
            this.treeViewFilterList.HideSelection = false;
            this.treeViewFilterList.Indent = 20;
            this.treeViewFilterList.ItemHeight = 20;
            this.treeViewFilterList.Location = new System.Drawing.Point(0, 0);
            this.treeViewFilterList.Name = "treeViewFilterList";
            this.treeViewFilterList.ShowLines = false;
            this.treeViewFilterList.ShowPlusMinus = false;
            this.treeViewFilterList.ShowRootLines = false;
            this.treeViewFilterList.Size = new System.Drawing.Size(300, 400);
            this.treeViewFilterList.TabIndex = 0;
            this.treeViewFilterList.Visible = false;
            this.treeViewFilterList.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseDoubleClick);
            this.treeViewFilterList.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
            // 
            // comboPlaces
            // 
            this.comboPlaces.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboPlaces.DropDownWidth = 170;
            this.comboPlaces.FormattingEnabled = true;
            this.comboPlaces.Location = new System.Drawing.Point(134, 53);
            this.comboPlaces.Name = "comboPlaces";
            this.comboPlaces.Size = new System.Drawing.Size(70, 21);
            this.comboPlaces.TabIndex = 5;
            this.toolTipChangePhoto.SetToolTip(this.comboPlaces, "Places you signed on");
            this.comboPlaces.Visible = false;
            this.comboPlaces.SelectedIndexChanged += new System.EventHandler(this.comboPlaces_SelectedIndexChanged);
            // 
            // userMenuStrip
            // 
            this.userMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sendIMMenuItem,
            this.sendOIMMenuItem,
            this.sendMIMMenuItem,
            this.toolStripMenuItem3,
            this.importContactsMenuItem,
            this.createCircleMenuItem,
            this.toolStripMenuItem2,
            this.blockMenuItem,
            this.unblockMenuItem,
            this.deleteMenuItem});
            this.userMenuStrip.Name = "contextMenuStrip1";
            this.userMenuStrip.Size = new System.Drawing.Size(201, 192);
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
            // importContactsMenuItem
            // 
            this.importContactsMenuItem.Name = "importContactsMenuItem";
            this.importContactsMenuItem.Size = new System.Drawing.Size(200, 22);
            this.importContactsMenuItem.Text = "Import Contacts";
            this.importContactsMenuItem.Click += new System.EventHandler(this.importContactsMenuItem_Click);
            // 
            // createCircleMenuItem
            // 
            this.createCircleMenuItem.Name = "createCircleMenuItem";
            this.createCircleMenuItem.Size = new System.Drawing.Size(200, 22);
            this.createCircleMenuItem.Text = "Circle Tests";
            this.createCircleMenuItem.Click += new System.EventHandler(this.createCircleMenuItem_Click);
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
            // deleteMenuItem
            // 
            this.deleteMenuItem.Name = "deleteMenuItem";
            this.deleteMenuItem.Size = new System.Drawing.Size(200, 22);
            this.deleteMenuItem.Text = "Delete";
            this.deleteMenuItem.Click += new System.EventHandler(this.deleteMenuItem_Click);
            // 
            // ContactPanel
            // 
            this.ContactPanel.BackColor = System.Drawing.SystemColors.Control;
            this.ContactPanel.Controls.Add(this.propertyGrid);
            this.ContactPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.ContactPanel.Location = new System.Drawing.Point(0, 88);
            this.ContactPanel.Name = "ContactPanel";
            this.ContactPanel.Size = new System.Drawing.Size(239, 427);
            this.ContactPanel.TabIndex = 2;
            // 
            // propertyGrid
            // 
            this.propertyGrid.BackColor = System.Drawing.SystemColors.Control;
            this.propertyGrid.CommandsBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(128)))));
            this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid.LineColor = System.Drawing.SystemColors.ScrollBar;
            this.propertyGrid.Location = new System.Drawing.Point(0, 0);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.Size = new System.Drawing.Size(239, 427);
            this.propertyGrid.TabIndex = 4;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.panel1.Controls.Add(this.displayImageBox);
            this.panel1.Controls.Add(this.cbRobotMode);
            this.panel1.Controls.Add(this.comboPlaces);
            this.panel1.Controls.Add(this.comboProtocol);
            this.panel1.Controls.Add(this.accountTextBox);
            this.panel1.Controls.Add(this.loginButton);
            this.panel1.Controls.Add(this.passwordTextBox);
            this.panel1.Controls.Add(this.comboStatus);
            this.panel1.Location = new System.Drawing.Point(239, 6);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(290, 77);
            this.panel1.TabIndex = 5;
            // 
            // displayImageBox
            // 
            this.displayImageBox.BackColor = System.Drawing.Color.White;
            this.displayImageBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.displayImageBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.displayImageBox.Dock = System.Windows.Forms.DockStyle.Right;
            this.displayImageBox.Location = new System.Drawing.Point(213, 0);
            this.displayImageBox.Name = "displayImageBox";
            this.displayImageBox.Size = new System.Drawing.Size(77, 77);
            this.displayImageBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.displayImageBox.TabIndex = 2;
            this.displayImageBox.TabStop = false;
            this.toolTipChangePhoto.SetToolTip(this.displayImageBox, "Click to change the photo");
            this.displayImageBox.Click += new System.EventHandler(this.displayImageBox_Click);
            // 
            // cbRobotMode
            // 
            this.cbRobotMode.AutoSize = true;
            this.cbRobotMode.Location = new System.Drawing.Point(134, 9);
            this.cbRobotMode.Name = "cbRobotMode";
            this.cbRobotMode.Size = new System.Drawing.Size(71, 17);
            this.cbRobotMode.TabIndex = 7;
            this.cbRobotMode.Text = "Bot mode";
            this.toolTipChangePhoto.SetToolTip(this.cbRobotMode, "If your account is provisioned, check this. This sets AutoSynchronize property to" +
                    " false when connected; that will make MSNPSharp don\'t use Address Book anymore, " +
                    "so your contact list isn\'t loaded.");
            this.cbRobotMode.UseVisualStyleBackColor = true;
            this.cbRobotMode.CheckedChanged += new System.EventHandler(this.cbRobotMode_CheckedChanged);
            // 
            // comboProtocol
            // 
            this.comboProtocol.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboProtocol.DropDownWidth = 75;
            this.comboProtocol.FormattingEnabled = true;
            this.comboProtocol.Items.AddRange(new object[] {
            "MSNP18"});
            this.comboProtocol.Location = new System.Drawing.Point(134, 53);
            this.comboProtocol.Name = "comboProtocol";
            this.comboProtocol.Size = new System.Drawing.Size(70, 21);
            this.comboProtocol.TabIndex = 6;
            this.toolTipChangePhoto.SetToolTip(this.comboProtocol, "Msn protocol used");
            // 
            // accountTextBox
            // 
            this.accountTextBox.Location = new System.Drawing.Point(6, 6);
            this.accountTextBox.Name = "accountTextBox";
            this.accountTextBox.Size = new System.Drawing.Size(121, 20);
            this.accountTextBox.TabIndex = 2;
            this.accountTextBox.Text = "testmsnpsharp@live.cn";
            this.accountTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.login_KeyPress);
            // 
            // loginButton
            // 
            this.loginButton.Location = new System.Drawing.Point(134, 28);
            this.loginButton.Name = "loginButton";
            this.loginButton.Size = new System.Drawing.Size(70, 21);
            this.loginButton.TabIndex = 1;
            this.loginButton.Tag = "0";
            this.loginButton.Text = "> Sign in";
            this.loginButton.UseVisualStyleBackColor = true;
            this.loginButton.Click += new System.EventHandler(this.loginButton_Click);
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.Location = new System.Drawing.Point(6, 28);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.PasswordChar = '*';
            this.passwordTextBox.Size = new System.Drawing.Size(121, 20);
            this.passwordTextBox.TabIndex = 3;
            this.passwordTextBox.Text = "tstmsnpshrp@123";
            this.passwordTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.login_KeyPress);
            // 
            // comboStatus
            // 
            this.comboStatus.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboStatus.FormattingEnabled = true;
            this.comboStatus.ItemHeight = 15;
            this.comboStatus.Items.AddRange(new object[] {
            "Online",
            "Busy",
            "Away",
            "Hidden",
            "Offline"});
            this.comboStatus.Location = new System.Drawing.Point(6, 53);
            this.comboStatus.Name = "comboStatus";
            this.comboStatus.Size = new System.Drawing.Size(121, 21);
            this.comboStatus.TabIndex = 4;
            this.toolTipChangePhoto.SetToolTip(this.comboStatus, "Your status");
            this.comboStatus.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.comboStatus_DrawItem);
            this.comboStatus.SelectedIndexChanged += new System.EventHandler(this.comboStatus_SelectedIndexChanged);
            this.comboStatus.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.comboStatus_KeyPress);
            // 
            // pnlNameAndPM
            // 
            this.pnlNameAndPM.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlNameAndPM.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.pnlNameAndPM.Controls.Add(this.btnSetMusic);
            this.pnlNameAndPM.Controls.Add(this.lblPM);
            this.pnlNameAndPM.Controls.Add(this.lblName);
            this.pnlNameAndPM.Location = new System.Drawing.Point(29, 6);
            this.pnlNameAndPM.Name = "pnlNameAndPM";
            this.pnlNameAndPM.Size = new System.Drawing.Size(204, 49);
            this.pnlNameAndPM.TabIndex = 1;
            this.pnlNameAndPM.Visible = false;
            // 
            // btnSetMusic
            // 
            this.btnSetMusic.Location = new System.Drawing.Point(175, 24);
            this.btnSetMusic.Name = "btnSetMusic";
            this.btnSetMusic.Size = new System.Drawing.Size(26, 21);
            this.btnSetMusic.TabIndex = 5;
            this.btnSetMusic.Tag = "0";
            this.btnSetMusic.Text = "M";
            this.toolTipChangePhoto.SetToolTip(this.btnSetMusic, "Set music");
            this.btnSetMusic.UseVisualStyleBackColor = true;
            this.btnSetMusic.Click += new System.EventHandler(this.btnSetMusic_Click);
            // 
            // lblPM
            // 
            this.lblPM.Location = new System.Drawing.Point(4, 25);
            this.lblPM.Name = "lblPM";
            this.lblPM.Size = new System.Drawing.Size(166, 20);
            this.lblPM.TabIndex = 1;
            this.lblPM.Leave += new System.EventHandler(this.lblName_Leave);
            // 
            // lblName
            // 
            this.lblName.Location = new System.Drawing.Point(4, 2);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(197, 20);
            this.lblName.TabIndex = 0;
            this.lblName.Leave += new System.EventHandler(this.lblName_Leave);
            // 
            // openFileDialog
            // 
            this.openFileDialog.Multiselect = true;
            // 
            // openImageDialog
            // 
            this.openImageDialog.Filter = "Supported Images|*.png;*.jpg;*.jpeg;*.gif";
            this.openImageDialog.Multiselect = true;
            this.openImageDialog.Title = "Select display image";
            // 
            // tmrKeepOnLine
            // 
            this.tmrKeepOnLine.Interval = 1000;
            this.tmrKeepOnLine.Tick += new System.EventHandler(this.tmrKeepOnLine_Tick);
            // 
            // tmrNews
            // 
            this.tmrNews.Interval = 5000;
            this.tmrNews.Tick += new System.EventHandler(this.tmrNews_Tick);
            // 
            // statusBar
            // 
            this.statusBar.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.statusBar.Location = new System.Drawing.Point(0, 0);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(539, 26);
            this.statusBar.TabIndex = 5;
            // 
            // OwnerPanel
            // 
            this.OwnerPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.OwnerPanel.Controls.Add(this.statusBar);
            this.OwnerPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.OwnerPanel.Location = new System.Drawing.Point(0, 561);
            this.OwnerPanel.Name = "OwnerPanel";
            this.OwnerPanel.Size = new System.Drawing.Size(539, 26);
            this.OwnerPanel.TabIndex = 1;
            // 
            // sortContextMenu
            // 
            this.sortContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSortByStatus,
            this.toolStripSortBygroup});
            this.sortContextMenu.Name = "sortContextMenu";
            this.sortContextMenu.ShowCheckMargin = true;
            this.sortContextMenu.ShowImageMargin = false;
            this.sortContextMenu.Size = new System.Drawing.Size(140, 48);
            // 
            // toolStripSortByStatus
            // 
            this.toolStripSortByStatus.Checked = true;
            this.toolStripSortByStatus.CheckOnClick = true;
            this.toolStripSortByStatus.CheckState = System.Windows.Forms.CheckState.Checked;
            this.toolStripSortByStatus.Name = "toolStripSortByStatus";
            this.toolStripSortByStatus.ShowShortcutKeys = false;
            this.toolStripSortByStatus.Size = new System.Drawing.Size(139, 22);
            this.toolStripSortByStatus.Text = "Sort by status";
            this.toolStripSortByStatus.Click += new System.EventHandler(this.toolStripSortByStatus_Click);
            // 
            // toolStripSortBygroup
            // 
            this.toolStripSortBygroup.CheckOnClick = true;
            this.toolStripSortBygroup.Name = "toolStripSortBygroup";
            this.toolStripSortBygroup.ShowShortcutKeys = false;
            this.toolStripSortBygroup.Size = new System.Drawing.Size(139, 22);
            this.toolStripSortBygroup.Text = "Sort by group";
            this.toolStripSortBygroup.Click += new System.EventHandler(this.toolStripSortBygroup_Click);
            // 
            // toolStripDeleteGroup
            // 
            this.toolStripDeleteGroup.Name = "toolStripDeleteGroup";
            this.toolStripDeleteGroup.Size = new System.Drawing.Size(142, 22);
            this.toolStripDeleteGroup.Text = "Delete group";
            this.toolStripDeleteGroup.Click += new System.EventHandler(this.toolStripDeleteGroup_Click);
            // 
            // groupContextMenu
            // 
            this.groupContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripDeleteGroup});
            this.groupContextMenu.Name = "sortContextMenu";
            this.groupContextMenu.ShowCheckMargin = true;
            this.groupContextMenu.ShowImageMargin = false;
            this.groupContextMenu.Size = new System.Drawing.Size(143, 26);
            // 
            // pictureBox
            // 
            this.pictureBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.pictureBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.pictureBox.Image = global::MSNPSharpClient.Properties.Resources.listbg;
            this.pictureBox.Location = new System.Drawing.Point(0, 0);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(539, 88);
            this.pictureBox.TabIndex = 5;
            this.pictureBox.TabStop = false;
            // 
            // pbNewsPicture
            // 
            this.pbNewsPicture.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pbNewsPicture.BackColor = System.Drawing.SystemColors.Highlight;
            this.pbNewsPicture.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbNewsPicture.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbNewsPicture.Location = new System.Drawing.Point(494, 1);
            this.pbNewsPicture.Name = "pbNewsPicture";
            this.pbNewsPicture.Size = new System.Drawing.Size(42, 42);
            this.pbNewsPicture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbNewsPicture.TabIndex = 3;
            this.pbNewsPicture.TabStop = false;
            this.toolTipChangePhoto.SetToolTip(this.pbNewsPicture, "Display Picture");
            // 
            // WhatsUpPanel
            // 
            this.WhatsUpPanel.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.WhatsUpPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.WhatsUpPanel.Controls.Add(this.lblNewsLink);
            this.WhatsUpPanel.Controls.Add(this.lblNews);
            this.WhatsUpPanel.Controls.Add(this.pbNewsPicture);
            this.WhatsUpPanel.Controls.Add(this.cmdNext);
            this.WhatsUpPanel.Controls.Add(this.cmdPrev);
            this.WhatsUpPanel.Controls.Add(this.lblWhatsup);
            this.WhatsUpPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.WhatsUpPanel.Location = new System.Drawing.Point(0, 515);
            this.WhatsUpPanel.Name = "WhatsUpPanel";
            this.WhatsUpPanel.Size = new System.Drawing.Size(539, 46);
            this.WhatsUpPanel.TabIndex = 1;
            // 
            // lblNewsLink
            // 
            this.lblNewsLink.Location = new System.Drawing.Point(428, 22);
            this.lblNewsLink.Name = "lblNewsLink";
            this.lblNewsLink.Size = new System.Drawing.Size(58, 20);
            this.lblNewsLink.TabIndex = 5;
            this.lblNewsLink.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblNewsLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblNewsLink_LinkClicked);
            // 
            // lblNews
            // 
            this.lblNews.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.lblNews.AutoEllipsis = true;
            this.lblNews.Location = new System.Drawing.Point(81, 3);
            this.lblNews.Name = "lblNews";
            this.lblNews.Size = new System.Drawing.Size(341, 39);
            this.lblNews.TabIndex = 4;
            this.lblNews.Text = " *";
            this.lblNews.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmdNext
            // 
            this.cmdNext.Location = new System.Drawing.Point(42, 20);
            this.cmdNext.Name = "cmdNext";
            this.cmdNext.Size = new System.Drawing.Size(29, 23);
            this.cmdNext.TabIndex = 2;
            this.cmdNext.Text = ">";
            this.cmdNext.UseVisualStyleBackColor = true;
            this.cmdNext.Click += new System.EventHandler(this.cmdNext_Click);
            // 
            // cmdPrev
            // 
            this.cmdPrev.Location = new System.Drawing.Point(11, 20);
            this.cmdPrev.Name = "cmdPrev";
            this.cmdPrev.Size = new System.Drawing.Size(29, 23);
            this.cmdPrev.TabIndex = 1;
            this.cmdPrev.Text = "<";
            this.cmdPrev.UseVisualStyleBackColor = true;
            this.cmdPrev.Click += new System.EventHandler(this.cmdPrev_Click);
            // 
            // lblWhatsup
            // 
            this.lblWhatsup.AutoSize = true;
            this.lblWhatsup.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.lblWhatsup.Location = new System.Drawing.Point(9, 3);
            this.lblWhatsup.Name = "lblWhatsup";
            this.lblWhatsup.Size = new System.Drawing.Size(66, 13);
            this.lblWhatsup.TabIndex = 0;
            this.lblWhatsup.Text = "What\'s Up";
            // 
            // ClientForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(539, 587);
            this.Controls.Add(this.pnlNameAndPM);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.ListPanel);
            this.Controls.Add(this.ContactPanel);
            this.Controls.Add(this.WhatsUpPanel);
            this.Controls.Add(this.OwnerPanel);
            this.Controls.Add(this.pictureBox);
            this.Name = "ClientForm";
            this.Text = "MSNPSharp Example Client (v3.0.2 - r1435)";
            this.Load += new System.EventHandler(this.ClientForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(ClientForm_FormClosing);
            this.ListPanel.ResumeLayout(false);
            this.SortPanel.ResumeLayout(false);
            this.SortPanel.PerformLayout();
            this.treeViewPanel.ResumeLayout(false);
            this.userMenuStrip.ResumeLayout(false);
            this.ContactPanel.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.displayImageBox)).EndInit();
            this.pnlNameAndPM.ResumeLayout(false);
            this.pnlNameAndPM.PerformLayout();
            this.OwnerPanel.ResumeLayout(false);
            this.sortContextMenu.ResumeLayout(false);
            this.groupContextMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbNewsPicture)).EndInit();
            this.WhatsUpPanel.ResumeLayout(false);
            this.WhatsUpPanel.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion

        private Panel ListPanel;
        private Panel ContactPanel;
        private PictureBox pictureBox;
        private PictureBox displayImageBox;
        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;
        private OpenFileDialog openImageDialog;
        private System.Windows.Forms.Timer tmrKeepOnLine;
        private System.Windows.Forms.Timer tmrNews;
        private TreeView treeViewFavoriteList;
        private ImageList ImageList1;
        private ContextMenuStrip userMenuStrip;
        private ToolStripMenuItem sendIMMenuItem;
        private ToolStripMenuItem blockMenuItem;
        private ToolStripSeparator toolStripMenuItem2;
        private ToolStripMenuItem unblockMenuItem;
        private ToolStripMenuItem sendOIMMenuItem;
        private ToolStripSeparator toolStripMenuItem3;
        private ToolStripMenuItem sendMIMMenuItem;
        private StatusBar statusBar;
        private Panel OwnerPanel;
        private Panel panel1;
        private TextBox accountTextBox;
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
        private Button btnAddNew;
        private ToolStripMenuItem importContactsMenuItem;
        private ToolStripMenuItem createCircleMenuItem;
        private TreeView treeViewFilterList;
        private TextBox txtSearch;
        private ToolStripMenuItem deleteMenuItem;
        private ComboBox comboStatus;
        private Panel pnlNameAndPM;
        private TextBox lblName;
        private TextBox lblPM;
        private ToolTip toolTipChangePhoto;
        private ComboBox comboPlaces;
        private Panel WhatsUpPanel;
        private Label lblWhatsup;
        private Button cmdNext;
        private Button cmdPrev;
        private PictureBox pbNewsPicture;
        private Label lblNews;
        private LinkLabel lblNewsLink;
        private ComboBox comboProtocol;
        private CheckBox cbRobotMode;
        private Button btnSetMusic;

    }
}
