using System;
using System.Windows.Forms;

namespace XisCoreSensors
{
    public partial class FrmRenameSensor : Form
    {
        public string NewId { get; private set; }

        public FrmRenameSensor(string currentId)
        {
            InitializeComponent();
            txtNewId.Text = currentId;
            txtNewId.SelectAll();
            btnOK.DialogResult = DialogResult.OK;
            btnCancel.DialogResult = DialogResult.Cancel;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            NewId = txtNewId.Text.Trim();
            Close();
        }
    }
}
