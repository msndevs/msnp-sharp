namespace MSNPSharpClient
{
    partial class ReverseAddedForm
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
            this.lblAdded = new System.Windows.Forms.Label();
            this.gbMembership = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.rbBlock2 = new System.Windows.Forms.RadioButton();
            this.rbDeleteRequest = new System.Windows.Forms.RadioButton();
            this.rbAllow = new System.Windows.Forms.RadioButton();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.gbMembership.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblAdded
            // 
            this.lblAdded.AutoSize = true;
            this.lblAdded.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.lblAdded.Location = new System.Drawing.Point(29, 9);
            this.lblAdded.Name = "lblAdded";
            this.lblAdded.Size = new System.Drawing.Size(29, 17);
            this.lblAdded.TabIndex = 0;
            this.lblAdded.Text = "{0}";
            // 
            // gbMembership
            // 
            this.gbMembership.Controls.Add(this.pictureBox1);
            this.gbMembership.Controls.Add(this.rbBlock2);
            this.gbMembership.Controls.Add(this.rbDeleteRequest);
            this.gbMembership.Controls.Add(this.rbAllow);
            this.gbMembership.Location = new System.Drawing.Point(16, 88);
            this.gbMembership.Name = "gbMembership";
            this.gbMembership.Size = new System.Drawing.Size(441, 102);
            this.gbMembership.TabIndex = 1;
            this.gbMembership.TabStop = false;
            this.gbMembership.Text = "Choise an action";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.White;
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pictureBox1.Location = new System.Drawing.Point(16, 19);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(72, 72);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // rbBlock2
            // 
            this.rbBlock2.AutoSize = true;
            this.rbBlock2.Location = new System.Drawing.Point(105, 65);
            this.rbBlock2.Name = "rbBlock2";
            this.rbBlock2.Size = new System.Drawing.Size(250, 17);
            this.rbBlock2.TabIndex = 2;
            this.rbBlock2.Text = "No, thanks. Block all invitations from this person";
            this.rbBlock2.UseVisualStyleBackColor = true;
            // 
            // rbDeleteRequest
            // 
            this.rbDeleteRequest.AutoSize = true;
            this.rbDeleteRequest.Location = new System.Drawing.Point(105, 42);
            this.rbDeleteRequest.Name = "rbDeleteRequest";
            this.rbDeleteRequest.Size = new System.Drawing.Size(309, 17);
            this.rbDeleteRequest.TabIndex = 1;
            this.rbDeleteRequest.Text = "No, thanks. Delete this request (may send another invitation)";
            this.rbDeleteRequest.UseVisualStyleBackColor = true;
            // 
            // rbAllow
            // 
            this.rbAllow.AutoSize = true;
            this.rbAllow.Checked = true;
            this.rbAllow.Location = new System.Drawing.Point(105, 19);
            this.rbAllow.Name = "rbAllow";
            this.rbAllow.Size = new System.Drawing.Size(119, 17);
            this.rbAllow.TabIndex = 0;
            this.rbAllow.TabStop = true;
            this.rbAllow.Text = "Yes, add as a friend";
            this.rbAllow.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(252, 358);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(104, 39);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btn_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(361, 358);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 39);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Decide later";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btn_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tableLayoutPanel1);
            this.groupBox1.Location = new System.Drawing.Point(16, 205);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(441, 147);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Friends in common";
            this.groupBox1.Visible = false;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 48.24121F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 51.75879F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 98F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 109F));
            this.tableLayoutPanel1.Location = new System.Drawing.Point(16, 19);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 75F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(405, 101);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label1.Location = new System.Drawing.Point(29, 45);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(146, 17);
            this.label1.TabIndex = 6;
            this.label1.Text = "wants to be friends";
            // 
            // ReverseAddedForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(470, 409);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lblAdded);
            this.Controls.Add(this.gbMembership);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ReverseAddedForm";
            this.Text = "Pending Contact {0}";
            this.Load += new System.EventHandler(this.ReverseAddedForm_Load);
            this.gbMembership.ResumeLayout(false);
            this.gbMembership.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblAdded;
        private System.Windows.Forms.GroupBox gbMembership;
        private System.Windows.Forms.RadioButton rbAllow;
        private System.Windows.Forms.RadioButton rbDeleteRequest;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.RadioButton rbBlock2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
    }
}