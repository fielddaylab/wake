using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using BeauUtil.Variants;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class KelpLeaf : MonoBehaviour, IFoodSource
    {
        #region Inspector

        [SerializeField] private Transform m_PivotTransform = null;
        [SerializeField] private Transform m_RenderTransform = null;
        [SerializeField] private SpriteAnimator m_Animator = null;

        #endregion // Inspector

        [NonSerialized] private StringHash32 m_Id;
        [NonSerialized] private float m_EnergyRemaining;
        [NonSerialized] private Routine m_BiteAnim;
        [NonSerialized] private ActorConfig m_Config;

        public void Initialize(float inHeight, float inPivotSide, ActorConfig inConfig)
        {
            m_Id = ExperimentServices.Actors.NextId("KelpLeaf");
            m_PivotTransform.SetPosition(inHeight, Axis.Y, Space.Self);
            m_PivotTransform.SetScale(inPivotSide, Axis.X);
            m_PivotTransform.SetRotation(RNG.Instance.NextFloat(-40) * inPivotSide, Axis.Z, Space.Self);
            m_EnergyRemaining = 100f;
            m_Config = inConfig;

            m_Animator.Play(inConfig.GetProperty<SpriteAnimation>("LeafHealthyAnimation"));
        }

        private void OnDisable()
        {
            m_BiteAnim.Stop();
            m_Animator.Stop();
        }

        private IEnumerator BiteAnim()
        {
            yield return m_PivotTransform.RotateTo(m_PivotTransform.localEulerAngles.z + RNG.Instance.Choose(-5, 5), 0.5f, Axis.Z, Space.Self).Wave(Wave.Function.CosFade, 3).RevertOnCancel(false);
        }

        #region IFoodSource

        Transform IFoodSource.Transform { get { return m_RenderTransform; } }

        float IFoodSource.EnergyRemaining { get { return m_EnergyRemaining; } }

        StringHash32 IFoodSource.Id { get { return m_Id; } }

        void IFoodSource.Bite(ActorCtrl inActor, float inBite)
        {
            float prevRemaining = m_EnergyRemaining;
            m_EnergyRemaining = Mathf.Max(m_EnergyRemaining - inBite, 15f);
            m_BiteAnim.Replace(this, BiteAnim());

            if (prevRemaining >= 50 && m_EnergyRemaining < 50)
            {
                m_Animator.Play(m_Config.GetProperty<SpriteAnimation>("LeafHalfEatenAnimation"));
            }
        }

        bool IFoodSource.HasTag(StringHash32 inTag)
        {
            return inTag == "Kelp";
        }

        bool IFoodSource.TryGetEatLocation(ActorCtrl inActor, out Transform outTransform, out Vector3 outOffset)
        {
            outTransform = m_PivotTransform;
            
            float dist = m_EnergyRemaining / 100f * m_Config.GetProperty<float>("MaxLeafLength") * RNG.Instance.NextFloat(0.25f, 1);
            outOffset = m_PivotTransform.right;
            outOffset.x *= m_PivotTransform.localScale.x;
            outOffset.Normalize();
            outOffset *= dist;

            return true;
        }

        #endregion // IFoodSource
    }
}