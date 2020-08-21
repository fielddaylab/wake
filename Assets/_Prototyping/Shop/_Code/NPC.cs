using UnityEngine;
using TMPro;

namespace ProtoAqua.Shop
{
    public class NPC : MonoBehaviour
    {
        [Header("NPC Dependencies")]
        [SerializeField] private TextMeshProUGUI NPCText;

        public void PurchasedDialog(Item item)
        {
            NPCText.SetText("Purchased " + item.Name);
        }

        public void NeedMoreCurrencyDialog()
        {
            NPCText.SetText("Need more currency!");
        }
    }
}
