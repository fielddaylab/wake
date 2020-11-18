using UnityEngine;
using ProtoCP;
using System;
using BeauRoutine;
using BeauPools;
using BeauData;
using BeauUtil;
using System.Collections.Generic;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class BehaviorCaptureControl : ServiceBehaviour, ISceneUnloadHandler
    {
        static public readonly FourCC GlobalServiceId = FourCC.Parse("BEHV");

        #region Types

        [Serializable] private class CaptureLocationPool : SerializablePool<BehaviorCaptureLocation> { }

        public class CaptureInstance : IDisposable
        {
            private BehaviorCaptureLocation m_Location;
            private readonly uint m_Magic;

            internal CaptureInstance(BehaviorCaptureLocation inLocation, Transform inTransform, StringHash32 inBehaviorId)
            {
                m_Location = inLocation;
                m_Magic = inLocation.Initialize(inBehaviorId, inTransform);
                inLocation.Show();
            }

            public void Dispose()
            {
                if (m_Location)
                {
                    m_Location.TryKill(m_Magic);
                    m_Location = null;
                }
            }
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private CaptureLocationPool m_LocationPool = null;

        #endregion // Inspector

        private void OnAttemptObserve(StringHash32 inBehaviorId)
        {
            Services.UI.WorldFaders.Flash(Color.white.WithAlpha(0.5f), 0.25f);
            Services.Audio.PostEvent("capture_flash");

            if (Services.Data.Profile.Bestiary.RegisterBehaviorObserved(inBehaviorId))
            {
                var behaviorDef = Services.Tweaks.Get<ExperimentSettings>().GetBehavior(inBehaviorId);

                Services.Audio.PostEvent("capture_new");
                Services.Events.Dispatch(ExperimentEvents.BehaviorAddedToLog, inBehaviorId);
                Services.UI.Popup.Display(Services.Loc.Localize(behaviorDef.ObserveTitleId, "Behavior Observed!"), Services.Loc.Localize(behaviorDef.DescId, inBehaviorId.ToDebugString()))
                    .OnComplete((r) => {
                        Services.Script.TriggerResponse(ExperimentTriggers.NewBehaviorObserved);
                    });
            }
            else
            {
                Services.Script.TriggerResponse(ExperimentTriggers.BehaviorAlreadyObserved);
            }
        }

        private void OnExperimentTeardown()
        {
            m_LocationPool.Reset();
        }

        public CaptureInstance GetCaptureInstance(ActorCtrl inActor, StringHash32 inBehaviorId)
        {
            return new CaptureInstance(m_LocationPool.Alloc(), inActor.transform, inBehaviorId);
        }

        public bool WasObserved(StringHash32 inBehaviorId)
        {
            return Services.Data.Profile.Bestiary.WasBehaviorObserved(inBehaviorId);
        }

        #region IService

        public override FourCC ServiceId()
        {
            return GlobalServiceId;
        }

        protected override void OnRegisterService()
        {
            base.OnRegisterService();

            Services.Events.Register<StringHash32>(ExperimentEvents.AttemptObserveBehavior, OnAttemptObserve, this)
                .Register(ExperimentEvents.ExperimentTeardown, OnExperimentTeardown, this);
        }

        protected override void OnDeregisterService()
        {
            Services.Events?.DeregisterAll(this);

            base.OnDeregisterService();
        }

        #endregion // IService

        #region ISceneUnload

        void ISceneUnloadHandler.OnSceneUnload(SceneBinding inScene, object inContext)
        {
            m_LocationPool.Reset();
        }

        #endregion // ISceneUnload
    }
}