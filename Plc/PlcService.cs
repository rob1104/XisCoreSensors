using libplctag;
using libplctag.DataTypes;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using libplctag.NativeImport;

namespace XisCoreSensors.Plc
{
    public class PlcService : IDisposable
    {
        // --- Eventos para notificar cambios ---
        public event Action<string, bool> TagBoolChanged;
        // NUEVO: Evento para cambios en tags DINT (enteros de 32 bits).
        public event Action<string, int> TagDintChanged;
        // NUEVO: Evento para cambios en tags STRING.
        public event Action<string, string> TagStringChanged;

        //Un diccionario para guardar el último estado conocido de CADA tag.
        private readonly Dictionary<string, bool?> _lastKnownStates = new Dictionary<string, bool?>();

        // --- Diccionarios para almacenar los tags por tipo ---
        private readonly Dictionary<string, Tag<BoolPlcMapper, bool>> _tagsBool = new Dictionary<string, Tag<BoolPlcMapper, bool>>();
        // NUEVO: Diccionario para tags DINT.
        private readonly Dictionary<string, Tag<DintPlcMapper, int>> _tagsDint = new Dictionary<string, Tag<DintPlcMapper, int>>();
        // NUEVO: Diccionario para tags STRING.
        private readonly Dictionary<string, Tag<StringPlcMapper, string>> _tagsString = new Dictionary<string, Tag<StringPlcMapper, string>>();

        // --- Diccionarios para el último estado conocido ---
        private readonly Dictionary<string, bool?> _lastKnownBoolStates = new Dictionary<string, bool?>();
        // NUEVO: Diccionario para el último estado de los DINT.
        private readonly Dictionary<string, int?> _lastKnownDintStates = new Dictionary<string, int?>();
        // NUEVO: Diccionario para el último estado de los STRING.
        private readonly Dictionary<string, string> _lastKnownStringStates = new Dictionary<string, string>();

        public event Action<string> MonitoringError;


        // La configuración de conexión no cambia
        private readonly string _gateway;
        private readonly string _path;
        private readonly PlcType _plcType;
        private readonly Protocol _protocol;
        private readonly TimeSpan _timeout;

        private readonly System.Windows.Forms.Timer _pollingTimer;

        private CancellationTokenSource _cts;

        public IEnumerable<string> BoolTagNames => _tagsBool.Keys.ToList();
        public IEnumerable<string> DintTagNames => _tagsDint.Keys.ToList();
        public IEnumerable<string> StringTagNames => _tagsString.Keys.ToList();

        public PlcService(string gateway, string path, PlcType plcType, Protocol protocol, TimeSpan timeout)
        {
            _gateway = gateway;
            _path = path;
            _plcType = plcType;
            _protocol = protocol;
            _timeout = timeout;

            _pollingTimer = new System.Windows.Forms.Timer();
            _pollingTimer.Interval = 250;
            _pollingTimer.Tick += PollingTimer_Tick;
        }

