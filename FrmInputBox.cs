using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XisCoreSensors
{
    public partial class FrmInputBox : Form
    {
        public string InputValue {  get; set; }

        public FrmInputBox(string title, string prompt)
        {
            InitializeComponent();
            Text = title;
            lblPrompt.Text = prompt;
            AcceptButton = btnOK;
            CancelButton = btnCancel;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            InputValue = txtInput.Text;
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
