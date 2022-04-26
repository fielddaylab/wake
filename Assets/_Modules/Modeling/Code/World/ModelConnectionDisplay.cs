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
        [NonSerialized] public int Key;
        [NonSerialized] public ushort IndexA;
        [NonSerialized] public ushort IndexB;
        [NonSerialized] public int Order;
        [NonSerialized] public int OverlapIndex;

        void IPoolAllocHandler.OnAlloc() {
        }

        void IPoolAllocHandler.OnFree() {
            Fact = null;
        }
    }
}