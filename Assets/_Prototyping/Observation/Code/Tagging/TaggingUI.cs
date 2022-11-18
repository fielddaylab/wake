using UnityEngine;
using Aqua;
using BeauUtil;
using ScriptableBake;

namespace ProtoAqua.Observation {
    public class TaggingUI : SharedPanel, IBaked
    {
        #region Inspector

        [Header("Tagging")]
        [SerializeField] private GameObject m_MeterGroup = null;
        [SerializeField, HideInInspector] private TaggingMeter[] m_AllMeters;

        #endregion // Inspector

        public void Populate(ListSlice<TaggingManifest> inManifest, ListSlice<ushort> inProgressEntries)
        {
            ushort progress;
            TaggingManifest manifest;
            TaggingMeter meter;

            int used = 0;

            for(int i = 0; i < inProgressEntries.Length; i++)
            {
                progress = inProgressEntries[i];
                manifest = inManifest[i];
                if (progress == 0 || progress >= manifest.Required)
                    continue;
                
                meter = m_AllMeters[used++];
                meter.gameObject.SetActive(true);
                meter.Icon.sprite = Assets.Bestiary(manifest.Id).Icon();
                meter.Meter.ArcFill = progress / (float) manifest.Required;
            }

            for(int i = used; i < m_AllMeters.Length; i++)
            {
                m_AllMeters[i].gameObject.SetActive(false);
            }

            m_MeterGroup.SetActive(used > 0);
        }

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context)
        {
            m_AllMeters = GetComponentsInChildren<TaggingMeter>(true);
            return true;
        }

        #endif // UNITY_EDITOR
    }
}