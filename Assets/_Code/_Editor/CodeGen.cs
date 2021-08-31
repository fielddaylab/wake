using System.Collections;
using BeauUtil;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System;
using BeauUtil.Debugger;
using BeauUtil.Editor;

namespace Aqua.Editor
{
    static internal class CodeGen
    {
        static private readonly string TargetFolder = "Assets/_Code/_Generated/";

        [MenuItem("Aqualab/CodeGen/Regen Layers")]
        static private void GenerateLayerConsts()
        {
            StringBuilder builder = new StringBuilder(1024);
            builder.Append("static public class GameLayers")
                .Append("\n{");

            HashSet<string> usedNames = new HashSet<string>();

            for(int i = 0; i < 32; ++i)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    if (!usedNames.Add(layerName))
                    {
                        Log.Warn("[CodeGen] Duplicate Unity layer name '{0}'", layerName);
                        continue;
                    }

                    int index = i;
                    int mask = 1 << i;

                    string safeName = ObjectNames.NicifyVariableName(layerName).Replace("-", "_").Replace(" ", "");

                    if (usedNames.Count > 1)
                    {
                        builder.Append('\n');
                    }

                    builder.Append("\n\t// Layer ").Append(index).Append(": ").Append(layerName);
                    builder.Append("\n\tpublic const int ").Append(safeName).Append("_Index = ").Append(index).Append(";");
                    builder.Append("\n\tpublic const int ").Append(safeName).Append("_Mask = ").Append(mask).Append(";");
                }
            }

            builder.Append("\n}");

            string outputPath = Path.Combine(TargetFolder, "GameLayers.cs");
            File.WriteAllText(outputPath, builder.Flush());
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
        }

        [MenuItem("Aqualab/CodeGen/Regen Scenes")]
        static private void GenerateSceneConsts()
        {
            StringBuilder builder = new StringBuilder(1024);
            builder.Append("using BeauUtil;");
            builder.Append("\n\nstatic public class GameScenes")
                .Append("\n{");

            foreach(var scene in EditorBuildSettings.scenes)
            {
                SceneBinding binding = SceneHelper.FindSceneByPath(scene.path, SceneCategories.AllBuild);
                string safeName = ObjectNames.NicifyVariableName(binding.Name).Replace("-", "_").Replace(" ", "");

                builder.Append("\n\tstatic public readonly StringHash32 ").Append(safeName).Append(" = new StringHash32(0x").Append(binding.Id.HashValue.ToString("X8")).Append(");");
            }

            builder.Append("\n}");

            string outputPath = Path.Combine(TargetFolder, "GameScenes.cs");
            File.WriteAllText(outputPath, builder.Flush());
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
        }

        [MenuItem("Aqualab/CodeGen/Regen Sorting Layers")]
        static private void GenerateSortingLayerConsts()
        {
            StringBuilder builder = new StringBuilder(1024);
            builder.Append("using System;");
            builder.Append("\n\nstatic public class GameSortingLayers")
                .Append("\n{");

            SortingLayer[] allLayers = SortingLayer.layers;
            Array.Sort(allLayers, (a, b) => a.value.CompareTo(b.value));

            foreach(var sortingLayer in allLayers)
            {
                string layerName = sortingLayer.name;
                string safeName = ObjectNames.NicifyVariableName(layerName).Replace("-", "_").Replace(" ", "");

                builder.Append("\n\tpublic const int ").Append(safeName).Append(" = ").Append(sortingLayer.id).Append(";");
            }

            builder.Append("\n\n\tstatic public readonly int[] Order = new int[]\n\t{");
            foreach(var layer in allLayers)
                builder.Append("\n\t\t").Append(layer.id).Append(",");
            builder.Append("\n\t};");

            builder.Append("\n\n\tstatic public int IndexOf(int inSortingLayerId)")
                .Append("\n\t{")
                .Append("\n\t\treturn Array.IndexOf(Order, inSortingLayerId);")
                .Append("\n\t}");

            builder.Append("\n}");

            string outputPath = Path.Combine(TargetFolder, "GameSortingLayers.cs");
            File.WriteAllText(outputPath, builder.Flush());
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
        }

        [MenuItem("Aqualab/CodeGen/Regen Items")]
        static private void GenerateItemConsts()
        {
            StringBuilder builder = new StringBuilder(1024);
            builder.Append("using BeauUtil;");
            builder.Append("\n\nstatic public class ItemIds")
                .Append("\n{");

            foreach(var item in AssetDBUtils.FindAssets<InvItem>())
            {
                string safeName = ObjectNames.NicifyVariableName(item.name).Replace("-", "_").Replace(" ", "");

                builder.Append("\n\tstatic public readonly StringHash32 ").Append(safeName).Append(" = new StringHash32(0x").Append(item.Id().HashValue.ToString("X8")).Append(");");
            }

            builder.Append("\n}");

            string outputPath = Path.Combine(TargetFolder, "ItemIds.cs");
            File.WriteAllText(outputPath, builder.Flush());
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
        }
    }
}