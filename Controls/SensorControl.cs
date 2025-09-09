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
                    Invalidate(); // Redibuja el control cuando cambia el estado
                }
            }
        }

        public SensorControl()
        {
            InitializeComponent();
            // Establecemos el tamaño por defecto y habilitamos el doble búfer para un dibujo más suave.
            Size = new Size(42, 42);
            DoubleBuffered = true;
        }

        private void SensorControl_Paint(object sender, PaintEventArgs e)
        {
            // Usamos AntiAlias para que los bordes del círculo se vean suaves.
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Determina los colores basados en el estado del sensor.
            Color baseColor = (_status == SensorStatus.Ok) ? Color.LimeGreen : Color.Red;
            Color highlightColor = Color.FromArgb(200, Color.White); // Un blanco semitransparente para el brillo.

            // 2. Crea una ruta circular para darle forma al control.
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(0, 0, this.Width - 1, this.Height - 1);
                this.Region = new Region(path);

                // 3. Rellena el fondo con el color base.
                using (var solidBrush = new SolidBrush(baseColor))
                {
                    e.Graphics.FillEllipse(solidBrush, 0, 0, this.Width - 1, this.Height - 1);
                }

                // 4. Dibuja un efecto de brillo en la parte superior para dar un aspecto 3D.
                RectangleF highlightRect = new RectangleF(
                    this.Width * 0.15f,
                    this.Height * 0.1f,
                    this.Width * 0.7f,
                    this.Height * 0.4f
                );

                using (var highlightBrush = new LinearGradientBrush(highlightRect, highlightColor, Color.Transparent, LinearGradientMode.Vertical))
                {
                    e.Graphics.FillEllipse(highlightBrush, highlightRect);
                }
            }

            // 5. Dibuja el texto del SensorId, centrado y con un color que contraste.
            using (StringFormat stringFormat = new StringFormat())
            {
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                if (!string.IsNullOrEmpty(this.SensorId))
                {

                    var fontSize = Math.Max(1, Height * 0.35f); // Ajusta el tamaño de la fuente según el tamaño del control
                    // Creamos una nueva fuente con el tamaño calculado en píxeles.
                    using (var font = new Font("Arial", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                    {
                        e.Graphics.DrawString(
                            this.SensorId,
                            font,
                            Brushes.White,
                            this.ClientRectangle,
                            stringFormat
                        );
                    }
                }
            }
        }
    }
}