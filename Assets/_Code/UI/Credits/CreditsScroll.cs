using System;
using System.Collections;
using Aqua.Character;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using BeauPools;

namespace Aqua {
    public class CreditsScroll : MonoBehaviour, IEditorOnlyData {
        #region Types

        [Serializable]
        private struct CreditsItem {
            public TextId Header;
            public TextId ColumnA;
            public TextId ColumnB;
            public bool UseSmallText;
        }

        #endregion // Types

        #region Inspector

        [Header("Pools")]
        [SerializeField] private CreditsElement.Pool m_HeaderPool = null;
        [SerializeField] private CreditsElement.Pool m_SingleColumnPool = null;
        [SerializeField] private CreditsElement.Pool m_SingleColumnSmallPool = null;
        [SerializeField] private CreditsElement.Pool m_DoubleColumnPool = null;
        
        [Header("Fixed Elements")]
        [SerializeField] private CreditsElement m_Logo = null;
        [SerializeField] private CreditsElement m_Thanks = null;
        [SerializeField] private RectTransform m_Container = null;
        [SerializeField] private float m_Speed = 4;
        [SerializeField] private float m_SectionSpacing = 32;

        [Header("Text")]
        [SerializeField, Multiline] private string m_Sections = null;
        [SerializeField] private CreditsItem[] m_Items = null;
        [SerializeField] private bool m_Looping = true;
        [SerializeField] private bool m_AllowFastForward = true;

        #endregion // Inspector

        [NonSerialized] private RingBuffer<CreditsElement> m_SpawnedItems = new RingBuffer<CreditsElement>(24);
        [NonSerialized] private float m_Top;
        [NonSerialized] private float m_Bottom;
        [NonSerialized] private float m_SpawnPos;
        [NonSerialized] private Routine m_Routine;

        private readonly Action<CreditsElement> MoveAction;

        public Action OnCompleted;

        private CreditsScroll() {
            MoveAction = (e) => e.Transform.Translate(0, m_Speed * Routine.DeltaTime, 0, Space.Self);
        }

        private void Awake() {
            m_Top = m_Container.rect.height / 2 + 8;
            m_Bottom = -m_Top;
            m_SpawnPos = m_Bottom - m_SectionSpacing;
        }

        private void OnEnable() {
            if (m_Logo != null) {
                m_Logo.Hide();
            }
            if (m_Thanks != null) {
                m_Thanks.Hide();
            }
            m_Routine.Replace(this, SpawnRoutine());
        }

        private void OnDisable() {
            m_Routine.Stop();
            m_SpawnedItems.Clear();
            
            if (m_Logo) {
                m_Logo.Hide();
            }
            if (m_Thanks) {
                m_Thanks.Hide();
            }
            
            Async.InvokeAsync(() => {
                if (!this) {
                    return;
                }
                m_HeaderPool.Reset();
                m_DoubleColumnPool.Reset();
                m_SingleColumnPool.Reset();
                m_SingleColumnSmallPool.Reset();
            });
        }

        private void LateUpdate() {
            if (!m_AllowFastForward) {
                return;
            }
            
            if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0)) {
                m_Routine.SetTimeScale(3);
            } else if (Input.GetKey(KeyCode.LeftShift) || Input.GetMouseButton(1)) {
                m_Routine.SetTimeScale(0.5f);
            } else {
                m_Routine.SetTimeScale(1);
            }
        }

        private void MoveAllItems() {
            m_SpawnedItems.ForEach(MoveAction);
            CheckTopItem();
        }

        private void CheckTopItem() {
            CreditsElement element;
            if (m_SpawnedItems.TryPeekFront(out element)) {
                float bottomY = element.Transform.anchoredPosition.y - element.Transform.rect.height;
                if (bottomY > m_Top) {
                    m_SpawnedItems.PopFront();
                    element.Hide();
                }
            }
        }

        private bool CheckBottomItem() {
            CreditsElement element;
            if (m_SpawnedItems.TryPeekBack(out element)) {
                float bottomY = element.Transform.anchoredPosition.y - element.Transform.rect.height;
                return bottomY > m_Bottom;
            }

            return true;
        }

        private IEnumerator SpawnRoutine() {
            while(true) {
                int i = 0;
                if (m_Logo != null) {
                    SpawnItem(m_Logo, null, null, 0);
                }
                while(i < m_Items.Length) {
                    while(!CheckBottomItem()) {
                        MoveAllItems();
                        yield return null;
                    }
                    SpawnItems(m_Items[i]);
                    i++;
                    yield return null;
                }
                while(m_SpawnedItems.Count > 0) {
                    MoveAllItems();
                    yield return null;
                }
                if (m_Thanks != null) {
                    SpawnItem(m_Thanks, null, null, 0);
                    while(m_SpawnedItems.Count > 0) {
                        MoveAllItems();
                        yield return null;
                    }
                }
                yield return 0.5f;
                OnCompleted?.Invoke();
                
                if (!m_Looping) {
                    yield break;
                }
            }
        }

        private void SpawnItems(CreditsItem item) {
            float offset = 0;
            if (!item.Header.IsEmpty) {
                offset = SpawnItem(m_HeaderPool, item.Header, null, offset);
            }
            if (!item.ColumnB.IsEmpty) {
                offset = SpawnItem(m_DoubleColumnPool, item.ColumnA, item.ColumnB, offset);
            } else if (!item.ColumnA.IsEmpty) {
                offset = SpawnItem(item.UseSmallText ? m_SingleColumnSmallPool : m_SingleColumnPool, item.ColumnA, null, offset);
            }
        }

        private float SpawnItem(IPool<CreditsElement> pool, TextId a, TextId b, float offset) {
            return SpawnItem(pool.Alloc(), a, b, offset);
        }

        private float SpawnItem(CreditsElement element, TextId a, TextId b, float offset) {
            float y = m_SpawnPos + offset;
            element.Transform.anchoredPosition = new Vector2(0, y);
            if (!a.IsEmpty) {
                Assert.True(element.Text.Length >= 1);
                element.Text[0].SetText(a);
            }
            if (!b.IsEmpty) {
                Assert.True(element.Text.Length >= 2);
                element.Text[1].SetText(b);
            }
            element.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(element.Transform);
            m_SpawnedItems.PushBack(element);
            return offset - element.Transform.rect.height - 8;
        }

        #if UNITY_EDITOR

        [ContextMenu("Generate Items")]
        private void AutoGenerateItems() {
            StringSlice[] sections = StringSlice.Split(m_Sections, StringUtils.DefaultNewLineChars, StringSplitOptions.RemoveEmptyEntries);
            m_Items = new CreditsItem[sections.Length];
            for(int i = 0; i < sections.Length; i++) {
                ref CreditsItem item = ref m_Items[i];
                StringHash32 header, names, names1, names2;
                header = "credits." + sections[i];
                names = header.Concat(".names");
                names1 = header.Concat(".names1");
                names2 = header.Concat(".names2");
                item.Header = header.ToDebugString();

                if (Loc.Find(names1) != null) {
                    item.ColumnA = names1.ToDebugString();
                    if (Loc.Find(names2) != null) {
                        item.ColumnB = names2.ToDebugString();
                    }
                } else {
                    item.ColumnA = names.ToDebugString();
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }

        void IEditorOnlyData.ClearEditorOnlyData()
        {
            m_Sections = null;
        }

        #endif // UNITY_EDITOR
    }
}