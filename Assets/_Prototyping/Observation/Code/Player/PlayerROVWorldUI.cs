using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using Aqua;

namespace ProtoAqua.Observation
{
    public class PlayerROVWorldUI : MonoBehaviour
    {
        static private readonly StringHash32 CursorLockKey = "PlayerROVWorldUI";

        #region Inspector

        [Header("Animations")]
        [SerializeField] private Sprite m_MoveSlow = null;
        [SerializeField] private Sprite m_MoveFast = null;
        [SerializeField] private Sprite m_ScanOff = null;
        [SerializeField] private Sprite m_ScanOn = null;

        #endregion // Inspector

        [NonSerialized] private Transform m_Transform;
        [NonSerialized] private bool m_PositionLocked;
        [NonSerialized] private bool m_RotationLocked;
        [NonSerialized] private bool m_CursorOn;

        private void OnDisable()
        {
            if (Services.UI)
                HideCursor();
        }

        public void ShowMoveArrow(Vector2 inOffset, float inPower)
        {
            SetCursorOn(true);
            ReleasePositionLock();
            
            Services.UI.Cursor.LockSprite(CursorLockKey, inPower > 0.5f ? m_MoveFast : m_MoveSlow);
            
            m_RotationLocked = true;
            Services.UI.Cursor.LockRotation(CursorLockKey, Mathf.Atan2(inOffset.y, inOffset.x) * Mathf.Rad2Deg);
        }

        public void ShowScan(Vector2 inOffset, bool inbScanning)
        {
            SetCursorOn(true);
            ReleaseRotationLock();

            if (inbScanning)
            {
                Services.UI.Cursor.LockSprite(CursorLockKey, m_ScanOn);
                LockPosition(inOffset);
            }
            else
            {
                Services.UI.Cursor.LockSprite(CursorLockKey, m_ScanOff);
                ReleasePositionLock();
            }
        }

        public void HideCursor()
        {
            SetCursorOn(false);
        }

        private void SetCursorOn(bool inbOn)
        {
            if (m_CursorOn == inbOn)
                return;

            if (!inbOn)
            {
                ReleasePositionLock();
                ReleaseRotationLock();
                Services.UI.Cursor.ReleaseSprite(CursorLockKey);
            }
            
            m_CursorOn = inbOn;
        }

        private void LockPosition(Vector2 inOffset)
        {
            Vector2 screenPos = Services.Camera.Current.WorldToScreenPoint(this.CacheComponent(ref m_Transform).position + (Vector3) inOffset);
            Services.UI.Cursor.LockPosition(CursorLockKey, screenPos);
            m_PositionLocked = true;
        }

        private void ReleasePositionLock()
        {
            if (m_PositionLocked)
            {
                Services.UI.Cursor.ReleasePosition(CursorLockKey);
                m_PositionLocked = false;
            }
        }

        private void ReleaseRotationLock()
        {
            if (m_RotationLocked)
            {
                Services.UI.Cursor.ReleaseRotation(CursorLockKey);
                m_RotationLocked = false;
            }
        }
    }
}