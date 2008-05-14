namespace MSNPSharpClient
{
    partial class AddContactForm
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
            this.lblAccount = new System.Windows.Forms.Label();
            this.txtAccount = new System.Windows.Forms.TextBox();
            this.txtInvitation = new System.Windows.Forms.TextBox();
            this.lblInvitation = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.rbLive = new System.Windows.Forms.RadioButton();
            this.rbYahoo = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // lblAccount
            // 
            this.lblAccount.AutoSize = true;
            this.lblAccount.Location = new System.Drawing.Point(4, 21);
            this.lblAccount.Name = "lblAccount";
            this.lblAccount.Size = new System.Drawing.Size(106, 13);
            this.lblAccount.TabIndex = 0;
            this.lblAccount.Text = "Messenger address:*";
            // 
            // txtAccount
            // 
            this.txtAccount.CharacterCasing = System.Windows.Forms.CharacterCasing.Lower;
            this.txtAccount.Location = new System.Drawing.Point(115, 18);
            this.txtAccount.Name = "txtAccount";
            this.txtAccount.Size = new System.Drawing.Size(250, 20);
            this.txtAccount.TabIndex = 1;
            this.txtAccount.TextChanged += new System.EventHandler(this.txtAccount_TextChanged);
            // 
            // txtInvitation
            // 
            this.txtInvitation.Location = new System.Drawing.Point(115, 47);
            this.txtInvitation.Name = "txtInvitation";
            this.txtInvitation.Size = new System.Drawing.Size(250, 20);
            this.txtInvitation.TabIndex = 2;
            // 
            // lblInvitation
            // 
            this.lblInvitation.AutoSize = true;
            this.lblInvitation.Location = new System.Drawing.Point(12, 50);
            this.lblInvitation.Name = "lblInvitation";
            this.lblInvitation.Size = new System.Drawing.Size(98, 13);
            this.lblInvitation.TabIndex = 4;
            this.lblInvitation.Text = "Invitation message:";
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(209, 108);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btn_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(290, 108);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(59, 78);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Network:";
            // 
            // rbLive
            // 
            this.rbLive.AutoSize = true;
            this.rbLive.Checked = true;
            this.rbLive.Location = new System.Drawing.Point(137, 78);
            this.rbLive.Name = "rbLive";
            this.rbLive.Size = new System.Drawing.Size(99, 17);
            this.rbLive.TabIndex = 7;
            this.rbLive.TabStop = true;
            this.rbLive.Text = "Live messenger";
            this.rbLive.UseVisualStyleBackColor = true;
            // 
            // rbYahoo
            // 
            this.rbYahoo.AutoSize = true;
            this.rbYahoo.Location = new System.Drawing.Point(252, 78);
            this.rbYahoo.Name = "rbYahoo";
            this.rbYahoo.Size = new System.Drawing.Size(113, 17);
            this.rbYahoo.TabIndex = 8;
            this.rbYahoo.Text = "Yahoo! messenger";
            this.rbYahoo.UseVisualStyleBackColor = true;
            // 
            // AddContactForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(373, 143);
            this.Controls.Add(this.rbYahoo);
            this.Controls.Add(this.rbLive);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.txtInvitation);
            this.Controls.Add(this.lblInvitation);
            this.Controls.Add(this.txtAccount);
            this.Controls.Add(this.lblAccount);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddContactForm";
            this.Text = "Add New Contact";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblAccount;
        private System.Windows.Forms.TextBox txtAccount;
        private System.Windows.Forms.TextBox txtInvitation;
        private System.Windows.Forms.Label lblInvitation;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton rbLive;
        private System.Windows.Forms.RadioButton rbYahoo;
    }
}