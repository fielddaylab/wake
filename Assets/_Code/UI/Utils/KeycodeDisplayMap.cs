using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Keycode Display Map")]
    public class KeycodeDisplayMap : ScriptableObject
    {
        public struct Mapping
        {
            public Sprite Image;
            public string Text;

            public Mapping(Sprite inSprite, string inText = null)
            {
                Image = inSprite;
                Text = inText;
            }
        }

        #region Inspector

        [SerializeField] private Sprite m_MouseLeft = null;
        [SerializeField] private Sprite m_MouseRight = null;
        [SerializeField] private Sprite m_Tap = null;
        [SerializeField] private Sprite m_SmallKey = null;
        [SerializeField] private Sprite m_MediumKey = null;
        [SerializeField] private Sprite m_WideKey = null;

        #endregion // Inspector

        public bool TouchscreenMode;

        [NonSerialized] private Dictionary<int, Mapping> m_MappingCache = new Dictionary<int, Mapping>();

        public Mapping ForKey(KeyCode inKeycode)
        {
            if (inKeycode >= KeyCode.Mouse0 && inKeycode <= KeyCode.Mouse6)
                return ForMouse(inKeycode - KeyCode.Mouse0);

            Mapping mapping;
            if (m_MappingCache.TryGetValue((int) inKeycode, out mapping))
                return mapping;

            switch(inKeycode)
            {
                case KeyCode.LeftControl:
                    mapping.Image = m_MediumKey;
                    mapping.Text = "Ctrl";
                    break;

                case KeyCode.LeftShift:
                    mapping.Image = m_MediumKey;
                    mapping.Text = "Shift";
                    break;

                case KeyCode.Return:
                case KeyCode.Tab:
                case KeyCode.CapsLock:
                case KeyCode.Backspace:
                    mapping.Image = m_MediumKey;
                    break;

                case KeyCode.Space:
                    mapping.Image = m_WideKey;
                    break;

                default:
                    mapping.Image = m_SmallKey;
                    break;
            }

            if (string.IsNullOrEmpty(mapping.Text))
                mapping.Text = inKeycode.ToString();

            m_MappingCache[(int) inKeycode] = mapping;
            return mapping;
        }

        public Mapping ForMouse(int inMouseButton)
        {
            switch(inMouseButton)
            {
                case 0:
                    return new Mapping(TouchscreenMode ? m_Tap : m_MouseLeft);
                case 1:
                    return new Mapping(m_MouseRight);
                default:
                    throw new ArgumentOutOfRangeException("inMouseButton");
            }
        }
    }
}