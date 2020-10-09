using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using BeauUtil.Variants;
using BeauRoutine.Extensions;

namespace ProtoAqua.Experiment
{
    public class UrchinActor : MonoBehaviour, IPoolConstructHandler
    {
        #region Inspector
        
        #endregion // Inspector

        [NonSerialized] private ActorCtrl m_Actor;

        private void OnCreate()
        {
        }

        private void OnThink()
        {
            if (RNG.Instance.Chance(0.3f))
            {
                // m_Actor.Nav.MoveTo
            }
        }

        void IPoolConstructHandler.OnConstruct()
        {
            m_Actor = GetComponent<ActorCtrl>();
            m_Actor.Callbacks.OnCreate = OnCreate;
            m_Actor.Callbacks.OnThink = OnThink;
        }

        void IPoolConstructHandler.OnDestruct()
        {
        }
    }
}