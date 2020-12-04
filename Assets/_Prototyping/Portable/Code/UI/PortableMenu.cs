using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using Aqua;

namespace ProtoAqua.Portable
{
    public class PortableMenu : BasePanel
    {
        #region Inspector

        [SerializeField, Required] private Canvas m_Canvas = null;

        [Header("Bottom Buttons")]
        [SerializeField, Required] private Button m_CloseButton = null;
        [Space]
        [SerializeField, Required] private PortableAppButton[] m_AppButtons = null;

        [Header("DEBUG")]
        [SerializeField, Required] private PortableTweaks m_Tweaks = null;

        #endregion // Inspector

        protected override void Awake()
        {
            base.Awake();
            m_CloseButton.onClick.AddListener(() => Hide());
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Services.Tweaks.Load(m_Tweaks);
        }

        protected override void OnDisable()
        {
            Services.Tweaks?.Unload(m_Tweaks);

            base.OnDisable();
        }

        protected override void OnShow(bool inbInstant)
        {
            m_Canvas.enabled = true;
        }

        protected override void OnHideComplete(bool inbInstant)
        {
            m_Canvas.enabled = false;
        }
    }
}