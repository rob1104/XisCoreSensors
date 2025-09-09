using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using XisCoreSensors.Controls;

namespace XisCoreSensors
{
    public partial class FrmPartViewer : Form
    {
        public event EventHandler<EditModeChangedEventArgs> EditModeChanged;
        //Variables de estado
        private List<SensorControl> _sensors = new List<SensorControl>();
        private Dictionary<string, PointF> _relativeSensorLocations = new Dictionary<string, PointF>();
        private bool _isZoomed = false;
        private const float ZoomFactor = 3f;
        private Size _originalSensorSize = new Size(42,42);
        // Variable para controlar si podemos mover/añadir sensores.
        private bool _isEditMode = false;
        // Variables para gestionar el arrastre de sensores.
        private bool _isDragging = false;
        private Point _dragStartPoint = Point.Empty;
        private SensorControl _draggedSensor = null;
        // Contador para generar IDs únicos de sensores.
        private int _nextSensorNumber = 1;
        public event Action<string> OnSensorFailed;


        //Secuencia de errores (Modo patrulla)
        private Timer _sequenceTimer;
        private List<SensorControl> _failedSensorsList = new List<SensorControl>();
        private enum SequenceState { Idle, ZoomedIn, PausedBetweenSensors }
        private int _sequenceIndex = -1;
        private SequenceState _currentSequenceState = SequenceState.Idle;

        private bool _hasUnsavedChanges = false;
        private string _currentLayoutPath = null;

        public FrmPartViewer()
        {
            InitializeComponent();
            InitalizeSequenceTimer();
        }

        private void InitalizeSequenceTimer()
        {
            _sequenceTimer = new Timer();
            _sequenceTimer.Tick += SequenceTimer_Tick;
        }

        private void SequenceTimer_Tick(object sender, EventArgs e)
        {
            ProcessNextSequence();
        }

        public bool ToggleEditMode()
        {
            _isEditMode = !_isEditMode;
            picCanvas.Cursor = _isEditMode ? Cursors.Cross : Cursors.Default;
            var message = _isEditMode ? "MODE: EDIT" : "Modo: LOCKED";
            EditModeChanged?.Invoke(this, new EditModeChangedEventArgs
            {
                StatusMessage = message,
                IsInEditMode = _isEditMode
            });
            return _isEditMode;
        }

        public void LoadNewImage()
        {
            if (!CheckForUnsavedChanges()) return;
            // Asegúrate de que tu OpenFileDialog se llame 'openFileDialog' en el diseñador del FrmPartViewer
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Cargamos la nueva imagen
                var img = Image.FromFile(openFileDialog.FileName);
                picCanvas.Image = img;
                picCanvas.SizeMode = PictureBoxSizeMode.Zoom;
                picCanvas.Tag = openFileDialog.FileName;
                // Limpiamos completamente el estado actual
                picCanvas.Controls.Clear(); // Elimina los controles de sensores
                _sensors.Clear(); // Limpia la lista de sensores
                _relativeSensorLocations.Clear(); // Limpia el diccionario de posiciones
                _nextSensorNumber = 1; // Reinicia el contador de IDs
                _currentLayoutPath = null; // Resetea la ruta del layout actual
                _hasUnsavedChanges = true;
                // Forzamos un redibujado
                picCanvas.Invalidate();
            }
        }

        public bool SaveLayout()
        {
            // Si no tenemos una ruta, esto es un "Guardar Como".
            if (string.IsNullOrEmpty(_currentLayoutPath))
            {
                return SaveLayoutAs();
            }
            else
            {
                // Si ya tenemos una ruta, simplemente sobrescribimos.
                return PerformSave(_currentLayoutPath);
            }
        }

        public bool SaveLayoutAs()
        {
            saveFileDialog.Filter = "Layout Files|*.json";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                return PerformSave(saveFileDialog.FileName);     
            return false;
        }

