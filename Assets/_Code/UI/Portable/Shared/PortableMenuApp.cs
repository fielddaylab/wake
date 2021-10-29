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

        protected override void Awake()
        {
            base.Awake();

            m_ParentMenu = GetComponentInParent<PortableMenu>();
        }

        protected override void OnShow(bool inbInstant)
        {
            base.OnShow(inbInstant);

            Services.Data.SetVariable("portable:app", m_Id.ToString());
            Services.Data.SetVariable(PortableMenu.Var_LastOpenTab, (int) m_Id);
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