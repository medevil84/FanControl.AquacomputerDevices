using AquacomputerStructs.Helpers;
using FanControl.Plugins;
using System;
using System.Linq;
using System.Threading;

namespace FanControl.AquacomputerDevices.Devices
{
    /// <summary>
    /// This class is EXPERIMENTAL, please use at your own risk.
    /// </summary>
    internal class OctoDevice : IAquacomputerDevice
    {
        private HidSharp.HidDevice hidDevice = null;
        private CachedHidStream hidStream = null;
        private IPluginLogger _logger;
        private AquacomputerStructs.Devices.Octo.Settings? initial_settings = null;
        private AquacomputerStructs.Devices.Octo.Settings? current_settings = null;
        private AquacomputerStructs.Devices.Octo.sensor_data sensor_data;
        private DateTime last_settings_read = DateTime.MinValue;
        private readonly TimeSpan settings_timeout = new TimeSpan(0, 5, 0); // every 5 minutes

        public int GetProductId() => 0xF011;
        public string GetDevicePath() => hidDevice?.DevicePath;

        public IAquacomputerDevice AssignDevice(HidSharp.HidDevice device, IPluginLogger logger)
        {
            _logger = logger;
            _logger.Log($"OctoDevice.AssignDevice(device: {device}, logger: {logger})");
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
            _logger.Log($"OctoDevice.Load(_container: {_container}, index = {index})");
            if (hidDevice == null)
                return;

            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice"+index+".Fans0", "Octo "+index+" Fan 1", () => this.Data_GetTemperature(() => this.sensor_data.fans[0].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice"+index+".Fans1", "Octo "+index+" Fan 2", () => this.Data_GetTemperature(() => this.sensor_data.fans[1].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice"+index+".Fans2", "Octo "+index+" Fan 3", () => this.Data_GetTemperature(() => this.sensor_data.fans[2].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice"+index+".Fans3", "Octo "+index+" Fan 4", () => this.Data_GetTemperature(() => this.sensor_data.fans[3].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice"+index+".Fans4", "Octo "+index+" Fan 5", () => this.Data_GetTemperature(() => this.sensor_data.fans[4].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice"+index+".Fans5", "Octo "+index+" Fan 6", () => this.Data_GetTemperature(() => this.sensor_data.fans[5].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice"+index+".Fans6", "Octo "+index+" Fan 7", () => this.Data_GetTemperature(() => this.sensor_data.fans[6].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice"+index+".Fans7", "Octo "+index+" Fan 8", () => this.Data_GetTemperature(() => this.sensor_data.fans[7].speed, 1.0f)));

            // Value for this sensor should be checked!
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice"+index+".Flow", "Octo "+index+" Flow Sensor", () => this.Data_GetTemperature(() => this.sensor_data.flow)));

            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice"+index+".Temp0", "Octo "+index+" Temperature 1", () => this.Data_GetTemperature(() => this.sensor_data.temperatures[0])));
            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice"+index+".Temp1", "Octo "+index+" Temperature 2", () => this.Data_GetTemperature(() => this.sensor_data.temperatures[1])));
            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice"+index+".Temp2", "Octo "+index+" Temperature 3", () => this.Data_GetTemperature(() => this.sensor_data.temperatures[2])));
            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice"+index+".Temp3", "Octo "+index+" Temperature 4", () => this.Data_GetTemperature(() => this.sensor_data.temperatures[3])));

            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Octo.Settings>("OctoDevice"+index+".Power0", "Octo Controller Power 1", 
                () => this.Settings_GetControllerPower(0), null,
                (x) => this.Settings_SetControllerPower(0, x),
                () => this.Settings_ResetControllerPower(0)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Octo.Settings>("OctoDevice"+index+".Power1", "Octo "+index+" Controller Power 2",
                () => this.Settings_GetControllerPower(1), null,
                (x) => this.Settings_SetControllerPower(1, x),
                () => this.Settings_ResetControllerPower(1)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Octo.Settings>("OctoDevice"+index+".Power2", "Octo "+index+" Controller Power 3",
                () => this.Settings_GetControllerPower(2), null,
                (x) => this.Settings_SetControllerPower(2, x),
                () => this.Settings_ResetControllerPower(2)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Octo.Settings>("OctoDevice"+index+".Power3", "Octo "+index+" Controller Power 4",
                () => this.Settings_GetControllerPower(3), null,
                (x) => this.Settings_SetControllerPower(3, x),
                () => this.Settings_ResetControllerPower(3)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Octo.Settings>("OctoDevice"+index+".Power4", "Octo "+index+" Controller Power 5",
                () => this.Settings_GetControllerPower(4), null,
                (x) => this.Settings_SetControllerPower(4, x),
                () => this.Settings_ResetControllerPower(4)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Octo.Settings>("OctoDevice"+index+".Power5", "Octo "+index+" Controller Power 6",
                () => this.Settings_GetControllerPower(5), null,
                (x) => this.Settings_SetControllerPower(5, x),
                () => this.Settings_ResetControllerPower(5)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Octo.Settings>("OctoDevice"+index+".Power6", "Octo "+index+" Controller Power 7",
                () => this.Settings_GetControllerPower(6), null,
                (x) => this.Settings_SetControllerPower(6, x),
                () => this.Settings_ResetControllerPower(6)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Octo.Settings>("OctoDevice."+index+"Power7", "Octo "+index+" Controller Power 8",
                () => this.Settings_GetControllerPower(7), null,
                (x) => this.Settings_SetControllerPower(7, x),
                () => this.Settings_ResetControllerPower(7)));
        }

        public void Unload()
        {
            _logger.Log($"OctoDevice.Unload()");
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
                EndianAttribute.GetStructAtOffset<AquacomputerStructs.Devices.Octo.device_status>(deviceData, ref offset);
                sensor_data = EndianAttribute.GetStructAtOffset<AquacomputerStructs.Devices.Octo.sensor_data>(deviceData, ref offset);
            }

            Settings_UpdateSettings();
        }

        private float? Data_GetTemperature(Func<short> temp, float ratio = 100.0f)
        {
            //_logger.Log($"OctoDevice.Data_GetReadLockLambda(x: {x.Method})");
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
            //_logger.Log($"OctoDevice.Settings_UpdateSettings()");
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

                this.current_settings = EndianAttribute.GetStructAtOffset<AquacomputerStructs.Devices.Octo.Settings>(data, ref offset);
                if (this.initial_settings == null)
                    this.initial_settings = current_settings;

                this.last_settings_read = DateTime.Now;
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log($"OctoDevice: Failed to read settings from device: {ex}");
                //Debugger.Launch();
                return false;
            }
        }

        private void Settings_SetControllerPower(int index, float? val)
        {
            //_logger.Log($"OctoDevice.Settings_SetControllerPower(index: {index}, val: {val})");
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
            byte[] reportdata = EndianAttribute.StructToBytes<AquacomputerStructs.Devices.Octo.Settings>(this.current_settings.Value);
            var crc = new Crc.CrcBase(Crc.CrcParameters.Crc16_USB).ComputeHash(reportdata);
            data = reportId.Concat(reportdata).Concat(crc.Reverse()).ToArray();

            try
            {
                hidStream.SetFeature(data);

                data = new byte[] { 0x2, 0, 0, 0, 0x2, 0, 0, 0, 0, 0x34, 0xc6 };
                hidStream.Write(data);
            }
            catch (Exception ex)
            {
                _logger.Log($"OctoDevice.Settings_SetControllerPower() error: {ex}");
            }

            Thread.Sleep(100);

            // Read current settings
            Settings_UpdateSettings(true);
        }

        private void Settings_ResetControllerPower(int index)
        {
            //_logger.Log($"OctoDevice.Settings_ResetControllerPower(index: {index})");
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
