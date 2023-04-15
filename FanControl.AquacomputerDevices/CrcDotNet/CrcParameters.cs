// Credits to: https://github.com/GediminasMasaitis/crc-dot-net
namespace Crc
{
    public class CrcParameters
    {
        public byte Size { get; }
        public ulong Polynomial { get; }
        public ulong InitialValue { get; }
        public ulong FinalXorValue { get; }
        public bool ReflectInput { get; }
        public bool ReflectOutput { get; }
        public ulong? ExpectedCheck { get; }

        public CrcParameters(byte size, ulong polynomial, ulong initialValue, ulong finalXorValue, bool reflectInput, bool reflectOutput, ulong? expectedCheck = null)
        {
            Size = size;
            Polynomial = polynomial;
            InitialValue = initialValue;
            FinalXorValue = finalXorValue;
            ReflectInput = reflectInput;
            ReflectOutput = reflectOutput;
            ExpectedCheck = expectedCheck;
        }

        protected bool Equals(CrcParameters other)
        {
            return Size == other.Size && Polynomial == other.Polynomial && InitialValue == other.InitialValue && FinalXorValue == other.FinalXorValue && ReflectInput == other.ReflectInput && ReflectOutput == other.ReflectOutput && ExpectedCheck == other.ExpectedCheck;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CrcParameters)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Size.GetHashCode();
                hashCode = (hashCode * 397) ^ Polynomial.GetHashCode();
                hashCode = (hashCode * 397) ^ InitialValue.GetHashCode();
                hashCode = (hashCode * 397) ^ FinalXorValue.GetHashCode();
                hashCode = (hashCode * 397) ^ ReflectInput.GetHashCode();
                hashCode = (hashCode * 397) ^ ReflectOutput.GetHashCode();
                hashCode = (hashCode * 397) ^ ExpectedCheck.GetHashCode();
                return hashCode;
            }
        }

        public static CrcParameters Crc3_GSM = new Crc.CrcParameters(3, 0x3, 0x0, 0x7, false, false, 0x4);
        public static CrcParameters Crc3_ROHC = new Crc.CrcParameters(3, 0x3, 0x7, 0x0, true, true, 0x6);
        public static CrcParameters Crc4_G704 = new Crc.CrcParameters(4, 0x3, 0x0, 0x0, true, true, 0x7);
        public static CrcParameters Crc4_INTERLAKEN = new Crc.CrcParameters(4, 0x3, 0xf, 0xf, false, false, 0xb);
        public static CrcParameters Crc5_EPCC1G2 = new Crc.CrcParameters(5, 0x09, 0x09, 0x00, false, false, 0x00);
        public static CrcParameters Crc5_G704 = new Crc.CrcParameters(5, 0x15, 0x00, 0x00, true, true, 0x07);
        public static CrcParameters Crc5_USB = new Crc.CrcParameters(5, 0x05, 0x1f, 0x1f, true, true, 0x19);
        public static CrcParameters Crc6_CDMA2000A = new Crc.CrcParameters(6, 0x27, 0x3f, 0x00, false, false, 0x0d);
        public static CrcParameters Crc6_CDMA2000B = new Crc.CrcParameters(6, 0x07, 0x3f, 0x00, false, false, 0x3b);
        public static CrcParameters Crc6_DARC = new Crc.CrcParameters(6, 0x19, 0x00, 0x00, true, true, 0x26);
        public static CrcParameters Crc6_G704 = new Crc.CrcParameters(6, 0x03, 0x00, 0x00, true, true, 0x06);
        public static CrcParameters Crc6_GSM = new Crc.CrcParameters(6, 0x2f, 0x00, 0x3f, false, false, 0x13);
        public static CrcParameters Crc7_MMC = new Crc.CrcParameters(7, 0x09, 0x00, 0x00, false, false, 0x75);
        public static CrcParameters Crc7_ROHC = new Crc.CrcParameters(7, 0x4f, 0x7f, 0x00, true, true, 0x53);
        public static CrcParameters Crc7_UMTS = new Crc.CrcParameters(7, 0x45, 0x00, 0x00, false, false, 0x61);
        public static CrcParameters Crc8_AUTOSAR = new Crc.CrcParameters(8, 0x2f, 0xff, 0xff, false, false, 0xdf);
        public static CrcParameters Crc8_BLUETOOTH = new Crc.CrcParameters(8, 0xa7, 0x00, 0x00, true, true, 0x26);
        public static CrcParameters Crc8_CDMA2000 = new Crc.CrcParameters(8, 0x9b, 0xff, 0x00, false, false, 0xda);
        public static CrcParameters Crc8_DARC = new Crc.CrcParameters(8, 0x39, 0x00, 0x00, true, true, 0x15);
        public static CrcParameters Crc8_DVBS2 = new Crc.CrcParameters(8, 0xd5, 0x00, 0x00, false, false, 0xbc);
        public static CrcParameters Crc8_GSMA = new Crc.CrcParameters(8, 0x1d, 0x00, 0x00, false, false, 0x37);
        public static CrcParameters Crc8_GSMB = new Crc.CrcParameters(8, 0x49, 0x00, 0xff, false, false, 0x94);
        public static CrcParameters Crc8_HITAG = new Crc.CrcParameters(8, 0x1d, 0xff, 0x00, false, false, 0xb4);
        public static CrcParameters Crc8_I_432_1 = new Crc.CrcParameters(8, 0x07, 0x00, 0x55, false, false, 0xa1);
        public static CrcParameters Crc8_ICODE = new Crc.CrcParameters(8, 0x1d, 0xfd, 0x00, false, false, 0x7e);
        public static CrcParameters Crc8_LTE = new Crc.CrcParameters(8, 0x9b, 0x00, 0x00, false, false, 0xea);
        public static CrcParameters Crc8_MAXIMDOW = new Crc.CrcParameters(8, 0x31, 0x00, 0x00, true, true, 0xa1);
        public static CrcParameters Crc8_MIFAREMAD = new Crc.CrcParameters(8, 0x1d, 0xc7, 0x00, false, false, 0x99);
        public static CrcParameters Crc8_NRSC5 = new Crc.CrcParameters(8, 0x31, 0xff, 0x00, false, false, 0xf7);
        public static CrcParameters Crc8_OPENSAFETY = new Crc.CrcParameters(8, 0x2f, 0x00, 0x00, false, false, 0x3e);
        public static CrcParameters Crc8_ROHC = new Crc.CrcParameters(8, 0x07, 0xff, 0x00, true, true, 0xd0);
        public static CrcParameters Crc8_SAEJ1850 = new Crc.CrcParameters(8, 0x1d, 0xff, 0xff, false, false, 0x4b);
        public static CrcParameters Crc8_SMBUS = new Crc.CrcParameters(8, 0x07, 0x00, 0x00, false, false, 0xf4);
        public static CrcParameters Crc8_TECH3250 = new Crc.CrcParameters(8, 0x1d, 0xff, 0x00, true, true, 0x97);
        public static CrcParameters Crc8_WCDMA = new Crc.CrcParameters(8, 0x9b, 0x00, 0x00, true, true, 0x25);
        public static CrcParameters Crc10_ATM = new Crc.CrcParameters(10, 0x233, 0x000, 0x000, false, false, 0x199);
        public static CrcParameters Crc10_CDMA2000 = new Crc.CrcParameters(10, 0x3d9, 0x3ff, 0x000, false, false, 0x233);
        public static CrcParameters Crc10_GSM = new Crc.CrcParameters(10, 0x175, 0x000, 0x3ff, false, false, 0x12a);
        public static CrcParameters Crc11_FLEXRAY = new Crc.CrcParameters(11, 0x385, 0x01a, 0x000, false, false, 0x5a3);
        public static CrcParameters Crc11_UMTS = new Crc.CrcParameters(11, 0x307, 0x000, 0x000, false, false, 0x061);
        public static CrcParameters Crc12_CDMA2000 = new Crc.CrcParameters(12, 0xf13, 0xfff, 0x000, false, false, 0xd4d);
        public static CrcParameters Crc12_DECT = new Crc.CrcParameters(12, 0x80f, 0x000, 0x000, false, false, 0xf5b);
        public static CrcParameters Crc12_GSM = new Crc.CrcParameters(12, 0xd31, 0x000, 0xfff, false, false, 0xb34);
        public static CrcParameters Crc12_UMTS = new Crc.CrcParameters(12, 0x80f, 0x000, 0x000, false, true, 0xdaf);
        public static CrcParameters Crc13_BBC = new Crc.CrcParameters(13, 0x1cf5, 0x0000, 0x0000, false, false, 0x04fa);
        public static CrcParameters Crc14_DARC = new Crc.CrcParameters(14, 0x0805, 0x0000, 0x0000, true, true, 0x082d);
        public static CrcParameters Crc14_GSM = new Crc.CrcParameters(14, 0x202d, 0x0000, 0x3fff, false, false, 0x30ae);
        public static CrcParameters Crc15_CAN = new Crc.CrcParameters(15, 0x4599, 0x0000, 0x0000, false, false, 0x059e);
        public static CrcParameters Crc15_MPT1327 = new Crc.CrcParameters(15, 0x6815, 0x0000, 0x0001, false, false, 0x2566);
        public static CrcParameters Crc16_ARC = new Crc.CrcParameters(16, 0x8005, 0x0000, 0x0000, true, true, 0xbb3d);
        public static CrcParameters Crc16_CDMA2000 = new Crc.CrcParameters(16, 0xc867, 0xffff, 0x0000, false, false, 0x4c06);
        public static CrcParameters Crc16_CMS = new Crc.CrcParameters(16, 0x8005, 0xffff, 0x0000, false, false, 0xaee7);
        public static CrcParameters Crc16_DDS110 = new Crc.CrcParameters(16, 0x8005, 0x800d, 0x0000, false, false, 0x9ecf);
        public static CrcParameters Crc16_DECT_R = new Crc.CrcParameters(16, 0x0589, 0x0000, 0x0001, false, false, 0x007e);
        public static CrcParameters Crc16_DECT_X = new Crc.CrcParameters(16, 0x0589, 0x0000, 0x0000, false, false, 0x007f);
        public static CrcParameters Crc16_DNP = new Crc.CrcParameters(16, 0x3d65, 0x0000, 0xffff, true, true, 0xea82);
        public static CrcParameters Crc16_EN_13757 = new Crc.CrcParameters(16, 0x3d65, 0x0000, 0xffff, false, false, 0xc2b7);
        public static CrcParameters Crc16_GENIBUS = new Crc.CrcParameters(16, 0x1021, 0xffff, 0xffff, false, false, 0xd64e);
        public static CrcParameters Crc16_GSM = new Crc.CrcParameters(16, 0x1021, 0x0000, 0xffff, false, false, 0xce3c);
        public static CrcParameters Crc16_IBM_3740 = new Crc.CrcParameters(16, 0x1021, 0xffff, 0x0000, false, false, 0x29b1);
        public static CrcParameters Crc16_IBM_SDLC = new Crc.CrcParameters(16, 0x1021, 0xffff, 0xffff, true, true, 0x906e);
        public static CrcParameters Crc16_ISO_IEC_14443_3_A = new Crc.CrcParameters(16, 0x1021, 0xc6c6, 0x0000, true, true, 0xbf05);
        public static CrcParameters Crc16_KERMIT = new Crc.CrcParameters(16, 0x1021, 0x0000, 0x0000, true, true, 0x2189);
        public static CrcParameters Crc16_LJ1200 = new Crc.CrcParameters(16, 0x6f63, 0x0000, 0x0000, false, false, 0xbdf4);
        public static CrcParameters Crc16_M17 = new Crc.CrcParameters(16, 0x5935, 0xffff, 0x0000, false, false, 0x772b);
        public static CrcParameters Crc16_MAXIM_DOW = new Crc.CrcParameters(16, 0x8005, 0x0000, 0xffff, true, true, 0x44c2);
        public static CrcParameters Crc16_MCRF4XX = new Crc.CrcParameters(16, 0x1021, 0xffff, 0x0000, true, true, 0x6f91);
        public static CrcParameters Crc16_MODBUS = new Crc.CrcParameters(16, 0x8005, 0xffff, 0x0000, true, true, 0x4b37);
        public static CrcParameters Crc16_NRSC_5 = new Crc.CrcParameters(16, 0x080b, 0xffff, 0x0000, true, true, 0xa066);
        public static CrcParameters Crc16_OPENSAFETY_A = new Crc.CrcParameters(16, 0x5935, 0x0000, 0x0000, false, false, 0x5d38);
        public static CrcParameters Crc16_OPENSAFETY_B = new Crc.CrcParameters(16, 0x755b, 0x0000, 0x0000, false, false, 0x20fe);
        public static CrcParameters Crc16_PROFIBUS = new Crc.CrcParameters(16, 0x1dcf, 0xffff, 0xffff, false, false, 0xa819);
        public static CrcParameters Crc16_RIELLO = new Crc.CrcParameters(16, 0x1021, 0xb2aa, 0x0000, true, true, 0x63d0);
        public static CrcParameters Crc16_SPI_FUJITSU = new Crc.CrcParameters(16, 0x1021, 0x1d0f, 0x0000, false, false, 0xe5cc);
        public static CrcParameters Crc16_T10_DIF = new Crc.CrcParameters(16, 0x8bb7, 0x0000, 0x0000, false, false, 0xd0db);
        public static CrcParameters Crc16_TELEDISK = new Crc.CrcParameters(16, 0xa097, 0x0000, 0x0000, false, false, 0x0fb3);
        public static CrcParameters Crc16_TMS37157 = new Crc.CrcParameters(16, 0x1021, 0x89ec, 0x0000, true, true, 0x26b1);
        public static CrcParameters Crc16_UMTS = new Crc.CrcParameters(16, 0x8005, 0x0000, 0x0000, false, false, 0xfee8);
        public static CrcParameters Crc16_USB = new Crc.CrcParameters(16, 0x8005, 0xffff, 0xffff, true, true, 0xb4c8);
        public static CrcParameters Crc16_XMODEM = new Crc.CrcParameters(16, 0x1021, 0x0000, 0x0000, false, false, 0x31c3);
        public static CrcParameters Crc17_CAN_FD = new Crc.CrcParameters(17, 0x1685b, 0x00000, 0x00000, false, false, 0x04f03);
        public static CrcParameters Crc21_CAN_FD = new Crc.CrcParameters(21, 0x102899, 0x000000, 0x000000, false, false, 0x0ed841);
        public static CrcParameters Crc24_BLE = new Crc.CrcParameters(24, 0x00065b, 0x555555, 0x000000, true, true, 0xc25a56);
        public static CrcParameters Crc24_FLEXRAY_A = new Crc.CrcParameters(24, 0x5d6dcb, 0xfedcba, 0x000000, false, false, 0x7979bd);
        public static CrcParameters Crc24_FLEXRAY_B = new Crc.CrcParameters(24, 0x5d6dcb, 0xabcdef, 0x000000, false, false, 0x1f23b8);
        public static CrcParameters Crc24_INTERLAKEN = new Crc.CrcParameters(24, 0x328b63, 0xffffff, 0xffffff, false, false, 0xb4f3e6);
        public static CrcParameters Crc24_LTE_A = new Crc.CrcParameters(24, 0x864cfb, 0x000000, 0x000000, false, false, 0xcde703);
        public static CrcParameters Crc24_LTE_B = new Crc.CrcParameters(24, 0x800063, 0x000000, 0x000000, false, false, 0x23ef52);
        public static CrcParameters Crc24_OPENPGP = new Crc.CrcParameters(24, 0x864cfb, 0xb704ce, 0x000000, false, false, 0x21cf02);
        public static CrcParameters Crc24_OS_9 = new Crc.CrcParameters(24, 0x800063, 0xffffff, 0xffffff, false, false, 0x200fa5);
        public static CrcParameters Crc30_CDMA = new Crc.CrcParameters(30, 0x2030b9c7, 0x3fffffff, 0x3fffffff, false, false, 0x04c34abf);
        public static CrcParameters Crc31_PHILIPS = new Crc.CrcParameters(31, 0x04c11db7, 0x7fffffff, 0x7fffffff, false, false, 0x0ce9e46c);
        public static CrcParameters Crc32_AIXM = new Crc.CrcParameters(32, 0x814141ab, 0x00000000, 0x00000000, false, false, 0x3010bf7f);
        public static CrcParameters Crc32_AUTOSAR = new Crc.CrcParameters(32, 0xf4acfb13, 0xffffffff, 0xffffffff, true, true, 0x1697d06a);
        public static CrcParameters Crc32_BASE91_D = new Crc.CrcParameters(32, 0xa833982b, 0xffffffff, 0xffffffff, true, true, 0x87315576);
        public static CrcParameters Crc32_BZIP2 = new Crc.CrcParameters(32, 0x04c11db7, 0xffffffff, 0xffffffff, false, false, 0xfc891918);
        public static CrcParameters Crc32_CD_ROM_EDC = new Crc.CrcParameters(32, 0x8001801b, 0x00000000, 0x00000000, true, true, 0x6ec2edc4);
        public static CrcParameters Crc32_CKSUM = new Crc.CrcParameters(32, 0x04c11db7, 0x00000000, 0xffffffff, false, false, 0x765e7680);
        public static CrcParameters Crc32_ISCSI = new Crc.CrcParameters(32, 0x1edc6f41, 0xffffffff, 0xffffffff, true, true, 0xe3069283);
        public static CrcParameters Crc32_ISO_HDLC = new Crc.CrcParameters(32, 0x04c11db7, 0xffffffff, 0xffffffff, true, true, 0xcbf43926);
        public static CrcParameters Crc32_JAMCRC = new Crc.CrcParameters(32, 0x04c11db7, 0xffffffff, 0x00000000, true, true, 0x340bc6d9);
        public static CrcParameters Crc32_MEF = new Crc.CrcParameters(32, 0x741b8cd7, 0xffffffff, 0x00000000, true, true, 0xd2c22f51);
        public static CrcParameters Crc32_MPEG_2 = new Crc.CrcParameters(32, 0x04c11db7, 0xffffffff, 0x00000000, false, false, 0x0376e6e7);
        public static CrcParameters Crc32_XFER = new Crc.CrcParameters(32, 0x000000af, 0x00000000, 0x00000000, false, false, 0xbd0be338);
        public static CrcParameters Crc40_GSM = new Crc.CrcParameters(40, 0x0004820009, 0x0000000000, 0xffffffffff, false, false, 0xd4164fc646);
        public static CrcParameters Crc64_ECMA_182 = new Crc.CrcParameters(64, 0x42f0e1eba9ea3693, 0x0000000000000000, 0x0000000000000000, false, false, 0x6c40df5f0b497347);
        public static CrcParameters Crc64_GO_ISO = new Crc.CrcParameters(64, 0x000000000000001b, 0xffffffffffffffff, 0xffffffffffffffff, true, true, 0xb90956c775a41001);
        public static CrcParameters Crc64_MS = new Crc.CrcParameters(64, 0x259c84cba6426349, 0xffffffffffffffff, 0x0000000000000000, true, true, 0x75d4b74f024eceea);
        public static CrcParameters Crc64_WE = new Crc.CrcParameters(64, 0x42f0e1eba9ea3693, 0xffffffffffffffff, 0xffffffffffffffff, false, false, 0x62ec59e3f1a4f00a);
        public static CrcParameters Crc64_XZ = new Crc.CrcParameters(64, 0x42f0e1eba9ea3693, 0xffffffffffffffff, 0xffffffffffffffff, true, true, 0x995dc9bbdf1939fa);
        //public static CrcParameters Crc82_DARC = new Crc.CrcParameters(82, 0x0308c0111011401440411, 0x000000000000000000000, 0x000000000000000000000, true, true, 0x09ea83f625023801fd612);
    }
}