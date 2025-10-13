using System;
using XisCoreSensors.Plc;
using static XisCoreSensors.Mapping.SensorTagMapping;

namespace XisCoreSensors.PLC
{
    public class PlcController
    {
        // Servicios y mapeadores necesarios
        private readonly PlcService _plcService;
        private readonly TagMapper _tagMapper;

        // Eventos para notificar a la UI
        public event Action<string, bool> SensorStateUpdateRequested;
        public event Action<int> SecuenceStepChanged;
        public event Action<bool> BoolMonitoringPausedStateChanged;
        public event Action<string, int> ImageSelectorTagChanged;
        public event Action<int> AlarmNumberChanged;
        public event Action<StopWatchCommand> StopwatchCommandReceived;

        public enum StopWatchCommand
        {
            Start,
            Pause,
            Reset
        }

        public PlcController(PlcService plcService, TagMapper tagMapper)
        {
            _plcService = plcService ?? throw new ArgumentNullException(nameof(plcService));
            _tagMapper = tagMapper ?? throw new ArgumentNullException(nameof(tagMapper));

            // Nos suscribimos a los eventos de cambio de tag del servicio.
            _plcService.TagBoolChanged += OnTagBoolChanged;
            _plcService.TagDintChanged += OnTagDintChanged;
        }

        // Este metodo se llamará cada vez que un tag de tipo DINT cambie su valor.
        private void OnTagDintChanged(string tagName, int newValue)
        {
            var secuenceTagName = Properties.Settings.Default.SequenceTagName;
            var secuenciaImagen = Properties.Settings.Default.ImageTagName;
            var chronoTagName = Properties.Settings.Default.ChronoTgName;
            var alarmTagName = Properties.Settings.Default.AlarmTagName;

            if (tagName.Equals(secuenceTagName, StringComparison.OrdinalIgnoreCase))
            {
                // 1. Pausa o reanuda el monitoreo de los sensores booleanos
                //    (Esta lógica ya la tenías y es correcta).
                var isPaused = (newValue == 0);
                _plcService.PauseBoolMonitoring(isPaused);
                BoolMonitoringPausedStateChanged?.Invoke(isPaused);
                // 2. Notifica a la UI sobre el nuevo paso de la secuencia.
                //    El FrmMainMDI se encargará de la lógica visual.
                SecuenceStepChanged?.Invoke(newValue);
            }
            else if (tagName.Equals(alarmTagName, StringComparison.OrdinalIgnoreCase))
            {
                // Simplemente notifica a la UI que el número de alarma ha cambiado.
                AlarmNumberChanged?.Invoke(newValue);
            }
            else if(tagName.Equals(secuenciaImagen, StringComparison.OrdinalIgnoreCase))
            {
                ImageSelectorTagChanged?.Invoke(tagName, newValue);
            }
            else if(tagName.Equals(chronoTagName, StringComparison.OrdinalIgnoreCase))
            {
                // Interpreta el valor del tag como un comando para el cronómetro.
                switch (newValue)
                {
                    case 1:
                        StopwatchCommandReceived?.Invoke(StopWatchCommand.Start);
                        break;
                    case 2:
                        StopwatchCommandReceived?.Invoke(StopWatchCommand.Pause);
                        break;
                    case 3:
                        StopwatchCommandReceived?.Invoke(StopWatchCommand.Reset);
                        break;      
                    default:
                        // Valor no reconocido, no hacer nada.
                        break;
                }
            }

        }

        // Este método se llamará cada vez que un tag BOOL cambie su estado.
        private void OnTagBoolChanged(string tagName, bool plcValue)
        {
            // 1. Buscamos qué sensor está mapeado a este tag.
            var sensorId = _tagMapper.GetSensorForTag(tagName);

            if (!string.IsNullOrEmpty(sensorId))
            {               
                // El PLC nos da 'plcValue': true (1) o false (0).
                // Nuestro evento 'SensorStateUpdateRequested' necesita saber si el sensor está fallado ('isFailed').
                //
                // Si plcValue es true (1), significa que está OK, por lo tanto, isFailed es 'false'.
                // Si plcValue es false (0), significa que hay una FALLA, por lo tanto, isFailed es 'true'.

                var isFailed = !plcValue;
                

                // 3. Disparamos nuestro evento con el valor ya traducido.
                SensorStateUpdateRequested?.Invoke(sensorId, isFailed);
            }
        }

        public void Unsubscribe()
        {
            _plcService.TagBoolChanged -= OnTagBoolChanged;
            _plcService.TagDintChanged -= OnTagDintChanged;
        }
       
    }
}
