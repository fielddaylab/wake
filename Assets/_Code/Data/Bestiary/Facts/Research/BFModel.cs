using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab Content/Fact/Model")]
    public class BFModel : BFBase // yes i know models aren't strictly facts in a scientific sense but this fits into our data model
    {
        #region Inspector

        [Header("Model")]
        public Sprite Image = null;
        public TextId DescriptionId = default;
        public TextId SentenceId = default;

        #endregion // Inspector

        private BFModel() : base(BFTypeId.Model) { }

        #region Behavior

        static public void Configure()
        {
            BFType.DefineAttributes(BFTypeId.Model, BFDiscoveredFlags.All, null);
            BFType.DefineMethods(BFTypeId.Model, null, GenerateSentence, null);
            BFType.DefineEditor(BFTypeId.Model, (Sprite) null, BFMode.Player);
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