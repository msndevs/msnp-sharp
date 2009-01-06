using System;
using System.IO;
using System.Data;
using System.Drawing;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;

namespace MSNPSharpClient
{
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
        private List<ConversationForm> convforms = new List<ConversationForm>(0);

        public List<ConversationForm> ConversationForms
        {
            get
            {
                return convforms;
            }
        }

        public Messenger Messenger
        {
            get
            {
                return messenger;
            }
        }

        #region Form controls
        private Panel ListPanel;
        private Panel ContactPanel;
        private PictureBox pictureBox;
        private PictureBox displayImageBox;
        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;
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
        private TreeView treeViewFilterList;
        private TextBox txtSearch;
        private ToolStripMenuItem deleteMenuItem;
        private ToolStripSeparator toolStripMenuItem4;
        private ComboBox comboStatus;
        private Panel pnlNameAndPM;
        private TextBox lblName;
        private TextBox lblPM;
        private ToolStripMenuItem viewContactCardMenuItem;
        private ToolTip toolTipChangePhoto;
        private IContainer components;
        #endregion

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.ListPanel = new System.Windows.Forms.Panel();
            this.treeViewPanel = new System.Windows.Forms.Panel();
            this.treeViewFavoriteList = new System.Windows.Forms.TreeView();
            this.treeViewFilterList = new System.Windows.Forms.TreeView();
            this.SortPanel = new System.Windows.Forms.Panel();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnAddNew = new System.Windows.Forms.Button();
            this.btnSortBy = new System.Windows.Forms.Button();
            this.userMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.sendIMMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sendOIMMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sendMIMMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.importContactsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.blockMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.unblockMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            this.viewContactCardMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ContactPanel = new System.Windows.Forms.Panel();
            this.propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.panel1 = new System.Windows.Forms.Panel();
            this.pnlNameAndPM = new System.Windows.Forms.Panel();
            this.lblPM = new System.Windows.Forms.TextBox();
            this.lblName = new System.Windows.Forms.TextBox();
            this.displayImageBox = new System.Windows.Forms.PictureBox();
            this.accountTextBox = new System.Windows.Forms.TextBox();
            this.loginButton = new System.Windows.Forms.Button();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.comboStatus = new System.Windows.Forms.ComboBox();
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
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.toolTipChangePhoto = new System.Windows.Forms.ToolTip(this.components);
            this.ListPanel.SuspendLayout();
            this.treeViewPanel.SuspendLayout();
            this.SortPanel.SuspendLayout();
            this.userMenuStrip.SuspendLayout();
            this.ContactPanel.SuspendLayout();
            this.panel1.SuspendLayout();
            this.pnlNameAndPM.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.displayImageBox)).BeginInit();
            this.OwnerPanel.SuspendLayout();
            this.sortContextMenu.SuspendLayout();
            this.groupContextMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // ListPanel
            // 
            this.ListPanel.BackColor = System.Drawing.SystemColors.Window;
            this.ListPanel.Controls.Add(this.treeViewPanel);
            this.ListPanel.Controls.Add(this.SortPanel);
            this.ListPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListPanel.Location = new System.Drawing.Point(239, 88);
            this.ListPanel.Name = "ListPanel";
            this.ListPanel.Size = new System.Drawing.Size(300, 522);
            this.ListPanel.TabIndex = 0;
            // 
            // treeViewPanel
            // 
            this.treeViewPanel.Controls.Add(this.treeViewFavoriteList);
            this.treeViewPanel.Controls.Add(this.treeViewFilterList);
            this.treeViewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewPanel.Location = new System.Drawing.Point(0, 27);
            this.treeViewPanel.Name = "treeViewPanel";
            this.treeViewPanel.Size = new System.Drawing.Size(300, 495);
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
            this.treeViewFavoriteList.Size = new System.Drawing.Size(300, 495);
            this.treeViewFavoriteList.TabIndex = 0;
            this.treeViewFavoriteList.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseDoubleClick);
            this.treeViewFavoriteList.DragDrop += new System.Windows.Forms.DragEventHandler(this.treeViewFavoriteList_DragDrop);
            this.treeViewFavoriteList.DragEnter += new System.Windows.Forms.DragEventHandler(this.treeViewFavoriteList_DragEnter);
            this.treeViewFavoriteList.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
            this.treeViewFavoriteList.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.treeViewFavoriteList_ItemDrag);
            this.treeViewFavoriteList.DragOver += new System.Windows.Forms.DragEventHandler(this.treeViewFavoriteList_DragOver);
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
            this.treeViewFilterList.Size = new System.Drawing.Size(300, 495);
            this.treeViewFilterList.TabIndex = 0;
            this.treeViewFilterList.Visible = false;
            this.treeViewFilterList.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseDoubleClick);
            this.treeViewFilterList.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
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
            // userMenuStrip
            // 
            this.userMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sendIMMenuItem,
            this.sendOIMMenuItem,
            this.sendMIMMenuItem,
            this.toolStripMenuItem3,
            this.importContactsMenuItem,
            this.toolStripMenuItem2,
            this.blockMenuItem,
            this.unblockMenuItem,
            this.deleteMenuItem,
            this.toolStripMenuItem4,
            this.viewContactCardMenuItem});
            this.userMenuStrip.Name = "contextMenuStrip1";
            this.userMenuStrip.Size = new System.Drawing.Size(201, 220);
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
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(197, 6);
            // 
            // viewContactCardMenuItem
            // 
            this.viewContactCardMenuItem.Name = "viewContactCardMenuItem";
            this.viewContactCardMenuItem.Size = new System.Drawing.Size(200, 22);
            this.viewContactCardMenuItem.Text = "View Contact Card";
            this.viewContactCardMenuItem.Click += new System.EventHandler(this.viewContactCardToolStripMenuItem_Click);
            // 
            // ContactPanel
            // 
            this.ContactPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(128)))));
            this.ContactPanel.Controls.Add(this.propertyGrid);
            this.ContactPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.ContactPanel.Location = new System.Drawing.Point(0, 88);
            this.ContactPanel.Name = "ContactPanel";
            this.ContactPanel.Size = new System.Drawing.Size(239, 522);
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
            this.propertyGrid.Size = new System.Drawing.Size(239, 522);
            this.propertyGrid.TabIndex = 4;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.panel1.Controls.Add(this.pnlNameAndPM);
            this.panel1.Controls.Add(this.displayImageBox);
            this.panel1.Controls.Add(this.accountTextBox);
            this.panel1.Controls.Add(this.loginButton);
            this.panel1.Controls.Add(this.passwordTextBox);
            this.panel1.Controls.Add(this.comboStatus);
            this.panel1.Location = new System.Drawing.Point(239, 6);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(290, 77);
            this.panel1.TabIndex = 5;
            // 
            // pnlNameAndPM
            // 
            this.pnlNameAndPM.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.pnlNameAndPM.Controls.Add(this.lblPM);
            this.pnlNameAndPM.Controls.Add(this.lblName);
            this.pnlNameAndPM.Location = new System.Drawing.Point(3, 3);
            this.pnlNameAndPM.Name = "pnlNameAndPM";
            this.pnlNameAndPM.Size = new System.Drawing.Size(204, 49);
            this.pnlNameAndPM.TabIndex = 1;
            this.pnlNameAndPM.Visible = false;
            // 
            // lblPM
            // 
            this.lblPM.Location = new System.Drawing.Point(4, 25);
            this.lblPM.Name = "lblPM";
            this.lblPM.Size = new System.Drawing.Size(197, 20);
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
            // accountTextBox
            // 
            this.accountTextBox.Location = new System.Drawing.Point(6, 6);
            this.accountTextBox.Name = "accountTextBox";
            this.accountTextBox.Size = new System.Drawing.Size(198, 20);
            this.accountTextBox.TabIndex = 1;
            this.accountTextBox.Text = "msnpsharp@live.cn";
            this.accountTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.login_KeyPress);
            // 
            // loginButton
            // 
            this.loginButton.Location = new System.Drawing.Point(148, 28);
            this.loginButton.Name = "loginButton";
            this.loginButton.Size = new System.Drawing.Size(56, 21);
            this.loginButton.TabIndex = 4;
            this.loginButton.Tag = "0";
            this.loginButton.Text = "> Sign in";
            this.loginButton.UseVisualStyleBackColor = true;
            this.loginButton.Click += new System.EventHandler(this.loginButton_Click);
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.Location = new System.Drawing.Point(7, 28);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.PasswordChar = '*';
            this.passwordTextBox.Size = new System.Drawing.Size(135, 20);
            this.passwordTextBox.TabIndex = 2;
            this.passwordTextBox.Text = "123456";
            this.passwordTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.login_KeyPress);
            // 
            // comboStatus
            // 
            this.comboStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboStatus.FormattingEnabled = true;
            this.comboStatus.Items.AddRange(new object[] {
            "Online",
            "Busy",
            "Away",
            "Hidden",
            "Offline"});
            this.comboStatus.Location = new System.Drawing.Point(7, 54);
            this.comboStatus.Name = "comboStatus";
            this.comboStatus.Size = new System.Drawing.Size(197, 21);
            this.comboStatus.TabIndex = 3;
            this.comboStatus.SelectedIndexChanged += new System.EventHandler(this.comboStatus_SelectedIndexChanged);
            this.comboStatus.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.comboStatus_KeyPress);
            // 
            // openFileDialog
            // 
            this.openFileDialog.Multiselect = true;
            // 
            // openImageDialog
            // 
            this.openImageDialog.Filter = "Supported Images|*.png;*.jpg;*.jpeg";
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
            this.statusBar.Location = new System.Drawing.Point(0, 4);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(539, 22);
            this.statusBar.TabIndex = 5;
            // 
            // OwnerPanel
            // 
            this.OwnerPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(163)))), ((int)(((byte)(163)))), ((int)(((byte)(186)))));
            this.OwnerPanel.Controls.Add(this.statusBar);
            this.OwnerPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.OwnerPanel.Location = new System.Drawing.Point(0, 610);
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
            // ClientForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(539, 636);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.ListPanel);
            this.Controls.Add(this.ContactPanel);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.OwnerPanel);
            this.Name = "ClientForm";
            this.Text = "MSNPSharp Example Client for MSNP18 (3.0 Dev)";
            this.ListPanel.ResumeLayout(false);
            this.treeViewPanel.ResumeLayout(false);
            this.SortPanel.ResumeLayout(false);
            this.SortPanel.PerformLayout();
            this.userMenuStrip.ResumeLayout(false);
            this.ContactPanel.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.pnlNameAndPM.ResumeLayout(false);
            this.pnlNameAndPM.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.displayImageBox)).EndInit();
            this.OwnerPanel.ResumeLayout(false);
            this.sortContextMenu.ResumeLayout(false);
            this.groupContextMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);

        }
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
            messenger.NameserverProcessor.ConnectionEstablished += new EventHandler<EventArgs>(NameserverProcessor_ConnectionEstablished);
            messenger.Nameserver.SignedIn += new EventHandler<EventArgs>(Nameserver_SignedIn);
            messenger.Nameserver.SignedOff += new EventHandler<SignedOffEventArgs>(Nameserver_SignedOff);
            messenger.NameserverProcessor.ConnectingException += new EventHandler<ExceptionEventArgs>(NameserverProcessor_ConnectingException);
            messenger.Nameserver.ExceptionOccurred += new EventHandler<ExceptionEventArgs>(Nameserver_ExceptionOccurred);
            messenger.Nameserver.AuthenticationError += new EventHandler<ExceptionEventArgs>(Nameserver_AuthenticationError);
            messenger.Nameserver.ServerErrorReceived += new EventHandler<MSNErrorEventArgs>(Nameserver_ServerErrorReceived);
            messenger.ConversationCreated += new EventHandler<ConversationCreatedEventArgs>(messenger_ConversationCreated);
            messenger.TransferInvitationReceived += new EventHandler<MSNSLPInvitationEventArgs>(messenger_TransferInvitationReceived);
            messenger.Nameserver.PingAnswer += new EventHandler<PingAnswerEventArgs>(Nameserver_PingAnswer);

            messenger.Nameserver.ContactOnline += new EventHandler<ContactEventArgs>(Nameserver_ContactOnline);
            messenger.Nameserver.ContactOffline += new EventHandler<ContactEventArgs>(Nameserver_ContactOffline);

            messenger.Nameserver.ContactService.ReverseAdded += new EventHandler<ContactEventArgs>(Nameserver_ReverseAdded);

            messenger.Nameserver.Owner.DisplayImageChanged += new EventHandler<EventArgs>(Owner_DisplayImageChanged);
            messenger.Nameserver.Owner.PersonalMessageChanged += new EventHandler<EventArgs>(Owner_PersonalMessageChanged);
            messenger.Nameserver.Owner.ScreenNameChanged += new EventHandler<EventArgs>(Owner_PersonalMessageChanged);
            messenger.Nameserver.SpaceService.ContactCardCompleted += new EventHandler<ContactCardCompletedEventArgs>(SpaceService_ContactCardCompleted);

            messenger.Nameserver.OIMService.OIMReceived += new EventHandler<OIMReceivedEventArgs>(Nameserver_OIMReceived);
            messenger.Nameserver.OIMService.OIMSendCompleted += new EventHandler<OIMSendCompletedEventArgs>(OIMService_OIMSendCompleted);

            treeViewFavoriteList.TreeViewNodeSorter = StatusSorter.Default;

            if (toolStripSortByStatus.Checked)
                SortByStatus();
            else
                SortByGroup();

            comboStatus.SelectedIndex = 0;

        }

        void SpaceService_ContactCardCompleted(object sender, ContactCardCompletedEventArgs arg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new EventHandler<ContactCardCompletedEventArgs>(SpaceService_ContactCardCompleted),
                    new object[] { sender, arg });
            }
            else
            {
                if (arg.Error == null && arg.Changed)
                {
                    ContactCardForm ccform = new ContactCardForm(arg.ContactCard);
                    ccform.Show();
                }
            }
        }

        void Owner_PersonalMessageChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<EventArgs>(Owner_PersonalMessageChanged), sender, e);
                return;
            }

            lblName.Text = messenger.Owner.Name;

            if (messenger.Owner.PersonalMessage != null && messenger.Owner.PersonalMessage.Message != null)
            {
                lblPM.Text = System.Web.HttpUtility.HtmlDecode(messenger.Owner.PersonalMessage.Message);
            }
        }

        void Owner_DisplayImageChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<EventArgs>(Owner_DisplayImageChanged), sender, e);
                return;
            }

            displayImageBox.Image = messenger.Owner.DisplayImage.Image;
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
            Invoke(new EventHandler<ContactEventArgs>(ContactOnlineOfline), sender, e);
        }
        void Nameserver_ContactOffline(object sender, ContactEventArgs e)
        {
            Invoke(new EventHandler<ContactEventArgs>(ContactOnlineOfline), sender, e);
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
                Invoke(new EventHandler<OIMReceivedEventArgs>(Nameserver_OIMReceived), sender, e);
                return;
            }

            if (DialogResult.Yes == MessageBox.Show(
                "OIM received at : " + e.ReceivedTime + "\r\nFrom : " + e.NickName + " (" + e.Email + ") " + ":\r\n"
                + e.Message + "\r\n\r\n\r\nClick yes, if you want to receive this message next time you login.",
                "Offline Message from " + e.Email, MessageBoxButtons.YesNoCancel))
            {
                e.IsRead = false;
            }
        }

        void OIMService_OIMSendCompleted(object sender, OIMSendCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<OIMSendCompletedEventArgs>(OIMService_OIMSendCompleted), sender, e);
                return;
            }

            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, "OIM Send Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void Nameserver_ReverseAdded(object sender, ContactEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<ContactEventArgs>(Nameserver_ReverseAdded), sender, e);
                return;
            }

            Contact contact = e.Contact;
            if (contact.NSMessageHandler.Owner.NotifyPrivacy == NotifyPrivacy.PromptOnAdd)
            {
                if (contact.OnPendingList || (contact.OnReverseList && !contact.OnAllowedList && !contact.OnBlockedList && !contact.OnPendingList))
                {
                    ReverseAddedForm form = new ReverseAddedForm(contact);
                    form.FormClosed += delegate(object f, FormClosedEventArgs fce)
                    {
                        form = f as ReverseAddedForm;
                        if (DialogResult.OK == form.DialogResult)
                        {
                            if (form.AddToContactList)
                            {
                                contact.NSMessageHandler.ContactService.AddNewContact(contact.Mail);
                                System.Threading.Thread.Sleep(200);

                                if (form.Blocked)
                                {
                                    contact.Blocked = true;
                                }
                            }
                            else if (form.Blocked)
                            {
                                contact.Blocked = true;
                            }
                            else
                            {
                                contact.OnAllowedList = true;
                            }

                            System.Threading.Thread.Sleep(200);
                            contact.OnPendingList = false;
                        }
                        return;
                    };
                    form.Show(this);
                }
                else
                {
                    MessageBox.Show(contact.Mail + " accepted your invitation and added you their contact list.");
                }
            }
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
            if (InvokeRequired)
            {
                this.Invoke(new SetStatusDelegate(SetStatusSynchronized), new object[] { status });
            }
            else
            {
                SetStatusSynchronized(status);
            }
        }

        /// <summary>
        /// Sign into the messenger network. Disconnect first if a connection has already been established.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loginButton_Click(object sender, System.EventArgs e)
        {
            switch (Convert.ToInt32(loginButton.Tag))
            {
                case 0: // not connected -> connect
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

                        displayImageBox.Image = global::MSNPSharpClient.Properties.Resources.loading;

                        loginButton.Tag = 1;
                        loginButton.Text = "Cancel";

                        // note that Messenger.Connect() will run in a seperate thread and return immediately.
                        // it will fire events that informs you about the status of the connection attempt. 
                        // these events are registered in the constructor.
                    }
                    break;

                case 1: // connecting -> cancel
                    {
                        if (messenger.Connected)
                            messenger.Disconnect();

                        if (toolStripSortByStatus.Checked)
                            SortByStatus();
                        else
                            SortByGroup();

                        displayImageBox.Image = null;
                        loginButton.Tag = 0;
                        loginButton.Text = "> Sign in";
                        pnlNameAndPM.Visible = false;
                    }
                    break;

                case 2: // connected -> disconnect
                    {
                        if (messenger.Connected)
                            messenger.Disconnect();

                        if (toolStripSortByStatus.Checked)
                            SortByStatus();
                        else
                            SortByGroup();

                        displayImageBox.Image = null;
                        loginButton.Tag = 0;
                        loginButton.Text = "> Sign in";
                        pnlNameAndPM.Visible = true;
                    }
                    break;
            }
        }

        private void login_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar == '\r') || (e.KeyChar == '\r'))
            {
                loginButton.PerformClick();
            }
        }


        private void comboStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(comboStatus_SelectedIndexChanged), sender, e);
                return;
            }

            PresenceStatus newstatus = (PresenceStatus)Enum.Parse(typeof(PresenceStatus), comboStatus.Text);

            if (messenger.Connected)
            {
                if (newstatus == PresenceStatus.Offline)
                {
                    PresenceStatus old = messenger.Owner.Status;
                    //close all ConversationForms
                    if (ConversationForms.Count == 0 ||
                        MessageBox.Show("You are signing out from example client. All windows will be closed.", "Sign out",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        if (ConversationForms.Count != 0)
                        {
                            for (int i = 0; i < ConversationForms.Count; i++)
                            {
                                ConversationForms[i].Close();
                            }

                        }
                        loginButton.Tag = 2;
                        loginButton.PerformClick();
                        pnlNameAndPM.Visible = false;

                    }
                    comboStatus.SelectedIndex = comboStatus.FindString(old.ToString());
                }
                else
                {
                    messenger.Owner.Status = newstatus;
                }
            }
            else if (newstatus == PresenceStatus.Offline)
            {
                MessageBox.Show("You can not login as Offline :)");
                comboStatus.SelectedIndex = 0;
            }
        }

        private void NameserverProcessor_ConnectionEstablished(object sender, EventArgs e)
        {
            SetStatus("Connected to server");
        }

        private void Nameserver_SignedIn(object sender, EventArgs e)
        {
            SetStatus("Signed into the messenger network as " + messenger.Owner.Name);

            if (InvokeRequired)
            {
                Invoke(new EventHandler(Nameserver_SignedIn), sender, e);
                return;
            }

            // set our presence status
            loginButton.Tag = 2;
            loginButton.Text = "Sign off";
            pnlNameAndPM.Visible = true;

            messenger.Owner.Status = (PresenceStatus)Enum.Parse(typeof(PresenceStatus), comboStatus.Text);

            Invoke(new EventHandler<EventArgs>(UpdateContactlist), sender, e);
        }

        private void Nameserver_SignedOff(object sender, SignedOffEventArgs e)
        {
            SetStatus("Signed off from the messenger network");

            if (InvokeRequired)
            {
                Invoke(new EventHandler<SignedOffEventArgs>(Nameserver_SignedOff), sender, e);
                return;
            }

            tmrKeepOnLine.Enabled = false;

            displayImageBox.Image = null;
            loginButton.Tag = 0;
            loginButton.Text = "> Sign in";
            pnlNameAndPM.Visible = false;
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
        /// Updates the treeView.
        /// </summary>
        private void UpdateContactlist(object sender, EventArgs e)
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
        /// A delegate passed to Invoke in order to create the conversation form in the thread of the main form.
        /// </summary>
        private delegate ConversationForm CreateConversationDelegate(Conversation conversation, Contact remote);

        private ConversationForm CreateConversationForm(Conversation conversation, Contact remote)
        {
            foreach (ConversationForm cform in ConversationForms)
            {
                if (cform.CanAttach(conversation))
                {
                    cform.AttachConversation(conversation);
                    return cform;
                }
            }

            // create a new conversation. However do not show the window untill a message is received.
            // for example, a conversation will be created when the remote client sends wants to send
            // you a file. You don't want to show the conversation form in that case.
            ConversationForm conversationForm = new ConversationForm(conversation, this, remote);
            // do this to create the window handle. Otherwise we are not able to call Invoke() on the
            // conversation form later.
            conversationForm.Handle.ToInt32();
            ConversationForms.Add(conversationForm);
            return conversationForm;
        }

        private void messenger_ConversationCreated(object sender, ConversationCreatedEventArgs e)
        {
            // check if the request is initiated remote or by this object
            // if it is initiated remote then we have to create a conversation form. Otherwise the 
            // form is already created and we don't need to create another one.
            if (e.Initiator == null)
            {
                // use the invoke method to create the form in the main thread, ONLY create the form after a contact joined our conversation.
                e.Conversation.ContactJoined += new EventHandler<ContactEventArgs>(Conversation_ContactJoined);
            }
        }

        void Conversation_ContactJoined(object sender, ContactEventArgs e)
        {
            //The request is initiated by remote user, so we needn't invite anyone.
            this.Invoke(new CreateConversationDelegate(CreateConversationForm), new object[] { sender, null });
            Conversation convers = sender as Conversation;
            convers.ContactJoined -= Conversation_ContactJoined; //We don't care any further join event anymore.
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
                    e.TransferSession.AutoCloseStream = true;
                }
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
                    sendIMMenuItem.PerformClick();
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

                        toolStripMenuItem2.Visible = true;
                    }
                    else
                    {
                        sendIMMenuItem.Visible = false;
                        sendOIMMenuItem.Visible = true;

                        toolStripMenuItem2.Visible = false;
                    }

                    viewContactCardMenuItem.Visible = (contact.ClientType == ClientType.PassportMember);

                    deleteMenuItem.Visible = contact.Guid != Guid.Empty;

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

        private void deleteMenuItem_Click(object sender, EventArgs e)
        {
            Contact contact = (Contact)treeViewFavoriteList.SelectedNode.Tag;
            RemoveContactForm form = new RemoveContactForm();
            form.FormClosed += delegate(object f, FormClosedEventArgs fce)
            {
                form = f as RemoveContactForm;
                if (DialogResult.OK == form.DialogResult)
                {
                    if (form.Block)
                    {
                        contact.Blocked = true;
                    }

                    if (form.RemoveFromAddressBook)
                    {
                        messenger.Nameserver.ContactService.RemoveContact(contact);
                    }
                    else
                    {
                        contact.IsMessengerUser = false;
                    }
                }
            };
            form.ShowDialog(this);
        }

        private void sendMessageToolStripMenuItem_Click(object sender, EventArgs e)
        {

            Contact contact = treeViewFavoriteList.SelectedNode.Tag as Contact;
            bool activate = false;
            ConversationForm activeForm = null;
            foreach (ConversationForm conv in ConversationForms)
            {
                if (conv.Conversation.HasContact(contact) && 
                    (conv.Conversation.Type & ConversationType.Chat) == ConversationType.Chat)
                {
                    activeForm = conv;
                    activate = true;
                }

            }

            if (activate)
            {
                if (activeForm.WindowState == FormWindowState.Minimized)
                    activeForm.Show();

                activeForm.Activate();
                return;
            }


            Conversation convers = messenger.CreateConversation();
            ConversationForm form = CreateConversationForm(convers, contact);
            form.Show();
        }

        private void sendOfflineMessageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Contact selectedContact = (Contact)treeViewFavoriteList.SelectedNode.Tag;
            this.propertyGrid.SelectedObject = selectedContact;

            messenger.Nameserver.OIMService.SendOIMMessage(selectedContact.Mail, "MSNP message");
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
                newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : (contact.DynamicChanged != DynamicItemState.None) ? PARENT_NODE_FONT : USER_NODE_FONT;
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
                    newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : (contact.DynamicChanged != DynamicItemState.None) ? PARENT_NODE_FONT : USER_NODE_FONT;
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
                        newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : (contact.DynamicChanged != DynamicItemState.None) ? PARENT_NODE_FONT : USER_NODE_FONT;
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
            if (this.loginButton.Tag.ToString() != "2")
            {
                MessageBox.Show("Please sign in first.");
                return;
            }
            AddContactForm acf = new AddContactForm();
            if (DialogResult.OK == acf.ShowDialog(this) && acf.Account != String.Empty)
            {
                messenger.Nameserver.ContactService.AddNewContact(acf.Account);
            }
        }

        private void importContactsMenuItem_Click(object sender, EventArgs e)
        {
            ImportContacts ic = new ImportContacts();
            if (ic.ShowDialog(this) == DialogResult.OK)
            {
                string invitation = ic.InvitationMessage;
                foreach (String account in ic.Contacts)
                {
                    messenger.Nameserver.ContactService.AddNewContact(account, ClientType.PassportMember, invitation);
                }
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (txtSearch.Text == String.Empty || txtSearch.Text == "Search contacts")
            {
                treeViewFilterList.Nodes.Clear();
                treeViewFavoriteList.Visible = true;
                treeViewFilterList.Visible = false;
            }
            else
            {
                treeViewFilterList.Nodes.Clear();
                treeViewFavoriteList.Visible = false;
                treeViewFilterList.Visible = true;
                TreeNode foundnode = treeViewFilterList.Nodes.Add("0", "Search Results:");

                foreach (Contact contact in messenger.ContactList.All)
                {
                    if (contact.Mail.IndexOf(txtSearch.Text, StringComparison.CurrentCultureIgnoreCase) != -1
                        ||
                        contact.Name.IndexOf(txtSearch.Text, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        TreeNode newnode = foundnode.Nodes.Add(contact.Mail, contact.Name);
                        newnode.NodeFont = contact.Blocked ? USER_NODE_FONT_BANNED : USER_NODE_FONT;
                        newnode.Tag = contact;
                    }
                }
                foundnode.Text = "Search Results: " + foundnode.Nodes.Count;
                foundnode.Expand();
            }
        }

        private void txtSearch_Enter(object sender, EventArgs e)
        {
            if (txtSearch.Text == "Search contacts")
            {
                txtSearch.Text = String.Empty;
            }
        }

        private void txtSearch_Leave(object sender, EventArgs e)
        {
            if (txtSearch.Text == String.Empty)
            {
                txtSearch.Text = "Search contacts";
            }
        }

        private void lblName_Leave(object sender, EventArgs e)
        {
            string dn = lblName.Text;
            string pm = lblPM.Text;

            if (dn != messenger.Nameserver.Owner.Name)
                messenger.Nameserver.Owner.Name = dn;

            if (messenger.Nameserver.Owner.PersonalMessage == null || pm != messenger.Nameserver.Owner.PersonalMessage.Message)
            {
#if MSNP18
                messenger.Nameserver.Owner.PersonalMessage = new PersonalMessage(pm, MediaType.None, null, new Guid(NSMessageHandler.MachineGuid));
#else
                messenger.Nameserver.Owner.PersonalMessage = new PersonalMessage(pm, MediaType.None, null);
#endif
                }
        }

        private void viewContactCardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Contact contact = (Contact)treeViewFavoriteList.SelectedNode.Tag;
            messenger.Nameserver.SpaceService.GetContactCard(contact.Mail);
        }

        private void comboStatus_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!messenger.Connected)
            {
                login_KeyPress(sender, e);
            }
        }

        private void displayImageBox_Click(object sender, EventArgs e)
        {
            if (messenger.Connected)
            {
                if (openImageDialog.ShowDialog() == DialogResult.OK)
                {
                    messenger.Nameserver.StorageService.UpdateProfile(Image.FromFile(openImageDialog.FileName), "MyPhoto");
                }
            }
        }
    }
}
