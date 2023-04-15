using AquacomputerStructs.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AquacomputerStructs.Devices.HighFlowNext
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct device_status
    {
        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U4)]
        public uint system_state;

        public byte feature_unlock;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U4)]
        public uint time;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U4)]
        public uint power_up_cnt;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U4)]
        public uint runtime_total;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U2)]
        public ushort last_key_press;

        public byte language;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct sensor_data
    {
        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public ushort[] adc_raw;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public short[] sensors;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] sensors_units;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U4)]
        public uint flow_raw_time;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short flow_calibration;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short power_sensor_auto_offset;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short sensor_diff;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short flow_uncompensated;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short flow;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short temperature_water;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short temperature_ext;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short water_quality;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short powerSensor;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short conductivity_uncompensate;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short conductivity;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short vcc5;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short vcc5_usb;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U4)]
        public uint volumeCounter;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U4)]
        public uint impulseCounter;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U4)]
        public uint counterTime;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U4)]
        public uint alarm_state;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U4)]
        public uint alarm_state_buffer;

        public byte profileId;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public short[] strip_power;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public short[] strip_power_scale;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] strip_scale;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public short[] debug1;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public short[] debug2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct alarm_settings
    {
        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U2)]
        public ushort flags;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short startTimeout;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short flowAlarm;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short temperatureAlarmWater;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short temperatureAlarmExt;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short waterQuality;

        public byte signalOutputMode;
    }
}
