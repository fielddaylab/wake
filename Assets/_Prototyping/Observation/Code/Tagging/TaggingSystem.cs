using System;
using System.Collections;
using Aqua;
using Aqua.Debugging;
using Aqua.Profile;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using Leaf.Runtime;
using ScriptableBake;
using UnityEngine;

namespace ProtoAqua.Observation {
    [DefaultExecutionOrder(-100)]
    public class TaggingSystem : SharedManager, IScenePreloader, IBaked {
        static private TaggingSystem s_Instance;

        [Serializable]
        private struct CritterProportion {
            [FilterBestiaryId(BestiaryDescCategory.Critter)] public SerializedHash32 CritterId;
            public Fraction16 Proportion;
        }

        #region Inspector

        [SerializeField, PrefabModeOnly] private VFX.Pool m_EffectPool = null;
        [Space]
        [SerializeField] private Fraction16 m_DefaultTagProportion = new Fraction16(0.8f);
        [SerializeField] private CritterProportion[] m_CritterProportionOverrides = null;

        [SerializeField, HideInInspector] private TaggingManifest[] m_SceneManifest;
        [SerializeField, HideInInspector] private BestiaryDesc m_EnvironmentType;
        [SerializeField, HideInInspector] private StringHash32 m_MapId;

        #endregion // Inspector

        [NonSerialized] private ushort[] m_TagCounts;
        private readonly RingBuffer<TaggableCritter> m_RemainingCritters = new RingBuffer<TaggableCritter>(64, RingBufferMode.Expand);

        private SiteSurveyData m_SiteData;
        private BestiaryData m_BestiaryData;

        [NonSerialized] private Collider2D m_Range;
        [NonSerialized] private TriggerListener2D m_Listener;
        [NonSerialized] private Vector2? m_ClosestInRange;

        #region Events

        protected override void Awake() {
            base.Awake();
            Assert.True(s_Instance == null);
            s_Instance = this;

            Services.Events.Register<BestiaryUpdateParams>(GameEvents.BestiaryUpdated, OnBestiaryUpdate, this);
        }

        protected override void OnDestroy() {
            s_Instance = null;
            Services.Events?.Deregister<BestiaryUpdateParams>(GameEvents.BestiaryUpdated, OnBestiaryUpdate);

            base.OnDestroy();
        }

        private void LateUpdate() {
            if (Script.IsPausedOrLoading)
                return;

            m_Listener.ProcessOccupants();

            Vector3 gameplayPlanePos;
            Vector2 gameplayPlaneDist;
            Vector2 listenerPos = m_Range.transform.position;
            float dist;
            float closestDist = float.MaxValue;
            Vector3? closestGameplayPlanePos = null;
            TaggableCritter critter;

            for (int i = m_RemainingCritters.Count - 1; i >= 0; i--) {
                critter = m_RemainingCritters[i];
                if (critter.WasTagged || !critter.ColliderPosition.enabled) {
                    continue;
                }

                gameplayPlanePos = critter.ColliderPosition.LastKnownPosition;
                gameplayPlaneDist = (Vector2)gameplayPlanePos - listenerPos;
                dist = gameplayPlaneDist.magnitude;

                if (dist < closestDist) {
                    closestGameplayPlanePos = gameplayPlanePos;
                    closestDist = dist;
                }
            }

            m_ClosestInRange = closestGameplayPlanePos;
        }

        IEnumerator IScenePreloader.OnPreloadScene(SceneBinding inScene, object inContext) {
            m_SiteData = Save.Science.GetSiteData(m_MapId);
            m_BestiaryData = Save.Bestiary;

            m_TagCounts = new ushort[m_SceneManifest.Length];

            TaggableCritter critter;
            for (int i = m_RemainingCritters.Count - 1; i >= 0; i--) {
                critter = m_RemainingCritters[i];
                if (IsFinished(critter.CritterId)) {
                    critter.Collider.enabled = false;
                    critter.ColliderPosition.enabled = false;
                    critter.WasTagged = true;
                    m_RemainingCritters.FastRemoveAt(i);
                } else if (IsReady(critter.CritterId)) {
                    critter.Collider.enabled = true;
                    critter.ColliderPosition.enabled = true;
                }
            }

            Services.UI.FindPanel<TaggingUI>().Populate(m_SceneManifest, m_TagCounts);
            return null;
        }

        private void OnBestiaryUpdate(BestiaryUpdateParams updateParams) {
            if (updateParams.Type != BestiaryUpdateParams.UpdateType.Entity) {
                return;
            }

            MarkAllAsAvailable(updateParams.Id);
        }

        #endregion // Events

        #region Taggable Critters

