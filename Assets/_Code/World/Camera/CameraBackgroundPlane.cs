using UnityEngine;
using Aqua;
using BeauUtil;
using EasyAssetStreaming;

namespace Aqua.Cameras
{
    [DefaultExecutionOrder(100000001)]
    public class CameraBackgroundPlane : MonoBehaviour {
        public CameraFOVPlane Camera;
        public StreamingQuadTexture QuadTexture;

        private void LateUpdate() {
            Vector2 size = QuadTexture.Size;
            size.y = Camera.ZoomedHeight();
            QuadTexture.Size = size;
        }
    }
}