using FanControl.AquacomputerDevices.Devices;
using FanControl.AquacomputerDevices.Logging;
using FanControl.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FanControl.AquacomputerDevices
{
    public class AquacomputerPlugin : IPlugin2
    {
        public string Name => "Aquacomputer Devices";
        Dictionary<IAquacomputerDevice, int> devices;

        private readonly IPluginLogger _logger;

        public AquacomputerPlugin(IPluginLogger logger)
        {
            // Silent rate-limit into FanControl service_log (same idea as LianLi v1.1.41+
            // not dual-writing noise; we only have the host log, so keep it quiet).
            _logger = new RateLimitedPluginLogger(logger);
        }

        public void Close()
        {
            if (devices == null) return;
            devices.Keys.ToList().ForEach(x => x.Unload());
            devices.Clear();
        }

        public void Initialize()
        {
            devices = HidSharp.DeviceList.Local.GetHidDevices(0x0C70)
                .Where(x => AllDevices.GetSupportedProductIds().Contains(x.ProductID))
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
            if (devices == null) return;
            foreach (var item in devices)
                item.Key.Load(_container, item.Value);
        }

        public void Update()
        {
            if (devices == null) return;
            devices.Keys.ToList().ForEach(x => x.Update());
        }
    }
}
