using UnityEngine;
using Aqua;
using BeauUtil;
using BeauData;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace Aqua.JobBoard
{
    public class ListHeader : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private LocText m_Label = null;

        #endregion // Inspector

        [NonSerialized] private Transform m_Transform;

        public Transform Transform { get { return this.CacheComponent(ref m_Transform); } }

        public void SetText(TextId text)
        {
            if (!text.IsEmpty)
            {
                m_Label.SetText(text);
            }
        }
    }
}
