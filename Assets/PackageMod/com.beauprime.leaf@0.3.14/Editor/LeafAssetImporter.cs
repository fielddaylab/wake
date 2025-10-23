/*
 * Copyright (C) 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    25 Oct 2020
 * 
 * File:    LeafAssetImporter.cs
 * Purpose: Leaf script asset importer.
 */

using System.IO;
using UnityEditor;
using UnityEngine;

#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif // UNITY_2020_2_OR_NEWER

namespace Leaf.Editor
{
    [ScriptedImporter(1, "leaf")]
    public class LeafAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            LeafAsset asset = ScriptableObject.CreateInstance<LeafAsset>();
            
            byte[] sourceBytes = File.ReadAllBytes(ctx.assetPath);
            asset.Create(sourceBytes);

            EditorUtility.SetDirty(asset);

            ctx.AddObjectToAsset("txt", asset);
            ctx.SetMainObject(asset);
        }
    }
}