using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProtoAqua.Shop
{
    public class NPC : MonoBehaviour
    {
        [Header("NPC Dependencies")]
        [SerializeField] private TextMeshProUGUI NPCText;
        [SerializeField] private Button NPCButton;

        private void Awake()
        {
            NPCButton.onClick.AddListener(() => ToggleDialog());
        }

        public void WelcomeDialog()
        {
            NPCText.SetText("Welcome!");
        }

        public void PurchasedDialog(Item item)
        {
            NPCText.SetText("Purchased " + item.Name);
        }

        public void NeedMoreCurrencyDialog()
        {
            NPCText.SetText("Need more currency!");
        }

        // Temporary, for testing interactivity until actual dialog is in place
        public void ToggleDialog()
        {
            if (NPCText.text.Equals("Welcome!"))
            {
                NPCText.SetText("Hello!");
            }
            else
            {
                NPCText.SetText("Welcome!");
            }
        }
    }
}
