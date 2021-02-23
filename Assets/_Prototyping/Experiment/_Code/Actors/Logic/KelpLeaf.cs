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
        [SerializeField] private Collider2D m_Collider = null;
        [SerializeField] private SpriteAnimator m_Animator = null;

        #endregion // Inspector

        [NonSerialized] private StringHash32 m_Id;
        [NonSerialized] private float m_EnergyRemaining;
        [NonSerialized] private Routine m_BiteAnim;
        [NonSerialized] private ActorCtrl m_Parent;
        [NonSerialized] private ActorConfig m_Config;

        public void Initialize(float inHeight, float inPivotSide, ActorCtrl inParent)
        {
            m_Id = ExperimentServices.Actors.NextId("KelpLeaf");
            m_PivotTransform.SetPosition(inHeight, Axis.Y, Space.Self);
            m_PivotTransform.SetScale(inPivotSide, Axis.X);
            m_PivotTransform.SetRotation(RNG.Instance.NextFloat(-40) * inPivotSide, Axis.Z, Space.Self);
            m_EnergyRemaining = 100f;
            m_Parent = inParent;
            m_Config = inParent.Config;

            m_Animator.Play(m_Config.GetProperty<SpriteAnimation>("LeafHealthyAnimation"));
        }

        private void OnDisable()
        {
            m_BiteAnim.Stop();
            m_Animator.Stop();
            m_Id = StringHash32.Null;
        }

        #region IFoodSource

        Transform IFoodSource.Transform { get { return m_RenderTransform; } }

        Collider2D IFoodSource.Collider { get { return m_Collider; } }

        float IFoodSource.EnergyRemaining { get { return m_EnergyRemaining; } }

        StringHash32 IFoodSource.Id { get { return m_Id; } }

        ActorCtrl IFoodSource.Parent { get { return m_Parent; } }

        void IFoodSource.Bite(ActorCtrl inActor, float inBite)
        {
            float prevRemaining = m_EnergyRemaining;
            m_EnergyRemaining = Mathf.Max(m_EnergyRemaining - inBite, 15f);
            m_BiteAnim.Replace(this, BittenAnim());

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

        private IEnumerator BittenAnim()
        {
            yield return m_PivotTransform.RotateTo(m_PivotTransform.localEulerAngles.z + RNG.Instance.Choose(-5, 5), 0.5f, Axis.Z, Space.Self).Wave(Wave.Function.CosFade, 3).RevertOnCancel(false);
        }

        #endregion // IFoodSource
    }
}