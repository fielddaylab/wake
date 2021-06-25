using BeauUtil;
using UnityEngine;

namespace Aqua.Cameras
{
    [DisallowMultipleComponent]
    public class CameraRig : MonoBehaviour, ISceneOptimizable
    {
        #region Inspector

        [Header("Objects")]
        [Required(ComponentLookupDirection.Children)] public Camera Camera = null;
        [Required(ComponentLookupDirection.Children)] public CameraFOVPlane FOVPlane = null;
        [Required] public Transform RootTransform = null;
        [Required] public Transform EffectsTransform = null;

        [Header("Settings")]
        public CameraMode DefaultMode = CameraMode.Scripted;

        [SerializeField, HideInInspector] public CameraTarget InitialTarget;

        #endregion // Inspector

        public void SetPlane(Transform inPlane)
        {
            FOVPlane.Target = inPlane;
        }

        public void SetPlane(Transform inPlane, float inHeight)
        {
            FOVPlane.Target = inPlane;
            FOVPlane.Height = inHeight;
        }

        public void SetHeight(float inHeight)
        {
            FOVPlane.Height = inHeight;
        }

        #region ISceneOptimizable

        void ISceneOptimizable.Optimize()
        {
            InitialTarget = null;
            
            var potentialTargets = FindObjectsOfType<CameraTarget>();
            foreach(var target in potentialTargets)
            {
                if (target.isActiveAndEnabled)
                {
                    InitialTarget = target;
                    break;
                }
            }
        }

        #endregion // ISceneOptimizable
    }
}