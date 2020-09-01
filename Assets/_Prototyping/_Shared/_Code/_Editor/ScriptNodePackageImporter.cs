using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using System.IO;

namespace ProtoAqua.Editor
{
    [ScriptedImporter(1, "aqscene")]
    public class ScriptNodePackageImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            TextAsset txtAsset = new TextAsset(File.ReadAllText(ctx.assetPath));
            ctx.AddObjectToAsset("Main Object", txtAsset);
            ctx.SetMainObject(txtAsset);
        }
    }
}