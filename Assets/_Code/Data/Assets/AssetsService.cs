using System.Collections;
using System.Collections.Generic;
using System.IO;
using Aqua.Journal;
using BeauData;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using NativeWebUtils;
using TMPro;
using UnityEngine;

namespace Aqua {
    public class AssetsService : ServiceBehaviour {
        #region Inspector

        [Header("Databases")]
        [SerializeField, Required] private ActDB m_Acts = null;
        [SerializeField, Required] private JobDB m_Jobs = null;
        [SerializeField, Required] private BestiaryDB m_Bestiary = null;
        [SerializeField, Required] private MapDB m_Map = null;
        [SerializeField, Required] private InventoryDB m_Inventory = null;
        [SerializeField, Required] private WaterPropertyDB m_WaterProperties = null;
        [SerializeField, Required] private ScriptCharacterDB m_ScriptCharacters = null;
        [SerializeField, Required] private JournalDB m_Journal = null;

        [Header("Fonts")]
        [SerializeField, Required] private TMP_FontAsset m_RegularFont = null;
        [SerializeField, Required] private TMP_FontAsset m_SemiBoldFont = null;
        [SerializeField, Required] private TMP_FontAsset m_BoldFont = null;

        [Header("Shaders")]
        [SerializeField, Required] private Material m_DefaultSpriteMaterial = null;
        [SerializeField, Required] private Material m_OverlaySpriteMaterial = null;

        [Header("Streaming")]
        [SerializeField, Range(1, 32)] private float m_StreamedTextureMem = 8;
        [SerializeField, Range(1, 32)] private float m_StreamedAudioMem = 8;

        [Header("Preload")]
        [SerializeField, Required] private TextAsset[] m_PreloadGroupFiles = null;

        #endregion // Inspector

        public ActDB Acts { get { return m_Acts; } }
        public JobDB Jobs { get { return m_Jobs; } }
        public BestiaryDB Bestiary { get { return m_Bestiary; } }
        public MapDB Map { get { return m_Map; } }
        public InventoryDB Inventory { get { return m_Inventory; } }
        public WaterPropertyDB WaterProp { get { return m_WaterProperties; } }
        public ScriptCharacterDB Characters { get { return m_ScriptCharacters; } }
        public JournalDB Journal { get { return m_Journal; } }

        public TMP_FontAsset RegularFont { get { return m_RegularFont; } }
        public TMP_FontAsset SemiBoldFont { get { return m_SemiBoldFont; } }
        public TMP_FontAsset BoldFont { get { return m_BoldFont; } }

        public Material DefaultSpriteMaterial { get { return m_DefaultSpriteMaterial; } }
        public Material OverlaySpriteMaterial { get { return m_OverlaySpriteMaterial; } }

        private Dictionary<StringHash32, PreloadGroup> m_PreloadGroupMap = new Dictionary<StringHash32, PreloadGroup>();
        private Dictionary<StringHash32, int> m_PreloadPathRefCountMap = new Dictionary<StringHash32, int>();
        private Unsafe.ArenaHandle m_DecompressionBuffer;

        protected override void Initialize() {
            base.Initialize();

            m_Acts.Initialize();
            m_Jobs.Initialize();
            m_Bestiary.Initialize();
            m_Map.Initialize();
            m_Inventory.Initialize();
            m_WaterProperties.Initialize();
            m_ScriptCharacters.Initialize();
            m_Journal.Initialize();

            Streaming.TextureMemoryBudget = (long) (m_StreamedTextureMem * 1024 * 1024);
            Streaming.AudioMemoryBudget = (long) (m_StreamedAudioMem * 1024 * 1024);
            m_DecompressionBuffer = Unsafe.CreateArena(1024 * 1024 * 2, "Decompression");

            Assets.Assign(this, m_DecompressionBuffer);
            Routine.Start(this, StreamingManagementRoutine());

            foreach(var file in m_PreloadGroupFiles) {
                PreloadManifest preloadManifest = Serializer.Read<PreloadManifest>(file);
                foreach(var group in preloadManifest.Groups) {
                    if (group.Paths != null) {
                        // pre-translate to streaming assets url
                        for(int i = 0; i < group.Paths.Length; i++) {
                            group.Paths[i] = NativePreload.StreamingAssetsURL(group.Paths[i]);
                        }
                    }
                    m_PreloadGroupMap.Add(group.Id, group);
                }
            }
            Log.Msg("[AssetsService] Found {0} preload groups", m_PreloadGroupMap.Count);
        }

