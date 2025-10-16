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

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMainMDI());            
        } 
        
        public static void CheckForUpdates()
        {
            try
            {
                var updateUrl = "";

                #if RELEASE2                                
                            updateUrl = "http://xis.myftp.biz/desarrollos/XisCoreSensors_Build_2/update.xml";
                #else                           
                            updateUrl = "http://xis.myftp.biz/desarrollos/XisCoreSensors/update.xml";
                #endif

                AutoUpdater.InstalledVersion = new Version("1.1.0.12");
                AutoUpdater.ShowSkipButton = false;
                AutoUpdater.LetUserSelectRemindLater = false;
                AutoUpdater.RemindLaterTimeSpan = RemindLaterFormat.Minutes;
                AutoUpdater.RemindLaterAt = 15;
                AutoUpdater.Start(updateUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, 
                    Application.ProductName, 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
    }
}
