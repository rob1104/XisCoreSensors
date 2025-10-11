using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XisCoreSensors.Controls
{
    public class VerticalLabel : Label
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Usamos un pincel con el color de texto (ForeColor) del Label
            using (Brush textBrush = new SolidBrush(this.ForeColor))
            {
                // Limpiamos el fondo
                g.Clear(this.BackColor);

                // --- INICIO DE LA MODIFICACIÓN ---

                // 1. Creamos un objeto StringFormat para controlar la alineación.
                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,     // Centrado Horizontal
                    LineAlignment = StringAlignment.Center  // Centrado Vertical
                };

                // 2. Movemos el origen y rotamos el lienzo como antes.
                g.TranslateTransform(0, this.Height);
                g.RotateTransform(-90);

                // 3. Definimos el rectángulo de dibujo.
                //    Ojo: las dimensiones están invertidas a propósito debido a la rotación.
                //    El nuevo "ancho" es el Alto original del control, y viceversa.
                RectangleF drawingRect = new RectangleF(0, 0, this.Height, this.Width);

                // 4. Dibujamos el texto, pasándole el formato para que lo centre en el rectángulo.
                g.DrawString(this.Text, this.Font, textBrush, drawingRect, format);

                // --- FIN DE LA MODIFICACIÓN ---

                // Reseteamos la transformación.
                g.ResetTransform();
            }
        }
    }
}
