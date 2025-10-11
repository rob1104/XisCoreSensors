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
    public partial class FrmAlertEditor : Form
    {
        private readonly AlertManager _alertManager;
        public FrmAlertEditor(AlertManager alertManager)
        {
            InitializeComponent();
            _alertManager = alertManager;
        }

        private void PopulateGrid()
        {
            dgvAlerts.DataSource = null;
            dgvAlerts.DataSource = _alertManager.GetAlerts();
            dgvAlerts.Columns["SequenceNumber"].HeaderText = "Value";
            dgvAlerts.Columns["Message"].HeaderText = "Message";
            dgvAlerts.Columns["Message"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void FrmAlertEditor_Load(object sender, EventArgs e)
        {
            PopulateGrid();
        }

        private void dgvAlerts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvAlerts.CurrentRow != null && dgvAlerts.CurrentRow.DataBoundItem is AlertMessage selected)
            {
                numSequence.Value = selected.SequenceNumber;
                txtMessage.Text = selected.Message;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var alert = new AlertMessage
            {
                SequenceNumber = (int)numSequence.Value,
                Message = txtMessage.Text.Trim()
            };

            if (string.IsNullOrEmpty(alert.Message))
            {
                MessageBox.Show("Alert message can not be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _alertManager.AddOrUpdateAlert(alert);
            PopulateGrid();
            MessageBox.Show("Alert message saved correctly.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dgvAlerts.CurrentRow == null) return;

            var sequenceNumber = (int)numSequence.Value;
            var result = MessageBox.Show($"Ayer you sure to delete message with value number: {sequenceNumber}?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _alertManager.DeleteAlert(sequenceNumber);
                PopulateGrid();
            }
        }
    }
}
