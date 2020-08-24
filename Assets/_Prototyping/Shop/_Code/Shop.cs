using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using BeauData;

namespace ProtoAqua.Shop
{
    public class Shop : MonoBehaviour
    {
        #region Dependencies

        [Header("Shop Dependencies")]
        [SerializeField] private PlayerData PlayerData;
        [SerializeField] private Transform ItemButton;
        [SerializeField] private Transform Group;
        [SerializeField] private TextMeshProUGUI Currency;
        [SerializeField] private NPC NPC;

        [Header("Details Panel Dependencies")]
        [SerializeField] private TextMeshProUGUI ItemName;
        [SerializeField] private TextMeshProUGUI ItemDescription;
        [SerializeField] private TextMeshProUGUI ItemPrice;
        [SerializeField] private Image ItemImage;
        [SerializeField] private TextMeshProUGUI PurchaseButtonText;

        [Header("Button Dependencies")]
        [SerializeField] private Button PurchaseButton;
        [SerializeField] private Button AvailableItemsButton;
        [SerializeField] private Button PurchasedItemsButton;
        [SerializeField] private Button CloseButton;

        [Header("Animation Dependencies")]
        [SerializeField] private Animator DetailsPanelAnimator;

        #endregion // Dependencies

        private int PlayerCurrency;
        private List<Item> PlayerInventory;

        private List<GameObject> AvailableItems = new List<GameObject>();
        private List<GameObject> PurchasedItems = new List<GameObject>();

        private void Start()
        {
            PlayerCurrency = PlayerData.PlayerCurrency;
            PlayerInventory = PlayerData.PlayerInventory;

            Currency.SetText(PlayerCurrency.ToString());

            AvailableItemsButton.onClick.AddListener(() => ToggleItems(PurchasedItems, AvailableItems));
            PurchasedItemsButton.onClick.AddListener(() => ToggleItems(AvailableItems, PurchasedItems));
            CloseButton.onClick.AddListener(() => HideDetails());

            Populate();
        }

        private void Populate()
        {
            string path = Path.Combine(Application.dataPath, "_Prototyping/Shop/_Code/Items.json");
            string json = File.ReadAllText(path);
            ItemSet itemSet = Serializer.Read<ItemSet>(json);

            foreach (Item item in itemSet.ItemData)
            {
                item.ItemSprite = (Sprite)AssetDatabase.LoadAssetAtPath(item.SpritePath, typeof(Sprite));

                Transform itemButtonTransform = Instantiate(ItemButton, Group);

                itemButtonTransform.Find("Name").GetComponent<TextMeshProUGUI>().SetText(item.Name);
                itemButtonTransform.Find("Price").GetComponent<TextMeshProUGUI>().SetText(item.Price.ToString());

                itemButtonTransform.Find("Image").GetComponent<Image>().sprite = item.ItemSprite;

                itemButtonTransform.GetComponentInChildren<Button>().onClick.AddListener(
                    () => ShowDetails(item, itemButtonTransform.gameObject));

                AvailableItems.Add(itemButtonTransform.gameObject);
            }
        }

        private void ShowDetails(Item item, GameObject button)
        {
            PurchaseButton.onClick.RemoveAllListeners();

            ItemName.SetText(item.Name);
            ItemDescription.SetText(item.Description);
            ItemPrice.SetText(item.Price.ToString());
            ItemImage.sprite = item.ItemSprite;

            if (!item.IsAvailable)
            {
                PurchaseButtonText.SetText("Purchased");
                PurchaseButton.GetComponent<Image>().color = Color.grey;
            }
            else
            {
                PurchaseButtonText.SetText("Purchase");
                PurchaseButton.GetComponent<Image>().color = Color.white;
                PurchaseButton.onClick.AddListener(() => Purchase(item, button));
            }

            DetailsPanelAnimator.SetBool("Open", true);
        }

        private void HideDetails()
        {
            DetailsPanelAnimator.SetBool("Open", false);
            NPC.WelcomeDialog();
        }

        private void Purchase(Item item, GameObject button)
        {
            if (PlayerCurrency < item.Price)
            {
                NPC.NeedMoreCurrencyDialog();
                PurchaseButton.GetComponent<Image>().color = Color.red;
                return;
            }

            PlayerInventory.Add(item);
            PlayerData.PlayerInventory = PlayerInventory;

            PlayerCurrency -= item.Price;
            PlayerData.PlayerCurrency = PlayerCurrency;

            item.IsAvailable = false;

            Currency.SetText(PlayerCurrency.ToString());

            PurchaseButtonText.SetText("Purchased");
            PurchaseButton.GetComponent<Image>().color = Color.grey;
            PurchaseButton.onClick.RemoveAllListeners();

            AvailableItems.Remove(button);
            PurchasedItems.Add(button);

            button.SetActive(false);

            NPC.PurchasedDialog(item);
        }

        private void ToggleItems(List<GameObject> hide, List<GameObject> show)
        {
            foreach (GameObject button in hide)
            {
                button.SetActive(false);
            }

            foreach (GameObject button in show)
            {
                button.SetActive(true);
            }
        }
    }
}
