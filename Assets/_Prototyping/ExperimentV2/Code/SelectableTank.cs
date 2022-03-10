using System;
using Aqua;
using Aqua.Cameras;
using AquaAudio;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using ScriptableBake;
using UnityEngine;

namespace ProtoAqua.ExperimentV2
{
    public class SelectableTank : MonoBehaviour, IBaked
    {
        #region Inspector

        public TankType Type = TankType.Observation;
        [Required] public CameraPose CameraPose = null;
        [Required] public BoxCollider BoundsCollider;
        [HideInInspector] public Bounds Bounds;
        [Required] public MonoBehaviour Controller;
        
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
        
        [Header("Emojis")]
        [SerializeField, Required] public ParticleSystem[] EmojiEmitters = Array.Empty<ParticleSystem>();
        [SerializeField, HideInInspector] public StringHash32[] EmojiIds = Array.Empty<StringHash32>();

        [Header("Actors")]
        [SerializeField] public ActorBehaviorSystem ActorBehavior = null;

        [HideInInspector] public ExperimentScreen[] AllScreens;

        #endregion // Inspector

        [NonSerialized] private StringHash32 m_Id;
        [NonSerialized] public TankState CurrentState;
        
        [NonSerialized] public Color DefaultWaterColor;
        [NonSerialized] public TankWaterSystem WaterSystem;
        [NonSerialized] public float WaterFillProportion;
        [NonSerialized] public AudioHandle WaterAudioLoop;

        [NonSerialized] public ExperimentScreen CurrentScreen;
        [NonSerialized] public Routine ScreenTransition;
        [NonSerialized] public Routine WaterTransition;

        public StringHash32 Id { get { return m_Id.IsEmpty ? (m_Id = name) : m_Id; } }

        public Action ActivateMethod;
        public Func<bool> CanDeactivate;
        public Action DeactivateMethod;
        public Func<StringHash32, bool> HasCritter;
        public Func<StringHash32, bool> HasEnvironment;

        static public void Reset(SelectableTank tank, bool full = false) {
            foreach(var screen in tank.AllScreens) {
                ExperimentScreen.Reset(screen);
            }
            tank.ScreenTransition.Stop();
            if (full) {
                tank.ActorBehavior.ClearAll();
            } else {
                tank.ActorBehavior.ClearActors();
            }
            foreach(var emoji in tank.EmojiEmitters) {
                emoji.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            tank.CurrentScreen = null;
        }

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags)
        {
            ActorBehavior = GetComponentInChildren<ActorBehaviorSystem>(false);
            AllScreens = GetComponentsInChildren<ExperimentScreen>(true);

            EmojiIds = new StringHash32[EmojiEmitters.Length];
            for(int i = 0; i < EmojiIds.Length; i++) {
                StringHash32 id = EmojiEmitters[i].name.Replace("Emoji", "").Replace("Emitter", "").Replace("Particles", "");
                EmojiIds[i] = id;
            }
            return true;
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