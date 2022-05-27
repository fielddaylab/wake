using System;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Modeling {
    [RequireComponent(typeof(Button))]
    public class GraphDivergencePoint : MonoBehaviour, IPoolAllocHandler {
        [Serializable] public class Pool : SerializablePool<GraphDivergencePoint> { }
        [SerializeField] public RectTransform Arrow;

        [NonSerialized] public SimGraphBlock Parent;
        [NonSerialized] public int Sign;

        public Action<GraphDivergencePoint> OnClick;

        private void Awake() {
            GetComponent<Button>().onClick.AddListener(() => {
                OnClick?.Invoke(this);
            });
        }

        void IPoolAllocHandler.OnAlloc() {
        }

        void IPoolAllocHandler.OnFree() {
            Sign = 0;
            Parent = null;
        }
    }
}