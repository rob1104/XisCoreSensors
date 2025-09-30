using libplctag;
using libplctag.DataTypes;
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
        // Eventos para notificar cambios y errores 
        public event Action<string, bool> TagBoolChanged;
        public event Action<string, int> TagDintChanged;
        public event Action<string> MonitoringError;
        public event Action<string, string> TagReadError;
        public event Action<bool> ConnectionStateChanged; 
        public event Action<string> ConnectionRecovered;

        // Diccionarios para manejar tags y sus estados
        private readonly Dictionary<string, Tag<BoolPlcMapper, bool>> _tagsBool = new Dictionary<string, Tag<BoolPlcMapper, bool>>();        
        private readonly Dictionary<string, Tag<DintPlcMapper, int>> _tagsDint = new Dictionary<string, Tag<DintPlcMapper, int>>();
        private readonly Dictionary<string, bool?> _lastKnownBoolStates = new Dictionary<string, bool?>();
        private readonly Dictionary<string, int?> _lastKnownDintStates = new Dictionary<string, int?>();

        // Sistema de salud de conexión
        private readonly Dictionary<string, int> _consecutiveErrors = new Dictionary<string, int>();
        private readonly Dictionary<string, DateTime> _lastSuccessfulRead = new Dictionary<string, DateTime>();
        private bool _isConnectionHealthy = true;
        private int _consecutiveGlobalErrors = 0;
        private DateTime _lastGlobalError = DateTime.MinValue;
        private readonly Timer _pollingTimer;
        private readonly Timer _healthCheckTimer; 

        // Constantes de configuración
        private const int MAX_CONSECUTIVE_ERRORS = 3;
        private const int MAX_GLOBAL_CONSECUTIVE_ERRORS = 5;
        private const int HEALTH_CHECK_INTERVAL = 10000; 
        private const int ERROR_RECOVERY_DELAY = 5000;
        private const int TAG_TIMEOUT_MINUTES = 2;

        // Estado de monitoreo
        private bool _isDisposed = false;
        private bool _shouldBeMonitoring = false;
        private bool _isActuallyMonitoring = false;
        private readonly object _monitoringStateLock = new object();

        // Propiedad para exponer el estado de salud de la conexión
        public bool IsConnectionHealthy => _isConnectionHealthy;
        private bool _isBoolMonitoringPaused = false;
        public bool IsMonitoring
        {
            get
            {
                lock (_monitoringStateLock)
                {
                    return !_isDisposed &&
                            _pollingTimer != null &&
                            _pollingTimer.Enabled &&
                            _shouldBeMonitoring &&
                            _isActuallyMonitoring;
                }
            }
        }
        public bool ShouldBeMonitoring
        {
            get
            {
                lock(_monitoringStateLock)
                {
                    return _shouldBeMonitoring && !_isDisposed;
                }
            }
        }
        public bool TimerIsEnabled => !_isDisposed && _pollingTimer?.Enabled == true;

        public bool IsBoolMonitoringPaused => _isBoolMonitoringPaused;
        public bool IsDintMonitoringActive => IsMonitoring;

        public PlcService()
        {
            _pollingTimer = new Timer { Interval = 300 };
            _pollingTimer.Tick += PollingTimer_Tick;
            _healthCheckTimer = new Timer { Interval = HEALTH_CHECK_INTERVAL };
            _healthCheckTimer.Tick += HealthCheckTimer_Tick;
            _healthCheckTimer.Start();
        }

        private void HealthCheckTimer_Tick(object sender, EventArgs e)
        {
            CheckConnectionHealth();
        }

        private void CheckConnectionHealth()
        {
            var currentTime = DateTime.Now;
            var problemTags = new List<string>();

            // Verificar salud de tags booleanos
            foreach (var tagName in _tagsBool.Keys)
            {
                if (_lastSuccessfulRead.TryGetValue(tagName, out var lastSuccess))
                {
                    if ((currentTime - lastSuccess).TotalMinutes > TAG_TIMEOUT_MINUTES)
                    {
                        problemTags.Add(tagName);
                    }
                }
            }

            // Verificar salud de tags DINT
            foreach (var tagName in _tagsDint.Keys)
            {
                if (_lastSuccessfulRead.TryGetValue(tagName, out var lastSuccess))
                {
                    if ((currentTime - lastSuccess).TotalMinutes > TAG_TIMEOUT_MINUTES)
                    {
                        problemTags.Add(tagName);
                    }
                }
            }

            // Si hay tags problemáticos y la conexión parecía saludable
            if (problemTags.Count > 0 && _isConnectionHealthy)
            {
                _isConnectionHealthy = false;
                ConnectionStateChanged?.Invoke(false);
                MonitoringError?.Invoke($"Connection issues detected with tags: {string.Join(", ", problemTags)}");
            }
        }

        private async void PollingTimer_Tick(object sender, EventArgs e)
        {
            _pollingTimer.Stop();

            lock (_monitoringStateLock)
            {
                if (!_shouldBeMonitoring || _isDisposed)
                {
                    _isActuallyMonitoring = false;
                    return;
                }

                _isActuallyMonitoring = true;
            }

            bool hasGlobalError = false;

            try
            {
                // CAMBIO CLAVE: Solo pausar el monitoreo de BOOL, no el de DINT
                if (!_isBoolMonitoringPaused)
                {
                    await ProcessBoolTags();
                }

                // SIEMPRE procesar tags DINT, incluso cuando el monitoreo BOOL está pausado
                // Esto permite detectar cambios de secuencia
                await ProcessDintTags();

                // Si llegamos aquí sin errores globales, resetear contador
                if (_consecutiveGlobalErrors > 0)
                {
                    _consecutiveGlobalErrors = 0;
                    if (!_isConnectionHealthy)
                    {
                        _isConnectionHealthy = true;
                        ConnectionStateChanged?.Invoke(true);
                        ConnectionRecovered?.Invoke("PLC connection recovered successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                hasGlobalError = true;
                HandleGlobalError(ex);

                lock (_monitoringStateLock)
                {
                    _isActuallyMonitoring = false;
                }
            }

            // Lógica de reinicio del timer
            lock (_monitoringStateLock)
            {
                if (_shouldBeMonitoring && !_isDisposed)
                {
                    if (!hasGlobalError || _consecutiveGlobalErrors < MAX_GLOBAL_CONSECUTIVE_ERRORS)
                    {
                        _pollingTimer.Start();
                    }
                    else
                    {
                        _isActuallyMonitoring = false;

                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(ERROR_RECOVERY_DELAY);

                            lock (_monitoringStateLock)
                            {
                                if (_shouldBeMonitoring && !_isDisposed)
                                {
                                    _consecutiveGlobalErrors = 0;
                                    _pollingTimer.Start();
                                }
                            }
                        });
                    }
                }
                else
                {
                    _isActuallyMonitoring = false;
                }
            }
        }


        private async Task ProcessBoolTags()
        {
            var tagsToRemove = new List<string>();

            foreach (var tagEntry in _tagsBool)
            {
                var tagName = tagEntry.Key;
                var tag = tagEntry.Value;

                try
                {
                    await tag.ReadAsync();
                    bool currentState = tag.Value;

                    // Marcar lectura exitosa
                    _lastSuccessfulRead[tagName] = DateTime.Now;
                    if (_consecutiveErrors.ContainsKey(tagName))
                    {
                        _consecutiveErrors[tagName] = 0;
                    }

                    _lastKnownBoolStates.TryGetValue(tagName, out bool? lastState);

                    if (currentState != lastState)
                    {
                        TagBoolChanged?.Invoke(tagName, currentState);
                        _lastKnownBoolStates[tagName] = currentState;
                    }
                }
                catch (LibPlcTagException ex) when (ex.Status == Status.ErrorNotFound)
                {
                    TagReadError?.Invoke(tagName, "Tag not found in PLC");
                    tagsToRemove.Add(tagName);
                }
                catch (Exception ex)
                {
                    HandleTagError(tagName, ex);

                    // Si un tag tiene demasiados errores consecutivos, considerar removerlo temporalmente
                    if (_consecutiveErrors.TryGetValue(tagName, out var errorCount) && errorCount >= MAX_CONSECUTIVE_ERRORS)
                    {
                        TagReadError?.Invoke(tagName, $"Tag removed due to consecutive errors: {ex.Message}");
                        tagsToRemove.Add(tagName);
                    }
                }
            }

            // Remover tags problemáticos
            foreach (var tagName in tagsToRemove)
            {
                RemoveBoolTag(tagName);
            }
        }

        private async Task ProcessDintTags()
        {
            var tagsToRemove = new List<string>();

            foreach (var tagEntry in _tagsDint)
            {
                var tagName = tagEntry.Key;
                var tag = tagEntry.Value;

                try
                {
                    await tag.ReadAsync();
                    int currentState = tag.Value;

                    // Marcar lectura exitosa
                    _lastSuccessfulRead[tagName] = DateTime.Now;
                    if (_consecutiveErrors.ContainsKey(tagName))
                    {
                        _consecutiveErrors[tagName] = 0;
                    }

                    _lastKnownDintStates.TryGetValue(tagName, out int? lastState);

                    if (currentState != lastState)
                    {
                        TagDintChanged?.Invoke(tagName, currentState);
                        _lastKnownDintStates[tagName] = currentState;
                    }
                }
                catch (Exception ex)
                {
                    HandleTagError(tagName, ex);

                    if (_consecutiveErrors.TryGetValue(tagName, out var errorCount) && errorCount >= MAX_CONSECUTIVE_ERRORS)
                    {
                        TagReadError?.Invoke(tagName, $"DINT Tag removed due to consecutive errors: {ex.Message}");
                        tagsToRemove.Add(tagName);
                    }
                }
            }

            // Remover tags problemáticos
            foreach (var tagName in tagsToRemove)
            {
                RemoveDintTag(tagName);
            }
        }

        private void HandleTagError(string tagName, Exception ex)
        {
            if (!_consecutiveErrors.ContainsKey(tagName))
            {
                _consecutiveErrors[tagName] = 0;
            }

            _consecutiveErrors[tagName]++;

            // Solo reportar error si es el primer error o cada 5 errores para evitar spam
            if (_consecutiveErrors[tagName] == 1 || _consecutiveErrors[tagName] % 5 == 0)
            {
                TagReadError?.Invoke(tagName, $"Read error (attempt {_consecutiveErrors[tagName]}): {ex.Message}");
            }
        }

        private void HandleGlobalError(Exception ex)
        {
            _consecutiveGlobalErrors++;
            _lastGlobalError = DateTime.Now;

            if (_isConnectionHealthy)
            {
                _isConnectionHealthy = false;
                ConnectionStateChanged?.Invoke(false);
            }

            MonitoringError?.Invoke($"Global PLC error (attempt {_consecutiveGlobalErrors}): {ex.Message}");
        }

        public void PauseBoolMonitoring(bool pause)
        {
            _isBoolMonitoringPaused = pause;

            if (!pause)
            {
                foreach (var tagName in _tagsBool.Keys)
                {
                    _lastKnownBoolStates[tagName] = null;
                }
            }
        }

        public async Task<bool> TestConnectionAsync(string testTagName)
        {
            if (string.IsNullOrEmpty(testTagName) || _isDisposed)
            {
                return false;
            }

            try
            {
                using (var testTag = new Tag<DintPlcMapper, int>
                       {
                           Name = testTagName,
                           Gateway = Settings.Default.PLC_IP,
                           Path = Settings.Default.PLC_Path,
                           PlcType = (PlcType)Settings.Default.PLC_Type,
                           Protocol = (Protocol)Settings.Default.PLC_Protocol,
                           Timeout = TimeSpan.FromSeconds(5)
                       })
                {
                    await testTag.ReadAsync();
                    return true;
                }
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void InitializeBoolTags(IEnumerable<string> tagNames)
        {
            foreach (var tagName in tagNames)
            {
                if (!_tagsBool.ContainsKey(tagName) && !string.IsNullOrEmpty(tagName))
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
                    _consecutiveErrors[tagName] = 0;
                    _lastSuccessfulRead[tagName] = DateTime.Now;
                }
            }
        }

        public void InitializeDintTags(IEnumerable<string> tagNames)
        {
            foreach (var tagName in tagNames)
            {
                if (!_tagsDint.ContainsKey(tagName) && !string.IsNullOrEmpty(tagName))
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
                    _consecutiveErrors[tagName] = 0;
                    _lastSuccessfulRead[tagName] = DateTime.Now;
                }
            }
        }

        public void StartMonitoring()
        {
            lock (_monitoringStateLock)
            {
                if (_isDisposed) return;

                _shouldBeMonitoring = true;

                if (_pollingTimer != null && (_tagsBool.Count > 0 || _tagsDint.Count > 0))
                {
                    _pollingTimer.Start();
                }
                else
                {
                    // Log: No hay tags para monitorear
                    MonitoringError?.Invoke("Cannot start monitoring: No tags configured");
                }
            }
        }

        public void StopMonitoring()
        {
            lock (_monitoringStateLock)
            {
                _shouldBeMonitoring = false;
                _isActuallyMonitoring = false;
                _consecutiveGlobalErrors = 0;
                _pollingTimer?.Stop();
            }
        }

        public void ClearTags()
        {
            foreach (var tag in _tagsBool.Values) tag.Dispose();
            foreach (var tag in _tagsDint.Values) tag.Dispose();

            _tagsBool.Clear();
            _tagsDint.Clear();
            _lastKnownBoolStates.Clear();
            _lastKnownDintStates.Clear();
            _consecutiveErrors.Clear();
            _lastSuccessfulRead.Clear();
        }

        public void RemoveBoolTag(string tagName)
        {
            if (_tagsBool.TryGetValue(tagName, out var tagToRemove))
            {
                tagToRemove.Dispose();
                _tagsBool.Remove(tagName);
                _lastKnownBoolStates.Remove(tagName);
                _consecutiveErrors.Remove(tagName);
                _lastSuccessfulRead.Remove(tagName);
            }
        }

        public void RemoveDintTag(string tagName)
        {
            if (_tagsDint.TryGetValue(tagName, out var tagToRemove))
            {
                tagToRemove.Dispose();
                _tagsDint.Remove(tagName);
                _lastKnownDintStates.Remove(tagName);
                _consecutiveErrors.Remove(tagName);
                _lastSuccessfulRead.Remove(tagName);
            }
        }
       
        public void RetryFailedTag(string tagName, bool isBoolTag = true)
        {
            if (isBoolTag)
            {
                InitializeBoolTags(new[] { tagName });
            }
            else
            {
                InitializeDintTags(new[] { tagName });
            }
        }
       
        public Dictionary<string, object> GetHealthStatistics()
        {
            return new Dictionary<string, object>
            {
                ["IsConnectionHealthy"] = _isConnectionHealthy,
                ["TotalBoolTags"] = _tagsBool.Count,
                ["TotalDintTags"] = _tagsDint.Count,
                ["ConsecutiveGlobalErrors"] = _consecutiveGlobalErrors,
                ["TagsWithErrors"] = _consecutiveErrors.Count(kv => kv.Value > 0),
                ["LastGlobalError"] = _lastGlobalError,
                ["IsBoolMonitoringPaused"] = _isBoolMonitoringPaused, 
                ["IsDintMonitoringActive"] = IsDintMonitoringActive 
            };
        }

        public Dictionary<string, object> GetMonitoringState()
        {
            lock (_monitoringStateLock)
            {
                return new Dictionary<string, object>
                {
                    ["IsDisposed"] = _isDisposed,
                    ["ShouldBeMonitoring"] = _shouldBeMonitoring,
                    ["IsActuallyMonitoring"] = _isActuallyMonitoring,
                    ["TimerEnabled"] = _pollingTimer?.Enabled ?? false,
                    ["TimerExists"] = _pollingTimer != null,
                    ["HasTags"] = (_tagsBool.Count + _tagsDint.Count) > 0,
                    ["IsMonitoring"] = IsMonitoring
                };
            }
        }

        public void Dispose()
        {
            lock (_monitoringStateLock)
            {
                if (_isDisposed) return;

                _isDisposed = true;
                _shouldBeMonitoring = false;
                _isActuallyMonitoring = false;
            }

            _pollingTimer?.Stop();
            _pollingTimer?.Dispose();
            _healthCheckTimer?.Stop();
            _healthCheckTimer?.Dispose();
            ClearTags();
        }
    }
}