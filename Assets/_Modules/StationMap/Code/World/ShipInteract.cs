using UnityEngine;
using BeauUtil;
using Aqua.Scripting;

namespace Aqua.StationMap
{
    public class ShipInteract : ScriptComponent
    {
        #region Inspector

        [Header("Components")]
        [SerializeField, Required] private Collider2D m_Collider = null;

        [Header("Text")]
        [SerializeField] private TextId m_InteractText = "ui.nav.examine";
        [SerializeField] private TextId m_TooltipText = "ui.nav.examine.tooltip";

        #endregion // Inspector
        
        private void Awake()
        {
            WorldUtils.ListenForPlayer(m_Collider, OnPlayerExit, OnPlayerExit);
        }

        private void OnPlayerEnter(Collider2D other)
        {
            if (Services.Data.CompareExchange(GameVars.InteractObject, null, Parent.Id()))
            {
                Services.UI.FindPanel<NavigationUI>().DisplayInspect(Parent, m_InteractText, m_InteractText);
            }
        }

        private void OnPlayerExit(Collider2D other)
        {
            if (Services.Data)
            {
                if (Services.Data.CompareExchange(GameVars.InteractObject, Parent.Id(), null))
                {
                    Services.UI?.FindPanel<NavigationUI>()?.Hide();
                }
            }
        }
    }

}
