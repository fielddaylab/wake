using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aqua
{
    public class CursorHintMgr
    {
        private CursorInteractionHint m_CurrentHint;
        private InputCursor m_Cursor;
        private CursorTooltip m_Tooltip;

        private float m_TooltipTimer = 0;

        public CursorHintMgr(InputCursor inCursor, CursorTooltip inTooltip)
        {
            m_Cursor = inCursor;
            m_Tooltip = inTooltip;
        }

        public void SetHint(CursorInteractionHint inHint)
        {
            Assert.NotNull(inHint);

            if (m_CurrentHint != inHint)
            {
                m_CurrentHint = inHint;
                m_Cursor.SetInteractionHint(m_CurrentHint.IsInteractable());
            }
        }

        public void Process(float inTooltipTime)
        {
            if (!m_CurrentHint.IsReferenceNull())
            {
                bool bInteractable = m_CurrentHint.IsInteractable();
                m_Cursor.SetInteractionHint(bInteractable);
                if (bInteractable)
                {
                    m_TooltipTimer += Time.deltaTime;
                    if (m_TooltipTimer >= inTooltipTime)
                    {
                        m_TooltipTimer = inTooltipTime;
                        m_Tooltip.SetId(m_CurrentHint.TooltipId);
                    }
                }
                else
                {
                    if (m_TooltipTimer > 0)
                    {
                        m_Tooltip.Clear();
                        m_TooltipTimer = 0;
                    }
                }
            }
        }

        public void ClearHint(CursorInteractionHint inHint)
        {
            Assert.NotNull(inHint);

            if (m_CurrentHint == inHint)
            {
                m_CurrentHint = null;
                m_Cursor.SetInteractionHint(false);
                m_TooltipTimer = 0;
                m_Tooltip.Clear();
            }
        }
    }
}