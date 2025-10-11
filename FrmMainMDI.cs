using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using XisCoreSensors.Controls;
using XisCoreSensors.Plc;
using XisCoreSensors.PLC;
using XisCoreSensors.Properties;
using static XisCoreSensors.Mapping.SensorTagMapping;
using static XisCoreSensors.PLC.PlcController;

namespace XisCoreSensors
{
    public partial class FrmMainMDI : Form
    {
        //Variables pantalla completa---
        private bool _isFullScreen = false;
        private FormWindowState _previousWindowState;
        private FormBorderStyle _previousBorderStyle;
        private bool _originalTopMost;
        //-----------------------------

        private PlcService _plcService;
        private PlcController _plcController;
        private System.Windows.Forms.Timer _monitoringIndicatorTimer;
        private bool _indicatorVisible = false;
        private AlertManager _alertManager;

        private System.Windows.Forms.Timer _reconnectTimer;

        public TagMapper TagMapper { get; } = new TagMapper();

        private enum PlcUiState { Disconnected, Connected, Monitoring, Paused, Error, Idle, Reconnecting, SequencePaused }

        private bool _isInRecoveryMode = false;
        private System.Windows.Forms.Timer _recoveryAttemptTimer;
        private System.Windows.Forms.Timer _healthDisplayTimer;

        public FrmMainMDI()
        {
            InitializeComponent();
            _alertManager = new AlertManager("alerts.json");
            ConfigureStatusStrip();
            ConfigureNotificationBar();
            ConfigureMonitoringTimer();
            UpdateConnectionHealthDisplay();
        }

        private bool ValidateMonitoringState()
        {
            if (_plcService == null) return false;

            // Durante transiciones (como salir del modo edición), ser más permisivo
            var timerEnabled = _plcService.TimerIsEnabled;
            var shouldBeMonitoring = _plcService.ShouldBeMonitoring;
            var isHealthy = _plcService.IsConnectionHealthy;

            // Si debería estar monitoreando, el timer está corriendo y la conexión es saludable,
            // entonces probablemente está bien (aunque _isActuallyMonitoring aún no se haya actualizado)
            if (shouldBeMonitoring && timerEnabled && isHealthy)
            {
                return true;
            }

            // Para otros casos, usar la lógica más estricta
            var monitoringState = _plcService.GetMonitoringState();
            var actuallyIs = (bool)monitoringState["IsActuallyMonitoring"];
            var hasTags = (bool)monitoringState["HasTags"];

            return shouldBeMonitoring && actuallyIs && hasTags;
        }

        private void ConfigureMonitoringTimer()
        {
            _monitoringIndicatorTimer = new System.Windows.Forms.Timer();
            _monitoringIndicatorTimer.Interval = 750;
            _monitoringIndicatorTimer.Tick += MonitoringIndicatorTimer_Tick;

            _reconnectTimer = new System.Windows.Forms.Timer();
            _reconnectTimer.Interval = 5000;
            _reconnectTimer.Tick += ReconnectTimer_Tick;

            _recoveryAttemptTimer = new System.Windows.Forms.Timer();
            _recoveryAttemptTimer.Interval = 15000;
            _recoveryAttemptTimer.Tick += RecoveryAttemptTimer_Tick;

            _healthDisplayTimer = new System.Windows.Forms.Timer();
            _healthDisplayTimer.Interval = 5000; // Actualizar cada 5 segundos
            _healthDisplayTimer.Tick += (s, e) => UpdateConnectionHealthDisplay();
            _healthDisplayTimer.Start();
        }

