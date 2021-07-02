using System;
using BeauUtil;
using System.Runtime.InteropServices;

namespace Aqua
{
    /// <summary>
    /// Mapping of property to 32-bit float.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack=4)]
    public struct WaterPropertyBlockF32
    {
        public float Oxygen;
        public float Temperature;
        public float Light;
        public float PH;
        public float CarbonDioxide;
        public float Salinity;

        public unsafe float this[WaterPropertyId inId]
        {
            get
            {
                if (inId < 0 || inId > WaterPropertyId.TRACKED_MAX)
                    throw new ArgumentOutOfRangeException("inId");

                fixed(float* start = &this.Oxygen)
                {
                    return *((float*) (start + (int) inId));
                }
            }
            set
            {
                if (inId < 0 || inId > WaterPropertyId.TRACKED_MAX)
                    throw new ArgumentOutOfRangeException("inId");

                fixed(float* start = &this.Oxygen)
                {
                    *((float*) (start + (int) inId)) = value;
                }
            }
        }

        public override string ToString()
        {
            return string.Format("[O2={0}, Temp={1}, Light={2}, PH={3}, CO2={4}, Salt={5}]",
                Oxygen, Temperature, Light, PH, CarbonDioxide, Salinity);
        }

        static public WaterPropertyBlockF32 operator *(WaterPropertyBlockF32 inA, float inB)
        {
            WaterPropertyBlockF32 result = inA;
            result.Oxygen *= inB;
            result.Temperature *= inB;
            result.Light *= inB;
            result.PH *= inB;
            result.CarbonDioxide *= inB;
            result.Salinity *= inB;
            return result;
        }

        static public WaterPropertyBlockF32 operator +(WaterPropertyBlockF32 inA, WaterPropertyBlockF32 inB)
        {
            WaterPropertyBlockF32 result = inA;
            result.Oxygen += inB.Oxygen;
            result.Temperature += inB.Temperature;
            result.Light += inB.Light;
            result.PH += inB.PH;
            result.CarbonDioxide += inB.CarbonDioxide;
            result.Salinity += inB.Salinity;
            return result;
        }

        static public WaterPropertyBlockF32 operator -(WaterPropertyBlockF32 inA, WaterPropertyBlockF32 inB)
        {
            WaterPropertyBlockF32 result = inA;
            result.Oxygen -= inB.Oxygen;
            result.Temperature -= inB.Temperature;
            result.Light -= inB.Light;
            result.PH -= inB.PH;
            result.CarbonDioxide -= inB.CarbonDioxide;
            result.Salinity -= inB.Salinity;
            return result;
        }

        static public unsafe WaterPropertyBlockF32 operator &(WaterPropertyBlockF32 inA, WaterPropertyMask inB)
        {
            WaterPropertyBlockF32 result = inA;
            
            float* ptr = &result.Oxygen;
            int idx = 0;
            int mask = 1;
            while(idx <= (int) WaterPropertyId.TRACKED_MAX)
            {
                if ((inB & mask) == 0)
                    *ptr = 0;
                idx++;
                ptr++;
                mask <<= 1;
            }

            return result;
        }
    }

    /// <summary>
    /// Mapping of property to unsigned 16-bit int.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack=2)]
    public struct WaterPropertyBlockU16
    {
        public ushort Oxygen;
        public ushort Temperature;
        public ushort Light;
        public ushort PH;
        public ushort CarbonDioxide;
        public ushort Salinity;

        public unsafe ushort this[WaterPropertyId inId]
        {
            get
            {
                if (inId < 0 || inId > WaterPropertyId.TRACKED_MAX)
                    throw new ArgumentOutOfRangeException("inId");

                fixed(ushort* start = &this.Oxygen)
                {
                    return *((ushort*) (start + (int) inId));
                }
            }
            set
            {
                if (inId < 0 || inId > WaterPropertyId.TRACKED_MAX)
                    throw new ArgumentOutOfRangeException("inId");

                fixed(ushort* start = &this.Oxygen)
                {
                    *((ushort*) (start + (int) inId)) = value;
                }
            }
        }

        public override string ToString()
        {
            return string.Format("[O2={0}, Temp={1}, Light={2}, PH={3}, CO2={4}, Salt={5}]",
                Oxygen, Temperature, Light, PH, CarbonDioxide, Salinity);
        }

        static public WaterPropertyBlockU16 operator *(WaterPropertyBlockU16 inA, float inB)
        {
            WaterPropertyBlockU16 result = inA;
            result.Oxygen = (ushort) (result.Oxygen * inB);
            result.Temperature = (ushort) (result.Temperature * inB);
            result.Light = (ushort) (result.Light * inB);
            result.PH = (ushort) (result.PH * inB);
            result.CarbonDioxide = (ushort) (result.CarbonDioxide * inB);
            result.Salinity = (ushort) (result.Salinity * inB);
            return result;
        }

