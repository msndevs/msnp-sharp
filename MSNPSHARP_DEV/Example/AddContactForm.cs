using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MSNPSharpClient
{
    public partial class AddContactForm : Form
    {
        public AddContactForm()
        {
            InitializeComponent();
        }

        private string account;
        public string Account
        {
            get
            {
                return account;
            }
        }

        private string invitationMessage;
        public string InvitationMessage
        {
            get
            {
                return invitationMessage;
            }
        }

        private void btn_Click(object sender, EventArgs e)
        {
            account = txtAccount.Text;
            invitationMessage = txtInvitation.Text;

            Close();
        }
    }
}