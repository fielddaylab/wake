using System;
using UnityEngine;
using UnityEngine.UI;
using Aqua;
using BeauUtil.Debugger;

public class ScrollRectTest : MonoBehaviour {
    [NonSerialized] private ScrollRect m_Parent;
    [NonSerialized] private RectTransform m_Rect;
    [NonSerialized] private bool m_Visible = false;

    private void Awake() {
        m_Parent = GetComponentInParent<ScrollRect>();
        m_Rect = (RectTransform) transform;
    }

    private void LateUpdate() {
        bool isVisible = m_Parent.IsVisible(m_Rect);
        if (m_Visible != isVisible) {
            m_Visible = isVisible;
            Log.Msg("'{0}' is {1}", name, isVisible ? "visible" : "not visible");
        }
    }
}