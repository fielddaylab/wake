using System;
using BeauUtil;

namespace Aqua
{
    public struct WaterPropertyBlockF
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
                    return *((float*) &(start) + (int) inId);
                }
            }
            set
            {
                if (inId < 0 || inId > WaterPropertyId.TRACKED_MAX)
                    throw new ArgumentOutOfRangeException("inId");

                fixed(float* start = &this.Oxygen)
                {
                    *((float*) &(start) + (int) inId) = value;
                }
            }
        }

        static public WaterPropertyBlockF operator *(WaterPropertyBlockF inA, float inB)
        {
            WaterPropertyBlockF result = inA;
            result.Oxygen *= inB;
            result.Temperature *= inB;
            result.Light *= inB;
            result.PH *= inB;
            result.CarbonDioxide *= inB;
            result.Salinity *= inB;
            result.Food *= inB;
            return result;
        }
    }

    public struct WaterPropertyBlock8
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
                    return *((byte*) &(start) + (int) inId);
                }
            }
            set
            {
                if (inId < 0 || inId > WaterPropertyId.TRACKED_MAX)
                    throw new ArgumentOutOfRangeException("inId");

                fixed(byte* start = &this.Oxygen)
                {
                    *((byte*) &(start) + (int) inId) = value;
                }
            }
        }
    }
}