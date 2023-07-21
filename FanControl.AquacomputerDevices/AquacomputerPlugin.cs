using FanControl.AquacomputerDevices.Devices;
using FanControl.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FanControl.AquacomputerDevices
{
    public class AquacomputerPlugin : IPlugin2
    {
        public string Name => "Aquacomputer Devices";
        //List<IAquacomputerDevice> devices;
        Dictionary<IAquacomputerDevice, int> devices;
        
        private readonly IPluginLogger _logger;

        public AquacomputerPlugin(IPluginLogger logger)
        {
            _logger = logger;
        }

        public void Close()
        {
            _logger.Log("AquacomputerPlugin: Closing.");
            devices.Keys.ToList().ForEach(x => x.Unload());
            devices.Clear();
        }

        public void Initialize()
        {
            _logger.Log("AquacomputerPlugin: Initializing.");
            devices = HidLibrary.HidDevices.Enumerate(0x0C70, AllDevices.GetSupportedProductIds().ToArray())
                .Select(x => AllDevices.GetDevice(x, _logger))
                .GroupBy(x => x.GetType())
                .SelectMany(x => x
                    .OrderBy(t => t.GetDevicePath().ToLowerInvariant())
                    .Select((y, i) => new { Key = y, Index = i })
                )
                .ToDictionary(k => k.Key, e => e.Index);
        }

        public void Load(IPluginSensorsContainer _container)
        {
            _logger.Log("AquacomputerPlugin: Loading");
            foreach (var item in devices)
            {
                _logger.Log("AquacomputerPlugin: Loading device " + item.Key.ToString() + " with index: " + item.Value);
                item.Key.Load(_container, item.Value);
            }
        }

        public void Update()
        {
            devices.Keys.ToList().ForEach(x => x.Update());
        }
    }
}
