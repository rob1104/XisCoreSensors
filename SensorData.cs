using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XisCoreSensors.Controls;
using XisCoreSensors.Mapping;

namespace XisCoreSensors
{
    public class LayoutData
    {
        public string ImagePath { get; set; }
        public List<SensorData> Sensors { get; set; } = new List<SensorData>();
        public List<SensorTagMapping> TagMappings { get; set; } = new List<SensorTagMapping>();
    }

    public class SensorData
    {
        public String Id { get; set; }
        public float RelativeX { get; set; } 
        public float RelativeY { get; set; } 
        public SensorControl.SensorStatus Status { get; set; } = SensorControl.SensorStatus.Unmapped;
        public string PlcTag { get; set; }
    }
}
