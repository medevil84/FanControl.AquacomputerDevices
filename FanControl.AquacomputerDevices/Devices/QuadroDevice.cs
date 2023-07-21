using AquacomputerStructs.Helpers;
using FanControl.Plugins;
using HidLibrary;
using System;
using System.Linq;
using System.Threading;

namespace FanControl.AquacomputerDevices.Devices
{
    internal class QuadroDevice : IAquacomputerDevice
    {
        private HidLibrary.HidDevice hidDevice = null;
        private IPluginLogger _logger;
        private AquacomputerStructs.Devices.Quadro.Settings? initial_settings = null;
        private AquacomputerStructs.Devices.Quadro.Settings? current_settings = null;
        private AquacomputerStructs.Devices.Quadro.sensor_data sensor_data;
        private DateTime last_settings_read = DateTime.MinValue;
        private readonly TimeSpan settings_timeout = new TimeSpan(0, 5, 0); // every 5 minutes

        public int GetProductId() => 0xF00D;
        public string GetDevicePath() => hidDevice?.DevicePath;

        public IAquacomputerDevice AssignDevice(HidDevice device, IPluginLogger logger)
        {
            _logger = logger;
            _logger.Log($"QuadroDevice.AssignDevice(device: {device}, logger: {logger})");
            if (hidDevice == null)
            {
                hidDevice = device;

                hidDevice.OpenDevice();
                Update();
            }
            return this;
        }

