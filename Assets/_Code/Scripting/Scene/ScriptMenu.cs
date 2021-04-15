using BeauUtil;
using UnityEngine;
using System.Collections;
using BeauUtil.Debugger;
using Leaf;
using System;
using BeauUtil.Variants;
using Leaf.Runtime;
using BeauRoutine.Extensions;

namespace Aqua.Scripting
{
    public class ScriptMenu : ScriptComponent
    {
        #region Inspector

        [SerializeField] private BasePanel m_Menu = null;

        #endregion // Inspector

        [LeafMember("ShowMenu")]
        public void Show()
        {
            m_Menu.Show();
        }

        [LeafMember("HideMenu")]
        public void Hide()
        {
            m_Menu.Hide();
        }
    }
}