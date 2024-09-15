using AquacomputerStructs.Helpers;
using FanControl.Plugins;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace FanControl.AquacomputerDevices.Devices
{
    internal class FarbwerkDevice : IAquacomputerDevice
    {
        private HidSharp.HidDevice hidDevice = null;
        private HidSharp.HidStream hidStream = null;
        private IPluginLogger _logger;
        private AquacomputerStructs.Devices.Farbwerk.Settings? initial_settings = null;
        private AquacomputerStructs.Devices.Farbwerk.Settings? current_settings = null;
        private AquacomputerStructs.Devices.Farbwerk.sensor_data sensor_data;
        private DateTime last_settings_read = DateTime.MinValue;
        private readonly TimeSpan settings_timeout = new TimeSpan(0, 5, 0); // every 5 minutes

        public int GetProductId() => 0xF010;
        public string GetDevicePath() => hidDevice?.DevicePath;

        public IAquacomputerDevice AssignDevice(HidSharp.HidDevice device, IPluginLogger logger)
        {
            _logger = logger;
            _logger.Log($"FarbwerkDevice.AssignDevice(device: {device}, logger: {logger})");
            if (hidDevice == null)
            {
                hidDevice = device;
                hidStream = hidDevice.Open();

                Update();
            }
            return this;
        }

        public void Load(IPluginSensorsContainer _container, int index = 0)
        {
            _logger.Log($"FarbwerkDevice.Load(_container: {_container}, index = {index})");
            if (hidDevice == null)
                return;

            // Value for this sensor should be checked!
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Farbwerk.sensor_data>("FarbwerkDevice"+index+".Flow", "Farbwerk "+index+" Flow Sensor", () => this.Data_GetTemperature(() => this.sensor_data.flow)));

            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Farbwerk.sensor_data>("FarbwerkDevice"+index+".Temp0", "Farbwerk "+index+" Temperature 1", () => this.Data_GetTemperature(() => this.sensor_data.temperatures[0])));
            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Farbwerk.sensor_data>("FarbwerkDevice"+index+".Temp1", "Farbwerk "+index+" Temperature 2", () => this.Data_GetTemperature(() => this.sensor_data.temperatures[1])));
            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Farbwerk.sensor_data>("FarbwerkDevice"+index+".Temp2", "Farbwerk "+index+" Temperature 3", () => this.Data_GetTemperature(() => this.sensor_data.temperatures[2])));
            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Farbwerk.sensor_data>("FarbwerkDevice"+index+".Temp3", "Farbwerk "+index+" Temperature 4", () => this.Data_GetTemperature(() => this.sensor_data.temperatures[3])));
        }

        public void Unload()
        {
            _logger.Log($"FarbwerkDevice.Unload()");
            hidStream?.Close();
            hidStream = null;
            hidDevice = null;
        }

        public void Update()
        {
            //_logger.Log($"FarbwerkDevice.Update() (hidDevice: {hidDevice}, is open: {hidDevice.IsOpen}");
            if (hidDevice == null || !hidStream?.CanRead == true)
                return;

            var deviceData = hidStream.Read();

            if (deviceData != null)
            {
                int offset = 0;
                EndianAttribute.GetStructAtOffset<AquacomputerStructs.Common.device_header>(deviceData, ref offset);
                EndianAttribute.GetStructAtOffset<AquacomputerStructs.Devices.Farbwerk.device_status>(deviceData, ref offset);
                sensor_data = EndianAttribute.GetStructAtOffset<AquacomputerStructs.Devices.Farbwerk.sensor_data>(deviceData, ref offset);
            }

            Settings_UpdateSettings();
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
                _logger.Log("Data_GetReadLockLambda() ApplicationException: " + e);
            }
            return null;
        }

        private bool Settings_UpdateSettings(bool force = false)
        {
            //_logger.Log($"FarbwerkDevice.Settings_UpdateSettings()");
            int offset = 1;

            if (this.current_settings != null && !force &&
                this.last_settings_read + this.settings_timeout >= DateTime.Now)
                return true;

            // Read current settings
            try
            {
                byte[] data = new byte[hidDevice.GetMaxFeatureReportLength() + 1];
                data[0] = 3;
                hidStream.GetFeature(data);

                this.current_settings = EndianAttribute.GetStructAtOffset<AquacomputerStructs.Devices.Farbwerk.Settings>(data, ref offset);
                if (this.initial_settings == null)
                    this.initial_settings = current_settings;

                this.last_settings_read = DateTime.Now;
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log($"FarbwerkDevice: Failed to read settings from device: {ex}");
                return false;
            }
        }
    }
}