        private void PlcService_ConnectionStateChanged(bool isHealty)
        {
            if(InvokeRequired)
            {
                Invoke(new Action(() => PlcService_ConnectionStateChanged(isHealty)));
                return; 
            }

            if(isHealty)
            {
                if(_isInRecoveryMode)
                {
                    UpdatePlcStatus(PlcUiState.Connected, "Connection recovered.");
                    _isInRecoveryMode = false;
                    _recoveryAttemptTimer.Stop();

                    if(_plcService != null && !_plcService.IsMonitoring)
                    {
                        _plcService.StartMonitoring();
                        UpdatePlcStatus(PlcUiState.Monitoring);
                    }
                    else if(_plcService != null && _plcService.IsMonitoring)
                    {
                        UpdatePlcStatus(PlcUiState.Monitoring, "Connection healthy");
                    }
                }
            }
            else
            {
                if(_plcService != null && _plcService.IsMonitoring)
                {
                    UpdatePlcStatus(PlcUiState.Monitoring, "Connection issues detected");
                }
                else
                {
                    UpdatePlcStatus(PlcUiState.Error, "Connection health issues detected.");
                }
                    
                if(!_isInRecoveryMode)
                {
                    _isInRecoveryMode = true;
                    _recoveryAttemptTimer.Start();
                }
            }
            UpdateConnectionHealthDisplay();
        }

