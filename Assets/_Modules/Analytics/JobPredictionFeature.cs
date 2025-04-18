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
            public double VisitedStationsCoefficient;
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
            return ResearchTests.IsABC(saveData, 1) ? Status.Active : Status.Inactive;
#else
            return Status.Invalid;
#endif // ANALYTICS_ABTEST_JOBPREDICTION
        }

        static public string GetModifiedBranchName(string branchName, Status active) {
            switch (active) {
                case Status.Invalid:
                default:
                    return branchName;
                case Status.Inactive:
                    return branchName + "-no-failure-prediction";
                case Status.Active:
                    return branchName + "-has-failure-prediction";
            }
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
            Services.State.RegisterLoadDependency(s_AsyncTableLoad);
#endif // ANALYTICS_ABTEST_JOBPREDICTION
        }

#if ANALYTICS_ABTEST_JOBPREDICTION
        static private readonly StringUtils.CSV.Splitter s_TableSplitter = new StringUtils.CSV.Splitter(false);

        private struct IndexedP {
            public int Index;
            public double P;
        }

        static private IEnumerator AsyncTableLoad() {
            var asyncLoad = Resources.LoadAsync<TextAsset>(JobDataTablePath);
            while(!asyncLoad.isDone) {
                yield return Async.Sleep(0);
            }

            TextAsset tableAsset = (TextAsset) asyncLoad.asset;
            StringSlice[] lines = StringSlice.Split(tableAsset.text, StringUtils.DefaultNewLineChars, StringSplitOptions.RemoveEmptyEntries);

            Dictionary<StringHash32, JobPredictionParams> table = MapUtils.Create<StringHash32, JobPredictionParams>(lines.Length - 1);

            for(int i = 1; i < lines.Length; i++) {
                JobPredictionParams parms = ReadLine(lines[i], out StringHash32 jobId);
                if (!jobId.IsEmpty) {
                    table[jobId] = parms;
                }
                yield return null;
            }

            Assets.FullyUnload(tableAsset);

            s_AsyncTableLoad = default;
            s_PredictTable = table;
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
                s_AsyncTableLoad = default;
            }

            TextAsset tableAsset = Resources.Load<TextAsset>(JobDataTablePath);
            StringSlice[] lines = StringSlice.Split(tableAsset.text, StringUtils.DefaultNewLineChars, StringSplitOptions.RemoveEmptyEntries);

            Dictionary<StringHash32, JobPredictionParams> table = MapUtils.Create<StringHash32, JobPredictionParams>(lines.Length - 1);

            for (int i = 1; i < lines.Length; i++) {
                JobPredictionParams parms = ReadLine(lines[i], out StringHash32 jobId);
                if (!jobId.IsEmpty) {
                    table[jobId] = parms;
                }
            }

            Assets.FullyUnload(tableAsset);

            s_PredictTable = table;
        }

        static private unsafe JobPredictionParams ReadLine(StringSlice slice, out StringHash32 id) {
            JobPredictionParams parms;
            var columns = slice.Split(s_TableSplitter, StringSplitOptions.None);
            if (columns.Length < 16) {
                id = null;
                return default;
            }

            int c = 0;
            id = columns[c++];
            if (id.IsEmpty || !Services.Assets.Jobs.HasId(id)) {
                Log.Error("[JobPredictionFeature] Job '{0}' not found", id.ToDebugString());
                id = default;
                return default;
            }

            JobPredictionJob* allJobs = stackalloc JobPredictionJob[3];
            IndexedP* pValues = stackalloc IndexedP[3];

            allJobs[0].Id = columns[c++];
            allJobs[1].Id = columns[c++];
            allJobs[2].Id = columns[c++];

            if (!ValidateJobCoefficients(id, allJobs, 3)) {
                id = default;
                return default;
            }

            pValues[0].Index = 0;
            pValues[1].Index = 1;
            pValues[2].Index = 2;

            pValues[0].P = StringParser.ParseDouble(columns[c++]);
            pValues[1].P = StringParser.ParseDouble(columns[c++]);
            pValues[2].P = StringParser.ParseDouble(columns[c++]);

            allJobs[0].Coefficient = StringParser.ParseDouble(columns[c++]);
            allJobs[1].Coefficient = StringParser.ParseDouble(columns[c++]);
            allJobs[2].Coefficient = StringParser.ParseDouble(columns[c++]);

            Unsafe.Quicksort(pValues, 3, (a, b) => {
                return Math.Sign(a.P - b.P);
            });

            parms.JobA = allJobs[pValues[0].Index];
            parms.JobB = allJobs[pValues[1].Index];
            parms.JobC = allJobs[pValues[2].Index];

            parms.CompletedJobsArgueCoefficient = StringParser.ParseDouble(columns[c++]);
            parms.CompletedJobsModelCoefficient = StringParser.ParseDouble(columns[c++]);
            parms.CompletedJobsExpCoefficient = StringParser.ParseDouble(columns[c++]);

            parms.PlaytimeCoefficient = StringParser.ParseDouble(columns[c++]);
            parms.CompletedJobsCoefficient = StringParser.ParseDouble(columns[c++]);
            parms.CompletedTasksCoefficient = StringParser.ParseDouble(columns[c++]);
            parms.VisitedStationsCoefficient = StringParser.ParseDouble(columns[c++]);
            parms.LaunchCountCoefficient = StringParser.ParseDouble(columns[c++]);
            parms.Constant = StringParser.ParseDouble(columns[c++]);

            Log.Msg("[JobPredictionFeature] Loaded parameters for job '{0}'", id.ToDebugString());
            return parms;
        }

        static private unsafe bool ValidateJobCoefficients(StringHash32 lineId, JobPredictionJob* indices, int count) {
            for(int i = 0; i < count; i++) {
                StringHash32 id = indices[i].Id;
                if (!id.IsEmpty && !Services.Assets.Jobs.HasId(id)) {
                    Log.Error("[JobPredictionFeature] Dependent job '{0}' (job '{1}') not found", id.ToDebugString(), lineId.ToDebugString());
                    return false;
                }
            }

            return true;
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
                if (job.Difficulty(ScienceActivityType.Experimentation) > JobDifficultyCategoryThreshold) {
                    experimentCount++;
                }
                if (job.Difficulty(ScienceActivityType.Modeling) > JobDifficultyCategoryThreshold) {
                    modelCount++;
                }
                if (job.Difficulty(ScienceActivityType.Argumentation) > JobDifficultyCategoryThreshold) {
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
            if (Save.Current.Map.HasVisitedLocation(MapIds.FinalStation)) {
                stationVisitCount++;
            }
            s_CachedPredictionValues.VisitedStations = stationVisitCount;
#endif // ANALYTICS_ABTEST_JOBPREDICTION
        }

        static public unsafe bool PredictFailure(StringHash32 jobId, out UnsafeSpan<StringHash32> jobsToCheck) {
#if ANALYTICS_ABTEST_JOBPREDICTION
            if (GetStatus(Save.Current) != Status.Active) {
                jobsToCheck = default;
#if DEVELOPMENT
                return s_DEBUGAlwaysPredictFailure;
#else
                return false;
#endif // DEVELOPMENT
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
            denomAccum += (jobParms.VisitedStationsCoefficient * cached.VisitedStations)
                + (jobParms.LaunchCountCoefficient * cached.LaunchCount)
                + (jobParms.PlaytimeCoefficient * playTime);

            double determValue = 1 / (1.0 + Math.Exp(-denomAccum));
            Log.Msg("[JobPredictionFeature] Probability of failure: {0}", determValue);

#if DEVELOPMENT
            if (s_DEBUGAlwaysPredictFailure) {
                determValue = 1;
                Log.Warn("[JobPredictionFeature] Forcing failure");
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