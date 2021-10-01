using System;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aqua
{
    [DisallowMultipleComponent]
    public class CursorInteractionHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// Tooltip string id.
        /// </summary>
        public TextId TooltipId;

        /// <summary>
        /// Tooltip text override.
        /// </summary>
        public string TooltipOverride;

        /// <summary>
        /// Cursor image type.
        /// </summary>
        [AutoEnum] public CursorImageType Cursor = CursorImageType.Select;

        [NonSerialized] private Selectable m_Selectable = null;
        [NonSerialized] private int m_EnterMask = 0;
        [NonSerialized] private bool m_Initialized = false;

        public bool IsInteractable()
        {
            if (!m_Initialized)
            {
                this.CacheComponent(ref m_Selectable);
                m_Initialized = true;
            }
            
            return m_Selectable.IsReferenceNull() || m_Selectable.IsInteractable();
        }

        #region Handlers

        private void OnDisable()
        {
            if (m_EnterMask != 0)
            {
                m_EnterMask = 0;
                Services.UI?.CursorHintMgr.ClearHint(this);
            }
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            int mask = 1 << ((eventData.pointerId + 32) % 32);
            if ((m_EnterMask & mask) != 0)
                return;

            if (!m_Initialized)
            {
                this.CacheComponent(ref m_Selectable);
                m_Initialized = true;
            }
            m_EnterMask |= mask;
            Services.UI.CursorHintMgr.SetHint(this);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            int mask = 1 << ((eventData.pointerId + 32) % 32);
            if ((m_EnterMask & mask) == 0)
                return;

            m_EnterMask &= ~mask;
            if (Services.UI)
            {
                Services.UI.CursorHintMgr.ClearHint(this);
            }
        }

        #endregion // Handlers
    }
}