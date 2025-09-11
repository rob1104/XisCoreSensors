using libplctag;
using libplctag.NativeImport;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private bool _hasChanges = false;

        public TagMapper TagMapper => _tagMapper;

        private readonly TagCatalogManager _catalogManager;
        private readonly List<string> _manualTags;

        public FrmTagMapper(List<SensorControl> sensors, TagMapper tagMapper)
        {
            InitializeComponent();
            _sensors = sensors ?? throw new ArgumentNullException(nameof(sensors));
            _tagMapper = tagMapper ?? throw new ArgumentNullException(nameof(tagMapper));

            lstSensors.DrawMode = DrawMode.OwnerDrawFixed;
            //lstPlcTags.DrawMode = DrawMode.OwnerDrawFixed;

            lstSensors.DrawItem += ListBox_DrawItem;
            lstPlcTags.DrawItem += ListBox_DrawItem;

            _catalogManager = new TagCatalogManager();
            _manualTags = _catalogManager.Load();
        }

        private void ListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();
            bool isMapped = false;
            string displayText = "";
            var item = (sender as ListBox).Items[e.Index];

            if (item is SensorListItem sensorItem)
            {
                isMapped = sensorItem.IsMapped;
                displayText = sensorItem.Sensor.SensorId;
            }
            else if (item is PlcTagListItem tagItem)
            {
                isMapped = tagItem.IsMapped;
                displayText = tagItem.TagName;
            }

            string icon = isMapped ? "✓" : "○";
            Brush textBrush = isMapped ? Brushes.Gray : Brushes.Black;
            Font textFont = isMapped ? new Font(e.Font, FontStyle.Italic) : e.Font;
            e.Graphics.DrawString($"{icon} {displayText}", textFont, textBrush, e.Bounds);
            e.DrawFocusRectangle();
        }

        private void UpdateListsVisualState()
        {
            // Actualiza el estado de cada SensorListItem
            foreach (SensorListItem item in lstSensors.Items)
            {
                item.IsMapped = _tagMapper.IsSensorMapped(item.Sensor.SensorId);
            }

            // Actualiza el estado de cada PlcTagListItem
            foreach (PlcTagListItem item in lstPlcTags.Items)
            {
                item.IsMapped = _tagMapper.IsTagMapped(item.TagName);
            }

            // Fuerza a ambos ListBox a redibujarse completamente
            lstSensors.Refresh();
            lstPlcTags.Refresh();
        }


        private void FrmTagMapper_Load(object sender, EventArgs e)
        {
            LoadSensors();
            RefreshTagList();
            RefreshMappings();
            UpdateStatistics();
            UpdateButtonStates();
            _tagMapper.MappingChanged += TagMapper_MappingChanged;
            
        }

        private void RefreshTagList()
        {
            lblPlcStatus.Text = "Status: Connecting...";            
            btnAddTag.Enabled = false;
            lstPlcTags.DataSource = null;

            var tagItems = _manualTags.Select(name => new PlcTagListItem(name)).ToList();
            lstPlcTags.DataSource = tagItems;
            lstPlcTags.DisplayMember = "TagName";
            // --- FIN DE LÓGICA MODIFICADA ---

         
            btnAddTag.Enabled = true;
            lblPlcStatus.Text = $"Found {_manualTags.Count} tags";
            lblPlcStatus.ForeColor = _manualTags.Count > 0 ? Color.Green : Color.Red;
            UpdateListsVisualState();
        }

        private void TagMapper_MappingChanged(object sender, MappingChangedEventArgs e)
        {
            _hasChanges = true;
            RefreshMappings();
            UpdateStatistics();
            UpdateSensorColors();
            RefreshTagList();
            UpdateListsVisualState();
        }

        private void LoadSensors()
        {
            lstSensors.Items.Clear();
            var sensorItems = _sensors.OrderBy(s => s.SensorId)
                .Select(s => new SensorListItem(s))
                .ToArray();
            lstSensors.Items.AddRange(sensorItems);
            UpdateListsVisualState(); // <-- Llamada clave
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
            var selectedSensorItem = lstSensors.SelectedItem as SensorListItem;
            var selectedTagItem = lstPlcTags.SelectedItem as PlcTagListItem;

            btnMapSelected.Enabled = selectedSensorItem != null && selectedTagItem != null;

            btnUnmapSelected.Enabled = selectedSensorItem != null && _tagMapper.IsSensorMapped(selectedSensorItem.Sensor.SensorId);
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
            RefreshTagList();

            lblStatus.Text = $"Refreshed - Found {_manualTags.Count} tags";
        }

        private void btnMapSelected_Click(object sender, EventArgs e)
        {
            var selectedSensors = lstSensors.SelectedItems.Cast<SensorListItem>().ToList();
            var selectedTag = lstPlcTags.SelectedItem as PlcTagListItem;

            if (selectedSensors.Count == 0 || selectedTag == null)
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
                _tagMapper.MapSensorToTag(sensor.Sensor.SensorId, selectedTag.TagName);
                sensor.Sensor.PlcTag = selectedTag.TagName; // Actualizar el sensor

                lblStatus.Text = $"Mapped sensor {sensor.Sensor.SensorId} to tag {selectedTag.TagName}";
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Mapping Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnUnmapSelected_Click(object sender, EventArgs e)
        {
            // Obtiene el sensor seleccionado (asumiendo que solo se puede desmapear uno a la vez).
            var selectedSensorItem = lstSensors.SelectedItem as SensorListItem;

            if (selectedSensorItem == null || !selectedSensorItem.IsMapped)
            {
                MessageBox.Show("Please select a mapped sensor to unmap.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to unmap sensor '{selectedSensorItem.Sensor.SensorId}'?",
                "Confirm Unmap",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // 1. Llama a tu lógica para desmapear el sensor.
                // Esto debería disparar el evento 'MappingChanged' si está configurado.
                _tagMapper.UnmapSensor(selectedSensorItem.Sensor.SensorId);

                // 2. --- LA CORRECCIÓN CLAVE ---
                // Llamamos explícitamente a nuestro método de actualización visual.
                // Esto garantiza que AMBAS listas se redibujen con su nuevo estado,
                // incluso si el evento no se dispara por alguna razón.
                UpdateListsVisualState();

                lblStatus.Text = $"Unmapped sensor {selectedSensorItem.Sensor.SensorId}";
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _tagMapper.MappingChanged -= TagMapper_MappingChanged;
            base.OnFormClosing(e);
        }

        private void btnAddTag_Click(object sender, EventArgs e)
        {
            while(true)
            {
                using (var inputBox = new FrmInputBox("Add New PLC Tag", "Tag Name:"))
                {
                    if (inputBox.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                    var newTag = inputBox.InputValue.Trim();
                    if (string.IsNullOrEmpty(newTag))
                    {
                        MessageBox.Show("Tag name cannot be empty.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }
                    if (Regex.IsMatch(newTag, @"\s"))
                    {
                        MessageBox.Show("Tag name cannot contain spaces.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    if (_manualTags.Contains(newTag, StringComparer.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("This tag already exists in the catalog.", "Duplicate Tag", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }                   
                    _manualTags.Add(newTag);
                    _manualTags.Sort();                  
                    _catalogManager.Save(_manualTags);
                    RefreshTagList();                   
                    lstPlcTags.SelectedItem = newTag;
                    break;
                }
            }         
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string selectedTag = lstPlcTags.SelectedItem.ToString();

            var result = MessageBox.Show(
                $"Are you sure you want to permanently delete the manual tag '{selectedTag}'?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                // Elimina el tag de nuestra lista maestra de tags manuales.
                _manualTags.Remove(selectedTag);

                // Guarda la lista actualizada en el archivo JSON.
                _catalogManager.Save(_manualTags);

                // Refresca la lista de la UI para que el cambio sea visible.
                RefreshTagList();

                lblStatus.Text = $"Tag '{selectedTag}' was deleted.";
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            // Por defecto, deshabilitamos la opción.
            deleteToolStripMenuItem.Enabled = false;

            // Si no hay ningún tag seleccionado en la lista, no hacemos nada.
            if (lstPlcTags.SelectedItem == null) return;

            string selectedTag = lstPlcTags.SelectedItem.ToString();

            // Verificamos tres condiciones:
            // 1. El tag seleccionado DEBE ser un tag manual (no uno del PLC).
            bool isManual = _manualTags.Contains(selectedTag, StringComparer.OrdinalIgnoreCase);

            // 2. El tag NO DEBE estar mapeado a ningún sensor.
            bool isMapped = _tagMapper.IsTagMapped(selectedTag);

            // Solo si es un tag manual Y no está mapeado, habilitamos la opción de eliminar.
            if (isManual && !isMapped)
            {
                deleteToolStripMenuItem.Enabled = true;
            }
        }

        private void btnCancel_Click_1(object sender, EventArgs e)
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

        private void btnAddRange_Click(object sender, EventArgs e)
        {
            using (var rangeForm = new FrmAddRange())
            {
                if (rangeForm.ShowDialog() == DialogResult.OK)
                {
                    string prefix = rangeForm.Prefix;
                    int endNumber = rangeForm.EndNumber;
                    int tagsAddedCount = 0;

                    // Determina cuántos dígitos se necesitan para el padding (ej: 10 -> 2 dígitos, 9 -> 1 dígito)
                    int padding = endNumber.ToString().Length;

                    List<string> newTags = new List<string>();

                    for (int i = 1; i <= endNumber; i++)
                    {
                        // Genera el nombre del tag con ceros a la izquierda (ej: LS01, LS02... LS10)
                        string tagName = $"{prefix}{i.ToString().PadLeft(padding, '0')}";

                        // Valida que el tag no exista ya en el catálogo
                        if (!_manualTags.Contains(tagName, StringComparer.OrdinalIgnoreCase))
                        {
                            newTags.Add(tagName);
                            tagsAddedCount++;
                        }
                    }

                    if (newTags.Any())
                    {
                        _manualTags.AddRange(newTags);
                        _manualTags.Sort();
                        _catalogManager.Save(_manualTags);

                        RefreshTagList();

                        MessageBox.Show($"{tagsAddedCount} new tags were added to the catalog.", "Range Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("No new tags were added. They may already exist in the catalog.", "No Changes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Clase auxiliar para mostrar sensores en la lista con información adicional
    /// </summary>
    public class SensorListItem
    {
        public SensorControl Sensor { get; }
        public bool IsMapped { get; set; }

        public SensorListItem(SensorControl sensor)
        {
            Sensor = sensor;
        }

        public override string ToString() => Sensor.SensorId;
    }

    public class PlcTagListItem
    {
        public string TagName { get; }
        public bool IsMapped { get; set; }

        public PlcTagListItem(string tagName)
        {
            TagName = tagName;
        }

        public override string ToString() => TagName;
    }
}
