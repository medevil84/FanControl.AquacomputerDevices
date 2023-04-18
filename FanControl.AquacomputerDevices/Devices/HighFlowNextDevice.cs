using AquacomputerStructs.Helpers;
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
    internal class HighFlowNextDevice : IAquacomputerDevice
    {
        private HidDevice hidDevice = null;
        private IPluginLogger _logger;
        private ReaderWriterLock rwl;
        private AquacomputerStructs.Devices.HighFlowNext.sensor_data sensor_data;

        public int GetProductId() => 0xF012;

        public IAquacomputerDevice AssignDevice(HidDevice device, IPluginLogger logger)
        {
            _logger = logger;
            _logger.Log($"HighFlowNextDevice.AssignDevice(device: {device}, logger: {logger})");
            if (hidDevice == null)
            {
                hidDevice = device;
                rwl = new ReaderWriterLock();
            }
            return this;
        }

        public void Load(IPluginSensorsContainer _container)
        {
            _logger.Log($"HighFlowNextDevice.Load(_container: {_container})");
            if (hidDevice == null)
                return;

            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.HighFlowNext.sensor_data>("HighFlowNextDevice.TemperatureWater", "Water Temperature", () => this.Data_GetTemperature(() => sensor_data.temperature_water)));
            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.HighFlowNext.sensor_data>("HighFlowNextDevice.TemperatureExt", "External Water Temperature", () => this.Data_GetTemperature(() => sensor_data.temperature_ext)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.HighFlowNext.sensor_data>("HighFlowNextDevice.Flow", "Flow", () => this.Data_GetTemperature(() => sensor_data.flow, 10.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.HighFlowNext.sensor_data>("HighFlowNextDevice.WaterQuality", "Water Quality", () => this.Data_GetTemperature(() => sensor_data.water_quality)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.HighFlowNext.sensor_data>("HighFlowNextDevice.Conductivity", "Conductivity", () => this.Data_GetTemperature(() => sensor_data.conductivity, 10.0f)));

        }

        public void Unload()
        {
            _logger.Log($"HighFlowNextDevice.Unload()");
            hidDevice?.CloseDevice();
            hidDevice = null;
        }

        public void Update()
        {
            if (hidDevice == null)
                return;

            rwl.AcquireWriterLock(100);
            try
            {
                var deviceData = hidDevice.Read(500);

                if (deviceData != null && deviceData.Status == HidLibrary.HidDeviceData.ReadStatus.Success)
                {
                    int offset = 0;
                    EndianAttribute.GetStructAtOffset<AquacomputerStructs.Common.device_header>(deviceData.Data, ref offset);
                    EndianAttribute.GetStructAtOffset<AquacomputerStructs.Devices.HighFlowNext.device_status>(deviceData.Data, ref offset);
                    sensor_data = EndianAttribute.GetStructAtOffset<AquacomputerStructs.Devices.HighFlowNext.sensor_data>(deviceData.Data, ref offset);
                }
            }
            finally
            {
                rwl.ReleaseWriterLock();
            }
        }

        private float? Data_GetTemperature(Func<short> temp, float ratio = 100.0f)
        {
            short val = (short)Data_GetReadLockLambda(() => (float)temp());
            if (val == short.MaxValue)
                return 0;

            return val / ratio;
        }

        private float? Data_GetReadLockLambda(Func<float?> x)
        {
            if (hidDevice == null)
                return null;

            try
            {
                rwl.AcquireReaderLock(100);
                try
                {
                    return x();
                }
                finally
                {
                    rwl.ReleaseReaderLock();
                }
            }
            catch (ApplicationException) { }
            return null;
        }
    }
}
