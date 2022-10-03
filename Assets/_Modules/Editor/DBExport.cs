using System;
using System.Collections.Generic;
using System.IO;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Editor;
using UnityEditor;
using UnityEngine;

namespace Aqua.Editor {
    static public class DBExport {
        public const string ExportPath = "DBExport.json";

        private struct ActiveRange : ISerializedObject {
            public long Added;
            public long Updated;
            public long Deprecated;

            public void Serialize(Serializer ioSerializer) {
                ioSerializer.Serialize("added", ref Added);
                ioSerializer.Serialize("updated", ref Updated, Added);
                ioSerializer.Serialize("deprecated", ref Deprecated, 0L);
            }
        }

        private class Data : ISerializedObject {
            public List<JobData> Jobs = new List<JobData>(64);
            public JobsSummary Summary = new JobsSummary();

            public void Serialize(Serializer ioSerializer) {
                ioSerializer.ObjectArray("jobs", ref Jobs);
                ioSerializer.Object("jobsSummary", ref Summary);
            }
        }

        private class JobData : ISerializedObject {
            public string Id;
            public ActiveRange Date;
            public MechanicRatingsData Difficulties;
            public int RequiredExp;
            public List<IdentifierData> RequiredJobs = new List<IdentifierData>();
            public List<IdentifierData> RequiredUpgrades = new List<IdentifierData>();
            public List<IdentifierData> Tasks = new List<IdentifierData>();

            internal bool Included;

            public void Serialize(Serializer ioSerializer) {
                ioSerializer.Serialize("id", ref Id);
                ioSerializer.Object("date", ref Date);
                ioSerializer.Serialize("requiredExp", ref RequiredExp, 0);
                ioSerializer.ObjectArray("requiredJobs", ref RequiredJobs);
                ioSerializer.ObjectArray("requiredUpgrades", ref RequiredUpgrades);
                ioSerializer.Object("difficulties", ref Difficulties);
                ioSerializer.ObjectArray("tasks", ref Tasks);
            }
        }

        private class JobsSummary : ISerializedObject {
            public List<int> ExperimentationSummary;
            public List<int> ModelingSummary;
            public List<int> ArgumentationSummary;

            public void Serialize(Serializer ioSerializer) {
                ioSerializer.Array("experimentSummary", ref ExperimentationSummary);
                ioSerializer.Array("modelingSummary", ref ModelingSummary);
                ioSerializer.Array("argumentationSummary", ref ArgumentationSummary);
            }
        }

        private struct MechanicRatingsData : ISerializedObject {
            public int ExperimentationDifficulty;
            public int ModelingDifficulty;
            public int ArgumentationDifficulty;

            public void Serialize(Serializer ioSerializer) {
                ioSerializer.Serialize("experimentation", ref ExperimentationDifficulty);
                ioSerializer.Serialize("modeling", ref ModelingDifficulty);
                ioSerializer.Serialize("argumentation", ref ArgumentationDifficulty);
            }
        }

        private class IdentifierData : ISerializedObject {
            public string Id;
            public ActiveRange Date;

            internal bool Included;

            public void Serialize(Serializer ioSerializer) {
                ioSerializer.Serialize("id", ref Id);
                ioSerializer.Object("date", ref Date);
            }
        }

