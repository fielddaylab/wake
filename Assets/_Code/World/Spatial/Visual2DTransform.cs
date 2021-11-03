using System;
using Aqua.Cameras;
using BeauUtil;
using UnityEngine;

namespace Aqua.Spatial {
    public class Visual2DTransform : MonoBehaviour {
        [Required] public Transform Source;
        public PositionMode PositionType;

        [NonSerialized] public ushort LastUpdatedFrame = Frame.InvalidIndex;
        [NonSerialized] public ushort LastWrittenFrame = Frame.InvalidIndex;
        
        [NonSerialized] public Vector3 LastKnownPosition;
        [NonSerialized] public float LastKnownScale;

        [NonSerialized] private Transform m_CachedTransform;

        public void OverwritePosition(ushort frameIndex, Vector3 position, float scale) {
            LastUpdatedFrame = frameIndex;
            LastKnownPosition = position;
            LastKnownScale = scale;
            LastWrittenFrame = frameIndex;
        }

        public void WritePosition(ushort frameIndex, Vector3 position, float scale) {
            if (frameIndex != LastUpdatedFrame) {
                LastUpdatedFrame = frameIndex;
                LastKnownPosition = position;
                LastKnownScale = scale;
            }
        }

        public void CalculatePosition(ushort frameIndex, in CameraService.PlanePositionHelper positionHelper) {
            if (frameIndex != LastUpdatedFrame) {
                LastUpdatedFrame = frameIndex;
                LastKnownPosition = positionHelper.CastToPlane(Source, out LastKnownScale);
            }
        }

        public void Apply() {
            if (LastWrittenFrame != LastUpdatedFrame) {
                LastWrittenFrame = LastUpdatedFrame;
                this.CacheComponent(ref m_CachedTransform).position = LastKnownPosition;
            }
        }

        public void Wipe() {
            LastUpdatedFrame = Frame.InvalidIndex;
            LastWrittenFrame = Frame.InvalidIndex;
        }
    }

    public interface IVisual2DObjectSource {
        Visual2DTransform VisualTransform();
    }
}