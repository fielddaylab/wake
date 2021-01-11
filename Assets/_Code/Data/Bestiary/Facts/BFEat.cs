using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Bestiary/Fact/Behavior/Eats")]
    public class BFEat : BFBehavior
    {
        #region Inspector

        [Header("Eating")]
        [SerializeField] private BestiaryDesc m_TargetEntry = null;
        [SerializeField] private uint m_Amount = 0;
        [SerializeField] private QualitativeMapping m_QualMap = null;

        #endregion // Inspector

        [NonSerialized] private QualitativeValue m_QualAmount = QualitativeValue.None;

        public override void Accept(IFactVisitor inVisitor, PlayerFactParams inParams = null)
        {
            inVisitor.Visit(this, inParams);
        }

        public override IEnumerable<BestiaryFactFragment> GenerateFragments(PlayerFactParams inParams = null)
        {
            yield return BestiaryFactFragment.CreateNoun(Parent().CommonName());
            yield return BestiaryFactFragment.CreateVerb("Eats");
            yield return BestiaryFactFragment.CreateNoun(m_TargetEntry.CommonName());
        }

        public override string GenerateSentence(PlayerFactParams inParams = null)
        {
            // TODO: localization!!!

            using(var psb = PooledStringBuilder.Create())
            {
                // TODO: Variants

                psb.Builder.Append(Services.Loc.MaybeLocalize(Parent().CommonName()))
                    .Append(" eats ")
                    .Append(Services.Loc.MaybeLocalize(m_TargetEntry.CommonName()));

                return psb.Builder.Flush();
            }
        }

        public override bool IsIdentitical(PlayerFactParams inParams1, PlayerFactParams inParams2)
        {
            throw new System.NotImplementedException();
        }

        public override bool HasSameSlot(BFBehavior inBehavior)
        {
            BFEat eat = inBehavior as BFEat;
            if (eat != null)
                return eat.m_TargetEntry == m_TargetEntry;

            return false;
        }

        protected override void GenerateQualitative()
        {
            base.GenerateQualitative();

            m_QualAmount = m_QualMap.Closest(m_Amount);
        }

        #if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();

            if (!m_QualMap)
                m_QualMap = UnityEditor.AssetDatabase.LoadAssetAtPath<QualitativeMapping>("Assets/_Assets/Data/QualitativeMappings/_Default.asset");
        }

        #endif // UNITY_EDITOR
    }
}