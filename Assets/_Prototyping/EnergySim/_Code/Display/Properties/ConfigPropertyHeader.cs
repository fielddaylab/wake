using System;
using System.Collections;
using System.Runtime.InteropServices;
using Aqua;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoAqua.Energy
{
    public class ConfigPropertyHeader : MonoBehaviour, IPooledObject<ConfigPropertyHeader>
    {
        #region Inspector
        
        [SerializeField] private IndentGroup m_Indent = null;
        [SerializeField] private TMP_Text m_Label = null;

        #endregion // Inspector

        public void Configure(string inLabel, int inIndent)
        {
            m_Label.SetText(inLabel);
            m_Indent.SetIndent(inIndent);
        }

        #region IPooledObject

        void IPooledObject<ConfigPropertyHeader>.OnAlloc()
        {
            // throw new NotImplementedException();
        }

        void IPooledObject<ConfigPropertyHeader>.OnConstruct(IPool<ConfigPropertyHeader> inPool)
        {
            // throw new NotImplementedException();
        }

        void IPooledObject<ConfigPropertyHeader>.OnDestruct()
        {
            // throw new NotImplementedException();
        }

        void IPooledObject<ConfigPropertyHeader>.OnFree()
        {
            m_Label.SetText(string.Empty);
            m_Indent.SetIndent(0);
        }

        #endregion // IPooledObject
    }
}