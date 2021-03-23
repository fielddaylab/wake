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
    public class KelpStem : MonoBehaviour, IFoodSource
    {
        #region Inspector

        [SerializeField] private Transform m_Body;
        [SerializeField] private Collider2D m_Collider = null;

        #endregion // Inspector

        public float height {get; set;}
        public float root { get; set; }
        public Vector3 position { get; set; }
        private SpriteRenderer m_Spine;
        private StringHash32 m_Id;
        private ActorCtrl m_Parent;

        public void Initialize(ActorCtrl inParent) {
            BullKelpActor bull = m_Body.GetComponentInParent<BullKelpActor>();
            GiantKelpActor giant = m_Body.GetComponentInParent<GiantKelpActor>();
            if(bull == null && giant == null) return;

            m_Id = ExperimentServices.Actors.NextId("KelpStem");
            m_Spine = bull == null ? giant.GetSpine() : bull.GetSpine();
            height = m_Spine.size.y;
            root = inParent.Body.WorldTransform.position.x;
            position = m_Spine.transform.position;
            m_Parent = inParent;
        }

        public void ResetPosition(Vector3 point) {
            root = point.x;
            position = point;
        }

        Transform IFoodSource.Transform { get { return m_Body; } }

        Collider2D IFoodSource.Collider { get { return m_Collider; } }

        float IFoodSource.EnergyRemaining { get { return 0f; } }

        StringHash32 IFoodSource.Id { get { return m_Id; } }

        ActorCtrl IFoodSource.Parent { get { return m_Parent; } }

        public bool hasSpine() {
            return m_Spine != null;
        }

        bool IFoodSource.HasTag(StringHash32 inTag) {
            return inTag == "KelpStem";
        }

        void IFoodSource.Bite(ActorCtrl inActor, float inBite) {
            return;
        }

        bool IFoodSource.TryGetEatLocation(ActorCtrl inActor, out Transform outTransform, out Vector3 outOffset) {
            outTransform = null;
            outOffset = Vector3.negativeInfinity;
            return false;
        }

    }
}