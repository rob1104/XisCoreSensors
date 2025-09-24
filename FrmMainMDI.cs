using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using XisCoreSensors.Controls;
using XisCoreSensors.Plc;
using XisCoreSensors.PLC;
using XisCoreSensors.Properties;
using static XisCoreSensors.Mapping.SensorTagMapping;

namespace XisCoreSensors
{
    public partial class FrmMainMDI : Form
    {
        //Variables pantalla completa---
        private bool _isFullScreen = false;
        private FormWindowState _previousWindowState;
        private FormBorderStyle _previousBorderStyle;
        //-----------------------------

        private PlcService _plcService;
        private PlcController _plcController;
        private System.Windows.Forms.Timer _monitoringIndicatorTimer;
        private bool _indicatorVisible = false;

        private System.Windows.Forms.Timer _reconnectTimer;

        public TagMapper TagMapper { get; } = new TagMapper();

        private enum PlcUiState { Disconnected, Connected, Monitoring, Paused, Error, Idle, Reconnecting, SequencePaused }

        private CancellationTokenSource _monitoringCts;
        private Task _monitoringTask;

        public FrmMainMDI()
        {
            InitializeComponent();
            ConfigureNotificationBar();
            ConfigureMonitoringTimer();            
        }

        private void ConfigureMonitoringTimer()
        {
            _monitoringIndicatorTimer = new System.Windows.Forms.Timer();
            _monitoringIndicatorTimer.Interval = 750;
            _monitoringIndicatorTimer.Tick += MonitoringIndicatorTimer_Tick;

            _reconnectTimer = new System.Windows.Forms.Timer();
            _reconnectTimer.Interval = 5000;
            _reconnectTimer.Tick += ReconnectTimer_Tick;
        }

        private void MonitoringIndicatorTimer_Tick(object sender, EventArgs e)
        {
            lblPlcStatus.Text = _indicatorVisible ? "PLC Monitoring" : "PLC: Monitoring ●";
            _indicatorVisible = !_indicatorVisible;
        }     

        private void PlcService_MonitoringError(string errorMessage)
        {
            // Aseguramos que se ejecute en el hilo de la UI
            if (InvokeRequired)
            {
                Invoke(new Action(() => PlcService_MonitoringError(errorMessage)));
                return;
            }
            UpdatePlcStatus(PlcUiState.Reconnecting, errorMessage);
        }

        public void StartMonitoringForLayout(List<string> boolTags)
        {
            if (_plcService == null) return;
            _plcService.StopMonitoring();
            _monitoringIndicatorTimer.Stop();
            _plcService.ClearTags();
            _plcService.InitializeBoolTags(boolTags);
            _plcService.StartMonitoring();            
            UpdatePlcStatus(PlcUiState.Monitoring);
        }

