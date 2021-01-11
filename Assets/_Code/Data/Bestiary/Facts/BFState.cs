using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public abstract class BFState : BFBase
    {
        private enum ActorStateProxy
        {
            Stressed = ActorStateId.Stressed,
            Dead = ActorStateId.Dead
        }

        #region Inspector

        [Header("State Change")]
        [SerializeField] private ActorStateProxy m_State = ActorStateProxy.Dead;

        #endregion // Inspector

        public ActorStateId TargetState() { return (ActorStateId) m_State; }

        public bool ShouldCheck(ActorStateId inCurrentState)
        {
            return inCurrentState < TargetState();
        }
    }
}