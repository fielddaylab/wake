using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using BeauUtil;
using Aqua.Profile;
using UnityEngine.Serialization;

namespace Aqua {
    public class PlayerLevelIcon : MonoBehaviour, ISceneLoadHandler {
        #region Inspector

        [SerializeField] private Image m_LevelIcon = null;

        #endregion // Inspector

        private readonly Action OnScienceLevelUpdated;

        private PlayerLevelIcon() {
            OnScienceLevelUpdated = Refresh;
        }

        private void OnEnable() {
            Services.Events.Register(GameEvents.ScienceLevelUpdated, OnScienceLevelUpdated);
            if (!Script.IsLoading) {
                Refresh();
            }
        }

        private void OnDisable() {
            Services.Events?.Deregister(GameEvents.ScienceLevelUpdated, OnScienceLevelUpdated);
        }

        private void Refresh() {
            m_LevelIcon.sprite = Services.Tweaks.Get<ScienceTweaks>().LevelIcon((int) Save.ExpLevel);
        }

        public void OnSceneLoad(SceneBinding inScene, object inContext) {
            if (isActiveAndEnabled) {
                Refresh();
            }
        }
    }
}