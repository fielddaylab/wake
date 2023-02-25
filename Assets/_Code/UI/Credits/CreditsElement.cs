using System;
using System.Collections;
using Aqua.Character;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using BeauPools;

namespace Aqua {
    public class CreditsElement : MonoBehaviour, IPooledObject<CreditsElement> {
        [Serializable] public class Pool : SerializablePool<CreditsElement> { }

        public RectTransform Transform;
        public LocText[] Text;

        private IPool<CreditsElement> m_Pool;

        public void Hide() {
            if (m_Pool != null) {
                m_Pool.Free(this);
            } else {
                gameObject.SetActive(false);
            }
        }

        #region IPooledObject

        void IPooledObject<CreditsElement>.OnConstruct(IPool<CreditsElement> inPool) {
            m_Pool = inPool;
        }

        void IPooledObject<CreditsElement>.OnDestruct() {
        }

        void IPooledObject<CreditsElement>.OnAlloc() {
        }

        void IPooledObject<CreditsElement>.OnFree() {
        }

        #endregion // IPooledObject
    }
}