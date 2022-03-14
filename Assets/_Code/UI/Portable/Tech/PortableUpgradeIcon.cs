using UnityEngine;
using BeauPools;
using Aqua.Profile;
using UnityEngine.UI;
using BeauUtil;
using BeauRoutine;
using System.Collections;
using System;

namespace Aqua
{
    public class PortableUpgradeIcon : MonoBehaviour
    {
        public Image Icon;
        public LocText Text;
        public CursorInteractionHint Cursor;
        public Button Button;

        [NonSerialized] public InvItem Item;
    }
}