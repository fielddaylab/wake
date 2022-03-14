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
            public long Deprecated;

            public void Serialize(Serializer ioSerializer) {
                ioSerializer.Serialize("added", ref Added);
                ioSerializer.Serialize("deprecated", ref Deprecated, 0L);
            }
        }

        private class Data : ISerializedObject {
            public List<JobData> Jobs = new List<JobData>(64);

            public void Serialize(Serializer ioSerializer) {
                ioSerializer.ObjectArray("jobs", ref Jobs);
            }
        }

        private class JobData : ISerializedObject {
            public string Id;
            public ActiveRange Date;
            public List<TaskData> Tasks = new List<TaskData>();

            internal bool Included;

            public void Serialize(Serializer ioSerializer) {
                ioSerializer.Serialize("id", ref Id);
                ioSerializer.Object("date", ref Date);
                ioSerializer.ObjectArray("tasks", ref Tasks);
            }
        }

        private class TaskData : ISerializedObject {
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

            foreach(var job in AssetDBUtils.FindAssets<JobDesc>()) {
                string jobName = job.name;
                JobData jobData = db.Jobs.Find((j) => j.Id == jobName);
                if (jobData == null) {
                    jobData = new JobData();
                    jobData.Date.Added = nowTS;
                    jobData.Id = jobName;
                    db.Jobs.Add(jobData);
                    Log.Msg("[DBExport] New job '{0}' found", jobName);
                }

                jobData.Included = true;

                foreach(var taskId in job.EditorTaskIds()) {
                    TaskData taskData = jobData.Tasks.Find((t) => t.Id == taskId);
                    if (taskData == null) {
                        taskData = new TaskData();
                        taskData.Id = taskId;
                        taskData.Date.Added = nowTS;
                        jobData.Tasks.Add(taskData);
                        Log.Msg("[DBExport] New job '{0}' task '{1}' found", jobName, taskId);
                    }

                    taskData.Included = true;
                }
            }

            // Deprecate stuff

            foreach(var job in db.Jobs) {
                if (!job.Included && job.Date.Deprecated == 0) {
                    Log.Msg("[DBExport] Job '{0}' found to be deprecated", job.Id);
                    job.Date.Deprecated = nowTS;
                } else if (job.Included && job.Date.Deprecated != 0) {
                    Log.Warn("[DBExport] Job '{0}' was deprecated but is now valid again? Please don't do that.", job.Id);
                    job.Date.Deprecated = 0;
                }

                foreach(var task in job.Tasks) {
                    if (!task.Included && task.Date.Deprecated == 0) {
                        Log.Msg("[DBExport] Job '{0}' task '{1}' found to be deprecated", job.Id, task.Id);
                        task.Date.Deprecated = nowTS;
                    } else if (task.Included && task.Date.Deprecated != 0) {
                        Log.Warn("[DBExport] Job '{0}' task '{1}' was deprecated but is now valid again? Please don't do that.", job.Id, task.Id);
                        job.Date.Deprecated = 0;
                    }
                }
            }

            Serializer.WriteFile(db, ExportPath, OutputOptions.PrettyPrint, Serializer.Format.JSON);
            Log.Msg("[DBExport] Exported database to '{0}'", ExportPath);
            EditorUtility.OpenWithDefaultApp(ExportPath);
        }
    }
}