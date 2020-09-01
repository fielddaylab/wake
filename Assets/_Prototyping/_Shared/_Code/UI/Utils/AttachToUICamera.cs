using UnityEngine;
using BeauRoutine.Extensions;
using BeauRoutine;
using System.Collections;
using BeauUtil;
#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
#endif // UNITY_EDITOR

namespace ProtoAqua
{
    [RequireComponent(typeof(Canvas)), ExecuteAlways]
    public class AttachToUICamera : MonoBehaviour
    {
        private void OnEnable()
        {
            #if UNITY_EDITOR
            if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
                return;
            #endif // UNITY_EDITOR

            Camera camera;
            if (TransformHelper.TryGetCameraFromLayer(transform, out camera))
                GetComponent<Canvas>().worldCamera = camera;
        }

        private void OnDisable()
        {
            #if UNITY_EDITOR
            if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
                return;
            #endif // UNITY_EDITOR

            GetComponent<Canvas>().worldCamera = null;
        }
    }
}