        private async void PollingTimer_Tick(object sender, EventArgs e)
        {
            // Para evitar que se ejecute de nuevo si la lectura anterior aún no ha terminado.
            _pollingTimer.Stop();

            try
            {
                // Itera sobre todos los tags booleanos para leer su valor.
                foreach (var tagEntry in _tagsBool)
                {
                    var tagName = tagEntry.Key;
                    var tag = tagEntry.Value;

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
                // ... (Aquí podrías añadir bucles similares para leer DINTs y STRINGs) ...
            }
            catch (Exception ex)
            {
                // Si hay un error, lo puedes notificar o simplemente registrarlo.
                MonitoringError?.Invoke($"Error de sondeo del PLC: {ex.Message}");
            }
            finally
            {
                // Vuelve a iniciar el timer para el siguiente ciclo.
                _pollingTimer.Start();
            }
        }

        public void StartMonitoring()
        {
            _pollingTimer.Start();
        }

        public void StopMonitoring()
        {
            _pollingTimer.Stop();
            _cts?.Cancel();
            _cts?.Dispose();
        }

        // --- Métodos de Inicialización ---
        public void InitializeBoolTags(IEnumerable<string> tagNames)
        {
            foreach (var tagName in tagNames)
            {
                var tag = new Tag<BoolPlcMapper, bool> { Name = tagName, Gateway = _gateway, Path = _path, PlcType = _plcType, Protocol = _protocol, Timeout = _timeout };
                _tagsBool.Add(tagName, tag);
                _lastKnownBoolStates.Add(tagName, null);
            }
        }

        // NUEVO: Método para inicializar tags DINT.
        public void InitializeDintTags(IEnumerable<string> tagNames)
        {
            foreach (var tagName in tagNames)
            {
                var tag = new Tag<DintPlcMapper, int> { Name = tagName, Gateway = _gateway, Path = _path, PlcType = _plcType, Protocol = _protocol, Timeout = _timeout };
                _tagsDint.Add(tagName, tag);
                _lastKnownDintStates.Add(tagName, null);
            }
        }

        // NUEVO: Método para inicializar tags STRING.
        public void InitializeStringTags(IEnumerable<string> tagNames)
        {
            foreach (var tagName in tagNames)
            {
                var tag = new Tag<StringPlcMapper, string> { Name = tagName, Gateway = _gateway, Path = _path, PlcType = _plcType, Protocol = _protocol, Timeout = _timeout };
                _tagsString.Add(tagName, tag);
                _lastKnownStringStates.Add(tagName, null);
            }
        }

        // --- Métodos de Monitoreo ---
        public async Task MonitorAllBoolTagsAsync(CancellationToken token)
        {
            const int pollingIntervalMs = 250;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Recorremos la lista de tags que queremos monitorear.
                    foreach (var tagEntry in _tagsBool)
                    {
                        var tagName = tagEntry.Key;
                        var tag = tagEntry.Value;

                        // Leemos el tag actual en el bucle.
                        await tag.ReadAsync(token);
                        bool currentState = tag.Value;

                        // Obtenemos el último estado conocido para ESTE tag específico.
                        _lastKnownBoolStates.TryGetValue(tagName, out bool? lastState);

                        // Comparamos el estado actual con el último conocido.
                        if (currentState != lastState)
                        {
                            // Disparamos el evento con el nombre del tag que cambió y su nuevo estado.
                            TagBoolChanged?.Invoke(tagName, currentState);
                            // Actualizamos el diccionario con el nuevo estado.
                            _lastKnownBoolStates[tagName] = currentState;
                        }
                    }

                    // Hacemos una sola pausa por cada ciclo de lectura de todos los tags.
                    await Task.Delay(pollingIntervalMs, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en el bucle de monitoreo: {ex.Message}");
                    await Task.Delay(5000, token);
                }
            }
        }

        // NUEVO: Método para monitorear todos los DINT. Es una copia del de BOOL, pero con tipos 'int'.
        public async Task MonitorAllDintTagsAsync(CancellationToken token)
        {
            const int pollingIntervalMs = 500; // Los enteros pueden cambiar más lento, podemos dar más tiempo.

            while (!token.IsCancellationRequested)
            {
                try
                {
                    foreach (var tagEntry in _tagsDint)
                    {
                        var tagName = tagEntry.Key;
                        var tag = tagEntry.Value;

                        await tag.ReadAsync(token);
                        int currentState = tag.Value;

                        _lastKnownDintStates.TryGetValue(tagName, out int? lastState);

                        if (currentState != lastState)
                        {
                            TagDintChanged?.Invoke(tagName, currentState);
                            _lastKnownDintStates[tagName] = currentState;
                        }
                    }
                    await Task.Delay(pollingIntervalMs, token);
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en bucle DINT: {ex.Message}");
                    await Task.Delay(5000, token);
                }
            }
        }

