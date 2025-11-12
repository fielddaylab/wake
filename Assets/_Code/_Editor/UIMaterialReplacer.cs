using BeauUtil.Debugger;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Editor {
    public sealed class UIMaterialReplacer : ScriptableWizard {
        public GameObject[] Roots;
        public Material FindMaterial;
        public Material SwapMaterial;

        public void Awake() {
            SwapMaterial = GetDefaultNoZMaterial();
        }

        public void OnWizardCreate() {
            foreach(var item in Roots) {
                Process(item, FindMaterial, SwapMaterial);
            }
        }

        [MenuItem("Window/Field Day/UI Material Swap Wizard")]
        static private void CreateWizard() {
            DisplayWizard<UIMaterialReplacer>("Swap UI Materials", "Remap");
        }

        [MenuItem("CONTEXT/Canvas/UI Material Swap Wizard")]
        static private void CreateWizardForCanvas(MenuCommand cmd) {
            var wizard = DisplayWizard<UIMaterialReplacer>("Swap UI Materials", "Remap");
            wizard.Roots = new GameObject[] { ((Canvas) cmd.context).gameObject };
        }

        [MenuItem("CONTEXT/RectTransform/UI Material Swap Wizard")]
        static private void CreateWizardForRectTransform(MenuCommand cmd) {
            var wizard = DisplayWizard<UIMaterialReplacer>("Swap UI Materials", "Remap");
            wizard.Roots = new GameObject[] { ((RectTransform) cmd.context).gameObject };
        }

        [MenuItem("CONTEXT/RectTransform/Replace Default UI Material with NoZ")]
        static private void SwapOnRectTransform(MenuCommand cmd) {
            GameObject root = ((RectTransform) cmd.context).gameObject;
            Process(root, null, GetDefaultNoZMaterial());
        }

        static private void Process(GameObject root, Material find, Material swap) {
            bool useDefault = find == null;
            foreach(var graphic in root.GetComponentsInChildren<Graphic>(true)) {
                if (graphic is TMP_Text) {
                    continue;
                }
                Material check = useDefault ? graphic.defaultMaterial : find;
                if (graphic.material == check) {
                    EditorUtility.SetDirty(graphic);
                    Undo.RecordObject(graphic, "Swapping material");
                    graphic.material = swap;
                    Debug.LogFormat(graphic, "Replaced material on " + graphic.name);
                }
            }
        }

        static private Material GetDefaultNoZMaterial() {
            return ValidationUtils.FindAsset<Material>("UI-DefaultNoZ");
        }
    }
}