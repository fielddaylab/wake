using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using System.IO;
using BeauUtil.Editor;
using Leaf;
using Aqua.Scripting;
using BeauUtil;
using System.Text;
using BeauUtil.Blocks;
using BeauUtil.Tags;
using BeauUtil.IO;

namespace Aqua.Editor
{
    static public class ScriptNodeTools
    {
        [MenuItem("Aqualab/Export String Table")]
        static public void ExportAllStrings()
        {
            StringBuilder sb = new StringBuilder(1024);
            sb.Append("Line Name, Code, English");
            foreach(var asset in AssetDBUtils.FindAssets<LeafAsset>())
            {
                ScriptNodePackage package = BlockParser.Parse(asset.name, asset.Source(), Parsing.Block, ScriptNodePackage.Generator.Instance);
                foreach(var line in package.AllLines())
                {
                    if (TagStringParser.ContainsText(line.Value, Parsing.InlineEvent))
                    {
                        string sourceKey = line.Key.ToDebugString();
                        string smallKey = line.Key.ToString();

                        sb.Append('\n').Append(sourceKey).Append(", ").Append(smallKey).Append(", \"");
                        StringUtils.Escape(line.Value, sb, StringUtils.CSV.Escaper.Instance);
                        sb.Append("\"");
                    }
                }
            }

            foreach(var asset in AssetDBUtils.FindAssets<TextAsset>())
            {
                if (asset.GetType() != typeof(TextAsset))
                    continue;
                
                string path = AssetDatabase.GetAssetPath(asset);
                if (path.EndsWith(".aqloc"))
                {
                    LocPackage package = BlockParser.Parse(asset.name, asset.text, Parsing.Block, LocPackage.Generator.Instance);
                    foreach(var node in package)
                    {
                        if (TagStringParser.ContainsText(node.Content(), Parsing.InlineEvent))
                        {
                            string sourceKey = node.Id().ToDebugString();
                            string smallKey = node.Id().ToString();

                            sb.Append('\n').Append(sourceKey).Append(", ").Append(smallKey).Append(", \"");
                            StringUtils.Escape(node.Content(), sb, StringUtils.CSV.Escaper.Instance);
                            sb.Append("\"");
                        }
                    }
                }
            }

            File.WriteAllText("ContentExport.csv", sb.Flush());
            EditorUtility.RevealInFinder("ContentExport.csv");
        }
    }
}