using System;
using System.Drawing;
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
                this.lblMessage.ForeColor = Color.Yellow;
            }
            else
            {
                this.lblMessage.ForeColor = Color.Orange;
            }
            _isTextColorToggled = !_isTextColorToggled; // Invertimos el estado
        }

        public void ShowMessage(string message, int timeOutMs = 0)
        {
            lblMessage.Text = message;
            Visible = true;
            _timer.Stop();

            if(timeOutMs > 0)
            {
                _timer.Interval -= timeOutMs;
                _timer.Start();
                _blinkTimer.Start();
            }
            else {
                _blinkTimer.Stop();
            }               
            
        }

        public void HideMessage()
        {
            Visible = false;
            _timer.Stop();
            _blinkTimer.Stop();
        }
    }
}
