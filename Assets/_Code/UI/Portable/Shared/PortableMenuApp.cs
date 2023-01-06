using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using System;
using Aqua.Scripting;
using System.Collections;

namespace Aqua.Portable
{
    public class PortableMenuApp : BasePanel, IAsyncLoadPanel
    {
        static private readonly string[] PortableAppIdToString = Enum.GetNames(typeof(PortableAppId));

        #region Inspector

        [Header("Portable App")]
        [SerializeField] private PortableAppId m_Id = default;

        #endregion // Inspector

        [NonSerialized] protected PortableMenu m_ParentMenu;
        [NonSerialized] private Routine m_LoadRoutine;

        public PortableAppId Id() { return m_Id; }

        public virtual void HandleRequest(PortableRequest inRequest)
        {
        }

        public virtual void ClearRequest()
        {
        }

        public bool IsLoading()
        {
            return m_LoadRoutine;
        }

        protected virtual IEnumerator LoadData()
        {
            return null;
        }

        protected override void Awake()
        {
            base.Awake();

            m_ParentMenu = GetComponentInParent<PortableMenu>();
            m_RootGroup.alpha = 0;
        }

        protected override void OnShow(bool inbInstant)
        {
            base.OnShow(inbInstant);

            Script.WriteVariable("portable:app", PortableAppIdToString[(int) m_Id]);
            Services.Events.Dispatch(GameEvents.PortableAppOpened, m_Id);

            IEnumerator dataLoad = LoadData();
            if (dataLoad != null) {
                SetInputState(false);
                m_LoadRoutine.Replace(this, dataLoad).OnComplete(OnLoadComplete);
            }
        }

        protected override IEnumerator TransitionToShow() {
            m_RootTransform.gameObject.SetActive(true);
            m_RootGroup.alpha = 0;
            if (m_LoadRoutine) {
                yield return m_LoadRoutine;
            }
            m_RootGroup.alpha = 1;
        }

        protected override void OnShowComplete(bool inbInstant)
        {
            base.OnShowComplete(inbInstant);

            if (m_LoadRoutine) {
                SetInputState(false);
            }
        }

        protected override void OnHide(bool inbInstant)
        {
            m_LoadRoutine.Stop();
            m_RootGroup.alpha = 0;

            Services.Data?.CompareExchange("portable:app", PortableAppIdToString[(int) m_Id], null);
            Services.Events?.Dispatch(GameEvents.PortableAppClosed, m_Id);

            base.OnHide(inbInstant);
        }

        private void OnLoadComplete() {
            SetInputState(true);

            if (IsShowing()) {
                using(var table = TempVarTable.Alloc())
                {
                    table.Set("appId", PortableAppIdToString[(int) m_Id]);
                    Services.Script.TriggerResponse(GameTriggers.PortableAppOpened, table);
                }
            }
        }
    }
}