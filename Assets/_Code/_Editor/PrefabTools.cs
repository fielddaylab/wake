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
    static public class PrefabTools
    {
        [MenuItem("Edit/Revert Prefab")]
        static public void RevertPrefab()
        {
            foreach(var obj in Selection.gameObjects)
            {
                PrefabUtility.RevertPrefabInstance(obj, InteractionMode.UserAction);
            }
        }
    }
}