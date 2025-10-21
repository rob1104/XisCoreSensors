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
        private readonly AlertManager _alertManager;
        
        private bool _indicatorVisible = false;
        private bool _isInRecoveryMode = false;        
        private bool _isMonitoringActive = false;

        private Timer _monitoringIndicatorTimer;
        private Timer _reconnectTimer;
        private Timer _recoveryAttemptTimer;     

        private enum PlcUiState { Disconnected, Connected, Monitoring, 
            Paused, Error, Idle, Reconnecting, SequencePaused }             

        public TagMapper TagMapper { get; } = new TagMapper();

        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _currentTagErrors =
            new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

        public FrmMainMDI()
        {
            InitializeComponent();
            ApplyStartupMonitor();
            _alertManager = new AlertManager("alerts.json");
            ConfigureStatusStrip();
            ConfigureNotificationBar();
            ConfigureMonitoringTimer();
        }
       

        private void ConfigureMonitoringTimer()
        {
            _monitoringIndicatorTimer = new Timer();
            _monitoringIndicatorTimer.Interval = 1000;
            _monitoringIndicatorTimer.Tick += MonitoringIndicatorTimer_Tick;

            _reconnectTimer = new Timer();
            _reconnectTimer.Interval = 5000;
            _reconnectTimer.Tick += ReconnectTimer_Tick;

            _recoveryAttemptTimer = new Timer();
            _recoveryAttemptTimer.Interval = 15000;
            _recoveryAttemptTimer.Tick += RecoveryAttemptTimer_Tick;

        }

        private void PlcService_ConnectionStateChanged(bool isHealty)
        {
            if(InvokeRequired)
            {
                // Usamos Invoke síncrono para poner la llamada en el hilo de UI
                Invoke(new Action(() => PlcService_ConnectionStateChanged(isHealty)));
                return; 
            }

            // --- Estamos en el hilo de la UI ---

            if(isHealty)
            {                
                if (_isInRecoveryMode)
                {
                    _currentTagErrors.Clear(); // Limpiamos errores viejos
                    UpdatePlcStatus(PlcUiState.Reconnecting, "Connection recovered. Reinitializing...");
                    
                    _isInRecoveryMode = false;
                    _recoveryAttemptTimer.Stop();

                    // *** ¡LA SOLUCIÓN DEFINITIVA PARA VOLVER A VERDE! ***
                    // Reiniciamos el servicio completo para matar
                    // los contadores de error antiguos y los eventos "zombis".
                    // Usamos "_ = " para no bloquear el hilo actual.
                    _ = ReinitializePlcAsync();
                }
                else if (_plcService != null && _plcService.IsMonitoring)
                {
                    // Si la conexión estaba bien y sigue bien, solo actualiza
                    UpdatePlcStatus(PlcUiState.Monitoring);
                }
            }
            else // isHealthy == false (Conexión global perdida)
            {
                if(!_isInRecoveryMode)
                {
                    _isInRecoveryMode = true;
                    _recoveryAttemptTimer.Start();
                }
                UpdatePlcStatus(PlcUiState.Error, "Connection health issues detected.");
            }
        }

        private void PlcService_ConnectionRecovered(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => PlcService_ConnectionRecovered(message)));
                return;
            }
            UpdatePlcStatus(PlcUiState.Connected, message);
        }

        private async void RecoveryAttemptTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!_isInRecoveryMode)
                {
                    _recoveryAttemptTimer.Stop();
                    return;
                }

                UpdatePlcStatus(PlcUiState.Reconnecting, "Attempting automatic recovery...");
               
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
            // *** 1. ESTA ES LA CORRECCIÓN CLAVE ***
            // Ya no preguntamos a _plcService.IsMonitoring.
            // Solo comprobamos nuestro flag local.
            if (!_isMonitoringActive)
            {
                _monitoringIndicatorTimer.Stop();
                return;
            }

            // Si el servicio desaparece, es un ERROR.
            if (_plcService == null)
            {
                _monitoringIndicatorTimer.Stop(); // Detiene el timer
                UpdatePlcStatus(PlcUiState.Error, "PLC Service NULL"); // Llama a Error
                return; // UpdatePlcStatus ya puso _isMonitoringActive = false
            }

            // *** 2. ELIMINAMOS EL BLOQUE DEL BUG ***
            /* if (!_plcService.IsMonitoring)  // <-- ESTE BLOQUE SE ELIMINA
            {
                 _monitoringIndicatorTimer.Stop();
                 if (!lblPlcStatus.Text.Contains("Paused"))
                 {
                    UpdatePlcStatus(PlcUiState.Error, "Monitoring stopped");
                 }
                 return;
            }
            */

            // 1. Obtener el estado de salud en CADA tick
            var health = GetConnectionHealthState();
            // ... (el resto del método está perfecto y no cambia)
            var isHealthOK = (bool)health["IsHealthy"];
            var healthLabel = health["Label"].ToString();

            // 2. Actualizar lblConnectionHealth (el de la derecha)
            lblConnectionHealth.Text = health["Label"].ToString();
            lblConnectionHealth.BackColor = (Color)health["BackColor"];
            lblConnectionHealth.ForeColor = (Color)health["ForeColor"];
            lblConnectionHealth.ToolTipText = health["ToolTip"].ToString();

            // 3. Actualizar lblPlcStatus (el principal) con parpadeo
            if (isHealthOK)
            {
                lblPlcStatus.Text = _indicatorVisible ? "PLC: Monitoring" : "PLC: Monitoring ●";
                lblPlcStatus.BackColor = Color.Green;
                lblPlcStatus.ForeColor = Color.White;
            }
            else if (healthLabel.Contains("Partial"))
            {
                lblPlcStatus.Text = _indicatorVisible ? "PLC: Monitoring (Issues)" : "PLC: Monitoring (Issues) ⚠";
                lblPlcStatus.BackColor = Color.Orange;
                lblPlcStatus.ForeColor = Color.Black;
            }
            else
            {
                lblPlcStatus.Text = _indicatorVisible ? "PLC: Monitoring (ERROR)" : "PLC: Monitoring (ERROR) ✖";
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
            if (InvokeRequired)
            {
                Invoke(new Action(() => PlcController_SensorStateUpdateRequested(sensorId, isFailed)));             
                return;
            }

            var tagName = TagMapper.GetTagForSensor(sensorId);
            if (!string.IsNullOrEmpty(tagName))
            {
                if (!isFailed) // Si el sensor está OK
                {                    
                    _currentTagErrors.TryRemove(tagName, out _);
                }
            }
          
            foreach (var childForm in this.MdiChildren)
            {
               
                if (childForm is FrmPartViewer viewer)
                {                    
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
                Program.CheckForUpdates();
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

            if(e.IsInEditMode)
            {
                PausePlcMonitoring();
            }
            else
            {
                ResumePlcMonitoring();
            }
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
            try
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error opening Tag Mapper: " + 
                    ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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

            _currentTagErrors.Clear();

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
            _plcController.AlarmNumberChanged += PlcController_AlarmNumberChanged;

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

            var alarmTag = Settings.Default.AlarmTagName;
            if (!string.IsNullOrEmpty(alarmTag))
            {
                _plcService.InitializeDintTags(new[] { alarmTag });
            }
        }

        private void PlcController_StopwatchCommandReceived(StopWatchCommand command)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => PlcController_StopwatchCommandReceived(command)));
                return;
            }

            // ***** AÑADIR ESTA LÓGICA *****
            switch (command)
            {
                case StopWatchCommand.Start:
                    lblChronoStatus.Text = "CHRONO: RUN";
                    break;
                case StopWatchCommand.Pause:
                    lblChronoStatus.Text = "CHRONO: PAUSE";
                    break;
                case StopWatchCommand.Reset:
                    lblChronoStatus.Text = "CHRONO: RESET";
                    break;
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
            if (InvokeRequired)
            {
                Invoke(new Action(() => PlcController_SequenceStepChanged(newStep)));
                return;
            }

            lblSequenceStatus.Text = $"SEQ: {newStep}";

            // La lógica del mensaje se ha movido. Aquí solo actualizamos el estado de pausa.
            if (newStep == 0)
            {               
                PausePlcMonitoring();
                UpdatePlcStatus(PlcUiState.SequencePaused);
            }          
            else
            {
                ResumePlcMonitoring();
            }               
        }

        private void PlcController_AlarmNumberChanged(int alarmNumber)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => PlcController_AlarmNumberChanged(alarmNumber)));
                return;
            }

            lblAlarmStatus.Text = $"ALARM: {alarmNumber}";

            // Busca el mensaje correspondiente al número de alarma.
            var alertMessage = _alertManager.GetMessageForSequence(alarmNumber);

            // Envía el mensaje al visor activo para que lo muestre.
            if (ActiveMdiChild is FrmPartViewer activeViewer)
            {
                activeViewer.ShowAlertMessage(alertMessage?.ToUpper());
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

        public async void ResumePlcMonitoring()
        {
            if (_plcService == null) return;
            if (lblPlcStatus.Text.Contains("Idle")) return;


            var seqTag = Settings.Default.SequenceTagName;

            if(!string.IsNullOrEmpty(seqTag))
            {
                var (success, seqValue) = await _plcService.ReadDintTagAsync(seqTag);

                if(success && seqValue == 0)
                {
                    UpdatePlcStatus(PlcUiState.SequencePaused);

                    if (ActiveMdiChild is FrmPartViewer activeViewer)
                    {
                        if(activeViewer.CurrentSequenceStep == 0)
                        {
                            activeViewer.UpdateSequenceStep(0);                                  
                        }
                        return;
                    }
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
            _monitoringIndicatorTimer.Stop(); // Detener siempre por defecto
           
           // *** 1. RESETEAMOS EL FLAG ***
           _isMonitoringActive = false;      // Resetear flag por defecto

           _indicatorVisible = false; 
           lblPlcStatus.ForeColor = Color.White;
           lblConnectionHealth.ForeColor = Color.White; 

            switch (state)
            {
                case PlcUiState.Disconnected:
                    lblPlcStatus.Text = "PLC: Disconnected";
                    lblPlcStatus.BackColor = Color.Orange;
                    lblPlcStatus.ForeColor = Color.Black;
                    lblConnectionHealth.Text = "CONN: Disconnected";
                    lblConnectionHealth.BackColor = Color.Orange;
                    lblConnectionHealth.ForeColor = Color.Black;
                    break;
                
                case PlcUiState.Connected:
                    lblPlcStatus.Text = "PLC: Connected";
                    lblPlcStatus.BackColor = Color.Green;
                    lblConnectionHealth.Text = "CONN: Healthy";
                    lblConnectionHealth.BackColor = Color.Green;
                    goto case PlcUiState.Monitoring; 

                case PlcUiState.Monitoring:
                    // *** 2. PONEMOS EL FLAG EN TRUE ***
                    _isMonitoringActive = true;  // Le decimos a la UI que debe monitorear
                    _monitoringIndicatorTimer.Start(); 
                    MonitoringIndicatorTimer_Tick(null, EventArgs.Empty); 
                    break;

                case PlcUiState.Paused:
                    lblPlcStatus.Text = "PLC: Paused (Edit Mode)";
                    lblPlcStatus.BackColor = Color.Yellow;
                    lblPlcStatus.ForeColor = Color.Black;
                    lblConnectionHealth.Text = "CONN: Paused";
                    lblConnectionHealth.BackColor = Color.Yellow;
                    lblConnectionHealth.ForeColor = Color.Black;
                    break;

                case PlcUiState.Error:
                    lblPlcStatus.Text = string.IsNullOrEmpty(details) ? "PLC: ERROR" : $"PLC: {details}";
                    lblPlcStatus.BackColor = Color.Red;
                    lblConnectionHealth.Text = "CONN: Error";
                    lblConnectionHealth.BackColor = Color.Red;
                    break;

                case PlcUiState.Idle:
                    lblPlcStatus.Text = string.IsNullOrEmpty(details) ? "PLC: Idle" : $"PLC: {details}";
                    lblPlcStatus.BackColor = Color.SlateGray;
                    _plcService?.StopMonitoring();
                    lblConnectionHealth.Text = "CONN: Idle";
                    lblConnectionHealth.BackColor = Color.SlateGray;
                    break;

                case PlcUiState.Reconnecting:
                    lblPlcStatus.Text = string.IsNullOrEmpty(details) ? "PLC: Reconnecting..." : $"PLC: {details}";
                    lblPlcStatus.BackColor = Color.Orange;
                    lblPlcStatus.ForeColor = Color.Black;
                    lblConnectionHealth.Text = "CONN: Recovering...";
                    lblConnectionHealth.BackColor = Color.Yellow;
                    lblConnectionHealth.ForeColor = Color.Black;
                    break;

                case PlcUiState.SequencePaused:
                    lblPlcStatus.Text = "PLC: Paused by Sequence (Step 0)";
                    lblPlcStatus.BackColor = Color.Gold;
                    lblPlcStatus.ForeColor = Color.Black;
                    lblConnectionHealth.Text = "CONN: Paused (Seq)";
                    lblConnectionHealth.BackColor = Color.Gold;
                    lblConnectionHealth.ForeColor = Color.Black;
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
                return;
            }       
            _currentTagErrors[tagName] = message;          
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
            try
            {
                if (_plcService == null) return;

                UpdatePlcStatus(PlcUiState.Reconnecting, "Manual recovery attempt...");
                await ReinitializePlcAsync();
            }
            catch (Exception ex)
            {
                UpdatePlcStatus(PlcUiState.Error, $"Manual recovery failed: {ex.Message}");
            }
        }

        private Dictionary<string,object> GetConnectionHealthState()
        {
          var stats = new Dictionary<string, object>();

            if (_plcService == null)
            {
                stats["IsHealthy"] = false;
                stats["Label"] = "CONN: No service";
                stats["BackColor"] = Color.Gray;
                stats["ForeColor"] = Color.White;
                stats["ToolTip"] = "PLC Service is not initialized.";
                return stats;
            }

            var serviceStats = GetPlcHealthStatistics();
            var isServiceHealthy = (bool)serviceStats["IsConnectionHealthy"];
            var totalBoolTags = (int)serviceStats["TotalBoolTags"];
            var totalDintTags = (int)serviceStats["TotalDintTags"];
            // var tagsWithServiceErrors = (int)serviceStats["TagsWithErrors"]; // <-- ¡IGNORAMOS ESTO!
            var consecutiveGlobalErrors = (int)serviceStats["ConsecutiveGlobalErrors"];

            // *** ¡LA ÚNICA FUENTE DE VERDAD PARA ERRORES ES NUESTRA LISTA! ***
            var totalErrors = _currentTagErrors.Count; 
            var isFullyHealthy = isServiceHealthy && totalErrors == 0;

            string toolTip = $"Connection Health Status\n" +
                             $"Service Healthy: {isServiceHealthy}\n" +
                             $"Bool Tags: {totalBoolTags}\n" +
                             $"DINT Tags: {totalDintTags}\n" +
                             // "tagsWithServiceErrors" ya no se muestra
                             $"Tracked Tag Errors: {_currentTagErrors.Count}\n" + // Usamos nuestra lista
                             $"Global Errors: {consecutiveGlobalErrors}\n" +
                             $"Recovery Mode: {_isInRecoveryMode}";
            stats["ToolTip"] = toolTip;

            if (isFullyHealthy)
            {
                stats["IsHealthy"] = true; // Totalmente saludable
                stats["Label"] = $"CONN: Healthy ({totalBoolTags + totalDintTags} tags)";
                stats["BackColor"] = Color.Green;
                stats["ForeColor"] = Color.White;
            }
            else if (isServiceHealthy && totalErrors > 0)
            {
                // Conexión bien, pero nuestra lista tiene errores
                stats["IsHealthy"] = true; 
                stats["Label"] = $"CONN: Partial ({totalErrors} issues)";
                stats["BackColor"] = Color.Orange;
                stats["ForeColor"] = Color.Black;
            }
            else if (_isInRecoveryMode)
            {
                stats["IsHealthy"] = false;
                stats["Label"] = "CONN: Recovering...";
                stats["BackColor"] = Color.Yellow;
                stats["ForeColor"] = Color.Black;
            }
            else
            {
                stats["IsHealthy"] = false;
                stats["Label"] = $"CONN: Error ({consecutiveGlobalErrors})";
                stats["BackColor"] = Color.Red;
                stats["ForeColor"] = Color.White;
            }

            return stats;

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

        private void ApplyStartupMonitor()
        {
            try
            {
                var monitorIndex = Settings.Default.StartupMonitor;
                var screens = Screen.AllScreens;

                if (monitorIndex < 0 || monitorIndex >= screens.Length) return;

                var tarhetScreen = screens[monitorIndex];
                StartPosition = FormStartPosition.Manual;
                Location = tarhetScreen.Bounds.Location;
                WindowState = FormWindowState.Maximized;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Monitor settings could not be applied: {e.Message}", 
                    "Screen Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Warning);
            }
        }

        private void lblConnectionHealth_Click(object sender, EventArgs e)
        {
            if (_plcService == null)
            {
                MessageBox.Show("PLC Service is not initialized.", "Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var stats = GetPlcHealthStatistics();
            var isHealthy = (bool)stats["IsConnectionHealthy"];
            var tagsWithErrors = (int)stats["TagsWithErrors"];
            var globalErrors = (int)stats["ConsecutiveGlobalErrors"];

            // Copiamos la lista de errores para evitar problemas de concurrencia al leerla
            var currentErrors = _currentTagErrors.ToList();

            if (isHealthy && tagsWithErrors == 0 && globalErrors == 0 && currentErrors.Count == 0)
            {
                MessageBox.Show("Connection is healthy. No errors to report.", "PLC Connection Health", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var messageBuilder = new System.Text.StringBuilder();
            messageBuilder.AppendLine("PLC Connection Issue Report:");
            messageBuilder.AppendLine("------------------------------------");
            messageBuilder.AppendLine($"Overall Status: {(isHealthy ? "Partial" : "Error")}");
            messageBuilder.AppendLine($"Consecutive Global Errors: {globalErrors}");
            messageBuilder.AppendLine($"Tags with Read Errors (Snapshot): {currentErrors.Count}"); // Usamos nuestra lista
            messageBuilder.AppendLine();

            if (currentErrors.Count > 0)
            {
                messageBuilder.AppendLine("Failing Tags Details:");
                foreach (var errorEntry in currentErrors)
                {
                    messageBuilder.AppendLine($"- Tag: {errorEntry.Key}, Error: {errorEntry.Value}");
                }
            }
            else if (globalErrors > 0)
            {
                messageBuilder.AppendLine("Global connection errors are occurring.");
                messageBuilder.AppendLine($"Last Error: {stats["LastGlobalError"]}");
            }
            else if (tagsWithErrors > 0)
            {
                messageBuilder.AppendLine("Tags with errors are reported, but no specific details are currently logged.");
            }
            else
            {
                messageBuilder.AppendLine("An unknown connection issue is present.");
            }

            MessageBox.Show(messageBuilder.ToString(), "PLC Connection Health", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
