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
        private const int CTRL_REPORT_LENGTH = 0x329;
        private const int CTRL_PUMP = 0x96;
        private const int CTRL_FAN  = 0x41;
        private const int CTRL_TYPE = 0x00; // mode: 0 = fixed percent
        private const int CTRL_PCT  = 0x01; // BE16 centi-percent

        // How many bytes of the channel control block we snapshot/restore.
        // type (1) + percent (2) is enough to leave curve/preset data intact
        // and switch mode back; we also keep a slightly larger window in case
        // firmware stores extra flags next to the mode byte.
        private const int CHANNEL_SNAPSHOT_LEN = 16;

        private HidSharp.HidDevice hidDevice = null;
        private CachedHidStream hidStream = null;
        private IPluginLogger _logger;

        private byte[] lastStatus = null;
        private int baseAdj = 0;
        private bool loggedOnce = false;

        private int? appliedPumpDuty = null;
        private int? appliedFanDuty = null;

        // Saved before FanControl first takes over each channel
        private byte[] savedPumpChannel = null;
        private byte[] savedFanChannel = null;
        private bool pumpSnapshotTaken = false;
        private bool fanSnapshotTaken = false;

        private readonly object ctrlLock = new object();
        private int writeInProgress = 0;

        // When HID stops answering, clear lastStatus so FanControl does not keep
        // showing the last coolant/RPM (same idea as LianLi bb769a3). Retry open
        // at most every 30s (LianLi 9259cd3 timing).
        private DateTime nextReopenUtc = DateTime.MinValue;
        private const int ReopenIntervalSeconds = 30;

        public int GetProductId() => 0xF00E;
        public string GetDevicePath() => hidDevice?.DevicePath;

        public IAquacomputerDevice AssignDevice(HidSharp.HidDevice device, IPluginLogger logger)
        {
            _logger = logger;
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

            container.FanSensors.Add(new DeviceBaseSensor<object>(
                p + ".PumpSpeed", "D5 Next " + index + " Pump Speed",
                () => AsU16(OFF_PUMP + G_SPEED, 1f)));
            container.FanSensors.Add(new DeviceBaseSensor<object>(
                p + ".FanSpeed", "D5 Next " + index + " Fan Speed",
                () => AsU16(OFF_FAN + G_SPEED, 1f)));


            // Auto-pair like lewisgibson/FanControl.LianLi:
            // PairedFanSensorId MUST be plugin.Name + "/" + fanSensor.Id
            const string pluginName = "Aquacomputer Devices";
            string pumpSpeedId = p + ".PumpSpeed";
            string fanSpeedId = p + ".FanSpeed";
            string pumpPaired = pluginName + "/" + pumpSpeedId;
            string fanPaired = pluginName + "/" + fanSpeedId;
container.ControlSensors.Add(new DeviceBaseControlSensor<object>(
                p + ".PumpDuty", "D5 Next " + index + " Pump Duty",
                () => appliedPumpDuty, null,
                v => SetDuty(true, v),
                () => SetDuty(true, null),
                pairedFanSensorId: pumpPaired));
            container.ControlSensors.Add(new DeviceBaseControlSensor<object>(
                p + ".FanDuty", "D5 Next " + index + " Fan Duty",
                () => appliedFanDuty, null,
                v => SetDuty(false, v),
                () => SetDuty(false, null),
                pairedFanSensorId: fanPaired));
        }

        public void Unload()
        {
            // Stop accepting new writes and wait briefly for any in-flight
            // ThreadPool write/restore so we don't NRE on a closed stream.
            Interlocked.Exchange(ref writeInProgress, 1);
            try
            {
                // Best-effort restore while the stream is still open
                if (appliedPumpDuty != null) RestoreChannel(true);
                if (appliedFanDuty != null) RestoreChannel(false);
            }
            catch (Exception ex)
            {
                _logger.Log("D5NextDevice.Unload restore: " + ex.Message);
            }

            lock (ctrlLock)
            {
                try { if (hidStream != null) hidStream.Close(); } catch { }
                hidStream = null;
                hidDevice = null;
                lastStatus = null;
                savedPumpChannel = null;
                savedFanChannel = null;
                pumpSnapshotTaken = false;
                fanSnapshotTaken = false;
                appliedPumpDuty = null;
                appliedFanDuty = null;
            }

            Interlocked.Exchange(ref writeInProgress, 0);
        }

        public void Update()
        {
            if (hidDevice == null)
                return;

            try
            {
                if (hidStream == null || !hidStream.CanRead)
                {
                    // Device gone / reader thread dead — do not freeze last values.
                    lastStatus = null;
                    if (!TryReopenStream())
                        return;
                    if (hidStream == null || !hidStream.CanRead)
                        return;
                }

                byte[] data = hidStream.Read();
                if (data == null || data.Length < 20)
                {
                    lastStatus = null;
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
                lastStatus = null;
                if (_logger != null)
                    _logger.Log("D5NextDevice.Update exception: " + ex.Message);
                // Allow reopen attempts after the interval.
                try { hidStream?.Close(); } catch { }
                hidStream = null;
            }
        }

        /// <summary>
        /// Best-effort reopen of the status stream. Rate-limited to once per
        /// <see cref="ReopenIntervalSeconds"/> so a missing device does not spam Open().
        /// </summary>
        private bool TryReopenStream()
        {
            if (hidDevice == null)
                return false;

            DateTime now = DateTime.UtcNow;
            if (now < nextReopenUtc)
                return false;
            nextReopenUtc = now.AddSeconds(ReopenIntervalSeconds);

            try
            {
                try { if (hidStream != null) hidStream.Close(); } catch { }
                hidStream = null;

                var stream = hidDevice.Open();
                stream.ReadTimeout = 2000;
                hidStream = new CachedHidStream(stream);
                return true;
            }
            catch (Exception ex)
            {
                hidStream = null;
                if (_logger != null)
                    _logger.Log("D5NextDevice.TryReopen failed: " + ex.Message);
                return false;
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

            // FanControl released control — restore previous mode/settings
            if (percent == null)
            {
                // Clear applied flag first so Unload won't double-restore
                if (isPump) appliedPumpDuty = null;
                else appliedFanDuty = null;

                if (Interlocked.CompareExchange(ref writeInProgress, 1, 0) != 0)
                    return;

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try { RestoreChannel(isPump); }
                    catch (Exception ex) { _logger.Log("D5NextDevice.SetDuty restore: " + ex.Message); }
                    finally { Interlocked.Exchange(ref writeInProgress, 0); }
                });
                return;
            }

            int duty = (int)Math.Round(Math.Max(0f, Math.Min(100f, percent.Value)));

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

            if (Interlocked.CompareExchange(ref writeInProgress, 1, 0) != 0)
                return;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { WriteDutyBlocking(isPump, duty); }
                catch (Exception ex) { _logger.Log("D5NextDevice.SetDuty write: " + ex.Message); }
                finally { Interlocked.Exchange(ref writeInProgress, 0); }
            });
        }

        private byte[] AllocCtrlBuffer()
        {
            int len = CTRL_REPORT_LENGTH;
            try
            {
                int maxF = hidDevice.GetMaxFeatureReportLength();
                if (maxF > 0)
                    len = maxF;
            }
            catch { }
            if (len < CTRL_REPORT_LENGTH)
                len = CTRL_REPORT_LENGTH;

            byte[] data = new byte[len];
            data[0] = CTRL_REPORT_ID;
            return data;
        }

        private void SnapshotChannelIfNeeded(bool isPump, byte[] data)
        {
            int baseOff = isPump ? CTRL_PUMP : CTRL_FAN;
            if (baseOff + CHANNEL_SNAPSHOT_LEN > data.Length)
                return;

            if (isPump && !pumpSnapshotTaken)
            {
                savedPumpChannel = new byte[CHANNEL_SNAPSHOT_LEN];
                Array.Copy(data, baseOff, savedPumpChannel, 0, CHANNEL_SNAPSHOT_LEN);
                pumpSnapshotTaken = true;
            }
            else if (!isPump && !fanSnapshotTaken)
            {
                savedFanChannel = new byte[CHANNEL_SNAPSHOT_LEN];
                Array.Copy(data, baseOff, savedFanChannel, 0, CHANNEL_SNAPSHOT_LEN);
                fanSnapshotTaken = true;
            }
        }

        private void WriteDutyBlocking(bool isPump, int dutyPercent)
        {
            lock (ctrlLock)
            {
                try
                {
                    if (hidStream == null || hidDevice == null)
                    {
                        _logger.Log("D5NextDevice.WriteDuty: stream closed, skip");
                        return;
                    }

                    int centi = dutyPercent * 100;
                    int baseOff = isPump ? CTRL_PUMP : CTRL_FAN;

                    byte[] data = AllocCtrlBuffer();

                    try { hidStream.GetFeature(data); }
                    catch (Exception gex)
                    {
                        _logger.Log("D5NextDevice.GetFeature failed: " + gex.Message);
                        return;
                    }

                    if (baseOff + CTRL_PCT + 1 >= data.Length)
                    {
                        _logger.Log("D5NextDevice: control offset out of range (len=" + data.Length + ")");
                        return;
                    }

                    // Remember original mode/settings the first time we take over
                    SnapshotChannelIfNeeded(isPump, data);

                    data[baseOff + CTRL_TYPE] = 0x00; // fixed percent
                    data[baseOff + CTRL_PCT]     = (byte)((centi >> 8) & 0xFF);
                    data[baseOff + CTRL_PCT + 1] = (byte)(centi & 0xFF);

                    if (!WriteCtrlReport(data))
                        return;

                    if (isPump) appliedPumpDuty = dutyPercent;
                    else appliedFanDuty = dutyPercent;

                    // Successful duty changes are normal operation. Do not log every
                    // curve adjustment to FanControl's service_log.txt.
                }
                catch (Exception ex)
                {
                    _logger.Log("D5NextDevice.WriteDuty exception: " + ex);
                }
            }
        }

        private void RestoreChannel(bool isPump)
        {
            lock (ctrlLock)
            {
                try
                {
                    // Stream may already be gone (Unload raced with async restore)
                    if (hidStream == null || hidDevice == null)
                    {
                        _logger.Log("D5NextDevice.Restore: stream closed, skip");
                        return;
                    }

                    byte[] saved = isPump ? savedPumpChannel : savedFanChannel;
                    bool taken = isPump ? pumpSnapshotTaken : fanSnapshotTaken;
                    if (!taken || saved == null)
                    {
                        return;
                    }

                    int baseOff = isPump ? CTRL_PUMP : CTRL_FAN;
                    byte[] data = AllocCtrlBuffer();

                    try { hidStream.GetFeature(data); }
                    catch (Exception gex)
                    {
                        _logger.Log("D5NextDevice.Restore GetFeature failed: " + gex.Message);
                        return;
                    }

                    if (baseOff + saved.Length > data.Length)
                    {
                        _logger.Log("D5NextDevice.Restore: buffer too small");
                        return;
                    }

                    Array.Copy(saved, 0, data, baseOff, saved.Length);

                    if (!WriteCtrlReport(data))
                        return;

                    // Allow a fresh snapshot next time FanControl takes over
                    if (isPump)
                    {
                        pumpSnapshotTaken = false;
                        savedPumpChannel = null;
                        appliedPumpDuty = null;
                    }
                    else
                    {
                        fanSnapshotTaken = false;
                        savedFanChannel = null;
                        appliedFanDuty = null;
                    }

                    // Successful restore is normal; rate-limited logger would still
                    // allow first occurrence — skip routine success entirely.
                }
                catch (Exception ex)
                {
                    _logger.Log("D5NextDevice.Restore exception: " + ex);
                }
            }
        }

        private bool WriteCtrlReport(byte[] data)
        {
            // Must be called under ctrlLock. Guard against Unload racing us.
            if (hidStream == null)
            {
                _logger.Log("D5NextDevice.WriteCtrlReport: stream is null, skip");
                return false;
            }

            int reportLen = CTRL_REPORT_LENGTH;
            if (data.Length < reportLen)
                reportLen = data.Length;

            int crcCount = reportLen - 3;
            if (crcCount > 0)
            {
                byte[] body = new byte[crcCount];
                Array.Copy(data, 1, body, 0, crcCount);
                byte[] crc = new Crc.CrcBase(Crc.CrcParameters.Crc16_USB).ComputeHash(body);
                int crcOff = reportLen - 2;
                if (crcOff + 1 < data.Length)
                {
                    data[crcOff]     = crc[1];
                    data[crcOff + 1] = crc[0];
                }
            }

            try
            {
                hidStream.SetFeature(data);
            }
            catch (Exception ex)
            {
                _logger.Log("D5NextDevice.SetFeature failed: " + ex.Message);
                return false;
            }

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
            return true;
        }
    }
}
