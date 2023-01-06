using System;
using Aqua;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class BestiarySelectButton : MonoBehaviour
    {
        public delegate void ToggleDelegate(BestiaryDesc inCritter, BestiarySelectButton inButton, bool inbOn);

        public ColorGroup Color;
        public Toggle Toggle;
        public Image Icon;
        public LocText Label;
        public CursorInteractionHint Tooltip;
        public GameObject Highlight;
        public RectTransform Marker;
        public AppearAnim Anim;
        public FlashAnim Flash;

        [NonSerialized] public BestiaryDesc Critter;
        [NonSerialized] public ToggleDelegate OnToggle;

        private void Awake()
        {
            Toggle.onValueChanged.AddListener((b) => OnToggle?.Invoke(Critter, this, b));
        }
    }
}