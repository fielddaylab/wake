using UnityEngine;
using Aqua;
using BeauUtil;
using ScriptableBake;
using System.Collections;
using BeauUtil.Variants;
using System;
using BeauRoutine;
using Aqua.Scripting;
using Aqua.Cameras;
using Leaf.Runtime;
using UnityEngine.Scripting;
using BeauUtil.Debugger;

namespace ProtoAqua.Observation
{
    public class SpecterSpawnSystem : ScriptComponent, IBaked, IScenePreloader, ISceneUnloadHandler {

        static public readonly TableKeyPair Var_LastSpecterSeen = TableKeyPair.Parse("world:specter.lastSeenTime");
        static public readonly TableKeyPair Var_LastSpecterSeenLocation = TableKeyPair.Parse("world:specter.lastSeenLocation");

        #region Inspector

        [SerializeField, HideInInspector] private SpecterSpawnRegion[] m_Regions;
        [SerializeField, HideInInspector] private float m_CurrentChance;
        [SerializeField, HideInInspector] private float m_ChanceIncrement;

        [SerializeField] private float m_DesiredSpecterDistance = 5;
        
        [Header("--DEBUG--")]
        [SerializeField] private bool m_DEBUGForceSpawn = false;

        #endregion // Inspector

        [NonSerialized] private SpecterReveal m_CurrentReveal;
        [NonSerialized] private SpecterSpawnRegion m_CurrentRegion;
        [NonSerialized] private SpecterReveal m_LoadedReveal;
        [NonSerialized] private Routine m_Cutscene;

        private void OnPlayerEnterSpecterRegion(SpecterSpawnRegion region) {
            if (Script.IsLoading || Script.ShouldBlock() || m_Cutscene)
                return;

            Log.Msg("[SpecterSpawnSystem] Current specter chance is {0}%...", ((int) (m_CurrentChance * 100)).ToStringLookup());
            if (!RNG.Instance.Chance(m_CurrentChance)) {
                m_CurrentChance += m_ChanceIncrement;
                Log.Msg("...failed, next specter chance is {0}%", ((int) (m_CurrentChance * 100)).ToStringLookup());
                return;
            }
            
            m_CurrentRegion = region;

            Log.Msg("[SpecterSpawnSystem] Starting specter encounter!");

            foreach(var toDisable in m_Regions) {
                toDisable.gameObject.SetActive(false);
            }

            m_Cutscene.Replace(this, SpecterSequence()).Tick();
        }

        #region Cutscene

        private IEnumerator SpecterSequence() {
            Services.UI.PreloadJournal();
            
            using(Script.Letterbox()) {
                ScriptThreadHandle thread;
                using(var table = TempVarTable.Alloc()) {
                    table.Set("specterIndex", Save.Science.SpecterCount() );
                    thread = Services.Script.TriggerResponse(GameTriggers.PlayerSpecter, table);
                }

                Save.Science.DequeueSpecter();
                Save.Science.SetSpecterCount(Save.Science.SpecterCount() + 1);
                Script.WriteVariable(Var_LastSpecterSeen, (float) Save.Current.Playtime);
                Script.WriteVariable(Var_LastSpecterSeenLocation, MapDB.LookupCurrentMap());

                if (!thread.IsRunning()) {
                    yield return 1;
                    SpawnSpecter();
                    yield return 2;
                    yield return PlayScanAnimation();
                    yield return 1;
                } else {
                    while(thread.IsRunning()) {
                        yield return null;
                    }
                }
            }

            if (m_CurrentReveal != null) {
                m_CurrentReveal.GetComponent<CameraTarget>().enabled = false;
                m_CurrentReveal.preScannedParticles.Stop();
                yield return m_CurrentReveal.specterVisuals.transform.ScaleTo(0, 0.3f, Axis.Y).Ease(Curve.Smooth);
                m_CurrentReveal.specterVisuals.SetActive(false);
                while(m_CurrentReveal.preScannedParticles.particleCount > 0) {
                    yield return null;
                }
                Destroy(m_CurrentReveal.gameObject);
                m_CurrentReveal = null;
            }
            m_CurrentRegion = null;
        }

