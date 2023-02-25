using UnityEngine;
using Aqua;
using BeauUtil;
using BeauData;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Text;

namespace Aqua.JobBoard
{
    public class ListHeader : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private LocText m_Label = null;

        #endregion // Inspector

        [NonSerialized] private Transform m_Transform;
        [NonSerialized] public int ListCount;

        public Transform Transform { get { return this.CacheComponent(ref m_Transform); } }

        public void SetText(TextId text)
        {
            if (!text.IsEmpty)
            {
                m_Label.SetText(text);
            }
        }

        public void SetText(StringBuilder text)
        {
            if (text.Length > 0)
            {
                m_Label.SetTextNoParse(text);
            }
        }
    }
}
