using System;
using System.Collections.Generic;
using Aqua;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Portable
{
    [CreateAssetMenu(menuName = "Aqualab/Portable/Portable Tweaks")]
    public class PortableTweaks : TweakAsset
    {        
        [Header("Colors")]
        [SerializeField] private Color m_CritterListColor = ColorBank.Orange;
        [SerializeField] private Color m_EcosystemListColor = ColorBank.Aqua;

        protected override void Apply()
        {
            base.Apply();
        }

        public Color BestiaryListColor(BestiaryDescCategory inCategory)
        {
            switch(inCategory)
            {
                case BestiaryDescCategory.Critter:
                    return m_CritterListColor;
                case BestiaryDescCategory.Ecosystem:
                    return m_EcosystemListColor;

                default:
                    throw new ArgumentOutOfRangeException("inCategory");
            }
        }
    }
}