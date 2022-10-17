using UnityEngine;
using EasyAssetStreaming;

namespace Aqua {
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Model")]
    public class BFModel : BFBase, IEditorOnlyData // yes i know models aren't strictly facts in a scientific sense but this fits into our data model
    {
        static private readonly TextId[] DefaultHeaders = new TextId[] {
            "fact.visualModel.header", "fact.descriptiveModel.header", "fact.predictionModel.header", "fact.interveneModel.header", ""
        };

        #region Inspector

        [Header("Model")]
        public BFModelType ModelType = BFModelType.Visual;
        [StreamingImagePath] public string HighResImagePath = null;
        public TextId HeaderId = default;
        public TextId DescriptionId = default;
        public TextId SentenceId = default;

        #endregion // Inspector

        private BFModel() : base(BFTypeId.Model) { }

        #region Behavior

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Model, BFShapeId.Model, BFFlags.HideFactInDetails, BFDiscoveredFlags.All, Compare);
            BFType.DefineMethods(BFTypeId.Model, null, GenerateDetails, null, null, null);
            BFType.DefineEditor(BFTypeId.Model, null, BFMode.Player);
        }

        static private int Compare(BFBase x, BFBase y)
        {
            BFModel modelX = (BFModel) x;
            BFModel modelY = (BFModel) y;

            int typeCompare = (int) modelX.ModelType - (int) modelY.ModelType;
            if (typeCompare < 0)
                return -1;
            if (typeCompare > 0)
                return 1;

            return x.Id.CompareTo(y.Id);
        }

        static private BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            BFModel fact = (BFModel) inFact;

            BFDetails details;
            TextId header = fact.HeaderId;
            if (header.IsEmpty)
                header = DefaultHeaders[(int) fact.ModelType];
            details.Header = Loc.Find(header);

            details.Image = new StreamedImageSet(fact.HighResImagePath, fact.Icon);

            TextId sentenceId = fact.SentenceId;
            if (sentenceId.IsEmpty)
                sentenceId = fact.DescriptionId;
            details.Description = Loc.Find(sentenceId);

            return details;
        }

        #endregion // Behavior

        #if UNITY_EDITOR

        void IEditorOnlyData.ClearEditorOnlyData() {
            ValidationUtils.StripDebugInfo(ref HeaderId);
            ValidationUtils.StripDebugInfo(ref DescriptionId);
            ValidationUtils.StripDebugInfo(ref SentenceId);
        }

        #endif // UNITY_EDITOR
    }

    public enum BFModelType : byte
    {
        Visual,
        Descriptive,
        Prediction,
        Intervention,
        Custom
    }
}