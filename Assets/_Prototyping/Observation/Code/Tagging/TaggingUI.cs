using UnityEngine;
using Aqua;
using UnityEngine.UI;
using BeauUtil;
using BeauRoutine;
using ScriptableBake;

namespace ProtoAqua.Observation
{
    public class TaggingUI : SharedPanel, IBaked
    {
        #region Inspector

        [Header("Tagging")]
        [SerializeField, HideInInspector] private TaggingMeter[] m_AllMeters;

        #endregion // Inspector

        public void Populate(ListSlice<TaggingProgress> inProgressEntries)
        {
            TaggingProgress progress;
            TaggingMeter meter;

            int used = 0;

            for(int i = 0; i < inProgressEntries.Length; i++)
            {
                progress = inProgressEntries[i];
                if (progress.Tagged == 0)
                    continue;
                
                meter = m_AllMeters[used++];
                meter.gameObject.SetActive(true);
                meter.Icon.sprite = Assets.Bestiary(progress.Id).Icon();
                meter.Meter.fillAmount = progress.Tagged / (float) (progress.TotalInScene * progress.Proportion);
            }

            for(int i = used; i < m_AllMeters.Length; i++)
            {
                m_AllMeters[i].gameObject.SetActive(false);
            }
        }

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags)
        {
            m_AllMeters = GetComponentsInChildren<TaggingMeter>(true);
            return true;
        }

        #endif // UNITY_EDITOR
    }
}