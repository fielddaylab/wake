using System.Drawing;
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
    public class GlassWall : MonoBehaviour, IClimbable, IFoodSource
    {
        #region Inspector

        [SerializeField] private Transform m_Body;
        [SerializeField] private Collider2D m_Collider = null;

        [SerializeField] private ActorNavHelper m_Helper = null;

        [SerializeField] private ClimbSettings m_Settings = ClimbSettings.NONE;

        #endregion // Inspector
        [NonSerialized] private Vector2 m_Position;
        [NonSerialized] private float m_Root;
        [NonSerialized] private float m_Height;

        ClimbSettings IClimbable.Settings { get{return m_Settings;} }

        Vector2 IClimbable.position { get{return m_Position;} }

        float IClimbable.root { get{return m_Root; } }

        float IClimbable.height { get{return m_Height; } }

        Transform IClimbable.Transform { get { return m_Body; }}

        Collider2D IClimbable.Collider{get { return m_Collider;}}

        void IClimbable.Initialize(ActorCtrl _=null) 
        {
            m_Position = transform.position;
            m_Height = m_Helper.Rect().yMax;
            m_Root = m_Position.x;
        }

        void IClimbable.ResetPosition(Vector3 point) {
            m_Root = point.x;
            m_Position = point;
        }

        Transform IFoodSource.Transform { get { return m_Body; } }

        Collider2D IFoodSource.Collider { get { return m_Collider; } }

        float IFoodSource.EnergyRemaining { get { return 0f; } }

        StringHash32 IFoodSource.Id { get { return null; } }

        ActorCtrl IFoodSource.Parent { get { return null; } }
        bool IFoodSource.HasTag(StringHash32 inTag) {
            return inTag == "GlassWall";
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