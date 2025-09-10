using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XisCoreSensors.Plc
{
    public class PlcServices
    {
        public event EventHandler<TagChangedEventArgs> TagValueChanged;

        public PlcServices() { }

        public void Connect(string ipAddress, string cpuType) 
        {
            
        }

        public void AddTags(IEnumerable<string> tagNames) { }

        public void StartReading() { }

        public void StopReading() { }
    }

    public class TagChangedEventArgs : EventArgs
    { 
        public string TagName { get; set; }
        public object NewValue { get; set; }
        
    }
}
