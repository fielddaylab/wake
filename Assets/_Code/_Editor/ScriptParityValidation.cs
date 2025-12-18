using Aqua.Scripting;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using Leaf;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using static Leaf.Editor.LeafExport;

namespace Aqua.Editor {
    static public class ScriptParityValidation {
        private struct LineInfo {
            public StringHash32 CharacterId;
            public StringHash32 PoseId;
            public string Text;

            static public LineInfo FromString(StringSlice data) {
                LineInfo info = default;
                info.Text = data.ToString();

                int openIdx = data.IndexOf("{@");
                if (openIdx >= 0) {
                    openIdx += 2;
                    int closeIdx = data.IndexOf("}", openIdx);
                    if (closeIdx >= 0) {
                        TagData tagData = TagData.Parse(data.Substring(openIdx, closeIdx - openIdx), Parsing.InlineEvent);
                        info.CharacterId = tagData.Id.Hash32();
                        if (tagData.Data.StartsWith('#')) {
                            info.PoseId = tagData.Data.Substring(1).Hash32();
                        }
                    }
                }

                return info;
            }
        }

        private static readonly string[] DefaultTextReplaceTags = new string[] { "random", "rand", "speaker" };

        static private Dictionary<StringHash32, LineInfo> BuildMasterDB(LocManifest manifest, LeafAsset[] scripts, IHasLocalizationKeys[] locKeys) {
            Dictionary<StringHash32, LineInfo> lineInfo = new Dictionary<StringHash32, LineInfo>(2048);

            LocPackage fullPackage = null;
            try {
                fullPackage = LocPackage.CombineAll(manifest.Packages);
                foreach (var key in fullPackage.AllKeys) {
                    fullPackage.TryGetContent(key, out var lineText);
                    if (TagStringParser.ContainsText(lineText, Parsing.InlineEvent, DefaultTextReplaceTags)) {
                        lineInfo.Add(key, LineInfo.FromString(lineText));
                    }
                }

                foreach (var leafAsset in scripts) {
                    ScriptNodePackage leafPackage = LeafAsset.Compile<ScriptNode, ScriptNodePackage>(leafAsset, ScriptNodePackage.Generator.Instance);
                    foreach (var line in leafPackage.AllLines()) {
                        if (TagStringParser.ContainsText(line.Value, Parsing.InlineEvent, DefaultTextReplaceTags)) {
                            lineInfo.Add(line.Key, LineInfo.FromString(line.Value));
                        }
                    }
                }
                foreach (var asset in locKeys) {
                    foreach (var kv in asset.GetStrings()) {
                        if (TagStringParser.ContainsText(kv.Value, Parsing.InlineEvent, DefaultTextReplaceTags)) {
                            lineInfo.Add(kv.Key, LineInfo.FromString(kv.Value));
                        }
                    }
                }

                return lineInfo;
            } finally {
                if (fullPackage) {
                    GameObject.DestroyImmediate(fullPackage);
                }
            }
        }

        static private Dictionary<StringHash32, LineInfo> BuildMasterDB() {
            var englishManifest = ValidationUtils.FindAsset<LocManifest>("EnglishLanguage");
            LeafAsset[] leafAssets = ValidationUtils.FindAllAssets<LeafAsset>();
            List<LeafAsset> leafAssetsList = new List<LeafAsset>(leafAssets);
            for(int i = leafAssetsList.Count; i-- > 0;) {
                if (leafAssetsList[i].name.Contains(".template")) {
                    leafAssetsList.FastRemoveAt(i);
                }
            }
            leafAssets = leafAssetsList.ToArray();

            ScriptableObject[] allScriptableObjects = ValidationUtils.FindAllAssets<ScriptableObject>();
            List<IHasLocalizationKeys> locKeysList = new List<IHasLocalizationKeys>();
            foreach(var obj in allScriptableObjects) {
                if (obj is IHasLocalizationKeys) {
                    locKeysList.Add((IHasLocalizationKeys)obj);
                }
            }
            IHasLocalizationKeys[] locKeys = locKeysList.ToArray();

            return BuildMasterDB(englishManifest, leafAssets, locKeys);
        }

        [MenuItem("Aqualab/Localization/Import SpanishLanguage from LocCompare", priority = 202)]
        static public void ImportSpanishLanguageFromLocCompare() {
            ImportLocalizationFromLocCompare("LocCompare_Import.txt", "Assets/_Content/Text/ES/ES-Loc.aqloc", "SpanishLanguage");
        }

        [MenuItem("Aqualab/Localization/Export Line Valildation DB", priority = 200)]
        static public void ExportMasterDB() {
            var masterDB = BuildMasterDB();
            ExportMasterDB(masterDB, "LineData.csv");
        }

