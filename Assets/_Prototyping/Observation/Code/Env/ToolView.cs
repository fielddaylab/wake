using System;
using UnityEngine;
using BeauUtil;
using Aqua.Scripting;

namespace ProtoAqua.Observation
{
    public class ToolView : ScriptComponent
    {
        #region Inspector

        [Required] public Transform Root;

        #endregion // Inspector
    }
}