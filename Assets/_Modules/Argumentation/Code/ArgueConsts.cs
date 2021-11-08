using BeauUtil;

namespace Aqua.Argumentation {
    static public class ArgueEvents {
        static public readonly StringHash32 Loaded = "argue:loaded"; // StringHash32 id
        static public readonly StringHash32 Unloaded = "argue:unloaded"; // no args
        static public readonly StringHash32 ClaimDisplay = "argue:claimDisplay"; // ArgueData data
        static public readonly StringHash32 ClaimHide = "argue:claimHide"; // no args
        static public readonly StringHash32 ClaimCancelled = "argue:claimCancelled"; // no args
        static public readonly StringHash32 FactSubmitted = "argue:factSubmitted"; // StringHash32 factId
        static public readonly StringHash32 FactRejected = "argue:factRejected"; // StringHash32 factId
        static public readonly StringHash32 FactsCleared = "argue:factsCleared"; // no args
        static public readonly StringHash32 FactsRefreshed = "argue:factsRefreshed"; // no args
        static public readonly StringHash32 Completed = "argue:completed"; // StringHash32 id
    }

    static public class ArgueConsts {
        public const int MaxFactsPerClaim = ArgueData.MaxFactsPerClaim;
    }
}