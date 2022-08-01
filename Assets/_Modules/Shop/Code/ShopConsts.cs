using BeauUtil;

namespace Aqua.Shop {
    static public class ShopConsts {
        static public readonly StringHash32 Trigger_AttemptBuy = "ShopAttemptBuy";

        static public readonly StringHash32 Trigger_OpenMenu = "ShopOpenMenu";
        static public readonly StringHash32 Trigger_OpenScience = "ShopOpenScience";
        static public readonly StringHash32 Trigger_OpenExploration = "ShopOpenExploration";
        static public readonly StringHash32 Trigger_Close = "ShopClose";

        static public readonly StringHash32 Event_InsufficientFunds = "shop:insufficient-funds";
        static public readonly StringHash32 Event_TalkToShopkeep = "shop:talk-to-shopkeep";
    }
}
