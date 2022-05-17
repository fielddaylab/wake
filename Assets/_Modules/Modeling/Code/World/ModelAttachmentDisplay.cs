using System;
using BeauPools;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;

namespace Aqua.Modeling {
    public class ModelAttachmentDisplay : MonoBehaviour, IPoolAllocHandler {

        #region Inspector

        public RectTransform Transform;
        public CanvasGroup CanvasGroup;
        public Graphic Arrow;
        public Image Icon;
        public GameObject Stressed;

        #endregion // Inspector

        [NonSerialized] public BFBase Fact;
        [NonSerialized] public MissingFactTypes Missing;
        [NonSerialized] public int Key;
        [NonSerialized] public ushort Index;
        [NonSerialized] public int AttachmentIndex;
        [NonSerialized] public WorldFilterMask Mask;

        void IPoolAllocHandler.OnAlloc() {
        }

        void IPoolAllocHandler.OnFree() {
            Fact = null;
            Missing = 0;
        }
    }
}