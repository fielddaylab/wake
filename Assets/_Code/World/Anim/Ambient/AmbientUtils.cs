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
        /// Initializes a color wave state.
        /// </summary>
        static public void InitColorWave(ref AmbientColorState ioState, ref AmbientColorPropertyConfig ioConfig, Color inInitial, float inTimestamp, Random inRandom)
        {
            ioConfig.Initial = inInitial;
            InitWave(ref ioState.ColorState, ioConfig.WaveColor, inTimestamp, inRandom);
            InitWave(ref ioState.AlphaState, ioConfig.WaveAlpha, inTimestamp, inRandom);
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

        #region Reset

        /// <summary>
        /// Resets a Vec3 wave state.
        /// </summary>
        static public void ResetVec3Wave(ref AmbientVec3State ioState, ref AmbientVec3PropertyConfig ioConfig, float inTimestamp)
        {
            ResetWave(ref ioState.XYZ, ioConfig.WaveXYZ, inTimestamp);
            ResetWave(ref ioState.X, ioConfig.WaveX, inTimestamp);
            ResetWave(ref ioState.Y, ioConfig.WaveY, inTimestamp);
            ResetWave(ref ioState.Z, ioConfig.WaveZ, inTimestamp);
        }
        
        /// <summary>
        /// Resets a wave.
        /// </summary>
        static public void ResetWave(ref AmbientWaveState ioState, in AmbientWaveConfig inConfig, float inTimestamp)
        {
            ioState.Length = inConfig.Duration;
            ioState.Start = inTimestamp;
        }

        #endregion // Reset

        #region Process

        /// <summary>
        /// Processes all addititive changes for a batch of vector3 waves.
        /// </summary>
        static public unsafe void ProcessVec3Additive(Vector3* ioValues, float* inAnimScales, AmbientVec3State* ioStateBuffer, AmbientVec3PropertyConfig* inPropertyBuffer, byte* ioChangeBuffer, int inObjectCount, float inTimestamp, byte inChangeFlag)
        {
            for(int i = 0; i < inObjectCount; ++i)
            {
                float xyzProgress = AmbientWaveState.CalculateProgress(ioStateBuffer[i].XYZ, inTimestamp, inPropertyBuffer[i].WaveXYZ.Delay <= 0);
                float xProgress = AmbientWaveState.CalculateProgress(ioStateBuffer[i].X, inTimestamp, inPropertyBuffer[i].WaveX.Delay <= 0);
                float yProgress = AmbientWaveState.CalculateProgress(ioStateBuffer[i].Y, inTimestamp, inPropertyBuffer[i].WaveY.Delay <= 0);
                float zProgress = AmbientWaveState.CalculateProgress(ioStateBuffer[i].Z, inTimestamp, inPropertyBuffer[i].WaveZ.Delay <= 0);
                float xyzWave = CalculateWave(inPropertyBuffer[i].WaveXYZ, xyzProgress);
                float scale = inAnimScales[i];

                float dx = scale * (inPropertyBuffer[i].DistanceXYZ.x * xyzWave + inPropertyBuffer[i].DistanceX * CalculateWave(inPropertyBuffer[i].WaveX, xProgress));
                float dy = scale * (inPropertyBuffer[i].DistanceXYZ.y * xyzWave + inPropertyBuffer[i].DistanceY * CalculateWave(inPropertyBuffer[i].WaveY, yProgress));
                float dz = scale * (inPropertyBuffer[i].DistanceXYZ.z * xyzWave + inPropertyBuffer[i].DistanceZ * CalculateWave(inPropertyBuffer[i].WaveZ, zProgress));

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
        /// Processes all multiplicative changes for a batch of color waves.
        /// </summary>
        static public unsafe void ProcessColorValues(Color* ioValues, AmbientColorState* ioStateBuffer, AmbientColorPropertyConfig* inPropertyBuffer, byte* ioChangeBuffer, int inObjectCount, float inTimestamp, byte inChangeFlag)
        {
            for(int i = 0; i < inObjectCount; ++i)
            {
                float colorProgress = AmbientWaveState.CalculateProgress(ioStateBuffer[i].ColorState, inTimestamp, inPropertyBuffer[i].WaveColor.Delay <= 0);
                float alphaProgress = AmbientWaveState.CalculateProgress(ioStateBuffer[i].AlphaState, inTimestamp, inPropertyBuffer[i].WaveAlpha.Delay <= 0);
                
                float colorWave = CalculateConstrainedWave(inPropertyBuffer[i].WaveColor, colorProgress);
                float alphaWave = CalculateConstrainedWave(inPropertyBuffer[i].WaveAlpha, alphaProgress);

                Color c = Color.LerpUnclamped(inPropertyBuffer[i].MinColor, inPropertyBuffer[i].MaxColor, colorWave);
                c.a = Mathf.LerpUnclamped(inPropertyBuffer[i].MinAlpha, inPropertyBuffer[i].MaxAlpha, alphaWave);

                if (NotZero(colorProgress) || NotZero(alphaProgress))
                {
                    ioChangeBuffer[i] |= inChangeFlag;
                    ioValues[i] = c;
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
        /// Processes all waves in a batch of color waves.
        /// </summary>
        static public unsafe void ProcessColorWaves(AmbientColorState* ioStateBuffer, AmbientColorPropertyConfig* inPropertyBuffer, int inObjectCount, float inTimestamp, Random inRandom)
        {
            for(int i = 0; i < inObjectCount; ++i)
            {
                ProcessWave(ref ioStateBuffer[i].ColorState, inPropertyBuffer[i].WaveColor, inTimestamp, inRandom);
                ProcessWave(ref ioStateBuffer[i].AlphaState, inPropertyBuffer[i].WaveAlpha, inTimestamp, inRandom);
            }
        }

        /// <summary>
        /// Processes a single wave.
        /// </summary>
        static public void ProcessWave(ref AmbientWaveState ioState, in AmbientWaveConfig inConfig, float inTimestamp, Random inRandom)
        {
            if (ioState.Length <= 0 || inTimestamp < ioState.Start + ioState.Length)
                return;

            float delay = inConfig.Delay * (1 - inConfig.DelayRandom * inRandom.NextFloat());
            float duration = inConfig.Duration * (1 - inConfig.DurationRandom * inRandom.NextFloat());
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
                case AmbientWaveMethod.Sawtooth:
                    return (inProportion * inConfig.Frequency) % 1;
                case AmbientWaveMethod.Triangle:
                    float x = ((inProportion * inConfig.Frequency) + 0.25f) % 1;
                    return 4 * Math.Abs(x - 0.5f) - 1;
                default:
                    throw new ArgumentOutOfRangeException("inMethod");
            }
        }

        /// <summary>
        /// Calculates the wave value for the given config and proportion through the entire wave, constrained to the 0-1 range.
        /// </summary>
        static public float CalculateConstrainedWave(in AmbientWaveConfig inConfig, float inProportion)
        {
            switch(inConfig.Method)
            {
                case AmbientWaveMethod.Sine:
                    return (float) (Math.Sin(inProportion * 2 * Math.PI * inConfig.Frequency) + 1) / 2;
                case AmbientWaveMethod.AbsSine:
                    return (float) Math.Abs(Math.Sin(inProportion * 2 * Math.PI * inConfig.Frequency));
                case AmbientWaveMethod.Sawtooth:
                    return (inProportion * inConfig.Frequency) % 1;
                case AmbientWaveMethod.Triangle:
                    float x = ((inProportion * inConfig.Frequency) + 0.25f) % 1;
                    return (4 * Math.Abs(x - 0.5f)) / 2;
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