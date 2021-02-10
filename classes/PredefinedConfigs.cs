using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace VoidProOverlay
{
    class PredefinedConfigs
    {
        public List<Device> deviceconfigs;
        public PredefinedConfigs()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "VoidProOverlay.predefined_configurations.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string jsonFile = reader.ReadToEnd();
                this.deviceconfigs = JsonConvert.DeserializeObject<List<Device>>(jsonFile);
            }
        }
    }

    class Device {
        public String Name { get; set; }
        public String PID { get; set; }
    }
}
