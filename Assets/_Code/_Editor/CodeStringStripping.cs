using System;
using System.IO;
using System.Text.RegularExpressions;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Editor;
using UnityEditor;

namespace Aqua.Editor {
    static public class CodeStringStripping {
        static private Regex HashFormat = new Regex("(static (?:public|private) readonly (?:StringHash32|TextId) (?:\\S+) = )\"(.*)\";");
        static private MatchEvaluator HashConstReplace = (m) => {
            string hashString = m.Groups[2].Value;
            string prelude = m.Groups[1].Value;
            string hashValue = new StringHash32(hashString).HashValue.ToString("X8");
            return string.Format("{0}new BeauUtil.StringHash32(0x{1});", prelude, hashValue);
        };

        static private Regex TableKeyPairFormat = new Regex("(static (?:public|private) readonly TableKeyPair (?:\\S+) = )TableKeyPair\\.Parse\\(\"(.*):(.*)\"\\);");
        static private MatchEvaluator TableKeyPairConstReplace = (m) => {
            string tableKeyString = m.Groups[2].Value;
            string valueKeyString = m.Groups[3].Value;
            string prelude = m.Groups[1].Value;
            string tableHashValue = new StringHash32(tableKeyString).HashValue.ToString("X8");
            string valueHashValue = new StringHash32(valueKeyString).HashValue.ToString("X8");
            return string.Format("{0}new BeauUtil.Variants.TableKeyPair(new StringHash32(0x{1}), new StringHash32(0x{2}));", prelude, tableHashValue, valueHashValue);
        };

        static private Regex PostAudioConstFormat = new Regex("(Services\\.Audio\\.PostEvent\\()\"(\\S+)\"");
        static private MatchEvaluator PostAudioConstReplace = (m) => {
            string hashString = m.Groups[2].Value;
            string prelude = m.Groups[1].Value;
            string hashValue = new StringHash32(hashString).HashValue.ToString("X8");
            return string.Format("{0}new BeauUtil.StringHash32(0x{1})", prelude, hashValue);
        };


        static public string ProcessFileText(string fileText)
        {
            fileText = HashFormat.Replace(fileText, HashConstReplace);
            fileText = TableKeyPairFormat.Replace(fileText, TableKeyPairConstReplace);
            return PostAudioConstFormat.Replace(fileText, PostAudioConstReplace);
        }

        static public void ProcessAllFiles(bool forceRecompile)
        {
            try {
                foreach(var filePath in Directory.EnumerateFiles("Assets/", "*.cs", SearchOption.AllDirectories)) {
                    string fileContents = File.ReadAllText(filePath);
                    string processedContents = ProcessFileText(fileContents);
                    EditorUtility.DisplayProgressBar("Stripping String Hash Constants", Path.GetFileName(filePath), 0);
                    if (fileContents != processedContents) {
                        File.WriteAllText(filePath, processedContents);
                        Log.Msg("[CodeStringStripping] Stripped hash constants from '{0}'", filePath);
                    }
                }

                if (forceRecompile) {
                    EditorUtility.DisplayProgressBar("Stripping String Hash Constants", "Recompiling...", 0.5f);
                    BuildUtils.ForceRecompile();
                }
            } finally {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Aqualab/DEBUG/Strip Hash Constant String")]
        static public void DEBUGProcessAllFiles()
        {
            ProcessAllFiles(true);
        }
    }
}