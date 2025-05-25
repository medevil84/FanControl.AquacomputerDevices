using AquacomputerStructs.Helpers;
using FanControl.Plugins;
using System;

namespace FanControl.AquacomputerDevices.Devices
{
    internal class HighFlowNextDevice : IAquacomputerDevice
    {
        private HidSharp.HidDevice hidDevice = null;
        private CachedHidStream hidStream = null;
        private IPluginLogger _logger;
        private AquacomputerStructs.Devices.HighFlowNext.sensor_data sensor_data;

        public int GetProductId() => 0xF012;

        public string GetDevicePath() => hidDevice?.DevicePath;

        public IAquacomputerDevice AssignDevice(HidSharp.HidDevice device, IPluginLogger logger)
        {
            _logger = logger;
            _logger.Log($"HighFlowNextDevice.AssignDevice(device: {device}, logger: {logger})");
            if (hidDevice == null)
            {
                hidDevice = device;
                hidStream = new CachedHidStream(hidDevice.Open());

                Update();
            }
            return this;
        }

        public void Load(IPluginSensorsContainer _container, int index = 0)
        {
            _logger.Log($"HighFlowNextDevice.Load(_container: {_container}, index = {index})");
            if (hidDevice == null)
                return;

            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.HighFlowNext.sensor_data>("HighFlowNextDevice"+index+".TemperatureWater", "HighFlowNext "+index+" Water Temperature", () => this.Data_GetTemperature(() => sensor_data.temperature_water)));
            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.HighFlowNext.sensor_data>("HighFlowNextDevice"+index+".TemperatureExt"  , "HighFlowNext "+index+" External Water Temperature", () => this.Data_GetTemperature(() => sensor_data.temperature_ext)));
            _container.FanSensors.Add( new DeviceBaseSensor<AquacomputerStructs.Devices.HighFlowNext.sensor_data>("HighFlowNextDevice"+index+".Flow"            , "HighFlowNext "+index+" Flow", () => this.Data_GetTemperature(() => sensor_data.flow, 10.0f)));
            _container.FanSensors.Add( new DeviceBaseSensor<AquacomputerStructs.Devices.HighFlowNext.sensor_data>("HighFlowNextDevice"+index+".WaterQuality"    , "HighFlowNext "+index+" Water Quality", () => this.Data_GetTemperature(() => sensor_data.water_quality)));
            _container.FanSensors.Add( new DeviceBaseSensor<AquacomputerStructs.Devices.HighFlowNext.sensor_data>("HighFlowNextDevice"+index+".Conductivity"    , "HighFlowNext "+index+" Conductivity", () => this.Data_GetTemperature(() => sensor_data.conductivity, 10.0f)));

        }

        public void Unload()
        {
            _logger.Log($"HighFlowNextDevice.Unload()");
            hidStream?.Close();
            hidStream = null;
            hidDevice = null;
        }

        public void Update()
        {
            if (hidDevice == null || !hidStream?.CanRead == true)
                return;

            var deviceData = hidStream.Read();

            if (deviceData != null)
            {
                int offset = 0;
                EndianAttribute.GetStructAtOffset<AquacomputerStructs.Common.device_header>(deviceData, ref offset);
                EndianAttribute.GetStructAtOffset<AquacomputerStructs.Devices.HighFlowNext.device_status>(deviceData, ref offset);
                sensor_data = EndianAttribute.GetStructAtOffset<AquacomputerStructs.Devices.HighFlowNext.sensor_data>(deviceData, ref offset);
            }
        }

        private float? Data_GetTemperature(Func<short> temp, float ratio = 100.0f)
        {
            if (hidDevice == null)
                return null;

            try
            {
                short val = temp();
                if (val == short.MaxValue)
                    return 0;

                return val / ratio;
            }
            catch (ApplicationException e)
            {
                _logger.Log("Data_GetTemperature() ApplicationException: " + e);
            }
            
            return null;
        }
    }
}
