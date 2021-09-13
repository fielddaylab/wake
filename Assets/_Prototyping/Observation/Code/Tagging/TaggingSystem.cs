using System;
using UnityEngine;
using BeauRoutine;
using System.Collections;
using UnityEngine.UI;
using Aqua;
using BeauUtil;
using BeauPools;
using System.Collections.Generic;
using Aqua.Debugging;
using Aqua.Cameras;
using Aqua.Profile;
using BeauUtil.Debugger;

namespace ProtoAqua.Observation
{
    [DefaultExecutionOrder(-100)]
    public class TaggingSystem : SharedManager, IScenePreloader
    {
        #region Inspector

        [SerializeField] private VFX.Pool m_EffectPool = null;

        #endregion // Inspector

        private readonly RingBuffer<TaggingProgress> m_CritterTypes = new RingBuffer<TaggingProgress>();
        private readonly RingBuffer<TaggableCritter> m_RemainingCritters = new RingBuffer<TaggableCritter>(64, RingBufferMode.Expand);
        private readonly RingBuffer<TaggableCritter> m_UntaggedCrittersInRange = new RingBuffer<TaggableCritter>(16, RingBufferMode.Expand);
        private readonly HashSet<TaggableCritter> m_TaggedCritterObjects = new HashSet<TaggableCritter>();

        private SiteSurveyData m_SiteData;
        private BestiaryData m_BestiaryData;
        [NonSerialized] private BestiaryDesc m_EnvironmentType;

        [NonSerialized] private Collider2D m_Range;
        [NonSerialized] private TriggerListener2D m_Listener;
        [NonSerialized] private float m_DeactivateRangeSq;
        [NonSerialized] private bool m_ActiveState = false;
        private RingBuffer<StringHash32> m_FactDisplayQueue = new RingBuffer<StringHash32>(16, RingBufferMode.Expand);
        private Routine m_QueueProcessor;

        #region Events

        protected override void Awake()
        {
            base.Awake();

            Services.Events.Register<BestiaryUpdateParams>(GameEvents.BestiaryUpdated, OnBestiaryUpdated);
        }

        protected override void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);

