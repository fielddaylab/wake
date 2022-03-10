using System.Collections.Generic;
using UnityEngine;
using BeauUtil;
using Aqua.Character;
using ScriptableBake;

namespace Aqua.StationMap
{
    public class DiveSiteMap : MonoBehaviour, ISceneLoadHandler, IBaked
    {
        [SerializeField, HideInInspector] private DiveSite[] m_DiveSites;

        public void OnSceneLoad(SceneBinding inScene, object inContext)
        {
            StringHash32 entrance = Services.State.LastEntranceId;

            var job = Save.Jobs.CurrentJob.Job;
            var mapData = Save.Map;

            foreach(var site in m_DiveSites)
            {
                site.Initialize(mapData, job);
            }

            StringHash32 currentMap = MapDB.LookupCurrentMap();
            mapData.SetCurrentStationId(currentMap);
        }

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags)
        {
            List<DiveSite> diveSites = new List<DiveSite>(8);
            SceneHelper.ActiveScene().Scene.GetAllComponents<DiveSite>(true, diveSites);
            m_DiveSites = diveSites.ToArray();
            return true;
        }

        #endif // UNITY_EDITOR
    }
}