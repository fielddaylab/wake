using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauUtil;
using BeauRoutine;

public class UpdateGuidePosition : MonoBehaviour {
    public static float lookSpeed = 120f;

    static public IEnumerator MoveGuide(Transform v1ctorCurrent, Transform target) {
        Debug.Log("[UpdateGuidePosition] Guide should be moved to: " + target.position);
        v1ctorCurrent.position = Vector3.Slerp(v1ctorCurrent.transform.position, target.position, TweenUtil.Lerp(lookSpeed));

        yield break;
    }
}
