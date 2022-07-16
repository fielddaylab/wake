using System;
using System.Collections.Generic;
using Aqua;
using Aqua.Cameras;
using Aqua.Entity;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace ProtoAqua.Observation {
    public sealed class CritterAI : MonoBehaviour, IActiveEntity {
        public float Radius = 1;
        public CritterAIParams Config;

        [Header("Rendering")]
        public Transform[] Renderers;
        public Vector3 RotationAdjust = new Vector3(0, -90, 0);

        [NonSerialized] public EntityActiveStatus Status;

        #region IActiveEntity

        int IActiveEntity.UpdateMask {
            get { return 1; }
        }

        int IBatchId.BatchId {
            get { return 0; }
        }

        EntityActiveStatus IActiveEntity.ActiveStatus {
            get { return Status; }
        }

        #endregion // IActiveEntity
    }
}