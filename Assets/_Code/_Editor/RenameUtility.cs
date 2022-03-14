using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using BeauUtil;
using System.Text.RegularExpressions;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using System;
using UnityEditorInternal;

namespace EditorScripts
{
    public class RenameUtility  : EditorWindow {
        #region Inspector

        [Serializable] private struct BatchEntry {
            public ScriptableObject file;
            public string newName;
        }

        [SerializeField] private List<BatchEntry> m_Batch = new List<BatchEntry>();

        #endregion // Inspector

        private ReorderableList m_BatchList;
        private SerializedObject m_SerializedObj;

        #region Wizard

        static private readonly string[] DefaultExtensions = new string[] {
            ".leaf", ".scan", ".aqloc", ".asset", ".unity", ".prefab"
        };

        private const string RenameRecordPath = "Assets/_Content/IdRenames.txt";

        [MenuItem("Aqualab/Content Renaming Tool")]
        static private void Create() {
            var rename = EditorWindow.GetWindow<RenameUtility>();
            rename.Show();
        }

        private void OnEnable() {
            titleContent = new GUIContent("Content Renaming Tool");

            m_SerializedObj = new SerializedObject(this);
            SerializedProperty batch = m_SerializedObj.FindProperty("m_Batch");

            m_BatchList = new ReorderableList(m_SerializedObj, batch);
            m_BatchList.drawHeaderCallback = (r) => EditorGUI.LabelField(r, "Files to Rename");
            m_BatchList.drawElementCallback = DefaultElementDelegate(m_BatchList);
            m_BatchList.elementHeight = 2f * EditorGUIUtility.singleLineHeight + 4;
        }

        private void OnGUI() {
            m_SerializedObj.UpdateIfRequiredOrScript();

            GUILayout.Box("Drag files here", GUILayout.Height(50), GUILayout.ExpandWidth(true));

            Rect lastRect = GUILayoutUtility.GetLastRect();
            Event currentEvt = Event.current;
            if (lastRect.Contains(currentEvt.mousePosition)) {
                if (currentEvt.type == EventType.DragUpdated) {
                    foreach(UnityEngine.Object obj in DragAndDrop.objectReferences) {
                        ScriptableObject file = obj as ScriptableObject;
                        if (!file) {
                            continue;
                        }

                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        currentEvt.Use();
                        break;
                    }
                } else if (currentEvt.type == EventType.DragPerform) {
                    foreach(UnityEngine.Object obj in DragAndDrop.objectReferences) {
                        ScriptableObject file = obj as ScriptableObject;
                        if (!file) {
                            continue;
                        }

                        bool valid = true;
                        foreach(var batch in m_Batch) {
                            if (batch.file == file) {
                                valid = false;
                                break;
                            }
                        }

                        if (!valid) {
                            continue;
                        }

                        m_Batch.Add(new BatchEntry() {
                            file = file
                        });
                    }
                    currentEvt.Use();
                }
            }

            EditorGUILayout.Space();

            m_BatchList.DoLayoutList();

            EditorGUILayout.Space();

            string error = GetCurrentError();
            bool hasError = !string.IsNullOrEmpty(error);
            if (hasError) {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            } else {
                if (GUILayout.Button("Rename")) {
                    if (EditorUtility.DisplayDialog("Confirm Rename",
                        string.Format("Renaming {0} identifiers\n\nAre you sure?", m_Batch.Count),
                        "Yes!", "No")) {
                            bool success = Rename(GenerateBatch(m_Batch), "Assets/", DefaultExtensions, true, RenameRecordPath);
                            if (success) {
                                m_Batch.Clear();
                            }
                        }
                }
            }

            m_SerializedObj.ApplyModifiedProperties();
        }

