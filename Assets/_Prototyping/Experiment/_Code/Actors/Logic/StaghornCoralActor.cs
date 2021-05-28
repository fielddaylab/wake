using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using BeauUtil.Variants;
using BeauRoutine.Extensions;

namespace ProtoAqua.Experiment
{
    public class StaghornCoralActor : ActorModule
    {
        #region Inspector

        [Header("Height")]
        [SerializeField] private FloatRange m_Height = new FloatRange(6);
        [SerializeField] private Transform m_HeightOffset = null;
        [SerializeField] private SpriteRenderer m_SpineRenderer = null;
        [SerializeField] private Transform m_HeightCapOffset = null;

        #endregion // Inspector

        [NonSerialized] private CoralBranch[] m_AllBranches = null;

        #region Events

        public override void OnConstruct()
        {
            base.OnConstruct();

            Actor.Callbacks.OnCreate = OnCreate;

            m_AllBranches = GetComponentsInChildren<CoralBranch>(true);
        }

        private void OnCreate()
        {
            float height = m_Height.Generate(RNG.Instance);
            m_SpineRenderer.flipX = RNG.Instance.NextBool();
            m_SpineRenderer.flipY = RNG.Instance.NextBool();
            
            Vector2 size = m_SpineRenderer.size;
            size.y = height;
            m_SpineRenderer.size = size;

            m_HeightOffset.SetPosition(height * 0.5f, Axis.Y, Space.Self);
            m_HeightCapOffset.SetPosition(height * 0.5f + 1, Axis.Y, Space.Self);

            int branchCount = (int)Math.Floor(RNG.Instance.NextFloat(GetProperty<float>("MinLeafCount", 2f), GetProperty<float>("MaxLeafCount", 4f)));

            float facing = RNG.Instance.Choose(-1, 1);
            for(int i = 0; i < branchCount; ++i)
            {
                float lerp = (i + 0.5f + RNG.Instance.NextFloat(-0.1f, 0.1f)) / branchCount;
                float leafHeight = (height * lerp);

                m_AllBranches[i].Initialize(leafHeight, facing, Actor);
                m_AllBranches[i].gameObject.SetActive(true);
            }

            for(int i = branchCount; i < m_AllBranches.Length; i++)
            {
                m_AllBranches[i].gameObject.SetActive(false);
            }
        }

        public SpriteRenderer GetSpine() {
            return m_SpineRenderer;
        }

        #endregion // Events
    }
}