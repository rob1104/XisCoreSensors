using System;
using System.Windows.Forms;

namespace XisCoreSensors
{
    public partial class FrmPassword : Form
    {
        public string Password { get; private set; }

        public FrmPassword(string prompt, string title)
        {
            InitializeComponent();
            Text = title;
            lblPrompt.Text = prompt;
            btnOK.DialogResult = DialogResult.OK;
            btnCancel.DialogResult = DialogResult.Cancel;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Password = txtPassword.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