        public void Load(IPluginSensorsContainer _container, int index = 0)
        {
            _logger.Log($"QuadroDevice.Load(_container: {_container}, index = {index})");
            if (hidDevice == null)
                return;

            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Quadro.sensor_data>("QuadroDevice"+index+".Fans0", "Quadro "+index+" Fan 1", () => this.Data_GetTemperature(() => this.sensor_data.fans[0].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Quadro.sensor_data>("QuadroDevice"+index+".Fans1", "Quadro "+index+" Fan 2", () => this.Data_GetTemperature(() => this.sensor_data.fans[1].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Quadro.sensor_data>("QuadroDevice"+index+".Fans2", "Quadro "+index+" Fan 3", () => this.Data_GetTemperature(() => this.sensor_data.fans[2].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Quadro.sensor_data>("QuadroDevice"+index+".Fans3", "Quadro "+index+" Fan 4", () => this.Data_GetTemperature(() => this.sensor_data.fans[3].speed, 1.0f)));

            // Value for this sensor should be checked!
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Quadro.sensor_data>("QuadroDevice"+index+".Flow", "Quadro "+index+" Flow Sensor", () => this.Data_GetTemperature(() => this.sensor_data.flow)));

            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Quadro.sensor_data>("QuadroDevice"+index+".Temp0", "Quadro "+index+" Temperature 1", () => this.Data_GetTemperature(() => this.sensor_data.temperatures[0])));
            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Quadro.sensor_data>("QuadroDevice"+index+".Temp1", "Quadro "+index+" Temperature 2", () => this.Data_GetTemperature(() => this.sensor_data.temperatures[1])));
            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Quadro.sensor_data>("QuadroDevice"+index+".Temp2", "Quadro "+index+" Temperature 3", () => this.Data_GetTemperature(() => this.sensor_data.temperatures[2])));
            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Quadro.sensor_data>("QuadroDevice"+index+".Temp3", "Quadro "+index+" Temperature 4", () => this.Data_GetTemperature(() => this.sensor_data.temperatures[3])));
            
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Quadro.Settings>("QuadroDevice"+index+".Power0", "Quadro "+index+" Controller Power 1", 
                () => this.Settings_GetControllerPower(0), null,
                (x) => this.Settings_SetControllerPower(0, x),
                () => this.Settings_ResetControllerPower(0)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Quadro.Settings>("QuadroDevice"+index+".Power1", "Quadro "+index+" Controller Power 2",
                () => this.Settings_GetControllerPower(1), null,
                (x) => this.Settings_SetControllerPower(1, x),
                () => this.Settings_ResetControllerPower(1)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Quadro.Settings>("QuadroDevice"+index+".Power2", "Quadro "+index+" Controller Power 3",
                () => this.Settings_GetControllerPower(2), null,
                (x) => this.Settings_SetControllerPower(2, x),
                () => this.Settings_ResetControllerPower(2)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Quadro.Settings>("QuadroDevice"+index+".Power3", "Quadro "+index+" Controller Power 4",
                () => this.Settings_GetControllerPower(3), null,
                (x) => this.Settings_SetControllerPower(3, x),
                () => this.Settings_ResetControllerPower(3)));
        }

        public void Unload()
        {
            _logger.Log($"QuadroDevice.Unload()");
            hidDevice?.CloseDevice();
            hidDevice = null;
        }

        public void Update()
        {
            //_logger.Log($"QuadroDevice.Update() (hidDevice: {hidDevice}, is open: {hidDevice.IsOpen}");
            if (hidDevice == null)
                return;

            var deviceData = hidDevice.Read(500);

            if (deviceData != null && deviceData.Status == HidDeviceData.ReadStatus.Success)
            {
                int offset = 0;
                EndianAttribute.GetStructAtOffset<AquacomputerStructs.Common.device_header>(deviceData.Data, ref offset);
                EndianAttribute.GetStructAtOffset<AquacomputerStructs.Devices.Quadro.device_status>(deviceData.Data, ref offset);
                sensor_data = EndianAttribute.GetStructAtOffset<AquacomputerStructs.Devices.Quadro.sensor_data>(deviceData.Data, ref offset);
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

        private float? Settings_GetControllerPower(int index)
        {
            //_logger.Log($"QuadroDevice.Settings_GetControllerPower(index: {index})");
            if (hidDevice == null)
                return null;

            try
            {
                if (this.current_settings == null)
                    return null;
                
                return this.current_settings?.controller[index].power / 100.0f;
            }
            catch (ApplicationException e)
            {
                _logger.Log("Settings_GetControllerPower() ApplicationException: " + e);
            }
            return null;
        }

        private bool Settings_UpdateSettings(bool force = false)
        {
            //_logger.Log($"QuadroDevice.Settings_UpdateSettings()");
            int offset = 1;

            if (this.current_settings != null && !force &&
                this.last_settings_read + this.settings_timeout >= DateTime.Now)
                return true;

            // Read current settings
            if (hidDevice.ReadFeatureData(out byte[] data, 3))
            {
                this.current_settings = EndianAttribute.GetStructAtOffset<AquacomputerStructs.Devices.Quadro.Settings>(data, ref offset);
                if (this.initial_settings == null)
                    this.initial_settings = current_settings;

                this.last_settings_read = DateTime.Now;
                return true;
            } 
            else
            {
                _logger.Log("QuadroDevice: Failed to read settings from device.");
                //Debugger.Launch();
                return false;
            }
        }

        private void Settings_SetControllerPower(int index, float? val)
        {
            //_logger.Log($"QuadroDevice.Settings_SetControllerPower(index: {index}, val: {val})");
            if (hidDevice == null)
                return;

            byte[] data;

            // Update current settings if never read
            if (this.current_settings == null)
                if (!Settings_UpdateSettings())
                    return;

            if (val != null)
            {
                var calc_val = (short)(Math.Round(val ?? 0, 2) * 100);
                if (calc_val == this.current_settings?.controller[index].power)
                    return;

                this.current_settings.Value.controller[index].mode = 0; // PWM_MODE
                this.current_settings.Value.controller[index].power = calc_val;
            }

            // Write new config
            var reportId = new byte[] { 0x03 };
            byte[] reportdata = EndianAttribute.StructToBytes<AquacomputerStructs.Devices.Quadro.Settings>(this.current_settings.Value);
            var crc = new Crc.CrcBase(Crc.CrcParameters.Crc16_USB).ComputeHash(reportdata);
            data = reportId.Concat(reportdata).Concat(crc.Reverse()).ToArray();

            if (hidDevice.WriteFeatureData(data))
            {
                data = new byte[] { 0x2, 0, 0, 0, 0x2, 0, 0, 0, 0, 0x34, 0xc6 };
                hidDevice.Write(data);
            }

            Thread.Sleep(100);

            // Read current settings
            Settings_UpdateSettings(true);
        }

        private void Settings_ResetControllerPower(int index)
        {
            //_logger.Log($"QuadroDevice.Settings_ResetControllerPower(index: {index})");
            if (hidDevice == null)
                return;

            if (this.current_settings != null && this.initial_settings != null)
                this.current_settings.Value.controller[index] = this.initial_settings.Value.controller[index];
            else if (this.initial_settings != null)
                this.current_settings = this.initial_settings;
            
            Settings_SetControllerPower(index, null);
        }
    }
}
