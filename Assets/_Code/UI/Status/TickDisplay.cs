using UnityEngine;
using System;
using UnityEngine.UI;

namespace Aqua
{
    public class TickDisplay : MonoBehaviour
    {
        [SerializeField] private Transform m_TickContainer = null;
        [SerializeField] private Color m_EnabledColor = Color.yellow;
        [SerializeField] private Color m_DisabledColor = Color.white;

        [NonSerialized] private Graphic[] m_Ticks;

        public void Display(int inTickCount)
        {
            if (m_Ticks == null)
                m_Ticks = m_TickContainer.GetComponentsInChildren<Graphic>(true);

            if (inTickCount > m_Ticks.Length)
            {
                Debug.LogErrorFormat("[TickDisplay] Too many ticks {0} to display with {1} objects", inTickCount, m_Ticks.Length);
                inTickCount = m_Ticks.Length;
            }

            for(int i = 0; i < inTickCount; ++i)
                m_Ticks[i].color = m_EnabledColor;
            for(int i = inTickCount; i < m_Ticks.Length; ++i)
                m_Ticks[i].color = m_DisabledColor;
        }
        
    }
}