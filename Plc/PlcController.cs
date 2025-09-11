using System;
using System.Windows.Forms;
using XisCoreSensors.Plc;
using static XisCoreSensors.Mapping.SensorTagMapping;

namespace XisCoreSensors.PLC
{
    public class PlcController
    {
        private readonly PlcService _plcService;
        private readonly TagMapper _tagMapper;

        public event Action<string, bool> SensorStateUpdateRequested;

        public PlcController(PlcService plcService, TagMapper tagMapper)
        {
            _plcService = plcService ?? throw new ArgumentNullException(nameof(plcService));
            _tagMapper = tagMapper ?? throw new ArgumentNullException(nameof(tagMapper));

            // Nos suscribimos a los eventos de cambio de tag del servicio.
            _plcService.TagBoolChanged += OnTagBoolChanged;
        }

        // Este método se llamará cada vez que un tag cambie su estado.
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
        }

       
    }
}
