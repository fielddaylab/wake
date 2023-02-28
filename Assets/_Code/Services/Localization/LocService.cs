using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Compression;
using Aqua.Debugging;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using EasyAssetStreaming;
using ScriptableBake;
using UnityEngine;

namespace Aqua
{
    public partial class LocService : ServiceBehaviour, ILoadable
    {
        static private readonly FourCC DefaultLanguage = FourCC.Parse("EN");

        #region Inspector

        [SerializeField, Required] private LocManifest m_EnglishManifest;

        #endregion // Inspector

        [NonSerialized] private LocPackage m_LanguagePackage;

        private Routine m_LoadRoutine;
        private IPool<TagString> m_TagStringPool;

        [NonSerialized] private bool m_Loading;
        [NonSerialized] private FourCC m_CurrentLanguage;
        [NonSerialized] private LayoutPrefabPackage m_CurrentJournalPackage;
        [NonSerialized] private List<LocText> m_ActiveTexts = new List<LocText>(64);
        
        #region Loading

        private IEnumerator InitialLoad()
        {
            yield return LoadLanguage(m_EnglishManifest);
        }

        private IEnumerator LoadLanguage(LocManifest manifest)
        {
            m_Loading = true;
            
            if (m_LanguagePackage == null) {
                m_LanguagePackage = ScriptableObject.CreateInstance<LocPackage>();
                m_LanguagePackage.name = "LanguageStrings";
            }

            m_LanguagePackage.Clear();
            using(Profiling.Time("loading language")) {
                bool loadPackages;
                #if PREVIEW || PRODUCTION
                loadPackages = false;
                #else
                loadPackages = manifest.Packages.Length > 0;
                #endif // PREVIEW || PRODUCTION
                if (loadPackages) {
                    DebugService.Log(LogMask.Loading | LogMask.Localization, "[LocService] Loading '{0}' from {1} packages", manifest.name, manifest.Packages.Length);
                    foreach(var file in manifest.Packages) {
                        var parser = BlockParser.ParseAsync(ref m_LanguagePackage, file, Parsing.Block, LocPackage.Generator.Instance);
                        yield return Async.Schedule(parser); 
                    }
                } else {
                    DebugService.Log(LogMask.Loading | LogMask.Localization, "[LocService] Loading '{0}' from {1:0.00}kb binary", manifest.name, manifest.Binary.Length / 1024);
                    yield return Async.Schedule(LocPackage.ReadFromBinary(m_LanguagePackage, manifest.Binary));
                }
            }

            DebugService.Log(LogMask.Loading | LogMask.Localization, "[LocService] Loaded {0} keys ({1})", m_LanguagePackage.Count, manifest.LanguageId.ToString());

            m_CurrentLanguage = manifest.LanguageId;
            m_CurrentJournalPackage = manifest.JournalLayout;
            m_Loading = false;
            DispatchTextRefresh();
        }

        #endregion // Loading

        #region Localization

        public FourCC CurrentLanguageId {
            get { return m_CurrentLanguage; }
        }

        public LayoutPrefabPackage CurrentJournalPackage {
            get { return m_CurrentJournalPackage; }
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
            if (m_Loading)
            {
                Debug.LogErrorFormat("[LocService] Localization is still loading");
                return inDefault.ToString();
            }

            if (inKey.IsEmpty)
                return inDefault.ToString();

            string content;
            bool hasEvents;
            if (!m_LanguagePackage.TryGetContent(inKey, out content))
            {
                if (inDefault.IsEmpty || m_CurrentLanguage != DefaultLanguage)
                {
                    Debug.LogErrorFormat("[LocService] Unable to locate entry for '{0}' ({1})", inKey.Source(), inKey.Hash().HashValue);
                }
                content = inDefault.ToString();
                hasEvents = content.IndexOf('{') >= 0;
            }
            else
            {
                hasEvents = m_LanguagePackage.HasEvents(inKey);
            }
            
            if (!inbIgnoreEvents && hasEvents)
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

        public bool LocalizeTagged(ref TagString ioTagString, TextId inKey, object inContext = null)
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
                Debug.LogErrorFormat("[LocService] Unable to locate entry for '{0}' ({1})", inKey.Source(), inKey.Hash().HashValue);
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
            m_LoadRoutine.Replace(this, InitialLoad()).Tick();

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