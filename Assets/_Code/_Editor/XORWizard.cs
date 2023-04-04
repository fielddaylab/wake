using UnityEngine;
using UnityEditor;
using BeauUtil.Editor;
using BeauUtil.Debugger;
using System.IO;

namespace Aqua.Editor {
    public class XORWizard : ScriptableWizard {
        public string Source;

        [MenuItem("Aqualab/DEBUG/XOR Generator")]
        static void CreateWizard() {
            ScriptableWizard.DisplayWizard<XORWizard>("Generate XOR Hashes", "Generate (Single)", "Generate");
        }

        private void OnWizardCreate() {
            string output = UnsafeExt.XORCrypt(Source);
            string verify = UnsafeExt.XORCrypt(output);
            Debug.LogFormat("[XORWizard] `{0}` -> `{1}` -> '{2}'", Source, output, verify);
        }

        private void OnWizardOtherButton() {
            OnWizardCreate();
        }
    }
}