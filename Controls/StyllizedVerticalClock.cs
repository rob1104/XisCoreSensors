using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace XisCoreSensors.Controls
{
    public partial class StyllizedVerticalClock : Control
    {
        private string _timeText = "00:00:00";
        private Timer _updateTimer;
        
        // Propiedades personalizables
        public Color PrimaryColor { get; set; } = Color.FromArgb(41, 128, 185); // Azul moderno
        public Color SecondaryColor { get; set; } = Color.FromArgb(52, 152, 219); // Azul claro
        public Color TextColor { get; set; } = Color.White;
        public Color GlowColor { get; set; } = Color.FromArgb(100, 52, 152, 219);
        public bool ShowGlow { get; set; } = true;
        public bool ShowGradient { get; set; } = true;
        public bool ShowBackground { get; set; } = true;
        public bool ShowBorder { get; set; } = true;
        public int CornerRadius { get; set; } = 15;
        public int GlowSize { get; set; } = 8;

        public StyllizedVerticalClock()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            this.DoubleBuffered = true;
            this.Font = new Font("Segoe UI", 32F, FontStyle.Bold);
            
            _updateTimer = new Timer();
            _updateTimer.Interval = 1000;
            _updateTimer.Tick += (s, e) => 
            {
                _timeText = DateTime.Now.ToString("hh:mm:ss tt");
                Invalidate();
            };
            _updateTimer.Start();
            
            _timeText = DateTime.Now.ToString("hh:mm:ss tt");
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Limpiar fondo
            g.Clear(this.Parent?.BackColor ?? Color.Transparent);

            // Crear el rectángulo
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            
            // Dibujar sombra/glow si está habilitado
            if (ShowGlow)
            {
                DrawGlow(g, bounds);
            }

            // Dibujar fondo con gradiente (solo si está habilitado)
            if (ShowBackground)
            {
                // Si CornerRadius es 0, usar rectángulos simples sin path
                if (CornerRadius == 0)
                {
                    if (ShowGradient)
                    {
                        using (LinearGradientBrush brush = new LinearGradientBrush(
                            bounds, PrimaryColor, SecondaryColor, 45f))
                        {
                            g.FillRectangle(brush, bounds);
                        }
                    }
                    else
                    {
                        using (SolidBrush brush = new SolidBrush(PrimaryColor))
                        {
                            g.FillRectangle(brush, bounds);
                        }
                    }

                    // Borde sutil (solo si está habilitado)
                    if (ShowBorder)
                    {
                        using (Pen pen = new Pen(Color.FromArgb(150, Color.White), 2))
                        {
                            g.DrawRectangle(pen, bounds);
                        }
                    }
                }
                else
                {
                    // Con esquinas redondeadas
                    using (GraphicsPath path = GetRoundedRect(bounds, CornerRadius))
                    {
                        if (ShowGradient)
                        {
                            using (LinearGradientBrush brush = new LinearGradientBrush(
                                bounds, PrimaryColor, SecondaryColor, 45f))
                            {
                                g.FillPath(brush, path);
                            }
                        }
                        else
                        {
                            using (SolidBrush brush = new SolidBrush(PrimaryColor))
                            {
                                g.FillPath(brush, path);
                            }
                        }

                        // Borde sutil (solo si está habilitado)
                        if (ShowBorder)
                        {
                            using (Pen pen = new Pen(Color.FromArgb(150, Color.White), 2))
                            {
                                g.DrawPath(pen, path);
                            }
                        }
                    }
                }

                // Efecto de brillo superior (solo si hay fondo)
                if (CornerRadius > 0)
                {
                    DrawHighlight(g, bounds);
                }
            }

            // Rotar y dibujar el texto
            DrawRotatedText(g);
        }

        private void DrawGlow(Graphics g, Rectangle bounds)
        {
            for (int i = GlowSize; i > 0; i--)
            {
                int alpha = (int)(30 * (1 - (float)i / GlowSize));
                Rectangle glowRect = new Rectangle(
                    bounds.X - i, 
                    bounds.Y - i, 
                    bounds.Width + (i * 2), 
                    bounds.Height + (i * 2));
                
                using (GraphicsPath path = GetRoundedRect(glowRect, CornerRadius + i))
                using (Pen pen = new Pen(Color.FromArgb(alpha, GlowColor), i * 2))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        private void DrawHighlight(Graphics g, Rectangle bounds)
        {
            Rectangle highlightRect = new Rectangle(
                bounds.X + 2, 
                bounds.Y + 2, 
                bounds.Width - 4, 
                bounds.Height / 3);
            
            using (GraphicsPath path = GetRoundedRect(highlightRect, CornerRadius - 2))
            using (LinearGradientBrush brush = new LinearGradientBrush(
                highlightRect, 
                Color.FromArgb(80, Color.White), 
                Color.FromArgb(10, Color.White), 
                LinearGradientMode.Vertical))
            {
                g.FillPath(brush, path);
            }
        }

        private void DrawRotatedText(Graphics g)
        {
            // Guardar estado
            GraphicsState state = g.Save();

            // Rotar el canvas
            g.TranslateTransform(0, Height);
            g.RotateTransform(-90);

            // Crear formato centrado
            StringFormat format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            RectangleF textRect = new RectangleF(0, 0, Height, Width);

            // Dibujar sombra del texto para mejor legibilidad
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(100, Color.Black)))
            {
                RectangleF shadowRect = textRect;
                shadowRect.Offset(2, 2);
                g.DrawString(_timeText, Font, shadowBrush, shadowRect, format);
            }

            // Dibujar texto principal
            using (SolidBrush textBrush = new SolidBrush(TextColor))
            {
                g.DrawString(_timeText, Font, textBrush, textRect, format);
            }

            // Restaurar estado
            g.Restore(state);
        }

        private GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // Esquinas redondeadas
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updateTimer?.Stop();
                _updateTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}