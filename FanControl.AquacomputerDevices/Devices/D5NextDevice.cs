using AquacomputerStructs.Helpers;
using FanControl.Plugins;
using System;
using System.Text;
using System.Threading;

namespace FanControl.AquacomputerDevices.Devices
{
    /// <summary>
    /// Aquacomputer D5 Next (VID 0x0C70, PID 0xF00E)
    /// </summary>
    internal class D5NextDevice : IAquacomputerDevice
    {
        private const int OFF_COOLANT = 0x57;
        private const int OFF_PUMP    = 0x6C;
        private const int OFF_FAN     = 0x5F;
        private const int OFF_5V      = 0x39;
        private const int OFF_12V     = 0x37;
        private const int OFF_VIRT0   = 0x3F;

        private const int G_VOLT  = 0x02;
        private const int G_POWER = 0x06;
        private const int G_SPEED = 0x08;

        private const byte CTRL_REPORT_ID = 0x03;
        // liquidctl uses 0x329 as the feature report length argument (includes report id at [0])
        private const int CTRL_REPORT_LENGTH = 0x329;
        private const int CTRL_PUMP = 0x96;
        private const int CTRL_FAN  = 0x41;
        private const int CTRL_TYPE = 0x00; // 0 = fixed percent
        private const int CTRL_PCT  = 0x01; // BE16 centi-percent

        private HidSharp.HidDevice hidDevice = null;
        private CachedHidStream hidStream = null;
        private IPluginLogger _logger;

        private byte[] lastStatus = null;
        private int baseAdj = 0;
        private bool loggedOnce = false;

        // Last successfully applied duties (0-100). null = not under our control.
        private int? appliedPumpDuty = null;
        private int? appliedFanDuty = null;

        // Serialize control writes; avoid overlapping Get/SetFeature from FanControl's 1 Hz updates
        private readonly object ctrlLock = new object();
        private int writeInProgress = 0;

        public int GetProductId() => 0xF00E;
        public string GetDevicePath() => hidDevice?.DevicePath;

        public IAquacomputerDevice AssignDevice(HidSharp.HidDevice device, IPluginLogger logger)
        {
            _logger = logger;
            _logger.Log("D5NextDevice.AssignDevice path=" + (device != null ? device.DevicePath : "null"));
            if (hidDevice == null)
            {
                hidDevice = device;
                try
                {
                    var stream = hidDevice.Open();
                    stream.ReadTimeout = 2000;
                    hidStream = new CachedHidStream(stream);
                    Update();
                }
                catch (Exception ex)
                {
                    _logger.Log("D5NextDevice.AssignDevice open failed: " + ex);
                }
            }
            return this;
        }

        public void Load(IPluginSensorsContainer container, int index = 0)
        {
            _logger.Log("D5NextDevice.Load index=" + index);
            if (hidDevice == null)
                return;

            string p = "D5Next" + index;

            container.TempSensors.Add(new DeviceBaseSensor<object>(
                p + ".CoolantTemp", "D5 Next " + index + " Coolant Temperature",
                () => AsTemp(OFF_COOLANT)));

            for (int i = 0; i < 8; i++)
            {
                int off = OFF_VIRT0 + i * 2;
                int n = i + 1;
                container.TempSensors.Add(new DeviceBaseSensor<object>(
                    p + ".Soft" + n, "D5 Next " + index + " Soft Sensor " + n,
                    () => AsTemp(off)));
            }

            // Pair these with the Duty controls
            container.FanSensors.Add(new DeviceBaseSensor<object>(
                p + ".PumpSpeed", "D5 Next " + index + " Pump Speed",
                () => AsU16(OFF_PUMP + G_SPEED, 1f)));
            container.FanSensors.Add(new DeviceBaseSensor<object>(
                p + ".FanSpeed", "D5 Next " + index + " Fan Speed",
                () => AsU16(OFF_FAN + G_SPEED, 1f)));

            // Extra metrics (UI still shows "RPM" — FanControl limitation)
            container.FanSensors.Add(new DeviceBaseSensor<object>(
                p + ".PumpPower", "D5 Next " + index + " Pump Power W",
                () => AsU16(OFF_PUMP + G_POWER, 100f)));
            container.FanSensors.Add(new DeviceBaseSensor<object>(
                p + ".FanPower", "D5 Next " + index + " Fan Power W",
                () => AsU16(OFF_FAN + G_POWER, 100f)));
            container.FanSensors.Add(new DeviceBaseSensor<object>(
                p + ".PumpVoltage", "D5 Next " + index + " Pump Voltage V",
                () => AsU16(OFF_PUMP + G_VOLT, 100f)));
            container.FanSensors.Add(new DeviceBaseSensor<object>(
                p + ".FanVoltage", "D5 Next " + index + " Fan Voltage V",
                () => AsU16(OFF_FAN + G_VOLT, 100f)));
            container.FanSensors.Add(new DeviceBaseSensor<object>(
                p + ".Rail5V", "D5 Next " + index + " Rail +5V",
                () => AsU16(OFF_5V, 100f)));
            container.FanSensors.Add(new DeviceBaseSensor<object>(
                p + ".Rail12V", "D5 Next " + index + " Rail +12V",
                () => AsU16(OFF_12V, 100f)));

            container.ControlSensors.Add(new DeviceBaseControlSensor<object>(
                p + ".PumpDuty", "D5 Next " + index + " Pump Duty",
                () => appliedPumpDuty, null,
                v => SetDuty(true, v),
                () => SetDuty(true, null)));
            container.ControlSensors.Add(new DeviceBaseControlSensor<object>(
                p + ".FanDuty", "D5 Next " + index + " Fan Duty",
                () => appliedFanDuty, null,
                v => SetDuty(false, v),
                () => SetDuty(false, null)));
        }

