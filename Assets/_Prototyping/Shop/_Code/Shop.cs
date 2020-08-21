using System.Collections.Generic;
using System.IO;
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
        [SerializeField] private NPC NPC;

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
        [SerializeField] private Button CloseButton;

        [Header("Animation Dependencies")]
        [SerializeField] private Animator DetailsPanelAnimator;

        private List<Item> Items = new List<Item>();

        private int PlayerCurrency;
        private List<Item> PlayerInventory;

        private List<GameObject> AvailableItems = new List<GameObject>();
        private List<GameObject> PurchasedItems = new List<GameObject>();

        private void Awake()
        {
            PlayerCurrency = PlayerData.PlayerCurrency;
            PlayerInventory = PlayerData.PlayerInventory;

            Currency.SetText(PlayerCurrency.ToString());

            AvailableItemsButton.onClick.AddListener(() => ToggleItems(PurchasedItems, AvailableItems));
            PurchasedItemsButton.onClick.AddListener(() => ToggleItems(AvailableItems, PurchasedItems));
            CloseButton.onClick.AddListener(() => DetailsPanelAnimator.SetBool("Open", false));
            
            //TestJsonCreate();
            
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

        // TODO: Properly parse JSON
        private void TestJsonCreate()
        {
            string path = Path.Combine(Application.dataPath, "./_Prototyping/Shop/_Code/Items.json");
            string json = File.ReadAllText(path);
            Debug.Log(json);
            Item item = Item.CreateFromJSON(path);
            Debug.Log(item.IsAvailable);
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

            if (!item.IsAvailable)
            {
                PurchaseButtonText.SetText("Purchased");
            }
            else
            {
                PurchaseButtonText.SetText("Purchase");
                PurchaseButton.onClick.AddListener(() => Purchase(item, button));
            }

            DetailsPanelAnimator.SetBool("Open", true);
        }

        private void Purchase(Item item, GameObject button)
        {
            if (PlayerCurrency < item.Price)
            {
                NPC.NeedMoreCurrencyDialog();
                return;
            }

            PlayerInventory.Add(item);
            PlayerData.PlayerInventory = PlayerInventory;

            PlayerCurrency -= item.Price;
            PlayerData.PlayerCurrency = PlayerCurrency;

            item.IsAvailable = false;

            Currency.SetText(PlayerCurrency.ToString());

            PurchaseButtonText.SetText("Purchased");
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
