using FanControl.Plugins;
using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FanControl.AquacomputerDevices.Devices
{
    internal interface IAquacomputerDevice
    {
        int GetProductId();

        IAquacomputerDevice AssignDevice(HidDevice device, IPluginLogger logger);

        void Load(IPluginSensorsContainer _container);

        void Unload();

        void Update();

        //ReaderWriterLock GetReaderWriterLock();

        //object GetStructureData();
    }
}
