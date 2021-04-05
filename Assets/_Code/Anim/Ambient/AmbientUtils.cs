using System;
using BeauUtil;
using UnityEngine;
using Random = System.Random;

namespace Aqua.Animation
{
    static public class AmbientUtils
    {
        public const float Epsilon = float.Epsilon;

        #region Init

        /// <summary>
        /// Initializes a Vec3 wave state.
        /// </summary>
        static public void InitVec3Wave(ref AmbientVec3State ioState, ref AmbientVec3PropertyConfig ioConfig, Vector3 inInitial, float inTimestamp, Random inRandom)
        {
            ioConfig.Initial = inInitial;
            InitWave(ref ioState.XYZ, ioConfig.WaveXYZ, inTimestamp, inRandom);
            InitWave(ref ioState.X, ioConfig.WaveX, inTimestamp, inRandom);
            InitWave(ref ioState.Y, ioConfig.WaveY, inTimestamp, inRandom);
            InitWave(ref ioState.Z, ioConfig.WaveZ, inTimestamp, inRandom);
        }

        /// <summary>
        /// Initializes a wave state.
        /// </summary>
        static public void InitWave(ref AmbientWaveState ioState, in AmbientWaveConfig inConfig, float inTimestamp, Random inRandom)
        {
            ioState.Length = inConfig.Duration * (1 - inConfig.DurationRandom * inRandom.NextFloat());
            ioState.Start = inTimestamp + (inConfig.Delay * inRandom.NextFloat()) - (inConfig.Duration * inRandom.NextFloat());
        }

        #endregion // Init

        #region Process

        /// <summary>
        /// Processes all addititive changes for a batch of vector3 waves.
        /// </summary>
        static public unsafe void ProcessVec3Additive(Vector3* ioValues, AmbientVec3State* ioStateBuffer, AmbientVec3PropertyConfig* inPropertyBuffer, byte* ioChangeBuffer, int inObjectCount, float inTimestamp, byte inChangeFlag)
        {
            for(int i = 0; i < inObjectCount; ++i)
            {
                float xyzProgress = AmbientWaveState.CalculateProgress(ioStateBuffer[i].XYZ, inTimestamp, inPropertyBuffer[i].WaveXYZ.Delay <= 0);
                float xProgress = AmbientWaveState.CalculateProgress(ioStateBuffer[i].X, inTimestamp, inPropertyBuffer[i].WaveX.Delay <= 0);
                float yProgress = AmbientWaveState.CalculateProgress(ioStateBuffer[i].Y, inTimestamp, inPropertyBuffer[i].WaveY.Delay <= 0);
                float zProgress = AmbientWaveState.CalculateProgress(ioStateBuffer[i].Z, inTimestamp, inPropertyBuffer[i].WaveZ.Delay <= 0);
                float xyzWave = CalculateWave(inPropertyBuffer[i].WaveXYZ, xyzProgress);

                float dx = inPropertyBuffer[i].DistanceXYZ.x * xyzWave + inPropertyBuffer[i].DistanceX * CalculateWave(inPropertyBuffer[i].WaveX, xProgress);
                float dy = inPropertyBuffer[i].DistanceXYZ.y * xyzWave + inPropertyBuffer[i].DistanceY * CalculateWave(inPropertyBuffer[i].WaveY, yProgress);
                float dz = inPropertyBuffer[i].DistanceXYZ.z * xyzWave + inPropertyBuffer[i].DistanceZ * CalculateWave(inPropertyBuffer[i].WaveZ, zProgress);

                if (NotZero(xyzProgress) || NotZero(xProgress) || NotZero(yProgress) || NotZero(zProgress))
                {
                    ioChangeBuffer[i] |= inChangeFlag;

                    ref Vector3 val = ref ioValues[i];
                    val.x += dx;
                    val.y += dy;
                    val.z += dz;
                }
            }
        }

        /// <summary>
        /// Processes all waves in a batch of vector3 waves.
        /// </summary>
        static public unsafe void ProcessVec3Waves(AmbientVec3State* ioStateBuffer, AmbientVec3PropertyConfig* inPropertyBuffer, int inObjectCount, float inTimestamp, Random inRandom)
        {
            for(int i = 0; i < inObjectCount; ++i)
            {
                ProcessWave(ref ioStateBuffer[i].XYZ, inPropertyBuffer[i].WaveXYZ, inTimestamp, inRandom);
                ProcessWave(ref ioStateBuffer[i].X, inPropertyBuffer[i].WaveX, inTimestamp, inRandom);
                ProcessWave(ref ioStateBuffer[i].Y, inPropertyBuffer[i].WaveY, inTimestamp, inRandom);
                ProcessWave(ref ioStateBuffer[i].Z, inPropertyBuffer[i].WaveZ, inTimestamp, inRandom);
            }
        }

        /// <summary>
        /// Processes a single wave.
        /// </summary>
        static public void ProcessWave(ref AmbientWaveState ioState, in AmbientWaveConfig inConfig, float inTimestamp, Random inRandom)
        {
            if (ioState.Length <= 0 || inTimestamp < ioState.Start + ioState.Length)
                return;

            float delay = inConfig.Delay * (1 + inConfig.DelayRandom * inRandom.NextFloat());
            float duration = inConfig.Duration * (1 + inConfig.DurationRandom * inRandom.NextFloat());
            AmbientWaveState.AdvanceAndSetLength(ref ioState, delay, duration);
        }

        #endregion // Process

        #region Utils

        /// <summary>
        /// Calculates the wave value for the given config and proportion through the entire wave.
        /// </summary>
        static public float CalculateWave(in AmbientWaveConfig inConfig, float inProportion)
        {
            switch(inConfig.Method)
            {
                case AmbientWaveMethod.Sine:
                    return (float) Math.Sin(inProportion * 2 * Math.PI * inConfig.Frequency);
                case AmbientWaveMethod.AbsSine:
                    return (float) Math.Abs(Math.Sin(inProportion * 2 * Math.PI * inConfig.Frequency));
                case AmbientWaveMethod.Triangle:
                    float x = ((inProportion * inConfig.Frequency) + 0.25f) % 1;
                    return 4 * Math.Abs(x - 0.5f) - 1;
                default:
                    throw new ArgumentOutOfRangeException("inMethod");
            }
        }

        static public bool NotZero(float inVal)
        {
            return inVal > Epsilon || inVal < -Epsilon;
        }

        static public bool NotOne(float inVal)
        {
            return inVal > 1 + Epsilon || inVal < 1 - Epsilon;
        }

        #endregion // Utils
    }
}