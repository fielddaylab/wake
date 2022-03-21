using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using System;
using Aqua.Scripting;

namespace Aqua.Portable
{
    public class PortableMenuApp : BasePanel
    {
        #region Inspector

        [Header("Portable App")]
        [SerializeField] private PortableAppId m_Id = default;

        #endregion // Inspector

        [NonSerialized] protected PortableMenu m_ParentMenu;

        public PortableAppId Id() { return m_Id; }

        public virtual void HandleRequest(PortableRequest inRequest)
        {
        }

        public virtual void ClearRequest()
        {
        }

        protected override void Awake()
        {
            base.Awake();

            m_ParentMenu = GetComponentInParent<PortableMenu>();
        }

        protected override void OnShow(bool inbInstant)
        {
            base.OnShow(inbInstant);

            Script.WriteVariable("portable:app", m_Id.ToString());
            Services.Events.Dispatch(GameEvents.PortableAppOpened, m_Id);
        }

        protected override void OnShowComplete(bool inbInstant)
        {
            base.OnShowComplete(inbInstant);

            using(var table = TempVarTable.Alloc())
            {
                table.Set("appId", m_Id.ToString());
                Services.Script.TriggerResponse(GameTriggers.PortableAppOpened, table);
            }
        }

        protected override void OnHide(bool inbInstant)
        {
            Services.Data?.CompareExchange("portable:app", m_Id.ToString(), null);
            Services.Events?.Dispatch(GameEvents.PortableAppClosed, m_Id);

            base.OnHide(inbInstant);
        }
    }
}