using System;
using System.Collections.Generic;
using System.IO;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Editor;
using UnityEditor;
using UnityEngine;

namespace Aqua.Editor
{
    static public class JobTextExport
    {
        public const string ExportPath = "JobText.txt";

        static private readonly StringHash32[] StationOrder = new StringHash32[] {
            MapIds.KelpStation, MapIds.CoralStation, MapIds.BayouStation, MapIds.ArcticStation, MapIds.FinalStation
        };

        [MenuItem("Aqualab/Export Job Text", true)]
        static private bool Export_Validate() {
            return Application.isPlaying;
        }

        [MenuItem("Aqualab/Export Job Text")]
        static public void Export() {
            using (var writer = new StreamWriter(File.Open(ExportPath, FileMode.Create))) {
                List<JobDesc> jobs = new List<JobDesc>();
                foreach (var job in AssetDBUtils.FindAssets<JobDesc>()) {
                    if (!char.IsLetterOrDigit(job.name[0])) {
                        continue;
                    }

                    jobs.Add(job);
                }

                jobs.Sort((a, b) => {
                    int envA = Array.IndexOf(StationOrder, a.StationId()), envB = Array.IndexOf(StationOrder, b.StationId());
                    if (envA == envB) {
                        return a.name.CompareTo(b.name);
                    } else {
                        return envA.CompareTo(envB);
                    }
                });

                foreach(var job in jobs) {
                    writer.Write("Job:\t");
                    writer.Write(job.name);
                    writer.Write("\nName:\t");
                    writer.Write(Loc.Find(job.NameId()));
                    writer.Write("\nDescription:\t");
                    writer.Write(Loc.Find(job.DescId()));
                    writer.Write("\nStation:\t");
                    writer.Write(Loc.Find(Assets.Map(job.StationId()).LabelId()));
                    writer.Write("\nDifficulties:");
                    writer.Write("\n\tExperimentation:\t");
                    writer.Write(job.Difficulty(ScienceActivityType.Experimentation));
                    writer.Write("\n\tModeling:\t");
                    writer.Write(job.Difficulty(ScienceActivityType.Modeling));
                    writer.Write("\n\tArgumentation:\t");
                    writer.Write(job.Difficulty(ScienceActivityType.Argumentation));
                    foreach(var task in job.Tasks()) {
                        writer.Write("\nTask:\t");
                        writer.Write(Loc.Find(task.LabelId));
                    }
                    writer.Write("\n\n");
                }
            }

            Log.Msg("[JobTextExport] Exported text to '{0}'", ExportPath);
            EditorUtility.OpenWithDefaultApp(ExportPath);
        }
    }
}