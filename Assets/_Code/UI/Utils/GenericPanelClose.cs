using System;
using System.Collections;
using Aqua.Scripting;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua {
    [RequireComponent(typeof(Button))]
    public class GenericPanelClose : MonoBehaviour {
        [Required] public BasePanel Panel;

        private void Awake() {
            GetComponent<Button>().onClick.AddListener(() => Panel.Hide());
        }
    }
}