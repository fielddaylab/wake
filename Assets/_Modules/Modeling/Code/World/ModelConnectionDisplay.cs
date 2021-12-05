using System;
using BeauPools;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Modeling {
    public class ModelConnectionDisplay : MonoBehaviour, IPoolAllocHandler {

        #region Inspector

        public RectTransform Transform;
        public RawImage Texture;

        #endregion // Inspector

        [NonSerialized] public BFBase Fact;
        [NonSerialized] public int IndexA;
        [NonSerialized] public int IndexB;

        void IPoolAllocHandler.OnAlloc() {
        }

        void IPoolAllocHandler.OnFree() {
            Fact = null;
        }
    }
}