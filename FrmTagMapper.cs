using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XisCoreSensors.Controls;
using XisCoreSensors.Mapping;
using static XisCoreSensors.Mapping.SensorTagMapping;

namespace XisCoreSensors
{
    public partial class FrmTagMapper : Form
    {
        private List<SensorControl> _sensors;
        private TagMapper _tagMapper;
        private List<string> _plcTags;
        private bool _hasChanges = false;

        public TagMapper TagMapper => _tagMapper;

        public FrmTagMapper(List<SensorControl> sensors, TagMapper tagMapper)
        {
            InitializeComponent();
            _sensors = sensors ?? throw new ArgumentNullException(nameof(sensors));
            _tagMapper = tagMapper ?? throw new ArgumentNullException(nameof(tagMapper));
            _plcTags = new List<string>();
        }

        private void FrmTagMapper_Load(object sender, EventArgs e)
        {
            LoadSensors();
            LoadMockPlcTags(); // Por ahora usaremos tags simulados
            RefreshMappings();
            UpdateStatistics();
            UpdateButtonStates();

            _tagMapper.MappingChanged += TagMapper_MappingChanged;
        }

        private void TagMapper_MappingChanged(object sender, MappingChangedEventArgs e)
        {
            _hasChanges = true;
            RefreshMappings();
            UpdateStatistics();
            UpdateSensorColors();
        }

        private void LoadSensors()
        {
            lstSensors.Items.Clear();

            foreach (var sensor in _sensors.OrderBy(s => s.SensorId))
            {
                lstSensors.Items.Add(new SensorListItem(sensor));
            }
        }

        private void LoadMockPlcTags()
        {
            // Por ahora simulamos tags del PLC
            // Más adelante esto se conectará al PLC real
            _plcTags.Clear();
            _plcTags.AddRange(new[]
            {
                "DB1.DBX0.0",
                "DB1.DBX0.1",
                "DB1.DBX0.2",
                "DB1.DBX0.3",
                "DB1.DBX1.0",
                "DB1.DBX1.1",
                "DB1.DBX1.2",
                "DB1.DBX1.3",
                "DB2.DBX0.0",
                "DB2.DBX0.1",
                "DB2.DBX0.2",
                "DB2.DBX0.3",
                "M0.0",
                "M0.1",
                "M0.2",
                "M0.3",
                "I0.0",
                "I0.1",
                "I0.2",
                "I0.3"
            });

            RefreshPlcTagsList();
        }

        private void RefreshPlcTagsList()
        {
            lstPlcTags.Items.Clear();

            foreach (var tag in _plcTags.OrderBy(t => t))
            {
                lstPlcTags.Items.Add(tag);
            }

            lblPlcStatus.Text = $"Found {_plcTags.Count} tags";
            lblPlcStatus.ForeColor = _plcTags.Count > 0 ? Color.Green : Color.Red;
        }

        private void RefreshMappings()
        {
            var mappings = _tagMapper.GetAllMappings().ToList();
            dgvMappings.DataSource = mappings;

            if (dgvMappings.Columns["CreatedAt"] != null)
            {
                dgvMappings.Columns["CreatedAt"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            }
        }

        private void UpdateStatistics()
        {
            var stats = _tagMapper.GetStatistics(_sensors.Count);
            lblSensorStats.Text = $"Mapped: {stats.MappedSensors} of {stats.TotalSensors}";
            lblSensorStats.ForeColor = stats.MappingPercentage > 50 ? Color.Green : Color.Orange;

            lblStatus.Text = $"Mapping Progress: {stats.MappingPercentage:F1}% ({stats.MappedSensors}/{stats.TotalSensors})";
        }

        private void UpdateSensorColors()
        {
            // Refrescar la lista de sensores para mostrar colores actualizados
            for (int i = 0; i < lstSensors.Items.Count; i++)
            {
                if (lstSensors.Items[i] is SensorListItem item)
                {
                    var isMapped = _tagMapper.IsSensorMapped(item.Sensor.SensorId);
                    item.Sensor.Status = isMapped ? SensorControl.SensorStatus.Ok : SensorControl.SensorStatus.Unmapped;
                }
            }

            lstSensors.Invalidate(); // Forzar redibujado
        }

        private void UpdateButtonStates()
        {
            var selectedSensors = lstSensors.SelectedItems.Cast<SensorListItem>().ToList();
            var selectedTag = lstPlcTags.SelectedItem as string;

            btnMapSelected.Enabled = selectedSensors.Count > 0 && !string.IsNullOrEmpty(selectedTag);
            btnUnmapSelected.Enabled = selectedSensors.Any(s => _tagMapper.IsSensorMapped(s.Sensor.SensorId));
        }

        private void lstSensors_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void lstPlcTags_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void btnRefreshTags_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Refreshing PLC tags...";
            Application.DoEvents();

            // Aquí iría la lógica real de lectura del PLC
            // Por ahora solo recargamos los tags mock
            LoadMockPlcTags();

            lblStatus.Text = $"Refreshed - Found {_plcTags.Count} tags";
        }

        private void btnMapSelected_Click(object sender, EventArgs e)
        {
            var selectedSensors = lstSensors.SelectedItems.Cast<SensorListItem>().ToList();
            var selectedTag = lstPlcTags.SelectedItem as string;

            if (selectedSensors.Count == 0 || string.IsNullOrEmpty(selectedTag))
            {
                MessageBox.Show("Please select sensor(s) and a PLC tag.", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (selectedSensors.Count > 1)
            {
                MessageBox.Show("You can only map one sensor at a time to a PLC tag.", "Multiple Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sensor = selectedSensors.First();

            try
            {
                _tagMapper.MapSensorToTag(sensor.Sensor.SensorId, selectedTag);
                sensor.Sensor.PlcTag = selectedTag; // Actualizar el sensor

                lblStatus.Text = $"Mapped sensor {sensor.Sensor.SensorId} to tag {selectedTag}";
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Mapping Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnUnmapSelected_Click(object sender, EventArgs e)
        {
            var selectedSensors = lstSensors.SelectedItems.Cast<SensorListItem>()
                .Where(s => _tagMapper.IsSensorMapped(s.Sensor.SensorId)).ToList();

            if (selectedSensors.Count == 0)
            {
                MessageBox.Show("Please select mapped sensor(s) to unmap.", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to unmap {selectedSensors.Count} sensor(s)?",
                "Confirm Unmap", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                foreach (var sensorItem in selectedSensors)
                {
                    _tagMapper.UnmapSensor(sensorItem.Sensor.SensorId);
                    sensorItem.Sensor.PlcTag = null; // Limpiar el tag del sensor
                }

                lblStatus.Text = $"Unmapped {selectedSensors.Count} sensor(s)";
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (_hasChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Are you sure you want to cancel?",
                    "Unsaved Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                    return;
            }

            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _tagMapper.MappingChanged -= TagMapper_MappingChanged;
            base.OnFormClosing(e);
        }
    }

    /// <summary>
    /// Clase auxiliar para mostrar sensores en la lista con información adicional
    /// </summary>
    public class SensorListItem
    {
        public SensorControl Sensor { get; }

        public SensorListItem(SensorControl sensor)
        {
            Sensor = sensor ?? throw new ArgumentNullException(nameof(sensor));
        }

        public override string ToString()
        {
            var status = Sensor.IsMapped ? "✓" : "○";
            return $"{status} {Sensor.SensorId}";
        }
    }
}
