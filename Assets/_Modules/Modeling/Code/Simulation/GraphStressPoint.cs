using System;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Modeling {
    public class GraphStressPoint : MonoBehaviour {
        [Serializable] public class Pool : SerializablePool<GraphStressPoint> { }
        [SerializeField] public Graphic Icon;
        [NonSerialized] public int Index;
    }
}