        private bool PerformSave(string path)
        {
            try
            {
                var layoutData = new LayoutData
                {
                    ImagePath = picCanvas.Tag?.ToString(), // Necesitamos guardar la ruta original
                    Sensors = new List<SensorData>()
                };

                foreach (var sensor in _sensors)
                {
                    layoutData.Sensors.Add(new SensorData
                    {
                        Id = sensor.SensorId,
                        RelativeX = _relativeSensorLocations[sensor.SensorId].X,
                        RelativeY = _relativeSensorLocations[sensor.SensorId].Y,
                        Status = sensor.Status
                    });
                }

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(layoutData, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(path, json);
                _currentLayoutPath = path;         // <-- Actualiza la ruta actual
                _hasUnsavedChanges = false;        // <-- Resetea la bandera de cambios
                Properties.Settings.Default.LastLayoutPath = path;
                Properties.Settings.Default.Save();
                return true; 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving layout: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false; 
            }
        }

        public void LoadLayout()
        {
            if (!CheckForUnsavedChanges()) return;
            openFileDialog.Filter = "Layout Files|*.json";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadLayoutFinal(openFileDialog.FileName);
            }
        }

        private void LoadLayoutFinal(string path)
        {
            string json = System.IO.File.ReadAllText(path);
            var layoutData = Newtonsoft.Json.JsonConvert.DeserializeObject<LayoutData>(json);

            _currentLayoutPath = path;
            

            // Limpiamos el estado actual.
            picCanvas.Controls.Clear();
            _sensors.Clear();
            _relativeSensorLocations.Clear();

            // Creamos los sensores desde los datos cargados.
            // Nota: NO cargamos la imagen todavía.
            foreach (var sensorData in layoutData.Sensors)
            {
                CreateSensor(sensorData.Id, new PointF(sensorData.RelativeX, sensorData.RelativeY), sensorData.Status);
            }

            // Cargamos la imagen AHORA. Esto disparará automáticamente el evento 'Resize' del panel.
            if (System.IO.File.Exists(layoutData.ImagePath))
            {
                var img = Image.FromFile(layoutData.ImagePath);
                picCanvas.Tag = layoutData.ImagePath;
                picCanvas.Image = img; // Esta acción provocará el reposicionamiento.
                picCanvas.SizeMode = PictureBoxSizeMode.Zoom;
            }
            RepositionAllSensors();
            _nextSensorNumber = _sensors.Count + 1; // Actualizamos el contador.
            Text = _currentLayoutPath; // Actualizamos el título del formulario.
        }

        private void CreateSensor(string id, PointF relativePos, SensorControl.SensorStatus status = SensorControl.SensorStatus.Ok)
        {
            var sensor = new SensorControl { SensorId = id, Tag = id, Status = status };
            sensor.MouseDown += Sensor_MouseDown;
            sensor.MouseMove += Sensor_MouseMove;
            sensor.MouseUp += Sensor_MouseUp;
            sensor.StatusChanged += Sensor_StatusChanged;
            sensor.ContextMenuStrip = this.contextMenuSensor;
            _sensors.Add(sensor);
            picCanvas.Controls.Add(sensor);
            _relativeSensorLocations[id] = relativePos;
            sensor.BringToFront();
        }

        private void FrmPartViewer_Load(object sender, EventArgs e)
        {
            // Ajustamos el tamaño inicial del lienzo al del viewport
            picCanvas.Size = pnlViewport.ClientSize;
            picCanvas.Resize += picCanvas_Resize;

            if(Properties.Settings.Default.LastLayoutPath != null &&
               System.IO.File.Exists(Properties.Settings.Default.LastLayoutPath))
            {
                LoadLayoutFinal(Properties.Settings.Default.LastLayoutPath);
            }

            _isEditMode = false;
        }       

        private void picCanvas_Resize(object sender, EventArgs e)
        {
            if (!_isZoomed)
            {
                // Esta condición evita un bucle infinito de eventos Resize
                if (picCanvas.Size != pnlViewport.ClientSize)
                {
                    picCanvas.Size = pnlViewport.ClientSize;
                }
            }
            RepositionAllSensors();            
        }

