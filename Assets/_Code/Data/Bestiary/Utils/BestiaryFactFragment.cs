using BeauUtil;
using System.Text;
using UnityEngine;

namespace Aqua
{
    public struct BFFragment
    {
        public BestiaryFactFragmentType Type;
        public StringSlice String;

        static public BFFragment CreateWord(BestiaryFactFragmentType inType, StringSlice inWord)
        {
            return new BFFragment()
            {
                Type = inType,
                String = inWord
            };
        }

        static public BFFragment CreateLocNoun(TextId inWord)
        {
            return new BFFragment()
            {
                Type = BestiaryFactFragmentType.Noun,
                String = Services.Loc.Localize(inWord, true)
            };
        }

        static public BFFragment CreateGenderedLocNoun(TextId inWord, TextId inArticle)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Services.Loc.Localize(inArticle, true));
            if (!inArticle.IsEmpty) {
                builder.Append(" ");
            }
            builder.Append(Services.Loc.Localize(inWord, true));

            return new BFFragment()
            {
                Type = BestiaryFactFragmentType.Noun,
                String = builder.ToString()
            };
        }

        static public BFFragment CreateVerb(StringSlice inWord)
        {
            return new BFFragment()
            {
                Type = BestiaryFactFragmentType.Verb,
                String = inWord
            };
        }

        static public BFFragment CreateLocVerb(TextId inWord)
        {
            return new BFFragment()
            {
                Type = BestiaryFactFragmentType.Verb,
                String = Services.Loc.Localize(inWord, true)
            };
        }

        static public BFFragment CreateAdjective(StringSlice inWord)
        {
            return new BFFragment()
            {
                Type = BestiaryFactFragmentType.Adjective,
                String = inWord
            };
        }

        static public BFFragment CreateLocAdjective(TextId inWord)
        {
            return new BFFragment()
            {
                Type = BestiaryFactFragmentType.Adjective,
                String = Services.Loc.Localize(inWord, true)
            };
        }

        static public BFFragment CreateAmount(float inAmount)
        {
            return new BFFragment()
            {
                Type = BestiaryFactFragmentType.Amount,
                String = inAmount.ToString()
            };
        }

        static public BFFragment CreateAmount(StringSlice inWord)
        {
            return new BFFragment()
            {
                Type = BestiaryFactFragmentType.Amount,
                String = inWord
            };
        }
    }

    public enum BestiaryFactFragmentType : byte
    {
        Noun,
        Verb,
        Adjective,
        Amount,
        Conjunction,
        Condition,
        Image,
    }
}