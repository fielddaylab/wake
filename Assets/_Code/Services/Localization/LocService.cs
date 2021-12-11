using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Debugging;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using UnityEngine;

namespace Aqua
{
    public partial class LocService : ServiceBehaviour, ILoadable
    {
        #region Inspector

        [SerializeField, Required] private LocPackage[] m_EnglishStrings = null;

        #endregion // Inspector

        private LocPackage m_LanguagePackage;

        private Routine m_LoadRoutine;
        private IPool<TagString> m_TagStringPool;

        private bool m_Loading;
        private List<LocText> m_ActiveTexts = new List<LocText>(64);
        
        #region Loading

        private IEnumerator InitialLoad()
        {
            m_Loading = true;
            yield return LoadLanguage(true);
            m_Loading = false;
            DispatchTextRefresh();
        }

        private IEnumerator LoadLanguage(bool inbForce)
        {
            if (m_LanguagePackage != null)
                yield break;

            m_LanguagePackage = ScriptableObject.CreateInstance<LocPackage>();
            m_LanguagePackage.name = "LanguageStrings";
            foreach(var file in m_EnglishStrings)
            {
                var parser = BlockParser.ParseAsync(ref m_LanguagePackage, file.name, file.Source(), Parsing.Block, LocPackage.Generator.Instance);
                yield return Async.Schedule(parser);
            }

            DebugService.Log(LogMask.Loading | LogMask.Localization, "[LocService] Loaded {0} keys (english)", m_LanguagePackage.Count);
        }

        #endregion // Loading

        #region Localization

        /// <summary>
        /// Localizes the given key.
        /// </summary>
        public string Localize(TextId inKey, bool inbIgnoreEvents = false)
        {
            return Localize(inKey, string.Empty, null, inbIgnoreEvents);
        }

        /// <summary>
        /// Localize the given key.
        /// </summary>
        public string Localize(TextId inKey, StringSlice inDefault, object inContext = null, bool inbIgnoreEvents = false)
        {
            if (m_Loading)
            {
                Debug.LogErrorFormat("[LocService] Localization is still loading");
                return inDefault.ToString();
            }

            if (inKey.IsEmpty)
                return inDefault.ToString();

            string content;
            if (!m_LanguagePackage.TryGetContent(inKey, out content))
            {
                Debug.LogErrorFormat("[LocService] Unable to locate entry for '{0}'", inKey);
                content = inDefault.ToString();
            }
            
            if (!inbIgnoreEvents && content.IndexOf('{') >= 0)
            {
                using(var tagAlloc = m_TagStringPool.TempAlloc())
                {
                    TagString tagStr = tagAlloc.Object;
                    Services.Script.ParseToTag(ref tagStr, content, inContext);
                    content = tagStr.RichText;
                    if (tagStr.EventCount > 0)
                    {
                        Log.Warn("[LocService] Entry for '{0}' contains {1} embedded events, which are discarded when translating directly to string", inKey, tagStr.EventCount);
                    }
                }
            }
            return content;
        }

        #endregion // Localization

        #region Tagged

        public TagString LocalizeTagged(StringHash32 inKey, object inContext = null)
        {
            TagString tagString = new TagString();
            LocalizeTagged(ref tagString, inKey, inContext);
            return tagString;
        }

        public bool LocalizeTagged(ref TagString ioTagString, StringHash32 inKey, object inContext = null)
        {
            if (ioTagString == null)
                ioTagString = new TagString();
            else
                ioTagString.Clear();

            if (m_Loading)
            {
                Debug.LogErrorFormat("[LocService] Localization is still loading");
                return false;
            }

            if (inKey.IsEmpty)
            {
                return true;
            }

            string content;
            if (!m_LanguagePackage.TryGetContent(inKey, out content))
            {
                Debug.LogErrorFormat("[LocService] Unable to locate entry for '{0}'", inKey);
                return false;
            }

            Services.Script.ParseToTag(ref ioTagString, content, inContext);
            return true;
        }

        #endregion // Tagged

        #region Texts

        public void RegisterText(LocText inText)
        {
            m_ActiveTexts.Add(inText);
        }

        public void DeregisterText(LocText inText)
        {
            m_ActiveTexts.FastRemove(inText);
        }

        private void DispatchTextRefresh()
        {
            for(int i = 0, length = m_ActiveTexts.Count; i < length; i++)
                m_ActiveTexts[i].OnLocalizationRefresh();
        }

        #endregion // Texts

        #region IService

        public bool IsLoading()
        {
            return m_LoadRoutine;
        }

        protected override void Initialize()
        {
            m_LoadRoutine.Replace(this, InitialLoad()).TryManuallyUpdate(0);

            m_TagStringPool = new DynamicPool<TagString>(4, Pool.DefaultConstructor<TagString>());
            m_TagStringPool.Prewarm();
        }

        protected override void Shutdown()
        {
            UnityHelper.SafeDestroy(ref m_LanguagePackage);

            base.Shutdown();
        }

        #endregion // IService
    }
}