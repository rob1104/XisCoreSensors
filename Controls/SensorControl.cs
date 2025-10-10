using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace XisCoreSensors.Controls
{
    public partial class SensorControl : UserControl
    {
        public bool RotateText { get; set; } = false;

        private string _sensorId;
        private string _plcTag;

        public enum SensorStatus { Ok, Fail, Unmapped, Paused }
        public enum SensorType { Normal, Laser }

        private SensorStatus _status = SensorStatus.Unmapped;

        public SensorType Type { get; set; } = SensorType.Normal;


        // --- INICIO: CÓDIGO NUEVO PARA PARPADEO ---
        private readonly Timer _flashTimer;
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
            Size = new Size(55, 55);
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

            // --- ROTACIÓN COMPLETA DEL SENSOR ---
            if (RotateText) // Reutilizamos la propiedad existente
            {
                var originalState = e.Graphics.Save();

                // Rotar 90 grados a la derecha (sentido horario) alrededor del centro
                e.Graphics.TranslateTransform(this.Width / 2f, this.Height / 2f);
                e.Graphics.RotateTransform(-90); // Cambiar a -90 para rotar a la izquierda
                e.Graphics.TranslateTransform(-this.Height / 2f, -this.Width / 2f);

                // Intercambiar dimensiones para que el dibujo se ajuste
                float w = Height; // Ahora el ancho es la altura
                float h = Width;  // Y la altura es el ancho

                PaintSensor(e.Graphics, w, h, false); // No establecer Region cuando está rotado

                e.Graphics.Restore(originalState);
            }
            else
            {
                PaintSensor(e.Graphics, Width, Height, true); // Establecer Region solo cuando NO está rotado
            }
        }

        private void PaintSensor(Graphics g, float width, float height, bool setRegion = true)
        {
            Color baseColor;
            Brush textColor;

            switch (_status)
            {
                case SensorStatus.Ok:
                    baseColor = Color.LimeGreen;
                    textColor = Brushes.Red;
                    break;
                case SensorStatus.Fail:
                    baseColor = Color.FromArgb(_flashAlpha, Color.Red);
                    textColor = Brushes.Yellow;
                    break;
                case SensorStatus.Paused:
                    baseColor = Color.FromArgb(_flashAlpha, Color.Yellow);
                    textColor = Brushes.Black;
                    break;
                case SensorStatus.Unmapped:
                default:
                    baseColor = Color.DimGray;
                    textColor = Brushes.White;
                    break;
            }

            int margin = 3;
            Rectangle mainRect = new Rectangle(margin, margin, (int)width - margin * 2 - 1, (int)height - margin * 2 - 1);
            Rectangle outerRect = new Rectangle(0, 0, (int)width - 1, (int)height - 1);

            using (var path = new GraphicsPath())
            using (var mainPath = new GraphicsPath())
            {
                // Definir las formas según el tipo de sensor
                if (Type == SensorType.Normal)
                {
                    path.AddEllipse(outerRect);
                    mainPath.AddEllipse(mainRect);
                }
                else // SensorType.Laser
                {
                    // Triángulo exterior
                    var outerPoints = new PointF[]
                    {
                new PointF(width / 2f, 0),
                new PointF(0, height - 1),
                new PointF(width - 1, height - 1)
                    };
                    path.AddPolygon(outerPoints);

                    // Triángulo interior (main)
                    var mainPoints = new PointF[]
                    {
                new PointF(width / 2f, margin),
                new PointF(margin, height - margin - 1),
                new PointF(width - margin - 1, height - margin - 1)
                    };
                    mainPath.AddPolygon(mainPoints);
                }

                // Solo establecer el Region cuando NO está rotado
                if (setRegion)
                {
                    this.Region = new Region(path);
                }

                // --- BORDE EXTERIOR (Efecto 3D) ---

                // 1. Sombra exterior (borde oscuro)
                using (var shadowBrush = new SolidBrush(Color.FromArgb(120, Color.Black)))
                {
                    using (var shadowPath = new GraphicsPath())
                    {
                        if (Type == SensorType.Normal)
                        {
                            shadowPath.AddEllipse(1, 1, width - 1, height - 1);
                        }
                        else
                        {
                            var shadowPoints = new PointF[]
                            {
                        new PointF(width / 2f, 1),
                        new PointF(1, height - 1),
                        new PointF(width - 1, height - 1)
                            };
                            shadowPath.AddPolygon(shadowPoints);
                        }
                        g.FillPath(shadowBrush, shadowPath);
                    }
                }

                // 2. Borde metálico principal con gradiente
                using (var borderBrush = new LinearGradientBrush(
                    outerRect,
                    Color.FromArgb(180, 180, 180),
                    Color.FromArgb(80, 80, 80),
                    45f))
                {
                    g.FillPath(borderBrush, path);
                }

                // 3. Borde interior brillante (highlight)
                if (Type == SensorType.Normal)
                {
                    Rectangle innerBorderRect = new Rectangle(1, 1, (int)width - 3, (int)height - 3);
                    using (var innerBorderPen = new Pen(Color.FromArgb(150, Color.White), 1))
                    {
                        g.DrawEllipse(innerBorderPen, innerBorderRect);
                    }
                }
                else // Triángulo con borde amarillo más visible
                {
                    // Borde amarillo exterior del triángulo
                    var outerBorderPoints = new PointF[]
                    {
                new PointF(width / 2f, 1.5f),
                new PointF(1.5f, height - 1.5f),
                new PointF(width - 1.5f, height - 1.5f)
                    };
                    using (var yellowBorderPen = new Pen(Color.FromArgb(220, Color.Gold), 2.5f))
                    {
                        g.DrawPolygon(yellowBorderPen, outerBorderPoints);
                    }

                    // Línea interna brillante para más profundidad
                    var innerPoints = new PointF[]
                    {
                new PointF(width / 2f, 2.5f),
                new PointF(2.5f, height - 2.5f),
                new PointF(width - 2.5f, height - 2.5f)
                    };
                    using (var innerBorderPen = new Pen(Color.FromArgb(180, Color.Yellow), 1f))
                    {
                        g.DrawPolygon(innerBorderPen, innerPoints);
                    }
                }

                // --- SUPERFICIE PRINCIPAL DEL SENSOR ---
                using (var mainBrush = new SolidBrush(baseColor))
                {
                    g.FillPath(mainBrush, mainPath);
                }

                // --- EFECTOS DE ILUMINACIÓN ---

                // 1. Highlight principal (brillo superior)
                RectangleF highlightRect = new RectangleF(
                    mainRect.X + mainRect.Width * 0.15f,
                    mainRect.Y + mainRect.Height * 0.1f,
                    mainRect.Width * 0.7f,
                    mainRect.Height * 0.4f);

                Color highlightColor = Color.FromArgb(200, Color.White);
                using (var highlightPath = new GraphicsPath())
                {
                    if (Type == SensorType.Normal)
                    {
                        highlightPath.AddEllipse(highlightRect);
                    }
                    else
                    {
                        // Para triángulo, crear un área de highlight proporcional
                        var highlightPoints = new PointF[]
                        {
                    new PointF(width / 2f, mainRect.Y + mainRect.Height * 0.15f),
                    new PointF(mainRect.X + mainRect.Width * 0.2f, mainRect.Y + mainRect.Height * 0.5f),
                    new PointF(mainRect.X + mainRect.Width * 0.8f, mainRect.Y + mainRect.Height * 0.5f)
                        };
                        highlightPath.AddPolygon(highlightPoints);
                    }

                    using (var highlightBrush = new LinearGradientBrush(
                        highlightRect,
                        highlightColor,
                        Color.Transparent,
                        LinearGradientMode.Vertical))
                    {
                        g.FillPath(highlightBrush, highlightPath);
                    }
                }

                // 2. Sombra interior (parte inferior)
                RectangleF shadowRect = new RectangleF(
                    mainRect.X + mainRect.Width * 0.1f,
                    mainRect.Y + mainRect.Height * 0.6f,
                    mainRect.Width * 0.8f,
                    mainRect.Height * 0.3f);

                Color shadowColor = Color.FromArgb(60, Color.Black);
                using (var shadowPath = new GraphicsPath())
                {
                    if (Type == SensorType.Normal)
                    {
                        shadowPath.AddEllipse(shadowRect);
                    }
                    else
                    {
                        // Para triángulo, crear sombra en la base
                        var shadowPoints = new PointF[]
                        {
                    new PointF(mainRect.X + mainRect.Width * 0.3f, mainRect.Y + mainRect.Height * 0.7f),
                    new PointF(mainRect.X + mainRect.Width * 0.15f, mainRect.Bottom - margin),
                    new PointF(mainRect.Right - mainRect.Width * 0.15f, mainRect.Bottom - margin),
                    new PointF(mainRect.X + mainRect.Width * 0.7f, mainRect.Y + mainRect.Height * 0.7f)
                        };
                        shadowPath.AddPolygon(shadowPoints);
                    }

                    using (var shadowBrush = new LinearGradientBrush(
                        shadowRect,
                        Color.Transparent,
                        shadowColor,
                        LinearGradientMode.Vertical))
                    {
                        g.FillPath(shadowBrush, shadowPath);
                    }
                }

                // 3. Punto de luz central
                if (_status == SensorStatus.Ok || _status == SensorStatus.Fail)
                {
                    RectangleF centerLight = new RectangleF(
                        mainRect.X + mainRect.Width * 0.35f,
                        mainRect.Y + mainRect.Height * 0.25f,
                        mainRect.Width * 0.3f,
                        mainRect.Height * 0.3f);

                    Color centerColor = Color.FromArgb(100, Color.White);

                    using (var centerPath = new GraphicsPath())
                    {
                        if (Type == SensorType.Normal)
                        {
                            centerPath.AddEllipse(centerLight);
                        }
                        else
                        {
                            // Pequeño triángulo de luz
                            var centerPoints = new PointF[]
                            {
                        new PointF(width / 2f, centerLight.Y),
                        new PointF(centerLight.X, centerLight.Bottom),
                        new PointF(centerLight.Right, centerLight.Bottom)
                            };
                            centerPath.AddPolygon(centerPoints);
                        }

                        using (var centerBrush = new PathGradientBrush(centerPath))
                        {
                            centerBrush.CenterColor = centerColor;
                            centerBrush.SurroundColors = new Color[] { Color.Transparent };
                            g.FillPath(centerBrush, centerPath);
                        }
                    }
                }
            }

            // --- TEXTO DEL SENSOR ---
            using (StringFormat stringFormat = new StringFormat())
            {
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                if (!string.IsNullOrEmpty(this.SensorId))
                {
                    float fontSize;

                    if (Type == SensorType.Normal)
                    {
                        fontSize = height * 0.4f; // Tamaño de fuente para sensor normal
                    }
                    else
                    {
                        fontSize = height * 0.25f; // Tamaño de fuente más pequeño para triángulo
                    }

                    using (var font = new Font("Arial", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                    {
                        // Ajustar posición del texto para triángulo
                        var textRect = new RectangleF(0, 0, width, height);
                        if (Type == SensorType.Laser)
                        {
                            // Mover el texto un poco hacia abajo en el triángulo
                            textRect.Y += height * 0.15f;
                        }

                        // Sombra del texto
                        using (var shadowBrush = new SolidBrush(Color.FromArgb(150, Color.Black)))
                        {
                            var shadowRect = textRect;
                            shadowRect.Offset(1, 1);
                            g.DrawString(this.SensorId, font, shadowBrush, shadowRect, stringFormat);
                        }
                        g.DrawString(this.SensorId, font, textColor, textRect, stringFormat);
                    }
                }

                // Indicador de unmapped
                if (_status == SensorStatus.Unmapped)
                {
                    var indicatorSize = 8;
                    var x = (int)width - indicatorSize - 6;
                    var y = 6;

                    using (var brush = new SolidBrush(Color.Red))
                    {
                        g.FillEllipse(brush, x, y, indicatorSize, indicatorSize);
                    }
                    using (var pen = new Pen(Color.White, 2))
                    {
                        g.DrawEllipse(pen, x, y, indicatorSize, indicatorSize);
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