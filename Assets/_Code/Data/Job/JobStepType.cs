using BeauUtil;

namespace Aqua
{
    [LabeledEnum]
    public enum JobStepType : byte
    {
        // Player must have all the given bestiary entries
        [Label("Bestiary/Get Bestiary Entry")]
        AcquireBestiaryEntry,

        // Player must have all the given facts
        [Label("Bestiary/Get Bestiary Fact")]
        AcquireFact,

        // Player must have values for all the given facts
        [Label("Bestiary/Upgrade Bestiary Fact")]
        UpgradeFact,

        // Goto a specific scene
        [Label("Map/Go to Scene")]
        GotoScene,

        // Goto a specific station
        [Label("Map/Go to Station")]
        GotoStation,

        // have seen a given script node
        [Label("Script/Visit Script Node")]
        SeeScriptNode,

        // an arbitrary condition string evaluates to true
        [Label("Script/Evaluate Condition")]
        EvaluateCondition,

        // scan object
        [Label("Scan/Scan Object")]
        ScanObject,

        // get item in inventory
        [Label("Inventory/Get Item")]
        GetItem,

        // add fact to food web
        [Label("Bestiary/Add Fact to Visual Modeling Graph")]
        AddFactToModel,

        [Hidden]
        Unknown = 255,
    }
}