using libplctag;
using libplctag.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
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

        private readonly FrmPartViewer _ownerPartViewer;

        public FrmTagMapper(FrmPartViewer ownerPartViewer, List<SensorControl> sensors, TagMapper tagMapper)
        {
            InitializeComponent();
            _sensors = sensors ?? throw new ArgumentNullException(nameof(sensors));
            _tagMapper = tagMapper ?? throw new ArgumentNullException(nameof(tagMapper));
            _ownerPartViewer = ownerPartViewer ?? throw new ArgumentNullException(nameof(ownerPartViewer));

            lstSensors.DrawMode = DrawMode.OwnerDrawFixed;
            lstPlcTags.DrawMode = DrawMode.OwnerDrawFixed;

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
            Brush textBrush = isMapped ? Brushes.LimeGreen : Brushes.Purple;
            Font textFont = isMapped ? new Font(e.Font, FontStyle.Bold) : e.Font;
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
            _tagMapper.MappingChanged += TagMapper_MappingChanged;
            LoadSensors();
            RefreshTagList();
            RefreshMappings();
            UpdateStatistics();
            UpdateButtonStates();
            lstPlcTags.ClearSelected();

            chkRotate.Checked = Properties.Settings.Default.RotateSensorText;
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
            var selectedSensorItems = lstSensors.SelectedItems.Cast<SensorListItem>().ToList();
            var selectedTagItem = lstPlcTags.SelectedItem as PlcTagListItem;

            // El botón "Map" ahora solo se activa si se selecciona EXACTAMENTE un sensor
            // y un tag que no estén ya en uso.
            bool canMap = selectedSensorItems.Count == 1 &&
                          selectedTagItem != null &&
                          !selectedSensorItems.First().IsMapped &&
                          !selectedTagItem.IsMapped;
            btnMapSelected.Enabled = canMap;

            // El botón "Unmap" se activa si CUALQUIERA de los sensores seleccionados está mapeado.
            btnUnmapSelected.Enabled = selectedSensorItems.Any(s => s.IsMapped);
        }

        private void lstSensors_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void lstPlcTags_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
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
            var selectedSensorsToUnmap = lstSensors.SelectedItems
                .Cast<SensorListItem>()
                .Where(s => s.IsMapped)
                .ToList();

            if (!selectedSensorsToUnmap.Any())
            {
                MessageBox.Show("Please select at least one mapped sensor to unmap.", "No Mapped Sensors Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 2. Muestra un mensaje de confirmación con la cantidad correcta.
            var result = MessageBox.Show(
                $"Are you sure you want to unmap {selectedSensorsToUnmap.Count} sensor(s)?",
                "Confirm Unmap",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // 3. Itera sobre la lista y desmapea cada uno.
                foreach (var sensorItem in selectedSensorsToUnmap)
                {
                    // Llama a la lógica central de desmapeo.
                    _tagMapper.UnmapSensor(sensorItem.Sensor.SensorId);

                    // Actualiza el objeto SensorControl original.
                    sensorItem.Sensor.PlcTag = null;
                }

                // El evento 'MappingChanged' se habrá disparado por cada sensor,
                // actualizando la UI automáticamente.
                lblStatus.Text = $"{selectedSensorsToUnmap.Count} sensor(s) unmapped.";
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.RotateSensorText = chkRotate.Checked;
            Properties.Settings.Default.Save();
            var saveSuccess = _ownerPartViewer.SaveLayout();
            if (saveSuccess)
            {
                DialogResult = DialogResult.OK;
                Close();
            }           
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

        private void btnAutoMap_Click(object sender, EventArgs e)
        {
            // 1-Obtener lista de sensores y tagos que no están mapeados
            var unmappedSensors = lstSensors.Items.OfType<SensorListItem>()
                .Where(s => !s.IsMapped)
                .ToList();

            var unmappedTags = lstPlcTags.Items.OfType<PlcTagListItem>()
                .Where(t => !t.IsMapped)
                .ToList();

            if(!unmappedSensors.Any() || !unmappedTags.Any())
            {
                MessageBox.Show("There are no unmapped sensors or tags available for auto-mapping.", 
                    "Auto-Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var mappingsMade = 0;

            // 2. Iterar sobre cada sensor sin mapear.
            foreach(var sensorItem in unmappedSensors)
            {
                // 3. Buscar un tag sin mapear que termine con el mismo número.
                var sensorNumberMatch = Regex.Match(sensorItem.Sensor.SensorId, @"\d+$");
                if (!sensorNumberMatch.Success) continue;
                var sensorNumber = sensorNumberMatch.Value;
                var matchingTagItem = unmappedTags.FirstOrDefault(tagItem =>
                {
                    var tagNumberMatch = Regex.Match(tagItem.TagName, @"\d+$");
                    return tagNumberMatch.Success && tagNumberMatch.Value == sensorNumber;
                });
                // 4. Si se encuentra una coincidencia, realiza el mapeo.
                if(matchingTagItem != null)
                {
                    _tagMapper.MapSensorToTag(sensorItem.Sensor.SensorId, matchingTagItem.TagName);
                    sensorItem.Sensor.PlcTag = matchingTagItem.TagName;
                    mappingsMade++;
                }
            }
            // 5. Muestra un resumen de la operación.
            if (mappingsMade > 0)
            {
                MessageBox.Show($"{mappingsMade} new mappings were created automatically.", 
                    "Auto-Map Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No matching sensors and tags were found to auto-map.", "" +
                    "Auto-Map", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void lstSensors_MouseDown(object sender, MouseEventArgs e)
        {
            if(lstSensors.SelectedItem != null)
                lstSensors.DoDragDrop(lstSensors.SelectedItem, DragDropEffects.Move);
        }

        private void lstPlcTags_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(typeof(SensorListItem)) ? DragDropEffects.Move : DragDropEffects.None;
        }

        private void lstPlcTags_DragDrop(object sender, DragEventArgs e)
        {
            var dropPoint = lstPlcTags.PointToClient(new Point(e.X, e.Y));
            var index = lstPlcTags.IndexFromPoint(dropPoint);
            if (index == ListBox.NoMatches) return;
            var sensorItem = e.Data.GetData(typeof(SensorListItem)) as SensorListItem;
            var tagItem = lstPlcTags.Items[index] as PlcTagListItem;
            if (sensorItem != null && tagItem != null && !sensorItem.IsMapped && !tagItem.IsMapped)
                _tagMapper.MapSensorToTag(sensorItem.Sensor.SensorId, tagItem.TagName);
        }

        private async void btnGetFromPLC_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Reading tags from PLC... Please wait.";
            Enabled = false; 

            try
            {
                // 1. Crea el tag especial "@tags" para listar todos los demás.
                var listTagsCommand = new Tag<TagInfoPlcMapper, TagInfo[]>()
                {
                    Gateway = Properties.Settings.Default.PLC_IP,
                    Path = Properties.Settings.Default.PLC_Path,
                    PlcType = (PlcType)Properties.Settings.Default.PLC_Type,
                    Protocol = (Protocol)Properties.Settings.Default.PLC_Protocol, // Corregido: Usaba PLC_Type por error
                    Timeout = TimeSpan.FromSeconds(Properties.Settings.Default.PLC_Timeout),
                    Name = "@tags"
                };

                // 2. Lee los tags de forma asíncrona para no congelar la aplicación.
                await listTagsCommand.ReadAsync();
              
                // 3. Filtra el resultado para obtener solo los tags que cumplen AMBAS condiciones:
                //    - Son de tipo BOOL.
                //    - NO están ya en nuestra lista de tags manuales.
                var newBoolTags = listTagsCommand.Value
                    .Where(tag => tag.Name.Any(char.IsDigit) && !_manualTags.Contains(tag.Name, StringComparer.OrdinalIgnoreCase))
                    .Select(tag => tag.Name)
                    .ToList();

                // 4. Si encontramos nuevos tags, los añadimos al catálogo y guardamos.
                if (newBoolTags.Any())
                {
                    _manualTags.AddRange(newBoolTags);
                    _manualTags.Sort();
                    _catalogManager.Save(_manualTags); // Guarda los nuevos tags en el archivo JSON

                    MessageBox.Show($"{newBoolTags.Count} new tags were found and added to the catalog.",
                                    "PLC Tags Imported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No newOOL tags were found on the PLC.",
                                    "PLC Tags", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // 5. Refresca toda la interfaz gráfica con la lista actualizada.
                RefreshTagList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read tags from PLC: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 6. Pase lo que pase, vuelve a habilitar el formulario.
                lblStatus.Text = "Ready.";
                this.Enabled = true;
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
