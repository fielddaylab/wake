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
    public class TankWall : MonoBehaviour, IFoodSource
    {
        #region Inspector

        [SerializeField] private RectTransform m_Body;
        [SerializeField] private Collider2D m_Collider = null;
        [SerializeField] private ActorNavHelper m_Nav = null;
        [SerializeField] private FoundationalTank m_Tank = null;


        #endregion // Inspector

        public float height {get; set;}
        public float root { get; set; }
        public Vector3 position { get; set; }

        public void Awake() {
            position = m_Body.position;
            root = position.x;
            height = m_Nav.Rect().yMax - m_Tank.ClimbOffset();
        }
        Transform IFoodSource.Transform { get { return m_Body; } }

        Collider2D IFoodSource.Collider { get { return m_Collider; } }

        float IFoodSource.EnergyRemaining { get { return 0f; } }

        StringHash32 IFoodSource.Id { get { return null; } }

        ActorCtrl IFoodSource.Parent { get { return null; } }

    
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