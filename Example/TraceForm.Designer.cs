namespace MSNPSharpClient
{
    partial class TraceForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.rtbTrace = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // rtbTrace
            // 
            this.rtbTrace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbTrace.Location = new System.Drawing.Point(0, 0);
            this.rtbTrace.Name = "rtbTrace";
            this.rtbTrace.Size = new System.Drawing.Size(661, 450);
            this.rtbTrace.TabIndex = 0;
            this.rtbTrace.Text = "";
            // 
            // TraceForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(661, 450);
            this.Controls.Add(this.rtbTrace);
            this.Name = "TraceForm";
            this.Text = "TraceForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtbTrace;
    }
}