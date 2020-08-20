using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProtoAqua.Shop
{
    public class Shop : MonoBehaviour
    {
        [Header("Shop Dependencies")]
        [SerializeField] private PlayerData PlayerData;
        [SerializeField] private Transform ItemButton;
        [SerializeField] private Transform Group;
        
        [Header("Text Dependencies")]
        [SerializeField] private TextMeshProUGUI Currency;
        [SerializeField] private TextMeshProUGUI ItemName;
        [SerializeField] private TextMeshProUGUI ItemDescription;
        [SerializeField] private TextMeshProUGUI ItemPrice;
        [SerializeField] private TextMeshProUGUI PurchaseButtonText;

        [Header("Button Dependencies")]
        [SerializeField] private Button PurchaseButton;
        [SerializeField] private Button AvailableItemsButton;
        [SerializeField] private Button PurchasedItemsButton;

        private List<Item> Items = new List<Item>();

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

            Populate();
        }

        // Temporary helper
        private List<Item> CreateItems()
        {
            List<Item> tempItems = new List<Item>();

            for (int i = 0; i < 10; i++)
            {
                Item temp = new Item();
                temp.Name = "test" + i;
                temp.Description = "test" + i;
                temp.Price = 100;
                tempItems.Add(temp);
            }

            return tempItems;
        }

        // TODO: Populate items from JSON input
        private void Populate()
        {
            Items = CreateItems();

            for (int i = 0; i < Items.Count; i++)
            {
                Item item = Items[i];
                Transform itemButtonTransform = Instantiate(ItemButton, Group);

                itemButtonTransform.Find("Name").GetComponent<TextMeshProUGUI>().SetText(item.Name);
                itemButtonTransform.Find("Price").GetComponent<TextMeshProUGUI>().SetText(item.Price.ToString());

                itemButtonTransform.GetComponentInChildren<Button>().onClick.AddListener(() => ShowDetails(item, itemButtonTransform.gameObject));

                AvailableItems.Add(itemButtonTransform.gameObject);
            }
        }

        private void ShowDetails(Item item, GameObject button)
        {
            PurchaseButton.onClick.RemoveAllListeners();

            ItemName.SetText(item.Name);
            ItemDescription.SetText(item.Description);
            ItemPrice.SetText(item.Price.ToString());

            if (!item.IsAvailable) {
                PurchaseButtonText.SetText("Purchased");
            } else if (PlayerCurrency < item.Price) {
                PurchaseButtonText.SetText("Need more currency");
            } else {
                PurchaseButtonText.SetText("Purchase");
                PurchaseButton.onClick.AddListener(() => Purchase(item, button));
            }
        }

        private void Purchase(Item item, GameObject button)
        {
            PlayerInventory.Add(item);
            PlayerData.PlayerInventory = PlayerInventory;

            PlayerCurrency -= item.Price;
            PlayerData.PlayerCurrency = PlayerCurrency;

            item.IsAvailable = false;

            Currency.SetText(PlayerCurrency.ToString());

            PurchaseButtonText.SetText("Owned");
            PurchaseButton.onClick.RemoveAllListeners();

            AvailableItems.Remove(button);
            PurchasedItems.Add(button);
            
            button.SetActive(false);
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
