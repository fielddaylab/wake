using UnityEngine;
using Aqua;
using UnityEngine.UI;

namespace ProtoAqua.Observation
{
    public class ToolButton : MonoBehaviour
    {
        #region Inspector

        public Image Icon;
        public Toggle Toggle;
        public CursorInteractionHint CursorHint;
        public KeyboardShortcut KeyboardShortcut;

        #endregion // Inspector
    }
}