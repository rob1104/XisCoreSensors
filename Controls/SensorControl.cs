using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace XisCoreSensors.Controls
{
    public partial class SensorControl : UserControl
    {
        private string _sensorId;
        private string _plcTag;

        public enum SensorStatus { Ok, Fail, Unmapped, Paused }
        private SensorStatus _status = SensorStatus.Unmapped;

        // --- INICIO: CÓDIGO NUEVO PARA PARPADEO ---
        private Timer _flashTimer;
        // Controla la intensidad actual del color (255 es opaco, valores menores son más tenues)
        private int _flashAlpha = 255;
        private bool _isFadingOut = true; // Controla si estamos atenuando o abrillantando
        // --- FIN: CÓDIGO NUEVO ---

        public string SensorId
        {
            get => _sensorId;
            set
            {
                _sensorId = value;
                Invalidate(); // Redibuja el control cuando cambia el ID
            }
        }

        public string PlcTag
        {
            get => _plcTag;
            set
            {
                _plcTag = value;
                if (!string.IsNullOrEmpty(value) && _status == SensorStatus.Unmapped)
                {
                    Status = SensorStatus.Ok; // Cambia el estado a OK si se asigna una etiqueta PLC
                }
                else if (string.IsNullOrEmpty(value) && _status != SensorStatus.Fail)
                {
                    Status = SensorStatus.Unmapped;
                }
            }
        }

        public event EventHandler StatusChanged;

        public SensorStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    StatusChanged?.Invoke(this, EventArgs.Empty);

                    // Si el sensor falla, inicia el parpadeo.
                    if (_status == SensorStatus.Fail)
                    {
                        _flashTimer.Start();
                    }
                    else // Si el sensor está OK, detiene el parpadeo y resetea el color.
                    {
                        _flashTimer.Stop();
                        _flashAlpha = 255; // Resetea la intensidad al máximo
                    }
                    Invalidate(); // Fuerza el redibujado
                }
            }
        }

        public SensorControl()
        {
            InitializeComponent();
            // Establecemos el tamaño por defecto y habilitamos el doble búfer para un dibujo más suave.
            Size = new Size(42, 42);
            DoubleBuffered = true;

            // --- INICIO: CÓDIGO NUEVO ---
            // Configuración del Timer para el efecto de parpadeo
            _flashTimer = new Timer();
            _flashTimer.Interval = 30; // Controla la velocidad del pulso (más bajo = más rápido)
            _flashTimer.Tick += FlashTimer_Tick;
            // --- FIN: CÓDIGO NUEVO ---
        }

        // --- MÉTODO NUEVO: El corazón del efecto de pulso ---
        private void FlashTimer_Tick(object sender, EventArgs e)
        {
            int step = 20; // El "salto" de intensidad en cada paso

            if (_isFadingOut)
            {
                _flashAlpha -= step; // Reduce la intensidad
                if (_flashAlpha <= 100) // Límite inferior (para que no desaparezca)
                {
                    _flashAlpha = 100;
                    _isFadingOut = false; // Cambia de dirección
                }
            }
            else // Aumentando intensidad
            {
                _flashAlpha += step; // Aumenta la intensidad
                if (_flashAlpha >= 255) // Límite superior (opaco)
                {
                    _flashAlpha = 255;
                    _isFadingOut = true; // Cambia de dirección
                }
            }
            Invalidate(); // Pide al control que se redibuje con la nueva intensidad
        }

        private void SensorControl_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Determina el color base según el estado
            Color baseColor;
            switch (_status)
            {
                case SensorStatus.Ok:
                    baseColor = Color.LimeGreen;
                    break;
                case SensorStatus.Fail:
                    baseColor = Color.FromArgb(_flashAlpha, Color.Red);
                    break;
                case SensorStatus.Paused:
                    baseColor = Color.FromArgb(_flashAlpha, Color.Yellow);
                    break;
                case SensorStatus.Unmapped:
                default:
                    baseColor = Color.DimGray;
                    break;
            }

            // Crea la región circular
            using (var path = new GraphicsPath())
            {
                int margin = 3; // Margen aumentado para mejor definición del borde
                Rectangle mainRect = new Rectangle(margin, margin, this.Width - margin * 2 - 1, this.Height - margin * 2 - 1);
                Rectangle outerRect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

                path.AddEllipse(outerRect);
                this.Region = new Region(path);

                // --- BORDE EXTERIOR (Efecto 3D) ---

                // 1. Sombra exterior (borde oscuro)
                using (var shadowBrush = new SolidBrush(Color.FromArgb(120, Color.Black)))
                {
                    e.Graphics.FillEllipse(shadowBrush, 1, 1, this.Width - 1, this.Height - 1);
                }

                // 2. Borde metálico principal
                using (var borderPath = new GraphicsPath())
                {
                    borderPath.AddEllipse(outerRect);

                    // Gradiente para simular metal brushed
                    using (var borderBrush = new LinearGradientBrush(
                        outerRect,
                        Color.FromArgb(180, 180, 180), // Gris claro
                        Color.FromArgb(80, 80, 80),    // Gris oscuro
                        45f)) // Ángulo diagonal
                    {
                        e.Graphics.FillPath(borderBrush, borderPath);
                    }
                }

                // 3. Borde interior brillante (highlight)
                Rectangle innerBorderRect = new Rectangle(1, 1, this.Width - 3, this.Height - 3);
                using (var innerBorderPen = new Pen(Color.FromArgb(150, Color.White), 1))
                {
                    e.Graphics.DrawEllipse(innerBorderPen, innerBorderRect);
                }

                // --- SUPERFICIE PRINCIPAL DEL SENSOR ---

                // Relleno principal con el color del estado
                using (var mainBrush = new SolidBrush(baseColor))
                {
                    e.Graphics.FillEllipse(mainBrush, mainRect);
                }

                // --- EFECTOS DE ILUMINACIÓN ---

                // 1. Highlight principal (brillo superior)
                RectangleF highlightRect = new RectangleF(
                    mainRect.X + mainRect.Width * 0.15f,
                    mainRect.Y + mainRect.Height * 0.1f,
                    mainRect.Width * 0.7f,
                    mainRect.Height * 0.4f);

                Color highlightColor = Color.FromArgb(200, Color.White);
                using (var highlightBrush = new LinearGradientBrush(
                    highlightRect,
                    highlightColor,
                    Color.Transparent,
                    LinearGradientMode.Vertical))
                {
                    e.Graphics.FillEllipse(highlightBrush, highlightRect);
                }

                // 2. Sombra interior (parte inferior)
                RectangleF shadowRect = new RectangleF(
                    mainRect.X + mainRect.Width * 0.1f,
                    mainRect.Y + mainRect.Height * 0.6f,
                    mainRect.Width * 0.8f,
                    mainRect.Height * 0.3f);

                Color shadowColor = Color.FromArgb(60, Color.Black);
                using (var shadowBrush = new LinearGradientBrush(
                    shadowRect,
                    Color.Transparent,
                    shadowColor,
                    LinearGradientMode.Vertical))
                {
                    e.Graphics.FillEllipse(shadowBrush, shadowRect);
                }

                // 3. Punto de luz central (opcional, para efecto más dramático)
                if (_status == SensorStatus.Ok || _status == SensorStatus.Fail)
                {
                    RectangleF centerLight = new RectangleF(
                        mainRect.X + mainRect.Width * 0.35f,
                        mainRect.Y + mainRect.Height * 0.25f,
                        mainRect.Width * 0.3f,
                        mainRect.Height * 0.3f);

                    Color centerColor = Color.FromArgb(100, Color.White);
                    using (var centerBrush = new RadialGradientBrush(centerLight, centerColor, Color.Transparent))
                    {
                        e.Graphics.FillEllipse(centerBrush, centerLight);
                    }
                }
            }

            // --- TEXTO DEL SENSOR (sin cambios) ---
            using (StringFormat stringFormat = new StringFormat())
            {
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                if (!string.IsNullOrEmpty(this.SensorId))
                {
                    float fontSize = Math.Max(1, this.Height * 0.35f);
                    using (var font = new Font("Arial", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                    {
                        // Sombra del texto para mejor legibilidad
                        using (var shadowBrush = new SolidBrush(Color.FromArgb(150, Color.Black)))
                        {
                            var shadowRect = this.ClientRectangle;
                            shadowRect.Offset(1, 1);
                            e.Graphics.DrawString(this.SensorId, font, shadowBrush, shadowRect, stringFormat);
                        }

                        // Texto principal
                        e.Graphics.DrawString(this.SensorId, font, Brushes.White, this.ClientRectangle, stringFormat);
                    }
                }

                // Indicador de unmapped (sin cambios)
                if (_status == SensorStatus.Unmapped)
                {
                    int indicatorSize = 8;
                    int x = Width - indicatorSize - 6; // Ajustado para el nuevo borde
                    int y = 6;

                    using (var brush = new SolidBrush(Color.Red))
                    {
                        e.Graphics.FillEllipse(brush, x, y, indicatorSize, indicatorSize);
                    }
                    using (var pen = new Pen(Color.White, 2))
                    {
                        e.Graphics.DrawEllipse(pen, x, y, indicatorSize, indicatorSize);
                    }
                }
            }
        }
        public bool IsMapped => !string.IsNullOrEmpty(_plcTag);
    }
    public class RadialGradientBrush : IDisposable
    {
        private GraphicsPath _path;
        private PathGradientBrush _brush;

        public RadialGradientBrush(RectangleF rect, Color centerColor, Color edgeColor)
        {
            _path = new GraphicsPath();
            _path.AddEllipse(rect);
            _brush = new PathGradientBrush(_path);
            _brush.CenterColor = centerColor;
            _brush.SurroundColors = new Color[] { edgeColor };
        }

        public static implicit operator Brush(RadialGradientBrush rgb)
        {
            return rgb._brush;
        }

        public void Dispose()
        {
            _path?.Dispose();
            _brush?.Dispose();
        }
    }
}