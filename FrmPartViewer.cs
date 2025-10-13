using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using XisCoreSensors.Controls;
using XisCoreSensors.PLC;

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
        private Size _originalSensorSize = new Size(55,55);
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
        private enum SequenceState { Idle, PreZoomPause, ZoomedIn, PausedBetweenSensors }
        private int _sequenceIndex = -1;
        private SequenceState _currentSequenceState = SequenceState.Idle;

        private bool _hasUnsavedChanges = false;
        private string _currentLayoutPath = null;

        private readonly Stopwatch _stopwatch = new Stopwatch();        

        private List<string> _imagePaths = new List<string>();
        private List<ImageInfo> _loadedImages = new List<ImageInfo>();
        private string _imageSelectorTag = "";

        public int CurrentSequenceStep { get; private set; } = -1; // Valor inicial indefinido.

        public string ImageSelectorTag => _imageSelectorTag;

        public FrmPartViewer()
        {
            InitializeComponent();
            InitalizeSequenceTimer();
        }

        // Método público para que el formulario principal pueda actualizar este valor.
        public void UpdateSequenceStep(int newStep)
        {
            CurrentSequenceStep = newStep;
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

            if(MdiParent is FrmMainMDI parentForm)
            {
                if (_isEditMode) parentForm.PausePlcMonitoring();
                else parentForm.ResumePlcMonitoring();
            }

            return _isEditMode;
        }

        public void LoadNewImage()
        {
            /*if (!CheckForUnsavedChanges()) return;
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
                Text += " Modfied";
                // Forzamos un redibujado
                picCanvas.Invalidate();
            }*/
            using (var imageManager = new FrmImageManager(_imagePaths, _imageSelectorTag))
            {
                if (imageManager.ShowDialog() == DialogResult.OK)
                {
                    // Actualiza los datos del FrmPartViewer con los cambios del diálogo
                    _imagePaths = imageManager.ImagePaths;
                    _imageSelectorTag = Properties.Settings.Default.ImageTagName;
                    SaveLayout();
                    LoadLayout();
                    
                    //_sensors.Clear(); // Limpia la lista de sensores
                    //_relativeSensorLocations.Clear(); // Limpia el diccionario de posiciones
                    //_nextSensorNumber = 1; // Reinicia el contador de IDs
                    //_currentLayoutPath = null; // Resetea la ruta del layout actual
                    _hasUnsavedChanges = true;
                    picCanvas.Invalidate();
                }
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
                    ImagePaths = _imagePaths,
                    ImageSelectorTag = _imageSelectorTag,
                    Sensors = new List<SensorData>()
                };

                foreach (var sensor in _sensors)
                {
                    layoutData.Sensors.Add(new SensorData
                    {
                        Id = sensor.SensorId,
                        RelativeX = _relativeSensorLocations[sensor.SensorId].X,
                        RelativeY = _relativeSensorLocations[sensor.SensorId].Y,
                        PlcTag = sensor.PlcTag,
                        Status = sensor.Status,
                        Type = sensor.Type
                    });
                }

                if(MdiParent is FrmMainMDI mainForm && mainForm.TagMapper != null)
                {
                    layoutData.TagMappings.AddRange(mainForm.TagMapper.GetAllMappings());
                }

                string json = JsonConvert.SerializeObject(layoutData, Formatting.Indented);
                File.WriteAllText(path, json);
                _currentLayoutPath = path;         // <-- Actualiza la ruta actual
                _hasUnsavedChanges = false;        // <-- Resetea la bandera de cambios
                Text += " Saved";
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
                SaveLayout();
            }
        }

        private void LoadLayoutFinal(string path)
        {
            string json = File.ReadAllText(path);
            var layoutData = JsonConvert.DeserializeObject<LayoutData>(json);

            _imagePaths = layoutData.ImagePaths ?? new List<string>();
            _imageSelectorTag = layoutData.ImageSelectorTag ?? "";
            _loadedImages.Clear();

            foreach(var imagePath in _imagePaths)
            {                
                if (File.Exists(imagePath))
                {
                    var nuevaImagen = new ImageInfo();
                    nuevaImagen.ImagenCargada = Image.FromFile(imagePath);
                    nuevaImagen.RutaDelArchivo = imagePath;
                    _loadedImages.Add(nuevaImagen);
                }                    
            }

            if(_loadedImages.Any())
            {
                var primeraImagen = _loadedImages.First();
                picCanvas.Image = primeraImagen.ImagenCargada;
                picCanvas.Tag = primeraImagen;
                picCanvas.SizeMode = PictureBoxSizeMode.Zoom;
            }

            _currentLayoutPath = path;
            

            // Limpiamos el estado actual.
            picCanvas.Controls.Clear();
            _sensors.Clear();
            _relativeSensorLocations.Clear();

            if (MdiParent is FrmMainMDI mainForm && mainForm.TagMapper != null && layoutData.TagMappings != null)
            {
                mainForm.TagMapper.LoadMappings(layoutData.TagMappings);
            }

            // Creamos los sensores desde los datos cargados.
            // Nota: NO cargamos la imagen todavía.
            foreach (var sensorData in layoutData.Sensors)
            {
                CreateSensor(sensorData.Id, new PointF(sensorData.RelativeX, sensorData.RelativeY), sensorData.Type, sensorData.Status);
                var sensor = _sensors.LastOrDefault();
                if (sensor != null && !string.IsNullOrEmpty(sensorData.PlcTag))
                {
                    sensor.PlcTag = sensorData.PlcTag; 
                }
            }

            // Cargamos la imagen AHORA. Esto disparará automáticamente el evento 'Resize' del panel.
            /*if (File.Exists(layoutData.ImagePath))
            {
                var img = Image.FromFile(layoutData.ImagePath);
                picCanvas.Tag = layoutData.ImagePath;
                picCanvas.Image = img; // Esta acción provocará el reposicionamiento.
                picCanvas.SizeMode = PictureBoxSizeMode.Zoom;
            }*/
            RepositionAllSensors();
            _nextSensorNumber = _sensors.Count + 1; // Actualizamos el contador.
            Text = System.IO.Path.GetFileName(_currentLayoutPath); // Actualizamos el título del formulario.

            var boolTags = layoutData.Sensors
                .Select(s => s.PlcTag)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .ToList();

            if (MdiParent is FrmMainMDI parentForm)
            {
                parentForm.StartMonitoringForLayout(boolTags);
            }

        }

        private void CreateSensor(string id, PointF relativePos, SensorControl.SensorType type, SensorControl.SensorStatus status = SensorControl.SensorStatus.Ok)
        {
            var sensor = new SensorControl { SensorId = id, Tag = id, Status = status, Type = type };
            sensor.MouseDown += Sensor_MouseDown;
            sensor.MouseMove += Sensor_MouseMove;
            sensor.MouseUp += Sensor_MouseUp;
            sensor.StatusChanged += Sensor_StatusChanged;
            sensor.ContextMenuStrip = contextMenuSensor;
            sensor.RotateText = Properties.Settings.Default.RotateSensorText;
            _sensors.Add(sensor);
            picCanvas.Controls.Add(sensor);
            _relativeSensorLocations[id] = relativePos;
            sensor.BringToFront();
        }

        private void FrmPartViewer_Load(object sender, EventArgs e)
        {
            clockTimer_Tick(sender, e);
            // Ajustamos el tamaño inicial del lienzo al del viewport
            picCanvas.Size = pnlViewport.ClientSize;
            picCanvas.Resize += picCanvas_Resize;

            if(Properties.Settings.Default.LastLayoutPath != null &&
               File.Exists(Properties.Settings.Default.LastLayoutPath))
            {
                LoadLayoutFinal(Properties.Settings.Default.LastLayoutPath);
            }

            _isEditMode = false;

            lblClock.Parent = picCanvas;

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
            /* int sensorIndex = -1;

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
             }*/
            // --- LÓGICA PARA EL ATAJO DE TECLADO ---
            if (e.Control && e.KeyCode == Keys.A)
            {
                // Suprime el sonido "ding" de Windows y evita que el evento se propague.
                e.SuppressKeyPress = true;
                e.Handled = true;

                // Calcula una posición por defecto: el centro de la vista actual.
                Point centerOfView = new Point(
                    pnlViewport.ClientSize.Width / 2,
                    pnlViewport.ClientSize.Height / 2
                );

                // Convierte las coordenadas del panel a las del lienzo, teniendo en cuenta el scroll.
                Point defaultLocation = new Point(
                    centerOfView.X - pnlViewport.AutoScrollPosition.X,
                    centerOfView.Y - pnlViewport.AutoScrollPosition.Y
                );

                // Llama a nuestro método central con la posición por defecto.
                AddSensorAtLocation(defaultLocation, SensorControl.SensorType.Normal);
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
                Text += " Modfied";
                _draggedSensor = null;
            }
        }

        private void agregarSensorToolTipMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void AddSensorAtLocation(Point location, SensorControl.SensorType type)
        {
            if (!_isEditMode)
            {
                MessageBox.Show("You must be in Edit Mode to add sensors.", "Edit Mode Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string newId = $"S{_nextSensorNumber:D2}";
            _nextSensorNumber++;

            var sensor = new SensorControl
            {
                SensorId = newId,
                Tag = newId,
                Location = location, 
                Type = type,
                RotateText = Properties.Settings.Default.RotateSensorText
            };

            // Asigna los eventos y el menú contextual
            sensor.ContextMenuStrip = this.contextMenuSensor;
            sensor.MouseDown += Sensor_MouseDown;
            sensor.MouseMove += Sensor_MouseMove;
            sensor.MouseUp += Sensor_MouseUp;
            sensor.StatusChanged += Sensor_StatusChanged;

            // Añade el sensor a las colecciones y a la pantalla
            _sensors.Add(sensor);
            picCanvas.Controls.Add(sensor);

            // Calcula y guarda su posición relativa
            RectangleF imageRect = CalculateImageRectangle(picCanvas);
            if (imageRect.Width > 0 && imageRect.Height > 0)
            {
                float relativeX = (sensor.Left - imageRect.Left) / imageRect.Width;
                float relativeY = (sensor.Top - imageRect.Top) / imageRect.Height;
                _relativeSensorLocations[newId] = new PointF(relativeX, relativeY);
            }

            MarkAsModified(); // Marca que hay cambios sin guardar
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

                var result = MessageBox.Show(
                    $"Are you sure you want to delete sensor '{sensorToDelete.SensorId}'?", 
                    "Confirm Deletion",                                                  
                    MessageBoxButtons.YesNo,                                             
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No) return;

                // 3. Eliminamos el sensor de todas nuestras colecciones.
                _sensors.Remove(sensorToDelete);
                _relativeSensorLocations.Remove(sensorToDelete.SensorId);

                // 4. Eliminamos el control visual del panel.
                picCanvas.Controls.Remove(sensorToDelete);

                // 5. Liberamos sus recursos.
                sensorToDelete.Dispose();
                _hasUnsavedChanges = true;
                Text += " Modified";
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
                            MessageBox.Show("Id can not be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Verificamos si el ID ya existe (ignorando el sensor actual).
                        if (_sensors.Any(s => s.SensorId.Equals(newId, StringComparison.OrdinalIgnoreCase) && s != sensorToRename))
                        {
                            MessageBox.Show($"Id '{newId}' in use for another sensor. Please, try again...", "Duplicated id", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                        Text += " Modfied";
                    }
                }
            }
        }

        private void ProcessNextSequence()
        {
            _sequenceTimer.Stop();

            if(CurrentSequenceStep == 0 || _isEditMode) 
            {
                StopSequence();
                return;
            }

            if (_failedSensorsList.Count == 0)
            {
                StopSequence();
                return;
            }

            switch (_currentSequenceState)
            {
                // 1. Inicia el ciclo. Se asegura de que la vista esté alejada y espera 5 segundos.
                case SequenceState.Idle:
                    _sequenceIndex++;
                    if (_sequenceIndex >= _failedSensorsList.Count)
                    {
                        _sequenceIndex = 0; // Reiniciamos el índice si llegamos al final de la lista.
                    }
                    ResetZoom(); // Nos aseguramos de que la imagen esté completa.
                    _currentSequenceState = SequenceState.PreZoomPause;
                    _sequenceTimer.Interval = Properties.Settings.Default.FullViewDuration;
                    _sequenceTimer.Start();
                    break;

                // 2. Después de 5s, hace zoom al sensor fallado y espera 3 segundos.
                case SequenceState.PreZoomPause:
                    var sensorToFocus = _failedSensorsList[_sequenceIndex];
                    ZoomToSensor(sensorToFocus);
                    OnSensorFailed?.Invoke($"¡Falla detectada! Sensor: {sensorToFocus.SensorId}");
                    _currentSequenceState = SequenceState.ZoomedIn;
                    _sequenceTimer.Interval = Properties.Settings.Default.ZoomInDuration;
                    _sequenceTimer.Start();
                    break;

                // 3. Después de 3s, se aleja de nuevo y espera 5 segundos antes de pasar al siguiente sensor.
                case SequenceState.ZoomedIn:
                    ResetZoom();
                    _currentSequenceState = SequenceState.PausedBetweenSensors;
                    _sequenceTimer.Interval = Properties.Settings.Default.FullViewDuration;
                    _sequenceTimer.Start();
                    break;

                // 4. Inicia la transición rápida al siguiente sensor en la lista.
                case SequenceState.PausedBetweenSensors:
                    _currentSequenceState = SequenceState.Idle;
                    _sequenceTimer.Interval = 10; // Transición rápida para volver a empezar el ciclo.
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
            if(_isEditMode)
            {
                var message = "Modo: LOCKED";
                EditModeChanged?.Invoke(this, new EditModeChangedEventArgs
                {
                    StatusMessage = message,
                    IsInEditMode = !_isEditMode
                });
            }


            if (!CheckForUnsavedChanges())
            {
                e.Cancel = true; // Cancela el cierre del formulario
            }
        }

        public List<SensorControl> GetSensors()
        {
            return _sensors?.ToList() ?? new List<SensorControl>();
        }

        public void MarkAsModified()
        {
            if(!_hasUnsavedChanges)
            {
                _hasUnsavedChanges = true;
                if(!Text.Contains(" Modfied"))
                {
                    Text += " Modfied";
                }
            }
            
        }

        public void UpdateSensorState(string sensorId, bool isFailed)
        {
            // Busca el control del sensor en nuestra lista interna.
            var sensorControl = _sensors.FirstOrDefault(s => s.SensorId.Equals(sensorId, StringComparison.OrdinalIgnoreCase));

            if (sensorControl != null)
            {
                if(string.IsNullOrEmpty(sensorControl.PlcTag))
                {
                    sensorControl.Status = SensorControl.SensorStatus.Unmapped;
                }
                // Actualiza la propiedad 'Status', lo que cambiará su color y comportamiento.
                sensorControl.Status = isFailed ? SensorControl.SensorStatus.Fail : SensorControl.SensorStatus.Ok;
            }
        }        

        public void SetSensorsPausedVisualState(bool isPaused)
        {
            foreach (var sensor in _sensors)
            {
                if (isPaused)
                {
                    // Si pausamos, todos los sensores se ponen en amarillo.
                    sensor.Status = SensorControl.SensorStatus.Paused;
                }
                else
                {
                    // Si reanudamos, los sensores vuelven a su estado 'Unmapped' o 'Ok'
                    // y el PlcService se encargará de ponerlos en rojo/verde en el siguiente ciclo.
                    if (string.IsNullOrEmpty(sensor.PlcTag))
                    {
                        sensor.Status = SensorControl.SensorStatus.Unmapped;
                    }
                    else
                    {
                        sensor.Status = SensorControl.SensorStatus.Ok;
                    }
                }
            }
        }

        public void SwitchBackgroundImage(int index)
        {
            if(InvokeRequired)
            {
                Invoke(new Action(() => SwitchBackgroundImage(index)));
                return; 
            }

            if(index >= 0 && index < _loadedImages.Count)
            {
                picCanvas.Image = _loadedImages[index].ImagenCargada;
            }
        }

        private void normalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var _clickPosition = picCanvas.PointToClient(contextMenu.SourceControl.PointToScreen(contextMenu.Bounds.Location));
            AddSensorAtLocation(_clickPosition, SensorControl.SensorType.Normal);
        }

        private void laserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var _clickPosition = picCanvas.PointToClient(contextMenu.SourceControl.PointToScreen(contextMenu.Bounds.Location));
            AddSensorAtLocation(_clickPosition, SensorControl.SensorType.Laser);

        }

        private void contextMenuSensor_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var editMode = _isEditMode;

            toNormalToolStripMenuItem.Enabled = editMode;
            toLaserToolStripMenuItem.Enabled = editMode;

            if(editMode && contextMenuSensor.SourceControl is SensorControl sensor)
            {
                toNormalToolStripMenuItem.Enabled = (sensor.Type != SensorControl.SensorType.Normal);
                toLaserToolStripMenuItem.Enabled = (sensor.Type != SensorControl.SensorType.Laser);
            }
            

        }

        private void toNormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(contextMenuSensor.SourceControl is SensorControl sensor)
            {
                sensor.Type = SensorControl.SensorType.Normal;
                sensor.Invalidate();
                MarkAsModified();
            }
        }

        private void toLaserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(contextMenuSensor.SourceControl is SensorControl sensor)
            {
                sensor.Type = SensorControl.SensorType.Laser;
                sensor.Invalidate();
                MarkAsModified();
            }
        }

        private void clockTimer_Tick(object sender, EventArgs e)
        {
            lblClock.Text = DateTime.Now.ToString("hh:mm:ss tt");
        }

        public void ShowAlertMessage(string message)
        {
            // Asegura que la actualización se haga en el hilo de la UI.
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowAlertMessage(message)));
                return;
            }

            if (string.IsNullOrEmpty(message))
            {
                lblMessage.Visible = false;
            }
            else
            {
                lblMessage.Text = message;
                lblMessage.Visible = true;
                // Nos aseguramos de que la etiqueta siempre esté por encima del panel del visor.
                lblMessage.BringToFront();
            }
        }

        public void ControlStopWatch(PlcController.StopWatchCommand command)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ControlStopWatch(command)));
                return;
            }
            switch (command)
            {
                case PlcController.StopWatchCommand.Start:
                    _stopwatch.Start();
                    stopwatchTimer.Start();
                    break;
                case PlcController.StopWatchCommand.Pause:
                    _stopwatch.Stop();
                    stopwatchTimer.Stop();
                    break;
                case PlcController.StopWatchCommand.Reset:
                    _stopwatch.Reset();
                    stopwatchTimer.Stop();
                    // Actualiza la UI inmediatamente al resetear.
                    lblStopWatch.Text = "00:00";
                    break;
            }
        }

        private void stopwatchTimer_Tick(object sender, EventArgs e)
        {
            lblStopWatch.Text = _stopwatch.Elapsed.ToString(@"mm\:ss");
        }

        private void pnlViewport_Paint(object sender, PaintEventArgs e)
        {

        }

        private void lblClock_Paint(object sender, PaintEventArgs e)
        {
            lblClock.Invalidate();
        }
    }

    public class ImageInfo
    {
        public Image ImagenCargada { get; set; }
        public string RutaDelArchivo { get; set; }
    }


    public class EditModeChangedEventArgs : EventArgs
    {
        public string StatusMessage { get; set; }
        public bool IsInEditMode { get; set; }
    }

}
