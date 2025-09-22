using libplctag;
using libplctag.DataTypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using XisCoreSensors.Properties;

namespace XisCoreSensors.Plc
{
    public class PlcService : IDisposable
    {
        public event Action<string, bool> TagBoolChanged;
        public event Action<string, int> TagDintChanged;
        public event Action<string> MonitoringError;
        public event Action<string, string> TagReadError;

        private readonly Dictionary<string, Tag<BoolPlcMapper, bool>> _tagsBool = new Dictionary<string, Tag<BoolPlcMapper, bool>>();
        private readonly Dictionary<string, bool?> _lastKnownBoolStates = new Dictionary<string, bool?>();
        private readonly Dictionary<string, Tag<DintPlcMapper, int>> _tagsDint = new Dictionary<string, Tag<DintPlcMapper, int>>();
        private readonly Dictionary<string, int?> _lastKnownDintStates = new Dictionary<string, int?>(); //

        private readonly Timer _pollingTimer;
        public bool IsMonitoring => _pollingTimer != null && _pollingTimer.Enabled;

        private bool _isBoolMonitoringPaused = false;

        public PlcService()
        {
            _pollingTimer = new Timer { Interval = 300 };
            _pollingTimer.Tick += PollingTimer_Tick;
        }

        private async void PollingTimer_Tick(object sender, EventArgs e)
        {            
            _pollingTimer.Stop();           
            try
            {
                if (!_isBoolMonitoringPaused)
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
                }

                foreach(var tagEntry in _tagsDint)
                {
                    try
                    {
                        await tagEntry.Value.ReadAsync();
                        int currentState = tagEntry.Value.Value;
                        _lastKnownDintStates.TryGetValue(tagEntry.Key, out int? lastState);

                        if(currentState != lastState)
                        {
                            TagDintChanged?.Invoke(tagEntry.Key, currentState);
                            _lastKnownDintStates[tagEntry.Key] = currentState;
                        }
                    }
                    catch (Exception exception)
                    {
                        MonitoringError?.Invoke($"PLC DINT Read Error: {exception.Message}");
                        return;
                    }
                }                
                

            }
            catch (Exception ex)
            {
                // Si hay un error, lo puedes notificar o simplemente registrarlo.
                MonitoringError?.Invoke($"Error PLC connection: {ex.Message}");
            }

            _pollingTimer.Start();
        }

        public void PauseBoolMonitoring(bool pause)
        {
            _isBoolMonitoringPaused = pause;

            // Opcional: Si se reanuda, resetea el estado de los sensores a OK
            if (!pause)
            {
                foreach (var tagName in _tagsBool.Keys)
                {
                    _lastKnownBoolStates[tagName] = null; // Forza una re-lectura
                }
            }
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
        public void InitializeDintTags(IEnumerable<string> tagNames)
        {
            foreach (var tagName in tagNames)
            {
                if(!_tagsDint.ContainsKey(tagName))
                {
                    var tag = new Tag<DintPlcMapper, int>
                    {
                        Name = tagName,
                        Gateway = Settings.Default.PLC_IP,
                        Path = Settings.Default.PLC_Path,
                        PlcType = (PlcType)Settings.Default.PLC_Type,
                        Protocol = (Protocol)Settings.Default.PLC_Protocol,
                        Timeout = TimeSpan.FromSeconds(Settings.Default.PLC_Timeout)
                    };
                    _tagsDint.Add(tagName, tag);
                    _lastKnownDintStates.Add(tagName, null);
                }
            }
        }
        public void StartMonitoring() => _pollingTimer.Start();
        public void StopMonitoring() => _pollingTimer.Stop();
        public void ClearTags()
        {           
            foreach (var tag in _tagsBool.Values) tag.Dispose();
            foreach (var tag in _tagsDint.Values) tag.Dispose();
            _tagsBool.Clear();
            _tagsDint.Clear();         
            _lastKnownBoolStates.Clear();
            _lastKnownDintStates.Clear();
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
        public void Dispose()
        {
            // Actualizamos para que limpie todos los diccionarios.
            _pollingTimer.Stop();
            _pollingTimer.Dispose();
            ClearTags();
        }

    }
}