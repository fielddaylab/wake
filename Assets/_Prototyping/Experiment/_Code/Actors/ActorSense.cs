using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using System.Collections.Generic;

namespace ProtoAqua.Experiment
{
    public class ActorSense : ActorModule
    {
        #region Inspector

        [SerializeField, Required(ComponentLookupDirection.Children)] private TriggerListener2D m_Listener = null;

        #endregion // Inspector

        public TriggerListener2D Listener { get { return m_Listener; } }

        public IReadOnlyList<TriggerListener2D.Occupant> SensedObjects
        {
            get { return m_Listener.Occupants(); }
        }
    }
}