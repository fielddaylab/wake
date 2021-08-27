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
        [SerializeField] private Image m_ActionFillMeter = null;

        #endregion // Inspector

        [NonSerialized] private uint m_PopulationMeterCap = 0;
        [NonSerialized] private uint m_PopulationIconCap = 0;

        [NonSerialized] private uint m_CurrentPopulation;
        [NonSerialized] private int m_CurrentIconCount;
        [NonSerialized] private ActorStateId m_CurrentState;

        public void Initialize(BestiaryDesc inActorType)
        {
            BFBody body = inActorType.FactOfType<BFBody>();
            m_PopulationIconCap = body.PopulationHardCap;
            m_PopulationMeterCap = body.PopulationSoftCap;

            foreach(var icon in m_Icons)
            {
                icon.sprite = inActorType.Icon();
            }

            SetPopulation(0, ActorStateId.Alive);
        }

        public void SetPopulation(uint inPopulation, ActorStateId inState)
        {
            float meterPercent = Mathf.Clamp01((float) inPopulation / m_PopulationMeterCap);
            int displayIconCount = (int) Math.Ceiling((float) inPopulation / m_PopulationIconCap * m_Icons.Length);

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

            InstantMeter(m_PopulationMeter, meterPercent);
            m_CurrentPopulation = inPopulation;
            m_CurrentIconCount = displayIconCount;
            m_CurrentState = inState;
            m_ActionFillMeter.fillAmount = 0;
        }

        public IEnumerator AnimatePopulation(uint inPopulation, bool inbPlayFill)
        {
            return AnimatePopulation(inPopulation, m_CurrentState, inbPlayFill);
        }

        public IEnumerator AnimatePopulation(uint inPopulation, ActorStateId inState, bool inbPlayFill)
        {
            if (m_CurrentPopulation == inPopulation && m_CurrentState == inState)
                return null;

            m_CurrentPopulation = inPopulation;
            m_CurrentState = inState;

            float meterPercent = Mathf.Clamp01((float) inPopulation / m_PopulationMeterCap);
            int displayIconCount = (int) Math.Ceiling((float) inPopulation / m_PopulationIconCap * m_Icons.Length);

            using(PooledList<IEnumerator> enumerators = PooledList<IEnumerator>.Create())
            {
                enumerators.Add(AnimateMeter(m_PopulationMeter, meterPercent));
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

                if (inbPlayFill)
                    enumerators.Add(AnimateFill(m_ActionFillMeter));

                m_CurrentIconCount = displayIconCount;
                return Routine.Combine(enumerators);
            }
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

        static private IEnumerator AnimateFill(Image inFill)
        {
            inFill.fillAmount = 0;
            yield return inFill.FillTo(1, 0.3f);
            yield return 0.3f;
            inFill.fillAmount = 0;
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

            yield return transform.ScaleTo(1, 0.3f, Axis.XY).Ease(Curve.BackOut).ForceOnCancel();
        }

        static private IEnumerator AnimateOff(Image inImage, float inDelay)
        {
            yield return inDelay;

            Transform transform = inImage.transform;
            if (transform.gameObject.activeSelf)
            {
                yield return transform.ScaleTo(0, 0.3f, Axis.XY).Ease(Curve.CubeIn).ForceOnCancel();
                transform.gameObject.SetActive(false);
            }
        }

        #endregion // Animations
    }
}