using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauUtil;
using System.Collections;
using BeauPools;
using System;
using Aqua.Scripting;
using Leaf.Runtime;
using BeauUtil.Debugger;

namespace Aqua
{
    public class FocusHighlight : ScriptComponent
    {
        #region Inspector

        [SerializeField] private Canvas m_Canvas = null;
        [SerializeField] private CanvasGroup m_CanvasGroup = null;
        [SerializeField] private RectTransform m_Transform = null;
        [SerializeField] private TweenSettings m_ToOnAnim = new TweenSettings(0.5f);
        [SerializeField] private TweenSettings m_ToOffAnim = new TweenSettings(0.5f);

        #endregion // Inspector

        [NonSerialized] private Vector2 m_DefaultSize;
        [NonSerialized] private CanvasSpaceTransformation m_SpaceHelper;
        private Routine m_Anim;
        [NonSerialized] private bool m_Activated;

        private void Awake()
        {
            m_SpaceHelper.CanvasCamera = m_Canvas.worldCamera;
            m_SpaceHelper.CanvasSpace = m_Transform;
            m_Canvas.enabled = false;
            m_DefaultSize = m_Transform.sizeDelta;
        }

        public void Focus(Transform inTransform, Vector2 inSize, float inAlpha)
        {
            bool bSnap = !m_Canvas.enabled;
            m_Canvas.enabled = true;

            Vector2 size = GetSize(inSize);
            Vector2 localPos = GetLocation(inTransform);
            if (bSnap)
            {
                m_Transform.sizeDelta = size;
                m_Transform.localPosition = localPos;
                m_CanvasGroup.alpha = 0;
            }
            
            m_Anim.Replace(this, Activate(localPos, size, inAlpha));
            m_Activated = true;
        }

        [LeafMember]
        public void Focus(StringSlice inObjectId, float inWidth = 0, float inHeight = 0, float inAlpha = 0.6f)
        {
            if (Services.UI.IsSkippingCutscene())
                return;

            ScriptObject obj;
            if (!Services.Script.TryGetScriptObjectById(inObjectId, out obj))
            {
                Log.Error("[FocusHighlight] Unable to find script object with id '{0}'", inObjectId);
                return;
            }

            Focus(obj.transform, new Vector2(inWidth, inHeight), inAlpha);
        }

        [LeafMember("Clear")]
        public void Hide(bool inbInstant = false)
        {
            inbInstant |= Services.UI.IsSkippingCutscene();

            if (inbInstant)
            {
                m_Anim.Stop();
                m_Canvas.enabled = false;
                m_Activated = false;
                return;
            }

            if (!m_Activated)
                return;

            m_Anim.Replace(this, Deactivate());
            m_Activated = false;
        }

        private IEnumerator Activate(Vector2 inPos, Vector2 inSize, float inAlpha)
        {
            return Routine.Combine(
                m_Transform.SizeDeltaTo(inSize, m_ToOnAnim),
                m_Transform.MoveTo(inPos, m_ToOnAnim, Axis.XY, Space.Self),
                m_CanvasGroup.FadeTo(inAlpha, m_ToOnAnim.Time)
            );
        }

        private IEnumerator Deactivate()
        {
            yield return m_CanvasGroup.FadeTo(0, m_ToOffAnim.Time);
            m_Canvas.enabled = false;
        }

        private Vector2 GetLocation(Transform inFocusTransform)
        {
            Vector3 pos;
            inFocusTransform.TryGetCamera(out m_SpaceHelper.WorldCamera);
            m_SpaceHelper.TryConvertToLocalSpace(inFocusTransform, out pos);
            return (Vector2) pos;
        }

        private Vector2 GetSize(Vector2 inSize)
        {
            if (inSize.x <= 0)
                inSize.x = m_DefaultSize.x;
            if (inSize.y <= 0)
                inSize.y = m_DefaultSize.y;
            return inSize;
        }
    }
}