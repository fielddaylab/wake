using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using BeauPools;
using System;
using Aqua.Profile;
using Aqua.Compression;

namespace Aqua.Portable
{
    public class LevelApp : PortableMenuApp
    {
        #region Inspector

        [SerializeField, Required] private LayoutDecompressor m_Decompressor = null;
        [SerializeField, Required] private GameObject m_CardRoot = null;
        [SerializeField, Required] private CanvasGroup m_CardGroup = null;

        #endregion

        #region Panel

        protected override void OnShowComplete(bool inbInstant)
        {
            base.OnShowComplete(inbInstant);
            LoadData();
        }

        protected override void OnHide(bool inbInstant)
        {
            m_Decompressor.ClearAll();
            base.OnHide(inbInstant);
        }

        #endregion // Panel

        #region Page Display

        private void LoadData()
        {
            GameObject spawned = m_Decompressor.Decompress(Services.UI.CompressedLayouts, Services.Tweaks.Get<ScienceTweaks>().LevelBadgeLayout((int) Save.ExpLevel), m_CardRoot);
            spawned.gameObject.SetActive(true);
        }

        #endregion // Page Display
    }
}