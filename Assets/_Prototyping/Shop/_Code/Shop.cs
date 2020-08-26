using System.Collections.Generic;
using System.IO;
using BeauData;
using BeauRoutine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Shop
{
    public class Shop : MonoBehaviour, ISerializerContext
    {
        #region Dependencies

        [Header("Shop Dependencies")]
        [SerializeField] private PlayerData PlayerData;
        [SerializeField] private Transform ItemButton;
        [SerializeField] private Transform Group;
        [SerializeField] private TextMeshProUGUI Currency;
        [SerializeField] private NPC NPC;
        [SerializeField] private Sprite[] SpriteRefs;

        [Header("Details Panel Dependencies")]
        [SerializeField] private RectTransform DetailsPanel;
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

        #endregion // Dependencies

        private const string JSON_PATH = "_Prototyping/Shop/_Code/Items.json";

        private int playerCurrency;
        private List<Item> playerInventory;

        private List<GameObject> availableItems = new List<GameObject>();
        private List<GameObject> purchasedItems = new List<GameObject>();

        private Routine dropRoutine;

        // Initialize variables, add listener functions to buttons, call Populate() to add items,
        // populate spriteDatabase
        private void Start()
        {
            playerCurrency = PlayerData.PlayerCurrency;
            playerInventory = PlayerData.PlayerInventory;

            Currency.SetText(playerCurrency.ToString());

            AvailableItemsButton.onClick.AddListener(() => ToggleItems(purchasedItems, availableItems));
            PurchasedItemsButton.onClick.AddListener(() => ToggleItems(availableItems, purchasedItems));
            CloseButton.onClick.AddListener(() => HideDetails());
            
            Populate();
        }

        #region Load Sprite Assets

        public bool TryResolveAsset<T>(string inId, out T outObject) where T : class
        {
            if (typeof(T) == typeof(Sprite))
            {
                foreach (Sprite sprite in SpriteRefs)
                {
                    if (sprite.name == inId)
                    {
                        outObject = sprite as T;
                        return true;
                    }
                }
            }

            outObject = null;
            return false;
        }

        // Unimplemented for now, as item sprites don't need to be serialized
        public bool TryGetAssetId<Sprite>(Sprite inObject, out string outId) where Sprite : class
        {
            outId = "";
            return false;
        }

        #endregion // Load Sprite Assets

        // Load ItemSet from JSON file, create ItemButtons based on parsed data and populate availableItems
        private void Populate()
        {
            string path = Path.Combine(Application.dataPath, JSON_PATH);
            string json = File.ReadAllText(path);
            ItemSet itemSet = Serializer.Read<ItemSet>(json, Serializer.Format.JSON, this);

            foreach (Item item in itemSet.ItemData)
            {
                Transform itemButtonTransform = Instantiate(ItemButton, Group);

                itemButtonTransform.Find("Name").GetComponent<TextMeshProUGUI>().SetText(item.Name);
                itemButtonTransform.Find("Price").GetComponent<TextMeshProUGUI>().SetText(item.Price.ToString());

                itemButtonTransform.Find("Image").GetComponent<Image>().sprite = item.ItemSprite;

                itemButtonTransform.GetComponentInChildren<Button>().onClick.AddListener(
                    () => ShowDetails(item, itemButtonTransform.gameObject));

                availableItems.Add(itemButtonTransform.gameObject);
            }
        }

        #region Details Panel

        // Assign DetailsPanel components with appropriate information and start dropdown routine
        private void ShowDetails(Item item, GameObject button)
        {
            PurchaseButton.onClick.RemoveAllListeners();

            ItemName.SetText(item.Name);
            ItemDescription.SetText(item.Description);
            ItemPrice.SetText(item.Price.ToString());
            ItemImage.sprite = item.ItemSprite;

            // Set functionality of Purchase button based on item's availability
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

            DetailsPanel.gameObject.SetActive(true);
            dropRoutine.Replace(this, DetailsPanel.AnchorPosTo(-250, 0.75f, Axis.Y).Ease(Curve.CubeOut));
        }

        // Start HideDetailsRoutine and reset NPC dialog
        private void HideDetails()
        {
            dropRoutine.Replace(this, HideDetailsRoutine());
            NPC.SetDialog("Welcome!");
        }

        // Routine for hiding DetailsPanel
        private IEnumerator<Tween> HideDetailsRoutine()
        {
            yield return DetailsPanel.AnchorPosTo(250, 0.75f, Axis.Y).Ease(Curve.CubeOut);
            DetailsPanel.gameObject.SetActive(false);
        }

        #endregion // Details Panel

        // If player has enough currency, perform necessary purchase operations to player and item fields,
        // and move item to purchasedItems
        public void Purchase(Item item, GameObject button)
        {
            // Check if player is able to purchase the item
            if (playerCurrency < item.Price)
            {
                NPC.SetDialog("Need more currency!");
                PurchaseButton.GetComponent<Image>().color = Color.red;
                return;
            }

            playerInventory.Add(item);
            PlayerData.PlayerInventory = playerInventory;

            playerCurrency -= item.Price;
            PlayerData.PlayerCurrency = playerCurrency;

            Currency.SetText(playerCurrency.ToString());

            item.IsAvailable = false;

            PurchaseButtonText.SetText("Purchased");
            PurchaseButton.GetComponent<Image>().color = Color.grey;
            PurchaseButton.onClick.RemoveAllListeners();

            availableItems.Remove(button);
            purchasedItems.Add(button);

            button.SetActive(false);

            NPC.SetDialog($"Purchased {item.Name}");
        }

        // Helper method for switching display between availableItems and purchasedItems
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
