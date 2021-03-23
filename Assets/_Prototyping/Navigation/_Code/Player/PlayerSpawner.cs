using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using Aqua;
using BeauUtil;
using BeauUtil.Variants;

namespace ProtoAqua.Navigation
{
    public class PlayerSpawner : MonoBehaviour, ISceneLoadHandler
    {
        [SerializeField] private PlayerController m_Player = null;

        public void OnSceneLoad(SceneBinding inScene, object inContext)
        {
            List<ResearchSite> allSites = new List<ResearchSite>(4);
            inScene.Scene.GetAllComponents(true, allSites);

            StringHash32 diveSite = Services.Data.PopVariable(GameVars.DiveSite).AsStringHash();

            var job = Services.Data.Profile.Jobs.CurrentJob;

            foreach(var site in allSites)
            {
                site.CheckAllowed();
            }

            if (!diveSite.IsEmpty)
            {
                foreach(var site in allSites)
                {
                    if (site.SiteId == diveSite)
                    {
                        m_Player.Teleport(site.PlayerSpawnLocation.position);
                        break;
                    }
                }
            }
        }
    }
}

