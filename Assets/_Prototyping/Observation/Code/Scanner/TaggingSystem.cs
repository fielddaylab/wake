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

namespace ProtoAqua.Observation
{
    [DefaultExecutionOrder(-100)]
    public class TaggingSystem : SharedManager, IScenePreloader
    {
        #region Types

        private struct CritterCategory
        {
            public StringHash32 Id;
            public ushort Total;
            public ushort Tagged;
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private VFX.Pool m_EffectPool = null;

        #endregion // Inspector

        private readonly RingBuffer<CritterCategory> m_CritterCategories = new RingBuffer<CritterCategory>();
        private readonly RingBuffer<TaggableCritter> m_RemainingCritters = new RingBuffer<TaggableCritter>(64, RingBufferMode.Expand);
        private readonly RingBuffer<TaggableCritter> m_UntaggedCrittersInRange = new RingBuffer<TaggableCritter>(16, RingBufferMode.Expand);
        private readonly HashSet<TaggableCritter> m_TaggedCritterObjects = new HashSet<TaggableCritter>();

        private SiteSurveyData m_SiteData;
        private BestiaryData m_BestiaryData;

        [NonSerialized] private Collider2D m_Range;
        [NonSerialized] private TriggerListener2D m_Listener;

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
                return;
            
            m_Listener.ProcessOccupants();

            CameraService cameraService = Services.Camera;
            TaggableCritter critter;
            for(int i = m_RemainingCritters.Count - 1; i >= 0; i--)
            {
                critter = m_RemainingCritters[i];
                critter.Collider.transform.position = cameraService.GameplayPlanePosition(critter.transform);
            }
        }

        IEnumerator IScenePreloader.OnPreloadScene(SceneBinding inScene, object inContext)
        {
            m_BestiaryData = Services.Data.Profile.Bestiary;
            m_SiteData = Services.Data.Profile.Science.GetSiteData(MapDB.LookupCurrentMap());
            
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
            return null;
        }

        #endregion // Events

        #region Taggable Critters

        public void Register(TaggableCritter inCritter)
        {
            m_RemainingCritters.PushBack(inCritter);
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

            Category(inCritterId).Total++;
            return true;
        }

        private void UntrackCritterType(StringHash32 inCritterId)
        {
            if (!IsUnfinished(inCritterId))
                return;

            int idx = IndexOfCategory(inCritterId);
            if (idx >= 0)
            {
                m_CritterCategories[idx].Total--;
            }
        }

        private int IndexOfCategory(StringHash32 inId)
        {
            for(int i = 0, length = m_CritterCategories.Count; i < length; i++)
            {
                ref CritterCategory category = ref m_CritterCategories[i];
                if (category.Id == inId)
                    return i;
            }

            return -1;
        }

        private ref CritterCategory Category(StringHash32 inId)
        {
            for(int i = 0, length = m_CritterCategories.Count; i < length; i++)
            {
                ref CritterCategory category = ref m_CritterCategories[i];
                if (category.Id == inId)
                    return ref category;
            }

            CritterCategory newCategory;
            newCategory.Id = inId;
            newCategory.Tagged = 0;
            newCategory.Total = 0;
            m_CritterCategories.PushBack(newCategory);
            return ref m_CritterCategories[m_CritterCategories.Count - 1];
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
                ref var category = ref m_CritterCategories[idx];
                category.Tagged++;
                if (category.Tagged >= category.Total)
                {
                    m_SiteData.TaggedCritters.Add(inCritter.CritterId);
                    m_CritterCategories.FastRemoveAt(idx);
                    // TODO: category is done!
                }
                return true;
            }

            return false;
        }

        static private IEnumerator PlayEffect(VFX inEffect)
        {
            yield return Routine.Combine(
                inEffect.Transform.ScaleTo(2, 0.5f, Axis.XY).Ease(Curve.CubeOut),
                inEffect.Sprite.FadeTo(0, 0.25f).DelayBy(0.25f)
            );
            inEffect.Free();
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
                m_Listener.LayerFilter = GameLayers.Critter_Mask;
                m_Listener.SetOccupantTracking(true);

                m_Listener.onTriggerEnter.AddListener(OnTaggableEnterRegion);
                m_Listener.onTriggerExit.AddListener(OnTaggableExitRegion);
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