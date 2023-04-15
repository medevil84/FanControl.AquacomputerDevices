using FanControl.AquacomputerDevices.Devices;
using FanControl.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanControl.AquacomputerDevices
{
    public class AquacomputerPlugin : IPlugin2
    {
        public string Name => "Aquacomputer Devices";
        List<IAquacomputerDevice> devices;

        private readonly IPluginLogger _logger;

        public AquacomputerPlugin(IPluginLogger logger)
        {
            _logger = logger;
        }

        public void Close()
        {
            _logger.Log("AquacomputerPlugin: Closing.");
            devices.ForEach(x => x.Unload());
            devices.Clear();
        }

        public void Initialize()
        {
            _logger.Log("AquacomputerPlugin: Initializing.");
            devices = HidLibrary.HidDevices.Enumerate(0x0C70, AllDevices.GetSupportedProductIds().ToArray())
                .Select(x => AllDevices.GetDevice(x, _logger)).ToList();
        }

        public void Load(IPluginSensorsContainer _container)
        {
            _logger.Log("AquacomputerPlugin: Loading");
            devices.ForEach(x => {
                _logger.Log("AquacomputerPlugin: Loading device "+x.ToString());
                x.Load(_container);
            });
        }

        public void Update()
        {
            devices.ForEach(x => x.Update());
        }
    }
}
