using System;
using Aqua;
using Aqua.Cameras;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class BestiaryButton : MonoBehaviour
    {
        public delegate void ToggleDelegate(BestiaryDesc inCritter, bool inbOn);

        public Toggle Toggle;
        public Image Icon;
        public LocText Label;
        public CursorInteractionHint Tooltip;

        [NonSerialized] public BestiaryDesc Critter;
        [NonSerialized] public ToggleDelegate OnToggle;

        private void Awake()
        {
            Toggle.onValueChanged.AddListener((b) => OnToggle?.Invoke(Critter, b));
        }
    }
}