        private void PlcService_ConnectionRecovered(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => PlcService_ConnectionRecovered(message)));
                return;
            }
            UpdatePlcStatus(PlcUiState.Connected, message);
            UpdateConnectionHealthDisplay();
        }

        private async void RecoveryAttemptTimer_Tick(object sender, EventArgs e)
        {
            if (!_isInRecoveryMode)
            {
                _recoveryAttemptTimer.Stop();
                return;
            }

            UpdatePlcStatus(PlcUiState.Reconnecting, "Attempting automatic recovery...");

            try
            {
               
                var testTag = "SEQ";

                if (!string.IsNullOrEmpty(testTag) && await _plcService.TestConnectionAsync(testTag))
                {
                    _isInRecoveryMode = false;
                    _recoveryAttemptTimer.Stop();
                    UpdatePlcStatus(PlcUiState.Connected, "Connection recovered automatically");

                    // Reiniciar monitoreo si es necesario
                    if (!_plcService.IsMonitoring)
                    {
                        _plcService.StartMonitoring();
                        UpdatePlcStatus(PlcUiState.Monitoring);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdatePlcStatus(PlcUiState.Error, $"Recovery failed: {ex.Message}");
            }
        }

        private void MonitoringIndicatorTimer_Tick(object sender, EventArgs e)
        {
            if (_plcService != null && ValidateMonitoringState())
            {
                if (_plcService.IsConnectionHealthy)
                {
                    lblPlcStatus.Text = _indicatorVisible ? "PLC: Monitoring" : "PLC: Monitoring ●";
                    lblPlcStatus.BackColor = Color.Green;
                    lblPlcStatus.ForeColor = Color.White;
                }
                else
                {
                    lblPlcStatus.Text = _indicatorVisible ? "PLC: Monitoring (Issues)" : "PLC: Monitoring (Issues) ⚠";
                    lblPlcStatus.BackColor = Color.Orange;
                    lblPlcStatus.ForeColor = Color.Black;
                }
            }
            else if (_plcService != null && _plcService.ShouldBeMonitoring)
            {
                // Debería estar monitoreando pero no lo está
                lblPlcStatus.Text = _indicatorVisible ? "PLC: Monitor Failed" : "PLC: Monitor Failed ✖";
                lblPlcStatus.BackColor = Color.Red;
                lblPlcStatus.ForeColor = Color.White;
            }

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
            editAlertMessagesToolStripMenuItem.Enabled = e.IsInEditMode;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new FrmAbout().ShowDialog();
        }

        private void ToggleFullScreen()
        {
            if (!_isFullScreen)
            {
                // Guardar el estado actual
                _previousWindowState = WindowState;
                _previousBorderStyle = FormBorderStyle;
                _originalTopMost = TopMost;
                
                if(menuStrip1 != null)
                {
                    menuStrip1.Visible = false;
                }

                // Cambiar a pantalla completa
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                TopMost = true;
                //Cambiar hijo MDI
                if (ActiveMdiChild != null)
                {
                    ActiveMdiChild.WindowState = FormWindowState.Maximized;
                }
                _isFullScreen = true;
            }
            else
            {
                // Restaurar el estado anterior
                FormBorderStyle = _previousBorderStyle;
                WindowState = _previousWindowState;
                TopMost = _originalTopMost;
                _isFullScreen = false;
                if (menuStrip1 != null)
                {
                    menuStrip1.Visible = true;
                }
            }
        }
        
        private void ViewerForm_OnSensorFailed(string message)
        {
            //notificationBar1.BringToFront();
            //notificationBar1.ShowMessage(message, 400);
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
            _recoveryAttemptTimer?.Stop();
            _reconnectTimer?.Dispose();
            _healthDisplayTimer?.Stop();
            _healthDisplayTimer?.Dispose();
            _plcService?.StopMonitoring();
            _plcService?.Dispose();
            _plcController?.Unsubscribe();
        }

        private async Task ReinitializePlcAsync()
        {
            // 1. Detiene lo anterior
            _plcService?.StopMonitoring();
            _plcService?.Dispose();
            _plcController?.Unsubscribe();
            _recoveryAttemptTimer.Stop();
            _isInRecoveryMode = false;

            UpdatePlcStatus(PlcUiState.Idle, "Applying new config...");

            // 2. Crea nuevas instancias
            _plcService = new PlcService();
            _plcController = new PlcController(_plcService, TagMapper);

            // 2.5 Suscribirse a eventos
            _plcService.MonitoringError += PlcService_MonitoringError;
            _plcService.TagReadError += PlcService_TagReadError;
            _plcService.ConnectionStateChanged += PlcService_ConnectionStateChanged;
            _plcService.ConnectionRecovered += PlcService_ConnectionRecovered;
            _plcController.SensorStateUpdateRequested += PlcController_SensorStateUpdateRequested;
            _plcController.BoolMonitoringPausedStateChanged += PlcController_BoolMonitoringPausedStateChanged;
            _plcController.SecuenceStepChanged += PlcController_SequenceStepChanged;
            _plcController.ImageSelectorTagChanged += PlcController_ImageSelectorTagChanged;
            _plcController.StopwatchCommandReceived += PlcController_StopwatchCommandReceived;

            // 3. Carga los tags del catálogo
            var catalogManager = new TagCatalogManager();
            var knownBoolTags = catalogManager.Load();
            if (knownBoolTags.Any())
            {
                _plcService.InitializeBoolTags(knownBoolTags);
            }

            
            if (await _plcService.TestConnectionAsync("SEQ"))
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
                UpdatePlcStatus(PlcUiState.Error, "Initial connection failed.");
                var r = MessageBox.Show("A connection to the PLC could not be established. Do you want to open the configuration?", "PLC ERROR", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if(r == DialogResult.Yes)
                {
                    new FrmConfigPLC().ShowDialog();
                }                   
            }
            
            var sequenceTag = Settings.Default.SequenceTagName;
            if (!string.IsNullOrEmpty(sequenceTag))
            {
                _plcService.InitializeDintTags(new[] { sequenceTag });
            }

            var imageTag = Settings.Default.ImageTagName;
            if (!string.IsNullOrEmpty(imageTag))
            {
                _plcService.InitializeDintTags(new[] { imageTag });
            }

            var crhonoTag = Settings.Default.ChronoTgName;
            if(!string.IsNullOrEmpty(crhonoTag))
            {
                _plcService.InitializeDintTags(new[] { crhonoTag });
            }
        }

        private void PlcController_StopwatchCommandReceived(StopWatchCommand command)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => PlcController_StopwatchCommandReceived(command)));
                return;
            }

            // Busca el visor activo y le envía el comando.
            if (ActiveMdiChild is FrmPartViewer activeViewer)
            {
                activeViewer.ControlStopWatch(command);
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
            
            lblSequenceStatus.Text = $"SEQ: {newStep}"; 

            string alertMessage = _alertManager.GetMessageForSequence(newStep);

            if (!string.IsNullOrEmpty(alertMessage))
            {
                // Pausa el monitoreo de booleanos si es SEQ=0 (lógica que ya tenías)
                if (newStep == 0)
                {
                    PausePlcMonitoring();
                    UpdatePlcStatus(PlcUiState.SequencePaused);
                    
                }
                else
                {
                    ResumePlcMonitoring();
                }
                if (ActiveMdiChild is FrmPartViewer activeViewer)
                {
                    activeViewer.ShowAlertMessage(alertMessage?.ToUpper());
                }
                
            }
            else
            {
                // Si no hay un mensaje configurado para el número, reanuda el monitoreo y oculta la barra.
                ResumePlcMonitoring();
                
            }

            foreach (var viewer in this.MdiChildren.OfType<FrmPartViewer>())
            {
                viewer.UpdateSequenceStep(newStep);
            }

            if (newStep == 0)
            {
                PausePlcMonitoring();
                UpdatePlcStatus(PlcUiState.SequencePaused);
                //sequenceNotificationBar.ShowMessage("WAITING FOR OPERATOR TO LOAD PART");
            }
            else
            {
                ResumePlcMonitoring();
                //sequenceNotificationBar.HideMessage();
            }                     
        }

        private void PlcController_ImageSelectorTagChanged(string tagName, int newValue)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => PlcController_ImageSelectorTagChanged(tagName, newValue)));
                return;
            }
            
            foreach(var viewer in this.MdiChildren.OfType<FrmPartViewer>())
            {
                lblImGStatus.Text = $"IMG: {newValue}";
                viewer.SwitchBackgroundImage(newValue);
            }
        }

        public void PausePlcMonitoring()
        {
            if (_plcService == null) return;
            _plcService.PauseBoolMonitoring(true);
            UpdatePlcStatus(PlcUiState.Paused);
        }

        public void ResumePlcMonitoring()
        {
            if (_plcService == null) return;
            if (lblPlcStatus.Text.Contains("Idle")) return;

            if (ActiveMdiChild is FrmPartViewer activeViewer)
            {
                if(activeViewer.CurrentSequenceStep == 0)
                {
                    UpdatePlcStatus(PlcUiState.SequencePaused);
                    return;
                }
            }

            // Mostrar estado de transición
            UpdatePlcStatus(PlcUiState.Connected, "Resuming sensor monitoring...");

            // Reanudar el monitoreo de sensores booleanos
            _plcService.PauseBoolMonitoring(false);

            // Dar tiempo para que el primer ciclo se complete
            var resumeTimer = new System.Windows.Forms.Timer();
            resumeTimer.Interval = 1000;
            resumeTimer.Tick += (s, e) =>
            {
                resumeTimer.Stop();
                resumeTimer.Dispose();

                if (_plcService != null)
                {
                    if (_plcService.IsConnectionHealthy && _plcService.IsMonitoring)
                    {
                        UpdatePlcStatus(PlcUiState.Monitoring);
                    }
                    else if (_plcService.ShouldBeMonitoring)
                    {
                        UpdatePlcStatus(PlcUiState.Monitoring, "Connection issues");
                    }
                    else
                    {
                        UpdatePlcStatus(PlcUiState.Error, "Failed to resume monitoring");
                    }
                }
            };
            resumeTimer.Start();
        }

        private void UpdatePlcStatus(PlcUiState state, string details = "")
        {
            _monitoringIndicatorTimer.Stop();
            lblPlcStatus.Text = string.IsNullOrEmpty(details) ? "" : details;
            lblPlcStatus.ForeColor = Color.White;

            switch(state)
            {
                case PlcUiState.Disconnected:
                    lblPlcStatus.Text = "PLC: Disconnected";
                    lblPlcStatus.BackColor = Color.Orange;
                    lblPlcStatus.ForeColor = Color.Black;
                    break;
                case PlcUiState.Connected:
                    lblPlcStatus.Text = string.IsNullOrEmpty(details) ? "PLC: Connected" : $"PLC: {details}";
                    lblPlcStatus.BackColor = Color.Green;
                    break;
                case PlcUiState.Monitoring:
                    bool isActuallyMonitoring = ValidateMonitoringState();
                    if (isActuallyMonitoring && _plcService != null)
                    {
                        if (!string.IsNullOrEmpty(details) && details.ToLower().Contains("issue"))
                        {
                            lblPlcStatus.Text = $"PLC: {details}";
                            lblPlcStatus.BackColor = Color.Orange;
                            lblPlcStatus.ForeColor = Color.Black;
                            _monitoringIndicatorTimer.Start();
                        }
                        else if (_plcService.IsConnectionHealthy)
                        {
                            lblPlcStatus.BackColor = Color.Green;
                            _monitoringIndicatorTimer.Start();
                        }
                        else
                        {
                            lblPlcStatus.Text = "PLC: Monitoring (Connection Issues)";
                            lblPlcStatus.BackColor = Color.Orange;
                            lblPlcStatus.ForeColor = Color.Black;
                            _monitoringIndicatorTimer.Start();
                        }
                    }
                    else if (_plcService != null && _plcService.ShouldBeMonitoring)
                    {
                        // Debería estar monitoreando pero no lo está - problema
                        lblPlcStatus.Text = "PLC: Monitoring Failed";
                        lblPlcStatus.BackColor = Color.Red;
                    }
                    else
                    {
                        lblPlcStatus.Text = "PLC: Monitoring Error: " + details;
                        lblPlcStatus.BackColor = Color.Red;
                    }
                    break;
                case PlcUiState.Paused:
                    lblPlcStatus.Text = "PLC: Paused (Edit Mode)";
                    lblPlcStatus.BackColor = Color.Yellow;
                    lblPlcStatus.ForeColor = Color.Black;
                    break;
                case PlcUiState.Error:
                    lblPlcStatus.Text = string.IsNullOrEmpty(details) ? "PLC: ERROR" : $"PLC: ERROR - {details}";
                    lblPlcStatus.BackColor = Color.Red;
                    break;
                case PlcUiState.Idle:
                    lblPlcStatus.Text = string.IsNullOrEmpty(details) ? "PLC: Idle" : $"PLC: {details}";
                    lblPlcStatus.BackColor = Color.SlateGray;
                    _plcService?.StopMonitoring();
                    break;
                case PlcUiState.Reconnecting:
                    lblPlcStatus.Text = string.IsNullOrEmpty(details) ? "PLC: Reconnecting..." : $"PLC: {details}";
                    lblPlcStatus.BackColor = Color.Orange;
                    lblPlcStatus.ForeColor = Color.Black;
                    break;
                case PlcUiState.SequencePaused:
                    lblPlcStatus.Text = "PLC: Paused by Sequence (Step 0)";
                    lblPlcStatus.BackColor = Color.Gold;
                    lblPlcStatus.ForeColor = Color.Black;                    
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

        public Dictionary<string, object> GetPlcHealthStatistics()
        {
            return _plcService?.GetHealthStatistics() ?? new Dictionary<string, object>();
        }

        public async void ForceConnectionRecovery()
        {
            if (_plcService == null) return;

            UpdatePlcStatus(PlcUiState.Reconnecting, "Manual recovery attempt...");

            try
            {
                await ReinitializePlcAsync();
            }
            catch (Exception ex)
            {
                UpdatePlcStatus(PlcUiState.Error, $"Manual recovery failed: {ex.Message}");
            }
        }

        private void UpdateConnectionHealthDisplay()
        {
            if(_plcService == null)
            {
                lblConnectionHealth.Text = "CONN: No service";
                lblConnectionHealth.BackColor = Color.Gray;
                return;
            }

            var stats = GetPlcHealthStatistics();
            var isHealthy = (bool)stats["IsConnectionHealthy"];
            var totalBoolTags = (int)stats["TotalBoolTags"];
            var totalDintTags = (int)stats["TotalDintTags"];
            var tagsWithErrors = (int)stats["TagsWithErrors"];
            var consecutiveGlobalErrors = (int)stats["ConsecutiveGlobalErrors"];

            if (isHealthy && tagsWithErrors == 0)
            {
                lblConnectionHealth.Text = $"CONN: Healthy ({totalBoolTags + totalDintTags} tags)";
                lblConnectionHealth.BackColor = Color.Green;
                lblConnectionHealth.ForeColor = Color.White;
            }
            else if (isHealthy && tagsWithErrors > 0)
            {
                lblConnectionHealth.Text = $"CONN: Partial ({tagsWithErrors} issues)";
                lblConnectionHealth.BackColor = Color.Orange;
                lblConnectionHealth.ForeColor = Color.Black;
            }
            else if (_isInRecoveryMode)
            {
                lblConnectionHealth.Text = "CONN: Recovering...";
                lblConnectionHealth.BackColor = Color.Yellow;
                lblConnectionHealth.ForeColor = Color.Black;
            }
            else
            {
                lblConnectionHealth.Text = $"CONN: Error ({consecutiveGlobalErrors})";
                lblConnectionHealth.BackColor = Color.Red;
                lblConnectionHealth.ForeColor = Color.White;
            }

            lblConnectionHealth.ToolTipText = $"Connection Health Status\n" +
                                              $"Healthy: {isHealthy}\n" +
                                              $"Bool Tags: {totalBoolTags}\n" +
                                              $"DINT Tags: {totalDintTags}\n" +
                                              $"Tags with errors: {tagsWithErrors}\n" +
                                              $"Global errors: {consecutiveGlobalErrors}\n" +
                                              $"Recovery mode: {_isInRecoveryMode}";

        }

        private void forcePLCReconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //var r = MessageBox.Show(
            //    "This will force a reconnection to the PLC. Current monitoring will be stopped temporarily.\\n\\nDo you want to continue?", 
            //    "Confirm Reconnect", 
            //    MessageBoxButtons.YesNo, 
            //    MessageBoxIcon.Question);

            //if (r == DialogResult.Yes)
            ForceConnectionRecovery();
        }

        private void ConfigureStatusStrip()
        {
            // Hacer que lblPlcStatus se expanda para empujar lblConnectionHealth a la derecha
            lblPlcStatus.Spring = true;
            lblPlcStatus.TextAlign = ContentAlignment.MiddleLeft;

            // Configurar lblConnectionHealth para que se mantenga a la derecha
            lblConnectionHealth.TextAlign = ContentAlignment.MiddleRight;
            lblConnectionHealth.AutoSize = false;
            lblConnectionHealth.Width = 150; // Ancho fijo para el label de conexión
        }

        private void ShowMonitoringDiagnostics()
        {
            if (_plcService == null)
            {
                MessageBox.Show("PLC Service is null", "Diagnostics");
                return;
            }

            var state = _plcService.GetMonitoringState();
            var health = _plcService.GetHealthStatistics();

            var message = "=== MONITORING DIAGNOSTICS ===\n\n" +
                          $"IsDisposed: {state["IsDisposed"]}\n" +
                          $"ShouldBeMonitoring: {state["ShouldBeMonitoring"]}\n" +
                          $"IsActuallyMonitoring: {state["IsActuallyMonitoring"]}\n" +
                          $"TimerEnabled: {state["TimerEnabled"]}\n" +
                          $"TimerExists: {state["TimerExists"]}\n" +
                          $"HasTags: {state["HasTags"]}\n" +
                          $"Final IsMonitoring: {state["IsMonitoring"]}\n\n" +
                          "=== HEALTH STATISTICS ===\n" +
                          $"IsConnectionHealthy: {health["IsConnectionHealthy"]}\n" +
                          $"TotalBoolTags: {health["TotalBoolTags"]}\n" +
                          $"TotalDintTags: {health["TotalDintTags"]}\n" +
                          $"TagsWithErrors: {health["TagsWithErrors"]}\n" +
                          $"ConsecutiveGlobalErrors: {health["ConsecutiveGlobalErrors"]}\n" +
                          $"LastGlobalError: {health["LastGlobalError"]}";

            MessageBox.Show(message, "PLC Monitoring Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void showDiagnosticsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowMonitoringDiagnostics();
        }

        private void fullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleFullScreen();
        }

        private void editAlertMessagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var editorForm = new FrmAlertEditor(_alertManager))
            {
                editorForm.ShowDialog(this);
            }
        }
    }
}
