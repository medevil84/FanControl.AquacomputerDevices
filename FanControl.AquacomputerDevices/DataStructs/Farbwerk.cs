using AquacomputerStructs.Helpers;
using System.Runtime.InteropServices;

namespace AquacomputerStructs.Devices.Farbwerk
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
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct sensor_data
    {
        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public ushort[] adc_raw;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public short[] temperatures;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public short[] sensors;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] sensors_units;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short vcc12;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short flow;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U4)]
        public uint alarm_state;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U4)]
        public uint alarm_state_last;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public short[] strip_power;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public short[] strip_power_scale;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] strip_scale;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U2)]
        public ushort strip_state;

        public byte profileId;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Sensor
    {
        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short offset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PidInfo
    {
        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short setpoint;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short p;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short i;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short d;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short d_tn;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short hysteresis;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U2)]
        public ushort flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CurveInfo
    {
        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short start_value;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public short[] input;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public short[] output;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Controller
    {
        public byte mode;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short power;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short source;

        public PidInfo pid;

        public CurveInfo curve;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StripConfigInfo
    {
        public byte brightness;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U2)]
        public ushort flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LedControllerSrcInfo
    {
        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short id;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] filter;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LedControllerBindingInfo
    {
        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short x1;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short x2;

        public byte y1;

        public byte y2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HsvColor
    {
        public const int MAX_HUE = 1535;
        public const int COLOR_RED = 0;
        public const int COLOR_ORANGE = 128;
        public const int COLOR_YELLOW = 256;
        public const int COLOR_GREEN = 512;
        public const int COLOR_AQUA = 768;
        public const int COLOR_BLUE = 1024;
        public const int COLOR_PURPLE = 1280;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U2)]
        public ushort h;
        public byte s;
        public byte v;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LedControllerInfo
    {
        public byte strip;

        public byte led_start;

        public byte count;

        public byte mode;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U2)]
        public ushort flags;

        public LedControllerSrcInfo src;

        public LedControllerBindingInfo binding1;

        public LedControllerBindingInfo binding2;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public short[] values;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public HsvColor[] hsv;
    }

    /// <summary>
    /// Report Id = 3 (append 2 byte crc after this struct)
    /// </summary>
    /// 
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Settings
    {
        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U2)]
        public ushort structure_id;

        public byte i2c_address;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U2)]
        public ushort flags;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public ushort flow_calibration;

        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.I2)]
        public short flow_factor;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public Sensor[] sensor;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public Controller[] controller;

        public StripConfigInfo strip_config;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public LedControllerInfo[] led_controller;

        public byte profileId;

        public byte dummy;
    }

    /// 
    /// Report Id 8 (FeatureData) 
    /// 

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ProfileName
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public byte[] name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Profile
    {
        [Endian(Endianness.BigEndian)]
        [MarshalAs(UnmanagedType.U2)]
        public ushort version_id;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 42)]
        public ProfileName[] names;
    }
}
