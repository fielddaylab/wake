using System;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class CursorTooltip : MonoBehaviour
    {
        private enum TooltipDirection
        {
            Left,
            Right,
            Up,
            Down
        }

        #region Inspector

        [SerializeField] private RectTransform m_Bounds = null;
        [SerializeField] private RectTransform m_Self = null;
        [SerializeField] private LayoutGroup m_Layout = null;
        [SerializeField] private LocText m_Text = null;
        [SerializeField] private float m_CursorOffset = 16;
        [SerializeField] private float m_EdgeOffset = 16;
        
        #endregion // Inspector
        
        private StringHash32 m_LastId;

        public void Process(Vector2 inCursorPosition)
        {
            if (m_LastId.IsEmpty)
                return;

            Vector2 offset = default(Vector2);
            TooltipDirection bestDirection = FindBestDirection(ref inCursorPosition);
            switch(bestDirection)
            {
                case TooltipDirection.Left:
                    SetPivot(1, 0.5f);
                    offset.x = -m_CursorOffset;
                    break;

                case TooltipDirection.Right:
                    SetPivot(0, 0.5f);
                    offset.x = m_CursorOffset;
                    break;

                case TooltipDirection.Up:
                    SetPivot(0.5f, 0);
                    offset.y = m_CursorOffset;
                    break;

                case TooltipDirection.Down:
                    SetPivot(0.5f, 1);
                    offset.y = -m_CursorOffset;
                    break;
            }

            inCursorPosition.x = (float) Math.Round(inCursorPosition.x + offset.x);
            inCursorPosition.y = (float) Math.Round(inCursorPosition.y + offset.y);
            m_Self.position = inCursorPosition;
        }

        private TooltipDirection FindBestDirection(ref Vector2 ioPosition)
        {
            Rect parentBounds = m_Bounds.rect;
            Vector2 localParentPos = m_Bounds.position;
            parentBounds.x += localParentPos.x + m_EdgeOffset;
            parentBounds.y += localParentPos.y + m_EdgeOffset;
            parentBounds.width -= m_EdgeOffset * 2;
            parentBounds.height -= m_EdgeOffset * 2;

            Vector2 selfSize = m_Self.sizeDelta;
            Rect selfBounds = new Rect(ioPosition.x - selfSize.x * 0.5f, ioPosition.y - selfSize.y * 0.5f, selfSize.x, selfSize.y);
            selfBounds.center = ioPosition = Geom.Constrain(ioPosition, default(Vector2), parentBounds);

            if (CheckOffsetRect(parentBounds, selfBounds, 0, -m_CursorOffset, 0, -0.5f))
                return TooltipDirection.Down;

            if (CheckOffsetRect(parentBounds, selfBounds, 0, m_CursorOffset, 0, 0.5f))
                return TooltipDirection.Up;
            
            if (selfBounds.x < parentBounds.center.x)
                return TooltipDirection.Right;

            return TooltipDirection.Left;
        }

        static private bool CheckOffsetRect(in Rect inParentBounds, Rect inSelfBounds, float inXOffset, float inYOffset, float inPivotOffsetX, float inPivotOffsetY)
        {
            inSelfBounds.x += inXOffset + inPivotOffsetX * inSelfBounds.width;
            inSelfBounds.y += inYOffset + inPivotOffsetY * inSelfBounds.height;
            return inSelfBounds.x >= inParentBounds.x && inSelfBounds.y >= inParentBounds.y
                && inSelfBounds.xMax <= inParentBounds.xMax && inSelfBounds.yMax <= inParentBounds.yMax;
        }

        private void SetPivot(float inX, float inY)
        {
            m_Self.pivot = new Vector2(inX, inY);
        }

        public void SetId(StringHash32 inId)
        {
            if (inId == m_LastId)
                return;

            m_LastId = inId;

            if (inId.IsEmpty)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                m_Text.SetText(inId);
                m_Layout.ForceRebuild();
            }
        }

        public void Clear()
        {
            SetId(StringHash32.Null);
        }
    }
}