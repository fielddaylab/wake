using UnityEngine;
using System;
using UnityEngine.UI;
using BeauUtil.Debugger;
using BeauUtil;

namespace Aqua
{
    public class TickDisplay : MonoBehaviour
    {
        [SerializeField] private Transform m_TickContainer = null;
        [SerializeField] private Color m_EnabledColor = Color.yellow;
        [SerializeField] private Color m_DisabledColor = Color.white;
        [SerializeField] private Transform m_Target = null;

        [NonSerialized] private LayoutGroup m_TickLayout = null;
        [NonSerialized] private Graphic[] m_Ticks;

        public void Display(int inTickCount, int target = -1)
        {
            if (m_Ticks == null) {
                m_Ticks = m_TickContainer.GetComponentsInChildren<Graphic>(true);
                m_TickLayout = m_TickContainer.GetComponent<LayoutGroup>();
            }

            if (inTickCount > m_Ticks.Length)
            {
                Log.Error("[TickDisplay] Too many ticks {0} to display with {1} objects", inTickCount, m_Ticks.Length);
                inTickCount = m_Ticks.Length;
            }

            for(int i = 0; i < inTickCount; ++i)
                m_Ticks[i].color = m_EnabledColor;
            for(int i = inTickCount; i < m_Ticks.Length; ++i)
                m_Ticks[i].color = m_DisabledColor;

            if (m_TickLayout != null) {
                m_TickLayout.ForceRebuild();
            }

            if (m_Target != null) {
                if (target >= 0) {
                    m_Target.position = m_Ticks[target].transform.position;
                    m_Target.gameObject.SetActive(true);
                } else {
                    m_Target.gameObject.SetActive(false);
                }
            }
        }
        
    }
}