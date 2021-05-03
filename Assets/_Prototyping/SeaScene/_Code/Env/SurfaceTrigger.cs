using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using UnityEngine.SceneManagement;
using Aqua;
using BeauUtil;
using System;

namespace ProtoAqua.Observation
{
    public class SurfaceTrigger : MonoBehaviour
    {
        #region Inspector

        [Header("Components")]
        [SerializeField, Required] private Collider2D m_Collider = null;

        #endregion // Inspector

        private void Awake()
        {
            var listener = m_Collider.EnsureComponent<TriggerListener2D>();
            listener.TagFilter.Add("Player");
            listener.onTriggerEnter.AddListener(OnPlayerEnter);
            listener.onTriggerExit.AddListener(OnPlayerExit);
        }

        private void OnPlayerEnter(Collider2D other)
        {
            Services.UI.FindPanel<SurfaceButton>().Display(other.transform);
        }

        private void OnPlayerExit(Collider2D other)
        {
            Services.UI?.FindPanel<SurfaceButton>()?.Hide();
        }
    }

}
