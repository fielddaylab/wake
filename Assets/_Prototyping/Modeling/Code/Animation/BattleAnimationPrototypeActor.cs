using BeauUtil;
using UnityEngine;
using System;
using UnityEngine.UI;
using Aqua;
using System.Collections;
using BeauRoutine;
using BeauPools;

namespace ProtoAqua.Modeling
{
    public class BattleAnimationPrototypeActor : MonoBehaviour
    {
        #region Inspector
        
        [SerializeField] private Image[] m_Icons = null;
        [SerializeField] private RectTransform m_PopulationMeter = null;
        [SerializeField] private Image m_StressIcon = null;

        #endregion // Inspector

        [NonSerialized] private BestiaryDesc m_ActorType = null;
        [NonSerialized] private BFBody m_ActorBody = null;

        [NonSerialized] private uint m_CurrentPopulation;
        [NonSerialized] private int m_CurrentIconCount;
        [NonSerialized] private ActorStateId m_CurrentState;
        private Routine m_Animation;

        public void Initialize(BestiaryDesc inActorType)
        {
            m_ActorType = inActorType;
            m_ActorBody = inActorType.FactOfType<BFBody>();

            foreach(var icon in m_Icons)
            {
                icon.sprite = inActorType.Icon();
            }

            SetPopulation(0, ActorStateId.Alive);
        }

        private void OnDisable()
        {
            m_Animation.Stop();
        }

        public void SetPopulation(uint inPopulation, ActorStateId inState)
        {
            m_Animation.Stop();

            float populationPercent = (float) inPopulation / m_ActorBody.PopulationHardCap();
            int displayIconCount = (int) Math.Ceiling(populationPercent * m_Icons.Length);

            for(int i = 0; i < displayIconCount; i++)
            {
                InstantOn(m_Icons[i]);
            }

            for(int i = displayIconCount; i < m_Icons.Length; i++)
            {
                InstantOff(m_Icons[i]);
            }

            if (inState == ActorStateId.Stressed)
                InstantOn(m_StressIcon);
            else
                InstantOff(m_StressIcon);

            InstantMeter(m_PopulationMeter, populationPercent);
            m_CurrentPopulation = inPopulation;
            m_CurrentIconCount = displayIconCount;
            m_CurrentState = inState;
        }

        public bool AnimatePopulation(uint inPopulation)
        {
            return AnimatePopulation(inPopulation, m_CurrentState);
        }

        public bool AnimatePopulation(uint inPopulation, ActorStateId inState)
        {
            if (m_CurrentPopulation == inPopulation && m_CurrentState == inState)
                return false;

            m_CurrentPopulation = inPopulation;
            m_CurrentState = inState;

            float populationPercent = (float) inPopulation / m_ActorBody.PopulationHardCap();
            int displayIconCount = (int) Math.Ceiling(populationPercent * m_Icons.Length);

            using(PooledList<IEnumerator> enumerators = PooledList<IEnumerator>.Create())
            {
                enumerators.Add(AnimateMeter(m_PopulationMeter, populationPercent));
                if (displayIconCount < m_CurrentIconCount)
                {
                    for(int i = m_CurrentIconCount - 1; i >= displayIconCount; i--)
                    {
                        enumerators.Add(AnimateOff(m_Icons[i], RNG.Instance.NextFloat(0.1f)));
                    }
                }
                else if (displayIconCount > m_CurrentIconCount)
                {
                    for(int i = m_CurrentIconCount; i < displayIconCount; i++)
                    {
                        enumerators.Add(AnimateOn(m_Icons[i], RNG.Instance.NextFloat(0.1f)));
                    }
                }

                if (m_CurrentState == ActorStateId.Stressed)
                    enumerators.Add(AnimateOn(m_StressIcon, 0));
                else
                    enumerators.Add(AnimateOff(m_StressIcon, 0));

                m_CurrentIconCount = displayIconCount;
                m_Animation.Replace(this, Routine.Combine(enumerators));
            }

            return true;
        }

        #region Animations

        static private void InstantMeter(RectTransform inTransform, float inPercent)
        {
            inTransform.anchorMax = new Vector2(inPercent, 1);
        }

        static private IEnumerator AnimateMeter(RectTransform inTransform, float inPercent)
        {
            Vector4 anchors = new Vector4(0, 0, inPercent, 1);
            yield return inTransform.AnchorTo(anchors, 0.2f, Axis.X).Ease(Curve.CubeOut).ForceOnCancel();
        }

        static private void InstantOn(Image inImage)
        {
            Transform transform = inImage.transform;
            transform.SetScale(1, Axis.XY);
            transform.gameObject.SetActive(true);
        }

        static private void InstantOff(Image inImage)
        {
            Transform transform = inImage.transform;
            transform.SetScale(0, Axis.XY);
            transform.gameObject.SetActive(false);
        }

        static private IEnumerator AnimateOn(Image inImage, float inDelay)
        {
            yield return inDelay;

            Transform transform = inImage.transform;
            if (!transform.gameObject.activeSelf)
            {
                transform.gameObject.SetActive(true);
                transform.SetScale(0, Axis.XY);
            }

            yield return transform.ScaleTo(1, 0.2f, Axis.XY).Ease(Curve.BackOut).ForceOnCancel();
        }

        static private IEnumerator AnimateOff(Image inImage, float inDelay)
        {
            yield return inDelay;

            Transform transform = inImage.transform;
            if (transform.gameObject.activeSelf)
            {
                yield return transform.ScaleTo(0, 0.2f, Axis.XY).Ease(Curve.CubeIn).ForceOnCancel();
                transform.gameObject.SetActive(false);
            }
        }

        #endregion // Animations
    }
}