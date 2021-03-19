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

        public CursorHintMgr(InputCursor inCursor)
        {
            m_Cursor = inCursor;
        }

        public void SetHint(CursorInteractionHint inHint)
        {
            Assert.NotNull(inHint);

            if (m_CurrentHint != inHint)
            {
                m_CurrentHint = inHint;
                m_Cursor.SetInteractionHint(true);
            }
        }

        public void ClearHint(CursorInteractionHint inHint)
        {
            Assert.NotNull(inHint);

            if (m_CurrentHint == inHint)
            {
                m_CurrentHint = null;
                m_Cursor.SetInteractionHint(false);
            }
        }
    }
}