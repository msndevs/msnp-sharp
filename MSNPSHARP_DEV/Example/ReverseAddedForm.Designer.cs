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
            this.lblAdded = new System.Windows.Forms.Label();
            this.gbMembership = new System.Windows.Forms.GroupBox();
            this.rbBlock2 = new System.Windows.Forms.RadioButton();
            this.rbDeleteRequest = new System.Windows.Forms.RadioButton();
            this.rbAllow = new System.Windows.Forms.RadioButton();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.gbMembership.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblAdded
            // 
            this.lblAdded.AutoSize = true;
            this.lblAdded.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.lblAdded.Location = new System.Drawing.Point(12, 9);
            this.lblAdded.Name = "lblAdded";
            this.lblAdded.Size = new System.Drawing.Size(189, 20);
            this.lblAdded.TabIndex = 0;
            this.lblAdded.Text = "{0} wants to be friends";
            // 
            // gbMembership
            // 
            this.gbMembership.Controls.Add(this.rbBlock2);
            this.gbMembership.Controls.Add(this.rbDeleteRequest);
            this.gbMembership.Controls.Add(this.rbAllow);
            this.gbMembership.Location = new System.Drawing.Point(16, 44);
            this.gbMembership.Name = "gbMembership";
            this.gbMembership.Size = new System.Drawing.Size(441, 97);
            this.gbMembership.TabIndex = 1;
            this.gbMembership.TabStop = false;
            this.gbMembership.Text = "Choise an action";
            // 
            // rbBlock2
            // 
            this.rbBlock2.AutoSize = true;
            this.rbBlock2.Location = new System.Drawing.Point(15, 65);
            this.rbBlock2.Name = "rbBlock2";
            this.rbBlock2.Size = new System.Drawing.Size(250, 17);
            this.rbBlock2.TabIndex = 2;
            this.rbBlock2.Text = "No, thanks. Block all invitations from this person";
            this.rbBlock2.UseVisualStyleBackColor = true;
            // 
            // rbDeleteRequest
            // 
            this.rbDeleteRequest.AutoSize = true;
            this.rbDeleteRequest.Location = new System.Drawing.Point(15, 42);
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
            this.rbAllow.Location = new System.Drawing.Point(15, 19);
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
            this.btnOK.Location = new System.Drawing.Point(266, 147);
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
            this.btnCancel.Location = new System.Drawing.Point(382, 147);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 39);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Decide later";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btn_Click);
            // 
            // ReverseAddedForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(470, 202);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.gbMembership);
            this.Controls.Add(this.lblAdded);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ReverseAddedForm";
            this.Text = "Pending Contact {0}";
            this.gbMembership.ResumeLayout(false);
            this.gbMembership.PerformLayout();
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
    }
}