        [MenuItem("Aqualab/Localization/Check for Line Inconsistencies", priority = 201)]
        static public void CheckForInconsistencies() {
            var masterDB = BuildMasterDB();
            using (StreamWriter writer = new StreamWriter("LocIssues.txt")) {
                using (StreamWriter compare = new StreamWriter("LocCompare.txt")) {
                    int errorCount = 0;
                    int missingLineCount = 0;

                    foreach (var manifest in ValidationUtils.FindAllAssets<LocManifest>()) {
                        if (manifest.LanguageId == LocService.DefaultLanguage) {
                            continue;
                        }

                        LocPackage tempPkg = null;
                        try {
                            tempPkg = LocPackage.CombineAll(manifest.Packages);
                            foreach (var kv in masterDB) {
                                if (!tempPkg.TryGetContent(kv.Key, out string data)) {
                                    errorCount++;
                                    missingLineCount++;
                                    writer.Write("Language ");
                                    writer.Write(manifest.name);
                                    writer.Write(" line '");
                                    writer.Write(kv.Key.ToDebugString());
                                    writer.Write("' is missing [!!]\n");

                                    compare.Write("Line: ");
                                    compare.Write(kv.Key.ToDebugString());
                                    compare.Write("\n\nOriginal:\n");
                                    compare.Write(kv.Value.Text);
                                    compare.Write("\n\n");
                                    compare.Write(manifest.name);
                                    compare.Write(":\n");
                                    compare.Write("");
                                    compare.Write("\n\n--------\n");
                                } else {
                                    LineInfo parsedLineInfo = LineInfo.FromString(data);
                                    LineInfo comparedLineInfo = kv.Value;
                                    if (parsedLineInfo.CharacterId != comparedLineInfo.CharacterId) {
                                        errorCount++;
                                        writer.Write("Language ");
                                        writer.Write(manifest.name);
                                        writer.Write(" line '");
                                        writer.Write(kv.Key.ToDebugString());
                                        writer.Write("' has incorrect character id (expected '");
                                        writer.Write(comparedLineInfo.CharacterId.ToDebugString());
                                        writer.Write("', has '");
                                        writer.Write(parsedLineInfo.CharacterId.ToDebugString());
                                        writer.Write("')\n");
                                    }
                                    if (parsedLineInfo.PoseId != comparedLineInfo.PoseId) {
                                        errorCount++;
                                        writer.Write("Language ");
                                        writer.Write(manifest.name);
                                        writer.Write(" line '");
                                        writer.Write(kv.Key.ToDebugString());
                                        writer.Write("' has incorrect pose id (expected '");
                                        writer.Write(comparedLineInfo.PoseId.ToDebugString());
                                        writer.Write("', has '");
                                        writer.Write(parsedLineInfo.PoseId.ToDebugString());
                                        writer.Write("')\n");
                                    }

                                    compare.Write("Line: ");
                                    compare.Write(kv.Key.ToDebugString());
                                    compare.Write("\n\nOriginal:\n");
                                    compare.Write(kv.Value.Text);
                                    compare.Write("\n\n");
                                    compare.Write(manifest.name);
                                    compare.Write(":\n");
                                    compare.Write(data);
                                    compare.Write("\n\n--------\n");
                                }
                            }
                            foreach (var key in tempPkg.AllKeys) {
                                if (!masterDB.ContainsKey(key)) {
                                    errorCount++;
                                    writer.Write("Language ");
                                    writer.Write(manifest.name);
                                    writer.Write(" line '");
                                    writer.Write(key.ToDebugString());
                                    writer.Write("' has no english counterpart [??]\n");

                                    compare.Write("Line: ");
                                    compare.Write(key.ToDebugString());
                                    compare.Write("\n\nOriginal:\n");
                                    compare.Write("");
                                    compare.Write("\n\n");
                                    compare.Write(manifest.name);
                                    compare.Write(":\n");
                                    tempPkg.TryGetContent(key, out string content);
                                    compare.Write(content);
                                    compare.Write("\n\n--------\n");
                                }
                            }
                        } finally {
                            if (tempPkg) {
                                GameObject.DestroyImmediate(tempPkg);
                            }
                        }
                    }

                    if (missingLineCount > 0) {
                        Log.Error("Found {0} missing lines! (total errors {1})", missingLineCount, errorCount);
                    } else if (errorCount > 0) {
                        Log.Error("Found {0} errors!", errorCount);
                    } else {
                        Log.Msg("No errors found! Hell yeah!");
                    }
                }
            }
        }

        static private void ImportLocalizationFromLocCompare(string inputFile, string outputFile, string localizationName) {
            using (StreamWriter writer = new StreamWriter(outputFile)) {
                using (StreamReader reader = new StreamReader(inputFile)) {
                    int state = 0;
                    localizationName = localizationName + ":";
                    string lineKey = null;
                    StringBuilder lineData = new StringBuilder(2048);
                    while (!reader.EndOfStream) {
                        string line = reader.ReadLine();
                        if (line == "--------") {
                            if (state == 2) {
                                lineData.TrimEnd(StringUtils.DefaultNewLineChars);
                                writer.Write(":: ");
                                writer.Write(lineKey.Replace("|", "__").Replace(":", "_c_"));
                                writer.Write('\n');
                                writer.Write(lineData.ToString());
                                writer.Write("\n\n");
                            }
                            state = 0;
                            lineData.Clear();
                            lineKey = null;
                        } else if (line.StartsWith("Line: ")) {
                            if (state == 0) {
                                lineKey = line.Substring(6).Trim();
                                state = 1;
                                lineData.Clear();
                            }
                        } else if (line == localizationName) {
                            if (state == 1) {
                                state = 2;
                            }
                        } else if (state == 2) {
                            lineData.Append(line).Append('\n');
                        }
                    }
                }
            }
        }

        static private void ExportMasterDB(Dictionary<StringHash32, LineInfo> db, string filePath) {
            using (StreamWriter writer = new StreamWriter(filePath)) {
                foreach(var kv in db) {
                    writer.Write(kv.Key.ToDebugString());
                    writer.Write(", ");
                    writer.Write(kv.Value.CharacterId.ToDebugString());
                    writer.Write(", ");
                    writer.Write(kv.Value.PoseId.ToDebugString());
                    writer.Write(", ");
                    string escaped = StringUtils.Escape(kv.Value.Text, StringUtils.CSV.Escaper.Instance);
                    writer.Write('"');
                    writer.Write(escaped);
                    writer.Write("\"\n");
                }
            }
        }
    }
}