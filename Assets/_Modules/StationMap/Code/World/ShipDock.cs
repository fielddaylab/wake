using UnityEngine;
using BeauUtil;
using System;
using Aqua.Scripting;

namespace Aqua.StationMap
{
    public class ShipDock : ScriptComponent
    {
        #region Inspector

        [Header("Components")]
        [SerializeField, Required] private Collider2D m_Collider = null;

        #endregion // Inspector

        [NonSerialized] private StringHash32 m_Id;

        private void Awake()
        {
            WorldUtils.ListenForPlayer(m_Collider, OnPlayerEnter, OnPlayerExit);
        }

        private void OnPlayerEnter(Collider2D other)
        {
            if (Services.Data.CompareExchange(GameVars.InteractObject, null, Parent.Id()))
            {
                Services.UI.FindPanel<NavigationUI>().DisplayDock(m_Collider.transform);
            }
        }

        private void OnPlayerExit(Collider2D other)
        {
            if (Services.Data)
            {
                if (Services.Data.CompareExchange(GameVars.InteractObject, Parent.Id(), null))
                {
                    Services.UI?.FindPanel<NavigationUI>()?.Hide();
                }
            }
        }
    }

}
