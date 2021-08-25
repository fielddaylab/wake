using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Modeling
{
    public class BattleAnimationPrototype : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private BattleAnimationPrototypeActor[] m_Actors = null;
        [SerializeField] private BattleAnimationPrototypeWaterProperty[] m_WaterProps = null;
        [SerializeField] private TMP_Text m_Label = null;
        [SerializeField] private ParticleSystem m_EatEmojis = null;

        [Header("Play Controls")]
        [SerializeField] private ToggleGroup m_PlaybackGroup = null;
        [SerializeField] private Toggle m_PlayToggle = null;
        [SerializeField] private Toggle m_PauseToggle = null;
        [SerializeField] private Toggle m_FFToggle = null;

        #endregion // Inspector

        [NonSerialized] private Routine m_Animation;
        [NonSerialized] private SimulationProfile m_Profile;
        [NonSerialized] private readonly List<IEnumerator> m_TempAnimationList = new List<IEnumerator>(16);
        [NonSerialized] private bool m_Paused = false;
        [NonSerialized] private float m_PlaybackSpeed = 1;

        [NonSerialized] private SimulationResult[] m_LastResultsCached;
        [NonSerialized] private SimulationResultDetails[] m_LastDetailsCached;

        private void Awake()
        {
            m_PlayToggle.onValueChanged.AddListener(OnPlayToggled);
            m_PauseToggle.onValueChanged.AddListener(OnPauseToggled);
            m_FFToggle.onValueChanged.AddListener(OnFFToggled);
        }

        private void OnDisable()
        {
            m_Animation.Stop();
            ResetPlayback();
        }

        public void SetBuffer(SimulationBuffer inBuffer)
        {
            for(WaterPropertyId id = 0; id < WaterPropertyId.TRACKED_COUNT; id++)
            {
                m_WaterProps[(int) id].Initialize(Services.Assets.WaterProp.Property(id));
            }

            m_Profile = inBuffer.PlayerProfile;

            var actorTypes = m_Profile.Critters();
            for(int i = 0; i < actorTypes.Count; i++)
            {
                m_Actors[i].gameObject.SetActive(true);
                m_Actors[i].Initialize(actorTypes[i].Desc());
            }
            for(int i = actorTypes.Count; i < m_Actors.Length; i++)
            {
                m_Actors[i].gameObject.SetActive(false);
            }

            m_Label.SetText(string.Empty);
            m_Animation.Stop();
        }

        private void SetResult(SimulationResult inResult, SimulationResultDetails inDetails, int inTick)
        {
            var actorTypes = m_Profile.Critters();
            for(int i = 0; i < actorTypes.Count; i++)
            {
                m_Actors[i].SetPopulation(inResult.GetCritters(actorTypes[i].Id()).Population, inDetails.StartingStates[i]);
            }

            for(WaterPropertyId id = 0; id < WaterPropertyId.TRACKED_MAX; id++)
            {
                m_WaterProps[(int) id].SetValue(inDetails.StartingEnvironment[id]);
            }

            m_Label.SetText(inTick.ToStringLookup());
        }

        #region Toggles

        private void ResetPlayback()
        {
            m_Paused = false;
            m_PlaybackSpeed = 1;
            m_PlaybackGroup.allowSwitchOff = true;
            m_PlayToggle.SetIsOnWithoutNotify(false);
            m_PauseToggle.SetIsOnWithoutNotify(false);
            m_FFToggle.SetIsOnWithoutNotify(false);
        }

        private void SetIsPlaying()
        {
            m_PlayToggle.SetIsOnWithoutNotify(!m_Paused && m_PlaybackSpeed == 1);
            m_PauseToggle.SetIsOnWithoutNotify(m_Paused);
            m_FFToggle.SetIsOnWithoutNotify(!m_Paused && m_PlaybackSpeed > 1);
            m_PlaybackGroup.allowSwitchOff = false;
        }

        private void OnPlayToggled(bool inbValue)
        {
            if (!inbValue)
                return;

            m_PlaybackSpeed = 1f;
            m_Paused = false;
            SyncAnimationSettings();
        }

        private void OnPauseToggled(bool inbValue)
        {
            if (!inbValue)
                return;

            m_PlaybackSpeed = 1f;
            m_Paused = true;
            SyncAnimationSettings();
        }

        private void OnFFToggled(bool inbValue)
        {
            if (!inbValue)
                return;

            m_PlaybackSpeed = 4f;
            m_Paused = false;
            SyncAnimationSettings();
        }

        private void SyncAnimationSettings()
        {
            if (!m_Animation)
            {
                ReplayAnimation();
            }
            else
            {
                if (m_Paused)
                    m_Animation.Pause();
                else
                    m_Animation.Resume();

                m_Animation.SetTimeScale(m_PlaybackSpeed);
            }
        }

        #endregion // Toggles

        #region Animation

        public void Animate(SimulationResult[] inResults, SimulationResultDetails[] inDetails)
        {
            m_LastResultsCached = inResults;
            m_LastDetailsCached = inDetails;

            m_EatEmojis.Stop();
            SetResult(inResults[0], inDetails[1], inResults[0].Timestamp);
            m_Animation.Replace(this, Animation(inResults, inDetails));

            if (m_Paused)
                m_Animation.Pause();
            m_Animation.SetTimeScale(m_PlaybackSpeed);

            SetIsPlaying();
        }

        private void ReplayAnimation()
        {
            Animate(m_LastResultsCached, m_LastDetailsCached);
        }

        private IEnumerator Animation(SimulationResult[] inResults, SimulationResultDetails[] inDetails)
        {
            for(int i = 1; i < inDetails.Length; i++)
            {
                yield return AnimateStep(inResults[i - 1], inDetails[i], inResults[i].Timestamp);
            }

            ResetPlayback();
        }

        private IEnumerator AnimateStep(SimulationResult inResult, SimulationResultDetails inDetails, int inTick)
        {
            SetResult(inResult, inDetails, inTick);
            ClearBatch();

            WaterPropertyBlockF32 properties = inDetails.StartingEnvironment;

            foreach(var prop in Simulator.PreemptiveProperties)
            {
                int critterIdx = 0;
                foreach(var critter in inDetails.Consumed)
                {
                    properties[prop] -= critter[prop];
                    if (critter[prop] > 0)
                    {
                        BatchAnimation(m_WaterProps[(int) prop].PlayOutgoing(m_Actors[critterIdx].transform));
                    }
                    ++critterIdx;
                }

                BatchAnimation(m_WaterProps[(int) prop].AnimateValue(properties[prop]));
            }

            yield return FlushBatch();
            yield return 0.2f;

            var actorTypes = m_Profile.Critters();
            bool bChanged = false;
            for(int i = 0; i < actorTypes.Count; i++)
            {
                ActorStateId state = inDetails.AfterLightStates[i];
                bChanged |= BatchAnimation(m_Actors[i].AnimatePopulation(state == ActorStateId.Dead ? 0 : inResult.GetCritters(actorTypes[i].Id()).Population, state, false));
                if (state == ActorStateId.Dead)
                    inResult.SetCritters(i, 0);
            }

            if (bChanged)
            {
                yield return FlushBatch();
                yield return 0.2f;
            }

            foreach(var prop in Simulator.SecondaryProperties)
            {
                int critterIdx = 0;
                foreach(var critter in inDetails.Consumed)
                {
                    properties[prop] -= critter[prop];
                    if (critter[prop] > 0)
                    {
                        BatchAnimation(m_WaterProps[(int) prop].PlayOutgoing(m_Actors[critterIdx].transform));
                    }
                    critterIdx++;
                }

                critterIdx = 0;
                foreach(var critter in inDetails.Produced)
                {
                    properties[prop] += critter[prop];
                    if (critter[prop] > 0)
                    {
                        BatchAnimation(m_WaterProps[(int) prop].PlayIncoming(m_Actors[critterIdx].transform));
                    }
                    critterIdx++;
                }

                BatchAnimation(m_WaterProps[(int) prop].AnimateValue(properties[prop]));
            }

            yield return FlushBatch();
            yield return 0.2f;

            foreach(var eat in inDetails.Eaten)
            {
                BattleAnimationPrototypeActor eater = m_Actors[eat.Eater];
                BattleAnimationPrototypeActor eaten = m_Actors[eat.Eaten];

                eaten.transform.SetAsLastSibling();
                eater.transform.SetAsLastSibling();

                Vector3 eaterPos = eater.transform.localPosition;
                Vector3 eatenPos = eaten.transform.localPosition;

                Vector3 vec = eatenPos - eaterPos;
                float dist = Math.Max(vec.magnitude - 100, 20);
                vec.Normalize();
                vec *= dist;

                uint newEaten = inResult.AdjustCritters(eat.Eaten, -(int) eat.Population);

                using(new AnimationLock((RectTransform) eater.transform))
                using(new AnimationLock((RectTransform) eaten.transform))
                {
                    yield return Routine.Combine(
                        eater.transform.MoveTo(eaterPos + vec, 0.3f, Axis.XY, Space.Self).Ease(Curve.CubeIn),
                        eater.transform.MoveTo(eaterPos, 0.3f, Axis.XY, Space.Self).Ease(Curve.Smooth).DelayBy(0.35f),
                        AnimateEaten(eater, eaten, m_EatEmojis, newEaten, 0.3f),
                        eaten.transform.MoveTo(eatenPos.x + 5, 0.3f, Axis.X, Space.Self).Wave(Wave.Function.Cos, 4).DelayBy(0.3f)
                    );
                }
            }

            for(int i = 0; i < inDetails.Deaths.Count; i++)
            {
                uint pop = inResult.AdjustCritters(i, -(int) inDetails.Deaths[i]);
                BatchAnimation(m_Actors[i].AnimatePopulation(pop, true));
            }

            yield return FlushBatch();
            yield return 0.2f;

            for(int i = 0; i < inDetails.Growth.Count; i++)
            {
                uint pop = inResult.AdjustCritters(i, (int) inDetails.Growth[i]);
                BatchAnimation(m_Actors[i].AnimatePopulation(pop, true));
            }

            yield return FlushBatch();
            yield return 0.2f;
        }

        static private IEnumerator AnimateEaten(BattleAnimationPrototypeActor inEater, BattleAnimationPrototypeActor inEaten, ParticleSystem inEmoji, uint inNewCritters, float inDelay)
        {
            yield return inDelay;

            ParticleSystem.EmitParams emit = default;
            emit.position = (inEater.transform.localPosition + inEaten.transform.localPosition) / 2;

            inEmoji.Emit(emit, 1);
            yield return inEaten.AnimatePopulation(inNewCritters, false);
        }

        #endregion // Animation

        #region Batch

        private void ClearBatch()
        {
            m_TempAnimationList.Clear();
        }

        private bool BatchAnimation(IEnumerator inEnumerator)
        {
            if (inEnumerator != null)
            {
                m_TempAnimationList.Add(inEnumerator);
                return true;
            }

            return false;
        }

        private IEnumerator FlushBatch()
        {
            IEnumerator combined = Routine.Combine(m_TempAnimationList);
            m_TempAnimationList.Clear();
            return combined;
        }

        #endregion // Batch

        private struct AnimationLock : IDisposable
        {
            public RectTransform RectTransform;
            public RectTransformState State;
            
            public AnimationLock(RectTransform inTransform)
            {
                RectTransform = inTransform;
                State = RectTransformState.Create(inTransform);
            }

            public void Dispose()
            {
                State.Apply(RectTransform);
            }
        }
    }
}