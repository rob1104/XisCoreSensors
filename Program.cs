using Microsoft.Win32;
using Squirrel;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using XisCoreSensors.Properties;

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

            if (Settings.Default.SettingsUpgradeRequired)
            {
                // 1. Migra la configuración en memoria.
                Settings.Default.Upgrade();
                // 2. Marca la migración como hecha y GUARDA INMEDIATAMENTE.
                Settings.Default.SettingsUpgradeRequired = false;
                Settings.Default.Save(); // <-- ¡Paso crucial! Ahora la config está a salvo.                
            }

            // 3. Ahora, intenta la operación secundaria que puede fallar.
            InicializarCatalogo();

            // squirrel --releasify  XisCoreSensors.X.X.X.nupkg



            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMainMDI());
        }

        public static void InicializarCatalogo()
        {
            try
            {
                string directorioActual = AppContext.BaseDirectory;
                var directorioPadre = Directory.GetParent(directorioActual)?.Parent;

                if (directorioPadre == null)
                {
                    // Opcional: Registrar que no se pudo encontrar el directorio padre.
                    return;
                }

                // 1. Busca todos los directorios que empiezan con "app-".
                var directoriosDeVersiones = directorioPadre.GetDirectories("app-*")
                    .Select(dir => {
                        // 2. Extrae el texto de la versión y intenta convertirlo.
                        string versionString = dir.Name.Substring("app-".Length);
                        Version.TryParse(versionString, out Version version); // Usa TryParse para evitar errores.
                        return new { Directory = dir, Version = version }; // Crea un objeto temporal con el directorio y su versión.
                    })
                    .Where(v => v.Version != null) // 3. Filtra cualquier directorio que no tenga una versión válida.
                    .OrderByDescending(v => v.Version) // 4. ¡LA CLAVE! Ordena por el objeto Version, no por el nombre.
                    .Select(v => v.Directory) // Vuelve a seleccionar solo el directorio.
                    .ToList();

                // 5. El resto de la lógica es la misma, pero ahora con la lista correctamente ordenada.
                if (directoriosDeVersiones.Count > 1)
                {
                    string directorioAnterior = directoriosDeVersiones[1].FullName;
                    string archivoOrigen = Path.Combine(directorioAnterior, "PlcCatalog.json");
                    string archivoDestino = Path.Combine(directorioActual, "PlcCatalog.json");

                    if (File.Exists(archivoOrigen) && !File.Exists(archivoDestino))
                    {
                        File.Copy(archivoOrigen, archivoDestino);
                    }
                }
            }
            catch (Exception ex)
            {
                // Para una aplicación real, considera usar un sistema de logs más formal
                // que escribir en la consola, como NLog o Serilog.
                System.Diagnostics.Debug.WriteLine("Error al copiar el catálogo: " + ex.Message);
            }
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

        private static void ManejarArchivoDeCatalogo()
        {
           
        }
    }
}
