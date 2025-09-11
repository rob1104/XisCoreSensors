using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace XisCoreSensors
{
    internal class TagCatalogManager
    {
        private static readonly string FilePath = Path.Combine(Application.StartupPath, "PlcCatalog.json");

        public List<string> Load()
        {
            if (!File.Exists(FilePath))
            {
                return new List<string>();
            }

            try
            {
                string json = File.ReadAllText(FilePath);
                return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error loading tag catalog: {e.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<string>();
            }
        }

        public void Save(List<string> tags)
        {
            try
            {
                string json = JsonConvert.SerializeObject(tags, Formatting.Indented);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error saving tag catalog: {e.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