        private string GetCurrentError() {
            if (m_Batch.Count == 0) {
                return "Select at least one file to rename.";
            } else {
                HashSet<string> targets = new HashSet<string>();
                foreach(var entry in m_Batch) {
                    if (entry.file == null) {
                        return "Each entry must have a file.";
                    } else {
                        targets.Add(entry.file.name);
                        if (!VariantUtils.IsValidIdentifier(entry.newName)) {
                            return "Each entry must have a valid new identifier";
                        } else if (!targets.Add(entry.newName)) {
                            return "Overlapping renamed identifiers";
                        }
                    }
                }

                return null;
            }
        }

        static private RenamePair[] GenerateBatch(List<BatchEntry> entries) {
            RenamePair[] pairs = new RenamePair[entries.Count];
            for(int i = 0; i < pairs.Length; i++) {
                pairs[i] = new RenamePair(
                    entries[i].file.name,
                    entries[i].newName
                );
            }
            return pairs;
        }

        static private ReorderableList.ElementCallbackDelegate DefaultElementDelegate(ReorderableList list) {
            return (Rect rect, int index, bool isActive, bool isFocused) => {
                SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                element.Next(true);
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(rect, element);
                element.Next(false);
                rect.y += rect.height + 2;
                EditorGUI.PropertyField(rect, element);
                // EditorGUI.PropertyField(rect, element, GUIContent.none, true);
            };
        }

        #endregion // Wizard

        #region Logic

        public struct RenamePair {
            public string src;
            public string dest;

            public RenamePair(string src, string dest) {
                this.src = src;
                this.dest = dest;
            }
        }

        static public bool Rename(string oldName, string newName, string directory, string[] allowedExtensions = null, bool forceUpdateDatabase = true, string outputResultsTo = null) {
            RenamePair[] pair = new RenamePair[] {
                new RenamePair(oldName, newName)
            };
            return Rename(pair, directory, allowedExtensions, forceUpdateDatabase, outputResultsTo);
        }

        static public bool Rename(RenamePair[] batch, string directory, string[] allowedExtensions = null, bool forceUpdateDatabase = true, string outputResultsTo = null) {
            if (batch.Length == 0) {
                return true;
            }

            using(Profiling.Time("renaming id references")) {
                try {
                    string header = GetHeader(batch);
                    EditorUtility.DisplayProgressBar(header, "Generating Regex", 0);
                    RegexPair[] renameOps = GenerateRegex(batch);
                    HashSet<string> paths = new HashSet<string>();
                    HashSet<RenamePair> fileRenames = new HashSet<RenamePair>();
                    EditorUtility.DisplayProgressBar(header, "Gathering Files", 0.1f);
                    GatherAllFiles(directory, batch, allowedExtensions, paths, fileRenames);
                    EditorUtility.DisplayProgressBar(header, "Renaming Files", 0.2f);
                    foreach(var rename in fileRenames) {
                        if (!PerformRename(rename)) {
                            Log.Error("Unable to perform rename of {0} to {1}", rename.src, rename.dest);
                            return false;
                        }
                    }
                    int processedCount = 0;
                    float processedProgressFactor = 0.75f / paths.Count;
                    foreach(var path in paths) {
                        EditorUtility.DisplayProgressBar(header, "Processing File: " + path, 0.25f + (processedCount++ * processedProgressFactor));
                        ProcessFile(path, renameOps);
                    }

                    if (!string.IsNullOrEmpty(outputResultsTo)) {
                        using(var writer = new StreamWriter(File.Open(outputResultsTo, FileMode.Append))) {
                            foreach(var pair in batch) {
                                writer.WriteLine();
                                writer.Write(pair.src);
                                writer.Write(": ");
                                writer.Write(pair.dest);
                            }
                        }
                    }
                } finally {
                    EditorUtility.ClearProgressBar();
                }
            }

            if (forceUpdateDatabase) {
                using(Profiling.Time("refreshing assets")) {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                }
            }

            return true;
        }

        static public string FieldRenamePattern(string fieldName) {
            return string.Format("{0}: ", fieldName);
        }

        private struct RegexPair {
            public Regex find;
            public string replace;
        }

