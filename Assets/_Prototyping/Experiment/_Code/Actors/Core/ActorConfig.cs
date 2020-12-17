using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using System.Collections.Generic;
using BeauUtil.Variants;

namespace ProtoAqua.Experiment
{
    [CreateAssetMenu(menuName = "Aqualab/Experiment/Actor Config")]
    public class ActorConfig : ScriptableObject
    {
        #region Inspector

        [SerializeField] private PropertyBlock m_CustomProperties = null;

        #endregion // Inspector

        [NonSerialized] private VariantTable m_SharedBlackboard = null;

        #region IReadOnlyPropertyBlock

        public TValue GetProperty<TValue>(PropertyName inKey)
        {
            return m_CustomProperties.Get<TValue>(inKey);
        }

        public TValue GetProperty<TValue>(PropertyName inKey, TValue inDefault)
        {
            return m_CustomProperties.Get(inKey, inDefault);
        }

        public bool HasProperty(PropertyName inKey, bool inbIncludePrototype = true)
        {
            return m_CustomProperties.Has(inKey, inbIncludePrototype);
        }

        #endregion // IReadOnlyPropertyBlock
    
        #region Blackboard

        public void ResetBlackboard()
        {
            m_SharedBlackboard?.Clear();
        }

        public VariantTable SharedBlackboard
        {
            get { return m_SharedBlackboard ?? (m_SharedBlackboard = new VariantTable(name)); }
        }

        #endregion // Blackboard
    }
}