using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ProtoAqua
{
    [Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct KinematicPropertyBlock2D
    {
        public Vector2 Velocity;
        public Vector2 Acceleration;

        [Header("Gravity")]
        public Vector2 Gravity;
        public float GravityMultiplier;

        [Header("Resistance")]
        public Vector2 Friction;
        public float Drag;

        [Header("Limits"), Range(0, 1000)]
        public float MaxSpeed;

        public void ApplyLimits()
        {
            KinematicMath2D.IntegrateLimits(ref this);
        }
    }
}