        // NUEVO: Método para monitorear todos los STRING.
        public async Task MonitorAllStringTagsAsync(CancellationToken token)
        {
            const int pollingIntervalMs = 1000; // Las cadenas suelen cambiar con menos frecuencia.

            while (!token.IsCancellationRequested)
            {
                try
                {
                    foreach (var tagEntry in _tagsString)
                    {
                        var tagName = tagEntry.Key;
                        var tag = tagEntry.Value;

                        await tag.ReadAsync(token);
                        string currentState = tag.Value;

                        _lastKnownStringStates.TryGetValue(tagName, out string lastState);

                        if (currentState != lastState)
                        {
                            TagStringChanged?.Invoke(tagName, currentState);
                            _lastKnownStringStates[tagName] = currentState;
                        }
                    }
                    await Task.Delay(pollingIntervalMs, token);
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en bucle STRING: {ex.Message}");
                    await Task.Delay(5000, token);
                }
            }
        }


        // --- Métodos de Escritura ---
        public async Task WriteBoolAsync(string tagName, bool value)
        {
            if (!_tagsBool.TryGetValue(tagName, out var tagToWrite))
            {
                throw new ArgumentException($"El tag '{tagName}' no ha sido inicializado.");
            }

            tagToWrite.Value = value;
            await tagToWrite.WriteAsync();
        }

        //Método para escribir en un tag DINT.
        public async Task WriteDintAsync(string tagName, int value)
        {
            if (!_tagsDint.TryGetValue(tagName, out var tagToWrite))
            {
                throw new ArgumentException($"El tag DINT '{tagName}' no ha sido inicializado.");
            }
            tagToWrite.Value = value;
            await tagToWrite.WriteAsync();
        }

        //Envia un pulso momentaneo ON, espera OFF) a un tag boolean
        public async Task SendPulseAsync(string tagName, int pulseDuration = 1000)
        {
            if (!_tagsBool.TryGetValue(tagName, out var tag))
            {
                throw new ArgumentException($"El tag '{tagName}' no ha sido inicializado.");
            }

            try
            {
                tag.Value = true; // Enviamos el pulso ON   
                await tag.WriteAsync();
                await Task.Delay(pulseDuration); // Esperamos el tiempo del pulso
            }
            finally
            {
                tag.Value = false; // Enviamos el pulso OFF
                await tag.WriteAsync();
            }
        }

        public async Task TestConnectionAsync(CancellationToken token)
        {
            // Intentamos leer el primer tag que tengamos en cualquiera de las listas.
            // Si esta operación no lanza una excepción, la conexión es exitosa.
            var tagToTest = _tagsBool.Values.FirstOrDefault() ??
                            (ITag)_tagsDint.Values.FirstOrDefault() ??
                            _tagsString.Values.FirstOrDefault();

            if (tagToTest == null)
            {
                // No hay tags para probar, asumimos que está bien por ahora.
                // En una aplicación real, podríamos querer manejar esto de otra forma.
                return;
            }

            // Usamos el método ReadAsync de la interfaz base ITag.
            await tagToTest.ReadAsync(token);
        }

        // --- Limpieza ---
        public void Dispose()
        {
            // Actualizamos para que limpie todos los diccionarios.
            _pollingTimer.Stop();
            _pollingTimer.Dispose();
            ClearTags();
        }

        //NUEVOS METODOS PARA OBTENER EL ULTIMO VALOR
        public bool TryGetLastBoolValue(string tagName, out bool? value)
        {
            return _lastKnownBoolStates.TryGetValue(tagName, out value);
        }

        public bool TryGetLastDintValue(string tagName, out int? value)
        {
            return _lastKnownDintStates.TryGetValue(tagName, out value);
        }

        public void ClearTags()
        {
            // 1. Libera los recursos de cada tag individualmente antes de eliminarlos.
            // Esto es crucial para evitar fugas de memoria y conexiones "zombis".
            foreach (var tag in _tagsBool.Values)
            {
                tag.Dispose();
            }
            foreach (var tag in _tagsDint.Values)
            {
                tag.Dispose();
            }
            foreach (var tag in _tagsString.Values)
            {
                tag.Dispose();
            }

            // 2. Limpia completamente los diccionarios que almacenan los tags.
            _tagsBool.Clear();
            _tagsDint.Clear();
            _tagsString.Clear();

            // 3. Limpia también los diccionarios que guardan el último estado conocido.
            _lastKnownBoolStates.Clear();
            _lastKnownDintStates.Clear();
            _lastKnownStringStates.Clear();
        }
       
    }

}
