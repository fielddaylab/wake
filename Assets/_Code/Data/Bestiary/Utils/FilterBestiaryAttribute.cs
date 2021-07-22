using UnityEngine;

namespace Aqua
{
    public class FilterBestiaryAttribute : PropertyAttribute
    {
        public BestiaryDescCategory Category { get; private set; }

        public FilterBestiaryAttribute(BestiaryDescCategory inCategory)
        {
            Category = inCategory;
        }
    }
}