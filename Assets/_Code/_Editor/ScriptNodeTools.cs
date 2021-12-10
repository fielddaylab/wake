using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using System.IO;
using BeauUtil.Editor;
using Leaf;
using Leaf.Runtime;
using Aqua.Scripting;
using BeauUtil;
using System.Text;
using BeauUtil.Blocks;
using BeauUtil.Tags;
using BeauUtil.IO;
using System.Reflection;
using BeauUtil.Variants;

namespace Aqua.Editor
{
    static public class ScriptNodeTools
    {
        [MenuItem("Aqualab/Localization/Export String Table")]
        static public void ExportAllStrings()
        {
            StringBuilder sb = new StringBuilder(1024);
            sb.Append("Line Name, Code, English");
            foreach(var asset in AssetDBUtils.FindAssets<LeafAsset>())
            {
                ScriptNodePackage package = LeafAsset.Compile(asset, ScriptNodePackage.Generator.Instance);
                if (package == null)
                    continue;
                
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

            File.WriteAllText("LeafExport.csv", sb.Flush());

            foreach(var asset in AssetDBUtils.FindAssets<LocPackage>())
            {
                asset.Parse(LocPackage.Generator.Instance);
                foreach(var node in asset)
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

            File.WriteAllText("LocExport.csv", sb.Flush());
            EditorUtility.RevealInFinder("LocExport.csv");
        }
    
        [MenuItem("Aqualab/Leaf/Export Leaf Function Outline")]
        static public void ExportLeafOutline()
        {
            StringBuilder sb = new StringBuilder(1024);
            List<AttributeBinding<LeafMember, MethodInfo>> conditions = new List<AttributeBinding<LeafMember, MethodInfo>>(64);
            List<AttributeBinding<LeafMember, MethodInfo>> actions = new List<AttributeBinding<LeafMember, MethodInfo>>(64);
            foreach(var pair in Reflect.FindAllMethods<LeafMember>(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                pair.Attribute.AssignId(pair.Info);
                if (IsConditionReturnType(pair.Info.ReturnType))
                    conditions.Add(pair);
                else
                    actions.Add(pair);
            }

            sb.Append("-- CONDITIONS --");
            foreach(var condition in conditions) {
                sb.Append('\n').Append(condition.Info.ReturnType.Name).Append(' ').Append(condition.Attribute.Name).Append('(');
                int propCount = 0;
                foreach(var property in condition.Info.GetParameters())
                {
                    if (propCount++ > 0)
                        sb.Append(", ");
                    sb.Append(property.ParameterType.Name).Append(' ').Append(property.Name);
                    if (property.HasDefaultValue)
                        sb.Append(" = ").Append(property.DefaultValue?.ToString() ?? "null");
                }
                sb.Append(')');
            }

            sb.Append("\n\n-- ACTIONS --");
            foreach(var action in actions) {
                sb.Append('\n');
                if (!action.Info.IsStatic) {
                    sb.Append('[').Append(action.Info.DeclaringType.Name).Append("] ");
                }
                
                sb.Append(action.Info.ReturnType.Name).Append(' ').Append(action.Attribute.Name).Append('(');
                int propCount = 0;
                foreach(var property in action.Info.GetParameters())
                {
                    if (propCount++ > 0)
                        sb.Append(", ");
                    sb.Append(property.ParameterType.Name).Append(' ').Append(property.Name);
                    if (property.HasDefaultValue)
                        sb.Append(" = ").Append(property.DefaultValue?.ToString() ?? "null");
                }
                sb.Append(')');
            }
            File.WriteAllText("LeafMethods.txt", sb.Flush());
            EditorUtility.RevealInFinder("LeafMethods.txt");
        }

        static private bool IsConditionReturnType(Type type) {
            if (type == typeof(Variant) || type == typeof(SerializedHash32) || type == typeof(StringHash32)) {
                return true;
            }

            TypeCode code = System.Type.GetTypeCode(type);

            switch(code)
            {
                case TypeCode.String:
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                    return true;

                default:
                    return false;
            }
        }
    }
}