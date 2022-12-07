using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Aqua;
using System;
using BeauRoutine;
using BeauUtil.Debugger;

public class GuideCursorFollow : WorldInput
{
    public Transform v1ctorHead;
    public SpriteRenderer sprite;
    public Vector2 spriteOffsetLimit;
    public float lookSpeed = 5f;

    [NonSerialized] private Vector2? m_OffsetTarget;
    [NonSerialized] private Vector2 m_SpriteOffset;
    [NonSerialized] private Vector3? m_LookAtTarget;
    [NonSerialized] private Plane m_LookPlane;
    [SerializeField] private float m_PlaneOffset = 15f;
    [SerializeField] private string m_DisableConditions = null;
    private bool m_Disabled = false;

    void Start() {
        m_LookPlane = new Plane(Vector3.back, v1ctorHead.position + Vector3.back * m_PlaneOffset);

        if (Services.Data.CheckConditions(m_DisableConditions)) {
            // guide should not get enabled
            m_Disabled = true;
            return;
        }

        Device.OnUpdate += UpdateInput;
    }

    void Update() {
        if (Script.IsLoading || Script.IsPaused || m_Disabled) {
            return;
        }

        UpdateAnim();
    }

    private void UpdateInput(DeviceInput input) {
        if (Script.IsPausedOrLoading) {
            return;
        }
        
        Vector2 pos = Services.Input.PointerOffsetFromCenter();
        m_OffsetTarget = pos * spriteOffsetLimit;
        m_LookAtTarget = Services.Camera.ScreenToPlanePosition(Input.mousePosition, m_LookPlane);
    }

    private void UpdateAnim() {
        if (m_OffsetTarget.HasValue) {
            m_SpriteOffset = Vector2.Lerp(m_SpriteOffset, m_OffsetTarget.Value, TweenUtil.Lerp(lookSpeed));
            // m_SpriteMaterial.SetVector("_spriteOffset", m_SpriteOffset);
        }

        if (m_LookAtTarget.HasValue) {
            Vector3 lookVec = m_LookAtTarget.Value - v1ctorHead.position;
            Quaternion look = Quaternion.LookRotation(lookVec);
            v1ctorHead.transform.rotation = Quaternion.Slerp(v1ctorHead.transform.rotation, look, TweenUtil.Lerp(lookSpeed));
        }
    }
}
