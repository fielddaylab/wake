using BeauUtil;
using UnityEngine;
using System.Collections;
using BeauUtil.Debugger;
using Leaf;
using System;
using BeauUtil.Variants;
using Leaf.Runtime;
using BeauRoutine.Extensions;
using UnityEngine.Scripting;

namespace Aqua.Scripting
{
    [AddComponentMenu("Aqualab/Scripting/Script Menu")]
    public class ScriptMenu : ScriptComponent
    {
        #region Inspector

        [SerializeField] private BasePanel m_Menu = null;

        #endregion // Inspector

        [LeafMember("Show"), Preserve]
        public void Show()
        {
            m_Menu.Show();
        }

        [LeafMember("Hide"), Preserve]
        public void Hide()
        {
            m_Menu.Hide();
        }
    }
}