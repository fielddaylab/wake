using UnityEngine;
using UnityEditor;
using BeauUtil.Editor;
using BeauUtil.Debugger;
using System.IO;

namespace Aqua.Editor {
    public class BestiaryIconHelper : ScriptableWizard {
        public const string IconDirectory = "Assets/_Content/Bestiary/Icons";

        public BestiaryDesc[] Entries;

        [MenuItem("Aqualab/Bestiary/Check for Icons")]
        static void CreateWizard() {
            ScriptableWizard.DisplayWizard<BestiaryIconHelper>("Check Bestiary Icons", "Process");
        }

        private void OnWizardCreate() {
            foreach(var entry in Entries) {
                Sprite icon = entry.m_Icon;
                if (icon == null) {
                    Log.Error("[BestiaryIconHelper] Icon missing on '{0}'", entry.name);
                } else {
                    string assetPath = AssetDatabase.GetAssetPath(icon.texture);
                    string assetDirectory = Path.GetDirectoryName(assetPath).TrimEnd('/').Replace('\\', '/');
                    if (assetDirectory != IconDirectory) {
                        string renamedPath = Path.Combine(IconDirectory, Path.GetFileName(assetPath));
                        Sprite preexisting = AssetDatabase.LoadAssetAtPath<Sprite>(renamedPath);
                        if (preexisting != null) {
                            entry.m_Icon = preexisting;
                            EditorUtility.SetDirty(entry);
                            Log.Warn("[BestiaryIconHelper] Icon for '{0}' was set to a version outside of Bestiary Icons folder", entry.name);
                        } else {
                            AssetDatabase.CopyAsset(assetPath, renamedPath);
                            AssetDatabase.ImportAsset(renamedPath, ImportAssetOptions.Default);
                            Sprite newSprite = AssetDatabase.LoadAssetAtPath<Sprite>(renamedPath);
                            entry.m_Icon = newSprite;
                            EditorUtility.SetDirty(entry);
                            Log.Warn("[BestiaryIconHelper] Icon for '{0}' was copied to Bestiary Icons folder", entry.name);
                        }
                    }
                }
            }
        }
    }
}