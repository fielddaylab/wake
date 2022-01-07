using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Model")]
    public class BFModel : BFBase // yes i know models aren't strictly facts in a scientific sense but this fits into our data model
    {
        #region Inspector

        [Header("Model")]
        public Sprite Image = null;
        public TextId HeaderId = default;
        public TextId DescriptionId = default;
        public TextId SentenceId = default;
        [StreamingPath("png,jpg,jpeg,webm,mp4")] public string HighResImagePath = null;

        #endregion // Inspector

        private BFModel() : base(BFTypeId.Model) { }

        #region Behavior

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Model, BFShapeId.Model, 0, BFDiscoveredFlags.All, null);
            BFType.DefineMethods(BFTypeId.Model, null, GenerateSentence, null);
            BFType.DefineEditor(BFTypeId.Model, null, BFMode.Player);
        }

        static private string GenerateSentence(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            BFModel fact = (BFModel) inFact;
            TextId sentenceId = fact.SentenceId;
            if (sentenceId.IsEmpty)
                sentenceId = fact.DescriptionId;
            return Loc.Find(sentenceId);
        }

        #endregion // Behavior
    }
}