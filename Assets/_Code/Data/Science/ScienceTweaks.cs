using System;
using System.Collections.Generic;
using Aqua.Profile;
using BeauData;
using BeauUtil;
using EasyAssetStreaming;
using ScriptableBake;
using UnityEngine;

namespace Aqua {

    [CreateAssetMenu(menuName = "Aqualab System/Science Tweaks", fileName = "ScienceTweaks")]
    public class ScienceTweaks : TweakAsset, IBaked {
        #region Inspector

        [SerializeField] private Sprite[] m_LevelIcons;
        [SerializeField] private SerializedHash32[] m_LevelBadgeLayouts;
        [SerializeField] private Color32[] m_LevelColors;

        [Header("Specters")]
        [SerializeField] private float m_SpecterMinIntervalMinutes = 10;
        [SerializeField] private string[] m_SpecterResourcePaths = null;

        [Header("Experience")]
        [SerializeField] private uint[] m_BaseExperiencePerLevel = new uint[] { 30, 50 };

        [Header("Bestiary Ordering")]
        [SerializeField] private TaggedBestiaryDesc[] m_CanonicalOrganismOrdering = null;
        [SerializeField] private BestiaryDesc[] m_CanonicalSpecterOrdering = null;

        #endregion // Inspector

        public Sprite LevelIcon(int level) { return m_LevelIcons[Mathf.Clamp(level - 1, 0, m_LevelIcons.Length - 1)]; }
        public StringHash32 LevelBadgeLayout(int level) { return m_LevelBadgeLayouts[Mathf.Clamp(level - 1, 0, m_LevelBadgeLayouts.Length - 1)]; }
        public Color LevelColor(int level) { return m_LevelColors[Mathf.Clamp(level - 1, 0, m_LevelColors.Length - 1)]; }
        public int MaxLevels() { return m_BaseExperiencePerLevel.Length + 1; }

        public int MaxSpecters() { return m_SpecterResourcePaths.Length; }
        public float MinSpecterIntervalSeconds() { return m_SpecterMinIntervalMinutes * 60f; }
        public string SpecterResourcePath(int idx) { return m_SpecterResourcePaths[Math.Min(idx, m_SpecterResourcePaths.Length - 1)]; }

        public ListSlice<TaggedBestiaryDesc> CanonicalOrganismOrdering() { return m_CanonicalOrganismOrdering; }
        public ListSlice<BestiaryDesc> CanonicalSpecterOrdering() { return m_CanonicalSpecterOrdering; }

        protected override void Apply() {
            base.Apply();

            uint[] cumulativeExp = (uint[]) m_BaseExperiencePerLevel.Clone();
            for(int i = 1; i < cumulativeExp.Length; i++) {
                cumulativeExp[i] += cumulativeExp[i - 1];
            }
            ScienceUtils.UpdateLevelingCalculation(m_BaseExperiencePerLevel, cumulativeExp);
            ScienceUtils.UpdateMaxSpecters(m_SpecterResourcePaths.Length);
        }

        #if UNITY_EDITOR

        int IBaked.Order { get { return 100; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            Dictionary<StringHash32, List<BestiaryDesc>> perStation = new Dictionary<StringHash32, List<BestiaryDesc>>();
            List<TaggedBestiaryDesc> finalList = new List<TaggedBestiaryDesc>(60);

            void AddToDictionary(StringHash32 station, BestiaryDesc entity) {
                if (!perStation.TryGetValue(station, out var perStationList)) {
                    perStationList = new List<BestiaryDesc>();
                    perStation.Add(station, perStationList);
                }
                if (!perStationList.Contains(entity)) {
                    perStationList.Add(entity);
                    if (!station.IsEmpty) {
                        RemoveFromDictionary(null, entity);
                    }
                }
            }

            void RemoveFromDictionary(StringHash32 station, BestiaryDesc entity) {
                if (perStation.TryGetValue(station, out var perStationList)) {
                    perStationList.FastRemove(entity);
                }
            }

            void AppendToFinalList(StringHash32 station) {
                if (perStation.TryGetValue(station, out var perStationList)) {
                    foreach(var entry in perStationList) {
                        finalList.Add(new TaggedBestiaryDesc(entry, station));
                    }
                }
            }

            foreach(var desc in ValidationUtils.FindAllAssets<BestiaryDesc>()) {
                if (!char.IsLetterOrDigit(desc.name[0])) {
                    continue;
                }
                switch(desc.Category()) {
                    case BestiaryDescCategory.Critter: {
                        if (!desc.HasFlags(BestiaryDescFlags.IsSpecter)) {
                            AddToDictionary(desc.StationId(), desc);
                        } else {
                            AddToDictionary("specter", desc);
                        }
                        break;
                    }

                    case BestiaryDescCategory.Environment: {
                        foreach(var organism in desc.Organisms()) {
                            AddToDictionary(desc.StationId(), Assets.Bestiary(organism));
                        }
                        break;
                    }
                }
            }

            foreach(var list in perStation.Values) {
                list.Sort(BestiaryDesc.SortByOrder);
            }

            AppendToFinalList(null);
            AppendToFinalList(MapIds.KelpStation);
            AppendToFinalList(MapIds.CoralStation);
            AppendToFinalList(MapIds.BayouStation);
            AppendToFinalList(MapIds.ArcticStation);
            AppendToFinalList(MapIds.FinalStation);

            m_CanonicalOrganismOrdering = finalList.ToArray();

            if (perStation.TryGetValue("specter", out var specterList)) {
                m_CanonicalSpecterOrdering = specterList.ToArray();
            } else {
                m_CanonicalSpecterOrdering = null;
            }

            return true;
        }

        #endif // UNITY_EDITOR
    }

