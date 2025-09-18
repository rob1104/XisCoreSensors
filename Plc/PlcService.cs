using libplctag;
using libplctag.DataTypes;
using libplctag.DataTypes.Simple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using XisCoreSensors.Properties;

namespace XisCoreSensors.Plc
{
    public class PlcService : IDisposable
    {
        public event Action<string, bool> TagBoolChanged;
        public event Action<string> MonitoringError;
        public event Action<string, string> TagReadError;

        private readonly Dictionary<string, Tag<BoolPlcMapper, bool>> _tagsBool = new Dictionary<string, Tag<BoolPlcMapper, bool>>();
        private readonly Dictionary<string, bool?> _lastKnownBoolStates = new Dictionary<string, bool?>();
        private readonly Timer _pollingTimer;

        public bool IsMonitoring => _pollingTimer != null && _pollingTimer.Enabled;

        public PlcService()
        {
            _pollingTimer = new Timer { Interval = 500 };
            _pollingTimer.Tick += PollingTimer_Tick;
        }

        private async void PollingTimer_Tick(object sender, EventArgs e)
        {
            // Para evitar que se ejecute de nuevo si la lectura anterior aún no ha terminado.
            _pollingTimer.Stop();

            try
            {                
                foreach (var tagEntry in _tagsBool)
                {
                    var tagName = tagEntry.Key;
                    var tag = tagEntry.Value;

                    try
                    {
                        await tag.ReadAsync(); // Lee el valor actual del PLC
                        bool currentState = tag.Value;

                        _lastKnownBoolStates.TryGetValue(tagName, out bool? lastState);

                        // Si el estado ha cambiado, notifica a los suscriptores.
                        if (currentState != lastState)
                        {
                            TagBoolChanged?.Invoke(tagName, currentState);
                            _lastKnownBoolStates[tagName] = currentState;
                        }
                    }
                    catch (LibPlcTagException ex) when (ex.Status == Status.ErrorNotFound)
                    {
                        // ¡Atrapado! El tag no existe.
                        TagReadError?.Invoke(tagEntry.Key, "Tag not found in PLC");
                        RemoveBoolTag(tagEntry.Key); // Lo eliminamos para no volver a intentar
                    }
                    catch (Exception ex)
                    {
                        // Error general (timeout, etc.), detenemos todo y notificamos.
                        MonitoringError?.Invoke($"PLC connection lost: {ex.Message}");
                        return; // Detiene el bucle actual
                    }

                }
                // ... (Aquí podrías añadir bucles similares para leer DINTs y STRINGs) ...
            }
            catch (Exception ex)
            {
                // Si hay un error, lo puedes notificar o simplemente registrarlo.
                MonitoringError?.Invoke($"Error PLC connection: {ex.Message}");
            }
            
            _pollingTimer.Start();
            
        }

       
        public async Task<bool> TestConnectionAsync(string testTagName)
        {
            using (var testTag = new Tag<BoolPlcMapper, bool>
            {
                Name = testTagName, // Un tag que probablemente no existe
                Gateway = Settings.Default.PLC_IP,
                Path = Settings.Default.PLC_Path,
                PlcType = (PlcType)Settings.Default.PLC_Type,
                Protocol = (Protocol)Settings.Default.PLC_Protocol,
                Timeout = TimeSpan.FromSeconds(5) // Un timeout más corto para la prueba
            })
            {
                try
                {
                    await testTag.ReadAsync();                   
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public void InitializeBoolTags(IEnumerable<string> tagNames)
        {
            foreach (var tagName in tagNames)
            {
                if (!_tagsBool.ContainsKey(tagName))
                {
                    var tag = new Tag<BoolPlcMapper, bool>
                    {
                        Name = tagName,
                        Gateway = Settings.Default.PLC_IP,
                        Path = Settings.Default.PLC_Path,
                        PlcType = (PlcType)Settings.Default.PLC_Type,
                        Protocol = (Protocol)Settings.Default.PLC_Protocol,
                        Timeout = TimeSpan.FromSeconds(Settings.Default.PLC_Timeout)
                    };
                    _tagsBool.Add(tagName, tag);
                    _lastKnownBoolStates.Add(tagName, null);
                }
            }
        }
        public void StartMonitoring() => _pollingTimer.Start();
        public void StopMonitoring() => _pollingTimer.Stop();
        public void ClearTags()
        {
            // 1. Libera los recursos de cada tag individualmente antes de eliminarlos.
            // Esto es crucial para evitar fugas de memoria y conexiones "zombis".
            foreach (var tag in _tagsBool.Values)
            {
                tag.Dispose();
            }
          

            // 2. Limpia completamente los diccionarios que almacenan los tags.
            _tagsBool.Clear();
           

            // 3. Limpia también los diccionarios que guardan el último estado conocido.
            _lastKnownBoolStates.Clear();
            
        }

        public void RemoveBoolTag(string tagName)
        {
            if (_tagsBool.TryGetValue(tagName, out var tagToRemove))
            {
                tagToRemove.Dispose();
                _tagsBool.Remove(tagName);
                _lastKnownBoolStates.Remove(tagName);
            }
        }

        // --- Limpieza ---
        public void Dispose()
        {
            // Actualizamos para que limpie todos los diccionarios.
            _pollingTimer.Stop();
            _pollingTimer.Dispose();
            ClearTags();
        }

    }
}