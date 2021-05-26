using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Model")]
    public class BFModel : BFBase // yes i know models aren't strictly facts in a scientific sense but this fits into our data model
    {
        #region Inspector

        [Header("Data")]
        [SerializeField, Required] private BestiaryDesc m_Environment = null;

        [Header("Text")]
        [SerializeField] private TextId m_NameId = null;
        [SerializeField] private TextId m_DescriptionId = null;
        [SerializeField] private TextId m_SentenceId = null;

        #endregion // Inspector

        public BestiaryDesc Environment() { return m_Environment; }

        public TextId NameId() { return m_NameId; }
        public TextId DescriptionId() { return m_DescriptionId; }

        public override string GenerateSentence()
        {
            return Loc.Find(m_SentenceId);
        }

        public override void Accept(IFactVisitor inVisitor)
        {
            inVisitor.Visit(this);
        }
    }
}