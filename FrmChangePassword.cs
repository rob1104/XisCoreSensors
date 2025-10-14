using System;
using System.Windows.Forms;
using XisCoreSensors.Properties;

namespace XisCoreSensors
{
    public partial class FrmChangePassword : Form
    {
        public FrmChangePassword()
        {
            InitializeComponent();
            AcceptButton = btnOK;
            CancelButton = btnCancel;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if(!SecurityHelper.VerifyPassword(txtOldPassword.Text, Settings.Default.EditModePasswordHash))
            {
                MessageBox.Show("Current password is incorrect.", "Auth error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(txtNewPassword.Text))
            {
                MessageBox.Show("New password can not be empty.", "Auth Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (txtNewPassword.Text != txtConfirmPassword.Text)
            {
                MessageBox.Show("Passwords does not match.", "Auth Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var newPasswordHash = SecurityHelper.HashPassword(txtNewPassword.Text);
            Settings.Default.EditModePasswordHash = newPasswordHash;
            Settings.Default.Save();

            MessageBox.Show("Password updated succesfully.", "Auth", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
