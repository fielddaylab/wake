using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.UI;
using TMPro;
using BeauPools;
using System;
using Aqua.Profile;
using Aqua.Compression;
using System.Collections;
using EasyAssetStreaming;
using System.Text;

namespace Aqua.Portable
{
    public class LevelApp : PortableMenuApp
    {
        #region Inspector

        [SerializeField, Required] private LayoutDecompressor m_Decompressor = null;
        [SerializeField, Required] private GameObject m_CardRoot = null;
        [SerializeField, Required] private RectGraphic m_CardBlocker = null;
        [SerializeField, Required] private TMP_Text m_PlaytimeCounter = null;

        #endregion

        [NonSerialized] private List<IStreamingComponent> m_StreamingComponents = new List<IStreamingComponent>(4);
        private Routine m_WaitRoutine;
        private ulong m_LastSeconds;
        private readonly StringBuilder m_Builder = new StringBuilder(8);

        private void LateUpdate() {
            UpdatePlaytime();
        }

        private void UpdatePlaytime() {
            ulong lastSeconds = (ulong) Save.Current.Playtime;
            if (m_LastSeconds != lastSeconds) {
                m_LastSeconds = lastSeconds;
                uint hours = (uint) (m_LastSeconds / 3600);
                uint minutes = (uint) (m_LastSeconds / 60) % 60;
                uint seconds = (uint) (m_LastSeconds % 60);
                m_Builder.Length = 0;
                m_Builder.AppendNoAlloc(hours).Append(':').AppendNoAlloc(minutes, 2).Append(':').AppendNoAlloc(seconds, 2);
                m_PlaytimeCounter.SetText(m_Builder);
            }
        }

        #region Panel

        protected override void OnShowComplete(bool inbInstant)
        {
            base.OnShowComplete(inbInstant);
            LoadCard();
            UpdatePlaytime();
        }

        protected override void OnHide(bool inbInstant)
        {
            m_StreamingComponents.Clear();
            m_Decompressor.ClearAll();
            m_WaitRoutine.Stop();
            m_LastSeconds = 0;
            base.OnHide(inbInstant);
        }

        #endregion // Panel

        #region Page Display

        private void LoadCard()
        {
            GameObject spawned = m_Decompressor.Decompress(Services.UI.CompressedLayouts, Services.Tweaks.Get<ScienceTweaks>().LevelBadgeLayout((int) Save.ExpLevel), m_CardRoot);
            spawned.gameObject.SetActive(true);
            m_CardBlocker.gameObject.SetActive(true);
            m_CardBlocker.SetAlpha(1);
            spawned.GetComponentsInChildren<IStreamingComponent>(false, m_StreamingComponents);
            m_WaitRoutine.Replace(this, WaitToShow());
        }

        private IEnumerator WaitToShow() {
            bool isLoading = true;
            while(isLoading) {
                yield return null;
                isLoading = false;
                foreach(var component in m_StreamingComponents) {
                    if (component.IsLoading()) {
                        isLoading = true;
                        break;
                    }
                }
            }

            yield return m_CardBlocker.FadeTo(0, 0.2f);
            m_CardBlocker.gameObject.SetActive(false);
        }

        #endregion // Page Display
    }
}