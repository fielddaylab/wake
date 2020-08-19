using System.Collections.Generic;
using UnityEngine;

namespace ProtoAqua.Shop
{
    public class Shop : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerData m_PlayerData;
        [SerializeField] private ItemPanel m_ItemPanel;

        private List<Item> m_Items;
        private int m_PlayerCurrency;
        private List<Item> m_PlayerInventory;

        private void OnEnable() 
        {
            m_PlayerCurrency = m_PlayerData.PlayerCurrency;
            m_PlayerInventory = m_PlayerData.PlayerInventory;

            Populate();
        }

        // TODO: Take input and populate m_Items
        private void Populate() 
        { 
            for (int i = 0; i < m_Items.Count; i++)
            {
                m_ItemPanel.CreateItemPanel(m_Items[i], i);
            }
        }

        private void Purchase(Item item)
        {
            m_PlayerInventory.Add(item);
            m_PlayerData.PlayerInventory = m_PlayerInventory;

            m_PlayerCurrency -= item.Price;
            m_PlayerData.PlayerCurrency = m_PlayerCurrency;

            item.IsAvailable = true;
        }
    }
}
