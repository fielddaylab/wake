using BeauUtil;
using UnityEngine;

namespace Aqua.Cameras
{
    public class CameraRig : MonoBehaviour
    {
        #region Inspector

        public Camera Camera = null;
        public CameraFOVPlane FOVPlane = null;
        public Transform RootTransform = null;
        public Transform EffectsTransform = null;

        #endregion // Inspector
    }
}