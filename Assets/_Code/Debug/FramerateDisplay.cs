using UnityEngine;
using TMPro;
using System.Text;
using BeauUtil;
using System;
using System.Diagnostics;

namespace Aqua.Debugging {
    public class FramerateDisplay : MonoBehaviour {
        public TMP_Text Counter;
        public float TargetFramerate = 60;
        public int AveragingFrames = 8;
        public Vector2 BuildOffset;

        private StringBuilder m_TextBuilder = new StringBuilder();
        [NonSerialized] private long m_FrameAccumulation;
        [NonSerialized] private int m_FrameCount;
        [NonSerialized] private long m_LastTimestamp;

        static private FramerateDisplay s_Instance;
        static private bool s_AlreadyAccessed = false;

        public void Awake() {
            s_Instance = this;
        }

        public void Start() {
            if (!s_AlreadyAccessed) {
                gameObject.SetActive(false);
            }

            if (!Application.isEditor) {
                GetComponent<RectTransform>().anchoredPosition += BuildOffset;
            }
        }

        public void OnDisable() {
            m_FrameAccumulation = 0;
            m_FrameCount = 0;
        }

        public void OnDestroy() {
            s_Instance = null;
        }

        public void LateUpdate() {
            long currentTS = Stopwatch.GetTimestamp();
            if (m_LastTimestamp != 0) {
                m_FrameAccumulation += currentTS - m_LastTimestamp;
                m_FrameCount++;
                if (m_FrameCount >= AveragingFrames) {
                    double framerate = m_FrameCount * (double) Stopwatch.Frequency / m_FrameAccumulation;
                    m_TextBuilder.Clear().AppendNoAlloc(framerate, 1, 0);
                    Counter.SetText(m_TextBuilder);
                    m_FrameAccumulation = 0;
                    m_FrameCount = 0;
                }
            }
            m_LastTimestamp = currentTS;
        }

        static private FramerateDisplay GetInstance() {
            if (s_Instance == null) {
                s_Instance = FindObjectOfType<FramerateDisplay>();
            }
            return s_Instance;
        }

        static public void Show() {
            s_AlreadyAccessed = true;
            GetInstance().gameObject.SetActive(true);
        }

        static public void Hide() {
            s_AlreadyAccessed = true;
            GetInstance().gameObject.SetActive(false);
        }
    }
}