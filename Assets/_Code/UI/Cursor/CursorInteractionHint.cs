using System;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aqua
{
    public class CursorInteractionHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [NonSerialized] private int m_EnterMask = 0;

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            int mask = 1 << ((eventData.pointerId + 32) % 32);
            if ((m_EnterMask & mask) != 0)
                return;

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
    }
}