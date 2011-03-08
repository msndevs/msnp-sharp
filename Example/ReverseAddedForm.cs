using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MSNPSharp;

namespace MSNPSharpClient
{
    public partial class ReverseAddedForm : Form
    {
        public ReverseAddedForm(Contact contact)
        {
            InitializeComponent();

            Text = String.Format(Text, contact.Account);
            lblAdded.Text = String.Format(lblAdded.Text, contact.Name + " (" + contact.Account + ")");
        }

        public bool AddAsFriend
        {
            get
            {
                return rbAllow.Checked;
            }
        }

        public bool Delete
        {
            get
            {
                return rbDeleteRequest.Checked;
            }
        }

        public bool Block
        {
            get
            {
                return rbBlock2.Checked;
            }
        }

        private void btn_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}