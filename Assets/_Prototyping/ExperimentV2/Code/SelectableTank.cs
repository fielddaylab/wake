using System;
using Aqua;
using Aqua.Cameras;
using AquaAudio;
using BeauUtil;
using BeauUtil.UI;
using UnityEngine;

namespace ProtoAqua.ExperimentV2
{
    public class SelectableTank : MonoBehaviour, ISceneOptimizable
    {
        #region Inspector

        public TankType Type = TankType.Observation;
        [Required] public CameraPose CameraPose = null;
        [Required] public BoxCollider BoundsCollider;
        [HideInInspector] public Bounds Bounds;
        
        [Header("Click")]
        [Required(ComponentLookupDirection.Children)] public PointerListener Clickable = null;
        [Required(ComponentLookupDirection.Children)] public CursorInteractionHint InteractionHint = null;

        [Header("Canvas")]
        [Required] public Canvas Interface = null;
        [Required] public InputRaycasterLayer InterfaceRaycaster = null;
        [Required] public CanvasGroup InterfaceFader = null;

        [Header("Water")]
        [Required] public Transform WaterRenderer;
        [Required] public ParticleSystem WaterAmbientParticles;
        [Required] public BoxCollider2D WaterTrigger;
        [Required] public BoxCollider WaterCollider3D;
        [Required] public ColorGroup WaterColor;
        [Required] public ParticleSystem WaterDrainParticles;
        [HideInInspector] public Rect WaterRect;

        [HideInInspector] public ActorAllocator ActorAllocator;

        #endregion // Inspector

        [NonSerialized] private StringHash32 m_Id;
        [NonSerialized] public Color DefaultWaterColor;
        [NonSerialized] public TankState CurrentState;
        [NonSerialized] public TankWaterSystem WaterSystem;
        [NonSerialized] public float WaterFillProportion;
        [NonSerialized] public AudioHandle WaterAudioLoop;

        public StringHash32 Id { get { return m_Id.IsEmpty ? (m_Id = name) : m_Id; } }

        public Action ActivateMethod;
        public Func<bool> CanDeactivate;
        public Action DeactivateMethod;
        public Func<StringHash32, bool> HasCritter;
        public Func<StringHash32, bool> HasEnvironment;

        #if UNITY_EDITOR

        void ISceneOptimizable.Optimize()
        {
            ActorAllocator = FindObjectOfType<ActorAllocator>();
        }

        #endif // UNITY_EDITOR
    }

    [Flags]
    public enum TankState : byte
    {
        Idle = 0x00,
        Selected = 0x01,
        Filling = 0x02,
        Draining = 0x04,
        Running = 0x08,
        Completed = 0x10
    }

    public enum TankType : byte
    {
        Observation = InProgressExperimentData.Type.Observation,
        Stress = InProgressExperimentData.Type.Stress,
        Measurement = InProgressExperimentData.Type.Measurement,

        Unknown = 255
    }
}