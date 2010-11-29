using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace MSNPSharpClient
{
    using MSNPSharp;
    using MSNPSharp.Apps;
    using MSNPSharp.Core;
    using MSNPSharp.P2P;


    public partial class FileTransferForm : Form
    {
        private FileTransfer fileTransfer;
        private P2PSession p2pSession;
        private bool transferFinished;

        public FileTransferForm(P2PSession p2pSess)
        {
            this.p2pSession = p2pSess;
            this.fileTransfer = p2pSess.Application as FileTransfer;
            InitializeComponent();
        }

        private void FileTransferForm_Load(object sender, EventArgs e)
        {
            string appPath = Path.GetFullPath(".");

            Text = "File Transfer: " + p2pSession.Remote.Mail;
            txtFilePath.Text = Path.Combine(appPath, fileTransfer.Context.Filename);
            lblSize.Text = fileTransfer.Context.FileSize + " bytes";

            if (fileTransfer.Context.Preview.Length > 0)
            {
                pictureBox1.Image = Image.FromStream(new MemoryStream(fileTransfer.Context.Preview));
                pictureBox1.Visible = true;
            }

            fileTransfer.TransferStarted += (TransferSession_TransferStarted);
            fileTransfer.Progressed += (TransferSession_TransferProgressed);
            fileTransfer.TransferAborted += (TransferSession_TransferProgressed);
            fileTransfer.TransferFinished += (TransferSession_TransferFinished);

            progressBar.Maximum = (int)fileTransfer.Context.FileSize;
        }

        void TransferSession_TransferStarted(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<EventArgs>(TransferSession_TransferStarted), sender, e);
                return;
            }

            progressBar.Visible = true;
            lblSize.Text = "Transfer started";
        }

        void TransferSession_TransferProgressed(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<EventArgs>(TransferSession_TransferProgressed), sender, e);
                return;
            }
            progressBar.Visible = true;
            progressBar.Value = (int)fileTransfer.Transferred;
            lblSize.Text = "Transferred: " + fileTransfer.Transferred + " / " + fileTransfer.Context.FileSize;
        }

        void TransferSession_TransferFinished(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<EventArgs>(TransferSession_TransferFinished), sender, e);
                return;
            }

            transferFinished = true;

            btnOK.Text = "Open File";
            btnOK.Tag = "OPENFILE";
            btnCancel.Visible = true;

            lblSize.Text = "Transfer finished";
            progressBar.Visible = false;
            progressBar.Value = 0;
        }

        void TransferSession_TransferAborted(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<EventArgs>(TransferSession_TransferAborted), sender, e);
                return;
            }

            btnOK.Text = "Close";
            btnOK.Tag = "CLOSE";
            lblSize.Text = "Transfer aborted";

            progressBar.Visible = false;
            progressBar.Value = 0;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = saveFileDialog.FileName;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (transferFinished)
            {
                Close();
            }
            else
            {
                fileTransfer.Decline();

                btnCancel.Visible = false;
                Close();
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            switch (btnOK.Tag.ToString())
            {
                case "OK":

                    fileTransfer.DataStream = new FileStream(txtFilePath.Text, FileMode.Create, FileAccess.Write);
                    fileTransfer.Accept(true);

                    btnCancel.Visible = false;

                    lblSize.Text = "Waiting to start...";

                    btnOK.Text = "Abort Transfer";
                    btnOK.Tag = "ABORT";
                    break;

                case "ABORT":

                    fileTransfer.Abort();

                    btnOK.Text = "Close";
                    btnOK.Tag = "CLOSE";
                    break;

                case "OPENFILE":
                    Process.Start(txtFilePath.Text);
                    Close();
                    break;

                case "CLOSE":
                    Close();
                    break;
            }
        }

        private void FileTransferForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            fileTransfer.TransferStarted -= (TransferSession_TransferStarted);
            fileTransfer.Progressed -= (TransferSession_TransferProgressed);
            fileTransfer.TransferAborted -= (TransferSession_TransferProgressed);
            fileTransfer.TransferFinished -= (TransferSession_TransferFinished);
        }

    }
};