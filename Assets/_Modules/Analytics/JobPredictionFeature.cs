#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

// comment out this define to disable the AB test
#define ANALYTICS_ABTEST_JOBPREDICTION

using System;
using BeauUtil;
using Aqua.Profile;
using System.Collections.Generic;
using UnityEngine;
using BeauUtil.Debugger;
using BeauRoutine;
using System.Collections;

namespace Aqua.Analytics {
    static public class JobPredictionFeature {
        private const int JobDifficultyCategoryThreshold = 2;
        private const double DenomEpsilon = double.Epsilon * 4;
        private const double FailureThreshold = 0.5;

        private const string JobDataTablePath = "Research/JobPredictionCoefficients";

        public enum Status {
            Invalid,
            Inactive,
            Active
        }

#if ANALYTICS_ABTEST_JOBPREDICTION

        private struct JobPredictionParams {
            public double Constant;
            public JobPredictionJob JobA;
            public JobPredictionJob JobB;
            public JobPredictionJob JobC;
            public double CompletedTasksCoefficient;
            public double CompletedJobsCoefficient;
            public double CompletedJobsExpCoefficient;
            public double CompletedJobsModelCoefficient;
            public double CompletedJobsArgueCoefficient;
            public double VisitedStationCoefficient;
            public double LaunchCountCoefficient;
            public double PlaytimeCoefficient;
        }

        private struct JobPredictionJob {
            public StringHash32 Id;
            public double Coefficient;
        }

        private struct JobPredictionCachedValues {
            public int CompletedTasks;
            public int CompletedJobs;
            public int CompletedJobsExp;
            public int CompletedJobsModel;
            public int CompletedJobsArgue;
            public int VisitedStations;
            public ulong LaunchCount;
        }

        static private JobPredictionCachedValues s_CachedPredictionValues;
        static private Dictionary<StringHash32, JobPredictionParams> s_PredictTable;
        static private AsyncHandle s_AsyncTableLoad;

#endif // ANALYTICS_ABTEST_JOBPREDICTION

#if DEVELOPMENT
        static private bool s_DEBUGAlwaysPredictFailure;

        static internal bool DEBUG_IsAlwaysPredictFailure() {
            return s_DEBUGAlwaysPredictFailure;
        }

        static internal void DEBUG_SetAlwaysPredictFailure(bool active) {
            s_DEBUGAlwaysPredictFailure = active;
        }
#endif // DEVELOPMENT

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

        static public void TryLoadTable() {
#if ANALYTICS_ABTEST_JOBPREDICTION
            if (s_PredictTable != null) {
                return;
            }
            
            if (GetStatus(Save.Current) != Status.Active) {
                return;
            }

            if (s_AsyncTableLoad.IsRunning()) {
                return;
            }

            s_AsyncTableLoad = Async.Schedule(AsyncTableLoad(), AsyncFlags.MainThreadOnly);
            
#endif // ANALYTICS_ABTEST_JOBPREDICTION
        }

#if ANALYTICS_ABTEST_JOBPREDICTION
        static private IEnumerator AsyncTableLoad() {

        }

        static private void TryLoadTablePanic() {
            if (s_PredictTable != null) {
                return;
            }

            if (GetStatus(Save.Current) != Status.Active) {
                return;
            }

            if (s_AsyncTableLoad.IsRunning()) {
                s_AsyncTableLoad.Cancel();
            }

            TextAsset asset = Resources.Load<TextAsset>(JobDataTablePath);
        }
#endif // ANALYTICS_ABTEST_JOBPREDICTION