        private void ZoomToSensor(SensorControl sensorToFocus)
        {
            //if (_isZoomed) return;
            _isZoomed = true;

            // Factor de zoom más moderado para una transición menos brusca
            float smoothZoomFactor = ZoomFactor; // Reducido de 3.0f para ser menos agresivo

            // Calculamos el nuevo tamaño con una transición más suave
            int newWidth = (int)(pnlViewport.ClientSize.Width * smoothZoomFactor);
            int newHeight = (int)(pnlViewport.ClientSize.Height * smoothZoomFactor);

            // Aplicamos el nuevo tamaño
            picCanvas.Size = new Size(newWidth, newHeight);

            // Forzamos el repintado inmediato para que se vea más fluido
            picCanvas.Refresh();
            Application.DoEvents(); // Procesa eventos pendientes para suavizar la transición

            // Calculamos la posición del sensor en el lienzo agrandado
            RectangleF imageRectangle = CalculateImageRectangle(picCanvas);
            PointF relativePos = _relativeSensorLocations[sensorToFocus.SensorId];

            // Calculamos el centro del sensor
            int sensorCenterX = (int)(imageRectangle.Left + (relativePos.X * imageRectangle.Width));
            int sensorCenterY = (int)(imageRectangle.Top + (relativePos.Y * imageRectangle.Height));

            // Añadimos un pequeño offset para que el sensor no quede exactamente centrado
            // sino ligeramente desplazado, lo que da una sensación más natural
            int offsetX = _originalSensorSize.Width / 2;
            int offsetY = _originalSensorSize.Height / 2;

            // Calculamos la posición del scroll con suavizado
            int targetScrollX = sensorCenterX - (pnlViewport.ClientSize.Width / 2) - offsetX;
            int targetScrollY = sensorCenterY - (pnlViewport.ClientSize.Height / 2) - offsetY;

            // Limitamos los valores del scroll para evitar saltos bruscos
            targetScrollX = Math.Max(0, Math.Min(targetScrollX, picCanvas.Width - pnlViewport.ClientSize.Width));
            targetScrollY = Math.Max(0, Math.Min(targetScrollY, picCanvas.Height - pnlViewport.ClientSize.Height));

            // Aplicamos el scroll de forma progresiva en dos pasos para suavizar
            // Primero movemos a la mitad del camino
            int intermediateX = pnlViewport.AutoScrollPosition.X + (targetScrollX / 2);
            int intermediateY = pnlViewport.AutoScrollPosition.Y + (targetScrollY / 2);
            pnlViewport.AutoScrollPosition = new Point(intermediateX, intermediateY);
            Application.DoEvents();

            // Luego completamos el movimiento
            pnlViewport.AutoScrollPosition = new Point(targetScrollX, targetScrollY);

            // Resaltamos visualmente el sensor que tiene la falla
            //HighlightFailedSensor(sensorToFocus);
        }

        private void ResetZoom()
        {
            if (!_isZoomed) return;
            _isZoomed = false;

            // Guardamos la posición actual del scroll para hacer una transición suave
            Point currentScroll = new Point(
                Math.Abs(pnlViewport.AutoScrollPosition.X),
                Math.Abs(pnlViewport.AutoScrollPosition.Y)
            );

            // Primero reducimos el scroll a la mitad para suavizar la transición
            if (currentScroll.X > 0 || currentScroll.Y > 0)
            {
                pnlViewport.AutoScrollPosition = new Point(currentScroll.X / 2, currentScroll.Y / 2);
                Application.DoEvents();
            }

            // Reseteamos completamente el scroll
            pnlViewport.AutoScrollPosition = new Point(0, 0);

            // Restauramos el tamaño original del canvas de forma suave
            picCanvas.Size = pnlViewport.ClientSize;

            // Quitamos cualquier resaltado de sensores
           // RemoveSensorHighlights();

            // Forzamos el repintado para que se vea fluido
            picCanvas.Refresh();
        }

        private void Sensor_StatusChanged(object sender, EventArgs e)
        {
            var sensor = sender as SensorControl;
            if (sensor == null) return;

            lock(_failedSensorsList)
            {
                if (sensor.Status == SensorControl.SensorStatus.Fail && !_failedSensorsList.Contains(sensor))
                {
                    if(!_failedSensorsList.Contains(sensor))
                    {
                        _failedSensorsList.Add(sensor);
                    }
                }
                else if(sensor.Status == SensorControl.SensorStatus.Ok)
                {
                    _failedSensorsList.Remove(sensor);
                }
            }

            if (_failedSensorsList.Count > 0 && !_sequenceTimer.Enabled)
            {
                StartSequence();
            }
            else if(_failedSensorsList.Count == 0 && _sequenceTimer.Enabled)
            {
                StopSequence();
            }            
        }