        public void Unload()
        {
            _logger.Log("D5NextDevice.Unload");
            try { if (hidStream != null) hidStream.Close(); } catch { }
            hidStream = null;
            hidDevice = null;
            lastStatus = null;
        }

        public void Update()
        {
            if (hidDevice == null || hidStream == null)
                return;

            try
            {
                if (!hidStream.CanRead)
                {
                    if (!loggedOnce)
                    {
                        _logger.Log("D5NextDevice.Update: CanRead=false");
                        loggedOnce = true;
                    }
                    return;
                }

                byte[] data = hidStream.Read();
                if (data == null || data.Length < 20)
                {
                    if (!loggedOnce)
                    {
                        _logger.Log("D5NextDevice.Update: short buffer len=" + (data == null ? -1 : data.Length));
                        loggedOnce = true;
                    }
                    return;
                }

                baseAdj = (data[0] == 0x01) ? 0 : -1;
                lastStatus = data;

                if (!loggedOnce)
                {
                    var sb = new StringBuilder();
                    sb.Append("D5NextDevice first report: len=").Append(data.Length)
                      .Append(" baseAdj=").Append(baseAdj).Append(" head=");
                    int n = Math.Min(16, data.Length);
                    for (int i = 0; i < n; i++)
                        sb.Append(data[i].ToString("X2")).Append(' ');
                    _logger.Log(sb.ToString());
                    _logger.Log("D5NextDevice sample: coolant=" + AsTemp(OFF_COOLANT)
                        + " pumpRpm=" + AsU16(OFF_PUMP + G_SPEED, 1f));
                    loggedOnce = true;
                }
            }
            catch (Exception ex)
            {
                if (!loggedOnce)
                {
                    _logger.Log("D5NextDevice.Update exception: " + ex);
                    loggedOnce = true;
                }
            }
        }

        private int Abs(int documentedOffset) => documentedOffset + baseAdj;

        private float? AsTemp(int documentedOffset)
        {
            short? raw = ReadI16(documentedOffset);
            if (raw == null) return null;
            if (raw.Value == short.MaxValue || (ushort)raw.Value == 0x7FFF) return null;
            return raw.Value / 100.0f;
        }

        private float? AsU16(int documentedOffset, float div)
        {
            short? raw = ReadI16(documentedOffset);
            if (raw == null) return null;
            if (raw.Value == short.MaxValue) return 0f;
            return ((ushort)raw.Value) / div;
        }

        private short? ReadI16(int documentedOffset)
        {
            if (lastStatus == null) return null;
            int o = Abs(documentedOffset);
            if (o < 0 || o + 1 >= lastStatus.Length) return null;
            return (short)((lastStatus[o] << 8) | lastStatus[o + 1]);
        }