        static private RegexPair[] GenerateRegex(RenamePair[] batch) {
            RegexPair[] regexes = new RegexPair[4 * batch.Length];
            for(int i = 0; i < batch.Length; i++) {
                RenamePair rename = batch[i];
                StringHash32 oldHash = rename.src;
                StringHash32 newHash = rename.dest;

                string oldNamePattern = EscapeRegex(rename.src);
                
                regexes[4 * i + 0] = new RegexPair() {
                    find = new Regex(string.Format("(?<!\\.)\\b{0}\\b(?!:)", oldNamePattern)),
                    replace = rename.dest,
                };
                regexes[4 * i + 1] = new RegexPair() {
                    find = new Regex(string.Format("(m_Hash: {0}\\n)", oldHash.HashValue)),
                    replace = string.Format("m_HashValue: {0}\n", newHash.HashValue)
                };
                regexes[4 * i + 2] = new RegexPair() {
                    find = new Regex(string.Format("(m_HashValue: {0}\\n)", oldHash.HashValue)),
                    replace = string.Format("m_HashValue: {0}\n", newHash.HashValue)
                };
                regexes[4 * i + 3] = new RegexPair() {
                    find = new Regex(string.Format("(value: {0}\\n)", oldHash.HashValue)),
                    replace = string.Format("value: {0}\n", newHash.HashValue)
                };
            }

            return regexes;
        }

        static private string GetHeader(RenamePair[] batch) {
            if (batch.Length == 1) {
                return string.Format("Renaming {0} to {1}", batch[0].src, batch[0].dest);
            } else {
                return "Renaming multiple ids";
            }
        }

        static private string EscapeRegex(string pattern) {
            string regex = Regex.Escape(pattern);
            if (pattern.Length > 0 && !char.IsLetterOrDigit(pattern[0]) && regex[0] == '\\') {
                regex = regex.Substring(1);
            }
            return regex;
        }

        static private void GatherAllFiles(string folder, RenamePair[] batch, string[] allowedExtensions, HashSet<string> filePaths, HashSet<RenamePair> renameFiles) {
            string header = GetHeader(batch);

            foreach(var filePath in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)) {
                EditorUtility.DisplayProgressBar(header, "Gathering Files: " + filePath, 0.1f);
                
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string extension = Path.GetExtension(filePath);
                bool isMeta = extension == ".meta";
                if (isMeta) {
                    extension = Path.GetExtension(fileName);
                    fileName = Path.GetFileNameWithoutExtension(fileName);
                }

                if (allowedExtensions != null) {
                    if (allowedExtensions.Length == 0) {
                        continue;
                    }

                    if (Array.IndexOf(allowedExtensions, extension) < 0) {
                        continue;
                    }
                }

                bool renaming = false;
                foreach(var rename in batch) {
                    if (fileName == rename.src) {
                        string newFilePath = string.Concat(Path.GetDirectoryName(filePath), "/", rename.dest, extension);
                        if (isMeta) {
                            newFilePath += ".meta";
                        }
                        renameFiles.Add(new RenamePair() {
                            src = filePath,
                            dest = newFilePath
                        });
                        filePaths.Add(newFilePath);
                        renaming = true;
                        break;
                    }
                }

                if (!renaming) {
                    filePaths.Add(filePath);
                }
            }
        }

        static private bool PerformRename(RenamePair renameFile) {
            if (string.IsNullOrEmpty(renameFile.src)) {
                return true;
            }

            if (File.Exists(renameFile.dest)) {
                Log.Error("Cannot perform rename - dest file already exists!");
                return false;
            }

            File.Move(renameFile.src, renameFile.dest);
            return true;
        }

        static private void ProcessFile(string filePath, RegexPair[] renames) {
            string fileContents = File.ReadAllText(filePath);
            if (fileContents.Length == 0) {
                return;
            }

            string extension = Path.GetExtension(filePath);
            if (extension == ".asset" && fileContents[0] != '%') {
                return;
            }

            foreach(var pair in renames) {
                fileContents = pair.find.Replace(fileContents, pair.replace);
            }
            File.WriteAllText(filePath, fileContents);
        }

        #endregion // Logic
    }
}