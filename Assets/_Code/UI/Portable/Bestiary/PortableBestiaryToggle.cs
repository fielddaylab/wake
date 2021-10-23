using UnityEngine;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using BeauPools;
using System;
using Aqua;

namespace Aqua.Portable
{
    public class PortableBestiaryToggle : MonoBehaviour, IPoolAllocHandler
    {
        [Serializable] public class Pool : SerializablePool<PortableBestiaryToggle> { }
        public delegate void ToggleDelegate(PortableBestiaryToggle toggle, bool state);

        #region Inspector

        public Toggle Toggle;
        [Required] public CursorInteractionHint Cursor;

        public Image Icon;
        public LocText Text;

        #endregion // Inspector

        public object Data;
        public ToggleDelegate Callback;

        private void Awake()
        {
            Toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        private void OnToggleValueChanged(bool inbValue)
        {
            Callback?.Invoke(this, inbValue);
        }

        void IPoolAllocHandler.OnAlloc()
        {
        }

        void IPoolAllocHandler.OnFree()
        {
            Toggle.group = null;
            Toggle.SetIsOnWithoutNotify(false);
            Data = null;
            Callback = null;
        }
    }
}