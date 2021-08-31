using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Model")]
    public class BFModel : BFBase // yes i know models aren't strictly facts in a scientific sense but this fits into our data model
    {
        #region Inspector

        [Header("Data")]
        [FilterBestiary(BestiaryDescCategory.Environment)] public BestiaryDesc Environment = null;
        public Sprite Image;

        [Header("Text")]
        public TextId NameId = null;
        public TextId DescriptionId = null;
        public TextId SentenceId = null;

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
            return Loc.Find(fact.SentenceId);
        }

        #endregion // Behavior
    }
}