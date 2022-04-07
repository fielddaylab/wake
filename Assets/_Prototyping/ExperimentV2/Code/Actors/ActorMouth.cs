using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Aqua;
using Aqua.Animation;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace ProtoAqua.ExperimentV2 {
    public sealed class ActorMouth : MonoBehaviour, IPoolAllocHandler {
        #region Inspector

        [Required] public Collider2D Region;
        public ParticleSystemForceField Vacuum;

        #endregion // Inspector

        #region IPoolAllocHandler

        void IPoolAllocHandler.OnAlloc() {
        }

        void IPoolAllocHandler.OnFree() {
        }

        #endregion // IPoolAllocHandler
    }
}