        public void Register(TaggableCritter inCritter) {
            if (m_SiteData == null) {
                m_RemainingCritters.PushBack(inCritter);
                inCritter.Collider.enabled = false;
                inCritter.ColliderPosition.enabled = false;
                return;
            }

            if (inCritter.WasTagged) {
                inCritter.ColliderPosition.enabled = false;
                return;
            }

            if (IsFinished(inCritter.CritterId)) {
                inCritter.WasTagged = true;
                inCritter.ColliderPosition.enabled = false;
                return;
            }

            if (IsReady(inCritter.CritterId)) {
                inCritter.ColliderPosition.enabled = true;
                m_RemainingCritters.PushBack(inCritter);
                return;
            }

            inCritter.Collider.enabled = false;
            m_RemainingCritters.PushBack(inCritter);
        }

        public void Deregister(TaggableCritter inCritter) {
            inCritter.ColliderPosition.enabled = false;
            m_RemainingCritters.FastRemove(inCritter);
        }

        private void MarkAllAsAvailable(StringHash32 inCritterId) {
            TaggableCritter critter;
            for (int i = m_RemainingCritters.Count - 1; i >= 0; i--) {
                critter = m_RemainingCritters[i];
                if (critter.CritterId == inCritterId && !critter.WasTagged) {
                    critter.ColliderPosition.enabled = true;
                }
            }
        }

        private void MarkAllAsTagged(StringHash32 inCritterId) {
            TaggableCritter critter;
            for (int i = m_RemainingCritters.Count - 1; i >= 0; i--) {
                critter = m_RemainingCritters[i];
                if (critter.CritterId == inCritterId) {
                    critter.WasTagged = true;
                    critter.ColliderPosition.enabled = false;
                    m_RemainingCritters.FastRemoveAt(i);
                }
            }
        }

        public bool TryGetClosestCritterGameplayPlane(out Vector2 outPosition) {
            if (m_RemainingCritters.Count <= 0 || m_ClosestInRange == null) {
                outPosition = default;
                return false;
            }

            outPosition = m_ClosestInRange.Value;
            return true;
        }

        #endregion // Taggable Critters

        #region State

        // Returns if the player is ready to start tagging this critter
        private bool IsReady(StringHash32 id) {
            return m_BestiaryData.HasEntity(id);
        }

        // Returns if the player has started tagging this critter
        private bool IsStarted(StringHash32 inId) {
            for (int i = 0, length = m_SceneManifest.Length; i < length; i++) {
                if (m_SceneManifest[i].Id == inId)
                    return m_TagCounts[i] > 0;
            }

            return false;
        }

        // Returns if the player has finished tagging this critter
        private bool IsFinished(StringHash32 inId) {
            return m_SiteData.TaggedCritters.Contains(inId);
        }

        // Returns the index of the critter, for both manifest and count
        private int IndexOf(StringHash32 inId) {
            for (int i = 0, length = m_SceneManifest.Length; i < length; i++) {
                if (m_SceneManifest[i].Id == inId)
                    return i;
            }

            return -1;
        }

        #endregion // State

        #region Tagging

        private bool AttemptTag(TaggableCritter inCritter) {
            if (IsFinished(inCritter.CritterId) || inCritter.WasTagged) {
                return false;
            }

            inCritter.WasTagged = true;
            inCritter.ColliderPosition.enabled = false;
            m_RemainingCritters.FastRemove(inCritter);

            VFX effect = m_EffectPool.Alloc(inCritter.TrackTransform.position, Quaternion.identity, false);
            effect.Sprite.SetAlpha(1);
            effect.Transform.SetScale(0, Axis.XY);
            effect.Animation = Routine.Start(effect, PlayEffect(effect));

            Services.Audio.PostEvent("dive.critterTagged");

            int idx = IndexOf(inCritter.CritterId);
            TaggingManifest manifest = m_SceneManifest[idx];
            m_TagCounts[idx]++;

            DebugService.Log(LogMask.Observation, "[TaggingSystem] Tagged '{0}' {1}/{2}/{3}", manifest.Id, m_TagCounts[idx], manifest.Required, manifest.TotalInScene);

            if (m_TagCounts[idx] < manifest.Required) {
                Services.UI.FindPanel<TaggingUI>().Populate(m_SceneManifest, m_TagCounts);
                return true;
            }

            m_SiteData.TaggedCritters.Add(manifest.Id);
            m_SiteData.OnChanged();

            Services.Events.Queue(GameEvents.SiteDataUpdated, m_SiteData.MapId);
            MarkAllAsTagged(manifest.Id);

            Services.UI.FindPanel<TaggingUI>().Populate(m_SceneManifest, m_TagCounts);

            BFPopulation population = BestiaryUtils.FindPopulationRule(m_EnvironmentType, manifest.Id);
            if (population != null) {
                m_BestiaryData.RegisterFact(population.Id);
                    Services.Script.QueueInvoke(() => {
                    Services.UI.Popup.PresentFact(
                        Loc.Find("ui.popup.newPopulationFact.header"),
                        null,
                        null,
                        Assets.Fact(population.Id),
                        Save.Bestiary.GetDiscoveredFlags(population.Id)
                    );
                }, -5);
            } else {
                Services.Script.QueueInvoke(() => {
                    Services.UI.Popup.DisplayWithClose(
                        "ERROR",
                        Loc.FormatFromString("Site '{0}' has no population data for critter id '{1}'", m_EnvironmentType.CommonName(), Assets.Bestiary(manifest.Id).CommonName())
                    );
                }, -5);
            }

            return true;
        }

