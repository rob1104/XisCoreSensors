using System;
using System.Collections.Generic;
using System.Linq;

namespace XisCoreSensors.Mapping
{
    public class SensorTagMapping
    {
        public string SensorId { get; set; }
        public string PlcTag { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public class TagMapper
        {
            private Dictionary<string, SensorTagMapping> _mappings = new Dictionary<string, SensorTagMapping>();
            public event EventHandler<MappingChangedEventArgs> MappingChanged;

            //Obtiene todas las asignaciones activas
            public IEnumerable<SensorTagMapping> GetAllMappings()
            {
                return _mappings.Values.Where(m => m.IsActive);
            }

            //Obtiene el tag asignado de un sensor especifico
            public string GetTagForSensor(string sensorId)
            {
                if (_mappings.TryGetValue(sensorId, out var mapping) && mapping.IsActive)
                {
                    return mapping.PlcTag;
                }
                return null;
            }

            //Obtiene el sensor asignado a un tag especifico
            public string GetSensorForTag(string plcTag)
            {
                var mapping = _mappings.Values.FirstOrDefault(m =>
                    m.IsActive &&
                    string.Equals(m.PlcTag, plcTag, StringComparison.OrdinalIgnoreCase));

                return mapping?.SensorId;
            }

            //Crea o actualiza un mapeo entre sensor y tag
            public bool MapSensorToTag(string sensorId, string plcTag)
            {
                if (string.IsNullOrWhiteSpace(sensorId) || string.IsNullOrWhiteSpace(plcTag))
                    return false;

                // Verificar si el tag ya está siendo usado por otro sensor
                var existingMapping = _mappings.Values.FirstOrDefault(m =>
                    m.IsActive &&
                    string.Equals(m.PlcTag, plcTag, StringComparison.OrdinalIgnoreCase) &&
                    m.SensorId != sensorId);

                if (existingMapping != null)
                {
                    throw new InvalidOperationException($"Tag '{plcTag}' is already mapped to sensor '{existingMapping.SensorId}'");
                }

                // Crear o actualizar el mapeo
                var mapping = new SensorTagMapping
                {
                    SensorId = sensorId,
                    PlcTag = plcTag,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                _mappings[sensorId] = mapping;

                // Notificar el cambio
                MappingChanged?.Invoke(this, new MappingChangedEventArgs
                {
                    SensorId = sensorId,
                    PlcTag = plcTag,
                    Action = MappingAction.Added
                });
                return true;
            }

            //Elimina el mapeo de un sensor
            public bool UnmapSensor(string sensorId)
            {
                if (_mappings.TryGetValue(sensorId, out var mapping))
                {
                    mapping.IsActive = false;

                    // Notificar el cambio
                    MappingChanged?.Invoke(this, new MappingChangedEventArgs
                    {
                        SensorId = sensorId,
                        PlcTag = mapping.PlcTag,
                        Action = MappingAction.Removed
                    });

                    return true;
                }
                return false;
            }

            //Verifica si un sensor tiene un tag asignado
            public bool IsSensorMapped(string sensorId)
            {
                return _mappings.TryGetValue(sensorId, out var mapping) && mapping.IsActive;
            }

            //Obtiene todos los sensores no mapeados de una lista
            public IEnumerable<string> GetUnmappedSensors(IEnumerable<string> allSensorIds)
            {
                return allSensorIds.Where(id => !IsSensorMapped(id));
            }

            //Obtiene estadísticas del mapeo
            public MappingStatistics GetStatistics(int totalSensors)
            {
                var activeMappings = GetAllMappings().Count();
                return new MappingStatistics
                {
                    TotalSensors = totalSensors,
                    MappedSensors = activeMappings,
                    UnmappedSensors = totalSensors - activeMappings,
                    MappingPercentage = totalSensors > 0 ? (double)activeMappings / totalSensors * 100 : 0
                };
            }

            //Carga mapeos desde datos serializados
            public void LoadMappings(IEnumerable<SensorTagMapping> mappings)
            {
                _mappings.Clear();
                foreach (var mapping in mappings.Where(m => m.IsActive))
                {
                    _mappings[mapping.SensorId] = mapping;
                }
            }

            //Limpia todos los mapeos
            public void ClearAllMappings()
            {
                var mappedSensors = _mappings.Keys.ToList();
                _mappings.Clear();

                // Notificar que se eliminaron todos los mapeos
                foreach (var sensorId in mappedSensors)
                {
                    MappingChanged?.Invoke(this, new MappingChangedEventArgs
                    {
                        SensorId = sensorId,
                        PlcTag = null,
                        Action = MappingAction.Removed
                    });
                }
            }
        }       
    }
    public class MappingChangedEventArgs : EventArgs
    {
        public string SensorId { get; set; }
        public string PlcTag { get; set; }
        public MappingAction Action { get; set; }
    }

    public enum MappingAction
    {
        Added,
        Removed,
        Updated
    }

    public class MappingStatistics
    {
        public int TotalSensors { get; set; }
        public int MappedSensors { get; set; }
        public int UnmappedSensors { get; set; }
        public double MappingPercentage { get; set; }
    }
}
