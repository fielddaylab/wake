// comment out this define to disable the AB test
#define ANALYTICS_ABTEST_JOBPREDICTION

using System;
using BeauUtil;
using Aqua.Profile;

namespace Aqua.Analytics {
    static public class JobPredictionFeature {
        public enum Status {
            Invalid,
            Inactive,
            Active
        }

        private struct JobPredictionParams {
            public float Constant;
            public JobPredictionJob JobA;
            public JobPredictionJob JobB;
            public JobPredictionJob JobC;
        }

        private struct JobPredictionJob {
            public StringHash32 Id;
            public float Coefficient;
        }

        private struct JobPredictionCachedValues {
            public int CompletedTasks;
            public int CompletedJobs;
            public int VisitedStations;
            public ulong LaunchCount;
        }

        static private JobPredictionCachedValues s_CachedPredictionValues;

        static public Status GetStatus(SaveData saveData) {
#if ANALYTICS_ABTEST_JOBPREDICTION
            if (saveData == null || saveData.IsBookmark || string.IsNullOrEmpty(saveData.Id)) {
                return Status.Inactive;
            }

            char c = saveData.Id[0];
            int offset = char.ToUpperInvariant(c) - 'A';
            return (offset & 0x1) == 0 ? Status.Active : Status.Inactive;
#else
            return Status.Inactive;
#endif // ANALYTICS_ABTEST_JOBPREDICTION
        }

        static public void CacheJobPredictionValues() {
#if ANALYTICS_ABTEST_JOBPREDICTION
            if (GetStatus(Save.Current) != Status.Active) {
                return;
            }

            s_CachedPredictionValues.CompletedJobs = Save.Current.Jobs.CompletedJobIds().Count;
            s_CachedPredictionValues.CompletedTasks = Save.Current.Jobs.CalculateTotalCompletedTasks();
            s_CachedPredictionValues.LaunchCount = Save.Current.LaunchCount;

            int stationVisitCount = 0;
            if (Save.Current.Map.HasVisitedLocation(MapIds.ArcticStation)) {
                stationVisitCount++;
            }
            if (Save.Current.Map.HasVisitedLocation(MapIds.BayouStation)) {
                stationVisitCount++;
            }
            if (Save.Current.Map.HasVisitedLocation(MapIds.CoralStation)) {
                stationVisitCount++;
            }
            if (Save.Current.Map.HasVisitedLocation(MapIds.KelpStation)) {
                stationVisitCount++;
            }
            s_CachedPredictionValues.VisitedStations = stationVisitCount;
#endif // ANALYTICS_ABTEST_JOBPREDICTION
        }

        static public bool PredictFailure(StringHash32 jobId, out UnsafeSpan<StringHash32> jobsToCheck) {
#if ANALYTICS_ABTEST_JOBPREDICTION
            jobsToCheck = default;
            return true;
#else
            jobsToCheck = default;
            return false;
#endif // ANALYTICS_ABTEST_JOBPREDICTION
        }
    }
}