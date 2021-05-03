using System;
using System.Collections.Generic;
using Aqua;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Argumentation
{
    public class LinkManager : MonoBehaviour
    {
        [Serializable]
        public class OptionPool : SerializablePool<ArgueOptionButton> { }

        [SerializeField] private OptionPool m_Pool = null;
        [SerializeField] private CanvasGroup m_BestiaryGroup = null;
        [SerializeField] private CanvasGroup m_ClaimGroup = null;
        [SerializeField] private LayoutGroup m_ClaimLayout = null;

        private void Start()
        {
            m_Pool.Initialize();
            
            m_BestiaryGroup.gameObject.SetActive(false);
            m_ClaimGroup.gameObject.SetActive(false);
        }

        public void DisplayClaims(IEnumerable<Link> inLinks)
        {
            m_ClaimGroup.gameObject.SetActive(true);
            m_BestiaryGroup.gameObject.SetActive(false);

            m_Pool.Reset();

            ArgueOptionButton button;
            foreach(var link in inLinks)
            {
                button = m_Pool.Alloc();
                button.Populate(link.ShortenedText ?? link.DisplayText, link.Id);
            }

            m_ClaimLayout.ForceRebuild();
        }

        public void DisplayBestiary()
        {
            m_ClaimGroup.gameObject.SetActive(false);
            m_BestiaryGroup.gameObject.SetActive(true);
        }

        public void Hide()
        {
            m_BestiaryGroup.gameObject.SetActive(false);
            m_ClaimGroup.gameObject.SetActive(false);
        }
    }
}