        private void PlcController_SensorStateUpdateRequested(string sensorId, bool isFailed)
        {
            // Esta parte para asegurar que se ejecute en el hilo de la UI es correcta.
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => PlcController_SensorStateUpdateRequested(sensorId, isFailed)));
                return;
            }

            // --- LA CORRECCIÓN CLAVE ---
            // En lugar de buscar solo en 'ActiveMdiChild', recorremos TODAS las ventanas hijas.
            // 'this.MdiChildren' es un array con todos los formularios abiertos dentro del MDI.
            foreach (Form childForm in this.MdiChildren)
            {
                // Verificamos si la ventana actual es un FrmPartViewer.
                if (childForm is FrmPartViewer viewer)
                {
                    // Si lo es, llamamos a su método de actualización.
                    // El propio FrmPartViewer se encargará de ver si contiene ese sensor.
                    viewer.UpdateSensorState(sensorId, isFailed);
                }
            }
        }        

        private void model1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadModel();
        }

        private void mnuChkEditMode_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;

            // 1. Obtenemos una referencia al formulario hijo que está activo.
            var activeChild = ActiveMdiChild;

            // 2. Verificamos que exista un hijo activo y que sea del tipo FrmPartViewer.
            if (activeChild is FrmPartViewer viewer)
            {
               
            }
            else
            {                
                MessageBox.Show("Please open a model before enter in edit mode..", "No active model viewer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Si ya estamos en modo edición, simplemente salimos sin pedir contraseña.
            if (menuItem.Checked)
            {
                if (ActiveMdiChild is FrmPartViewer activeViewer)
                {
                    activeViewer.ToggleEditMode();
                    menuItem.Checked = false;
                }
                return;
            }

            // --- Lógica para ENTRAR al modo edición ---

            string storedHash = Settings.Default.EditModePasswordHash;

            // Escenario 1: No hay contraseña guardada (primer uso)
            if (string.IsNullOrEmpty(storedHash))
            {
                string newPassword;
                string confirmPassword;

                using (var form = new FrmPassword("Please create a password:", "Create Password"))
                {
                    if (form.ShowDialog() != DialogResult.OK) return;
                    newPassword = form.Password;
                }

                using (var form = new FrmPassword("Confirm new Password:", "Confirm Contraseña"))
                {
                    if (form.ShowDialog() != DialogResult.OK) return;
                    confirmPassword = form.Password;
                }

                if (newPassword == confirmPassword && !string.IsNullOrEmpty(newPassword))
                {
                    // Las contraseñas coinciden, guardamos el hash.
                    Settings.Default.EditModePasswordHash = SecurityHelper.HashPassword(newPassword);
                    Settings.Default.Save();
                    MessageBox.Show("Password created successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("The passwords do not match or are empty. Please try again..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return; // No entramos en modo edición.
                }
            }

            // Escenario 2: Ya existe una contraseña, la pedimos.
            using (var form = new FrmPassword("Enter the password for Edit Mode:", "Edit Mode"))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    string inputHash = SecurityHelper.HashPassword(form.Password);
                    if (inputHash == Properties.Settings.Default.EditModePasswordHash)
                    {
                        // Contraseña correcta, entramos en modo edición.
                        if (this.ActiveMdiChild is FrmPartViewer activeViewer)
                        {
                            activeViewer.ToggleEditMode();
                            menuItem.Checked = true;
                            MessageBox.Show("Edit mode active, you can add, delete or rename sensors. Ensure yo save new layout to keep changes!!!!", "Edit Mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Incorrect Password", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void loadImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 1. Obtenemos una referencia al formulario hijo que está activo.
            Form activeChild = this.ActiveMdiChild;

            // 2. Verificamos que exista un hijo activo y que sea del tipo FrmPartViewer.
            if (activeChild is FrmPartViewer viewer)
            {
                // 3. Llamamos al método público del hijo para que haga todo el trabajo.
                viewer.LoadNewImage();
            }
            else
            {
                // Opcional: Avisar al usuario si no hay una ventana de visor abierta.
                MessageBox.Show("Please open a model before load an image.", "No active model viewer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void saveLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 1. Obtenemos una referencia al formulario hijo que está activo.
            Form activeChild = this.ActiveMdiChild;

            // 2. Verificamos que exista un hijo activo y que sea del tipo FrmPartViewer.
            if (activeChild is FrmPartViewer viewer)
            {
                // 3. Llamamos al método público del hijo para que haga todo el trabajo.
                viewer.SaveLayoutAs();
            }
            else
            {
                // Opcional: Avisar al usuario si no hay una ventana de visor abierta.
                MessageBox.Show("Please open a model before save a layout.", "No active model viewer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void loadLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 1. Obtenemos una referencia al formulario hijo que está activo.
            Form activeChild = this.ActiveMdiChild;

            // 2. Verificamos que exista un hijo activo y que sea del tipo FrmPartViewer.
            if (activeChild is FrmPartViewer viewer)
            {
                // 3. Llamamos al método público del hijo para que haga todo el trabajo.
                viewer.LoadLayout();
            }
            else
            {
                // Opcional: Avisar al usuario si no hay una ventana de visor abierta.
                MessageBox.Show("Please open a model before save a layout.", "No active model viewer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void FrmMainMDI_Load(object sender, EventArgs e)
        {
            try
            {
                if (Settings.Default.LastLayoutPath != string.Empty)
                {
                    LoadModel();
                }
                await ReinitializePlcAsync();               

               
            }
            catch (Exception exception)
            {
                lblPlcStatus.Text = $"PLC:Config ERROR. {exception}";
            }            
        }

        private void LoadModel()
        {
            if (ActiveMdiChild is FrmPartViewer existingViewer)
            {
                existingViewer.Focus();
            }
            else
            {
                var partViewer = new FrmPartViewer();
                partViewer.MdiParent = this;
                partViewer.WindowState = FormWindowState.Maximized;
                partViewer.EditModeChanged += Viewer_EditModeChanged;
                partViewer.OnSensorFailed += ViewerForm_OnSensorFailed;
                partViewer.FormClosed += Viewer_FormClosed;
                partViewer.Show();
            }
        }

        private void Viewer_EditModeChanged(object sender, EditModeChangedEventArgs e)
        {
            lblEditModeStatus.Text = e.StatusMessage;
            lblEditModeStatus.BackColor = e.IsInEditMode ? Color.Yellow : Color.Lime;
            mnuChkEditMode.Checked = e.IsInEditMode;

            pLCToolStripMenuItem.Enabled = e.IsInEditMode;
            tagMapperToolStripMenuItem.Enabled = e.IsInEditMode;
            saveLayoutToolStripMenuItem.Enabled = e.IsInEditMode;
            loadLayoutToolStripMenuItem.Enabled = e.IsInEditMode;
            saveLayoutToolStripMenuItem1.Enabled = e.IsInEditMode;
            loadImageToolStripMenuItem.Enabled = e.IsInEditMode;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new FrmAbout().ShowDialog();
        }

        private void FrmMainMDI_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                ToggleFullScreen();
            }
        }

        private void ToggleFullScreen()
        {
            if (!_isFullScreen)
            {
                // Guardar el estado actual
                _previousWindowState = this.WindowState;
                _previousBorderStyle = this.FormBorderStyle;
                // Cambiar a pantalla completa
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                //Cambiar hijo MDI
                if (this.ActiveMdiChild != null)
                {
                    this.ActiveMdiChild.WindowState = FormWindowState.Maximized;
                }
                _isFullScreen = true;
            }
            else
            {
                // Restaurar el estado anterior
                this.FormBorderStyle = _previousBorderStyle;
                this.WindowState = _previousWindowState;
                _isFullScreen = false;
            }
        }
        
        private void ViewerForm_OnSensorFailed(string message)
        {
            notificationBar1.BringToFront();
            notificationBar1.ShowMessage(message, 600);
        }
        
        private void ConfigureNotificationBar()
        {
            if(notificationBar1 != null)
            {
                notificationBar1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                notificationBar1.Width = ClientSize.Width;
            }
        }

        private void saveLayoutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // Llama al método SaveLayout del formulario hijo ACTIVO.
            if (this.ActiveMdiChild is FrmPartViewer activeChild)
            {
                activeChild.SaveLayout();
            }
        }

        private async void pLCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (var configForm = new FrmConfigPLC())                
                {                   
                    var result = configForm.ShowDialog();
                    if (result != DialogResult.OK) return;
                    MessageBox.Show("PLC configuration saved. Attempting to reconnect with the new settings....", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await ReinitializePlcAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error saving new config: " + ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private async void tagMapperToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(!(ActiveMdiChild is FrmPartViewer activeViewer))
            {
                MessageBox.Show("Please open a model before open Tag Mapper.", "No active model viewer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sensors = activeViewer.GetSensors();

            if(sensors == null || !sensors.Any())
            {
                MessageBox.Show("No sensors found in the current model. Please add sensors before opening Tag Mapper.", "No sensors", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            using (var tagMapperForm = new FrmTagMapper(activeViewer, sensors, TagMapper))
            {
                var result = tagMapperForm.ShowDialog();

                if (result != DialogResult.OK) return;
                ApplyMappingsToSensors(sensors);                   
                await ReinitializePlcAsync();
                MessageBox.Show("Tag mappings and layout have been updated and the PLC connection has been refreshed.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        private void ApplyMappingsToSensors(List<SensorControl> sensors)
        {
            foreach (var sensor in sensors)
            {
                var mappedTag = TagMapper.GetTagForSensor(sensor.SensorId);
                sensor.PlcTag = mappedTag;

                if(sensor.Status != SensorControl.SensorStatus.Fail)
                {
                    sensor.Status = string.IsNullOrEmpty(mappedTag) ? SensorControl.SensorStatus.Unmapped : SensorControl.SensorStatus.Ok;
                }
            }
        }

        private void FrmMainMDI_FormClosing(object sender, FormClosingEventArgs e)
        {
            _plcService?.StopMonitoring();
            _plcService?.Dispose();
            _plcController?.Unsubscribe();
        }

        private async Task ReinitializePlcAsync()
        {
            // 1. Detiene lo anterior (sin cambios)
            _plcService?.StopMonitoring();
            _plcService?.Dispose();
            _plcController?.Unsubscribe();

            UpdatePlcStatus(PlcUiState.Idle, "Applying new config...");

            // 2. Crea nuevas instancias (sin cambios)
            _plcService = new PlcService();
            _plcController = new PlcController(_plcService, TagMapper);
            _plcService.MonitoringError += PlcService_MonitoringError;
            _plcService.TagReadError += PlcService_TagReadError;
            _plcController.SensorStateUpdateRequested += PlcController_SensorStateUpdateRequested;

            _plcController.BoolMonitoringPausedStateChanged += PlcController_BoolMonitoringPausedStateChanged;

            // 3. Carga los tags del catálogo (sin cambios)
            var catalogManager = new TagCatalogManager();
            var knownBoolTags = catalogManager.Load();
            if (knownBoolTags.Any())
            {
                _plcService.InitializeBoolTags(knownBoolTags);
            }

            // 4. --- LÓGICA DE CONEXIÓN CORREGIDA ---
            if (await _plcService.TestConnectionAsync(knownBoolTags.FirstOrDefault()))
            {
                // Si la conexión es exitosa, actualiza el estado...
                UpdatePlcStatus(PlcUiState.Connected);
                // ...e INICIA EL MONITOREO INCONDICIONALMENTE.
                // El servicio estará activo y listo para cuando se cargue un layout.
                _plcService.StartMonitoring();
                UpdatePlcStatus(PlcUiState.Monitoring);
            }
            else
            {
                // Si la conexión falla, entra en el ciclo de reconexión (sin cambios)
                UpdatePlcStatus(PlcUiState.Error, "Connection Error.");
                var r = MessageBox.Show("A connection to the PLC could not be established. Do you want to open the configuration?", "PLC ERROR", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if(r == DialogResult.Yes)
                {
                    new FrmConfigPLC().ShowDialog();
                }
                   
            }

            _plcController.SecuenceStepChanged += PlcController_SequenceStepChanged;
            string sequenceTag = Properties.Settings.Default.SequenceTagName;
            if (!string.IsNullOrEmpty(sequenceTag))
            {
                _plcService.InitializeDintTags(new[] { sequenceTag });
            }
        }

        private void PlcController_BoolMonitoringPausedStateChanged(bool isPaused)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => PlcController_BoolMonitoringPausedStateChanged(isPaused)));
                return;
            }

            // Pasa la orden de actualizar el estado visual a todos los visores abiertos.
            foreach (var viewer in this.MdiChildren.OfType<FrmPartViewer>())
            {
                viewer.SetSensorsPausedVisualState(isPaused);
            }
        }

        private void PlcController_SequenceStepChanged(int newStep)
        {
            if(InvokeRequired)
            {
                Invoke(new Action(() => PlcController_SequenceStepChanged(newStep)));
                return;
            }        
            
            if(newStep == 0)
            {
                PausePlcMonitoring();
                UpdatePlcStatus(PlcUiState.SequencePaused);
                sequenceNotificationBar.ShowMessage("WAITING FOR OPERATOR TO LOAD PART");
            }
            else
            {
                ResumePlcMonitoring();
                sequenceNotificationBar.HideMessage();
            }                     
        }

        public void PausePlcMonitoring()
        {
            if (_plcService == null) return;
            _plcService.StopMonitoring();
            UpdatePlcStatus(PlcUiState.Paused);
        }

        public void ResumePlcMonitoring()
        {
            if (_plcService == null) return;
            if (lblPlcStatus.Text.Contains("Idle")) return;
            _plcService.StartMonitoring();
            UpdatePlcStatus(PlcUiState.Monitoring);
        }

        private void UpdatePlcStatus(PlcUiState state, string details = "")
        {
            _monitoringIndicatorTimer.Stop();
            lblPlcStatus.Text = details;
            lblPlcStatus.ForeColor = Color.White;
            switch(state)
            {
                case PlcUiState.Disconnected:
                    lblPlcStatus.Text = "PLC: Disconnected";
                    lblPlcStatus.BackColor = Color.Orange;
                    break;
                case PlcUiState.Connected:
                    lblPlcStatus.Text = "PLC: Connected";
                    lblPlcStatus.BackColor = Color.Green;
                    break;
                case PlcUiState.Monitoring:
                    if (_plcService != null && _plcService.IsMonitoring)
                    {
                        lblPlcStatus.BackColor = Color.Green;
                        _monitoringIndicatorTimer.Start();
                    }
                    else
                    {
                        lblPlcStatus.Text = "PLC: Monitoring Error: " + details;
                        lblPlcStatus.BackColor = Color.Red;
                    }
                    break;
                case PlcUiState.Paused:
                    lblPlcStatus.Text = "PLC: Paused (Edit Mode)";
                    lblPlcStatus.BackColor = Color.Purple;
                    break;
                case PlcUiState.Error:
                    lblPlcStatus.Text = "PLC: ERROR:" + details;
                    lblPlcStatus.BackColor = Color.Red;
                    _plcService?.StopMonitoring();
                    break;
                case PlcUiState.Idle:
                    lblPlcStatus.Text = "PLC: Idle";
                    lblPlcStatus.BackColor = Color.SlateGray;
                    _plcService?.StopMonitoring();
                    break;
                case PlcUiState.Reconnecting:
                    lblPlcStatus.Text = "PLC: Lost connection. Reconeccting...";
                    lblPlcStatus.BackColor = Color.Red;
                    _plcService?.StopMonitoring();
                    _reconnectTimer.Start();
                    break;
                case PlcUiState.SequencePaused:
                    lblPlcStatus.Text = "PLC: Paused by Sequence (Step 0)";
                    lblPlcStatus.BackColor = Color.Gold;
                    lblPlcStatus.ForeColor = Color.Black;
                    _plcService?.StopMonitoring();
                    break;
            }
        }

        private void UpdateMonitoringState()
        {     
            _plcService?.StopMonitoring();
            UpdatePlcStatus(PlcUiState.Idle);            
        }


        private void Viewer_FormClosed(object sender, FormClosedEventArgs e)
        {            
            UpdateMonitoringState();
        }
        private async void ReconnectTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                _reconnectTimer.Stop(); 
                await ReinitializePlcAsync();
            }
            catch (Exception exception)
            {
                UpdatePlcStatus(PlcUiState.Error, exception.Message);
            }
            
        }

        private void PlcService_TagReadError(string tagName, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => PlcService_TagReadError(tagName, message)));
                //return;
            }            
        }

        private void changeEditModePasswordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var changePasswordForm = new FrmChangePassword())
            {
                changePasswordForm.ShowDialog(this);
            }
        }
    }
}
