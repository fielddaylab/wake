using System;
using System.Collections;
using System.Runtime.InteropServices;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoCP
{
    public class CPLabel : CPControl
    {
        #region Inspector
        
        [SerializeField] private TMP_Text m_Label = null;

        #endregion // Inspector

        public void Configure(string inLabel)
        {
            m_Label.SetText(inLabel);
        }

        protected override void OnFree()
        {
            base.OnFree();
            
            m_Label.SetText(string.Empty);
        }

        public override FourCC Type()
        {
            return CPControlType.Label;
        }
    }
}