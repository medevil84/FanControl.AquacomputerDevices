// Credits to: https://github.com/GediminasMasaitis/crc-dot-net
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Crc
{
    public class CrcBase : HashAlgorithm
    {
        public CrcParameters Parameters { get; }

        private ulong MsbMask { get; }
        private ulong FullMask { get; }
        private int Shift { get; }
        private bool DoReflectOutput { get; }
        private ulong CorrectedInitialValue { get; set; }
        private ulong[] Table { get; }

        private ulong CurrentValue { get; set; }

        public bool AutoReset { get; set; }

        private static IDictionary<CrcParameters, ulong[]> TableCache { get; }

        static CrcBase()
        {
            TableCache = new Dictionary<CrcParameters, ulong[]>();
        }

        private void AssertConfiguration(bool condition, string message)
        {
            if (!condition)
            {
                throw new CrcConfigurationException(message);
            }
        }

        private void DoChecks()
        {
            if (!Parameters.ExpectedCheck.HasValue)
            {
                return;
            }

            var testBytes = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 };
            var crc = ComputeHash(testBytes);
            Array.Resize(ref crc, 8);
            var calculatedCheck = BitConverter.ToUInt64(crc, 0);
            var expectedCheck = Parameters.ExpectedCheck.Value;
            Reset();
            AssertConfiguration(calculatedCheck == expectedCheck, "Crc check failed");
        }

        public CrcBase(CrcParameters parameters)
        {
            Parameters = parameters;

            AssertConfiguration(Parameters.Size > 0, "CRC size must be greater than 0");
            AssertConfiguration(Parameters.Size <= 64, "CRC size must not exceed 64");

            HashSize = parameters.Size;
            Shift = parameters.Size - 8;
            MsbMask = 1UL << (parameters.Size - 1);
            FullMask = parameters.Size < sizeof(ulong) * 8 ? (1UL << parameters.Size) - 1 : ~0UL;
            DoReflectOutput = Parameters.ReflectInput ^ Parameters.ReflectOutput;
            CorrectedInitialValue = Parameters.ReflectInput ? ReflectCrc(Parameters.InitialValue) : Parameters.InitialValue;
            AutoReset = false;

            AssertConfiguration((Parameters.Polynomial & FullMask) == Parameters.Polynomial, "Polynomial is larger than the specified CRC size");
            AssertConfiguration((Parameters.InitialValue & FullMask) == Parameters.InitialValue, "Initial value is larger than the specified CRC size");
            AssertConfiguration((Parameters.FinalXorValue & FullMask) == Parameters.FinalXorValue, "Final xor value is larger than the specified CRC size");

            lock (TableCache)
            {
                if (TableCache.TryGetValue(parameters, out var lookupTable))
                {
                    Table = lookupTable;
                }
                else
                {
                    Table = CalculateNewLookupTable();
                    TableCache.Add(parameters, Table);
                }
            }

            Reset();
            DoChecks();
        }

        public CrcBase(byte length, ulong polynomial, ulong initialValue, ulong finalXorValue, bool reflectInput, bool reflectOutput, ulong? check = null)
            : this(new CrcParameters(length, polynomial, initialValue, finalXorValue, reflectInput, reflectOutput, check))
        {
        }

        private byte ReflectByte(byte b, int len)
        {
            byte result = 0;
            for (var i = 0; i < len; ++i)
            {
                result <<= 1;
                result |= (byte)(b & 1);
                b >>= 1;
            }
            return result;

            /*b = (byte)((b & 0xF0) >> 4 | (b & 0x0F) << 4);
            b = (byte)((b & 0xCC) >> 2 | (b & 0x33) << 2);
            b = (byte)((b & 0xAA) >> 1 | (b & 0x55) << 1);
            return b;*/
        }

        private ulong ReflectCrc(ulong crc)
        {
            var remainingSize = Parameters.Size;
            ulong reflected = 0;
            for (var i = 0; i < Parameters.Size; i += 8)
            {
                var len = remainingSize < 8 ? remainingSize : (byte)8;
                remainingSize -= len;
                var b = (byte)(crc & 0xFF);
                var rb = ReflectByte(b, len);
                reflected <<= len;
                reflected |= rb;
                crc >>= len;
            }
            return reflected;
        }

        private ulong[] CalculateNewLookupTable()
        {
            const int tableSize = 256;
            var table = new ulong[tableSize];
            var crc = MsbMask;
            for (var i = 1; i < tableSize; i <<= 1)
            {
                if ((crc & MsbMask) > 0)
                {
                    crc <<= 1;
                    crc ^= Parameters.Polynomial;
                }
                else
                {
                    crc <<= 1;
                }
                crc &= FullMask;
                for (var j = 0; j < i; ++j)
                {
                    table[i + j] = crc ^ table[j];
                }
            }

            if (!Parameters.ReflectInput)
            {
                return table;
            }

            var refl = new ulong[tableSize];
            for (var i = 0; i < tableSize; ++i)
            {
                var reflOffset = ReflectByte((byte)i, 8);
                var reflEntry = ReflectCrc(table[reflOffset]);
                refl[i] = reflEntry;
            }
            return refl;
        }

        public void Reset()
        {
            CurrentValue = CorrectedInitialValue;
        }

        public sealed override void Initialize()
        {
        }

        protected sealed override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            if (Parameters.ReflectInput)
            {
                for (var i = ibStart; i < ibStart + cbSize; i++)
                {
                    var dataByte = array[i];
                    var offs = dataByte ^ (CurrentValue & 0xFF);
                    CurrentValue = ((CurrentValue >> 8) ^ Table[offs]) & FullMask;
                }
            }
            else if (Shift < 0)
            {
                for (var i = ibStart; i < ibStart + cbSize; i++)
                {
                    var dataByte = array[i];
                    var offs = dataByte ^ (CurrentValue << -Shift);
                    CurrentValue = ((CurrentValue << 8) ^ Table[offs]) & FullMask;
                }
            }
            else
            {
                for (var i = ibStart; i < ibStart + cbSize; i++)
                {
                    var dataByte = array[i];
                    var offs = dataByte ^ (CurrentValue >> Shift);
                    CurrentValue = ((CurrentValue << 8) ^ Table[offs]) & FullMask;
                }
            }
        }

        protected sealed override byte[] HashFinal()
        {
            var result = CurrentValue;
            if (DoReflectOutput)
            {
                result = ReflectCrc(result);
            }
            result ^= Parameters.FinalXorValue;
            if (AutoReset)
            {
                Reset();
            }

            var byteSize = (HashSize + 7) / 8;

            var arr = BitConverter.GetBytes(result);
            Array.Resize(ref arr, byteSize);
            return arr;
        }

        public override int HashSize { get; }
    }
}