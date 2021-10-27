using System;
using BeauUtil;
using UnityEngine;

namespace Aqua.Cameras
{
    /// <summary>
    /// Delegate for evaluating a dynamic camera hint weight.
    /// </summary>
    public delegate float CameraWeightFunction(Vector2 inHintPosition, Vector2 inCameraTargetPosition);
    
    /// <summary>
    /// Data regarding a camera point.
    /// </summary>
    public struct CameraPointData
    {
        public uint Id;

        public float Zoom;
        public float Lerp;

        public Transform Anchor;
        public Vector2 Offset;

        public CameraWeightFunction Weight;
        public float WeightOffset;

        internal Vector2 m_CachedPosition;
        internal float m_CachedWeight;
    }

    /// <summary>
    /// Data regarding a camera target.
    /// </summary>
    public struct CameraTargetData
    {
        public uint Id;
        public CameraModifierFlags Flags;

        public float Zoom;
        public float Lerp;

        public Transform Anchor;
        public Vector2 Offset;

        internal Vector2 m_CachedPosition;
    }

    /// <summary>
    /// Data regarding a camera bounds.
    /// </summary>
    public struct CameraBoundsData
    {
        public uint Id;

        public BoxCollider2D Anchor2D;
        public Rect Region;
        public RectEdges SoftEdges;
        public RectEdges HardEdges;

        internal Rect m_CachedRegion;
    }

    /// <summary>
    /// Data regarding a camera drift.
    /// </summary>
    public struct CameraDriftData
    {
        public uint Id;

        public Vector2 Distance;
        public Vector2 Period;
        public Vector2 Offset;
    }

    /// <summary>
    /// Data regarding a camera shake.
    /// </summary>
    public struct CameraShakeData
    {
        public Vector2 Distance;
        public Vector2 Period;
        public Vector2 Offset;
        public float Duration;

        internal double m_StartTime;
    }

    /// <summary>
    /// Flags indicating which types of modifiers to apply.
    /// </summary>
    [Flags]
    public enum CameraModifierFlags : byte
    {
        [Hidden]
        None = 0,

        Bounds = 0x01,
        Hints = 0x02,
        Drift = 0x04,

        [Hidden]
        NoHints = Bounds | Drift,
        
        [Hidden]
        All = Bounds | Hints | Drift
    }
}