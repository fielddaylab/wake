/*
 * Copyright (C) 2022. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    10 Jan 2022
 * 
 * File:    LeafExport.cs
 * Purpose: Leaf script localization export.
 */

using System;
using System.Collections.Generic;
using System.IO;
using BeauUtil;
using Leaf.Compiler;
using UnityEditor;
using UnityEngine;
using BeauUtil.Editor;
using System.Text;
using BeauUtil.Tags;

namespace Leaf.Editor
{
    /// <summary>
    /// Handles export of localizable strings from leaf files.
    /// </summary>
    static public class LeafExport
    {
        static private readonly string[] DefaultTagsContainingText = new string[]
        {
            "random", "rand", "speaker"
        };

        public struct LeafRule<TNode, TPackage>
            where TNode: LeafNode
            where TPackage : LeafNodePackage<TNode>
        {
            public LeafParser<TNode, TPackage> Parser;
            public IDelimiterRules Delimiters;
            public IEnumerable<string> TagsWithText;

            public LeafRule(LeafParser<TNode, TPackage> inParser, IDelimiterRules inDelimiters = null, IEnumerable<string> inTagsWithText = null)
            {
                Parser = inParser;
                Delimiters = inDelimiters ?? TagStringParser.CurlyBraceDelimiters;
                TagsWithText = inTagsWithText;
            }
        }

        public struct CustomRule
        {
            public Type AssetType;
            public CustomStringGatherer GatherStrings;

            public CustomRule(Type inAssetType, CustomStringGatherer inGatherDelegate)
            {
                AssetType = inAssetType;
                GatherStrings = inGatherDelegate;
            }
        }

        public delegate IEnumerable<KeyValuePair<StringHash32, string>> CustomStringGatherer(ScriptableObject inAsset);

        static public void StringsAsCSV<TNode, TPackage>(string inDirectory, string inExportFilePath, string inLanguageName, LeafParser<TNode, TPackage> inParser, params CustomRule[] inCustomRules)
            where TNode: LeafNode
            where TPackage : LeafNodePackage<TNode>
        {
            StringsAsCSV<TNode, TPackage>(inDirectory, inExportFilePath, inLanguageName, new LeafRule<TNode, TPackage>(inParser), inCustomRules);
        }

        static public void StringsAsCSV<TNode, TPackage>(string inDirectory, string inExportFilePath, string inLanguageName, LeafRule<TNode, TPackage> inLeafRule, params CustomRule[] inCustomRules)
            where TNode: LeafNode
            where TPackage : LeafNodePackage<TNode>
        {
            inCustomRules = inCustomRules ?? Array.Empty<CustomRule>();
            List<LeafAsset> allLeafAssets = new List<LeafAsset>();
            List<ScriptableObject>[] allCustomObjects = new List<ScriptableObject>[inCustomRules.Length];

            for(int i = 0; i < allCustomObjects.Length; i++)
            {
                allCustomObjects[i] = new List<ScriptableObject>(16);
            }

            var allScriptableAssets = AssetDBUtils.FindAssets<ScriptableObject>(null, new string[] { inDirectory });

            HashSet<string> textTags = new HashSet<string>(DefaultTagsContainingText);
            if (inLeafRule.TagsWithText != null)
            {
                foreach(var tag in inLeafRule.TagsWithText)
                    textTags.Add(tag);
            }

            foreach(var obj in allScriptableAssets)
            {
                LeafAsset leaf = obj as LeafAsset;
                if (leaf != null)
                {
                    if (leaf.name.Contains(".template")) {
                        continue;
                    }

                    allLeafAssets.Add(leaf);
                }
                else
                {
                    for(int i = 0; i < inCustomRules.Length; i++)
                    {
                        if (inCustomRules[i].AssetType.IsAssignableFrom(obj.GetType()))
                        {
                            allCustomObjects[i].Add(obj);
                            break;
                        }
                    }
                }
            }

            StringBuilder sb = new StringBuilder(1024);
            sb.Append("Line Name, Code, ").Append(inLanguageName);

            foreach(var leaf in allLeafAssets)
            {
                TPackage package = LeafAsset.Compile(leaf, inLeafRule.Parser);
                if (package == null)
                    continue;

                foreach(var line in package.AllLines())
                {
                    if (TagStringParser.ContainsText(line.Value, inLeafRule.Delimiters, textTags))
                    {
                        string sourceKey = line.Key.ToDebugString();
                        string smallKey = line.Key.ToString();

                        sb.Append('\n').Append(sourceKey).Append(", ").Append(smallKey).Append(", \"");
                        StringUtils.Escape(line.Value, sb, StringUtils.CSV.Escaper.Instance);
                        sb.Append('\"');
                    }
                }
            }

            for(int i = 0; i < inCustomRules.Length; i++)
            {
                foreach(var file in allCustomObjects[i])
                {
                    foreach(var line in inCustomRules[i].GatherStrings(file))
                    {
                        if (TagStringParser.ContainsText(line.Value, inLeafRule.Delimiters, textTags))
                        {
                            string sourceKey = line.Key.ToDebugString();
                            string smallKey = line.Key.ToString();

                            sb.Append('\n').Append(sourceKey).Append(", ").Append(smallKey).Append(", \"");
                            StringUtils.Escape(line.Value, sb, StringUtils.CSV.Escaper.Instance);
                            sb.Append('\"');
                        }
                    }
                }
            }

            File.WriteAllText(inExportFilePath, sb.Flush());
            EditorUtility.RevealInFinder(inExportFilePath);
        }
    }
}