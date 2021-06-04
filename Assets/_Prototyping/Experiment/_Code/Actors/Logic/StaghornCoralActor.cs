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
    public class StaghornCoralActor : ActorModule
    {
        #region Inspector

        [Header("Height")]
        [SerializeField] private FloatRange m_Height = new FloatRange(6);
        [SerializeField] private StaghornCoral m_Coral = null;
        #endregion // Inspector

        #region Events


        public override void OnConstruct()
        {
            base.OnConstruct();

            Actor.Callbacks.OnCreate = OnCreate;

            // DisableAmbient();
        }

        private void OnCreate()
        {
            m_Coral.Initialize(Actor);

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