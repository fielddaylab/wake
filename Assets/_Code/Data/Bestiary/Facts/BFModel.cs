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

        [SerializeField] private TextId m_Name = null;
        [SerializeField] private TextId m_Description = null;

        #endregion // Inspector

        public TextId Name() { return m_Name; }
        public TextId Description() { return m_Description; }

        public override string GenerateSentence()
        {
            return Loc.Find(m_Description);
        }
    }
}