    static public class ScienceUtils {
        static private uint[] s_CumulativeExpPerLevel;
        static private uint[] s_BaseExpPerLevel;
        static private int s_MaxSpecters;
        static private int s_MaxLevel;

        static internal void UpdateMaxSpecters(int maxSpecters) {
            s_MaxSpecters = maxSpecters;
        }

        static internal void UpdateLevelingCalculation(uint[] baseExpPerLevel, uint[] cumulativeExpPerLevel) {
            s_BaseExpPerLevel = baseExpPerLevel;
            s_CumulativeExpPerLevel = cumulativeExpPerLevel;
            s_MaxLevel = cumulativeExpPerLevel.Length + 1;
        }

        static public int MaxSpecters() {
            return s_MaxSpecters;
        }

        static public float DecryptProgress(int specterCount) {
            if (specterCount <= 1)
                return 0;
            if (specterCount >= s_MaxSpecters)
                return 1;
            return (float) (specterCount - 1) / (s_MaxSpecters - 1);
        }

        static public uint TotalExpForLevel(uint level) {
            if (level < 2) {
                return 0;
            } else {
                return s_CumulativeExpPerLevel[Math.Min(level - 2, s_CumulativeExpPerLevel.Length - 1)];
            }
        }

        static public uint ExpForLevel(uint level) {
            if (level < 2) {
                return 0;
            } else {
                return s_BaseExpPerLevel[Math.Min(level - 2, s_BaseExpPerLevel.Length - 1)];
            }
        }

        static public uint LevelForTotalExp(uint exp) {
            int idx = 0;
            while(idx < s_CumulativeExpPerLevel.Length && exp > s_CumulativeExpPerLevel[idx++]);
            return (uint) (1 + idx);
        }

        static public uint TotalExpForNextLevel(SaveData data) {
            uint nextLevel = TotalExpForLevel(data.Science.CurrentLevel() + 1);
            uint current = data.Inventory.Exp();
            return current >= nextLevel ? 0 : nextLevel - current;
        }

        static public ScienceLevelProgress EvaluateLevel(uint exp) {
            int idx = 0;
            while(idx < s_CumulativeExpPerLevel.Length && exp > s_CumulativeExpPerLevel[idx++]);
            ScienceLevelProgress progress;
            progress.Level = (uint) idx;
            if (idx >= s_CumulativeExpPerLevel.Length) {
                progress.Percent = 1;
                progress.ToNext = 0;
                progress.ExpForLevel = 0;
            } else {
                progress.ExpForLevel = exp - s_CumulativeExpPerLevel[idx - 1];
                progress.ToNext = ExpForLevel(progress.Level + 1) - progress.ExpForLevel;
                progress.Percent = (float) progress.ExpForLevel / (progress.ExpForLevel + progress.ToNext);
            }
            return progress;
        }

        static public ScienceLevelProgress EvaluateLevel(SaveData data) {
            ScienceLevelProgress progress;
            progress.Level = data.Science.CurrentLevel();
            if (progress.Level >= s_MaxLevel) {
                progress.ExpForLevel = 0;
                progress.ToNext = 0;
                progress.Percent = 1;
            } else {
                progress.ExpForLevel = data.Inventory.Exp() - TotalExpForLevel(progress.Level);
                progress.ToNext = ExpForLevel(progress.Level + 1) - progress.ExpForLevel;
                progress.Percent = (float) progress.ExpForLevel / (progress.ExpForLevel + progress.ToNext);
            }
            return progress;
        }

        static public bool CanLevelUp(SaveData data) {
            return TotalExpForNextLevel(data) == 0;
        }

        static public bool AttemptLevelUp(SaveData data, out ScienceLevelUp levelUp) {
            levelUp.OriginalLevel = data.Science.CurrentLevel();
            levelUp.LevelAdjustment = 0;

            PlayerInv totalExp = data.Inventory.GetItem(ItemIds.Exp);
            uint checkedLevel = levelUp.OriginalLevel + 1;
            while (checkedLevel <= s_MaxLevel && totalExp.Count >= TotalExpForLevel(checkedLevel)) {
                checkedLevel++;
                levelUp.LevelAdjustment++;
            }

            if (levelUp.LevelAdjustment > 0) {
                data.Science.SetCurrentLevel(checkedLevel - 1);
                return true;
            }

            return false;
        }
    }

    public struct ScienceLevelUp {
        public uint OriginalLevel;
        public int LevelAdjustment;
    }

    public struct ScienceLevelProgress {
        public uint Level;
        public uint ExpForLevel;
        public uint ToNext;
        public float Percent;
    }
}