        private void FrmPartViewer_KeyDown(object sender, KeyEventArgs e)
        {
            int sensorIndex = -1;

            // Convertimos la tecla presionada a un número.
            // D0-D9 son las teclas de la fila superior, NumPad0-NumPad9 son las del teclado numérico.
            if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
            {
                sensorIndex = e.KeyCode - Keys.D0; // D1 - D0 = 1, D2 - D0 = 2, etc.
            }
            else if (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9)
            {
                sensorIndex = e.KeyCode - Keys.NumPad0; // NumPad1 - NumPad0 = 1, etc.
            }

            // El índice 0 corresponde a la tecla '0', para acceder al décimo sensor (si existe).
            if (sensorIndex == 0) sensorIndex = 10;

            // Verificamos si el índice está dentro del rango de nuestra lista de sensores.
            // Restamos 1 porque las listas se basan en 0 (índice 0 es el primer elemento).
            if (sensorIndex > 0 && sensorIndex <= _sensors.Count)
            {
                // Accedemos al sensor por su posición en la lista.
                var sensor = _sensors[sensorIndex - 1];

                // Intercalamos su estado (OK <-> Fail).
                sensor.Status = (sensor.Status == SensorControl.SensorStatus.Ok)
                    ? SensorControl.SensorStatus.Fail
                    : SensorControl.SensorStatus.Ok;

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private RectangleF CalculateImageRectangle(PictureBox panel)
        {
            if (picCanvas.Image == null) return new RectangleF();

            var canvasSize = picCanvas.ClientSize;
            var imageSize = picCanvas.Image.Size;

            float canvasRatio = (float)canvasSize.Width / canvasSize.Height;
            float imageRatio = (float)imageSize.Width / imageSize.Height;

            float scale;
            float offsetX = 0;
            float offsetY = 0;

            if (canvasRatio > imageRatio) // El PictureBox es más ancho que la imagen (espacio a los lados)
            {
                scale = (float)canvasSize.Height / imageSize.Height;
                float scaledWidth = imageSize.Width * scale;
                offsetX = (canvasSize.Width - scaledWidth) / 2;
            }
            else // El PictureBox es más alto que la imagen (espacio arriba y abajo)
            {
                scale = (float)canvasSize.Width / imageSize.Width;
                float scaledHeight = imageSize.Height * scale;
                offsetY = (canvasSize.Height - scaledHeight) / 2;
            }

            return new RectangleF(offsetX, offsetY, imageSize.Width * scale, imageSize.Height * scale);
        }

        private void FrmPartViewer_Shown(object sender, EventArgs e)
        {         

            // El evento clave ahora es el Resize del lienzo
            picCanvas.Resize += picCanvas_Resize;

            picCanvas_Resize(picCanvas, EventArgs.Empty);
        }

        private void Sensor_MouseDown(object sender, MouseEventArgs e)
        {
            if(_isEditMode && e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _draggedSensor = sender as SensorControl;
                _dragStartPoint = e.Location;
                _draggedSensor.BringToFront();
            }
        }

        private void Sensor_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedSensor != null)
            {
                Point newLocation = new Point(
                    _draggedSensor.Left + (e.X - _dragStartPoint.X),
                    _draggedSensor.Top + (e.Y - _dragStartPoint.Y)
                );
                _draggedSensor.Location = newLocation;
            }
        }

