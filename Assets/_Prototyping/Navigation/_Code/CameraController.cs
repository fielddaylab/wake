using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
namespace ProtoAqua.Navigation {
    public class CameraController : MonoBehaviour {

        [SerializeField] Camera mainCamera = null;
        [SerializeField] Transform target = null;

        [SerializeField] float maxX = 0;
        [SerializeField] float minX = 0;
        [SerializeField] float maxY = 0;
        [SerializeField] float minY = 0;
        
        [SerializeField] float lerpStrength = 4;


        // Start is called before the first frame update
        void Start() {
            
        }

        
        // Update is called once per frame
        void Update() {
            Vector2 targetPos = new Vector2(
                Mathf.Clamp(target.position.x, minX, maxX),
                Mathf.Clamp(target.position.y, minY, maxY)
            );

            Vector2 lerp = Vector2.Lerp(
                transform.position, targetPos, TweenUtil.Lerp(lerpStrength)
            );

            transform.SetPosition(lerp, Axis.XY, Space.World);
        }

        public Vector3 ScreenToWorldOnPlane(Vector2 inScreenPos, Transform inWorldRef) {
                Vector3 screenPos = inScreenPos;
                screenPos.z = 1;

                Plane p = new Plane(-mainCamera.transform.forward, inWorldRef.position);
                Ray r = mainCamera.ScreenPointToRay(screenPos);

                float dist;
                p.Raycast(r, out dist);

                return r.GetPoint(dist);
            }

        #if UNITY_EDITOR

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                var comp = GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (comp)
                    comp.hideFlags = HideFlags.DontSave;
            }
        }

        #endif // UNITY_EDITOR
    }
}
