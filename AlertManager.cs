using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XisCoreSensors
{
    public class AlertManager
    {
        private List<AlertMessage> _alerts;
        private readonly string _filePath;

        public AlertManager(string filePath)
        {
            _filePath = filePath;           
            LoadAlerts();
        }

        private void LoadAlerts()
        {
            if (!File.Exists(_filePath))
            {
                // Si el archivo no existe, crea una lista con valores por defecto.
                _alerts = new List<AlertMessage>
                {
                    new AlertMessage { SequenceNumber = 0, Message = "WAITING OPERATOR TO LOAD PART" },
                    new AlertMessage { SequenceNumber = 1, Message = "READY" }
                };
                SaveChanges(); // Y lo guarda.
                return;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                _alerts = JsonConvert.DeserializeObject<List<AlertMessage>>(json) ?? new List<AlertMessage>();
            }
            catch (Exception)
            {
                // En caso de error, inicializa una lista vacía.
                _alerts = new List<AlertMessage>();
            }
        }

        public void SaveChanges()
        {
            var json = JsonConvert.SerializeObject(_alerts, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }

        public List<AlertMessage> GetAlerts()
        {
            // Devuelve una copia ordenada por número.
            return _alerts.OrderBy(a => a.SequenceNumber).ToList();
        }

        public string GetMessageForSequence(int sequenceNumber)
        {
            var alert = _alerts.FirstOrDefault(a => a.SequenceNumber == sequenceNumber);
            return alert?.Message; // Devuelve el mensaje o null si no lo encuentra.
        }

        public bool AddOrUpdateAlert(AlertMessage alert)
        {
            if (alert == null || string.IsNullOrWhiteSpace(alert.Message)) return false;

            var existing = _alerts.FirstOrDefault(a => a.SequenceNumber == alert.SequenceNumber);
            if (existing != null)
            {
                // Actualiza el mensaje si el número ya existe.
                existing.Message = alert.Message;
            }
            else
            {
                // Añade la nueva alerta si no existe.
                _alerts.Add(alert);
            }
            SaveChanges();
            return true;
        }

        public bool DeleteAlert(int sequenceNumber)
        {
            var alertToRemove = _alerts.FirstOrDefault(a => a.SequenceNumber == sequenceNumber);
            if (alertToRemove != null)
            {
                _alerts.Remove(alertToRemove);
                SaveChanges();
                return true;
            }
            return false;
        }
    }
}
