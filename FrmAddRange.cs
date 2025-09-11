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
    public partial class FrmAddRange : Form
    {
        public string Prefix { get; private set; }
        public int EndNumber { get; private set; }
        public FrmAddRange()
        {
            InitializeComponent();
            AcceptButton = btnOK;
            CancelButton = btnCancel;
            numEndNumber.Minimum = 1;
            numEndNumber.Maximum = 99;
        }

        private void FrmAddRange_Load(object sender, EventArgs e)
        {

        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(txtPrefix.Text) || System.Text.RegularExpressions.Regex.IsMatch(txtPrefix.Text, @"\s"))
            {
                MessageBox.Show("Prefix cannot be empty or contain spaces.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!char.IsLetter(txtPrefix.Text[txtPrefix.Text.Length - 1]))
            {
                MessageBox.Show("The prefix must end with a letter.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            Prefix = txtPrefix.Text;
            EndNumber = (int)numEndNumber.Value;
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