        static public WaterPropertyBlockU16 operator +(WaterPropertyBlockU16 inA, WaterPropertyBlockU16 inB)
        {
            WaterPropertyBlockU16 result = inA;
            result.Oxygen += inB.Oxygen;
            result.Temperature += inB.Temperature;
            result.Light += inB.Light;
            result.PH += inB.PH;
            result.CarbonDioxide += inB.CarbonDioxide;
            result.Salinity += inB.Salinity;
            return result;
        }

        static public WaterPropertyBlockU16 operator -(WaterPropertyBlockU16 inA, WaterPropertyBlockU16 inB)
        {
            WaterPropertyBlockU16 result = inA;
            result.Oxygen -= inB.Oxygen;
            result.Temperature -= inB.Temperature;
            result.Light -= inB.Light;
            result.PH -= inB.PH;
            result.CarbonDioxide -= inB.CarbonDioxide;
            result.Salinity -= inB.Salinity;
            return result;
        }

        static public unsafe WaterPropertyBlockU16 operator &(WaterPropertyBlockU16 inA, WaterPropertyMask inB)
        {
            WaterPropertyBlockU16 result = inA;
            
            ushort* ptr = &result.Oxygen;
            int idx = 0;
            int mask = 1;
            while(idx <= (int) WaterPropertyId.TRACKED_MAX)
            {
                if ((inB & mask) == 0)
                    *ptr = 0;
                idx++;
                ptr++;
                mask <<= 1;
            }

            return result;
        }
    }

    /// <summary>
    /// Mapping of property to unsigned 32-bit int.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack=4)]
    public struct WaterPropertyBlockU32
    {
        public uint Oxygen;
        public uint Temperature;
        public uint Light;
        public uint PH;
        public uint CarbonDioxide;
        public uint Salinity;

        public unsafe uint this[WaterPropertyId inId]
        {
            get
            {
                if (inId < 0 || inId > WaterPropertyId.TRACKED_MAX)
                    throw new ArgumentOutOfRangeException("inId");

                fixed(uint* start = &this.Oxygen)
                {
                    return *((uint*) (start + (int) inId));
                }
            }
            set
            {
                if (inId < 0 || inId > WaterPropertyId.TRACKED_MAX)
                    throw new ArgumentOutOfRangeException("inId");

                fixed(uint* start = &this.Oxygen)
                {
                    *((uint*) (start + (int) inId)) = value;
                }
            }
        }

        public override string ToString()
        {
            return string.Format("[O2={0}, Temp={1}, Light={2}, PH={3}, CO2={4}, Salt={5}]",
                Oxygen, Temperature, Light, PH, CarbonDioxide, Salinity);
        }

        static public WaterPropertyBlockU32 operator *(WaterPropertyBlockU32 inA, float inB)
        {
            WaterPropertyBlockU32 result = inA;
            result.Oxygen = (uint) (result.Oxygen * inB);
            result.Temperature = (uint) (result.Temperature * inB);
            result.Light = (uint) (result.Light * inB);
            result.PH = (uint) (result.PH * inB);
            result.CarbonDioxide = (uint) (result.CarbonDioxide * inB);
            result.Salinity = (uint) (result.Salinity * inB);
            return result;
        }

        static public WaterPropertyBlockU32 operator +(WaterPropertyBlockU32 inA, WaterPropertyBlockU32 inB)
        {
            WaterPropertyBlockU32 result = inA;
            result.Oxygen += inB.Oxygen;
            result.Temperature += inB.Temperature;
            result.Light += inB.Light;
            result.PH += inB.PH;
            result.CarbonDioxide += inB.CarbonDioxide;
            result.Salinity += inB.Salinity;
            return result;
        }

        static public WaterPropertyBlockU32 operator -(WaterPropertyBlockU32 inA, WaterPropertyBlockU32 inB)
        {
            WaterPropertyBlockU32 result = inA;
            result.Oxygen -= inB.Oxygen;
            result.Temperature -= inB.Temperature;
            result.Light -= inB.Light;
            result.PH -= inB.PH;
            result.CarbonDioxide -= inB.CarbonDioxide;
            result.Salinity -= inB.Salinity;
            return result;
        }

        static public unsafe WaterPropertyBlockU32 operator &(WaterPropertyBlockU32 inA, WaterPropertyMask inB)
        {
            WaterPropertyBlockU32 result = inA;
            
            uint* ptr = &result.Oxygen;
            int idx = 0;
            int mask = 1;
            while(idx <= (int) WaterPropertyId.TRACKED_MAX)
            {
                if ((inB & mask) == 0)
                    *ptr = 0;
                idx++;
                ptr++;
                mask <<= 1;
            }

            return result;
        }
    }

