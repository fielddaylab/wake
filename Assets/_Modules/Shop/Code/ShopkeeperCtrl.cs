using System;
using System.Collections;
using Aqua.Scripting;
using BeauRoutine;
using UnityEngine;

namespace Aqua.Shop {
    public class ShopkeeperCtrl : MonoBehaviour {

        #region Inspector

        [SerializeField] private SpriteRenderer m_Renderer = null;
        [SerializeField] private Transform m_RendererTransform = null;
        [SerializeField] private float m_StoolHeight = 1.6f;
        [SerializeField] private float m_StoolDistance = 1f;
        [SerializeField] private float m_MovementSpeed = 6;

        #endregion // Inspector

        [NonSerialized] private Vector3 m_OriginalPos;
        [NonSerialized] private float m_OriginalRendererYOffset;
        [NonSerialized] private Transform m_SittingStool;
        [NonSerialized] private ShopTable m_CurrentTable;
        [NonSerialized] private Routine m_MoveRoutine;

        private void Awake() {
            m_OriginalPos = transform.position;
            m_OriginalRendererYOffset = m_RendererTransform.localPosition.y;

            GetComponent<ScriptInspectable>().OnInspect = (_) => OnTalkToShopkeep();
        }

        public void SetTable(ShopTable table) {
            if (m_CurrentTable == table) {
                return;
            }

            m_CurrentTable = table;
            if (!table) {
                m_MoveRoutine.Replace(this, MoveToCenter());
            } else {
                m_MoveRoutine.Replace(this, MoveToTable(table));
            }
        }

        private IEnumerator MoveToTable(ShopTable table) {
            yield return DismountStool(table.Stool.position.x);
            yield return MoveNextToStool(table.Stool);
            yield return MountStool(table.Stool, table.StoolFaceLeft);
        }

        private IEnumerator MoveToCenter() {
            yield return DismountStool(m_OriginalPos.x);
            yield return MoveToPosition(m_OriginalPos.x, 0);
        }

        private IEnumerator MoveNextToStool(Transform stool) {
            return MoveToPosition(stool.position.x, m_StoolDistance);
        }

        private IEnumerator MoveToPosition(float destinationX, float offsetX) {
            bool left = destinationX < transform.position.x;
            m_Renderer.flipX = left;
            if (left) {
                destinationX += offsetX;
            } else {
                destinationX -= offsetX;
            }

            float dist = Math.Abs(destinationX - transform.position.x);

            yield return transform.MoveTo(destinationX, dist / m_MovementSpeed, Axis.X, Space.World);
        }

        private IEnumerator MountStool(Transform stool, bool faceLeft) {
            m_Renderer.flipX = faceLeft;
            m_SittingStool = stool;
            yield return 0.1f;
            yield return Routine.Combine(
                transform.MoveTo(stool.position, 0.2f, Axis.X, Space.World).Ease(Curve.Smooth).DelayBy(0.05f),
                m_RendererTransform.MoveTo(m_OriginalRendererYOffset + m_StoolHeight, 0.2f, Axis.Y, Space.Self).Ease(Curve.BackOut)
            );
        }

        private IEnumerator DismountStool(float destinationX) {
            if (m_SittingStool) {
                bool left = destinationX < transform.position.x;
                m_Renderer.flipX = left;
                float stoolPos = m_SittingStool.transform.position.x;
                float target = left ? stoolPos - m_StoolDistance : stoolPos + m_StoolDistance;
                yield return Routine.Combine(
                    transform.MoveTo(target, 0.2f, Axis.X, Space.World).Ease(Curve.Smooth),
                    m_RendererTransform.MoveTo(m_OriginalRendererYOffset, 0.2f, Axis.Y, Space.Self).Ease(Curve.BackIn).DelayBy(0.05f)
                );
                m_SittingStool = null;
                yield return 0.1f;
            }
        }

        private void OnTalkToShopkeep() {
            Services.Events.Dispatch(ShopConsts.Event_TalkToShopkeep);
        }
    }
}