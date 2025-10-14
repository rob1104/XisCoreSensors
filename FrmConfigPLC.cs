using libplctag;
using System;
using System.Windows.Forms;

namespace XisCoreSensors
{
    public partial class FrmConfigPLC : Form
    {
        public FrmConfigPLC()
        {
            InitializeComponent();
        }

        private void FrmConfigPLC_Load(object sender, EventArgs e)
        {
            cmbPlcyType.DataSource = Enum.GetValues(typeof(PlcType));
            cmbProtocol.DataSource = Enum.GetValues(typeof(Protocol));

            CargaConfig();
        }

        private void CargaConfig()
        {
            txtIp.Text = Properties.Settings.Default.PLC_IP;
            txtPath.Text = Properties.Settings.Default.PLC_Path;
            cmbPlcyType.SelectedItem = (PlcType)Properties.Settings.Default.PLC_Type;
            cmbProtocol.SelectedItem = (Protocol)Properties.Settings.Default.PLC_Protocol;
            txtTimeout.Text = Properties.Settings.Default.PLC_Timeout.ToString();
            txtPulseTime.Text = Properties.Settings.Default.Pulse_Time.ToString();
            txtFullView.Value = Properties.Settings.Default.FullViewDuration;
            txtZoomView.Value = Properties.Settings.Default.ZoomInDuration;
            txtSEQ.Text = Properties.Settings.Default.SequenceTagName;
            txtIMG.Text = Properties.Settings.Default.ImageTagName;
            txtCHRONO.Text = Properties.Settings.Default.ChronoTgName;
            txtALARM.Text = Properties.Settings.Default.AlarmTagName;
        }

        private void GuardaConfig()
        {
            var selectedPlcType = (PlcType)cmbPlcyType.SelectedItem;
            var selectedProtocol = (Protocol)cmbProtocol.SelectedItem;

            Properties.Settings.Default.PLC_IP = txtIp.Text;
            Properties.Settings.Default.PLC_Path = txtPath.Text;
            Properties.Settings.Default.PLC_Type = (int)selectedPlcType;
            Properties.Settings.Default.PLC_Protocol = (int)selectedProtocol;
            Properties.Settings.Default.PLC_Timeout = (int)txtTimeout.Value;
            Properties.Settings.Default.Pulse_Time = (int)txtPulseTime.Value;
            Properties.Settings.Default.FullViewDuration = (int)txtFullView.Value;
            Properties.Settings.Default.ZoomInDuration = (int)txtZoomView.Value;
            Properties.Settings.Default.SequenceTagName = txtSEQ.Text;
            Properties.Settings.Default.ImageTagName = txtIMG.Text;
            Properties.Settings.Default.ChronoTgName = txtCHRONO.Text;
            Properties.Settings.Default.AlarmTagName = txtALARM.Text;
            Properties.Settings.Default.Save();
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            GuardaConfig(); 
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
