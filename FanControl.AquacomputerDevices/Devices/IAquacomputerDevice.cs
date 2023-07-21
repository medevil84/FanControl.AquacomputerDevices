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

        String GetDevicePath();

        IAquacomputerDevice AssignDevice(HidDevice device, IPluginLogger logger);

        void Load(IPluginSensorsContainer _container, int index = 0);

        void Unload();

        void Update();

        //ReaderWriterLock GetReaderWriterLock();

        //object GetStructureData();
    }
}
