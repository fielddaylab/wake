using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEngine.UI {
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public class LayoutOffset : MonoBehaviour, ICanvasElement, ILayoutElement, ILayoutSelfController {
        private enum CallbackMode : byte {
            None,
            CanvasElement,
            LayoutElement
        }

        #region Inspector

        [SerializeField] private Vector2 m_Offset0 = default(Vector2);
        [SerializeField] private Vector2 m_Offset1 = default(Vector2);

        #endregion // Inspector

        [NonSerialized] private RectTransform m_Rect;
        [NonSerialized] private Vector2 m_AppliedOffset;
        [NonSerialized] private CallbackMode m_RebuildMode;
        [NonSerialized] private bool m_InLayoutGroup;

        public Vector2 Offset0 {
            get { return m_Offset0; }
            set {
                if (m_Offset0 != value) {
                    m_Offset0 = value;
                    TryUpdate();
                }
            }
        }

        public Vector2 Offset1 {
            get { return m_Offset1; }
            set {
                if (m_Offset1 != value) {
                    m_Offset1 = value;
                    TryUpdate();
                }
            }
        }

        #region Handlers

        private void OnEnable() {
            if (!m_Rect) {
                m_Rect = (RectTransform) transform;
            }
            Transform parent = m_Rect.parent;
            m_InLayoutGroup = parent && parent.GetComponent<ILayoutGroup>() != null;
            CanvasUpdateRegistry.TryRegisterCanvasElementForLayoutRebuild(this);
        }

        private void OnDisable() {
            if (m_Rect) {
                ApplyOffset(default(Vector2));
            }
        }

        private void OnTransformParentChanged() {
            if (object.ReferenceEquals(m_Rect, null)) {
                m_Rect = (RectTransform) transform;
            }
            Transform parent = m_Rect.parent;
            m_InLayoutGroup = parent && parent.GetComponent<ILayoutGroup>() != null;
        }

        [Preserve]
        private void OnDidApplyAnimationProperties() {
            TryUpdate();
        }

        #if UNITY_EDITOR

        private void OnValidate() {
            TryUpdate();
        }

        #endif // UNITY_EDITOR

        #endregion // Handlers

        #region Updates

        private void TryUpdate() {
            // if (m_InLayoutGroup) {
            //     CanvasUpdateRegistry.TryRegisterCanvasElementForLayoutRebuild(this);
            // }
            
            ApplyOffset(m_Offset0 + m_Offset1);
        }

        private void ApplyOffset(Vector2 offset) {
            Vector2 delta = offset - m_AppliedOffset;
            m_AppliedOffset = offset;

            if (delta.x != 0 || delta.y != 0) {
                if (object.ReferenceEquals(m_Rect, null)) {
                    m_Rect = (RectTransform) transform;
                }
                m_Rect.anchoredPosition += delta;
            }
        }

        #endregion // Updates

        #region ICanvasElement

        Transform ICanvasElement.transform { get { return m_Rect; } }

        void ICanvasElement.Rebuild(CanvasUpdate executing) {
            switch(executing) {
                case CanvasUpdate.Prelayout: {
                    if (m_RebuildMode == CallbackMode.None) {
                        m_RebuildMode = CallbackMode.CanvasElement;
                        // Debug.LogFormat("[LayoutOffset] ({0}) CanvasUpdate.PreLayout", gameObject.name);
                        ApplyOffset(default(Vector2));
                    }
                    break;
                }
                case CanvasUpdate.PostLayout: {
                    if (m_RebuildMode == CallbackMode.CanvasElement) {
                        m_RebuildMode = CallbackMode.None;
                        ApplyOffset(m_Offset0 + m_Offset1);
                        // Debug.LogFormat("[LayoutOffset] ({0}) CanvasUpdate.PostLayout", gameObject.name);
                    }
                    break;
                }
            }
        }

        void ICanvasElement.LayoutComplete() { }

        void ICanvasElement.GraphicUpdateComplete() { }

        bool ICanvasElement.IsDestroyed() {
            return !this;
        }

        #endregion // ICanvasElement

        #region ILayoutElement

        float ILayoutElement.minWidth  { get { return -1; } }

        float ILayoutElement.preferredWidth { get { return -1; } }

        float ILayoutElement.flexibleWidth { get { return -1; } }

        float ILayoutElement.minHeight { get { return -1; } }

        float ILayoutElement.preferredHeight { get { return -1; } }

        float ILayoutElement.flexibleHeight { get { return -1; } }

        int ILayoutElement.layoutPriority { get { return 0; } }

        void ILayoutElement.CalculateLayoutInputHorizontal() {
            if (m_RebuildMode == CallbackMode.None) {
                m_RebuildMode = CallbackMode.LayoutElement;
                // Debug.LogFormat("[LayoutOffset] ({0}) CalculateLayoutInputHorizontal", gameObject.name);
                ApplyOffset(default(Vector2));
            }
        }

        void ILayoutElement.CalculateLayoutInputVertical() {
        }

        void ILayoutController.SetLayoutHorizontal() {
        }

        void ILayoutController.SetLayoutVertical() {
            if (m_RebuildMode == CallbackMode.LayoutElement) {
                m_RebuildMode = CallbackMode.None;
                ApplyOffset(m_Offset0 + m_Offset1);
                // Debug.LogFormat("[LayoutOffset] ({0}) SetLayoutVertical", gameObject.name);
            }
        }

        #endregion // ILayoutElement
    }
}