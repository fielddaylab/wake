using System.Net.Security;
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
using Aqua.Animation;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class SargassumActor : ActorModule
    {
        #region Inspector

        [Header("Height")]
        [SerializeField] private FloatRange m_Height = new FloatRange(6);

        [SerializeField] private AmbientTransform m_Anim = null;

        #endregion // Inspector

        [NonSerialized] private Sargassum m_Sarg = null;

        #region Events


        public override void OnConstruct()
        {
            base.OnConstruct();

            Actor.Callbacks.OnCreate = OnCreate;

            // DisableAmbient();

            Services.Events.Register(ExperimentEvents.ExperimentBegin, StartAmbient, this);

            m_Anim.enabled = false;

            m_Sarg = GetComponent<Sargassum>();
        }

        private void StartAmbient() 
        {
            m_Anim.AnimationScale = 1;
            m_Anim.enabled = true;
        }

        private void OnCreate()
        {
            m_Anim.enabled = false;
            m_Sarg.Initialize(Actor);

        }


        // private IEnumerator Animation()
        // {
        //     while(1)
        //     {
        //         yield return 
        //     }
        // }


        #endregion // Events
    }
}