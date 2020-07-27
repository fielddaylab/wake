using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using ProtoAqua;
using UnityEngine;

namespace ProtoAqua
{
    public abstract class TweakAsset : ScriptableObject, ISerializedVersion, ISerializedObject
    {
        #region Inspector

        [SerializeField] private PropertyBlock m_MiscProperties = null;

        #endregion // Inspector

        internal void OnAdded() { Apply(); }
        internal void OnRemoved() { Remove(); }

        protected virtual void Apply() { }
        protected virtual void Remove() { }

        public IReadOnlyPropertyBlock<PropertyName> MiscProperties { get { return m_MiscProperties; } }

        #region ISerializedObject

        public virtual ushort Version { get { return 1; } }

        public virtual void Serialize(Serializer ioSerializer)
        {
            throw new NotImplementedException();
        }

        #endregion // ISerializedObject
    }
}