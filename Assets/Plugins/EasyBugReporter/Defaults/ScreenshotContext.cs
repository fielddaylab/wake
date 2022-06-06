using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace EasyBugReporter {
    /// <summary>
    /// Captures a screenshot of the game window.
    /// </summary>
    public class ScreenshotContext : IDumpSystem {
        private Texture2D m_Texture;

        public void Freeze() {
            if (m_Texture) {
                GameObject.DestroyImmediate(m_Texture);
                m_Texture = null;
            }
            
            BugReporter.OnEndOfFrame(TakeScreenshot);
        }

        public bool Dump(IDumpWriter writer) {
            if (!m_Texture) {
                return false;
            }

            writer.BeginSection("Screenshot");
            writer.Image(m_Texture, "current screenshot");
            writer.EndSection();

            return true;
        }

        public void Initialize() {
        }

        public void Shutdown() {
            if (m_Texture) {
                GameObject.DestroyImmediate(m_Texture);
                m_Texture = null;
            }
        }

        public void Unfreeze() {
            if (m_Texture) {
                GameObject.DestroyImmediate(m_Texture);
                m_Texture = null;
            }
        }

        private void TakeScreenshot() {
            m_Texture = ScreenCapture.CaptureScreenshotAsTexture();
        }
    }
}