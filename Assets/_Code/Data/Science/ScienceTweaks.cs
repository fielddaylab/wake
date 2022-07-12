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

        [SerializeField] private Sprite m_LevelIcon = null;
        [SerializeField, StreamingImagePath] private string m_HiResLevelIconPath = null;
        [SerializeField, StreamingImagePath] private string m_HiResLevelUpPath = null;

        [Header("Experience")]
        [SerializeField] private uint m_BaseExperiencePerLevel = 20;
        [SerializeField] private uint m_AdditionalExperiencePerLevel = 5;

        [Header("Rewards")]
        [SerializeField] private int m_CashPerLevel = 200;

        [Header("Bestiary Ordering")]
        [SerializeField] private TaggedBestiaryDesc[] m_CanonicalOrganismOrdering = null;

        #endregion // Inspector

        public Sprite LevelIcon() { return m_LevelIcon; }
        public StreamedImageSet LevelIconSet() { return new StreamedImageSet(m_LevelIcon, m_HiResLevelIconPath); }
        public StreamedImageSet LevelUpSet() { return new StreamedImageSet(m_LevelIcon, m_HiResLevelUpPath); }
        public int CashPerLevel() { return m_CashPerLevel; }

        public ListSlice<TaggedBestiaryDesc> CanonicalOrganismOrdering() { return m_CanonicalOrganismOrdering; }

        protected override void Apply() {
            base.Apply();

            ScienceUtils.UpdateLevelingCalculation(m_BaseExperiencePerLevel, m_AdditionalExperiencePerLevel);
        }

        #if UNITY_EDITOR

        int IBaked.Order { get { return 100; } }

        bool IBaked.Bake(BakeFlags flags) {
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
                        AddToDictionary(desc.StationId(), desc);
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
            return true;
        }

        #endif // UNITY_EDITOR
    }

    static public class ScienceUtils {
        static private uint s_BaseExperiencePerLevel;
        static private uint s_AdditionalExperiencePerLevel;

        static internal void UpdateLevelingCalculation(uint baseExp, uint additionalExpPerLevel) {
            s_BaseExperiencePerLevel = baseExp;
            s_AdditionalExperiencePerLevel = additionalExpPerLevel;
        }

        static public uint ExpForLevel(uint level) {
            return level == 0 ? 0 : s_BaseExperiencePerLevel + s_AdditionalExperiencePerLevel * (level - 1);
        }

        static public uint ExpForNextLevel(SaveData data) {
            uint nextLevel = ExpForLevel(data.Science.CurrentLevel() + 1);
            uint current = data.Inventory.ItemCount(ItemIds.Exp);
            return current >= nextLevel ? 0 : nextLevel - current;
        }

        static public bool CanLevelUp(SaveData data) {
            return ExpForNextLevel(data) == 0;
        }

        static public bool AttemptLevelUp(SaveData data, out ScienceLevelUp levelUp) {
            levelUp.OriginalLevel = data.Science.CurrentLevel();
            levelUp.LevelAdjustment = 0;

            PlayerInv totalExp = data.Inventory.GetItem(ItemIds.Exp);
            uint checkedLevel = levelUp.OriginalLevel + 1;
            uint expToLevel;
            while (totalExp.Count >= (expToLevel = ExpForLevel(checkedLevel))) {
                totalExp.Count -= expToLevel;
                checkedLevel++;
                levelUp.LevelAdjustment++;
            }

            if (levelUp.LevelAdjustment > 0) {
                data.Inventory.SetItemWithoutNotify(ItemIds.Exp, totalExp.Count);
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
}