        static public void CacheJobPredictionValues() {
#if ANALYTICS_ABTEST_JOBPREDICTION
            if (GetStatus(Save.Current) != Status.Active) {
                return;
            }

            s_CachedPredictionValues.CompletedJobs = Save.Current.Jobs.CompletedJobIds().Count;
            s_CachedPredictionValues.CompletedTasks = Save.Current.Jobs.CalculateTotalCompletedTasks();
            s_CachedPredictionValues.LaunchCount = Save.Current.LaunchCount;

            int experimentCount = 0, modelCount = 0, argueCount = 0;
            foreach(var jobId in Save.Current.Jobs.CompletedJobIds()) {
                var job = Assets.Job(jobId);
                if (job.Difficulty(ScienceActivityType.Experimentation) >= JobDifficultyCategoryThreshold) {
                    experimentCount++;
                }
                if (job.Difficulty(ScienceActivityType.Modeling) >= JobDifficultyCategoryThreshold) {
                    modelCount++;
                }
                if (job.Difficulty(ScienceActivityType.Argumentation) >= JobDifficultyCategoryThreshold) {
                    argueCount++;
                }
            }

            s_CachedPredictionValues.CompletedJobsExp = experimentCount;
            s_CachedPredictionValues.CompletedJobsModel = modelCount;
            s_CachedPredictionValues.CompletedJobsArgue = argueCount;

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

        static public unsafe bool PredictFailure(StringHash32 jobId, out UnsafeSpan<StringHash32> jobsToCheck) {
#if ANALYTICS_ABTEST_JOBPREDICTION
            if (GetStatus(Save.Current) != Status.Active) {
                jobsToCheck = default;
                return false;
            }

            TryLoadTablePanic();

            double playTime = Save.Current.Playtime;
            JobsData jobs = Save.Current.Jobs;

            JobPredictionParams jobParms;
            if (!s_PredictTable.TryGetValue(jobId, out jobParms)) {
                Log.Warn("[JobPredictionFeature] Prediction parameters not specified for job '{0}'", jobId.ToDebugString());
                jobsToCheck = default;
#if DEVELOPMENT
                return s_DEBUGAlwaysPredictFailure;
#else
                return false;
#endif // DEVELOPMENT
            }

            JobPredictionCachedValues cached = s_CachedPredictionValues;

            double denomAccum = jobParms.Constant;
            denomAccum += AccumulateJob(jobParms.JobA, jobs)
                + AccumulateJob(jobParms.JobB, jobs)
                + AccumulateJob(jobParms.JobC, jobs);
            denomAccum += (jobParms.CompletedTasksCoefficient * cached.CompletedTasks)
                + (jobParms.CompletedJobsCoefficient * cached.CompletedJobs)
                + (jobParms.CompletedJobsExpCoefficient * cached.CompletedJobsExp)
                + (jobParms.CompletedJobsModelCoefficient * cached.CompletedJobsModel)
                + (jobParms.CompletedJobsArgueCoefficient * cached.CompletedJobsArgue);
            denomAccum += (jobParms.VisitedStationCoefficient * cached.VisitedStations)
                + (jobParms.LaunchCountCoefficient * cached.LaunchCount)
                + (jobParms.PlaytimeCoefficient * playTime);

            double determValue;
            if (Math.Abs(denomAccum) <= DenomEpsilon) {
                determValue = 0;
            } else {
                determValue = Math.Log(1.0 / denomAccum);
            }

#if DEVELOPMENT
            if (s_DEBUGAlwaysPredictFailure) {
                determValue = 1;
            }
#endif // DEVELOPMENT
            if (determValue > FailureThreshold) {
                jobsToCheck = new UnsafeSpan<StringHash32>(Frame.AllocArray<StringHash32>(3), 3);
                jobsToCheck[0] = jobParms.JobA.Id;
                jobsToCheck[1] = jobParms.JobB.Id;
                jobsToCheck[2] = jobParms.JobC.Id;
                return true;
            }

            jobsToCheck = default;
            return false;
#else
            jobsToCheck = default;
            return false;
#endif // ANALYTICS_ABTEST_JOBPREDICTION
        }

#if ANALYTICS_ABTEST_JOBPREDICTION
        static private double AccumulateJob(in JobPredictionJob job, JobsData jobs) {
            if (job.Id.IsEmpty || !jobs.IsComplete(job.Id)) {
                return 0;
            }

            return job.Coefficient;
        }
#endif // ANALYTICS_ABTEST_JOBPREDICTION
    }
}