    /// <summary>
    /// Mapping of property to unsigned 8-bit integer.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct WaterPropertyBlockU8
    {
        public byte Oxygen;
        public byte Temperature;
        public byte Light;
        public byte PH;
        public byte CarbonDioxide;
        public byte Salinity;

        public unsafe byte this[WaterPropertyId inId]
        {
            get
            {
                if (inId < 0 || inId > WaterPropertyId.TRACKED_MAX)
                    throw new ArgumentOutOfRangeException("inId");

                fixed(byte* start = &this.Oxygen)
                {
                    return *((byte*) (start + (int) inId));
                }
            }
            set
            {
                if (inId < 0 || inId > WaterPropertyId.TRACKED_MAX)
                    throw new ArgumentOutOfRangeException("inId");

                fixed(byte* start = &this.Oxygen)
                {
                    *((byte*) (start + (int) inId)) = value;
                }
            }
        }

        public override string ToString()
        {
            return string.Format("[O2={0}, Temp={1}, Light={2}, PH={3}, CO2={4}, Salt={5}]",
                Oxygen, Temperature, Light, PH, CarbonDioxide, Salinity);
        }

        static public unsafe WaterPropertyBlockU8 operator &(WaterPropertyBlockU8 inA, WaterPropertyMask inB)
        {
            WaterPropertyBlockU8 result = inA;
            
            byte* ptr = &result.Oxygen;
            int idx = 0;
            int mask = 1;
            while(idx <= (int) WaterPropertyId.TRACKED_MAX)
            {
                if ((inB & mask) == 0)
                    *ptr = 0;
                idx++;
                ptr++;
                mask <<= 1;
            }
            
            return result;
        }
    }

    /// <summary>
    /// Bit mask for property ids.
    /// </summary>
    public struct WaterPropertyMask
    {
        private const byte FullMask = (1 << ((int) WaterPropertyId.TRACKED_MAX + 1)) - 1;

        public byte Mask;

        public WaterPropertyMask(byte inMask)
        {
            Mask = inMask;
        }

        public WaterPropertyMask(WaterPropertyId[] inIds)
        {
            Mask = 0;
            for(int i = 0; i < inIds.Length; ++i)
            {
                Mask |= (byte) (1 << (int) inIds[i]);
            }
        }

        public bool this[WaterPropertyId inId]
        {
            get
            {
                if (inId < 0 || inId > WaterPropertyId.TRACKED_MAX)
                    throw new ArgumentOutOfRangeException("inId");

                return (Mask & (1 << (int) inId)) != 0;
            }
            set
            {
                if (inId < 0 || inId > WaterPropertyId.TRACKED_MAX)
                    throw new ArgumentOutOfRangeException("inId");

                if (value)
                {
                    Mask |= (byte) (1 << (int) inId);
                }
                else
                {
                    Mask &= (byte)(~(1 << (int) inId));
                }
            }
        }

        static public implicit operator byte(WaterPropertyMask inMask)
        {
            return inMask.Mask;
        }

        static public WaterPropertyMask All()
        {
            return new WaterPropertyMask(FullMask);
        }
    }

    /// <summary>
    /// Block mapping property to arbitrary data.
    /// </summary>
    public struct WaterPropertyBlock<T>
    {
        public T Oxygen;
        public T Temperature;
        public T Light;
        public T PH;
        public T CarbonDioxide;
        public T Salinity;

        public T this[WaterPropertyId inId]
        {
            get
            {
                switch(inId)
                {
                    case WaterPropertyId.Oxygen:
                        return Oxygen;
                    case WaterPropertyId.Temperature:
                        return Temperature;
                    case WaterPropertyId.Light:
                        return Light;
                    case WaterPropertyId.PH:
                        return PH;
                    case WaterPropertyId.CarbonDioxide:
                        return CarbonDioxide;
                    case WaterPropertyId.Salinity:
                        return Salinity;

                    default:
                        throw new ArgumentOutOfRangeException("inId");
                }
            }
            set
            {
                switch(inId)
                {
                    case WaterPropertyId.Oxygen:
                        Oxygen = value;
                        break;
                    case WaterPropertyId.Temperature:
                        Temperature = value;
                        break;
                    case WaterPropertyId.Light:
                        Light = value;
                        break;
                    case WaterPropertyId.PH:
                        PH = value;
                        break;
                    case WaterPropertyId.CarbonDioxide:
                        CarbonDioxide = value;
                        break;
                    case WaterPropertyId.Salinity:
                        Salinity = value;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("inId");
                }
            }
        }

        public override string ToString()
        {
            return string.Format("[O2={0}, Temp={1}, Light={2}, PH={3}, CO2={4}, Salt={5}]",
                Oxygen, Temperature, Light, PH, CarbonDioxide, Salinity);
        }
    }
}