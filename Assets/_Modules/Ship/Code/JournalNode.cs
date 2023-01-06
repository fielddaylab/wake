using System;
using Aqua.Scripting;
using Aqua.View;
using BeauRoutine.Extensions;
using UnityEngine;

namespace Aqua.Ship
{
    [RequireComponent(typeof(ViewNode))]
    public class JournalNode : ScriptComponent {
        [SerializeField] private ViewNode m_BackNode = null;

        [NonSerialized] private JournalCanvas m_JournalCanvas;

        private void Awake() {
            ViewNode node = GetComponent<ViewNode>();

            node.OnEnter = () => {
                if (!Services.Script.IsCutscene()) {
                    m_JournalCanvas.Show();
                }
            };
            node.OnExit = () => {
                m_JournalCanvas.Hide();
            };

            Script.OnSceneLoad(() => {
                m_JournalCanvas = Services.UI.FindPanel<JournalCanvas>();
                m_JournalCanvas.OnHideCompleteEvent.AddListener(OnJournalClosed);
            });
        }

        private void OnDestroy() {
            if (m_JournalCanvas != null) {
                m_JournalCanvas.OnHideCompleteEvent.RemoveListener(OnJournalClosed);
            }
        }

        private void OnJournalClosed(BasePanel.TransitionType _) {
            ViewManager.Find<ViewManager>()?.GoToNode(m_BackNode);
        }
    }
}