        private void SetDuty(bool isPump, float? percent)
        {
            if (hidDevice == null || hidStream == null)
                return;

            // Reset path — do not touch hardware (avoid freeze / mode fights)
            if (percent == null)
            {
                if (isPump) appliedPumpDuty = null;
                else appliedFanDuty = null;
                return;
            }

            int duty = (int)Math.Round(Math.Max(0f, Math.Min(100f, percent.Value)));

            // FanControl calls Set() every second while a curve is active.
            // Only write to the device when the value actually changed.
            if (isPump)
            {
                if (appliedPumpDuty.HasValue && appliedPumpDuty.Value == duty)
                    return;
            }
            else
            {
                if (appliedFanDuty.HasValue && appliedFanDuty.Value == duty)
                    return;
            }

            // Prevent overlapping blocking HID feature ops (this was freezing the UI)
            if (Interlocked.CompareExchange(ref writeInProgress, 1, 0) != 0)
                return;

            // Fire-and-forget on a background thread so FanControl UI stays responsive
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    WriteDutyBlocking(isPump, duty);
                }
                finally
                {
                    Interlocked.Exchange(ref writeInProgress, 0);
                }
            });
        }

        private void WriteDutyBlocking(bool isPump, int dutyPercent)
        {
            lock (ctrlLock)
            {
                try
                {
                    int centi = dutyPercent * 100;
                    int baseOff = isPump ? CTRL_PUMP : CTRL_FAN;

                    // Buffer size: report id + body. HidSharp wants report ID in [0].
                    int len = CTRL_REPORT_LENGTH;
                    try
                    {
                        int maxF = hidDevice.GetMaxFeatureReportLength();
                        if (maxF > 0)
                            len = maxF;
                    }
                    catch { }

                    // Ensure at least liquidctl size
                    if (len < CTRL_REPORT_LENGTH)
                        len = CTRL_REPORT_LENGTH;

                    byte[] data = new byte[len];
                    data[0] = CTRL_REPORT_ID;

                    // Read-modify-write so we don't wipe other settings
                    try
                    {
                        hidStream.GetFeature(data);
                    }
                    catch (Exception gex)
                    {
                        _logger.Log("D5NextDevice.GetFeature failed: " + gex.Message);
                        // Continue with mostly-zero buffer still risks bad config — abort
                        return;
                    }

                    if (baseOff + CTRL_PCT + 1 >= data.Length)
                    {
                        _logger.Log("D5NextDevice: control offset out of range (len=" + data.Length + ")");
                        return;
                    }

                    // Fixed percent mode
                    data[baseOff + CTRL_TYPE] = 0x00;
                    // BE16 centi-percent
                    data[baseOff + CTRL_PCT]     = (byte)((centi >> 8) & 0xFF);
                    data[baseOff + CTRL_PCT + 1] = (byte)(centi & 0xFF);

                    // CRC-16/USB over [1 .. report_length-3] inclusive (liquidctl)
                    // report_length here is CTRL_REPORT_LENGTH (0x329) as used by liquidctl
                    int reportLen = CTRL_REPORT_LENGTH;
                    if (data.Length < reportLen)
                        reportLen = data.Length;

                    int crcCount = reportLen - 3; // indices 1 .. reportLen-3
                    if (crcCount > 0)
                    {
                        byte[] body = new byte[crcCount];
                        Array.Copy(data, 1, body, 0, crcCount);
                        byte[] crc = new Crc.CrcBase(Crc.CrcParameters.Crc16_USB).ComputeHash(body);
                        int crcOff = reportLen - 2;
                        if (crcOff + 1 < data.Length)
                        {
                            // LE hash from CrcBase → store as BE on wire (same as Quadro .Reverse())
                            data[crcOff]     = crc[1];
                            data[crcOff + 1] = crc[0];
                        }
                    }

                    hidStream.SetFeature(data);

                    // Short pause then secondary apply report (official software / kernel)
                    Thread.Sleep(50);
                    try
                    {
                        byte[] secondary = { 0x02, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x34, 0xC6 };
                        hidStream.Write(secondary);
                    }
                    catch (Exception wex)
                    {
                        _logger.Log("D5NextDevice secondary write: " + wex.Message);
                    }

                    if (isPump) appliedPumpDuty = dutyPercent;
                    else appliedFanDuty = dutyPercent;

                    _logger.Log("D5NextDevice.WriteDuty " + (isPump ? "pump" : "fan")
                        + "=" + dutyPercent + "% centi=" + centi + " ok");
                }
                catch (Exception ex)
                {
                    _logger.Log("D5NextDevice.WriteDuty exception: " + ex);
                }
            }
        }
    }
}
