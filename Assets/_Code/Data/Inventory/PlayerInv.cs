using UnityEngine;
using BeauUtil;
using System.Collections.Generic;

namespace Aqua
{
    public class PlayerInv {

        public InvItem Item { get; set; }

        private int m_CurrentValue = 0;

        public PlayerInv(InvItem invItem)
        {
            Item = invItem;
            m_CurrentValue = invItem.Value;
        }

        public int Value() { return m_CurrentValue; }


        public void UpdateItem(int value)
        {

            if ((Item.Value + value) < 0)
            {
                throw new System.Exception("value changed went below 0: " + (Item.Value + value));
            }

            Item.Value = Item.Value + value;

            m_CurrentValue = Item.Value;

            return;

        }

    }
    
}