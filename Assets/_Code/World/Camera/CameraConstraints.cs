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
        public StringHash32 Id;

        public float Zoom;
        public float Lerp;

        public Transform Anchor;
        public Vector2 Offset;

        public CameraWeightFunction Weight;
        public float WeightOffset;

        public Vector2 CachedPosition;
        public float CachedWeight;
    }

    /// <summary>
    /// Data regarding a camera bounds.
    /// </summary>
    public struct CameraBoundsData
    {
        public StringHash32 Id;

        public BoxCollider2D Anchor2D;
        public Rect Region;
        public RectEdges SoftEdges;
        public RectEdges HardEdges;

        public Rect CachedRegion;
    }

    /// <summary>
    /// Data regarding a camera drift.
    /// </summary>
    public struct CameraDriftData
    {
        public StringHash32 Id;

        public Vector2 Distance;
        public Vector2 Period;
        public Vector2 Offset;
    }
}