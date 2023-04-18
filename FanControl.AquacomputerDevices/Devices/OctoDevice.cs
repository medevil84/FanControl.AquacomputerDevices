using AquacomputerStructs.Helpers;
using FanControl.Plugins;
using HidLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FanControl.AquacomputerDevices.Devices
{
    /// <summary>
    /// This class is EXPERIMENTAL, please use at your own risk.
    /// </summary>
    internal class OctoDevice : IAquacomputerDevice
    {
        private HidDevice hidDevice = null;
        private IPluginLogger _logger;
        private ReaderWriterLock rwl;
        private AquacomputerStructs.Devices.Octo.Settings? initial_settings = null;
        private AquacomputerStructs.Devices.Octo.Settings? current_settings = null;
        private AquacomputerStructs.Devices.Octo.sensor_data sensor_data;
        private DateTime last_settings_read = DateTime.MinValue;
        private readonly TimeSpan settings_timeout = new TimeSpan(0, 5, 0); // every 5 minutes

        public int GetProductId() => 0xF011;

        public IAquacomputerDevice AssignDevice(HidDevice device, IPluginLogger logger)
        {
            _logger = logger;
            _logger.Log($"OctoDevice.AssignDevice(device: {device}, logger: {logger})");
            if (hidDevice == null)
            {
                hidDevice = device;
                rwl = new ReaderWriterLock();

                hidDevice.OpenDevice();
                Update();
            }
            return this;
        }

        public void Load(IPluginSensorsContainer _container)
        {
            _logger.Log($"OctoDevice.Load(_container: {_container})");
            if (hidDevice == null)
                return;

            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice.Fans0", "Octo Fan 1", () => this.Data_GetTemperature(() => this.sensor_data.fans[0].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice.Fans1", "Octo Fan 2", () => this.Data_GetTemperature(() => this.sensor_data.fans[1].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice.Fans2", "Octo Fan 3", () => this.Data_GetTemperature(() => this.sensor_data.fans[2].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice.Fans3", "Octo Fan 4", () => this.Data_GetTemperature(() => this.sensor_data.fans[3].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice.Fans4", "Octo Fan 5", () => this.Data_GetTemperature(() => this.sensor_data.fans[4].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice.Fans5", "Octo Fan 6", () => this.Data_GetTemperature(() => this.sensor_data.fans[5].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice.Fans6", "Octo Fan 7", () => this.Data_GetTemperature(() => this.sensor_data.fans[6].speed, 1.0f)));
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice.Fans7", "Octo Fan 8", () => this.Data_GetTemperature(() => this.sensor_data.fans[7].speed, 1.0f)));

            // Value for this sensor should be checked!
            _container.FanSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice.Flow", "Octo Flow Sensor", () => this.Data_GetTemperature(() => this.sensor_data.flow)));

            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice.Temp0", "Octo Temperature 1", () => this.Data_GetTemperature(() => this.sensor_data.temperatures[0])));
            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice.Temp1", "Octo Temperature 2", () => this.Data_GetTemperature(() => this.sensor_data.temperatures[1])));
            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice.Temp2", "Octo Temperature 3", () => this.Data_GetTemperature(() => this.sensor_data.temperatures[2])));
            _container.TempSensors.Add(new DeviceBaseSensor<AquacomputerStructs.Devices.Octo.sensor_data>("OctoDevice.Temp3", "Octo Temperature 4", () => this.Data_GetTemperature(() => this.sensor_data.temperatures[3])));

            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Octo.Settings>("OctoDevice.Power0", "Octo Controller Power 1", 
                () => this.Settings_GetControllerPower(0), null,
                (x) => this.Settings_SetControllerPower(0, x),
                () => this.Settings_ResetControllerPower(0)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Octo.Settings>("OctoDevice.Power1", "Octo Controller Power 2",
                () => this.Settings_GetControllerPower(1), null,
                (x) => this.Settings_SetControllerPower(1, x),
                () => this.Settings_ResetControllerPower(1)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Octo.Settings>("OctoDevice.Power2", "Octo Controller Power 3",
                () => this.Settings_GetControllerPower(2), null,
                (x) => this.Settings_SetControllerPower(2, x),
                () => this.Settings_ResetControllerPower(2)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Octo.Settings>("OctoDevice.Power3", "Octo Controller Power 4",
                () => this.Settings_GetControllerPower(3), null,
                (x) => this.Settings_SetControllerPower(3, x),
                () => this.Settings_ResetControllerPower(3)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Octo.Settings>("OctoDevice.Power4", "Octo Controller Power 5",
                () => this.Settings_GetControllerPower(4), null,
                (x) => this.Settings_SetControllerPower(4, x),
                () => this.Settings_ResetControllerPower(4)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Octo.Settings>("OctoDevice.Power5", "Octo Controller Power 6",
                () => this.Settings_GetControllerPower(5), null,
                (x) => this.Settings_SetControllerPower(5, x),
                () => this.Settings_ResetControllerPower(5)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Octo.Settings>("OctoDevice.Power6", "Octo Controller Power 7",
                () => this.Settings_GetControllerPower(6), null,
                (x) => this.Settings_SetControllerPower(6, x),
                () => this.Settings_ResetControllerPower(6)));
            _container.ControlSensors.Add(new DeviceBaseControlSensor<AquacomputerStructs.Devices.Octo.Settings>("OctoDevice.Power7", "Octo Controller Power 8",
                () => this.Settings_GetControllerPower(7), null,
                (x) => this.Settings_SetControllerPower(7, x),
                () => this.Settings_ResetControllerPower(7)));
        }

        public void Unload()
        {
            _logger.Log($"OctoDevice.Unload()");
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
                    EndianAttribute.GetStructAtOffset<AquacomputerStructs.Devices.Octo.device_status>(deviceData.Data, ref offset);
                    sensor_data = EndianAttribute.GetStructAtOffset<AquacomputerStructs.Devices.Octo.sensor_data>(deviceData.Data, ref offset);
                }

                Settings_UpdateSettings();
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
            //_logger.Log($"OctoDevice.Data_GetReadLockLambda(x: {x.Method})");
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



        private float? Settings_GetControllerPower(int index)
        {
            //_logger.Log($"QuadroDevice.Settings_GetControllerPower(index: {index})");
            if (hidDevice == null)
                return null;

            try
            {
                if (this.current_settings == null)
                    return null;

                rwl.AcquireReaderLock(100);
                try
                {
                    return this.current_settings?.controller[index].power / 100.0f;
                }
                finally
                {
                    rwl.ReleaseReaderLock();
                }
            }
            catch (ApplicationException) { }
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
            if (hidDevice.ReadFeatureData(out byte[] data, 3))
            {
                this.current_settings = EndianAttribute.GetStructAtOffset<AquacomputerStructs.Devices.Octo.Settings>(data, ref offset);
                if (this.initial_settings == null)
                    this.initial_settings = current_settings;

                this.last_settings_read = DateTime.Now;
                return true;
            }
            else
            {
                _logger.Log("OctoDevice: Failed to read settings from device.");
                //Debugger.Launch();
                return false;
            }
        }

        private void Settings_SetControllerPower(int index, float? val)
        {
            //_logger.Log($"OctoDevice.Settings_SetControllerPower(index: {index}, val: {val})");
            if (hidDevice == null)
                return;

            rwl.AcquireWriterLock(100);
            try
            {
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

                if (hidDevice.WriteFeatureData(data))
                {
                    data = new byte[] { 0x2, 0, 0, 0, 0x2, 0, 0, 0, 0, 0x34, 0xc6 };
                    hidDevice.Write(data);
                }

                Thread.Sleep(100);

                // Read current settings
                Settings_UpdateSettings(true);
            }
            finally
            {
                rwl.ReleaseWriterLock();
            }
        }

        private void Settings_ResetControllerPower(int index)
        {
            //_logger.Log($"OctoDevice.Settings_ResetControllerPower(index: {index})");
            if (hidDevice == null)
                return;

            rwl.AcquireWriterLock(100);
            try
            {
                if (this.current_settings != null && this.initial_settings != null)
                    this.current_settings.Value.controller[index] = this.initial_settings.Value.controller[index];
                else if (this.initial_settings != null)
                    this.current_settings = this.initial_settings;
            }
            finally
            {
                rwl.ReleaseWriterLock();
            }
            Settings_SetControllerPower(index, null);
        }
    }
}
