using System;
using UnityEngine;

namespace Aqua
{
    [Serializable]
    public class EnableDisableGroup
    {
        public GameObject[] Enabled;
        public GameObject[] Disabled;

        [NonSerialized] private bool m_Initialized;
        [NonSerialized] public bool EnabledState;

        static public bool SetEnabled(EnableDisableGroup inGroup, bool inbState)
        {
            if (inGroup.m_Initialized && inGroup.EnabledState == inbState)
                return false;

            inGroup.m_Initialized = true;
            inGroup.EnabledState = inbState;

            foreach(var enabledObj in inGroup.Enabled)
            {
                enabledObj.SetActive(inbState);
            }

            foreach(var disabledObj in inGroup.Disabled)
            {
                disabledObj.SetActive(!inbState);
            }

            return true;
        }
    }
}