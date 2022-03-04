using System;
using Aqua;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2 {
    public sealed class ExperimentScreen : MonoBehaviour {
        public CanvasGroup Group;
        
        public Button NextButton;
        public LocText NextLabel;
        
        public Button BackButton;
        public LocText BackLabel;

        public Action OnOpen;
        public Action OnClose;
        public Action OnReset;
    }
}