        [LeafMember("Spawn"), Preserve]
        private IEnumerator SpawnSpecter() {
            if (m_CurrentReveal != null)
                yield break;
            Vector3 currentPos = Script.CurrentPlayer.transform.position;
            Vector3 pos = ScoringUtils.GetMinElement(m_CurrentRegion.LocationPositions, (p) => Math.Abs(Vector3.Distance(currentPos, p) - m_DesiredSpecterDistance));
            m_CurrentReveal = Instantiate(m_LoadedReveal, pos, Quaternion.identity);
            m_CurrentReveal.preScannedParticles.Play();
            yield return 0.5f;
        }

        [LeafMember("Scan"), Preserve]
        private IEnumerator PlayScanAnimation(float duration = 3) {
            Services.UI.Dialog.Hide();
            ScannerDisplay ui = Services.UI.FindPanel<ScannerDisplay>();
            ui.ShowProgress(0);
            Services.Audio.PostEvent("scan_start");
            yield return Tween.ZeroToOne(ui.ShowProgress, duration);
            Services.Audio.PostEvent("scan_specter");
            ui.Hide();
            if (m_CurrentReveal) {
                m_CurrentReveal.SetScanned();
            }
            yield return 0.8f;
            Save.Bestiary.RegisterEntity(m_LoadedReveal.specterId);
            if (!Services.UI.IsSkippingCutscene()) {
                yield return Script.PopupNewSpecter(Assets.Bestiary(m_LoadedReveal.specterId));
            }
            yield return 0.5f;
        }

        #endregion // Cutscene

        #if UNITY_EDITOR

        int IBaked.Order { get { return 15; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            m_Regions = FindObjectsOfType<SpecterSpawnRegion>();
            m_CurrentChance = m_Regions.Length == 0 ? 1f : 0.4f + (0.6f / m_Regions.Length);
            m_ChanceIncrement = 2.5f * (1 - (m_CurrentChance)) / m_Regions.Length;
            if ((flags & BakeFlags.IsBuild) != 0) {
                m_DEBUGForceSpawn = false;
            }
            return true;
        }

        #endif // UNITY_EDITOR

        #region ISceneLoad

        public IEnumerator OnPreloadScene(SceneBinding inScene, object inContext) {
            if (m_Regions.Length == 0)
                yield break;
            
            ScienceTweaks tweaks = Services.Tweaks.Get<ScienceTweaks>();

            StringHash32 currentMapId = MapDB.LookupCurrentMap();
            float lastSpecterTime = Script.ReadVariable(Var_LastSpecterSeen).AsFloat();
            float timeSinceLastSpecter = (float) (Save.Current.Playtime - lastSpecterTime);
            bool enoughTime = lastSpecterTime <= 0 || timeSinceLastSpecter >= tweaks.MinSpecterIntervalSeconds();
            bool isQueued = Save.Inventory.HasUpgrade(ItemIds.ROVScanner) && Save.Science.IsSpecterQueued(currentMapId) && enoughTime && Save.Science.SpecterCount() < ScienceUtils.MaxSpecters();

            #if UNITY_EDITOR
            if (BootParams.BootedFromCurrentScene) {
                isQueued |= m_DEBUGForceSpawn;
            }
            #endif // UNITY_EDITOR
            
            if (isQueued) {
                // if override set for this map, then increase initial chances
                if (Save.Science.IsSpecterQueuedExact(currentMapId)) {
                    m_CurrentChance += m_ChanceIncrement;
                    m_ChanceIncrement *= 1.25f;
                }

                foreach(var region in m_Regions) {
                    WorldUtils.ListenForPlayer(region.Region, (c) => OnPlayerEnterSpecterRegion(region), null);
                }

                string specterPath = tweaks.SpecterResourcePath((int) Save.Science.SpecterCount());
                Log.Msg("[SpecterSpawnSystem] Preparing specter from '{0}'...", specterPath);
                var load = Resources.LoadAsync<SpecterReveal>(specterPath);
                yield return load;
                m_LoadedReveal = (SpecterReveal) load.asset;
                Log.Msg("[SpecterSpawnSystem] Specter loaded");
            } else {
                foreach(var region in m_Regions) {
                    Destroy(region.gameObject);
                }
                m_Regions = null;
            }
        }

        public void OnSceneUnload(SceneBinding inScene, object inContext) {
            if (m_LoadedReveal != null) {
                // GameObject.Destroy(m_LoadedReveal.gameObject);
                m_LoadedReveal = null;
            }
        }

        #endregion // ISceneLoad
    }
}