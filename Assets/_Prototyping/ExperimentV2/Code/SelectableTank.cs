using System;
using Aqua;
using Aqua.Cameras;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using UnityEngine;

namespace ProtoAqua.ExperimentV2
{
    public class SelectableTank : MonoBehaviour
    {
        #region Inspector

        public TankType Type = TankType.Observation;
        [Required] public CameraPose CameraPose = null;
        
        [Header("In Use State")]
        [Required] public GameObject InUseRoot = null;
        [Required] public GameObject InProgressRoot = null;
        [Required] public Transform InProgressTimerScaler = null;
        [Required] public GameObject ReadyRoot = null;
        
        [Header("Click")]
        [Required(ComponentLookupDirection.Children)] public PointerListener Clickable = null;
        [Required(ComponentLookupDirection.Children)] public CursorInteractionHint InteractionHint = null;

        [Header("Canvas")]
        [Required] public Canvas Interface = null;
        [Required] public InputRaycasterLayer InterfaceRaycaster = null;
        [Required] public CanvasGroup InterfaceFader = null;

        [Header("Dirty")]
        [Required] public GameObject DirtyRoot = null;

        #endregion // Inspector

        [NonSerialized] private StringHash32 m_Id;

        public StringHash32 Id { get { return m_Id.IsEmpty ? (m_Id = name) : m_Id; } }

        public Action ActivateMethod;
        public Func<bool> CanDeactivate;
        public Action DeactivateMethod;

        [NonSerialized] public TankAvailability CurrentAvailability = TankAvailability.Available;
    }

    public enum TankAvailability
    {
        Available,
        TimedExperiment,
        TimedExperimentCompleted,
        Dirty
    }

    public enum TankType
    {
        Observation = InProgressExperimentData.Type.Observation,
        Stress = InProgressExperimentData.Type.Stress,
        Measurement = InProgressExperimentData.Type.Measurement
    }
}