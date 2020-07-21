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
    public class ConfigPropertyCollapsible : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private RectTransform m_RectTransform = null;
        [SerializeField] private CanvasGroup m_CanvasGroup = null;

        #endregion // Inspector

        [NonSerialized] private string m_SelfPath;
        [NonSerialized] private string m_FullPath;
        [NonSerialized] private bool m_SelfCollapsed;
        [NonSerialized] private bool m_ChildrenCollapsed;
    }
}