        private void Sensor_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedSensor != null)
            {
                _isDragging = false;

                // --- AÑADIR ESTA COMPROBACIÓN ---
                // Si no hay imagen, no podemos calcular la nueva posición.
                if (picCanvas.Image == null)
                {
                    _draggedSensor = null;
                    return;
                }
                // --- FIN DE LA COMPROBACIÓN ---

                // El resto del código sigue igual...
                RectangleF imageRect = CalculateImageRectangle(picCanvas);

                // ...y podemos añadir otra seguridad aquí
                if (imageRect.Width == 0 || imageRect.Height == 0) return;

                float relativeX = (_draggedSensor.Left - imageRect.Left) / imageRect.Width;
                float relativeY = (_draggedSensor.Top - imageRect.Top) / imageRect.Height;

                var sensorId = _sensors.First(s => s == _draggedSensor).SensorId;
                _relativeSensorLocations[sensorId] = new PointF(relativeX, relativeY);
                _hasUnsavedChanges = true;
                _draggedSensor = null;
            }
        }

        private void agregarSensorToolTipMenuItem_Click(object sender, EventArgs e)
        {
            if(!_isEditMode)
            {
                MessageBox.Show("You must be in edit mode to add sensors.", "Edit Mode Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Obtenemos la posición donde se hizo clic derecho.
            Point clickPosition = picCanvas.PointToClient(contextMenu.SourceControl.PointToScreen(contextMenu.Bounds.Location));

            // Generamos un ID único.
            string newId = $"S{_nextSensorNumber:D2}";
            _nextSensorNumber++;

            // Creamos y configuramos el nuevo sensor.
            var sensor = new SensorControl
            {
                SensorId = newId, // Usamos string para el ID ahora
                Tag = newId,
                Location = clickPosition
            };

            // IMPORTANTE: Conectamos los eventos para que sea arrastrable.
            sensor.MouseDown += Sensor_MouseDown;
            sensor.MouseMove += Sensor_MouseMove;
            sensor.MouseUp += Sensor_MouseUp;

            // Lo añadimos a las colecciones.
            _sensors.Add(sensor);
            picCanvas.Controls.Add(sensor);

            // Calculamos y guardamos su posición relativa inicial.
            RectangleF imageRect = CalculateImageRectangle(picCanvas);
            float relativeX = (sensor.Left - imageRect.Left) / imageRect.Width;
            float relativeY = (sensor.Top - imageRect.Top) / imageRect.Height;
            _relativeSensorLocations[newId] = new PointF(relativeX, relativeY);
            _hasUnsavedChanges = true;
        }

        private void RepositionAllSensors()
        {
            if (_relativeSensorLocations.Count == 0 || picCanvas.Image == null) return;

            // 1. Obtenemos el rectángulo correcto de la imagen.
            RectangleF imageRectangle = CalculateImageRectangle(picCanvas);

            // Si el rectángulo calculado no tiene tamaño, no podemos continuar.
            if (imageRectangle.Width == 0 || imageRectangle.Height == 0) return;

            float currentScale = (float)picCanvas.Width / pnlViewport.ClientSize.Width;
            currentScale = Math.Max(1.0f, currentScale);
            int newSize = (int)(_originalSensorSize.Width * currentScale);
            Size scaledSensorSize = new Size(newSize, newSize);

            // 2. Recorremos los sensores y los posicionamos.
            foreach (var sensor in _sensors)
            {
                sensor.Size = scaledSensorSize;
                if (_relativeSensorLocations.ContainsKey(sensor.SensorId))
                {
                    PointF relativePos = _relativeSensorLocations[sensor.SensorId];
                    int newX = (int)(imageRectangle.Left + (relativePos.X * imageRectangle.Width));
                    int newY = (int)(imageRectangle.Top + (relativePos.Y * imageRectangle.Height));
                    sensor.Location = new Point(newX, newY);
                }
            }
        }

        private void eliminarSensorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 1. Solo permitimos borrar si estamos en Modo Edición.
            if (!_isEditMode)
            {
                MessageBox.Show("You must be in edit mode to delete sensors.", "Edit Mode Required", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // 2. Obtenemos el sensor sobre el que se hizo clic derecho.
            // 'SourceControl' nos dice qué control abrió el menú contextual.
            if (contextMenuSensor.SourceControl is SensorControl sensorToDelete)
            {
                // 3. Eliminamos el sensor de todas nuestras colecciones.
                _sensors.Remove(sensorToDelete);
                _relativeSensorLocations.Remove(sensorToDelete.SensorId);

                // 4. Eliminamos el control visual del panel.
                picCanvas.Controls.Remove(sensorToDelete);

                // 5. Liberamos sus recursos.
                sensorToDelete.Dispose();
                _hasUnsavedChanges = true;
            }
        }

        private void renombrarSensorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Solo permitimos renombrar en Modo Edición.
            if (!_isEditMode)
            {
                MessageBox.Show("You must be in edit mode to rename sensors.", "Edit Mode Required", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // Obtenemos el sensor sobre el que se hizo clic derecho.
            if (contextMenuSensor.SourceControl is SensorControl sensorToRename)
            {
                // 1. Abrimos nuestro formulario de renombrado como un diálogo.
                using (var renameForm = new FrmRenameSensor(sensorToRename.SensorId))
                {
                    // Si el usuario presiona "Aceptar"...
                    if (renameForm.ShowDialog() == DialogResult.OK)
                    {
                        string newId = renameForm.NewId;

                        // 2. --- VALIDACIONES ---
                        if (string.IsNullOrWhiteSpace(newId))
                        {
                            MessageBox.Show("El ID no puede estar vacío.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Verificamos si el ID ya existe (ignorando el sensor actual).
                        if (_sensors.Any(s => s.SensorId.Equals(newId, StringComparison.OrdinalIgnoreCase) && s != sensorToRename))
                        {
                            MessageBox.Show($"El ID '{newId}' ya está en uso. Por favor, elige otro.", "ID Duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // 3. --- APLICAMOS EL CAMBIO ---
                        // Actualizamos el diccionario de posiciones.
                        PointF position = _relativeSensorLocations[sensorToRename.SensorId];
                        _relativeSensorLocations.Remove(sensorToRename.SensorId);
                        _relativeSensorLocations[newId] = position;

                        // Actualizamos el ID en el propio control del sensor.
                        sensorToRename.SensorId = newId;
                        sensorToRename.Tag = newId; // También actualizamos el Tag por consistencia.
                        _hasUnsavedChanges = true;
                    }
                }
            }
        }

        private void ProcessNextSequence()
        {
            _sequenceTimer.Stop();

            if(_failedSensorsList.Count == 0)
            {
                StopSequence();
                return;
            }
            
            switch(_currentSequenceState)
            {
                case SequenceState.Idle:
                    _sequenceIndex++;
                    if(_sequenceIndex >= _failedSensorsList.Count)
                    {
                        _sequenceIndex = 0; // Reiniciamos el índice
                    }
                    var sensorToFocus = _failedSensorsList[_sequenceIndex];
                    ZoomToSensor(sensorToFocus);
                    OnSensorFailed?.Invoke($"¡Falla Detectada! Sensor: {sensorToFocus.SensorId}");
                    _currentSequenceState = SequenceState.ZoomedIn;
                    _sequenceTimer.Interval = 5000; // Tiempo en zoom
                    _sequenceTimer.Start();
                    break;
                case SequenceState.ZoomedIn:
                    ResetZoom();
                    _currentSequenceState = SequenceState.PausedBetweenSensors;
                    _sequenceTimer.Interval = 2000; // Pausa entre sensores
                    _sequenceTimer.Start();
                    break;
                case SequenceState.PausedBetweenSensors:
                    _currentSequenceState = SequenceState.Idle;
                    _sequenceTimer.Interval = 10; // Transición rápida
                    _sequenceTimer.Start();
                    break;
            }
        }
        private void StartSequence()
        {
            _currentSequenceState = SequenceState.Idle;
            _sequenceIndex = -1; // Para que empiece en el primer sensor (índice 0)
            _sequenceTimer.Interval = 10; // Iniciar inmediatamente
            _sequenceTimer.Start();
        }
        private void StopSequence()
        {
            _sequenceTimer.Stop();
            _currentSequenceState = SequenceState.Idle;
            ResetZoom();
        }

        private bool CheckForUnsavedChanges()
        {
            if (!_hasUnsavedChanges)
            {
                return true; // No hay cambios, se puede proceder.
            }

            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save them?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                return SaveLayout(); // Intenta guardar. Si el usuario cancela, SaveLayout devolverá false.
            }
            if (result == DialogResult.No)
            {
                return true; // Descartar cambios y proceder.
            }

            // Si es Cancel, no hagas nada.
            return false; // Cancelar la acción original (cerrar, cargar, etc.).
        }

        private void FrmPartViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!CheckForUnsavedChanges())
            {
                e.Cancel = true; // Cancela el cierre del formulario
            }
        }
    }

    public class LayoutData
    {
        public string ImagePath { get; set; }
        public List<SensorData> Sensors { get; set; } = new List<SensorData>();
    }

    public class SensorData
    {
        public String Id { get; set; }
        public float RelativeX { get; set; } // Posición X relativa (0 a 1)
        public float RelativeY { get; set; } // Posición Y relativa (0 a 1)
        public SensorControl.SensorStatus Status { get; set; } = SensorControl.SensorStatus.Ok;
    }

    public class EditModeChangedEventArgs : EventArgs
    {
        public string StatusMessage { get; set; }
        public bool IsInEditMode { get; set; }
    }


}
