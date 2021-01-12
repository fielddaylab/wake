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
        [SerializeField] private PlayerController m_Player;

        public void OnSceneLoad(SceneBinding inScene, object inContext)
        {
            List<ResearchSite> allSites = new List<ResearchSite>(4);
            inScene.Scene.GetAllComponents(true, allSites);

            StringHash32 diveSite = Services.Data.GetVariable(GameVars.DiveSite).AsStringHash();
            Services.Data.SetVariable(GameVars.DiveSite, null);

            var job = Services.Data.Profile.Jobs.CurrentJob;

            if (!diveSite.IsEmpty)
            {
                foreach(var site in allSites)
                {
                    if (site.SiteId == diveSite)
                    {
                        m_Player.transform.SetPosition(site.transform.position, Axis.XY);
                        break;
                    }
                }
            }
        }
    }
}

