using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using Leaf.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting;

namespace Aqua {
    static public partial class Assets {
        [LeafMember("ItemCost"), Preserve]
        static private int LeafItemCost(StringHash32 itemId) {
            return Assets.Item(itemId).CashCost();
        }
    }
}