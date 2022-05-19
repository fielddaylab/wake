using System;
using BeauRoutine.Extensions;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua.Ship {
    public sealed class ShipRoomMap : BasePanel {

        [NonSerialized] private BaseInputLayer m_Input;

        protected override void Awake() {
            m_Input = BaseInputLayer.Find(this);
        }

        protected override void OnShow(bool inbInstant) {
            base.OnShow(inbInstant);

            m_Input.PushPriority();
        }

        protected override void OnHide(bool inbInstant) {
            base.OnHide(inbInstant);

            m_Input?.PopPriority();
        }
    }
}