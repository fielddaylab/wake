using System.Collections;
using BeauUtil;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using System.Collections.Generic;
using BeauUtil.Editor;

namespace ProtoAqua.Editor
{
    static public class BuildSettings
    {
        [InitializeOnLoadMethod]
        static private void Setup()
        {
            BuildInfoGenerator.Enabled = true;
            BuildInfoGenerator.IdLength = 8;
        }
    }
}