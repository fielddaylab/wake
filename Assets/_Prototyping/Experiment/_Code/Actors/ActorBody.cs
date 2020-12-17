using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class ActorBody : ActorModule
    {
        #region Inspector

        [SerializeField] private Rigidbody2D m_Body = null;
        [SerializeField] private float m_BodyRadius = 0.5f;
        [SerializeField] private ColorGroup m_ColorGroup = null;

        #endregion // Inspector
        
        [NonSerialized] private Transform m_Transform;
        [NonSerialized] private TriggerListener2D m_WaterListener;

        public Transform WorldTransform { get { return m_Transform; } }
        public Rigidbody2D Rigidbody { get { return m_Body; } }
        public float BodyRadius { get { return m_BodyRadius; } }
        public ColorGroup RenderGroup { get { return m_ColorGroup; } }

        public void Show()
        {
            m_WaterListener.enabled = true;
            m_Body.simulated = true;
            m_ColorGroup.Visible = true;
        }

        public void Hide()
        {
            m_WaterListener.enabled = false;
            m_Body.simulated = false;
            m_ColorGroup.Visible = false;
        }

        #region Listeners

        private void OnContactWater(Collider2D inCollider)
        {
            Services.Audio.PostEvent("tank_water_splash");
        }

        private void OnLeaveWater(Collider2D inCollider)
        {

        }

        #endregion // Listeners

        #region IPool

        public override void OnAlloc()
        {
            base.OnAlloc();
            Hide();
        }

        public override void OnConstruct()
        {
            base.OnConstruct();
            m_Transform = transform;

            m_WaterListener = m_Body.gameObject.AddComponent<TriggerListener2D>();
            m_WaterListener.LayerFilter = GameLayers.Water_Mask;
            m_WaterListener.onTriggerEnter.AddListener(OnContactWater);
            m_WaterListener.onTriggerExit.AddListener(OnLeaveWater);
        }

        #endregion // IPool
    }
}