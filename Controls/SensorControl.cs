using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace XisCoreSensors.Controls
{
    public partial class SensorControl : UserControl
    {
        private string _sensorId;
        public enum SensorStatus { Ok, Fail }
        private SensorStatus _status = SensorStatus.Ok;

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

        public event EventHandler StatusChanged;

        public SensorStatus Status
        {
            get { return _status; }
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

            Color baseColor;
            // Si el sensor está en falla, usa el color rojo con la intensidad variable.
            if (_status == SensorStatus.Fail)
            {
                baseColor = Color.FromArgb(_flashAlpha, Color.Red);
            }
            else // Si está OK, usa el verde lima sólido.
            {
                baseColor = Color.LimeGreen;
            }

            Color highlightColor = Color.FromArgb(200, Color.White);

            using (var path = new GraphicsPath())
            {
                path.AddEllipse(0, 0, this.Width - 1, this.Height - 1);
                this.Region = new Region(path);

                using (var solidBrush = new SolidBrush(baseColor))
                {
                    e.Graphics.FillEllipse(solidBrush, 0, 0, this.Width - 1, this.Height - 1);
                }

                RectangleF highlightRect = new RectangleF(
                    this.Width * 0.15f, this.Height * 0.1f,
                    this.Width * 0.7f, this.Height * 0.4f);

                using (var highlightBrush = new LinearGradientBrush(highlightRect, highlightColor, Color.Transparent, LinearGradientMode.Vertical))
                {
                    e.Graphics.FillEllipse(highlightBrush, highlightRect);
                }
            }

            // El código para dibujar el texto no necesita cambios
            using (StringFormat stringFormat = new StringFormat())
            {
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;
                if (!string.IsNullOrEmpty(this.SensorId))
                {
                    float fontSize = Math.Max(1, this.Height * 0.35f);
                    using (var font = new Font("Arial", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                    {
                        e.Graphics.DrawString(this.SensorId, font, Brushes.White, this.ClientRectangle, stringFormat);
                    }
                }
            }
        }
    }
}