        static private IEnumerator PlayEffect(VFX inEffect) {
            yield return Routine.Combine(
                inEffect.Transform.ScaleTo(2, 0.5f, Axis.XY).Ease(Curve.CubeOut),
                inEffect.Sprite.FadeTo(0, 0.25f).DelayBy(0.25f)
            );
            inEffect.Free();
        }

        #endregion // Tagging

        #region Range

        public void SetDetector(Collider2D inCollider) {
            if (m_Range == inCollider)
                return;

            if (m_Listener != null) {
                m_Listener.onTriggerEnter.RemoveListener(OnTaggableEnterRegion);
            }

            m_Range = inCollider;

            if (m_Range != null) {
                m_Listener = inCollider.EnsureComponent<TriggerListener2D>();
                m_Listener.LayerFilter = GameLayers.CritterTag_Mask;
                m_Listener.SetOccupantTracking(true);

                m_Listener.onTriggerEnter.AddListener(OnTaggableEnterRegion);
            } else {
                m_Listener = null;
            }
        }

        #endregion // Range

        #region Callbacks

        private void OnTaggableEnterRegion(Collider2D inCollider) {
            TaggableCritter critter = inCollider.GetComponentInParent<TaggableCritter>();
            if (critter != null && !critter.WasTagged) {
                AttemptTag(critter);
            }
        }

        #endregion // Callbacks

        #region Leaf

        [LeafMember("TaggingHasStarted"), UnityEngine.Scripting.Preserve]
        static private bool LeafHasStartedTagging(StringHash32 inCritterId) {
            Assert.NotNull(s_Instance, "Cannot call tagging functions if not in dive scene");
            return s_Instance.IsStarted(inCritterId);
        }

        [LeafMember("TaggingHasFinished"), UnityEngine.Scripting.Preserve]
        static private bool LeafHasFinishedTagging(StringHash32 inCritterId) {
            Assert.NotNull(s_Instance, "Cannot call tagging functions if not in dive scene");
            return !s_Instance.IsFinished(inCritterId);
        }

        #endregion // Leaf

        #region IBaked

        #if UNITY_EDITOR

        static private ref TaggingManifest FindManifest(RingBuffer<TaggingManifest> manifest, StringHash32 id) {
            for (int i = 0; i < manifest.Count; i++) {
                if (manifest[i].Id == id) {
                    return ref manifest[i];
                }
            }

            manifest.PushBack(new TaggingManifest() { Id = id });
            return ref manifest[manifest.Count - 1];
        }

        static private Fraction16 FindProportion(StringHash32 id, Fraction16 defaultProportion, CritterProportion[] overrides) {
            for(int i = 0; i < overrides.Length; i++) {
                if (overrides[i].CritterId == id) {
                    return overrides[i].Proportion;
                }
            }

            return defaultProportion;
        }

        bool IBaked.Bake(ScriptableBake.BakeFlags flags) {
            StringHash32 mapId = MapDB.LookupCurrentMap();
            Assert.False(mapId.IsEmpty, "Tagging enabled in scene {0} which has no corresponding map", SceneHelper.ActiveScene().Name);

            m_MapId = mapId;
            m_EnvironmentType = Assets.Bestiary(Assets.Map(mapId).EnvironmentId());

            RingBuffer<TaggingManifest> entries = new RingBuffer<TaggingManifest>();
            SceneHelper.ActiveScene().Scene.ForEachComponent<TaggableCritter>(true, (scn, critter) => {
                FindManifest(entries, critter.CritterId).TotalInScene++;
            });
            for(int i = 0; i < entries.Count; i++) {
                ref TaggingManifest manifest = ref entries[i];
                manifest.Required = (ushort) (manifest.TotalInScene * FindProportion(manifest.Id, m_DefaultTagProportion, m_CritterProportionOverrides));
            }
            m_SceneManifest = entries.ToArray();
            return true;
        }

        int IBaked.Order {
            get { return 0; }
        }

        #endif // UNITY_EDITOR

        #endregion // IBaked
    }
}