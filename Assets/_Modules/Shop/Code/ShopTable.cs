using UnityEngine;
using BeauUtil;
using Aqua.Cameras;
using BeauUtil.UI;

namespace Aqua.Shop {
    public class ShopTable : MonoBehaviour {
        public SerializedHash32 Id;
        public CameraPose CameraPose;
        public PointerListener Clickable;
        public Transform Stool;
        public bool StoolFaceLeft;
    }
}