using System;
using BeauUtil;

namespace Aqua
{
    public struct WaterPropertyBlockF32
    {
        public float Oxygen;
        public float Temperature;
        public float Light;
        public float PH;
        public float CarbonDioxide;
        public float Salinity;
        public float Food;

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
            return string.Format("[O2={0}, Temp={1}, Light={2}, PH={3}, CO2={4}, Salt={5}, Food={6}]",
                Oxygen, Temperature, Light, PH, CarbonDioxide, Salinity, Food);
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
            result.Food *= inB;
            return result;
        }

        static public WaterPropertyBlockF32 operator +(WaterPropertyBlockF32 inA, WaterPropertyBlockF32 inB)
        {
            WaterPropertyBlockF32 result = inA;
            result.Oxygen += inB.Oxygen;
            result.Temperature += inB.Oxygen;
            result.Light += inB.Light;
            result.PH += inB.Light;
            result.CarbonDioxide += inB.CarbonDioxide;
            result.Salinity += inB.Salinity;
            result.Food += inB.Food;
            return result;
        }

        static public WaterPropertyBlockF32 operator -(WaterPropertyBlockF32 inA, WaterPropertyBlockF32 inB)
        {
            WaterPropertyBlockF32 result = inA;
            result.Oxygen -= inB.Oxygen;
            result.Temperature -= inB.Oxygen;
            result.Light -= inB.Light;
            result.PH -= inB.Light;
            result.CarbonDioxide -= inB.CarbonDioxide;
            result.Salinity -= inB.Salinity;
            result.Food -= inB.Food;
            return result;
        }

        static public unsafe WaterPropertyBlockF32 operator &(WaterPropertyBlockF32 inA, WaterPropertyMask inB)
        {
            WaterPropertyBlockF32 result = inA;
            
            float* ptr = &result.Oxygen;
            int idx = 0;
            int mask = 1;
            while(idx < (int) WaterPropertyId.TRACKED_MAX)
            {
                if ((inB & mask) == 0)
                    *ptr = 0;
                idx++;
                mask <<= 1;
            }

            return result;
        }
    }

    public struct WaterPropertyBlockU8
    {
        public byte Oxygen;
        public byte Temperature;
        public byte Light;
        public byte PH;
        public byte CarbonDioxide;
        public byte Salinity;
        public byte Food;

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
            return string.Format("[O2={0}, Temp={1}, Light={2}, PH={3}, CO2={4}, Salt={5}, Food={6}]",
                Oxygen, Temperature, Light, PH, CarbonDioxide, Salinity, Food);
        }

        static public unsafe WaterPropertyBlockU8 operator &(WaterPropertyBlockU8 inA, WaterPropertyMask inB)
        {
            WaterPropertyBlockU8 result = inA;
            
            byte* ptr = &result.Oxygen;
            int idx = 0;
            int mask = 1;
            while(idx < (int) WaterPropertyId.TRACKED_MAX)
            {
                if ((inB & mask) == 0)
                    *ptr = 0;
                idx++;
                mask <<= 1;
            }
            
            return result;
        }
    }

    public struct WaterPropertyMask
    {
        public byte Mask;

        public unsafe bool this[WaterPropertyId inId]
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
    }

    public struct WaterPropertyBlock<T>
    {
        public T Oxygen;
        public T Temperature;
        public T Light;
        public T PH;
        public T CarbonDioxide;
        public T Salinity;
        public T Food;

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
                    case WaterPropertyId.Food:
                        return Food;

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
                    case WaterPropertyId.Food:
                        Food = value;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("inId");
                }
            }
        }

        public override string ToString()
        {
            return string.Format("[O2={0}, Temp={1}, Light={2}, PH={3}, CO2={4}, Salt={5}, Food={6}]",
                Oxygen, Temperature, Light, PH, CarbonDioxide, Salinity, Food);
        }
    }
}