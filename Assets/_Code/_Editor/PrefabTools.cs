using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
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
    static public class PrefabTools
    {
        [MenuItem("Aqualab/Clean Camera")]
        static public void CleanCameraData()
        {
            foreach(var obj in Selection.gameObjects)
            {
                var cameraData = obj.GetComponent<UniversalAdditionalCameraData>();
                if (cameraData != null)
                {
                    cameraData.hideFlags = HideFlags.HideAndDontSave;
                }
            }
        }

        [MenuItem("Aqualab/Refresh Databases")]
        static public void RefreshAllDBs()
        {
            DBObject.RefreshCollection<MapDesc, MapDB>();
            DBObject.RefreshCollection<BestiaryDesc, BestiaryDB>();
            DBObject.RefreshCollection<JobDesc, JobDB>();
            DBObject.RefreshCollection<ActDesc, ActDB>();
            DBObject.RefreshCollection<WaterPropertyDesc, WaterPropertyDB>();
            DBObject.RefreshCollection<ScriptCharacterDef, ScriptCharacterDB>();
            DBObject.RefreshCollection<InvItem, InventoryDB>();
        }
    }
}