        private IEnumerator StreamingManagementRoutine() {
            object bigWait = 60f;
            object smallWait = 5f;

            while (true) {
                yield return bigWait;
                while (Script.IsLoading || Streaming.IsUnloading()) {
                    yield return smallWait;
                }

                Streaming.UnloadUnusedAsync(60f);
                while(Streaming.IsUnloading()) {
                    yield return null;
                }
            }
        }

        protected override void Shutdown() {
            Streaming.UnloadAll();

            Unsafe.TryDestroyArena(ref m_DecompressionBuffer);

            base.Shutdown();
        }
    
        #region Preload

        public void PreloadGroup(StringHash32 groupId) {
            if (groupId.IsEmpty) {
                return;
            }
            TryPreloadGroup(groupId);
        }

        public bool PreloadGroupIsPrimaryLoaded(StringHash32 groupId) {
            if (groupId.IsEmpty) {
                return true;
            }

            return IsPreloadGroupLoaded(groupId);
        }

        public void CancelPreload(StringHash32 groupId) {
            if (groupId.IsEmpty) {
                return;
            }
            TryCancelPreloadGroup(groupId);
        }

        private bool IsPreloadGroupLoaded(StringHash32 id) {
            if (!m_PreloadGroupMap.TryGetValue(id, out PreloadGroup group)) {
                Log.Error("[AssetsService] Preload group with id '{0}' does not exist", id);
                return false;
            }

            if (group.RefCount <= 0) {
                return false;
            }

            if (group.Include != null) {
                foreach(var include in group.Include) {
                    if (!IsPreloadGroupLoaded(include)) {
                        return false;
                    }
                }
            }
            if (group.Paths != null) {
                foreach(var path in group.Paths) {
                    if (!IsPathLoaded(path)) {
                        return false;
                    }
                }
            }

            return true;
        }

        private void TryPreloadGroup(StringHash32 id) {
            if (!m_PreloadGroupMap.TryGetValue(id, out PreloadGroup group)) {
                Log.Error("[AssetsService] Preload group with id '{0}' does not exist", id);
                return;
            }

            group.RefCount++;
            if (group.RefCount == 1) {
                Log.Msg("[AssetsService] Preloading group '{0}'", id);
                if (group.Include != null) {
                    foreach(var include in group.Include) {
                        TryPreloadGroup(include);
                    }
                }
                if (group.Paths != null) {
                    foreach(var path in group.Paths) {
                        TryPreloadPath(path);
                    }
                }
                if (group.LowPriority != null) {
                    foreach(var include in group.LowPriority) {
                        TryPreloadGroup(include);
                    }
                }
            }
        }

        private void TryCancelPreloadGroup(StringHash32 id) {
            if (!m_PreloadGroupMap.TryGetValue(id, out PreloadGroup group)) {
                Log.Error("[AssetsService] Preload group with id '{0}' does not exist", id);
                return;
            }

            if (group.RefCount > 0) {
                group.RefCount--;
                if (group.RefCount == 0) {
                    Log.Msg("[AssetsService] Canceling preload group '{0}'", id);
                    if (group.Include != null) {
                        foreach(var include in group.Include) {
                            TryCancelPreloadGroup(include);
                        }
                    }
                    if (group.Paths != null) {
                        foreach(var path in group.Paths) {
                            TryCancelPreloadPath(path);
                        }
                    }
                    if (group.LowPriority != null) {
                        foreach(var include in group.LowPriority) {
                            TryCancelPreloadGroup(include);
                        }
                    }
                }
            }
        }

        private void TryPreloadPath(string path) {
            StringHash32 id = path;
            m_PreloadPathRefCountMap.TryGetValue(id, out int refCount);
            refCount++;
            m_PreloadPathRefCountMap[id] = refCount;
            if (refCount == 1) {
                string extension = Path.GetExtension(path).ToLowerInvariant();
                NativePreload.ResourceType type = NativePreload.ResourceType.Unknown;
                if (extension == ".mp3") {
                    type = NativePreload.ResourceType.Audio;
                }
                NativePreload.Preload(path, type);
            }
        }

        private bool IsPathLoaded(string path) {
            return m_PreloadPathRefCountMap.ContainsKey(path) && NativePreload.IsLoaded(path);
        }

        private void TryCancelPreloadPath(string path) {
            StringHash32 id = path;
            if (m_PreloadPathRefCountMap.TryGetValue(id, out int refCount) && refCount > 0) {
                refCount--;
                m_PreloadPathRefCountMap[id] = refCount;
                if (refCount == 0) {
                    NativePreload.Cancel(path);
                }
            }
        }

        #endregion // Preload
    }
}