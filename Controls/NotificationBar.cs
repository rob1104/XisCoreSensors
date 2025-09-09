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
            _timer.Interval = 3000; // 3 segundos
            _timer.Tick += (sender, e) => Visible = false; // Oculta la barra después del tiempo
        }

        public void ShowMessage(string message)
        {
            lblMessage.Text = message; // Establece el mensaje
            Visible = true; // Muestra la barra
            _timer.Stop(); // Reinicia el temporizador
            _timer.Start();
        }
    }
}
