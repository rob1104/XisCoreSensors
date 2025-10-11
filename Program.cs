using AutoUpdaterDotNET;
using System;
using System.Windows.Forms;


namespace XisCoreSensors
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            AutoUpdater.InstalledVersion = new Version("1.1.0.4");
            AutoUpdater.ShowSkipButton = false;
            AutoUpdater.LetUserSelectRemindLater = false;
            AutoUpdater.RemindLaterTimeSpan = RemindLaterFormat.Minutes;
            AutoUpdater.RemindLaterAt = 15;
            AutoUpdater.Start("http://xis.myftp.biz/desarrollos/XisCoreSensors/update.xml");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMainMDI());
        }     
    
    }
}
