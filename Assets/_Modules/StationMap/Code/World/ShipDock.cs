using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using UnityEngine.SceneManagement;
using Aqua;
using BeauUtil;
using System;

namespace Aqua.StationMap
{
    public class ShipDock : MonoBehaviour
    {
        static public readonly StringHash32 DefaultId = "playerShip";

        #region Inspector

        [Header("Components")]
        [SerializeField, Required] private Collider2D m_Collider = null;
        [SerializeField, Required] private Transform m_PlayerSpawnLocation = null;

        #endregion // Inspector

        [NonSerialized] private StringHash32 m_Id;

        public Transform PlayerSpawnLocation { get { return m_PlayerSpawnLocation; } }

        private void Awake()
        {
            var listener = m_Collider.EnsureComponent<TriggerListener2D>();
            listener.FilterByComponentInParent<PlayerController>();
            listener.onTriggerEnter.AddListener(OnPlayerEnter);
            listener.onTriggerExit.AddListener(OnPlayerExit);

            m_Id = ScriptObject.FindId(this, DefaultId);
        }

        private void OnPlayerEnter(Collider2D other)
        {
            if (Services.Data.CompareExchange(GameVars.InteractObject, null, m_Id))
            {
                Services.UI.FindPanel<NavigationUI>().DisplayDock(m_Collider.transform);
            }
        }

        private void OnPlayerExit(Collider2D other)
        {
            if (Services.Data)
            {
                if (Services.Data.CompareExchange(GameVars.InteractObject, m_Id, null))
                {
                    Services.UI?.FindPanel<NavigationUI>()?.Hide();
                }
            }
        }
    }

}
