using System;
using BeauUtil;
using UnityEngine;

namespace Aqua.Animation
{
    [Serializable]
    public struct AmbientWaveConfig
    {
        public int Frequency;
        [AutoEnum] public AmbientWaveMethod Method;

        [Header("Delay")]
        public float Delay;
        public Fraction8 DelayRandom;

        [Header("Duration")]
        public float Duration;
        public Fraction8 DurationRandom;
    }

    [Serializable]
    public struct AmbientFloatPropertyConfig
    {
        [HideInInspector] public float Initial;

        public AmbientWaveConfig Wave;
        public float Distance;
    }

    [Serializable]
    public struct AmbientVec3PropertyConfig
    {
        [HideInInspector] public Vector3 Initial;

        [Header("Full")]
        public AmbientWaveConfig WaveXYZ;
        public Vector3 DistanceXYZ;

        [Header("X Offset")]
        public AmbientWaveConfig WaveX;
        public float DistanceX;

        [Header("Y Offset")]
        public AmbientWaveConfig WaveY;
        public float DistanceY;

        [Header("Z Offset")]
        public AmbientWaveConfig WaveZ;
        public float DistanceZ;
    }

    [LabeledEnum(false)]
    public enum AmbientWaveMethod : byte
    {
        [Label("Sine Wave")]
        Sine,

        [Label("Positive Sine Wave")]
        AbsSine,

        [Label("Yoyo")]
        Triangle
    }
}