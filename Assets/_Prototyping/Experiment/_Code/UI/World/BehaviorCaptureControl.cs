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

        [NonSerialized] private ExperimentSetupData m_Setup;

        private void OnAttemptObserve(StringHash32 inBehaviorId)
        {
            Services.UI.WorldFaders.Flash(Color.white.WithAlpha(0.5f), 0.25f);
            Services.Audio.PostEvent("capture_flash");

            if (Services.Data.Profile.Bestiary.RegisterFact(inBehaviorId))
            {
                var factDef = Services.Assets.Bestiary.Fact(inBehaviorId);

                Services.Audio.PostEvent("capture_new");
                Services.Events.Dispatch(ExperimentEvents.BehaviorAddedToLog, inBehaviorId);

                Services.UI.Popup.Display(factDef.name, factDef.GenerateSentence())
                    .OnComplete((r) => {
                        using(var table = Services.Script.GetTempTable())
                        {
                            table.Set("factId", inBehaviorId);
                            Services.Script.TriggerResponse(ExperimentTriggers.NewBehaviorObserved);
                        }
                    });
            }
            else
            {
                using(var table = Services.Script.GetTempTable())
                {
                    table.Set("factId", inBehaviorId);
                    Services.Script.TriggerResponse(ExperimentTriggers.BehaviorAlreadyObserved);
                }
            }
        }

        private void OnExperimentSetup(ExperimentSetupData inData)
        {
            m_Setup = inData;
        }

        private void OnExperimentTeardown()
        {
            m_Setup = null;
            m_LocationPool.Reset();
        }

        public CaptureInstance GetCaptureInstance(ActorCtrl inActor, StringHash32 inBehaviorId)
        {
            if (m_Setup.Tank != TankType.Foundational)
                return null;
            
            return new CaptureInstance(m_LocationPool.Alloc(), inActor.transform, inBehaviorId);
        }

        public bool WasObserved(StringHash32 inBehaviorId)
        {
            return Services.Data.Profile.Bestiary.HasFact(inBehaviorId);
        }

        #region IService

        protected override void Initialize()
        {
            base.Initialize();

            Services.Events.Register<StringHash32>(ExperimentEvents.AttemptObserveBehavior, OnAttemptObserve, this)
                .Register<ExperimentSetupData>(ExperimentEvents.SetupInitialSubmit, OnExperimentSetup, this)
                .Register(ExperimentEvents.ExperimentTeardown, OnExperimentTeardown, this);
        }

        protected override void Shutdown()
        {
            Services.Events?.DeregisterAll(this);

            base.Shutdown();
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