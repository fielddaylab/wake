using System;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class InputCursor : MonoBehaviour
    {
        [SerializeField] private Image m_Image = null;
        [SerializeField] private Sprite m_InteractSprite = null;
        
        [NonSerialized] private RectTransform m_Transform;
        [NonSerialized] private Sprite m_OriginalSprite;

        [NonSerialized] private StringHash32 m_PosLock;
        [NonSerialized] private StringHash32 m_SpriteLock;
        [NonSerialized] private bool m_HasInteraction;

        static private readonly Vector3 MouseDownScale = new Vector3(0.75f, 0.75f, 1);
        static private readonly Vector3 MouseUpScale = new Vector3(1, 1, 1);

        #region Events

        private void Awake()
        {
            this.CacheComponent(ref m_Transform);
            m_OriginalSprite = m_Image.sprite;
        }

        private void OnEnable()
        {
            Cursor.visible = false;
            this.CacheComponent(ref m_Transform);
        }

        private void OnDisable()
        {
            Cursor.visible = true;
        }

        public void DoUpdate()
        {
            Cursor.visible = false;
            if (m_PosLock.IsEmpty)
            {
                m_Transform.position = Input.mousePosition;
            }

            if (m_SpriteLock.IsEmpty)
            {
                m_Image.sprite = m_HasInteraction ? m_InteractSprite : m_OriginalSprite;
                if (Input.GetMouseButton(0))
                {
                    m_Transform.localScale = MouseDownScale;
                }
                else
                {
                    m_Transform.localScale = MouseUpScale;
                }
            }
        }

        #endregion // Events

        #region Position Lock

        public void LockPosition(StringHash32 inHash, Vector2 inScreenPosition)
        {
            Assert.True(m_PosLock.IsEmpty || m_PosLock == inHash, "Current position lock is {0}, attempting to lock with mismatched key {1}", m_PosLock.ToDebugString(), inHash.ToDebugString());
            m_PosLock = inHash;
            m_Transform.position = inScreenPosition;
        }

        public void ReleasePosition(StringHash32 inHash)
        {
            Assert.True(inHash == m_PosLock, "Current position lock is {0}, attempting to unlock with mismatched key {1}", m_PosLock.ToDebugString(), inHash.ToDebugString());
            m_PosLock = StringHash32.Null;
        }
    
        #endregion // Position Lock

        #region Interaction

        public void SetInteractionHint(bool inbHint)
        {
            m_HasInteraction = inbHint;
        }

        #endregion // Interaction
    }
}