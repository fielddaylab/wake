using System;
using UnityEngine;
using BeauUtil;
using Aqua.Scripting;
using BeauRoutine;

namespace ProtoAqua.Observation
{
    public class ToolView : ScriptComponent
    {
        public enum PlacementMode
        {
            AwayFromPlayer,
            Fixed,
        }

        #region Inspector

        [Required] public Transform Root;
        [Space]
        [AutoEnum] public PlacementMode Placement = PlacementMode.AwayFromPlayer;
        [ShowIfField("IsAwayFromPlayer")] public float DistanceAway;

        #endregion // Inspector

        [NonSerialized] public Routine Animation;

        #if UNITY_EDITOR

        private bool IsAwayFromPlayer()
        {
            return Placement == PlacementMode.AwayFromPlayer;
        }

        #endif // UNITY_EDITOR
    }
}