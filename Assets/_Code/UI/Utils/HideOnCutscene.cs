using System;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class HideOnCutscene : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Self)] private CanvasGroup m_Group = null;

        #endregion // Inspector
    }
}