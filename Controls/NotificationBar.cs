using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XisCoreSensors.Controls
{
    public partial class NotificationBar : UserControl
    {
        private Timer _timer;

        private Timer _blinkTimer;
        private bool _isTextColorToggled = false;

        public NotificationBar()
        {
            InitializeComponent();
            Configure();
        }

        private void Configure()
        {
            Visible = false; // Inicialmente invisible
            Dock = DockStyle.Top; // Se ancla en la parte superior
            BackColor = Color.Red; // Color de fondo dorado
            Height = 120; // Altura fija
            //Temporizador
            _timer = new Timer();
            _timer.Interval = 5000; // 5 segundos
            _timer.Tick += (sender, e) => Visible = false; // Oculta la barra después del tiempo

            // Temporizador para cambiar el color del texto
            _blinkTimer = new Timer();
            _blinkTimer.Interval = 300; // Cambia cada 0.3 segundos
            _blinkTimer.Tick += BlinkTimer_Tick;
        }

        private void BlinkTimer_Tick(object sender, EventArgs e)
        {
            // Alternamos el color del texto entre Negro y Rojo oscuro para llamar la atención.
            if (_isTextColorToggled)
            {
                this.lblMessage.ForeColor = Color.Black;
            }
            else
            {
                this.lblMessage.ForeColor = Color.DarkRed;
            }
            _isTextColorToggled = !_isTextColorToggled; // Invertimos el estado
        }

        public void ShowMessage(string message)
        {
            lblMessage.Text = message; // Establece el mensaje
            Visible = true; // Muestra la barra
            _timer.Stop(); // Reinicia el temporizador
            _timer.Start();
            _blinkTimer.Stop();
            _blinkTimer.Start();
        }
    }
}
