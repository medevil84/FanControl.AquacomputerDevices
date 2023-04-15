using AquacomputerStructs.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AquacomputerStructs.Common
{

    /// <summary>
    /// Device Header, identifies the type of device (devicetype)
    /// Currently tested on quadro and highflownext devices.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct device_header
    {
        public byte report_id;

        [MarshalAs(UnmanagedType.U2)]
        [Endian(Endianness.BigEndian)]
        public ushort structure_id;

        [MarshalAs(UnmanagedType.U4)]
        [Endian(Endianness.BigEndian)]
        public uint serial;

        [MarshalAs(UnmanagedType.U2)]
        [Endian(Endianness.BigEndian)]
        public ushort hardware;

        [MarshalAs(UnmanagedType.U2)]
        [Endian(Endianness.BigEndian)]
        public ushort device_type;

        [MarshalAs(UnmanagedType.U2)]
        [Endian(Endianness.BigEndian)]
        public ushort bootloader;

        [MarshalAs(UnmanagedType.U2)]
        [Endian(Endianness.BigEndian)]
        public ushort firmware;

        public static string SerialToText(uint sn)
        {
            return ((sn & 0xFFFF0000L) >> 16).ToString("D5") + "-" + (sn & 0xFFFFL).ToString("D5");
        }
    }
}
