using System;
using UnityEngine;

namespace EasyBugReporter {

    /// <summary>
    /// Reports system stats as according to SystemInfo.
    /// </summary>
    public class SystemInfoContext : IDumpSystem {
        public bool Dump(IDumpWriter writer) {
            writer.BeginSection("System Information", false);
            writer.Text(SystemInfoExt.Report(false));
            writer.Space();
            try {
                writer.KeyValue("Command Line Args", Environment.CommandLine);
            } catch(Exception) { }
            writer.EndSection();

            return true;
        }

        public void Initialize() {
        }

        public void Shutdown() {
        }

        public void Freeze() {
        }

        public void Unfreeze() {
        }
    }
}