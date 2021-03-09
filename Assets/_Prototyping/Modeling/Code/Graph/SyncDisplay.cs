using Aqua;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using UnityEngine.UI.Extensions;
using System;
using TMPro;

namespace ProtoAqua.Modeling
{
    public class SyncDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private TMP_Text m_Text = null;

        #endregion // Inspector

        private int m_LastSync = -1;

        public void Display(int inSync)
        {
            if (m_LastSync != inSync)
            {
                m_Text.SetText(inSync.ToStringLookup() + "%");
                m_LastSync = inSync;
            }
        }
    }
}