        [MenuItem("Aqualab/Export DB")]
        static public void Export() {
            Data db;
            if (File.Exists(ExportPath)) {
                db = Serializer.ReadFile<Data>(ExportPath);
                Log.Msg("[DBExport] Read old database export from '{0}'", ExportPath);
            } else {
                db = new Data();
                Log.Msg("[DBExport] No database export found at '{0}': creating new", ExportPath);
            }

            long nowTS = DateTime.UtcNow.ToFileTimeUtc();

            // init summary table
            db.Summary = new JobsSummary();
            db.Summary.ExperimentationSummary = new List<int>();
            db.Summary.ModelingSummary = new List<int>();
            db.Summary.ArgumentationSummary = new List<int>();
            int numCols = 6; // ratings from 0 to 5
            for (int c = 0; c < numCols; c++){
                db.Summary.ExperimentationSummary.Add(0);
                db.Summary.ModelingSummary.Add(0);
                db.Summary.ArgumentationSummary.Add(0);            
            }

            foreach (var job in AssetDBUtils.FindAssets<JobDesc>()) {
                string jobName = job.name;
                JobData jobData = db.Jobs.Find((j) => j.Id == jobName);
                if (jobData == null) {
                    jobData = new JobData();
                    jobData.Date.Added = nowTS;
                    jobData.Date.Updated = nowTS;
                    jobData.Id = jobName;
                    db.Jobs.Add(jobData);
                    Log.Msg("[DBExport] New job '{0}' found", jobName);
                }

                jobData.Included = true;

                foreach (var reqJobObj in job.RequiredJobs()) {
                    string reqJobId = reqJobObj.name;
                    IdentifierData reqJobData = jobData.RequiredJobs.Find((t) => t.Id == reqJobId);
                    if (reqJobData == null) {
                        reqJobData = new IdentifierData();
                        reqJobData.Id = reqJobId;
                        reqJobData.Date.Added = nowTS;
                        reqJobData.Date.Updated = nowTS;
                        jobData.RequiredJobs.Add(reqJobData);
                        Log.Msg("[DBExport] New job '{0}' required job '{1}' found", jobName, reqJobId);
                    }

                    reqJobData.Included = true;
                }

                foreach (var upgradeIdHash in job.RequiredUpgrades()) {
                    string upgradeId = upgradeIdHash.ToDebugString();
                    IdentifierData upgradeData = jobData.RequiredUpgrades.Find((t) => t.Id == upgradeId);
                    if (upgradeData == null) {
                        upgradeData = new IdentifierData();
                        upgradeData.Id = upgradeId;
                        upgradeData.Date.Added = nowTS;
                        upgradeData.Date.Updated = nowTS;
                        jobData.RequiredUpgrades.Add(upgradeData);
                        Log.Msg("[DBExport] New job '{0}' required upgrade '{1}' found", jobName, upgradeId);
                    }

                    upgradeData.Included = true;
                }

                foreach (var taskId in job.EditorTaskIds()) {
                    IdentifierData taskData = jobData.Tasks.Find((t) => t.Id == taskId);
                    if (taskData == null) {
                        taskData = new IdentifierData();
                        taskData.Id = taskId;
                        taskData.Date.Added = nowTS;
                        taskData.Date.Updated = nowTS;
                        jobData.Tasks.Add(taskData);
                        Log.Msg("[DBExport] New job '{0}' task '{1}' found", jobName, taskId);
                    }

                    taskData.Included = true;
                }

                jobData.RequiredExp = job.RequiredExp();
                jobData.Difficulties.ExperimentationDifficulty = job.Difficulty(ScienceActivityType.Experimentation);
                jobData.Difficulties.ModelingDifficulty = job.Difficulty(ScienceActivityType.Modeling);
                jobData.Difficulties.ArgumentationDifficulty = job.Difficulty(ScienceActivityType.Argumentation);

                // Unimplemented jobs should be left out. They have an argumentation rating of 0
                if (job.Difficulty(ScienceActivityType.Argumentation) != 0) {
                    // add to summary table
                    int oldVal = db.Summary.ExperimentationSummary[job.Difficulty(ScienceActivityType.Experimentation)];
                    int newVal = oldVal + 1;
                    db.Summary.ExperimentationSummary[job.Difficulty(ScienceActivityType.Experimentation)] = newVal;

                    oldVal = db.Summary.ModelingSummary[job.Difficulty(ScienceActivityType.Modeling)];
                    newVal = oldVal + 1;
                    db.Summary.ModelingSummary[job.Difficulty(ScienceActivityType.Modeling)] = newVal;

                    oldVal = db.Summary.ArgumentationSummary[job.Difficulty(ScienceActivityType.Argumentation)];
                    newVal = oldVal + 1;
                    db.Summary.ArgumentationSummary[job.Difficulty(ScienceActivityType.Argumentation)] = newVal;
                }            
            }

            // Deprecate stuff

            foreach (var job in db.Jobs) {
                if (!job.Included && job.Date.Deprecated == 0) {
                    Log.Msg("[DBExport] Job '{0}' found to be deprecated", job.Id);
                    job.Date.Deprecated = nowTS;
                } else if (job.Included && job.Date.Deprecated != 0) {
                    Log.Warn("[DBExport] Job '{0}' was deprecated but is now valid again? Please don't do that.", job.Id);
                    job.Date.Deprecated = 0;
                }

                foreach (var reqJob in job.RequiredJobs) {
                    if (!reqJob.Included && reqJob.Date.Deprecated == 0) {
                        Log.Msg("[DBExport] Job '{0}' required job '{1}' found to be deprecated", job.Id, reqJob.Id);
                        reqJob.Date.Deprecated = nowTS;
                    } else if (reqJob.Included && reqJob.Date.Deprecated != 0) {
                        Log.Warn("[DBExport] Job '{0}' required job '{1}' was deprecated but is now valid again? Please don't do that.", job.Id, reqJob.Id);
                        reqJob.Date.Deprecated = 0;
                    }

                    foreach (var upgrade in job.RequiredUpgrades) {
                        if (!upgrade.Included && upgrade.Date.Deprecated == 0) {
                            Log.Msg("[DBExport] Job '{0}' required upgrade '{1}' found to be deprecated", job.Id, upgrade.Id);
                            upgrade.Date.Deprecated = nowTS;
                        } else if (upgrade.Included && upgrade.Date.Deprecated != 0) {
                            Log.Warn("[DBExport] Job '{0}' required upgrade '{1}' was deprecated but is now valid again? Please don't do that.", job.Id, upgrade.Id);
                            upgrade.Date.Deprecated = 0;
                        }
                    }

                    foreach (var task in job.Tasks) {
                        if (!task.Included && task.Date.Deprecated == 0) {
                            Log.Msg("[DBExport] Job '{0}' task '{1}' found to be deprecated", job.Id, task.Id);
                            task.Date.Deprecated = nowTS;
                        } else if (task.Included && task.Date.Deprecated != 0) {
                            Log.Warn("[DBExport] Job '{0}' task '{1}' was deprecated but is now valid again? Please don't do that.", job.Id, task.Id);
                            task.Date.Deprecated = 0;
                        }
                    }
                }

                Serializer.WriteFile(db, ExportPath, OutputOptions.PrettyPrint, Serializer.Format.JSON);
                Log.Msg("[DBExport] Exported database to '{0}'", ExportPath);
                EditorUtility.OpenWithDefaultApp(ExportPath);
            }
        }
    }
}