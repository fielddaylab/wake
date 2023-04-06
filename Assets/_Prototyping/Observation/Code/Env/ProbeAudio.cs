using UnityEngine;
using Aqua;
using AquaAudio;
using Aqua.Scripting;
using BeauUtil;
using System.Collections;
using BeauRoutine;
using Leaf.Runtime;

namespace ProtoAqua.Observation {
    public class ProbeAudio : ScriptComponent {
        [SerializeField] private ScannableRegion m_Scannable = null;
        [SerializeField] private PatternAudio m_Pattern = null;

        private void Start() {
            if (m_Pattern != null) {
                Script.OnSceneLoad(OnLoad);
                m_Scannable.OnScanComplete += OnScanComplete;
            }
        }

        private void OnLoad() {
            StringHash32 scanId = m_Scannable.ScanId;
            if (Save.Inventory.HasUpgrade(ItemIds.ProbeHacker) && Save.Inventory.WasScanned(scanId)) {
                m_Pattern.Play();
            }
        }

        private void OnScanComplete(ScanResult result) {
            if ((result & ScanResult.NewScan) != 0 && Save.Inventory.HasUpgrade(ItemIds.ProbeHacker)) {
                Routine.Start(this, StartPattern());
            }
        }

        private IEnumerator StartPattern() {
            yield return RNG.Instance.Next(6, 9);
            m_Pattern.Play();
        }
    }
}