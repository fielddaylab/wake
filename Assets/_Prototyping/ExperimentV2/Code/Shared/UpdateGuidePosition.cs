using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauUtil;
using BeauRoutine;

namespace ProtoAqua.ExperimentV2
{
    public class UpdateGuidePosition : MonoBehaviour {
        public static float lookSpeed = 120f;

        static public IEnumerator MoveGuide(Transform v1ctorCurrent, Transform target) {
            yield return v1ctorCurrent.MoveTo(target.position, 0.5f).Ease(Curve.CubeOut);
        }
    }
}
