using System;
using Aqua;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class StressSummary : MonoBehaviour, IBakedComponent
    {
        #region Inspector

        [Required] public Button ContinueButton;

        [Required] public LocText CritterNameText;
        [Required] public Image CritterImage;
        [HideInInspector] public StateFactDisplay[] StateFacts;

        #endregion // Inspector

        #if UNITY_EDITOR

        void IBakedComponent.Bake()
        {
            StateFacts = GetComponentsInChildren<StateFactDisplay>(true);
        }

        #endif // UNITY_EDITOR
    }
}