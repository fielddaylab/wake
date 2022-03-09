using System;
using Aqua;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using System.Collections;
using BeauRoutine;

namespace ProtoAqua.ExperimentV2 {
    public sealed class ExperimentHeaderUI : MonoBehaviour {
        [Required] public CanvasGroup Group;
        
        [Required] public Button NextButton;
        [Required] public LocText NextLabel;
        [Required] public GameObject NextArrow;
        
        [Required] public Button BackButton;
        [Required] public LocText BackLabel;

        public Routine Transition;
    }
}