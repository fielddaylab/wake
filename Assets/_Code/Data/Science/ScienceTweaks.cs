using System;
using System.Collections.Generic;
using Aqua.Profile;
using BeauData;
using BeauUtil;
using EasyAssetStreaming;
using UnityEngine;

namespace Aqua {

    [CreateAssetMenu(menuName = "Aqualab System/Science Tweaks", fileName = "ScienceTweaks")]
    public class ScienceTweaks : TweakAsset {
        #region Inspector

        [SerializeField] private Sprite m_LevelIcon = null;
        [SerializeField, StreamingImagePath] private string m_HiResLevelIconPath = null;
        [SerializeField, StreamingImagePath] private string m_HiResLevelUpPath = null;

        [Header("Experience")]
        [SerializeField] private uint m_BaseExperiencePerLevel = 20;
        [SerializeField] private uint m_AdditionalExperiencePerLevel = 5;

        [Header("Rewards")]
        [SerializeField] private int m_CashPerLevel = 200;

        #endregion // Inspector

        public Sprite LevelIcon() { return m_LevelIcon; }
        public StreamedImageSet LevelIconSet() { return new StreamedImageSet(m_LevelIcon, m_HiResLevelIconPath); }
        public StreamedImageSet LevelUpSet() { return new StreamedImageSet(m_LevelIcon, m_HiResLevelUpPath); }
        public int CashPerLevel() { return m_CashPerLevel; }

        protected override void Apply() {
            base.Apply();

            ScienceUtils.UpdateLevelingCalculation(m_BaseExperiencePerLevel, m_AdditionalExperiencePerLevel);
        }
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