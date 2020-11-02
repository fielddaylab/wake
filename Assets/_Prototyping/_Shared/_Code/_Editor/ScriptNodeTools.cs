using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using System.IO;
using BeauUtil.Editor;
using Leaf;
using ProtoAqua.Scripting;
using BeauUtil;
using System.Text;
using BeauUtil.Blocks;
using BeauUtil.Tags;

namespace ProtoAqua.Editor
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

            File.WriteAllText("ContentExport.csv", sb.Flush());
        }
    }
}