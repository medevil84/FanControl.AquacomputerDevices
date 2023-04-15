using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace AquacomputerStructs.Helpers
{
    public enum Endianness
    {
        BigEndian,
        LittleEndian
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class EndianAttribute : Attribute
    {
        public Endianness Endianness { get; private set; }

        public EndianAttribute(Endianness endianness)
        {
            this.Endianness = endianness;
        }

        private struct TypeOffset
        {
            public int startOffset;
            public Type type;
            public TypeOffset(Type t, int o)
            {
                this.type = t;
                this.startOffset = o;
            }
        }

        private static void RespectEndianness(Type type, byte[] data, int startOffset = 0)
        {
            var q_types = new Queue<TypeOffset>();
            Endianness archEndianness = BitConverter.IsLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian;

            q_types.Enqueue(new TypeOffset(type, startOffset));

            while (q_types.Count > 0)
            {
                var t = q_types.Dequeue();
                foreach (var field in t.type.GetFields())
                {
                    if (field.IsStatic)
                        continue;

                    var fieldType = field.FieldType;
                    if (fieldType == typeof(string))
                        continue;

                    // handle enums
                    if (fieldType.IsEnum)
                        fieldType = Enum.GetUnderlyingType(fieldType);

                    // Get Field endianness if defined
                    Endianness attribute = archEndianness;
                    if (field.IsDefined(typeof(EndianAttribute), false))
                        attribute = ((EndianAttribute)field.GetCustomAttributes(typeof(EndianAttribute), false)[0]).Endianness;

                    var Offset = Marshal.OffsetOf(t.type, field.Name).ToInt32();
                    var effectiveOffset = t.startOffset + Offset;
                    var subFields = fieldType.GetFields().Where(subField => subField.IsStatic == false).ToArray();

                    // Check if array:
                    if (fieldType.IsArray)
                    {
                        var arrayFieldType = fieldType.GetElementType();
                        var arraySubFields = arrayFieldType.GetFields().Where(subField => subField.IsStatic == false).ToArray();
                        
                        Endianness arrayFieldEndianess = attribute;
                        if (arrayFieldType.IsDefined(typeof(EndianAttribute), false))
                            arrayFieldEndianess = ((EndianAttribute)arrayFieldType.GetCustomAttributes(typeof(EndianAttribute), false)[0]).Endianness;

                        MarshalAsAttribute attr = (MarshalAsAttribute)field.GetCustomAttributes(typeof(MarshalAsAttribute), false)[0];
                        int arraySize = Marshal.SizeOf(arrayFieldType);

                        if (arraySubFields.Length == 0 && 
                            arrayFieldEndianess != archEndianness)
                        {
                            for (int i = 0; i < attr.SizeConst; i++)
                                Array.Reverse(data, effectiveOffset + i * arraySize, arraySize);
                        }

                        if (arraySubFields.Length > 0)
                        {
                            for (int i = 0; i < attr.SizeConst; i++)
                                q_types.Enqueue(new TypeOffset(arrayFieldType, effectiveOffset + i * arraySize));
                        }
                    }
                    // check for sub-fields to recurse if necessary
                    else if (subFields.Length != 0)
                    {
                        q_types.Enqueue(new TypeOffset(fieldType, effectiveOffset));
                    }
                    else if (attribute != archEndianness)
                    {
                        Array.Reverse(data, effectiveOffset, Marshal.SizeOf(fieldType));
                    }
                }
            }
        }

        public static T BytesToStruct<T>(byte[] rawData) where T : struct
        {
            T result = default;

            RespectEndianness(typeof(T), rawData);

            GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);

            try
            {
                IntPtr rawDataPtr = handle.AddrOfPinnedObject();
                result = (T)Marshal.PtrToStructure(rawDataPtr, typeof(T));
            }
            finally
            {
                handle.Free();
            }

            return result;
        }

        public static byte[] StructToBytes<T>(T data) where T : struct
        {
            byte[] rawData = new byte[Marshal.SizeOf(data)];
            GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
            try
            {
                IntPtr rawDataPtr = handle.AddrOfPinnedObject();
                Marshal.StructureToPtr(data, rawDataPtr, false);
            }
            finally
            {
                handle.Free();
            }

            RespectEndianness(typeof(T), rawData);

            return rawData;
        }

        public static T GetStructAtOffset<T>(byte[] rawData, ref int offset) where T : struct
        {
            T result = default;

            int size = Marshal.SizeOf(result);
            byte[] rawDataStruct = new byte[size];
            Array.Copy(rawData, offset, rawDataStruct, 0, size);
            offset += size;

            RespectEndianness(typeof(T), rawDataStruct);

            GCHandle handle = GCHandle.Alloc(rawDataStruct, GCHandleType.Pinned);

            try
            {
                IntPtr rawDataPtr = handle.AddrOfPinnedObject();
                result = (T)Marshal.PtrToStructure(rawDataPtr, typeof(T));
            }
            finally
            {
                handle.Free();
            }

            return result;

        }

        public static T GetStructAtOffsetNoEndianess<T>(byte[] rawData, ref int offset) where T : struct
        {
            T result = default;

            int size = Marshal.SizeOf(result);
            byte[] rawDataStruct = new byte[size];
            Array.Copy(rawData, offset, rawDataStruct, 0, size);
            offset += size;

            //RespectEndianness(typeof(T), rawDataStruct);

            GCHandle handle = GCHandle.Alloc(rawDataStruct, GCHandleType.Pinned);

            try
            {
                IntPtr rawDataPtr = handle.AddrOfPinnedObject();
                result = (T)Marshal.PtrToStructure(rawDataPtr, typeof(T));
            }
            finally
            {
                handle.Free();
            }

            return result;

        }
    }
}
