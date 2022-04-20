using System;
using BeauPools;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;

namespace Aqua.Modeling {
    public class ModelConnectionDisplay : MonoBehaviour, IPoolAllocHandler {

        #region Inspector

        public RectTransform Transform;
        public RawImage Texture;
        public ScrollTiledRawImage Scroll;
        public Graphic Arrow;
        public GameObject Fader;

        #endregion // Inspector

        [NonSerialized] public BFBase Fact;
        [NonSerialized] public int IndexA;
        [NonSerialized] public int IndexB;
        [NonSerialized] public int Order;

        void IPoolAllocHandler.OnAlloc() {
        }

        void IPoolAllocHandler.OnFree() {
            Fact = null;
        }
    }
}