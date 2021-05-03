using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Debugging;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Tags;
using UnityEngine;

namespace Aqua
{
    public partial class LocService : ServiceBehaviour, ILoadable
    {
        #region Inspector

        [SerializeField, Required] private LocPackage[] m_GlobalStrings = null;
        [SerializeField, Required] private LocPackage[] m_EnglishStrings = null;

        #endregion // Inspector

        private LocPackage m_GlobalPackage;
        private LocPackage m_LanguagePackage;

        private Routine m_LoadRoutine;
        private IPool<TagString> m_TagStringPool;

        private List<LocText> m_ActiveTexts = new List<LocText>(64);
        
        #region Loading

        private IEnumerator InitialLoad()
        {
            yield return Routine.Combine(
                LoadIndependent(true),
                LoadLanguage(true)
            );

            DispatchTextRefresh();
        }

        private IEnumerator LoadIndependent(bool inbForce)
        {
            if (!inbForce && m_GlobalPackage != null)
                yield break;

            m_GlobalPackage = ScriptableObject.CreateInstance<LocPackage>();
            m_GlobalPackage.name = "GlobalStrings";
            foreach(var file in m_GlobalStrings)
            {
                var parser = BlockParser.ParseAsync(ref m_GlobalPackage, file.name, file.Source(), Parsing.Block, LocPackage.Generator.Instance);
                yield return Async.Schedule(parser);
            }

            DebugService.Log(LogMask.Loading | LogMask.Localization, "[LocService] Loaded {0} keys (global)", m_GlobalPackage.Count);
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
        /// Localizes the given text if the given string starts with a ' character.
        /// Otherwise uses the text as given.
        /// </summary>
        public string MaybeLocalize(StringSlice inString, object inContext = null, bool inbIgnoreEvents = false)
        {
            if (inString.IsEmpty)
                return string.Empty;

            if (inString.StartsWith('\''))
            {
                return Localize(inString.Substring(1).Hash32(), inString, inContext, inbIgnoreEvents);
            }

            StringSlice content = inString;
            if (!inbIgnoreEvents && inString.IndexOf('{') >= 0)
            {
                using(var tagAlloc = m_TagStringPool.TempAlloc())
                {
                    TagString tagStr = tagAlloc.Object;
                    Services.Script.ParseToTag(ref tagStr, content, inContext);
                    content = tagStr.RichText;
                    if (tagStr.EventCount > 0)
                    {
                        Debug.LogWarningFormat("[LocService] String '{0}' contains {1} embedded events, which are discarded when translating directly to string", inString, tagStr.EventCount);
                    }
                }
            }
            return content.ToString();
        }

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
            if (m_LoadRoutine)
            {
                Debug.LogErrorFormat("[LocService] Localization is still loading");
                return inDefault.ToString();
            }

            if (inKey.IsEmpty)
                return inDefault.ToString();

            string content;
            if (!m_LanguagePackage.TryGetContent(inKey, out content) && !m_GlobalPackage.TryGetContent(inKey, out content))
            {
                Debug.LogErrorFormat("[LocService] Unable to locate entry for '{0}'", inKey.ToDebugString());
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
                        Debug.LogWarningFormat("[LocService] Entry for '{0}' contains {1} embedded events, which are discarded when translating directly to string", inKey, tagStr.EventCount);
                    }
                }
            }
            return content;
        }

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

            if (m_LoadRoutine)
            {
                Debug.LogErrorFormat("[LocService] Localization is still loading");
                return false;
            }

            if (inKey.IsEmpty)
            {
                return true;
            }

            string content;
            if (!m_LanguagePackage.TryGetContent(inKey, out content) && !m_GlobalPackage.TryGetContent(inKey, out content))
            {
                Debug.LogErrorFormat("[LocService] Unable to locate entry for '{0}'", inKey.ToDebugString());
                return false;
            }

            Services.Script.ParseToTag(ref ioTagString, content, inContext);
            return true;
        }

        #endregion // Localization

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
            m_LoadRoutine.Replace(this, InitialLoad());

            m_TagStringPool = new DynamicPool<TagString>(4, Pool.DefaultConstructor<TagString>());
            m_TagStringPool.Prewarm();
        }

        protected override void Shutdown()
        {
            UnityHelper.SafeDestroy(ref m_GlobalPackage);
            UnityHelper.SafeDestroy(ref m_LanguagePackage);

            base.Shutdown();
        }

        #endregion // IService
    }
}