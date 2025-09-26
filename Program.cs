using Microsoft.Win32;
using Squirrel;
using System;
using System.Diagnostics;
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

            if (args.Length > 0 && args[0].Contains("--squirrel"))
            {
                HandleSquirrelEvents(args);
                return;
            }

            // squirrel --releaseify  XisCoreSensors.X.X.X.exe

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMainMDI());
        }

        private static void HandleSquirrelEvents(string[] args)
        {
            var firstArg = args[0];
            switch (firstArg)
            {
                case "--squirrel-install":
                case "--squirrel-updated":
                    InstallRedistributableIfNeeded();
                    using (var mgr = new UpdateManager(""))
                    {
                        mgr.CreateShortcutsForExecutable("XisCoreSensors.exe",
                            ShortcutLocation.Desktop |
                            ShortcutLocation.StartMenu, false);
                    }
                    break;
                case "--squirrel-uninstall":
                    using (var mgr = new UpdateManager(""))
                    {
                        mgr.RemoveShortcutsForExecutable(
                            "XisCoreSensors.exe",
                            ShortcutLocation.StartMenu | ShortcutLocation.Desktop);
                    }
                    break;
            }
        }

        private static void InstallRedistributableIfNeeded()
        {
            // Comprueba en el registro de Windows si el redistribuible ya está instalado.
            if (IsVCRedistInstalled())
            {
                return; // Si ya está, no hacemos nada.
            }

            try
            {
                // Inicia el instalador del redistribuible en modo silencioso.
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "VC_redist.x86.exe",
                        // Argumentos para una instalación silenciosa y sin reinicio
                        Arguments = "/install /quiet /norestart",
                        UseShellExecute = true,
                        Verb = "runas" // Pide elevación de privilegios (UAC)
                    }
                };
                process.Start();
                process.WaitForExit(); // Espera a que termine la instalación
            }
            catch (Exception)
            {
                // Si falla, la aplicación principal probablemente no funcionará,
                // pero al menos la instalación base se completará.
            }
        }

        private static bool IsVCRedistInstalled()
        {
            // Ruta en el registro para la versión de 32 bits en un sistema de 64 bits
            string keyPath = @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x86";

            using (var key = Registry.LocalMachine.OpenSubKey(keyPath))
            {
                // Si la clave existe y el valor "Installed" es 1, entonces ya está instalado.
                if (key != null && (int)key.GetValue("Installed", 0) == 1)
                {
                    return true;
                }
            }
            // También comprueba la ruta para sistemas de 32 bits puros
            keyPath = @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86";
            using (var key = Registry.LocalMachine.OpenSubKey(keyPath))
            {
                if (key != null && (int)key.GetValue("Installed", 0) == 1)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