            base.OnDestroy();
        }

        private void LateUpdate()
        {
            if (Services.Pause.IsPaused() || Services.State.IsLoadingScene())
                return;

            if (m_Listener == null || !m_Listener.isActiveAndEnabled)
            {
                if (m_ActiveState)
                {
                    m_ActiveState = false;
                    DeactivateAllColliders();
                }
                return;
            }
            
            m_ActiveState = true;
            m_Listener.ProcessOccupants();

            CameraService.PlanePositionHelper positionHelper = Services.Camera.GetPositionHelper();
            TaggableCritter critter;
            Vector3 gameplayPlanePos;
            Vector3 gameplayPlaneDist;
            Vector2 listenerPos = m_Range.transform.position;
            for(int i = m_RemainingCritters.Count - 1; i >= 0; i--)
            {
                critter = m_RemainingCritters[i];
                gameplayPlanePos = positionHelper.CastToPlane(critter.transform);
                critter.Collider.transform.position = gameplayPlanePos;

                gameplayPlaneDist = (Vector2) gameplayPlanePos - listenerPos;
                critter.Collider.enabled = gameplayPlaneDist.sqrMagnitude < m_DeactivateRangeSq;
            }
        }

        IEnumerator IScenePreloader.OnPreloadScene(SceneBinding inScene, object inContext)
        {
            StringHash32 mapId = MapDB.LookupCurrentMap();
            Assert.False(mapId.IsEmpty, "Tagging enabled in scene {0} which has no corresponding map", inScene.Name);
            
            m_SiteData = Services.Data.Profile.Science.GetSiteData(mapId);
            m_EnvironmentType = Assets.Map(mapId).Environment();
            m_BestiaryData = Services.Data.Profile.Bestiary;
            
            TaggableCritter critter;
            for(int i = m_RemainingCritters.Count - 1; i >= 0; i--)
            {
                critter = m_RemainingCritters[i];
                if (!TrackCritterType(critter.CritterId))
                {
                    critter.Collider.enabled = false;
                    m_RemainingCritters.FastRemoveAt(i);
                }
            }

            Services.UI.FindPanel<TaggingUI>().Populate(m_CritterTypes);
            return null;
        }

        #endregion // Events

        #region Taggable Critters

        public void Register(TaggableCritter inCritter)
        {
            if (m_SiteData != null)
            {
                if (TrackCritterType(inCritter.CritterId))
                {
                    if (!WasTagged(inCritter))
                        m_RemainingCritters.PushBack(inCritter);
                }
            }
            else
            {
                m_RemainingCritters.PushBack(inCritter);
                inCritter.Collider.enabled = false;
            }
        }

        public void Deregister(TaggableCritter inCritter)
        {
            m_RemainingCritters.FastRemove(inCritter);
            m_UntaggedCrittersInRange.FastRemove(inCritter);

            if (m_SiteData != null)
            {
                UntrackCritterType(inCritter.CritterId);
            }
        }

        private bool TrackCritterType(StringHash32 inCritterId)
        {
            if (!IsUnfinished(inCritterId))
                return false;

            Category(inCritterId).TotalInScene++;
            return true;
        }

        private void UntrackCritterType(StringHash32 inCritterId)
        {
            if (!IsUnfinished(inCritterId))
                return;

            int idx = IndexOfCategory(inCritterId);
            if (idx >= 0)
            {
                m_CritterTypes[idx].TotalInScene--;
            }
        }

        private int IndexOfCategory(StringHash32 inId)
        {
            for(int i = 0, length = m_CritterTypes.Count; i < length; i++)
            {
                ref TaggingProgress category = ref m_CritterTypes[i];
                if (category.Id == inId)
                    return i;
            }

            return -1;
        }

        private ref TaggingProgress Category(StringHash32 inId)
        {
            for(int i = 0, length = m_CritterTypes.Count; i < length; i++)
            {
                ref TaggingProgress category = ref m_CritterTypes[i];
                if (category.Id == inId)
                    return ref category;
            }

            TaggingProgress newCategory;
            newCategory.Id = inId;
            newCategory.Tagged = 0;
            newCategory.TotalInScene = 0;
            m_CritterTypes.PushBack(newCategory);
            return ref m_CritterTypes[m_CritterTypes.Count - 1];
        }

        private bool IsUnfinished(StringHash32 inId)
        {
            return !m_SiteData.TaggedCritters.Contains(inId);
        }

        private bool WasTagged(TaggableCritter inCritter)
        {
            return m_TaggedCritterObjects.Contains(inCritter) || !IsUnfinished(inCritter.CritterId);
        }

        public bool AttemptTag(TaggableCritter inCritter)
        {
            if (IsUnfinished(inCritter.CritterId) && m_BestiaryData.HasEntity(inCritter.CritterId) && m_TaggedCritterObjects.Add(inCritter))
            {
                inCritter.Collider.enabled = false;
                m_RemainingCritters.FastRemove(inCritter);

                VFX effect = m_EffectPool.Alloc(inCritter.transform.position, Quaternion.identity, false);
                effect.Sprite.SetAlpha(1);
                effect.Transform.SetScale(0, Axis.XY);
                effect.Animation = Routine.Start(effect, PlayEffect(effect));

                Services.Audio.PostEvent("dive.critterTagged");

                int idx = IndexOfCategory(inCritter.CritterId);
                ref var category = ref m_CritterTypes[idx];
                category.Tagged++;
                
                Log.Msg("[TaggingSystem] Tagged '{0}' {1}/{2}", category.Id, category.Tagged, category.TotalInScene);

                if (category.Tagged >= category.TotalInScene)
                {
                    BFPopulation population = BestiaryUtils.FindPopulationRule(m_EnvironmentType, category.Id, m_SiteData.SiteVersion);
                    Assert.NotNull(population, "No Population Fact for '{0}' found for environment '{1}'", category.Id, m_EnvironmentType.Id());
                    m_BestiaryData.RegisterFact(population.Id);
                    m_SiteData.TaggedCritters.Add(inCritter.CritterId);
                    m_CritterTypes.FastRemoveAt(idx);
                    m_SiteData.OnChanged();

                    m_FactDisplayQueue.PushBack(population.Id);
                    if (!m_QueueProcessor)
                        m_QueueProcessor = Routine.Start(this, DisplayModelQueue());
                }

                Services.UI.FindPanel<TaggingUI>().Populate(m_CritterTypes);
                return true;
            }

            return false;
        }

        private IEnumerator DisplayModelQueue()
        {
            StringHash32 factId;
            while(m_FactDisplayQueue.Count > 0)
            {
                factId = m_FactDisplayQueue.PopFront();
                yield return Services.UI.Popup.PresentFact(Loc.Find("ui.popup.newPopulationFact.header"), null, Assets.Fact(factId), Services.Data.Profile.Bestiary.GetDiscoveredFlags(factId)).Wait();
            }
        }

        static private IEnumerator PlayEffect(VFX inEffect)
        {
            yield return Routine.Combine(
                inEffect.Transform.ScaleTo(2, 0.5f, Axis.XY).Ease(Curve.CubeOut),
                inEffect.Sprite.FadeTo(0, 0.25f).DelayBy(0.25f)
            );
            inEffect.Free();
        }

        private void DeactivateAllColliders()
        {
            TaggableCritter critter;
            for(int i = 0, len = m_RemainingCritters.Count; i < len; i++)
            {
                critter = m_RemainingCritters[i];
                critter.Collider.enabled = false;
            }
        }

        #endregion // Taggable Critters
    
        #region Scan Range

        public void SetDetector(Collider2D inCollider)
        {
            if (m_Range == inCollider)
                return;

            if (m_Listener != null)
            {
                m_Listener.onTriggerEnter.RemoveListener(OnTaggableEnterRegion);
                m_Listener.onTriggerExit.RemoveListener(OnTaggableExitRegion);
            }

            m_Range = inCollider;

            if (m_Range != null)
            {
                m_Listener = inCollider.EnsureComponent<TriggerListener2D>();
                m_Listener.LayerFilter = GameLayers.CritterTag_Mask;
                m_Listener.SetOccupantTracking(true);

                m_Listener.onTriggerEnter.AddListener(OnTaggableEnterRegion);
                m_Listener.onTriggerExit.AddListener(OnTaggableExitRegion);

                m_DeactivateRangeSq = PhysicsUtils.GetRadius(inCollider) + 1;
                m_DeactivateRangeSq *= m_DeactivateRangeSq;
            }
            else
            {
                m_Listener = null;
            }
        }

        #endregion // Scan Range

        #region Callbacks

        private void OnBestiaryUpdated(BestiaryUpdateParams inParams)
        {
            if (inParams.Type == BestiaryUpdateParams.UpdateType.Entity)
            {
                TaggableCritter critter;
                for(int i = m_UntaggedCrittersInRange.Count - 1; i >= 0; i--)
                {
                    critter = m_UntaggedCrittersInRange[i];
                    if (critter.CritterId == inParams.Id)
                        AttemptTag(critter);
                }
            }
        }

        private void OnTaggableEnterRegion(Collider2D inCollider)
        {
            TaggableCritter critter = inCollider.GetComponentInParent<TaggableCritter>();
            if (critter != null && !WasTagged(critter))
            {
                if (!AttemptTag(critter))
                {
                    m_UntaggedCrittersInRange.PushBack(critter);
                }
            }
        }

        private void OnTaggableExitRegion(Collider2D inCollider)
        {
            if (!inCollider)
                return;

            TaggableCritter critter = inCollider.GetComponentInParent<TaggableCritter>();
            if (critter != null)
            {
                m_UntaggedCrittersInRange.FastRemove(critter);
            }
        }

        #endregion // Callbacks
    }
}