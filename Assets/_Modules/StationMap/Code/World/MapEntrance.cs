using UnityEngine;
using BeauUtil;
using System;
using Aqua.Scripting;

namespace Aqua.StationMap
{
    public class MapEntrance : ScriptComponent
    {
        #region Inspector

        [Header("Components")]
        [SerializeField, Required] private Collider2D m_Collider = null;
        [SerializeField, MapId] private SerializedHash32 m_MapId = null;

        #endregion // Inspector

        private void Awake()
        {
            WorldUtils.ListenForPlayer(m_Collider, OnPlayerEnter, OnPlayerExit);
        }

        private void OnPlayerEnter(Collider2D other)
        {
            if (Services.Data.CompareExchange(GameVars.InteractObject, null, Parent.Id()))
            {
                Services.UI.FindPanel<NavigationUI>().DisplayMap(m_Collider.transform, m_MapId);
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
