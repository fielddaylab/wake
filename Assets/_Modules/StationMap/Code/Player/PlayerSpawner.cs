using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using Aqua;
using BeauUtil;
using BeauUtil.Variants;

namespace Aqua.StationMap
{
    public class PlayerSpawner : MonoBehaviour, ISceneLoadHandler, ISceneOptimizable
    {
        [SerializeField, HideInInspector] private PlayerController m_Player = null;
        [SerializeField, HideInInspector] private DiveSite[] m_DiveSites;
        [SerializeField, HideInInspector] private ShipDock m_Dock;

        public void OnSceneLoad(SceneBinding inScene, object inContext)
        {
            StringHash32 entrance = Services.State.LastEntranceId;

            var job = Services.Data.Profile.Jobs.CurrentJob?.Job;

            foreach(var site in m_DiveSites)
            {
                site.CheckAllowed(job);
            }

            if (entrance.IsEmpty || entrance == "Ship")
            {
                m_Player.Teleport(m_Dock.PlayerSpawnLocation.position);
                return;
            }

            foreach(var site in m_DiveSites)
            {
                if (site.MapId == entrance)
                {
                    m_Player.Teleport(site.PlayerSpawnLocation.position);
                    break;
                }
            }
        }

        void ISceneOptimizable.Optimize()
        {
            List<DiveSite> diveSites = new List<DiveSite>(8);
            SceneHelper.ActiveScene().Scene.GetAllComponents<DiveSite>(true, diveSites);
            m_DiveSites = diveSites.ToArray();

            m_Dock = FindObjectOfType<ShipDock>();
            m_Player = FindObjectOfType<PlayerController>();
        }
    }
}