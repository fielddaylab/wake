using System;
using System.Collections;
using Aqua.Profile;
using Aqua.Scripting;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Services;
using Leaf;

namespace Aqua
{
    [ServiceDependency(typeof(DataService), typeof(AssetsService), typeof(EventService))]
    internal partial class PreloadWatcher : ServiceBehaviour
    {
        [NonSerialized] private StringHash32 m_LastKnownStation;
        [NonSerialized] private bool m_HasShip;

        protected override void Initialize()
        {
            base.Initialize();

            SaveSummaryData predictedProfile = Services.Data.LastProfileSummary();
            StringHash32 stationId;
            bool hasShip;
            if (!string.IsNullOrEmpty(predictedProfile.Id)) {
                stationId = predictedProfile.CurrentStation;
                hasShip = Bits.Contains(predictedProfile.Flags, SaveSummaryFlags.UnlockedShip);
            } else {
                stationId = Services.Assets.Map.DefaultStationId();
                hasShip = false;
            }

            SetStation(stationId);
            SetHasShip(hasShip);

            Services.Events.Register<StringHash32>(GameEvents.StationChanged, SetStation, this)
                .Register(GameEvents.ProfileLoaded, OnProfileLoaded, this)
                .Register(GameEvents.SceneLoaded, OnSceneLoaded, this)
                .Register(GameEvents.JobStarted, OnJobStarted, this);
        }

        private void OnProfileLoaded() {
            SetStation(Save.Map.CurrentStationId());
            SaveSummaryFlags summaryFlags = SaveSummaryData.GetFlags(Save.Current);
            SetHasShip(Bits.Contains(summaryFlags, SaveSummaryFlags.UnlockedShip));
        }

        private void OnSceneLoaded() {
            StringHash32 mapId = MapDB.LookupCurrentMap();
            if (mapId == MapIds.Helm || mapId == MapIds.KelpStation) {
                SetHasShip(true);
            }
        }

        private void OnJobStarted() {
            StringHash32 jobId = Save.CurrentJobId;
            if (jobId == JobIds.Kelp_welcome) {
                SetHasShip(true);
            }
        }

        private void SetStation(StringHash32 stationId) {
            if (m_LastKnownStation == stationId) {
                return;
            }

            StringHash32 old = m_LastKnownStation;
            m_LastKnownStation = stationId;
            Services.Assets.PreloadGroup(stationId);
            Services.Assets.CancelPreload(old);
        }

        private void SetHasShip(bool hasShip) {
            if (m_HasShip == hasShip) {
                return;
            }

            m_HasShip = hasShip;
            if (hasShip) {
                Services.Assets.PreloadGroup("Ship");
            } else {
                Services.Assets.CancelPreload("Ship");
            }
        }

        protected override void Shutdown()
        {
            Services.Events?.DeregisterAll(this);

            base.Shutdown();
        }
    }
}
